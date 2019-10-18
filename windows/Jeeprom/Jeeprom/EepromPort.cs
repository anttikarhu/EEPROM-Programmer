using System;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;

namespace Jeeprom
{
    class EepromPort
    {
        enum Status
        {
            NONE,
            SCANNING_PORTS,
            SAYING_HELLO,
            GETTING_VERSION,
            FOUND_DEVICE,
            SENDING_HEARTBEAT,
            ERASING,
            READING,
            STOPPED
        }

        private const string EXPECTED_HELLO_RESPONSE = "bonjour :)";
        private const string EXPECTED_VERSION = "0";
        private const int SCAN_INTERVAL = 500;
        private const int CONNECTION_INTERVAL = 2;
        private const int HEARTBEAT_INTERVAL = 4;

        public event EventHandler FoundBoard;
        public event EventHandler LostBoard;
        public event EventHandler<EraseProgressEventArgs> EraseProgress;
        public event EventHandler EraseDone;
        public event EventHandler<ReadProgressEventArgs> ReadProgress;
        public event EventHandler ReadDone;

        private Status status = Status.NONE;
        private string[] portNames;
        private SerialPort port;
        private StringBuilder readBuffer = new StringBuilder();

        public void Reset()
        {
            status = Status.NONE;
            Scan();
        }

        public async void Scan()
        {
            if (status == Status.NONE || status == Status.SCANNING_PORTS)
            {
                Close();

                status = Status.SCANNING_PORTS;

                portNames = SerialPort.GetPortNames();
                if (portNames.Length > 0)
                {
                    port = new SerialPort(portNames[0], 57600, Parity.None, 8, StopBits.One);
                    port.ReadTimeout = 500;
                    port.WriteTimeout = 500;
                    port.Open();

                    Console.WriteLine("Connecting...");

                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(CONNECTION_INTERVAL));
                        if (port.IsOpen)
                        {
                            break;
                        }
                    }

                    if (port.IsOpen)
                    {
                        port.DataReceived += Port_DataReceived;
                        port.ErrorReceived += Port_ErrorReceived;
                        port.PinChanged += Port_PinChanged;

                        SayHello();
                    }
                    else
                    {
                        Console.WriteLine("Could not open port");
                        Stop();
                    }
                }
                else
                {
                    Console.WriteLine("No ports connected, retrying in {0} seconds", SCAN_INTERVAL / 1000.0);
                    await Task.Delay(TimeSpan.FromMilliseconds(SCAN_INTERVAL));
                    Scan();
                }
            }
            else
            {
                Console.WriteLine("Now now, status: " + status);
            }
        }

        public void Erase()
        {
            status = Status.ERASING;
            port.WriteLine("/erase");
        }

        public void Zero()
        {
            status = Status.ERASING;
            port.WriteLine("/zero");
        }

        internal void Read()
        {
            status = Status.READING;
            readBuffer.Clear();
            port.WriteLine("/read");
        }

        private async void SayHello()
        {
            if (status == Status.SCANNING_PORTS && port != null && port.IsOpen)
            {
                status = Status.SAYING_HELLO;
                port.WriteLine("/hello");
            }

            // Wait a second
            await Task.Delay(TimeSpan.FromSeconds(CONNECTION_INTERVAL));

            if (status == Status.SAYING_HELLO)
            {
                Console.WriteLine("No hello response :(");
                Stop();
            }

        }

        private void GetVersion()
        {
            if (status == Status.SAYING_HELLO && port != null && port.IsOpen)
            {
                status = Status.GETTING_VERSION;
                port.WriteLine("/version");
            }
        }

        private async void Heartbeat()
        {
            if (status == Status.ERASING || status == Status.READING)
            {
                Console.WriteLine("Busy {0}, skipping heartbeat", status);
                await Task.Delay(TimeSpan.FromSeconds(HEARTBEAT_INTERVAL));
                Heartbeat();
                return;
            }

            try
            {
                if (status == Status.FOUND_DEVICE && port != null && port.IsOpen)
                {
                    await Task.Delay(TimeSpan.FromSeconds(HEARTBEAT_INTERVAL));
                    //status = Status.SENDING_HEARTBEAT;
                    port.WriteLine("/hello");
                    Heartbeat();
                }
                else
                {
                    Console.WriteLine("Cannot do heartbeat, no connection");
                    Reset();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Heartbeat failed: " + e.Message);
                Reset();
            }
        }

        public void Close()
        {
            if (port != null)
            {
                port.Close();
                port = null;
                status = Status.NONE;
                Console.WriteLine("Closed port");
                OnLostBoard(new EventArgs());
            }
        }

        public void Stop()
        {
            Close();
            status = Status.STOPPED;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = "";
            try
            {
                data = port.ReadLine().Trim();
            }
            catch
            {
                Console.WriteLine("Something went wrong");
                Reset();
            }

            if (status == Status.SAYING_HELLO)
            {
                if (EXPECTED_HELLO_RESPONSE.Equals(data))
                {
                    Console.WriteLine("Got a proper board...");
                    GetVersion();
                }
                else
                {
                    Console.WriteLine("Wrong hello response {0}", data);
                    Stop();
                }
            }
            else if (status == Status.GETTING_VERSION)
            {
                if (EXPECTED_VERSION.Equals(data))
                {
                    Console.WriteLine("... with proper firmware version");
                    status = Status.FOUND_DEVICE;
                    OnFoundBoard(new EventArgs());
                    Heartbeat();
                }
                else
                {
                    Console.WriteLine("Unsupported firmware version {0}", data);
                    Stop();
                }
            }
            else if (status == Status.FOUND_DEVICE)
            {
                if (EXPECTED_HELLO_RESPONSE.Equals(data))
                {
                    Console.WriteLine("All is well");
                }
                else
                {
                    Console.WriteLine("Wrong heartbeat response '{0}', stopping", data);
                    Stop();
                }
            }
            else if (status == Status.ERASING)
            {
                if ("/done".Equals(data))
                {
                    Console.WriteLine("Erase done");
                    status = Status.FOUND_DEVICE;
                    OnEraseDone(new EventArgs());
                }
                else if (data.StartsWith("/erased "))
                {
                    int progress = Convert.ToInt32(data.Substring(data.IndexOf("/erased ") + "/erased ".Length));
                    Console.WriteLine("Erase progress: {0}", progress);
                    OnEraseProgress(new EraseProgressEventArgs(progress));
                } else
                {
                    Console.WriteLine("Erase failed");
                    Reset();
                }
            }
            else if (status == Status.READING)
            {
                if ("/done".Equals(data))
                {
                    Console.WriteLine("Read done");
                    status = Status.FOUND_DEVICE;
                    OnReadDone(new EventArgs());
                    readBuffer.Clear();
                }
                else if(data != null && data.Length > 4)
                {
                    int progress = Convert.ToInt32(data.Substring(0, 4), 16);
                    Console.WriteLine("Read progress: {0}", progress);

                    // Invoke event when 64 bytes of data have been read to give UI some slack
                    readBuffer.Append(data);
                    readBuffer.Append("\r");
                    if ((progress + 16) % 64 == 0)
                    {
                        OnReadProgress(new ReadProgressEventArgs(progress, readBuffer.ToString()));
                        readBuffer.Clear();
                    }
                } else
                {
                    Console.WriteLine("Read failed");
                    Reset();
                }
            }
        }

        private void Port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("Error: " + e.ToString());
        }

        private void Port_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Console.WriteLine("PinChanged: " + e.ToString());
        }

        protected virtual void OnFoundBoard(EventArgs e)
        {
            EventHandler eventHandler = FoundBoard;
            eventHandler?.Invoke(this, e);
        }

        protected virtual void OnLostBoard(EventArgs e)
        {
            EventHandler eventHandler = LostBoard;
            eventHandler?.Invoke(this, e);
        }

        protected virtual void OnEraseProgress(EraseProgressEventArgs e)
        {
            EventHandler<EraseProgressEventArgs> eventHandler = EraseProgress;
            eventHandler?.Invoke(this, e);
        }

        protected virtual void OnEraseDone(EventArgs e)
        {
            EventHandler eventHandler = EraseDone;
            eventHandler?.Invoke(this, e);
        }

        protected virtual void OnReadProgress(ReadProgressEventArgs e)
        {
            EventHandler<ReadProgressEventArgs> eventHandler = ReadProgress;
            eventHandler?.Invoke(this, e);
        }

        protected virtual void OnReadDone(EventArgs e)
        {
            EventHandler eventHandler = ReadDone;
            eventHandler?.Invoke(this, e);
        }
    }
}
