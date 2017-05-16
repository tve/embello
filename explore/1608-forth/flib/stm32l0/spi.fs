\ hardware SPI driver

[ifndef] ssel  PA4 variable ssel  [then]  \ can be changed at run time
[ifndef] SCLK  PA5 constant SCLK  [then]
[ifndef] MISO  PA6 constant MISO  [then]
[ifndef] MOSI  PA7 constant MOSI  [then]

0 variable ssel.addr \ address where to toggle chip select
0 variable ssel.bit  \ bitmask to toggle for chip select

$40013000 constant SPI1
     SPI1 $00 + constant SPI1-CR1
     SPI1 $04 + constant SPI1-CR2
     SPI1 $08 + constant SPI1-SR
     SPI1 $0C + constant SPI1-DR
\    SPI1 $10 + constant SPI1-CRCPR
\    SPI1 $14 + constant SPI1-RXCRCR
\    SPI1 $18 + constant SPI1-TXCRCR

: spi? ( -- )
  SPI1
  cr ."     CR1 " dup @ hex. 4 +
     ."     CR2 " dup @ hex. 4 +
     ."      SR " dup @ hex. 4 +
     ."      DR " dup @ hex. 4 +
  cr ."   CRCPR " dup @ hex. 4 +
     ."  RXCRCR " dup @ hex. 4 +
     ."  TXCRCR " dup @ hex. drop ;

\ : +spi ( -- ) ssel @ ioc! ;  \ select SPI
\ : -spi ( -- ) ssel @ ios! ;  \ deselect SPI
\ faster:
: +spi ( -- ) $10000 ssel.bit @ lshift ssel.addr @ ! inline ;  \ select SPI
: -spi ( -- )      1 ssel.bit @ lshift ssel.addr @ ! inline ;  \ deselect SPI

: >spi> ( c -- c )  \ hardware SPI, 8 bits
  SPI1-DR !  begin SPI1-SR @ 1 and until  SPI1-DR @ ;

\ single byte transfers
: spi> ( -- c ) 0 >spi> ;     \ read byte from SPI
: >spi ( c -- ) >spi> drop ;  \ write byte to SPI

\ ===== faster SPI for devices that use 2-byte cycles for register+data

\ calculate SPI1-DR from SPI1-CR
: spi1>dr ( spi1-sr -- spi1-dr ) 4 + inline ;
\ wait for tx ready
: spi-txrdy ( spi1-sr -- spi1-sr ) begin dup @ 2 and until inline ;
\ wait for rx ready
: spi-rxrdy ( spi1-sr -- spi1-sr ) begin dup @ 1 and until inline ;
\ wait for rx ready and drop rx byte
: spi-rxdrop ( spi1-sr -- spi1-sr ) begin dup @ 1 and until dup spi1>dr @ drop inline ;
\ push byte into SPI1-DR
: spi-push ( c spi1-sr -- spi1-sr ) swap over spi1>dr ! inline ;
\ push zero into SPI1-DR
: spi-push0 ( spi1-sr -- spi1-sr ) 0 over spi1>dr ! inline ;

: >spi2 ( c reg -- ) \ write register
  +spi SPI1-SR ( c reg spi1-sr )
  spi-push spi-rxdrop
  spi-push spi-rxrdy
  spi1>dr @ drop
  -spi
  ;

: spi2> ( reg -- c ) \ read register
  +spi SPI1-SR ( reg spi1-sr )
  spi-push spi-rxdrop ( spi1-sr )
  spi-push0 spi-rxrdy ( spi1-sr )
  spi1>dr @ ( c )
  -spi
  ;

: >spi.N ( addr len spi-sr -- spi-sr ) \ write len bytes to spi loop starts with spi-rxdrop
  >r 0 ?do ( addr R:spi-sr lim ix )
    dup c@ ( addr c )
    2 rpick spi-rxdrop spi-push drop ( addr )
    1+ ( addr+1 )
  loop
  drop r> spi-rxdrop ( spi1-sr )
  ;

: >spiN ( addr len reg -- ) \ write len bytes to reg
  +spi
  SPI1-SR spi-push ( addr len spi1-sr )
  >spi.N drop -spi
  ;

: spi.N> ( addr len spi-sr -- ) \ read len bytes from spi, loop starts with spi-push0
  >r 0 ?do ( addr )
    2 rpick spi-push0 spi-rxrdy ( addr )
    spi1>dr @ ( addr c )
    over c! 1+ ( addr )
  loop drop rdrop
  ;

: spiN> ( addr len reg -- ) \ read len bytes from reg
  +spi
  SPI1-SR spi-push spi-rxdrop ( addr len spi1-sr )
  spi.N> -spi
  ;

\ ===== initialization

: fix-ssel ( -- ) \ internal to calculate ssel.bit & ssel.addr
  ssel @
  dup OMODE-PP swap io-mode!
  dup io-base GPIO.BSRR + ssel.addr !
  io# ssel.bit !
  -spi
  ;

: spi!ssel ( ssel -- ) \ set chip-select pin, e.g. "PA4 spi!ssel"
  ssel ! fix-ssel
  ;

: spi-init ( -- )  \ set up hardware SPI
  fix-ssel
  OMODE-AF-PP SCLK   io-mode!
  OMODE-AF-PP MISO   io-mode!
  OMODE-AF-PP MOSI   io-mode!

  12 bit RCC-APB2ENR bis!  \ set SPI1EN
  %0000001101000100 SPI1-CR1 !  \ clk/2, i.e. 8 MHz, master
  SPI1-SR @ drop  \ appears to be needed to avoid hang in some cases
  2 bit SPI1-CR2 bis!  \ SS output enable
;
