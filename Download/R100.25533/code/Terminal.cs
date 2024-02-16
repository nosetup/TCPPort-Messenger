#region Credits
/* 
 * simple UDP messenging app.
 * by N Diep
*/
#endregion
#region Namespace Inclusions
using System;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using TextBox = System.Windows.Forms.TextBox;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Collections.Concurrent;

// Added namespaces
using TcpIpInterface;
using MyUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Reflection;
using static TcpIpInterface.NetworkUtil;

#endregion
namespace LogTerminal
{
    #region Public Enumerations
    public enum LogMsgType { Incoming, Outgoing, MsgFromApp, Normal, Warning, Error };
    #endregion


    public partial class FrmTerminal : Form
    {
        public static string TraceClass;
        private AboutBox _aboutBoxForm; // Declare the form at the class level
        private readonly Color[] _logMsgTypeColor = {
            Color.DarkSlateGray, 
            Color.Black, 
            Color.Blue, 
            Color.Black, 
            Color.Orange, 
            Color.Red }; // Various colors for logging info

        #region Local Variables
        public const string SystemAppName = "TCPIPMessengerApp";
        private int _dgvLoglineCounter = 1;
        private readonly List<string> _messageHistory = new List<string>(); // Save Textbox history to list
        private readonly int _maxHistorySize = 10; // Set the maximum number of records in history
        private int _historyIndex = -1;         // Handles Textbox history

        // Create an instance of TcpServer
        readonly TcpIpServer _tcpIpServer = new TcpIpServer();
        readonly TcpIpClient _tcpIpClient = new TcpIpClient();
        readonly MyVar _myVar = new MyVar();
        readonly MyDgv _myDgv = new MyDgv();
        readonly MyUi _myUi = new MyUi();
        readonly NetworkUtil _networkUtil = new NetworkUtil();

        readonly NetworkAdapterMgr _networkAdapterManager = new NetworkAdapterMgr();


        #endregion
        #region Constructor
        public FrmTerminal()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable

#if DEBUG
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : NOTICE : DEBUG BUILD IS ENABLED");
#endif

            // Build the form
            InitializeComponent();

            // If Release build, hide Debug UI.
            InitializeReleaseUI();

            // Initialize DataGridView
            InitializeDataGridView();

            cmbAdapterName.SelectedIndexChanged += CmbAdapterName_SelectedIndexChanged;

            // Create List of NIC and load to cmbAdapterName
            InitializeCmbAdapterName();

            // Enable/disable controls based on the current state
            // EnableControls();


            _tcpIpServer.NewRxServerTcpData += RxServerTcpMsg;
            _tcpIpServer.TcpServerDebugData += TcpConnectDebugMsg;
            _tcpIpClient.NewRxClientTcpData += RxClientTcpMsg;
            _tcpIpClient.TcpClientDebugData += TcpConnectDebugMsg;
            _networkUtil.DiscoveryTask += UpdateCmbNetworkDevices;

            TbAnalyzeHexData.GotFocus += TbAnalyzeHexData_GotFocus;
            TbAnalyzeHexData.KeyPress += TbAnalyzeHexData_KeyPress;
            TbSrcPort.KeyPress += TbSrcPort_KeyPress;
            TbDestPort.KeyPress += TbDestPort_KeyPress;

            TbClientMessage.KeyDown += TbMessage_KeyDown;
            TbClientMessage.KeyPress += TbMessage_KeyPress;

            TbServerMessage.KeyPress += TbServerMessage_KeyPress;
        }


        #endregion
        #region Local Properties
        #endregion
        #region Local Methods

        /// <summary> Hide all Debug UI</summary>
        private void InitializeReleaseUI()
        {
#if !DEBUG
            BtnStopListener.Visible = false;
            BtnListAllClients.Visible = false;
            BtnTFTPFile.Visible = false;
            BtnClientStatus.Visible = false;
            TsbtnClearConsole.Visible = false;
            BtnServerCloseAllClients.Visible = false;
#endif
        }

        private void InitializeDataGridView()
        {
            // Set up columns
            _dgvLoglineCounter = 1;
            dgvLogWindow.ColumnCount = 6;

            // Set column names
            dgvLogWindow.Columns[0].Name = "#";
            dgvLogWindow.Columns[1].Name = "HH:MM:SS";
            dgvLogWindow.Columns[2].Name = "<->";
            dgvLogWindow.Columns[3].Name = "Source IP";
            dgvLogWindow.Columns[4].Name = "Dest IP";
            dgvLogWindow.Columns[5].Name = "Message";

            // Set up Column Type
            dgvLogWindow.Columns[0].SortMode = DataGridViewColumnSortMode.Automatic;
            dgvLogWindow.Columns[0].ValueType = typeof(int);
            dgvLogWindow.Columns[0].DefaultCellStyle.Format = "N0"; // Specify the numeric format if needed

            // Set column widths and make the second column fixed size
            dgvLogWindow.Columns[0].Width = 50; // Set the width of the first column
            dgvLogWindow.Columns[0].Frozen = true; // Make the second column a fixed size
            dgvLogWindow.Columns[0].Resizable = DataGridViewTriState.False;
            dgvLogWindow.Columns[1].Width = 75; // Set the width of the first column
            dgvLogWindow.Columns[1].Frozen = true; // Make the second column a fixed size
            dgvLogWindow.Columns[1].Resizable = DataGridViewTriState.False;
            dgvLogWindow.Columns[2].Width = 25; // Set the width of the second column
            dgvLogWindow.Columns[2].Frozen = true; // Make the second column a fixed size
            dgvLogWindow.Columns[2].Resizable = DataGridViewTriState.False;
            dgvLogWindow.Columns[3].Width = 120; // Set the width of the second column
            dgvLogWindow.Columns[3].Frozen = true; // Make the second column a fixed size
            dgvLogWindow.Columns[4].Width = 120; // Set the width of the second column
            dgvLogWindow.Columns[4].Frozen = true; // Make the second column a fixed size
            dgvLogWindow.Columns[5].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // Allow the third column to fill the remaining space

            // Add a row with sample values
            LogMsg(LogMsgType.MsgFromApp, "", "", string.Format($"{SystemAppName}: Started at {DateTime.Now} "));
        }

        /// <summary> Network show only active network interfaces</summary>
        private void InitializeCmbAdapterName()
        {
            cmbAdapterName.Items.Clear();
            _networkAdapterManager.UpdateNetworkAdapterList();
            foreach (var nic in _networkAdapterManager.adapterDictionary.Keys)
            {
                // Access the description using the key (adapter name)
                var description = _networkAdapterManager.adapterDictionary[nic];

                // Add the adapter name and description to the ComboBox
                cmbAdapterName.Items.Add($"{nic} / {description}");
            }

            //After list is items, Selects First Index
            if (cmbAdapterName.Items.Count > 0) cmbAdapterName.SelectedIndex = 0;
            //Loads Default / Last Saved Port Name
            // else if (cmbAdapterName.Items.Contains(Settings.Default.PortName)) cmbAdapterName.Text = Settings.Default.PortName;
            else
            {
                MessageBox.Show(this, "There are no network adapters detected on this computer.\nPlease check device manager then restart this app.", "No Network Adapters Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }

        }
        /// <summary> A single update command to display either Quick Scan or Slow scan
        /// Quick Scan will contain IP Address (Key) with Null (Value)
        /// Slow Scan will contain IP Address (Key) with Host Name or Null
        /// Output to Cmb.. Display all Keys
        /// Output to log.. if null, Give a generic response that would work with both null and !null values.
        /// "Ping successful.. Host name information not availalbe"
        /// </summary>
        private void UpdateCmbNetworkDevices(object sender, DeviceDiscoveryEventArgs e)
        {
            
            _myUi.InvokeIfRequired(cmbNetworkDevices, () =>
            {
                // lets clear the list to start fresh
                cmbNetworkDevices.Items.Clear();
            });
#if debug
            Trace.WriteLine("Go over dictionary and add to combo list.");
#endif
            foreach (var ipaddress in _networkUtil.listActiveNetworkDevices.Keys)
            {
                // Access the description using the key (adapter name)
                var hostname = _networkUtil.listActiveNetworkDevices[ipaddress];

                // Add all ping successful to combobox list
                _myUi.InvokeIfRequired(cmbNetworkDevices, () =>
                {
                    cmbNetworkDevices.Items.Add($"{ipaddress}");
                    LogHostInfo(ipaddress, hostname);
                });
            }
            Trace.WriteLine("Dictionary List Complete.");
            //After list is items, Selects First Index

            _myUi.InvokeIfRequired(cmbNetworkDevices, () =>
            {
                if (cmbNetworkDevices.Items.Count > 0) cmbNetworkDevices.SelectedIndex = 0;
            });
        }
        public void LogHostInfo(string ipaddress, string hostname)
        {
            if (hostname != null)
            {
                LogRx(LogMsgType.Incoming, ipaddress, "", $"Host Info='{hostname}'");
                Trace.WriteLine($"Device Address ='{ipaddress}', Host Info='{hostname}'");
            }
            else
            {
                LogRx(LogMsgType.Incoming, ipaddress, "", $"Ping successful.. Host name information not availalbe");
                Trace.WriteLine($"Device Address ='{ipaddress}', Ping successful.. Host name information not availalbe");
            }
        }
        public void AddRowToDataGridView(string logMode, string logSrc, string logDest, string logMessage)
        {
            _myUi.InvokeIfRequired(dgvLogWindow, () =>
            {
                dgvLogWindow.Rows.Add(
                    _dgvLoglineCounter.ToString(),
                    DateTime.Now.ToString("hh:mm:ss tt"),
                    logMode, logSrc, logDest, logMessage
                );

                _dgvLoglineCounter++;

                if (cbTermAutoScroll.Checked)
                {
                    _myDgv.ScrollToLastRecord(dgvLogWindow);
                }
            });
        }
        /// <summary> Log Tx data to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="src"> The string containing the src to be shown. </param>
        /// <param name="dest"> The string containing the dest to be shown. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        public void LogTx(LogMsgType msgtype, string src, string dest, string msg)
        {
            AddRowToDataGridView("Tx", src, dest, msg);
            _myDgv.SetFontColorForLastRow(dgvLogWindow, _logMsgTypeColor[(int)msgtype]);
        }
        /// <summary> Log Rx data to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="src"> The string containing the src to be shown. </param>
        /// <param name="dest"> The string containing the dest to be shown. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        public void LogRx(LogMsgType msgtype, string src, string dest, string msg)
        {
            // Uses InvokeIfRequired to ensure this method is called on the UI thread
            AddRowToDataGridView("Rx", src, dest, msg);

            // Use InvokeIfRequired for SetFontColorForLastRow if it accesses UI controls
            _myUi.InvokeIfRequired(dgvLogWindow, () => _myDgv.SetFontColorForLastRow(dgvLogWindow, _logMsgTypeColor[(int)msgtype]));
        }
        /// <summary> Log system message to the terminal window. </summary>
        /// <param name="msgtype"> The type of message to be written. </param>
        /// <param name="src"> The string containing the src to be shown. </param>
        /// <param name="dest"> The string containing the dest to be shown. </param>
        /// <param name="msg"> The string containing the message to be shown. </param>
        public void LogMsg(LogMsgType msgtype, string src, string dest, string msg)
        {
            AddRowToDataGridView("--", src, dest, msg);
            // Use InvokeIfRequired for SetFontColorForLastRow if it accesses UI controls
            _myUi.InvokeIfRequired(dgvLogWindow, () => _myDgv.SetFontColorForLastRow(dgvLogWindow, _logMsgTypeColor[(int)msgtype]));
        }
        /// <summary> Invoke print the client received message</summary>
        public void TcpConnectDebugMsg(object sender, TcpLogDataEventArgs e)
        {
            LogMsg(LogMsgType.MsgFromApp, $"", $"", $"{SystemAppName} : {e.Data}");
        }
        /// <summary> Invoke print the server received message</summary>
        public void RxServerTcpMsg(object sender, TcpServerDataEventArgs e)
        {
            var receivedMessage = Encoding.ASCII.GetString(e._data);
            foreach (var client in _tcpIpServer.GetConnectedClients())
            {
                var localEndpoint = _tcpIpServer.GetServerLocalEndpoint(client);
                var remoteEndpoint = _tcpIpServer.GetServerRemoteEndpoint(client);

                if (remoteEndpoint != null)
                {
                    LogRx(LogMsgType.Incoming, $"{localEndpoint}", $"{remoteEndpoint}", $"{receivedMessage}");
                }
                else
                {
                    Console.WriteLine("Unable to retrieve client endpoint.");
                }
            }
        }
        /// <summary> Invoke print the client received message</summary>
        public void RxClientTcpMsg(object sender, TcpClientDataEventArgs e)
        {
            var receivedMessage = Encoding.ASCII.GetString(e._data);
            var localEndpoint = _tcpIpClient.GetClientLocalEndpoint();
            var remoteEndpoint = _tcpIpClient.GetClientRemoteEndpoint();
            LogRx(LogMsgType.Incoming, $"{localEndpoint}", $"{remoteEndpoint}", $"{receivedMessage}");

        }
        /// <summary> Open Window About Box</summary>
        private void OpenWindowAboutBox()
        {
            // Check if the form is null or has been closed
            if (_aboutBoxForm == null || _aboutBoxForm.IsDisposed)
            {
                _aboutBoxForm = new AboutBox
                {
                    TopMost = true // Set the form to always be on top
                };
                _aboutBoxForm.Show();
            }
            else
            {
                // If the form is already open, bring it to the front
                _aboutBoxForm.BringToFront();
            }
        }
#endregion // Local Methods
        #region Event Handlers
        #region Event Handlers > Main Form
        private void FrmTerminal_Load(object sender, EventArgs e)
        {

        }
        private void FrmTerminal_Shown(object sender, EventArgs e)
        {
        }
        private async void FrmTerminal_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_tcpIpClient.IsConnected)
            {
                await _tcpIpClient.Disconnect();
            }
 
            await _tcpIpServer.Disconnect();
        }
        #endregion
        #region Events Handlers > Tool Strip Menu Item 
        private void TsmLogsOpen_Click(object sender, EventArgs e)
        {
            _myDgv.OpenTxtFileToDgvDialog(dgvLogWindow);
        }
        private void TsmLogsSaveAs_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = saveFileDialog.FileName;
                    _myDgv.SaveDgvToTxtFile(dgvLogWindow, filePath);
                }
            }
        }
        private void TsmTVControls_Click(object sender, EventArgs e)
        {
        }
        private void TsmScriptOpen_Click(object sender, EventArgs e)
        {
        }
        private void TsmScriptSaveAs_Click(object sender, EventArgs e)
        {
        }
        private void TsmAutoReplyOpen_Click(object sender, EventArgs e)
        {
        }
        private void TsmAutoReplySaveAs_Click(object sender, EventArgs e)
        {
        }
        private void TsmiFileExit_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void TsmiFileLogs_Click(object sender, EventArgs e)
        {

        }
        private void TsmiAbout_Click(object sender, EventArgs e)
        {
            OpenWindowAboutBox();
        }
        #endregion
        #region Event Handlers > Tool Strip Button Click
        private void TsbtnTVControls_Click(object sender, EventArgs e)
        {

        }
        private void TsbtnConnect_Click(object sender, EventArgs e)
        {

        }
        private void TsbtnDisconnect_Click(object sender, EventArgs e)
        {

        }
        private void TsbtnLogOpen_Click(object sender, EventArgs e)
        {
            _myDgv.OpenTxtFileToDgvDialog(dgvLogWindow);
        }
        private void TsbtnLogSaveAs_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = saveFileDialog.FileName;
                    _myDgv.SaveDgvToTxtFile(dgvLogWindow, filePath);
                }
            }
        }
        /// <summary> Add space for Console / Trace Writeline</summary>
        private void TsbtnClearConsole_Click(object sender, EventArgs e)
        {
            Trace.WriteLine($"\n\n");
        }
        #endregion // Event Handler Tool Strip Button Click
        #region Event Handlers > Form Change
        private void CmbAdapterName_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Call the method to get the IPv4 address for the selected adapter
            // We will remove the '/' that was added in GUI.
            var ipv4Address = _networkAdapterManager.GetIPv4AddressForSelectedAdapter(cmbAdapterName.Text.Split('/')[0].Trim());
            TbSrcIP.Text = ipv4Address;
            // Display or use the IPv4 address as needed
            Console.WriteLine($"IPv4 Address for Selected Adapter='{ipv4Address}'");
        }
        private void TbAnalyzeHexData_GotFocus(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() => (sender as TextBox).SelectAll()));
        }
        private void TbAnalyzeHexData_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Handle Enter key press
                e.Handled = true;

                // Trigger the button click event (send the message)
                BtnAnalyzeHexDataSearch_Click(sender, e);
            }
        }
        private void TbServerMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Handle Enter key press
                e.Handled = true;

                // Trigger the button click event (send the message)
                BtnServerSendMessage_Click(sender, e);
            }
        }
        private void TbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            {
                if (e.KeyCode == Keys.Down)
                {
                    // Handle up arrow key press
                    e.Handled = true;

                    // If history is not empty, display the previous message from history
                    if (_messageHistory.Count > 0)
                    {
                        // Display the previous non-empty message from history
                        _historyIndex = (_historyIndex - 1 + _messageHistory.Count) % _messageHistory.Count;
                        TbClientMessage.Text = _messageHistory[_historyIndex];
                        TbClientMessage.SelectAll();
                    }
                }
                else if (e.KeyCode == Keys.Up)
                {
                    // Handle down arrow key press
                    e.Handled = true;

                    // If history is not empty, display the next message from history
                    if (_messageHistory.Count > 0)
                    {
                        // Display the next non-empty message from history
                        _historyIndex = (_historyIndex + 1) % _messageHistory.Count;
                        TbClientMessage.Text = _messageHistory[_historyIndex];
                        TbClientMessage.SelectAll();
                    }
                }
            }
        }
        private void TbMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Handle Enter key press
                e.Handled = true;

                // Save the current non-blank message to history
                var currentMessage = TbClientMessage.Text.Trim();

                if (!string.IsNullOrWhiteSpace(currentMessage))
                {
                    // Check if the message is already in history and remove it
                    _messageHistory.Remove(currentMessage);

                    // Save the current message to history
                    _messageHistory.Insert(0, currentMessage);
#if DEBUG
                    Trace.WriteLine("Added to history: " + currentMessage);
                    Trace.WriteLine("History count: " + _messageHistory.Count);
#endif
                    // Limit the number of records in history
                    if (_messageHistory.Count > _maxHistorySize)
                    {
                        _messageHistory.RemoveAt(_messageHistory.Count - 1);
                    }
                }

                // Trigger the button click event (send the message)
                BtnClientSendMessage_Click(sender, e);

                // Reset history index for new messages
                _historyIndex = -1;
            }
        }

        private void TbSrcPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only digits, backspace, and the delete key
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != 8 && e.KeyChar != 127)
            {
                e.Handled = true;
            }
        }
        private void TbDestPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow only digits, backspace, and the delete key
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != 8 && e.KeyChar != 127)
            {
                e.Handled = true;
            }
        }
        private void TimerScript_Tick(object sender, EventArgs e)
        {
        }
#endregion
        #region Events Handlers > Form Button Click
        private async void BtnServerConnect_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Connect");
#endif
            // Start the server asynchronously
            await _tcpIpServer.Connect(TbSrcIP.Text, _myVar.StringtoInt(TbSrcPort.Text));
            // Wait for the server to start
            await Task.Delay(1000);
#if DEBUG
            Trace.WriteLine($"Status for {_tcpIpClient.IsConnected}");
#endif
        }
        private void BtnRefreshAdapters_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Refresh");
#endif
        }
        private async void BtnServerDisconnect_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Disconnect");
#endif
            await _tcpIpServer.Disconnect(); // To stop the server, call the Stop method
        }
        private async void BtnClientConnect_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Client Connect ");
#endif
            try
            {
                // Connect to the server
                await _tcpIpClient.Connect(TbDestIP.Text, _myVar.StringtoInt(TbDestPort.Text));
            }
            catch (Exception ex)
            {
                LogMsg(LogMsgType.MsgFromApp, $"", $"", $"Error Client cannot connect: {ex.Message}");
            }
            finally
            {
            }
            // Wait for the server to start
            await Task.Delay(1000);
        }
        private async void BtnClientDisconnect_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Client Disconnect");
#endif
            await _tcpIpClient.Disconnect();
        }
        private async void BtnServerSendMessage_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Send Message");
#endif
            try
            {
                var messageToSend = TbServerMessage.Text;
#if DEBUG
                Trace.WriteLine($"Ready to Send Message to all Clients");
#endif
                // Send the message to all connected clients
                foreach (var client in _tcpIpServer.GetConnectedClients())
                {
                    await _tcpIpServer.SendMessageToClientAsync(client, messageToSend);

                    var localEndpoint = _tcpIpServer.GetServerLocalEndpoint(client);
                    var remoteEndpoint = _tcpIpServer.GetServerRemoteEndpoint(client);
                    LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"{messageToSend}");
                }


            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Server fail to send message: {ex.Message}");
            }
        }
        private void BtnListAllClients_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick List All Clients");
#endif
            var count = 0;
            try
            {
                foreach (var client in _tcpIpServer.GetConnectedClients()) // List and Count all Active Clients
                {
#if DEBUG
                    Trace.WriteLine($"ListClients : {count} at {client.Client.RemoteEndPoint}:");
#endif
                    count++;
                }
#if DEBUG
                Trace.WriteLine($"ListClients Total : {count}");
#endif
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error trying to Get List of Connected Clients: {ex.Message}");
            }
        }
        private void BtnStopListener_Click(object sender, EventArgs e)
        {
            _tcpIpServer.ListenerStop(); // Used for debugging
        }
        private void BtnClientStatus_Click(object sender, EventArgs e)
        {
            Trace.WriteLine($"Print IsConnected Status '{_tcpIpClient.IsConnected}'");  // Used for debugging
        }
        private async void BtnQuickScan_Click(object sender, EventArgs e)
        {
            // Get the IP address from the text box
            var ipAddressText = TbSrcIP.Text;
            // Parse the three octets
            if (_myVar.TryParseIpAddress(ipAddressText, out var baseIpAddress))
            {
                // Perform the scan with the parsed base IP address
                await _networkUtil.ScanForNetworkDevicesAsync(baseIpAddress, true);
            }
            else
            {
                // Handle invalid input
                MessageBox.Show($"Invalid IP address format. {TbSrcIP.Text}", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private async void BtnSlowScan_Click(object sender, EventArgs e)
        {
            // Get the IP address from the text box
            var ipAddressText = TbSrcIP.Text;

            // Parse the three octets
            if (_myVar.TryParseIpAddress(ipAddressText, out var baseIpAddress))
            {
                // Perform the scan with the parsed base IP address
                await _networkUtil.ScanForNetworkDevicesAsync(baseIpAddress, false);
            }
        }
        private void BtnHostInfo_Click(object sender, EventArgs e)
        {
            // This is a blocking method
            if (cmbNetworkDevices.Text != "")
            {
                var hostname = _networkUtil.GetHostNameByIPAddress(cmbNetworkDevices.Text);
                LogHostInfo(cmbNetworkDevices.Text, hostname);
            }
            else
            {
                // Display local information if Network Devices is blank
                // Keep it blank to avoid corner conditions.
                var hostname = _networkUtil.GetHostNameByIPAddress(cmbNetworkDevices.Text);
                LogHostInfo(TbSrcIP.Text, hostname);
                cmbNetworkDevices.Items.Clear();
            }

        }
        private async void BtnClientSendMessage_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Client Send Message");
#endif
            try
            {
                var messageToSend = TbClientMessage.Text;
                // Encode the message as bytes
                //var sendBuffer = Encoding.ASCII.GetBytes(messageToSend);

                // Send the message asynchronously
                await _tcpIpClient.SendMessageAsync(messageToSend);

                // Clear the text box after sending the message
                if (cbClearMsgAfterSend.Checked) TbClientMessage.Clear();

                var localEndpoint = _tcpIpClient.GetClientLocalEndpoint();
                var remoteEndpoint = _tcpIpClient.GetClientRemoteEndpoint();
                LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"{messageToSend}");
            }
            catch (Exception ex)
            {
                LogMsg(LogMsgType.MsgFromApp, $"", $"", $"Error sending message: {ex.Message}");
                Trace.WriteLine($"Error sending message: {ex.Message}");
            }
        }
        private void BtnAnalyzeHexDataSearch_Click(object sender, EventArgs e)
        {
            // Search in 2nd Column
            _myDgv.SearchAndShowResults(dgvLogWindow, 5, TbAnalyzeHexData.Text);
        }
        private void BtnAnalyzeHexDataShowAll_Click(object sender, EventArgs e)
        {
            _myDgv.UnhideAllRows(dgvLogWindow);
        }
        private void BtnTermClearAll_Click(object sender, EventArgs e)
        {
            // Clear all rows and columns
            // todo: add pop up to give a warning
            dgvLogWindow.Rows.Clear();
        }
        private void BtnDelSelRow_Click(object sender, EventArgs e)
        {
            // Delete selected rows in dgvLogWindow
            _myDgv.DeleteSelectedRows(dgvLogWindow);
        }
        private void BtnTermReset_Click(object sender, EventArgs e)
        {
            dgvLogWindow.Rows.Clear();
            LogMsg(LogMsgType.MsgFromApp, "", "", $"{SystemAppName}: Resetting Log...");
            InitializeDataGridView();
        }
        #endregion // Events Handler Form Button Click

        #endregion // Event Handlers

        private void BtnServerCloseAllClients_Click(object sender, EventArgs e)
        {
            _tcpIpServer.CloseAllClients(); // Used for debugging
        }
    }
}
