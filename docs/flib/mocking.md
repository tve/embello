# Mocking and expectations library for unit tests

[code]: any/mocking.fs (testing)
* Code: <a href="https://github.com/jeelabs/embello/tree/master/explore/1608-forth/flib/any/mocking.fs">any/mocking.fs</a>
* Needs: testing

The mocking library enables simple mocking of dependencies and checking of expectations.
In order to test a word that uses some dependency the idea is to override the
dependency and implement it using the mocking library.

### API

[defs]: <> (mocks m>)
```
: mocks ( v1 .. vN -- ) \ save values into mock buffer so they can be accessed in mocks
: m> ( -- v ) \ pull next value from mockbuf
```

[defs]: <> (expect e? e=)
```
: expect ( v1 .. vN -- ) \ save values into expectations buffer so they can be checked
: e? ( v -- ) \ verify that next expectation matches
: e= ( -- ) \ verify that all expectations were consumed
```

### Example

This example tests a word `too-hot` that uses the SPI library,
and specifically the `spi2>` word. The tests mocks the `spi2>` word's response and verifies
that it is called with the correct parameter.

```
\ this example uses SPI and mocks the spi2> function
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
  e=

45 expect 101 mocks \ test that it returns true above 100
  too-hot true =always
  e=
```
A more complete example can be found in `smem-rec-test.fs`.

