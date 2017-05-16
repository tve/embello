include testing.fs
include mocking.fs

\ ===== redefine smem primitives for tests

: >smem-start ( hdr flash-addr -- ) \ start write to flash address and push header
  swap e? e? ;
: >smem-buf ( addr len -- ) \ push buffer to flash and end spi transaction
  swap e? e? ;
: >smem.n ( addr len flash-addr -- )
  rot e? swap e? e? ;
: smem-size 16384 ;
: smem-erase e? ;

: smem-init ;
: smem.1> ( addr -- c ) e? m> ;

include smem-rec.fs

\ ===== test xs?

save-size

    0 next-addr !  0 xs?  0 =always
$0ff0 next-addr ! 16 xs?  0 =always
$0ff0 next-addr ! 17 xs? -1 =always
 4000 next-addr ! 96 xs?  0 =always
$2ff0 next-addr ! 16 xs?  0 =always
$2ff0 next-addr ! 17 xs? -1 =always

\ ===== test page-left

  0 next-addr ! page-left 256 =always
200 next-addr ! page-left  56 =always
255 next-addr ! page-left   1 =always
256 next-addr ! page-left 256 =always

\ ===== test rec-buf

256 buffer: buf

10 0 buf 10 expect  0 next-addr ! \ simple case: 10-byte buffer written at flash addr 0
  buf 10 rec-buf  e=
  next-addr @ 11 =always

20 1000 buf 20 expect  1000 next-addr ! \ write 20 @1000, short of boundary @1024
  buf 20 rec-buf  e=
  next-addr @ 1021 =always

23 1000  buf 23 expect  1000 next-addr ! \ write 23 @1000, just fits incl hdr
  buf 23 rec-buf  e=
  next-addr @ 1024 =always

24 1000  buf 23  buf 23 + 1 1024 expect  1000 next-addr ! \ write 24 @1000: one over boundary
  buf 24 rec-buf  e=
  next-addr @ 1025 =always

223 1000  buf 23  buf 23 + 200 1024 expect  1000 next-addr ! \ write 223 @1000: 200 over
  buf 223 rec-buf  e=
  next-addr @ 1224 =always

\ ===== test rec

\ repeat same tests as for rec-buf, should have same results
10 0 buf 10 expect  0 next-addr ! \ simple case: 10-byte buffer written at flash addr 0
  buf 10 rec  e=
  next-addr @ 11 =always

\ tests that cross sector boundary
95 4000 buf 95 expect  4000 next-addr ! \ fill last byte of sector
  buf  95 rec  e=
  next-addr @ 4096 =always

96 4096 buf 96 32 expect  4000 next-addr ! \ cross sector by one
  buf  96 rec  e=
  next-addr @ 4193 =always

100 4096  buf 100  32 expect  4000 next-addr ! \ cross sector: bumped to next sect
  buf 100 rec  e=
  next-addr @ 4197 =always

\ test wrap-around at end

100 12288 buf 100 0 expect  12200 next-addr ! \ trick: wrap to erase sect 0
  buf 100 rec  e=
  next-addr @ 12389 =always

100 0 buf 100 16 expect  16300 next-addr ! \ trick: wrap to write page 0
  buf 100 rec  e=
  next-addr @ 101 =always

\ ===== test free-sect

$ff mocks      0 expect \ first sector is free
  free-sect     0 =always  e=
$23 $ff mocks  0 4096 expect \ second sector is free
  free-sect  4096 =always  e=
0 1 2 3 mocks  0 4096 8192 12288 expect \ all 4 sectors are full
  free-sect    -1 =always  e=

\ ===== test busy-sect

$23 mocks  4096 expect \ first sector is busy
  0 busy-sect 4096 =always  e=
$ff $23 mocks  4096 8192 expect \ first sector is free, second busy
  0 busy-sect 8192 =always  e=
$ff $23 mocks  12288 expect \ last sector is free, first is busy
  8192 busy-sect 0 =always  e=
$ff $ff $ff $ff mocks  4096 8192 12288 expect \ all sectors free
  0 busy-sect 0 =always  e=

\ ===== test sect-end

$ff mocks  0 expect \ sector is empty
  0 sect-end  0 =always  e=
23 $ff mocks  0 24  expect \ one msg len 23, then free
  0 sect-end  24 =always  e=
10 $ff $ff mocks  4085 expect \ one msg goes to end of sect
  4085 sect-end  4096 =always  e=

\ ===== test rec-init

$ff $ff mocks  0 12288 expect \ flash is empty
  rec-init  0 =always e=
  next-addr @ 0 =always

10 $ff 10 $ff mocks  0 4096 0 11 expect \ first sector has 10 bytes, second empty
  rec-init  0 =always  e=
  next-addr @ 11 =always

10 11 12 13 mocks 0 4096 8192 12288 expect \ flash is full (error condition)
  rec-init  -1 =always  e=

$ff 10 20 $ff mocks 0 12288 12299 12320 expect \ first sect free, last has 10 and 20 byte msg
  rec-init 0 =always  e=
  next-addr @ 12288 11 + 21 + =always

\ ===== test pb-init

$ff 10 mocks  0 4096 expect \ first sect free, second has 10 byte msg
  pb-init 0 =always  e=
  next-addr @ 4096 =always

10 20 30 $ff mocks  0 4096 8192 12288 expect \ first 3 sects have data, last is free
  pb-init 0 =always  e=
  next-addr @ 0 =always

10 20 30 40 mocks 0 4096 8192 12288 expect \ all sects full (error)
  pb-init -1 =always  e=

\ ===== test pb

test-summary
