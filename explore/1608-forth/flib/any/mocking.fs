\ mocking.fs contains primitives to support mocking and expectations in unit tests

16 cells buffer: mockbuf   \ values to be provided in mocks
16 cells buffer: expectbuf \ values expected
mockbuf variable mockptr
expectbuf variable expptr
expectbuf variable expstart

: mocks ( v1 .. vN -- ) \ save values into mock buffer so they can be accessed in mocks
  depth cells mockbuf + mockptr ! \ pointer past last value
  depth 0 do mockbuf i cells + ! loop \ save in reverse order
  ;

: m> ( -- v ) \ pull next value from mockbuf
  mockptr @ dup mockbuf = if
    ." FAIL: consuming too many mocks" fail-test
    drop 0 exit
  then
  1 cells - dup mockptr !
  @ ;

: expect ( v1 .. vN -- ) \ save values into expectations buffer so they can be checked
  depth cells expectbuf + dup expptr ! expstart ! \ pointer past last value
  depth 0 do expectbuf i cells + ! loop \ save in reverse order
  ;

: e? ( v -- ) \ verify that next expectation matches
  expptr @ dup expectbuf = if
    ." FAIL: consuming too many expectations" fail-test
    2drop exit
  then
  1 cells - dup expptr !
  @ 2dup <> if
      ." FAIL: expectation " expstart @ expptr @ - 2 rshift .
      swap ." got " . ." expected " . cr fail-test
  else 2drop then ;

: e= ( -- ) \ verify that all expectations were consumed
  expptr @ expectbuf - 2 rshift ?dup if
    ." FAIL: " . ." expectations were not consumed" fail-test
  then ;

