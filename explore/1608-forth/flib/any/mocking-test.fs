\ test mocking.fs

include testing.fs
include mocking.fs

55 mocks \ simple case with single mock
  m> 55 =always
depth 0 =always
 
1 2 3 4 5 mocks \ test case with multiple
  m> 1 =always
  m> 2 =always
  m> 3 =always
  m> 4 =always
  m> 5 =always
depth 0 =always

56 expect \ simple test case with one expectations
  56 e?
  e=
depth 0 =always

6 7 8 9 0 expect \ test case with multiple expectations
  6 e?
  7 e?
  8 e?
  9 e?
  0 e?
  e=
depth 0 =always

1 2 expect \ consume too many expectations
  1 e?
  2 e?
  e=
depth 0 =always

." *** The following tests will fail by design!" cr

1 2 mocks \ consume too many mocks
  m> 1 =always
  m> 2 =always
  m> drop
  BAD-tests @ 1 =always  -1 BAD-tests +!
depth 0 =always

1 2 expect \ consume too many expectations
  1 e?
  2 e?
  3 e?
  BAD-tests @ 1 =always  -1 BAD-tests +!
depth 0 =always

1 2 expect \ consume too few expectations
  1 e?
  e=
  BAD-tests @ 1 =always  -1 BAD-tests +!
depth 0 =always

\ ===== example for how to use

\ the example uses SPI and mocks the spi2> function
: spi2> ( reg -- c ) \ read register (MOCKED)
  e? \ check expectation of input param
  m> \ return mocked value
  ;

\ example function to be tested
: too-hot ( -- f ) \ read temperature in reg 45 and return whether it's over 100 degrees
  45 spi2> 100 > ;

\ example tests
45 expect 99 mocks \ test that it returns false below 100
  too-hot false =always
  e= \ check that all expectations were consumed
45 expect 100 mocks \ test that it returns false at 100
  too-hot false =always
  e= \ check that all expectations were consumed
45 expect 101 mocks \ test that it returns true above 100
  too-hot true =always
  e=

  

test-summary
