using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Jeeprom.Connection
{
    class EepromPortWatcher
    {
        enum Status
        {
            NONE,
            SCANNING_PORTS,
            SAYING_HELLO,
            GETTING_VERSION,
            FOUND_DEVICE,
            SENDING_HEARTBEAT,
            STOPPED
        }

        private const string EXPECTED_HELLO_RESPONSE = "bonjour :)";
        private const string EXPECTED_VERSION = "0";
        private const int SCAN_INTERVAL = 2;
        private const int HEARTBEAT_INTERVAL = 4;

        public event EventHandler FoundBoard;
        public event EventHandler LostBoard;

        private Status status = Status.NONE;
        private string[] portNames;
        private SerialPort port;

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
                        await Task.Delay(TimeSpan.FromSeconds(SCAN_INTERVAL));
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
                    Console.WriteLine("No ports connected, retrying in {0} seconds", SCAN_INTERVAL);
                    await Task.Delay(TimeSpan.FromSeconds(SCAN_INTERVAL));
                    Scan();
                }
            }
            else
            {
                Console.WriteLine("Now now, status: " + status);
            }
        }

        private async void SayHello()
        {
            if (status == Status.SCANNING_PORTS && port != null && port.IsOpen)
            {
                status = Status.SAYING_HELLO;
                port.WriteLine("/hello");
            }

            // Wait a second
            await Task.Delay(TimeSpan.FromSeconds(SCAN_INTERVAL));

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
                    status = Status.NONE;
                    Scan();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Heartbeat failed: " + e.Message);
                status = Status.NONE;
                Scan();
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
                Scan();
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
                    status = Status.FOUND_DEVICE;
                }
                else
                {
                    Console.WriteLine("Wrong heartbeat response '{0}', stopping", data);
                    Stop();
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
    }
}
