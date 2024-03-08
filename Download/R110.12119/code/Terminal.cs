#region Credits
/* 
 * simple TCPIP messenging app using TCP Listener and TCP Client class.
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
using System.Threading.Tasks;
using TextBox = System.Windows.Forms.TextBox;
using System.Reflection;
using System.Net.Sockets;


// User Added Namespace
using TcpIpInterface;
using MyUtilities;

#endregion
namespace LogTerminal
{
    #region Public Enumerations
    public enum LogMsgType { Incoming, Outgoing, MsgFromApp, Normal, Warning, Error };
    #endregion

    public partial class FrmTerminal : Form
    {

        #region Local Variables
        public static string TraceClass;
        public const string SystemAppName = "TCPIPMessengerApp";
        private int _dgvLoglineCounter = 1;
        private readonly Color[] _logMsgTypeColor = {
            Color.DarkSlateGray,
            Color.Black,
            Color.Blue,
            Color.Black,
            Color.Orange,
            Color.Red }; // Various colors for logging info
        private readonly List<string> _messageHistory = new List<string>(); // Save Textbox history to list
        private readonly int _maxHistorySize = 10; // Set the maximum number of records in history
        private int _historyIndex = -1;         // Handles Textbox history
        #endregion

        #region Constructor
        private AboutBox _aboutBoxForm;
        readonly TcpIpServer _tcpIpServer = new TcpIpServer(); // Create an instance of TcpServer
        readonly TcpIpClient _tcpIpClient = new TcpIpClient();
        readonly MyVar _myVar = new MyVar();
        readonly MyDgv _myDgv = new MyDgv();
        readonly MyUi _myUi = new MyUi();
        readonly NetworkUtil _networkUtil = new NetworkUtil();
        readonly NetworkAdapterMgr _networkAdapterManager = new NetworkAdapterMgr();
        #endregion

        #region Local Properties
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
            _tcpIpServer.NewRxServerTcpData += (sender, e) => RxServerTcpMsg(sender, e, e.Client);
            _tcpIpClient.NewRxClientTcpData += (sender, e) => RxClientTcpMsg(sender, e, e.Client);
            _tcpIpServer.TcpServerDebugData += TcpConnectDebugMsg;
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

        #region Local Methods

        /// <summary>
        /// Hide all Debug UI</summary>
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
            LogMsg(LogMsgType.MsgFromApp, "", "", string.Format($"{SystemAppName}: Started at {DateTime.Now} "));

#if DEBUG
            LogMsg(LogMsgType.MsgFromApp, "", "", string.Format($"***** DEBUG BUILD *****"));
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : ***** DEBUG BUILD *****");
#endif
        }

        /// <summary>
        /// Network show only active network interfaces in Combo box</summary>
        private void InitializeCmbAdapterName()
        {
            cmbAdapterName.Items.Clear();
            _networkAdapterManager.UpdateNetworkAdapterList();
            foreach (var nic in _networkAdapterManager.adapterDictionary.Keys)
            {
                var description = _networkAdapterManager.adapterDictionary[nic];
                cmbAdapterName.Items.Add($"{nic} / {description}");
            }

            //After list is items, Selects First Index
            if (cmbAdapterName.Items.Count > 0) cmbAdapterName.SelectedIndex = 0;
            // TODO: Feature: ability to Load user previously used Adapter/NIC
            //Loads Default / Last Saved Port Name
            // else if (cmbAdapterName.Items.Contains(Settings.Default.PortName)) cmbAdapterName.Text = Settings.Default.PortName;
            else
            {
                MessageBox.Show(this, "There are no network adapters detected on this computer.\nPlease check device manager then restart this app.", "No Network Adapters Installed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            TbDestIP.Text = TbSrcIP.Text; // Pre-Select a Known address for new user to connect to.
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
                // avoid list build up so start fresh.
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
            AddRowToDataGridView("Rx", src, dest, msg);
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
            // Using InvokeIfRequired since this is accessed by TCPIPConnect
            _myUi.InvokeIfRequired(dgvLogWindow, () => _myDgv.SetFontColorForLastRow(dgvLogWindow, _logMsgTypeColor[(int)msgtype]));
        }
        /// <summary>
        /// To format and log information collected from Network Utilities Slow Scan
        /// </summary>
        public void LogHostInfo(string ipaddress, string hostname)
        {
            if (hostname != null)
            {
                LogRx(LogMsgType.Incoming, ipaddress, "", $"Host Info='{hostname}'");
                Trace.WriteLine($"Device Address ='{ipaddress}', Host Info='{hostname}'");
            }
            else
            {
                LogRx(LogMsgType.Incoming, ipaddress, "", $"Ping successful.. Host name not availalbe");
                Trace.WriteLine($"Device Address ='{ipaddress}', Ping successful.. Host name not availalbe");
            }
        }
        public void AddRowToDataGridView(string logMode, string logSrc, string logDest, string logMessage)
        {
            _myUi.InvokeIfRequired(dgvLogWindow, () =>
            {
                // TODO: Bug Fixed: DGV AddRowToDataGridView Under observation Added checks since this method was called during FormClosing and threw error.
                if (!dgvLogWindow.IsDisposed || dgvLogWindow != null && dgvLogWindow.ColumnCount != 0)
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
                }

            });
        }
        /// <summary>
        /// Invoke print the tcp connect debug message</summary>
        public void TcpConnectDebugMsg(object sender, TcpLogDataEventArgs e)
        {
            LogMsg(LogMsgType.MsgFromApp, $"", $"", $"{SystemAppName} : {e.Data}");
        }
        /// <summary>
        /// Invoke print the server received message</summary>
        public void RxServerTcpMsg(object sender, TcpServerDataEventArgs e, TcpClient client)
        {
            var receivedMessage = Encoding.ASCII.GetString(e.Buffer);
            var localEndpoint = _tcpIpServer.GetServerLocalEndpoint(client);
            var remoteEndpoint = _tcpIpServer.GetServerRemoteEndpoint(client);

            if (client != null)
            {
                LogRx(LogMsgType.Incoming, $"{localEndpoint}", $"{remoteEndpoint}", $"{receivedMessage}");
            }
            else
            {
                Trace.WriteLine("Unable to retrieve client endpoint.");
                Trace.WriteLine($"localip='{localEndpoint}' remteip='{remoteEndpoint}' msg='{receivedMessage}'");
                Trace.WriteLine($"TcpClient='{client}'");
            }
        }
        /// <summary>
        /// Invoke print the client received message</summary>
        public void RxClientTcpMsg(object sender, TcpClientDataEventArgs e, TcpClient client)
        {
            var receivedMessage = Encoding.ASCII.GetString(e.Buffer);
            var localEndpoint = _tcpIpClient.GetClientLocalEndpoint(client);
            var remoteEndpoint = _tcpIpClient.GetClientRemoteEndpoint(client);

            if (client != null)
            {
                LogRx(LogMsgType.Incoming, $"{localEndpoint}", $"{remoteEndpoint}", $"{receivedMessage}");
            }
            else
            {
                Trace.WriteLine("Unable to retrieve client endpoint.");
                Trace.WriteLine($"localip='{localEndpoint}' remteip='{remoteEndpoint}' msg='{receivedMessage}'");
                Trace.WriteLine($"TcpClient='{client}'");
            }
        }
        /// <summary>
        /// Open Window About Box</summary>
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
            // TODO: Bug Fixed: DGV Problem FormClosing : Caused by here
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
        /// <summary>
        /// Add space for Console / Trace Writeline</summary>
        private void TsbtnClearConsole_Click(object sender, EventArgs e)
        {
            Trace.WriteLine($"\n\n");
        }
        #endregion // Event Handler Tool Strip Button Click
        #region Event Handlers > Form Change
        private void CmbAdapterName_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Call the method to get the IPv4 address for the selected adapter
            // We will remove the '/' that was added when generating UI.
            var ipv4Address = _networkAdapterManager.GetIPv4AddressForSelectedAdapter(cmbAdapterName.Text.Split('/')[0].Trim());
            TbSrcIP.Text = ipv4Address;
            Trace.WriteLine($"IPv4 Address for Selected Adapter='{ipv4Address}'");
        }
        private void TbAnalyzeHexData_GotFocus(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() => (sender as TextBox).SelectAll()));
        }
        private void TbAnalyzeHexData_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Enter key press
                e.Handled = true;
                BtnAnalyzeHexDataSearch_Click(sender, e);
            }
        }
        private void TbServerMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                // Enter key press
                e.Handled = true;
                BtnServerSendMessage_Click(sender, e);
            }
        }
        private void TbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            {
                if (e.KeyCode == Keys.Down)
                {
                    // Arrow down key press
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
                    // Arrow up key press
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
                // Enter key press
                e.Handled = true;
                BtnClientSendMessage_Click(sender, e);
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
            await _tcpIpServer.Disconnect();
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
                    Trace.WriteLine($"ListClients : {count} at {client.Client.RemoteEndPoint}");
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
            _tcpIpServer.StopAllListeners(); // Quick Jump, Used for debugging
        }
        private void BtnClientStatus_Click(object sender, EventArgs e)
        {
            Trace.WriteLine($"Print IsConnected Status '{_tcpIpClient.IsConnected}'");  // Quick Jump, Used for debugging
        }
        private async void BtnQuickScan_Click(object sender, EventArgs e)
        {
            var ipAddressText = TbSrcIP.Text;
            // Parse the three octets
            if (_myVar.TryParseIpAddress(ipAddressText, out var baseIpAddress))
            {
                await _networkUtil.ScanForNetworkDevicesAsync(baseIpAddress, true);
            }
            else
            {
                // We have Invalid IPAdress
                MessageBox.Show($"Invalid IP address format. {TbSrcIP.Text}", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private async void BtnSlowScan_Click(object sender, EventArgs e)
        {
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
            if (cmbNetworkDevices.Text != "")
            {
                // Display host name of selected IPAddress.
                var hostname = _networkUtil.GetHostNameByIPAddress(cmbNetworkDevices.Text);
                LogHostInfo(cmbNetworkDevices.Text, hostname);
            }
            else
            {
                // Display local host name if no valid address to select from.
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

                // Send the message asynchronously
                await _tcpIpClient.SendMessageAsync(messageToSend);

                var localEndpoint = _tcpIpClient.GetClientLocalEndpoint(_tcpIpClient._client); // For debugging, we will just use the last open client
                var remoteEndpoint = _tcpIpClient.GetClientRemoteEndpoint(_tcpIpClient._client);
                if (localEndpoint != null && remoteEndpoint != null)
                {
                    LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"{messageToSend}");
                }
                else
                {
                    LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"Null Local or Remote Endpoint Error, Not connected to server.");
                }
            }
            catch (Exception ex)
            {
                LogMsg(LogMsgType.MsgFromApp, $"", $"", $"Error sending message: {ex.Message}");
                Trace.WriteLine($"Error sending message: {ex.Message}");
            }

            // Save the current non-blank message to history
            var currentMessage = TbClientMessage.Text.Trim();

            if (!string.IsNullOrWhiteSpace(currentMessage))
            {
                // Checks for duplicates and remove it
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
            // TODO: Improvement: we may not want to clear textbox if fail to send.
            // Clear the text box or take other actions as needed
            if (cbClearMsgAfterSend.Checked) TbClientMessage.Clear();

            // Reset history index for new messages
            _historyIndex = -1;
        }
        private void BtnAnalyzeHexDataSearch_Click(object sender, EventArgs e)
        {
            _myDgv.SearchAndShowResults(dgvLogWindow, 5, TbAnalyzeHexData.Text);
        }
        private void BtnAnalyzeHexDataShowAll_Click(object sender, EventArgs e)
        {
            _myDgv.UnhideAllRows(dgvLogWindow);
        }
        private void BtnTermClearAll_Click(object sender, EventArgs e)
        {
            // TODO: Improvement: add pop up to give a warning
            // Clear all rows and columns
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
        private void BtnServerCloseAllClients_Click(object sender, EventArgs e)
        {
            _tcpIpServer.CloseAllClients(); // Used for debugging
        }
        private void BtnServerRefreshClient_Click(object sender, EventArgs e)
        {
            cmbServerConnectedClients.Items.Clear();

            foreach (var client in _tcpIpServer.GetConnectedClients())
            {
                cmbServerConnectedClients.Items.Add(client.Client.RemoteEndPoint);
            }
            if (cmbServerConnectedClients.Items.Count > 0) cmbServerConnectedClients.SelectedIndex = 0;
        }
        private void BtnServerBootClient_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Boot a Client");
#endif
            // Convert the selected value from the combo box to an IPEndPoint object
            var selectedRemoteEndpointText = cmbServerConnectedClients.Text;

            foreach (var client in _tcpIpServer.GetConnectedClients())
            {
                var remoteEndpoint = client.Client.RemoteEndPoint.ToString();
                var localEndpoint = client.Client.LocalEndPoint.ToString();

                // Check if the remote endpoint of the current client matches the selected remote endpoint
                if (remoteEndpoint == selectedRemoteEndpointText)
                {
                    // Send the message to the matching client
                    _tcpIpServer.BootThisClient(client);
                    LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"Boot the Client");
                    break;
                }
            }

        }
        private async void BtnServerPrivateMessage_Click(object sender, EventArgs e)
        {
#if DEBUG
            Trace.WriteLine($"ButtonClick Server Private Message");
#endif
            try
            {
                var messageToSend = TbServerMessage.Text;
#if DEBUG
                Trace.WriteLine($"Ready to Send Private Message to Selected Clients");
#endif
                var selectedRemoteEndpointText = cmbServerConnectedClients.Text;

                // Need to match the selected Client IPAddress to list of Active Clients
                foreach (var client in _tcpIpServer.GetConnectedClients())
                {
                    var remoteEndpoint = client.Client.RemoteEndPoint.ToString(); // Convert the remote endpoint to string for comparison
                    var localEndpoint = client.Client.LocalEndPoint.ToString();

                    if (remoteEndpoint == selectedRemoteEndpointText)
                    {
                        await _tcpIpServer.SendMessageToClientAsync(client, messageToSend);
                        LogTx(LogMsgType.Outgoing, $"{localEndpoint}", $"{remoteEndpoint}", $"{messageToSend}");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error Server fail to send private message: {ex.Message}");
            }
        }
        #endregion // Events Handler Form Button Click

        #endregion // Event Handlers


    }
}
