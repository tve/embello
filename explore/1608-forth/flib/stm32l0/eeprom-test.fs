include eeprom.fs
include ../any/testing.fs

create ee-data
  hex $01020304 , $05060708 , $00fb090a , decimal

\ write bytes, read back as half
$ab 3 ee!c $cd 2 ee!c 2 ee@h $abcd =always
\ write half, read back as bytes
$4567 dup 2 ee!h 3 ee@c 8 lshift 2 ee@c or =always
\ write word, read back half
$1234567e 4 ee! 6 ee@h $1234 =always
\ write halves, read back word
$4567 2 ee!h  $abcd 0 ee!h 0 ee@ $4567abcd =always

\ write buffer read back words
ee-data 11 4 ee!buf   0 15 ee!c
  4 ee@ ee-data @ =always
  8 ee@ ee-data 4 + @ =always
  12 ee@ ee-data 8 + @ =always

\ read back buffer and check
16 buffer: buf
buf 16 4 ee@buf
  ee-data 11 buf 11 compare always

test-summary
