\ record messages into SPI Flash memory using the smem driver.

\ Assumes that flash consists of 4KB erasable sectors and 256 byte pages.
\ Supports writing up to 255 byte long messages (not 256!). Writes them using a length header
\ byte followed by the data

0 variable next-addr \ next address to store/retrieve message
0 variable size-mask \ memory size -1 to use as mask

\ ===== special smem primitives to handle writing of length byte separately from following buffer

[ifndef] >smem-start \ allow redefinition of smem primitives for testing purposes

: >smem-start ( hdr flash-addr -- ) \ start write to flash address and push header
  smem-wait $06 smem-cmd -spi \ wait-ready, write-enable command
  $02 smem-cmd-addr ( hdr spi-sr )
  spi-rxdrop spi-push ( spi-sr ) \ write hdr
  ;

: >smem-buf ( addr len -- ) \ push buffer to flash and end spi transaction
  >spi.N drop -spi ; \ write the buffer

[then]

\ ===== helpers

: xs? ( len -- f ) \ crossing into the next sector? (or filling current one)
  next-addr @ $fff and $1000 swap - > ;

: page-left ( -- n ) \ number of bytes left in page
  next-addr @ $ff and $100 swap - ;

: erase-next ( -- ) \ erase sector after the current being filled, needs to wrap-around
  next-addr @ $fff or 1+ smem-size 1- and 8 rshift smem-erase ; \ 16 pages per sector

: wrap ( flash-addr -- flash-addr ) \ wraps flash address around end
  size-mask @ and inline ;

: next-sect ( flash-addr -- flash-addr ) \ move up to start of next sector with wrap
  $fff or 1+ wrap ;

\ ===== recording

: rec-buf ( addr len -- ) \ record buffer to flash at current next-addr (helper)
  \ if crossing page boundary, record first part
  page-left 2dup >= if ( addr len left ) \ crossing into next page? (>= due to len prefix)
    \ need to perform 2 writes, first finish-up current page
    over next-addr @ cr >smem-start ( unchanged ) \ start writing current page
    1- tuck -               ( addr left-1 len-left+1 ) \ amount to write now, amt to write next
    over 1+ next-addr +!    ( addr left-1 len-left+1 ) \ bump next-addr
    -rot 2dup >smem-buf     ( len-left+1 addr left-1 ) \ finish writing current page
    \ write remainder into next page
    + swap tuck next-addr @ >smem.n  ( len-left+1 )
    next-addr +!            ( ) \ bump next-addr
  else drop 
    dup next-addr @ >smem-start ( addr len )
    dup 1+ next-addr +!     ( unchanged) \ bump next-addr
    >smem-buf               ( ) \ finish writing
  then ;

: rec ( addr len -- ) \ record buffer to flash, ensure following sector is erased
  dup 1+ xs? dup if ( addr len xs ) \ 1+ due to len prefix
    \ don't write buffers across sector boundaries, instead move to next sector
    next-addr dup @ next-sect swap !
  then -rot ( xs addr len )
  rec-buf
  \ if crossed sector boundary, erase following sector
  if erase-next then
  ;

\ ===== playback

: pb ( addr -- len ) \ play back next buffer into addr and return its length, -1 if there is nothing
  \ read length byte where we are
  next-addr @ dup smem.1> ( addr flash-addr len ) \ read length byte
  cr .v
  dup $ff = if
    \ no message here, if not at start of sector, see whether the next sector has something
    \ this happens because we don't write a partial message to the end of a sector
    swap ( addr len flash-addr )
    .v
    dup $fff and 0= if 2drop drop -1 exit then \ already at start of sector, no data left, return -1
    next-sect
    dup next-addr ! \ save new flash address
    nip dup smem.1> ( addr flash-addr len ) \ read length byte
    dup $ff = if 2drop drop -1 exit then \ no data left, return -1
  then ( addr flash-addr len )
  \ do bookkeeping and get actual message
  dup >r \ save length
  swap 1+ 2dup + next-addr ! ( addr len flash-addr+1 ) \ bump next-addr
  ." >" .v smem.n> r> \ read data and return length
  ;

\ ===== initialization

: free-sect ( -- flash-addr ) \ find first free sector in smem
  size-mask @ 1+ 0 do
    i smem.1> $ff = if \ first byte of sector $ff => free sector
      i unloop exit
    then
  4096 +loop
  -1 ; \ no free sector, something's amiss

: busy-sect ( flash-addr -- flash-addr ) \ find first busy sector after flash-addr, -1 if none
  size-mask @ swap
  begin ( size-mask flash-addr )
    4096 + over and
    dup 0= if nip exit then \ wrapped around to where we started, flash is empty
    dup smem.1> ( size flash-addr c )
    $ff <> if nip exit then \ found non-erased sector
  again ;

: sect-end ( flash-addr -- flash-addr ) \ given start of sector, find first free location
  begin ( flash-addr )
    dup smem.1> dup $ff = if drop exit then \ fetch length, $ff means it's free
    + 1+ \ jump past buffer
    dup $fff and 0= if exit then \ if at next sect start, prev sect was full to last byte
  again ;

: save-size smem-size 1- size-mask ! ;

: rec-init ( -- nak ) \ initialize recording, which finds the first free page
  smem-init save-size free-sect ( flash-addr )
  dup -1 = if drop -1 exit then \ no free sector, something's bad
  4096 - wrap \ start of previous sector w/wrap-around
  sect-end \ find first free byte of that sector
  dup size-mask @ 4095 - = if drop 0 then \ special case when all of flash is free
  size-mask @ and next-addr ! \ save where we start recording
  0 ;

: pb-init ( -- nak ) \ initialize playback, which finds the first written sector after a free one
  smem-init save-size free-sect ( flash-addr )
  dup -1 = if exit then \ no free sector, something's bad
  busy-sect next-addr !
  0 ;
