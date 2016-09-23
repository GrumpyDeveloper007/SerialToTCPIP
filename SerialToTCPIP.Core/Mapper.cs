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
    public class Mapper
    {
        TcpListener Port;
        Socket client;

        SerialPort ComPort = new SerialPort();
        const int MaxBuffer = 8192;
        private DateTime LastDataRx;

        Thread TCPIPToSerial;


        public void OpenPort(string address,int port, string serialPort )
        {
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

            Debug.WriteLine("Server is listening on " + Port.LocalEndpoint);
            while (true)
            {
                if (Port.Pending())
                {
                    client = Port.AcceptSocket();

                    Debug.WriteLine("Connection accepted.");

                    try
                    {
                        while (client.Connected)
                        {
                            System.Threading.Thread.Sleep(100);

                            int size = client.Receive(data);
                            if (size > 0)
                            {
                                Debug.WriteLine("ip rx:" + BitConverter.ToString(data,0,size) + "(" + size + ")");
                                SerialWrite(data, size);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
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
                Debug.WriteLine("ip tx:" + BitConverter.ToString(data, 0, sendLen) + "(" + sendLen + ")");
            }
            catch (Exception ex)
            { }
        }

        public void SerialWrite(byte[] data, int sendLen)
        {
            this.ComPort.Write(data, 0, sendLen);
            Debug.WriteLine("s tx:" + BitConverter.ToString(data, 0, sendLen) + "(" + sendLen + ")");
        }

        private void ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            LastDataRx = DateTime.Now;
            byte[] rxBuffer = new byte[MaxBuffer];
            int tmpLen = ComPort.Read(rxBuffer, 0, MaxBuffer);
            Debug.WriteLine("s rx:" + BitConverter.ToString(rxBuffer, 0, tmpLen) + "(" + tmpLen + ")");
            TCPIPWrite(rxBuffer, tmpLen);
        }


    }
}
