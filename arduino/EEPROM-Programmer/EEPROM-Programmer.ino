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

#define COMMAND_NONE    -1
#define COMMAND_HELLO   0
#define COMMAND_VERSION 1
#define COMMAND_WRITE   2
#define COMMAND_DATA    3
#define COMMAND_ERASE   4
#define COMMAND_ZERO    5
#define COMMAND_READ    6
#define COMMANDS "commands: /hello, /version, /write, /data, /erase, /zero, /read, /status"

int currentCommand = COMMAND_NONE;
int totalWritten = 0;
int dataChunkSize = 0;
int startAddr = 0;
int totalSize = 0;

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

      if (currentCommand == COMMAND_NONE) {
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
        } else if (line.startsWith("/testFill")) {
          testFill();
        } else {
          unknownCommand(line);
        }
      } else if (currentCommand == COMMAND_WRITE || currentCommand == COMMAND_DATA) {
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
  currentCommand = COMMAND_HELLO;
  Serial.println("bonjour :)");
  currentCommand = COMMAND_NONE;
}

void version() {
  currentCommand = COMMAND_VERSION;
  Serial.println(PROTOCOL_VERSION);
  currentCommand = COMMAND_NONE;
}

void write(String command) {
  Serial.println("/notImplemented write");
  return;

  currentCommand = COMMAND_WRITE;

  totalWritten = 0;
  dataChunkSize = getParamValue(command, "dataChunkSize").toInt();
  startAddr = getParamValue(command, "startAddr").toInt();
  totalSize = getParamValue(command, "totalSize").toInt();

  if (dataChunkSize > 0 && startAddr >= 0 && totalSize > 0) {
    Serial.println("/waiting data");
  } else {
    invalidParameter("dataChunkSize, startAddr and totalSize as positive integer expected");
    currentCommand = COMMAND_NONE;
  }
}

void data(String command) {
  Serial.println("/notImplemented data");
  return;

  currentCommand = COMMAND_DATA;

  String data = command.substring(command.indexOf(' ') + 1);
  // TODO use start address and total data size parameters

  // TODO decide data format
  // TODO actually write to EEPROM

  // TODO Test code
  if (data.length() == dataChunkSize) {
    totalWritten += data.length();
    Serial.print("/written ");
    Serial.println(totalWritten);
  } else if (data.length() > 0 && data.length() < dataChunkSize) {
    totalWritten += data.length();
    currentCommand = COMMAND_NONE;
    Serial.print("/done ");
    Serial.println(totalWritten);
  } else {
    invalidParameter("data chunk expected with length as specified in write command");
    currentCommand = COMMAND_NONE;
  }
}

void erase() {
  currentCommand = COMMAND_ERASE;

  startWrite();

  for (int address = 0; address <= 0x2000; address++) {
    writeByte(address, 0xFF);

    if (address % 64 == 0) {
      Serial.print("/erased ");
      Serial.println(address);
    }
  }

  Serial.println("/done");
  currentCommand = COMMAND_NONE;
}

void zero() {
  currentCommand = COMMAND_ZERO;
  startWrite();

  for (int address = 0; address <= 0x2000; address++) {
    writeByte(address, 0x00);

    if (address % 64 == 0) {
      Serial.print("/erased ");
      Serial.println(address);
    }
  }

  Serial.println("/done");
  currentCommand = COMMAND_NONE;
}

void read() {
  currentCommand = COMMAND_READ;

  for (int a = 0; a < 0x2000; a += 16) {
    byte chunk[16];
    for (int i = 0; i < 16; i++) {
      chunk[i] = readByte(a + i);
    }

    char buf[80];

    sprintf(buf, "%04x:  %02x %02x %02x %02x %02x %02x %02x %02x   %02x %02x %02x %02x %02x %02x %02x %02x",
            a, chunk[0], chunk[1], chunk[2], chunk[3], chunk[4], chunk[5], chunk[6], chunk[7],
            chunk[8], chunk[9], chunk[10], chunk[11], chunk[12], chunk[13], chunk[14], chunk[15]);

    Serial.println(buf);
  }

  currentCommand = COMMAND_NONE;
}

void status() {
  if (currentCommand == COMMAND_NONE) {
    Serial.println("idle");
  } else {
    Serial.println("busy");
  }
}

void testFill() {
  startWrite();
  for (int i = 0; i < 256; i++) {
    writeByte(i, i);
  }
  Serial.println("/done");
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

void writeByte(int address, byte value) {
  setAddress(address, WRITE);

  for (int d = DATA_0; d <= DATA_7; d++) {
    digitalWrite(d, value & 1);
    value = value >> 1;
  }

  digitalWrite(WRITE_ENABLE, LOW);
  delayMicroseconds(1);
  digitalWrite(WRITE_ENABLE, HIGH);
  if (address % 64 == 0) {
    // Not sure if paging works like this, but having a longer break after each page
    delay(10);
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
