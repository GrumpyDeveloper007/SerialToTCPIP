using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;
using System.Net;

namespace SerialToTCPIP.Core
{
    /// <summary>
    /// Simple class to forward data from the serial port to the TCPIP port and visa versa.
    /// </summary>
    public class Mapper
    {
        TcpListener Port;
        Socket client;

        SerialPort ComPort = new SerialPort();
        const int MaxBuffer = 8192;
        private DateTime LastDataRx;
        bool _TCPIPActive;

        Thread TCPIPToSerial;

        public delegate void LogEventDelegate(string eventMessage);
        public event LogEventDelegate TriggerLog;

        public void ClosePort()
        {
            ComPort.Close();
            _TCPIPActive = false;
            Port.Stop();
        }


        /// <summary>
        /// Open the serial port / TCPIP port and start listening for connections
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="serialPort"></param>
        public void OpenPort(string address,int port, string serialPort )
        {
            _TCPIPActive = true;
            IPAddress ipAddress = IPAddress.Parse(address);
            Port = new TcpListener (ipAddress,port);
            Port.Start();

            TCPIPToSerial = new Thread(TCPIPReveiceHandler);
            TCPIPToSerial.Start();

            if (ComPort.IsOpen)
                ComPort.Close();

            ComPort.PortName = serialPort;
            ComPort.BaudRate = 9600;
            ComPort.DataBits = 8;
            ComPort.StopBits = StopBits.One;
            ComPort.Handshake = Handshake.None;
            ComPort.Parity = Parity.None;

            ComPort.DataReceived += ComPort_DataReceived;

            ComPort.DtrEnable = true;
            ComPort.RtsEnable = true;

            ComPort.Open();

        }


        private void TCPIPReveiceHandler()
        {
            byte[] data = new byte[1024];

            LogEvent("Server is listening on " + Port.LocalEndpoint);
            while (_TCPIPActive == true)
            {
                if (Port.Pending())
                {
                    client = Port.AcceptSocket();

                    LogEvent("Connection accepted.");

                    try
                    {
                        while (client.Connected && _TCPIPActive == true)
                        {
                            System.Threading.Thread.Sleep(100);

                            int size = client.Receive(data);
                            if (size > 0)
                            {
                                LogEvent("ip rx:" + BitConverter.ToString(data,0,size) + "(" + size + ")");
                                SerialWrite(data, size);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogEvent(ex.Message);
                    }
                    client.Close();
                }

                System.Threading.Thread.Sleep(100);

            }
        }

        private void TCPIPWrite(byte[] data, int sendLen)
        {
            try
            {
                client.Send(data, sendLen, SocketFlags.None);
                LogEvent("ip tx:" + BitConverter.ToString(data, 0, sendLen) + "(" + sendLen + ")");
            }
            catch (Exception ex)
            { }
        }

        private void SerialWrite(byte[] data, int sendLen)
        {
            this.ComPort.Write(data, 0, sendLen);
            LogEvent("s tx:" + BitConverter.ToString(data, 0, sendLen) + "(" + sendLen + ")");
        }

        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            LastDataRx = DateTime.Now;
            byte[] rxBuffer = new byte[MaxBuffer];
            int tmpLen = ComPort.Read(rxBuffer, 0, MaxBuffer);
            LogEvent("s rx:" + BitConverter.ToString(rxBuffer, 0, tmpLen) + "(" + tmpLen + ")");
            TCPIPWrite(rxBuffer, tmpLen);
        }

        private void LogEvent(string eventMessage)
        {
            Debug.WriteLine(eventMessage);
            TriggerLog?.Invoke(eventMessage);
        }

    }
}
