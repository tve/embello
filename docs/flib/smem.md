# SPI flash chip driver for Winbond W25Q16 .. W25Q128

[code]: spi/smem.fs (spi)
* Code: <a href="https://github.com/jeelabs/embello/tree/master/explore/1608-forth/flib/spi/smem.fs">spi/smem.fs</a>
* Needs: spi

This is an SPI driver for flash chips such as the W25Q16 through W25Q128 series and many other
compatibles. It provides the basics to read, erase, write flash. No support is provided for
locking or double/quad access modes.

The W25 series of flash chips are organized into 4K sectors containing 256-byte pages. The smallest
erasable unit is a sector and the smallest writable unit is a byte, however, a single write is
confined to a page (cannot cross page boundaries).

The performance specs vary, but typically more than 100000 erase/program cycles are supported and
the flash chip can be read at over 50Mbps. A sector erase takes 45-400ms and a page write takes
0.7-3ms.

### API

To initialize the driver `spi-init` needs to be called.
Then `smem-id` can be used to retrieve the flash chip manufacturer ID and device ID as a 
24-bit value, `smem-uid` can be used to read a 64-bit unique ID programmed into the chip and
`smem-size` can be used to calculate the size of the flash chip in KB based on its ID.

Most of the words take a page address parameter. This is a byte address in the page right-shifted by
8 bits (since a page contains 256 bytes).

[defs]: <> (smem-id smem-uid smem-size)
```
: smem-id ( -- u)  \ return the SPI memory's manufacturer and device ID (24-bit value)
: smem-uid ( -- u1 u2 )  \ return the chip's 64-bit unique ID
: smem-size ( -- u )  \ return size of spi memory chip in KB
```

To erase flash memory use `smem-wipe` to erase the entire chip (takes 40-200 seconds) and
`smem-erase` to erase one 4K sector. The parameter to `smem-erase` is the address of the first
page in the sector to erase.

[defs]: <> (smem-wipe smem-erase)
```
: smem-wipe ( -- )  \ wipe entire flash memory
: smem-erase ( page -- )  \ erase one 4K sector in flash memory
```

To read/write flash memory the `smem>` and `>smem` words read and write a 256-byte page,
respectively. `smem.1>` reads one byte. Note that the `>smem` write operation busy-waits for
the page flashing operation to complete (typ. 0.7ms).

In order to write a page and not wait for completion use `>smem*`. This word does busy-wait
_before_ starting the write, so it is safe to call `>smem*` rapidly without explicitly
busy-waiting.

[defs]: <> (smem> >smem smem.1> >smem*)
```
: smem> ( addr page -- )  \ read 256 bytes from flash page
: >smem ( addr page )  \ write 256 bytes to specified page
: smem.1> ( addr flash-addr -- c )  \ read 1 byte from flash address
: >smem* ( addr page )  \ write 256 bytes to specified page, no-wait
```

Smaller blocks can be read and written using `smem.n>` and `>smem.n`.
The `>smem.n` operation does not wait for the flashing to complete.
Note that when using `>smem.n` the block must fit inside one page, it is not possible to
write across page boundaries using one write operation.

[defs]: <> (smem.n> >smem.n)
```
: smem.n> ( addr len flash-addr -- )  \ read len bytes from flash address
: >smem.n ( addr len flash-addr -- ) \ write len bytes to flash address (must fit into page), no wait
```

The `smem-wait` and `smem-ready` words can be used explicitly to wait for the flash chip
to be idle/ready.
`smem-wait` busy-loops until the chip is ready while `smem-ready`performs a single status register
read and returns true if the chip is idle/ready.

[defs]: <> (smem-wait smem-ready)
```
: smem-wait ( -- )  \ wait in a busy loop as long as spi memory is busy, calls pause
: smem-ready ( -- f ) \ return memory ready status as boolean flag
```
