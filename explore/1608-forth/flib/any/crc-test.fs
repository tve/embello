include testing.fs
include crc.fs

create test-data
  hex $01020304 , $abcdef89 , decimal

: t ( n -- crc )
  $ffff test-data rot 0 do ( crc test-data )
    dup i + c@ ( crc test-data b )
    rot crc16 swap
  loop drop
  ;

$ffff test-data 16 crc16buf  16 t  =always
16 t 8 t <> always

test-summary




