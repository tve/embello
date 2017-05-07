\ on-chip EEPROM

$08080000 constant EEPROM       \ start of eeprom
$40022004 constant FLASH_PECR   \ flash/eeprom control register
$4002200C constant FLASH_PEKEYR \ key register to unlock flash/eeprom
$40022018 constant FLASH_SR     \ flash status register

: ee-unlock ( -- ) \ unlock the flash/eeprom writing
  FLASH_PEKEYR
  $89ABCDEF over !
  $02030405 swap !
  ;

: ee-wait ( -- ) \ wait 'til write completes
  begin FLASH_SR @ 1 and not until ;
: ee-lock ( -- ) \ lock flash/eeprom writing
  ee-wait 7 FLASH_PECR ! ;

: ee! ( v off -- ) \ write 32-bit word v to EEPROM offset off
  ee-unlock EEPROM + ! ee-lock ;

: ee!h ( v off -- ) \ write 16-bit half-word v to EEPROM offset off
  ee-unlock EEPROM + h! ee-lock ;

: ee!c ( v off -- ) \ write byte v to EEPROM offset offc
  ee-unlock EEPROM + c! ee-lock ;

: ee@ ( off -- v ) \ read 32-bit word from EEPROM offset off
  EEPROM + @ ;

: ee@h ( off -- v ) \ read 16-bit half-word from EEPROM offset off
  EEPROM + h@ ;

: ee@c ( off -- v ) \ read byte from EEPROM offset off
  EEPROM + c@ ;

: ee@buf ( addr count off -- ) \ read into buffer from EEPROM offset off, off&3==0
  EEPROM + rot ( count eeaddr addr )
  \ copy full bytes
  2 pick 3 not and 0 ?do ( count eeaddr addr )
    over @ over !
    4 + swap  4 + swap
  4 +loop
  \ copy remaining bytes
  rot 3 and 0 ?do ( eeaddr addr )
    over c@ over c!
    1+ swap 1+ swap
  loop
  2drop ;

: ee!buf ( addr count off -- ) \ write from buffer to EEPROM offset off, off&3==0
  ee-unlock EEPROM + rot swap ( count addr eeaddr )
  \ copy full bytes
  2 pick 3 not and 0 ?do ( count eeaddr eeaddr )
    over @ over ee-wait !
    4 + swap  4 + swap
  4 +loop
  \ copy remaining bytes
  rot 3 and 0 ?do ( addr eeaddr )
    over c@ over ee-wait c!
    1+ swap 1+ swap
  loop
  2drop ee-lock ;
