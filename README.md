# EEPROM-Programmer

## Overview
The goal is to make an Arduino EEPROM programmer that can write, read and erase EEPROMs of up to 16 bit address lines. I'm using them to write 8kB EEPROMs 28C64.

The design principle is that one can control the Arduino fully via Serial text commands. That way I can develop the Arduio side without need for additional software besides the Arduino Serial Monitor. After programming Arduino (Nano), type /? to the Serial Monitor to get started. Another goal is to create a Windows app to flash ROM images from a file.

Big thanks to Ben Eater and his eeprom-programmer (https://github.com/beneater/eeprom-programmer) we are familiar from the Ben Eater 8-bit computer series (check his Youtube channel for more info: https://www.youtube.com/channel/UCS0N5baNlQWJCUrhCEo8WlA). The Arduino code is heavily influenced by him.

There's no schematics of the hardware side of it. It follows almost exactly the Ben Eater schematics available online. Only difference (Ben's using 28C16 EEPROM) is that I assume 3 74LS595 shift registers instead of 2, because that way one can achieve the 16 bit address line. Also the extra address lines must be connected to the EEPROM to thei proper pins.


## What's in the project
### Arduino EEPROM programmer
Designed on a cheap Arduino Nano clone from
China, so I guess almost any Arduino with serial port will work :)
The programmer currently assumes 28C64 eeprom, and a Ben-Eater-like
circuit. It accepts text command via serial, so one can test (or use) 
it manually.

### Windows C# app
Simple client that can make the Arduino
serial commands, and erase/zero, read and write
data to EEPROM. Currently the serial communication
is done without special flow control, and it
is tested to work only with my computer setup :) 
If it does not work, one could adjust the baud
rate, or how much ROM data is sent in one command.
Data verification and per byte editing would be nice
additions.

### PCB
I modified the https://github.com/SukkoPera/OpenC64Cart 
PCB design by removing all I did not need for 8K roms.
You can find a pic and gerber files.

### Test C64 Cartridge app
I took one of my learning projects (C64-Multicolor-background),
and modified it to work from a cartridge. It's just
copy-pasted between coldstart-warmstart and 8K padding
sections, and needed no other modifications, luckily.
