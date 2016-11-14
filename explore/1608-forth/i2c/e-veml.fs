\ VEML6040 readout over I2C experiment
\ needs core.fs
cr cr reset

include ../flib/i2c-stm32l0.fs
\ include ../flib/veml6040.fs

\ assumes that the VEML6040 sensor is connected to PB6..PB7

: veml-init
  $10 i2c-tx drop  $00 >i2c drop i2c-stop
  $10 i2c-rx drop
  0 i2c> 1 i2c> \ i2c-stop
  8 lshift or hex.

\ $10 i2c-tx drop  $00 >i2c drop i2c-stop
\ ." aha! "
\ $10 i2c-tx drop  $00 >i2c drop i2c-stop
\ ." oho! "
;

: veml-rd ( reg -- val )
  $10 i2c-tx drop >i2c drop $10 i2c-rx drop 0 i2c> 1 i2c> \ i2c-stop
  8 lshift or ;

: veml-data
  1 2 3
  8 veml-rd
;

: go
  veml-init
  begin
    500 ms
    cr
    micros veml-data 2>r 2>r micros swap - . ." µs: " 2r> 2r>
    ." w: " .  ." b: " .  ." g: " .  ." r: " .
    $30 i2c-tx drop i2c-stop  \ FIXME hangs with back-to-back accesses to $29!
  key? until ;

+i2c 100 ms i2c? \ i2c.

\ this causes folie to timeout on include matching, yet still starts running
\ 1234 ms go

: conf
  $10 i2c-tx drop
  $00 >i2c drop
  $00 >i2c drop
  $00 >i2c drop
  i2c-stop ;
: reg ( reg -- )
  $10 i2c-tx drop
  >i2c drop
\ i2c-start
  i2c-stop
  $10 i2c-rx drop
  0 i2c> 1 i2c> i2c-stop
  8 lshift or . ;
: rd $08 reg $09 reg $0A reg $0B reg ;

conf rd
