\ Simple support for unit tests.

0 variable OK-tests
0 variable BAD-tests
: good-test 1 OK-tests +! ; \ used by assertions
: fail-test 1 BAD-tests +! ; \ used by assertions

: test-summary ( -- ) \ print a summary of tests
  BAD-tests @ ?dup if ." ** " . ." TESTS FAILED! (" OK-tests @ . ." OK) **" else
  depth 0<> if ." ** ALL " OK-tests @ . ." TESTS OK but stack not empty: " .v else
  ." ** ALL " OK-tests @ . ." TESTS OK **" then then cr ;

: =always ( n1 n2 -- ) \ assert that the two TOS values must be equal
  2dup <> if
    ." FAIL: got " swap . ." expected " . fail-test
  else 2drop good-test then ;

: =always-fix ( df1 df2 -- ) \ assert that the two TOS fixed-point values must be equal
  2dup 2rot 2dup 2rot ( df2 df1 df1 df2 )
  d<> if
    ." FAIL: got " f. ." expected " f. fail-test
  else 2drop 2drop good-test then ;

: always ( f -- ) \ assert that the flag on TOS is true
  0= if
    ." FAIL!" fail-test
  else good-test then ;
