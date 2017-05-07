\ driver for a Bosch BNO055 9-dof IMU sensor with sensor fusion.
\ needs i2c

: addr $28 i2c-addr ;             \ set the device i2c address
: reg ( n -- ) addr >i2c ;        \ select register n
: reg! ( v n -- ) reg >i2c 0 i2c-xfer drop ; \ write v into reg n
: reg@ ( n -- v ) reg 1 i2c-xfer drop i2c> ; \ read byte from reg n
: page ( n -- ) 7 swap reg! ;     \ switch to register page n

: id? ( -- f ) 0 page 0 reg@ $a0 = ; \ verify the CHIP ID

: bno-reset ( -- nak ) \ reset the chip and wait 'til it inits, hangs if it doesn't init
  id? not if -1 exit then \ bail if this is not a bno055
  $20 $3f reg!        \ trigger reset in sys_trigger register
  begin $39 reg@      \ read sys_status register
    1 u> while        \ 0=idle, 1=error, 2=init-periph, 3=sys-init, 4=bist, 5=fusion, 6=non-fusion
  10 ms repeat
  $39 reg@ if
    $3a reg@          \ init error, read error register
  else 0
  then ;

: bno-init ( -- nak ) \ reset and initialize the chip, takes ~600ms!
  i2c-init bno-reset ?dup if exit then
  $86 $3b reg! \ android orientation, radians for euler & gyroscope
  $00 $3e reg! \ normal power mode
  $24 $41 reg! \ axis remapping
  $00 $42 reg! \ axis sign remapping
  $0c $3d reg! \ set operation mode ($a=ndof_fmc_off, $c=ndof)
  0 ;

: bno.info ( -- ) \ print some info about the chip versions
  ." BNO-055: "
  0 reg
  7 i2c-xfer drop
  7 0 do i2c++ c@ h.2 space loop
  $39 reg@ .
  cr ;

14 buffer: bno.data \ data in halfwords: quaternion w, x, y, z; linear accel x, y, z

: bno-data ( -- f ) \ write data to bno.data, return calibration flag (0=uncal, 1=ok, 2=perfect)
  $20 reg 14 i2c-xfer drop  \ transfer quaternion and accel data
  bno.data 14 0 do          \ save into our buffer
    i2c> over i + c!
  loop drop
  $35 reg@ $c0 2dup and = if \ fetch calibration, $c0 is system cal
    $ff = if 2 else 1 then \ $ff is system and all sensors cal
  else drop 0
  then
  ;

: bno-calib@ ( -- count ) \ fetch calibration data into bno.data, returns number of bytes
  $00 $3d reg! \ set operation mode to config
  $55 reg 22 i2c-xfer drop
  bno.data 22 0 do i2c> over i + c! loop drop
  $0c $3d reg! \ set operation mode to ndof
  ;

: bno-calib! ( -- ) \ write calibration data from bno.data
  $00 $3d reg! \ set operation mode to config
  $55 reg
  bno.data 22 0 do dup i + c@ >i2c loop drop
  0 i2c-xfer drop
  $0c $3d reg! \ set operation mode to ndof
  ;

: bno. ( -- ) \ fetch and print current data
  bno-data
  case
  0 of ." uncal q(" endof
  1 of ." ~cal  q(" endof
  2 of ." calib q(" endof
  endcase
  bno.data
  4 0 do dup h@ 16 lshift 16 arshift . 2+ loop \ print quaternions
  ." ) a("
  3 0 do dup h@ 16 lshift 16 arshift . 2+ loop \ print accel
  drop
  [char] ) emit
  $34 reg@ .
  ;

: bno-test \ print quaternion, accel, and system registers every second
  bno-init if ." Cannot find/init BNO055" exit then
  cr bno.info 0 page
  begin
    bno. cr
    0 dup h.2 [char] : emit reg@ h.2 space
    $43 $34 do i dup h.2 [char] : emit reg@ h.2 space loop cr
    1000 ms
  key? until
  ;

bno-test
