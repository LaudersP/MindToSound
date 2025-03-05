using CoreOSC;
using CoreOSC.IO;
using System;
using System.Net;
using System.Net.Sockets;

namespace CortexAccess
{
    public class OSC
    {
        private UdpClient _udpClient;
        private string _ipAddress;
        private int _portNum;
        private IPEndPoint _receiveEndpoint;
        private Utils _utilities = new Utils();

        public OSC(string ipAddress, int portNum)
        {
            _ipAddress = ipAddress;
            _portNum = portNum;

            // Create the instance of the client for sending
            _udpClient = new UdpClient(_ipAddress, _portNum);

            // Set up endpoint for receiving from the specific address
            _receiveEndpoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _portNum);

            _utilities.SendSuccessMessage("Sending data on [" + _portNum + "]");
        }

        public void SendMessage(string address, params object[] arguments)
        {
            var message = new OscMessage(new Address(address), arguments);
           
            _udpClient.SendMessageAsync(message).Wait();
        }

        // Clean up resources when done
        public void Close()
        {
            _udpClient?.Close();
            _udpClient = null;
        }
    }
}