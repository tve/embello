\ Helpers to work with character buffers

\ stack>buffer copies i bytes from the stack into a buffer
: stack>buffer ( b1 b2 ... bi i c-addr -- c-addr len )
  2dup swap 2>r \ save c-addr len
  over + ( b1 b2 ... bi i c-end-addr )
  swap 0 ?do 1- tuck c! loop
  drop 2r>
  ;

: buffer-cpy ( c-addr1 c-addr2 len -- c-addr1 len ) \ c-addr1 is dest c-addr2 is src
  swap 2 pick 2 pick move ;

: buffer. ( c-addr len -- ) \ print buffer like @<adr> <len> [ <c1> <c2> ... <cLen> ]
  ." @" over hex. dup . ( c-addr len ) \ print address & len
  ." [ $" over c@ h.2 space ( c-addr len ) \ print first byte in hex
  $fff and 1 ?do dup i + c@ . loop drop ." ]" ; \ print remaining bytes in decimal
