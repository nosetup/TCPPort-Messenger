#region Credits
/* 
 * Modified by N Diep
 * 
*/
#endregion

#region Namespace Inclusions
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
#endregion

namespace UdpInterface
{
    public class UdpDataEventArgs : EventArgs
    {
        public UdpDataEventArgs(byte[] buffer, IPEndPoint udpendpoint)
        {
            Buffer = buffer;
            Udpendpoint = udpendpoint;
        }

        public byte[] Buffer { get; }  // Property to hold the message buffer
        public IPEndPoint Udpendpoint { get; }  // Property to hold the associated TcpClient object
    }

    public class UdpConnect
    {
        #region Local Variables
        public static string TraceClass;
        private bool _exitRequested = false;
        #endregion

        #region Constructor
        static UdpClient s_udpClient;
        static IPEndPoint s_localEndPoint;
        private Thread _receiveThread;
        public event EventHandler<UdpDataEventArgs> NewRxUdpData;
        public UdpConnect()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : UDP Messaging App Starting ..");
        }
        #endregion

        #region Local Methods
        /// <summary> Convert a string (ex: "123") to int. </summary>
        /// <param name="s"> The string containing the digits </param>
        /// <returns> Returns an int value. </returns>
        public int StringtoInt(string s)
        {
            try
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : String : {s}");
#endif
                s = s.TrimEnd(); // Trim trailing spaces from the input string
                s = s.Replace(" ", ""); // Remove spaces from the modified string
                var intValue = int.Parse(s);
            }
            catch (FormatException)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Invalid Input : {s}");
            }
            catch (OverflowException)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Overflow Ex : {s}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Unexpected Exception ex : {ex.Message}");
            }

            if (int.TryParse(s, out var parsedValue))
            {
                return parsedValue;
            }
            else
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : fail to parse: var='{s}'");
                return 0;
            }
        }
        public bool SocketConnect(string strport)
        {
            // Set the local port for receiving messages
            var localPort = StringtoInt(strport);
            try
            {
                _exitRequested = false;
                
                if (s_udpClient == null)
                {
                    s_udpClient = new UdpClient(); // Set up the UDP client for sending and receiving
                    s_localEndPoint = new IPEndPoint(IPAddress.Any, localPort); // Create an endpoint for receiving messages on any available IP address
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Starting UDP Client");
#endif
                }
                else
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Udp Client is already open.. Closing and Restarting Udp Client.");
#endif
                    s_udpClient.Close();
                    s_localEndPoint = null;

                    s_udpClient = new UdpClient();
                    s_localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
                }
                s_udpClient.Client.Bind(s_localEndPoint); // Bind the UDP client to the local endpoint

                if (_receiveThread == null)
                {
                    _receiveThread = new Thread(RxUdpMsg); // Start a new thread for receiving messages
                    _receiveThread.Start();
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Receive Thread Started.");
#endif
                }
                else
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Receive Thread is already running.");
#endif
                }
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Socket Connected at='{s_localEndPoint.Address}:{s_localEndPoint.Port}'");
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Error in Connect Udp: {ex.Message}");
                return false;   
            }
        }
        /// <summary>
        /// UDP Connection status</summary>
        public bool UdpIsConnected()
        {
            // Check if the UDP client is initialized and the receiving thread is alive
            return s_udpClient != null && _receiveThread != null && _receiveThread.IsAlive;
        }
        /// <summary>
        /// UDP Disconnect</summary>
        public void SocketDisconnect()
        {
            // Set the exit flag to true
            _exitRequested = true;
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} Set Exit Request");
            if (s_udpClient != null)
            {
                // Close the UDP client and the receiving thread
                s_udpClient.Close();
                s_udpClient.Dispose();
                s_udpClient = null;
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} Client Closed ..");
            }
            if (_receiveThread != null)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} Stoping Receive");
                _receiveThread.Join();
                _receiveThread.Abort();
                _receiveThread = null;
            }
        }
        /// <summary>
        /// UDP Recieve message</summary>
        public void RxUdpMsg()
        {
            while (!_exitRequested)
            {
                if (s_udpClient.Available > 0)
                {
                    var receivedBytes = s_udpClient.Receive(ref s_localEndPoint); // Receive a message and the endpoint it was sent from
                    var receivedMessage = Encoding.ASCII.GetString(receivedBytes);
                    NewRxUdpData?.Invoke(this, new UdpDataEventArgs(receivedBytes, s_localEndPoint)); // Pass RxMessage to UI
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : From ='{s_localEndPoint}' : Msg='{receivedMessage}'");
#endif
                }
                else
                {
                    Thread.Sleep(10); // Introduce a small delay if no data is available Adjust the sleep duration as needed
                }
            }
        }
        /// <summary>
        /// UDP Send message</summary>
        public void TxUdpMsg(string ipaddress, int remotePort, string message)
        {
            var messageBytes = Encoding.ASCII.GetBytes(message);
            var remoteIpAddress = IPAddress.Parse(ipaddress);
            var remoteEndPoint = new IPEndPoint(remoteIpAddress, remotePort);
            s_udpClient.Send(messageBytes, messageBytes.Length, remoteEndPoint);
        }
        #endregion

        #region Event Handlers
        #endregion

    }

}
