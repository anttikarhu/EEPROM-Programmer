#define SERIAL_OUT    2
#define REG_CLK       3
#define SHIFT_CLK     4
#define WRITE_ENABLE  5
#define DATA_0        6
#define DATA_7        13
#define LED           13 // Led and data 7 share a common pin, so the output must be set accordingly when reading and using led the "same" time

#define READ  true
#define WRITE false

#define READ_FLAG   0x00
#define WRITE_FLAG  0x80

#define PROTOCOL_VERSION        0
#define COMMAND_INDICATOR_SPEED 33
#define ERROR_INDICATOR_SPEED   200

// For now this will do for configuration
#define ROM_MAX_SIZE 0x2000

#define COMMANDS "commands: /hello, /version, /write, /data, /erase, /zero, /read, /status"

boolean busyWriting = false;
int dataChunkSize = 0;
int startAddr = 0;
int totalSize = 0;
int totalWritten = 0;
int currentWriteAddr = 0;

void setup() {
  Serial.begin(57600);

  digitalWrite(SERIAL_OUT, LOW);
  pinMode(SERIAL_OUT, OUTPUT);

  digitalWrite(REG_CLK, LOW);
  pinMode(REG_CLK, OUTPUT);

  digitalWrite(SHIFT_CLK, LOW);
  pinMode(SHIFT_CLK, OUTPUT);

  digitalWrite(WRITE_ENABLE, LOW);
  pinMode(WRITE_ENABLE, OUTPUT);
}

void loop() {
  setLed(LOW);

  if (Serial.available() > 0) {
    String line = Serial.readStringUntil('\n');

    if (line.startsWith("/")) {
      setLed(HIGH);

      if (!busyWriting) {
        if (line.equals("/hello")) {
          hello();
        } else if (line.equals("/version")) {
          version();
        } else if (line.startsWith("/write")) {
          write(line);
        } else if (line.startsWith("/data")) {
          notNow("use /write command first");
        } else if (line.startsWith("/erase")) {
          erase();
        } else if (line.startsWith("/zero")) {
          zero();
        } else if (line.startsWith("/read")) {
          read();
        } else if (line.startsWith("/status")) {
          status();
        } else {
          unknownCommand(line);
        }
      } else {
        if (line.startsWith("/data")) {
          data(line);
        } else if (line.startsWith("/status")) {
          status();
        } else {
          notNow("please write all the /data first");
        }
      }

      delay(COMMAND_INDICATOR_SPEED);
    } else {
      Serial.println("start command with /");
    }
  }
}

void hello() {
  Serial.println("bonjour :)");
}

void version() {
  Serial.println(PROTOCOL_VERSION);
}

void write(String command) {
  busyWriting = true;

  dataChunkSize = getParamValue(command, "dataChunkSize").toInt();
  if (dataChunkSize <= 0) {
    dataChunkSize = ROM_MAX_SIZE;
  }

  startAddr = getParamValue(command, "startAddr").toInt();
  if (startAddr < 0) {
    startAddr = 0;
  }

  totalSize = getParamValue(command, "totalSize").toInt();
  if (totalSize <= 0) {
    totalSize = ROM_MAX_SIZE;
  }

  totalWritten = 0;
  currentWriteAddr = startAddr;

  Serial.println("/waiting data");
  startWrite();
}

void data(String command) {
  busyWriting = true;

  String data = command.substring(command.indexOf(' ') + 1);

  char m = -1;
  char l = -1;
  int writeStart = currentWriteAddr;

  for (int i = 0; i < data.length(); i++) {
    char c = data.charAt(i);
    if (isHexadecimalDigit(c)) {
      if (m == -1) {
        m = hexToByte(c);
      } else {
        l = hexToByte(c);
      }
    }

    if (m != -1 && l != -1) {
      byte b = 16 * m + l;
      m = -1;
      l = -1;

      writeByte(currentWriteAddr, b);
      currentWriteAddr++;
      totalWritten++;

      if (totalWritten >= totalSize) {
        busyWriting = false;
        Serial.print("/done ");
        Serial.println(totalWritten);
        return;
      }
    }
  }

  if (currentWriteAddr - writeStart == dataChunkSize) {
    Serial.print("/written ");
    Serial.println(totalWritten);
  } else if (currentWriteAddr - writeStart < dataChunkSize) {
    busyWriting = false;
    Serial.print("/done ");
    Serial.println(totalWritten);
  } else {
    busyWriting = false;
    invalidParameter("data chunk expected with length as specified in write command");
  }
}

void erase() {
  startWrite();

  for (int address = 0; address <= ROM_MAX_SIZE; address++) {
    writeByte(address, 0xFF);

    if (address % 64 == 0) {
      Serial.print("/erased ");
      Serial.println(address);
    }
  }

  Serial.println("/done");
}

void zero() {
  startWrite();

  for (int address = 0; address <= ROM_MAX_SIZE; address++) {
    writeByte(address, 0x00);

    if (address % 64 == 0) {
      Serial.print("/erased ");
      Serial.println(address);
    }
  }

  Serial.println("/done");
}

void read() {
  for (int a = 0; a < ROM_MAX_SIZE; a += 16) {
    byte chunk[16];
    for (int i = 0; i < 16; i++) {
      chunk[i] = readByte(a + i);
    }

    char buf[80];

    sprintf(buf, "%04X:  %02X %02X %02X %02X %02X %02X %02X %02X   %02X %02X %02X %02X %02X %02X %02X %02X",
            a, chunk[0], chunk[1], chunk[2], chunk[3], chunk[4], chunk[5], chunk[6], chunk[7],
            chunk[8], chunk[9], chunk[10], chunk[11], chunk[12], chunk[13], chunk[14], chunk[15]);

    Serial.println(buf);
  }

  Serial.println("/done");
}

void status() {
  if (busyWriting) {
    Serial.println("busy");
  } else {
    Serial.println("idle");
  }
}

void unknownCommand(String command) {
  Serial.print("unknown command: ");
  Serial.println(command);
  Serial.println(COMMANDS);

  errorIndicator(2);
}

void invalidParameter(String hint) {
  Serial.print("invalid parameter: ");
  Serial.println(hint);

  errorIndicator(3);
}

void notNow(String hint) {
  Serial.print("not now: ");
  Serial.println(hint);

  errorIndicator(4);
}

void notImplemented(String hint) {
  Serial.print("not implemented: ");
  Serial.println(hint);

  errorIndicator(5);
}

void errorIndicator(int count) {
  delay(ERROR_INDICATOR_SPEED);

  for (int i = 0; i < count - 1; i++) {
    setLed(LOW);
    delay(ERROR_INDICATOR_SPEED);
    setLed(HIGH);
    delay(ERROR_INDICATOR_SPEED);
  }
}

String getParamValue(String command, String paramName) {
  paramName.concat(':');
  String paramStart = command.substring(command.indexOf(paramName) + paramName.length());
  return paramStart.substring(0, paramStart.indexOf(' '));
}

void setAddress(int address, bool direction) {
  shiftOut(SERIAL_OUT, SHIFT_CLK, MSBFIRST, 0 | (direction ? READ_FLAG : WRITE_FLAG));
  shiftOut(SERIAL_OUT, SHIFT_CLK, MSBFIRST, address >> 8);
  shiftOut(SERIAL_OUT, SHIFT_CLK, MSBFIRST, address);

  digitalWrite(REG_CLK, LOW);
  digitalWrite(REG_CLK, HIGH);
  digitalWrite(REG_CLK, LOW);
}

void setLed(boolean state) {
  digitalWrite(LED, state);
  pinMode(LED, OUTPUT);
}

void startWrite() {
  for (int d = DATA_0; d <= DATA_7; d++) {
    digitalWrite(d, LOW);
    pinMode(d, OUTPUT);
  }
  delay(100);
}

byte hexToByte(char hex) {
  if (hex >= 48 && hex <= 57) {
    return hex - 48;
  } else if (hex >= 65 && hex <= 70) {
    return hex - 55;
  } else if (hex >= 97 && hex <= 102) {
    return hex - 87;
  } else {
    return 0;
  }
}

void writeByte(int address, byte value) {
  setAddress(address, WRITE);

  for (int d = DATA_0; d <= DATA_7; d++) {
    digitalWrite(d, value & 1);
    value = value >> 1;
  }

  digitalWrite(WRITE_ENABLE, LOW);
  delayMicroseconds(1);
  digitalWrite(WRITE_ENABLE, HIGH);
  if ((address + 1) % 64 == 0) {
    // Not sure if paging works like this, but having a longer break after each page
    delay(1);
  } else {
    delayMicroseconds(120);
  }

  setLed(HIGH);
}

byte readByte(int address) {
  for (int d = DATA_0; d <= DATA_7; d++) {
    pinMode(d, INPUT);
  }

  setAddress(address, READ);

  byte data = 0;

  for (int d = DATA_7; d >= DATA_0; d--) {
    data = (data << 1) + digitalRead(d);
  }

  setLed(HIGH);

  return data;
}
