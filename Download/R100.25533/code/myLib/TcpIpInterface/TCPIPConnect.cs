using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Windows.Forms;

namespace TcpIpInterface
{



    public class TcpLogDataEventArgs : EventArgs
    {
        public TcpLogDataEventArgs(string buffer)
        {
            Data = buffer;
        }

        public string Data { get; }
    }

    public class TcpServerDataEventArgs : EventArgs
    {
        public TcpServerDataEventArgs(byte[] buffer)
        {
            _data = buffer;
        }
        public byte[] _data;
    }

    public class TcpClientDataEventArgs : EventArgs
    {
        public TcpClientDataEventArgs(byte[] buffer)
        {
            _data = buffer;
        }
        public byte[] _data;
    }
        
    public class TcpIpServer
    {
        public static string TraceClass;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<TcpClient> _connectedClients;
        public event EventHandler<TcpServerDataEventArgs> NewRxServerTcpData;
        public event EventHandler<TcpLogDataEventArgs> TcpServerDebugData;

        public TcpIpServer()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            //_tcpListener = new TcpListener(System.Net.IPAddress.Any, port);
            _connectedClients = new List<TcpClient>();

        }
        public async Task Connect(string localendpoint, int port)
        {
            var localAddr = IPAddress.Parse(localendpoint);
            _tcpListener = new TcpListener(localAddr, port);

            _cancellationTokenSource = new CancellationTokenSource();
            try
            {

                _tcpListener.Start();
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server started. Listener Local Endpoint='{_tcpListener.LocalEndpoint}'");
                TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server started. Listener Local Endpoint='{_tcpListener.LocalEndpoint}'"));
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    using (var client = await _tcpListener.AcceptTcpClientAsync())
                    {
                        _connectedClients.Add(client);
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server connected to Client from='{client.Client.RemoteEndPoint}'");
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Local Endpoint='{client.Client.LocalEndPoint}'");
                        TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server connected to Client from='{client.Client.RemoteEndPoint}'"));
                        await HandleClientAsync(client, _cancellationTokenSource.Token); // Handle each client asynchronously
                            await Task.Delay(1000);
                    }
                }
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Clearing Client List");
#endif
                foreach (var client in _connectedClients)
                {
                    DisconnectClient(client);
                    client.Close();
                    client.Dispose();
                }
                _cancellationTokenSource.Cancel();
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Listener Stopped");
#endif
                _tcpListener?.Stop(); // Shut down server
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Shutdown Complete");
                TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server Shutdown Complete"));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server error: {ex.Message}");
            }
            finally
            {

            }
        }
        public List<TcpClient> GetConnectedClients()
        {
            return _connectedClients;
        }
        public async Task Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : User request Server Shutdown");
            TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"User request Server Shutdown"));
            CloseAllClients();
            // Wait for the server to stop accepting new clients
            await Task.Delay(1000);
#if DEBUG
#endif
        }
        public void ListenerStop()
        {
            _tcpListener.Stop();
        }
        public void CloseAllClients()
        {
            foreach (var client in _connectedClients)
            {
                DisconnectClient(client);
            }
         //   _connectedClients.Clear(); // Clear the stored list of connected clients
        }
        private void DisconnectClient(TcpClient client)
        {
            try
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Attempt to Disconnect: {client.Client.RemoteEndPoint}");
#endif
                client.Close();
                client.Dispose();
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client disconnected : {client.Client.RemoteEndPoint}");
#endif
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Error disconnecting client: {ex.Message}");
            }
        }
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server starting networkStream");
#endif
                using (var networkStream = client.GetStream())
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var buffer = new byte[1024];
#if DEBUG
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server waiting for message");
#endif
                        var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        NewRxServerTcpData?.Invoke(this, new TcpServerDataEventArgs(buffer));
#if DEBUG
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server message received");
#endif
                        if (cancellationToken.IsCancellationRequested) // Immediate Exit was requested
                        {
#if DEBUG
                            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Exit was requested, Discard All input");
#endif
                            break;  // User commanding Server to shut down, exit the loop
                        }
                        if (bytesRead <= 0) // call from
                        {
                            TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server Detected Client Disconnected"));
                            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Detected Client Disconnected");
                            break;  // Server have detected Client disconnected, exit the loop
                        }

                        var receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        // Receive and display messages from the client
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Received from='{client.Client.RemoteEndPoint}' Message='{receivedMessage}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server to end the client");
#endif
                client.Close();
                // Proceed to clear list if Client Disconnected or Server shut down
                _connectedClients.Clear();
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server has cleared list of active devices");
#endif
            }
        }
        public async Task SendMessageToClientAsync(TcpClient client, string message)
        {
            try
            {
                var stream = client.GetStream();
                var data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                Trace.WriteLine($"TcpIpServer SendMessageToClientAsync: Server Sent to='{client.Client.RemoteEndPoint} : Message='{message}'");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"TcpIpServer SendMessageToClientAsync: Error sending to='{client.Client.RemoteEndPoint}' : {ex.Message}");
            }
        }
        public IPEndPoint GetListenerLocalEndpoint()
        {
            if (_tcpListener != null && _tcpListener.LocalEndpoint is IPEndPoint localEndPoint)
            {
                return localEndPoint;
            }
            else
            {
                return null; // or throw an exception or handle it as per your requirement
            }
        }
        public IPEndPoint GetServerLocalEndpoint(TcpClient client)
        {
            if (client != null && client.Client.LocalEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint;
            }
            else
            {
                return null; // or throw an exception or handle it as per your requirement
            }
        }
        public IPEndPoint GetServerRemoteEndpoint(TcpClient client)
        {
            if (client != null && client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint;
            }
            else
            {
                return null; // or throw an exception or handle it as per your requirement
            }
        }
    }

    public class TcpIpClient
    {
        public static string TraceClass;
        private TcpClient _client;
        private CancellationTokenSource _cancellationTokenSource;
        public event EventHandler<TcpClientDataEventArgs> NewRxClientTcpData;
        public event EventHandler<TcpLogDataEventArgs> TcpClientDebugData;
        public bool IsConnected => _client?.Connected ?? false;
        public TcpIpClient()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }
        public async Task Connect(string serverIp, int serverPort)
        {
            if (!IsConnected)
            { 
                try
                {

                    _client = new TcpClient();
                    await _client.ConnectAsync(serverIp, serverPort);
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client connected to server at='{_client.Client.RemoteEndPoint}'");
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client Local Endpoint='{_client.Client.LocalEndPoint}'");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Client connected to server at='{_client.Client.RemoteEndPoint}'"));


                    // Start a background task to monitor the connection
                    // This was initially added because the IsConnected property was not getting updated.
                    // We can keep this if we want to get periodic status of client connection
                    _cancellationTokenSource = new CancellationTokenSource();
                    _ = Task.Run(() => MonitorConnection(_cancellationTokenSource.Token));
                    // Another background task for RxMessage
                    _ = Task.Run(() => ReceiveMessageAsync(_cancellationTokenSource.Token));

                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error connecting to server: {ex.Message}");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Error connecting to server: {ex.Message}"));
                }
            } 
            else
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Already connected to Server at '{serverIp}:{serverPort}', IsConnected Satus='{IsConnected}'");
                TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Already connected to Server at '{serverIp}:{serverPort}', IsConnected Satus='{IsConnected}'"));
            }
        }
        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (IsConnected)
                {
                    var stream = _client.GetStream();
                    var data = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client Sent to='{_client.Client.RemoteEndPoint}' : Message='{message}'");
                }
                else 
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Unable to send message, server offline : {message}");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error sending message : {ex.Message}");
            }
            finally
            {
#if debug
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Completed Send Client Message : {message}");
#endif
            }
        }
        public async Task ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var stream = _client.GetStream();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {

                        NewRxClientTcpData?.Invoke(this, new TcpClientDataEventArgs(buffer));


                        var receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client Received from='{_client.Client.RemoteEndPoint}' Message='{receivedMessage}'");
                                               
                        // return receivedMessage;
                    }
                    else
                    {
                       // return null; // Or any other appropriate value indicating no message received
                    }
                }
            }
            catch (Exception ex)
            {
                TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Client Error receiving message"));
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error receiving message : {ex.Message}");
               // return null; // Or any other appropriate value indicating an error
            }
        }
        public async Task Disconnect()
        {
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : User request Client Disconnect");
            TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"User request Client Disconnect"));
            try
            {
                _cancellationTokenSource.Cancel();
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                    await Task.Delay(1000);
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client is disconnected");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Client is disconnected"));
                    _client = null;
                }
                else
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client was never initialized");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Client was never initialized"));

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error stopping TCP client : {ex.Message}");
            }
        }
        private async Task MonitorConnection(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken); // Adjust the delay as needed

                if (!IsConnected)
                {
                    try
                    {

#if DEBUG
                        // Server disconnected
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : The Client no longer detect the Server.. Closing Client");
#endif
                        TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Connection Lost.. Closing Client"));
                        if ( _client != null )
                        {
                            _client.Close();
                            _client.Dispose();
                            _client = null;
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Monitor Disconnect Error : {ex.Message}");
                    }

                    
                }
                else
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : The Client can detect Server");
#endif
                }
            }
        }
        public IPEndPoint GetClientLocalEndpoint()
        {
            if (_client.Client.LocalEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint;
            }
            else
            {
                return null; // or throw an exception or handle it as per your requirement
            }
        }
        public IPEndPoint GetClientRemoteEndpoint()
        {
            if (_client.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
            {
                return remoteEndPoint;
            }
            else
            {
                return null; // or throw an exception or handle it as per your requirement
            }
        }
    }
}
