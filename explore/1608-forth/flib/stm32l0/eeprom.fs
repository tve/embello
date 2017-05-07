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

: ee-lock ( -- ) \ lock flash/eeprom writing
  begin FLASH_SR @ 1 and not until
  7 FLASH_PECR ! ;

: ee! ( v off -- ) \ write 32-bit word v to EEPROM offset off
  ee-unlock EEPROM + ! ee-lock ;

: ee!h
  ee-unlock EEPROM + h! ee-lock ;

: ee!c
  ee-unlock EEPROM + c! ee-lock ;

: ee@ ( off -- v ) \ read 32-bit word from EEPROM offset off
  EEPROM + @ ;

: ee@h ( off -- v ) \ read 16-bit half-word from EEPROM offset off
  EEPROM + h@ ;

: ee@c ( off -- v ) \ read byte from EEPROM offset off
  EEPROM + c@ ;
