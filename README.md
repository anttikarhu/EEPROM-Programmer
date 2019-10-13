# EEPROM-Programmer

The goal is to make an Arduino EEPROM programmer that can write, read and erase EEPROMs of up to 16 bit address lines. I'm using them to write 8kB EEPROMs 28C64.

The design principle is that one can control the Arduino fully via Serial text commands. That way I can develop the Arduio side without need for additional software besides the Arduino Serial Monitor. After programming Arduino (Nano), type /? to the Serial Monitor to get started. Another goal is to create a Windows app to flash ROM images from a file.

Big thanks to Ben Eater and his eeprom-programmer (https://github.com/beneater/eeprom-programmer) we are familiar from the Ben Eater 8-bit computer series (check his Youtube channel for more info: https://www.youtube.com/channel/UCS0N5baNlQWJCUrhCEo8WlA). The Arduino code is heavily influenced by him.

There's no schematics of the hardware side of it. It follows almost exactly the Ben Eater schematics available online. Only difference (Ben's using 28C16 EEPROM) is that I assume 3 74LS595 shift registers instead of 2, because that way one can achieve the 16 bit address line. Also the extra address lines must be connected to the EEPROM to thei proper pins.
