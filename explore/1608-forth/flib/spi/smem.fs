\ Serial flash memory SPI chip interface for WinBond W25Q16, etc.
\ uses spi words, assumes spi-init has been called

: smem-cmd ( cmd -- )  +spi >spi ;
: smem-page ( u -- )  dup 8 rshift >spi >spi 0 >spi ;
: smem-addr ( u -- )  dup 16 rshift >spi dup 8 rshift >spi >spi ;
: smem-wait ( -- )  \ wait in a busy loop as long as spi memory is busy, calls pause
  -spi  begin pause $05 smem-cmd spi> -spi 1 and 0= until ;
: smem-fwait ( -- )  \ fast wait using a single SPI transaction, doesn't pause
  -spi  $05 smem-cmd  begin spi> 1 and 0= until  -spi ;
: smem-ready ( -- f ) \ return memory ready status as boolean flag
  $05 smem-cmd spi> 1 and 0= ;
: smem-wcmd ( cmd -- ) \ wait for ready, write write-enable command followed by cmd command
  smem-wait  $06 smem-cmd -spi smem-cmd ;

: smem-id ( -- u)  \ return the SPI memory's manufacturer and device ID (24-bit value)
  $9F smem-cmd spi> 8 lshift spi> or 8 lshift spi> or -spi ;
: smem-size ( -- u )  \ return size of spi memory chip in KB
  smem-id $FF and 10 -  bit ;

: smem32b ( -- u )  \ get 4 SPI bytes and return them as one 32b word
  0  4 0 do 8 lshift spi> or loop ;
: smem-uid ( -- u1 u2 )  \ return the chip's 64-bit unique ID
  $4B smem-cmd 4 0 do 0 >spi loop smem32b smem32b -spi ;

: smem-wipe ( -- )  \ wipe entire flash memory
  $60 smem-wcmd  smem-wait ;
: smem-erase ( page -- )  \ erase one 4K sector in flash memory
  $20 smem-wcmd smem-page  smem-wait ;

: smem-wipe* ( -- )  \ wipe entire flash memory, no-wait
  $60 smem-wcmd -spi ;
: smem-erase* ( page -- )  \ erase one 4K sector in flash memory, no-wait
  $20 smem-wcmd smem-page -spi ;

\ send command and flash address using fast spi commands
: smem-cmd-addr ( addr cmd -- spi-sr )
  +spi
  SPI1-SR spi-push spi-rxdrop ( addr spi-sr ) \ send command
  over 16 rshift swap spi-push spi-rxdrop \ top address byte
  over  8 rshift swap spi-push spi-rxdrop \ mid address byte
                      spi-push ( spi-sr ) \ low address byte
  ;

\ block reads

: smem.n> ( addr len flash-addr -- )  \ read len bytes from flash address
  $03 smem-cmd-addr spi-rxdrop
  spi.N> -spi ;

: smem> ( addr page -- )  \ read 256 bytes from flash page
  8 lshift 256 swap smem.n> ;

: smem.1> ( addr flash-addr -- c )  \ read 1 byte from flash address
  $03 smem-cmd-addr spi-rxdrop spi-push0 spi-rxrdy spi1>dr @ ;

\ block writes

: >smem.n ( addr len flash-addr -- ) \ write len bytes to flash address (must fit into page), no wait
  smem-wait $06 smem-cmd -spi \ wait-ready, write-enable command
  $02 smem-cmd-addr ( addr len spi-sr )
  >spi.N drop -spi ;

: >smem* ( addr page )  \ write 256 bytes to specified page, no-wait
  8 lshift 256 swap >smem.n ;
: >smem ( addr page )  \ write 256 bytes to specified page
  >smem* smem-wait ;
