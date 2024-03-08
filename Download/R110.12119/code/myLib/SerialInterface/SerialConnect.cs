#region Credits
/* 
 * Hex logging utility with automated scripting.</p>
 * Modified by N Diep
 * 
 * Based on SerialPort Terminal by http://coad.net Coad.NET
 * This is written as a demonstation of how to use the SerialPort control that is part of .NET 2.0.</p>
 * Written by http://coad.net/noah Noah Coad
 * 
*/
#endregion


#region Namespace Inclusions
using System;
using System.Collections;
using System.Diagnostics;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using MyUtilities;
#endregion

namespace SerialInterface
{
    #region Public Enumerations
    public enum DataMode { Text, Hex }
    #endregion
    public class SerialConnect
    {
        readonly MyVar _myVar = new MyVar();

        #region Local Variables
        public static string TraceClass;
        #endregion

        #region Constructor
        // The main control for communicating through the RS-232 port
        public SerialPort _comport = new SerialPort();
        public event EventHandler<SerialDataEventArgs> NewRxData;
        public SerialConnect()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
        }
        #endregion

        #region Local Properties
        private DataMode _currentDataMode;
        public DataMode CurrentDataMode
        {
            get
            {
                return _currentDataMode;
            }
            set
            {
                _currentDataMode = value;
            }
        }
        public bool IsOpen { get { return _comport.IsOpen; } }
        public int BytesToRead { get { return _comport.BytesToRead; } }

        #endregion

        #region Local Methods
        public void COMConnect(string baudRate, string dataBits, string parity, string stopBits, string portName)
        {
            // If the port is open, close it.
            if (!_comport.IsOpen)
            {
                // Set the port's settings
                _comport.BaudRate = int.Parse(baudRate);
                _comport.DataBits = int.Parse(dataBits);
                _comport.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);
                _comport.Parity = (Parity)Enum.Parse(typeof(Parity), parity);
                _comport.PortName = portName;

                // Open the port
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Serial Port Open : {portName},{baudRate},bits={dataBits},par={parity},stop={stopBits}");
                _comport.Open();
                _comport.DataReceived += new SerialDataReceivedEventHandler(RxData); // handle rxdata
            }
        }
        public void COMDisconnect()
        {
            // If the port is open, close it.
            if (_comport != null)
            {
                if (_comport.IsOpen)
                {
                    _comport.Close();
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Serial Port Close");
                }
                _comport.DataReceived -= new SerialDataReceivedEventHandler(RxData);
                _comport.Dispose();
            }
        }
        /// <summary> Send the user's data</summary>
        public bool TxData(string strdata)
        {
            if (!_comport.IsOpen)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error: Com Port is closed");
                return false;
            }

            if (strdata == null)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error: Input string is null");
                return false; // or throw an exception, log an error, etc.
            }

            if (CurrentDataMode == DataMode.Text)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : DataMode='{CurrentDataMode}'");
                // Send the user's text straight out the port
                _comport.Write(strdata);

                // Convert string to byte array.
                var byteArray = Encoding.ASCII.GetBytes(strdata);
                // Convert each byte to its hexadecimal representation with space between each byte
                var hexString = BitConverter.ToString(byteArray).Replace("-", " ");

                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : '{strdata}' : {hexString}");
                return true;
            }
            else if ((CurrentDataMode == DataMode.Hex))
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : DataMode : {CurrentDataMode}");
                try
                {
                    // Convert the user's string of hex digits (ex: B4 CA E2) to a byte array
                    var data = _myVar.HexStringToByteArray(strdata);

                    // Send the binary data out the port
                    if ( data == null ) { return false; }

                    _comport.Write(data, 0, data.Length);

                    // Convert each byte to its hexadecimal representation with space between each byte
                    var hexString = BitConverter.ToString(data).Replace("-", " ");
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : {hexString}");
                    return true;
                }
                catch (FormatException)
                {
                    // Inform the user if the hex string was not properly formatted
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Write Hex Format Error");
                    return false;
                }
            }
            else 
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name}  : DataMode Error : {CurrentDataMode}");
                return false;
            }
        }
        public void RxData (object sender, SerialDataReceivedEventArgs e)
        {
            if (_comport.IsOpen)
            { 
                // Determain which mode (string or binary) the user is in
                if (CurrentDataMode == DataMode.Text)
                {
                    // Read all the data waiting in the buffer
                    var data = _comport.ReadExisting();
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : DataMode='{CurrentDataMode}' Data='{data}'");
                }
                else
                {
                    // DataMode.Hex Obtain the number of bytes waiting in the port's buffer
                    var bytes = _comport.BytesToRead;

                    // Create a byte array buffer to hold the incoming data
                    var buffer = new byte[bytes];

                    // Read the data from the port and store it in our buffer
                    _comport.Read(buffer, 0, bytes);
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : DataMode={CurrentDataMode} : {_myVar.ByteArrayToHexString(buffer)}");
                    // Send data to whom ever interested : Cross thread here
                     NewRxData?.Invoke(this, new SerialDataEventArgs(buffer));
                }
            }
        }
        #endregion

        #region Event Handlers
        #endregion
    }
    /// <summary>
    /// EventArgs used to send bytes recieved on serial port
    /// </summary>
    public class SerialDataEventArgs : EventArgs
    {
        public SerialDataEventArgs(byte[] buffer)
        {
            _data = buffer;
        }

        /// <summary>
        /// Byte array containing data from serial port
        /// </summary>
        public byte[] _data;
    }
}
