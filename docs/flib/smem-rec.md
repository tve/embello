# Record messages to SPI flash chip

[code]: any/smem-rec.fs (smem spi)
* Code: <a href="https://github.com/jeelabs/embello/tree/master/explore/1608-forth/flib/any/smem-rec.fs">any/smem-rec.fs</a>
* Needs: smem, spi

This library records messages (arbitrary byte sequences) of up to 255 byte length to andSPI flash
chip using the smem driver.

The flash chip is used in its entirety (minus one sector that is always free) for the message
storage and there is no index or directory. The library is initialized into recording mode
using `rec-init` and into playback mode using `pb-init`. When recording the new data is appended
to what's already there and the flash memory is used as a circular buffer, i.e., once the chip is
full new messages erase the oldest existing messages. When playing back, the oldest message is
played back first followed by newer messages one at a time.

The storage is organized a sector at a time with messages stored in each sector consisting of a
length byte followed by the message bytes. Length $ff signifies empty space. At all times the filles
sectors are contiguous and when the flash chip is full there is one free sector. When initializing
for recording the first free sector is found and then the previous sector is appended to (there is
a special-case which starts recording in sector 0 if flash is free). When initializing for playback
the first free sector is found and then, going backward, the oldest filles sector is located and
playback starts there.

Note that messages never cross sector boundaries: the first byte of a sector is always the length of
the first message or $ff is the sector is empty.

When writing a message the location is needs to be written to is always already erased. This is why
one free (erased) sector is always maintained. When the first message is written to this sector the
next sector is erased without waiting for the erase to complete (it takes ~100ms on a typical part).
This means that the sector erase can be fully overlapped with the work that produces the next
message.

### API

To record:

[defs]: <> (rec-init rec)
```
: rec-init ( -- nak ) \ initialize recording, which finds the first free page
: rec ( addr len -- ) \ record buffer to flash, ensure following sector is erased
```

To play back:

[defs]: <> (pb-init pb)
```
: pb-init ( -- nak ) \ initialize playback, which finds the first written sector after a free one
: pb ( addr -- len ) \ play back next buffer into addr and return its length, -1 if there is nothing
```
