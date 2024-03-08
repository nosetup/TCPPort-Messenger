using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;


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
        public TcpServerDataEventArgs(byte[] buffer, TcpClient client)
        {
            Buffer = buffer;
            Client = client;
        }

        public byte[] Buffer { get; }  // Property to hold the message buffer
        public TcpClient Client { get; }  // Property to hold the associated TcpClient object
    }

    public class TcpClientDataEventArgs : EventArgs
    {
        public TcpClientDataEventArgs(byte[] buffer, TcpClient client)
        {
            Buffer = buffer;
            Client = client;
        }

        public byte[] Buffer { get; }  // Property to hold the message buffer
        public TcpClient Client { get; }  // Property to hold the associated TcpClient object
    }
        
    public class TcpIpServer
    {
        #region Local Variables
        public static string TraceClass;
        #endregion

        #region Constructor
        private TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<TcpListener> _connectedListeners;
        public List<TcpClient> _connectedClients;
        public event EventHandler<TcpServerDataEventArgs> NewRxServerTcpData;
        public event EventHandler<TcpLogDataEventArgs> TcpServerDebugData;
        public TcpIpServer()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            _connectedListeners = new List<TcpListener>();
            _connectedClients = new List<TcpClient>();
        }
        #endregion

        #region Local Variables
        public List<TcpClient> GetConnectedClients()
        {
            return _connectedClients;
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
        #endregion

        #region Local Methods
        public async Task Connect(string localendpoint, int port)
        {
            var localAddr = IPAddress.Parse(localendpoint);

            if (_tcpListener == null)
            {
                _tcpListener = new TcpListener(localAddr, port);
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Creating TCP Listener Object.");
#endif
            }
            else
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : TCP Listener is already open.. Closing and Restarting TCP Listener.");
#endif
            }
            _connectedListeners.Add(_tcpListener);

            if (_cancellationTokenSource == null)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            } else
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
            }
            try
            {
                _tcpListener.Start();
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Waiting for Clients. Listener Local Endpoint='{_tcpListener.LocalEndpoint}'");
                    TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Waiting for Clients. Listener ip='{_tcpListener.LocalEndpoint}'"));
                    var client = await _tcpListener.AcceptTcpClientAsync();
                    try
                    {
                        _connectedClients.Add(client); // Add the connected TcpClient to list of active clients
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server connected to Client from='{client.Client.RemoteEndPoint}'");
                        TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server connected to Client from='{client.Client.RemoteEndPoint}'"));
                        _ = HandleClientAsync(client, _cancellationTokenSource.Token); // Handle multiple client asynchronously
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Client error: {ex.Message}");
                    }
                    finally
                    {
                        // In order to handle multiple clients, this dispose will be done at HandleClientAsync, having it here will prematurely close the client..
                    }
                }
            }
            catch (Exception ex) //catch for _tcpListener.AcceptTcpClientAsync();
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Listener error: {ex.Message}");
            }
            finally
            {
                // Handle when user initiated Server Shutdown before a client Connected 
                // or something happened to server (as in Shutdown Listener only)
                // Proceed to gracefully disconnect and shutdown server.
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Connect Method to set Token=Cancel");
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;

                StopAllListeners(); // Shut down all instances of server
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Stopped all Listeners");
#endif
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Shutdown Completed");
                // Notify user the server shutdown is complete
                TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"Server Shutdown Completed"));
            }
        }
        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server starting networkStream");
#endif
                var networkStream = client.GetStream();
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : complete initialize networkStream");
#endif
                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server waiting for message");
#endif
                    var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    NewRxServerTcpData?.Invoke(this, new TcpServerDataEventArgs(buffer, client)); // Pass message to UI
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
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Received from='{client.Client.RemoteEndPoint}' Message='{receivedMessage}'");
#endif
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally // A Client was connected and Disconnect Requested from Server or  Client Disconnected
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server to end the HandleAsyncclient for {client.Client.RemoteEndPoint}");
#endif
                if (client != null)
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server closing and removing {client.Client.RemoteEndPoint}");
#endif
                    _connectedClients.Remove(client); // Item can only be removed before Client.close
                    client.Close();
                }
            }
        }
        public async Task Disconnect()
        {
            _cancellationTokenSource?.Cancel();
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : User request Server Shutdown");
            TcpServerDebugData?.Invoke(this, new TcpLogDataEventArgs($"User request Server Shutdown"));
            CloseAllClients();
            StopAllListeners(); //Although this is already done upon exit of connect, this will initiate an immediate exit.
            await Task.Delay(1000);
        }
        public void StopAllListeners()
        {
            foreach (var listener in _connectedListeners)
            {
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Attempt to Stop all Listeners");
#endif
                listener.Stop();
            }
            _connectedListeners.Clear(); // Clear the stored list of connected listeners
#if DEBUG
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Clear all Listener Success");
#endif
        }
        public void CloseAllClients()
        {
            foreach (var client in _connectedClients)
            {
                try
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Server Attempt to SocketShutdown Client: {client.Client.RemoteEndPoint}");
#endif
                    client.Client.Shutdown(SocketShutdown.Both);
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client disconnected : {client.Client.RemoteEndPoint}");
#endif
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Error disconnecting client: {ex.Message}");
                }
            }
             _connectedClients.Clear(); // Clear the stored list of connected clients outside of for loop
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Clear _ConnectedClient List");
        }
        public void BootThisClient(TcpClient client)
        {
            client.Client.Shutdown(SocketShutdown.Both);
            _connectedClients.Remove(client);
#if DEBUG
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Removing from List: {client.Client.RemoteEndPoint}");
#endif
        }
        public async Task SendMessageToClientAsync(TcpClient client, string message)
        {
            try
            {
                var stream = client.GetStream();
                var data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
#if DEBUG
                Trace.WriteLine($"TcpIpServer SendMessageToClientAsync: Server Sent to='{client.Client.RemoteEndPoint} : Message='{message}'");
#endif
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"TcpIpServer SendMessageToClientAsync: Error sending to='{client.Client.RemoteEndPoint}' : {ex.Message}");
            }
        }
        #endregion // Local Methods
    }

    public class TcpIpClient
    {
        #region Local Variables
        public static string TraceClass;
        public TcpClient _client;
        private CancellationTokenSource _cancellationTokenSource;
        #endregion

        #region Constructor
        public event EventHandler<TcpClientDataEventArgs> NewRxClientTcpData;
        public event EventHandler<TcpLogDataEventArgs> TcpClientDebugData;
        public bool IsConnected => _client?.Connected ?? false;
        public TcpIpClient()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }
        #endregion

        #region Local Methods
        public async Task Connect(string serverIp, int serverPort)
        {
#if !DEBUG
            if (!IsConnected)  // Release version will Limits number of instances we can connect to server.
            { 
#endif
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(serverIp, serverPort);
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client connected to server at='{_client.Client.RemoteEndPoint}' from '{_client.Client.LocalEndPoint}'");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Client connected to server at='{_client.Client.RemoteEndPoint}'"));

                    _cancellationTokenSource = new CancellationTokenSource();
                    // Start a background task to monitor the connection
                    _ = Task.Run(() => MonitorConnection(_cancellationTokenSource.Token));
                    // Another background task for RxMessage
                    _ = Task.Run(() => ReceiveMessageAsync(_cancellationTokenSource.Token, _client));
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error connecting to server: {ex.Message}");
                    TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Error connecting to server: {ex.Message}"));
                }
                finally
                {

                }
#if !DEBUG
            } // Release version will Limits number of instances we can connect to server.
            else
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} :Already connected to Server at '{serverIp}:{serverPort}', IsConnected Satus='{IsConnected}'");
                TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"Already connected to Server at '{serverIp}:{serverPort}', IsConnected Satus='{IsConnected}'"));
            }
#endif
        }
        public async Task Disconnect()
        {
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : User request Client Disconnect");
            TcpClientDebugData?.Invoke(this, new TcpLogDataEventArgs($"User request Client Disconnect"));
            try
            {
                await Task.Delay(1000);

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
            finally
            {
                _cancellationTokenSource = null;
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
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client Sent to='{_client.Client.RemoteEndPoint}' : Message='{message}'");
#endif
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
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Completed Send Client Message : {message}");
#endif
            }
        }
        public async Task ReceiveMessageAsync(CancellationToken cancellationToken, TcpClient _client)
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
                        NewRxClientTcpData?.Invoke(this, new TcpClientDataEventArgs(buffer, _client));
                        var receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
#if DEBUG
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Client='{_client.Client.LocalEndPoint}' Received from='{_client.Client.RemoteEndPoint}' Message='{receivedMessage}'");
#endif
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
            }
        }
        private async Task MonitorConnection(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(60000, cancellationToken); // Adjust the delay as needed

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
        public IPEndPoint GetClientLocalEndpoint(TcpClient tcpClient)
        {
            try
            {
                if (tcpClient != null && tcpClient.Client.LocalEndPoint is IPEndPoint remoteEndPoint)
                {
                    return remoteEndPoint;
                }
                else
                {
                    return null; // or throw an exception or handle it as per your requirement
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Get Client Local Endpoint error: {ex.Message}");
                return null;    
            }
        }
        public IPEndPoint GetClientRemoteEndpoint(TcpClient tcpClient)
        {
            try
            {
                if (tcpClient != null && tcpClient.Client.RemoteEndPoint is IPEndPoint remoteEndPoint)
                {
                    return remoteEndPoint;
                }
                else
                {
                    return null; // or throw an exception or handle it as per your requirement
                }

            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Get Client Local Endpoint error: {ex.Message}");
                return null;
            }
        }
        #endregion // Local Methods
    }
}
