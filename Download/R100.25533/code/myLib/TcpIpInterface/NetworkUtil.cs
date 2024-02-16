using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace TcpIpInterface
{
    public class DeviceDiscoveryEventArgs : EventArgs
    {
        public DeviceDiscoveryEventArgs(bool taskcompete)
        {
            Data = taskcompete;
        }

        public bool Data { get; }
    }

    public class NetworkUtil
    {
        public static string TraceClass;
        public Dictionary<string, string> listActiveNetworkDevices;
        public event EventHandler<DeviceDiscoveryEventArgs> DiscoveryTask ;

        public NetworkUtil()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            listActiveNetworkDevices = new Dictionary<string, string>();
        }
        #region Local Methods
        public void AddActiveNetworkDevices(string ipaddress, string hostname)
        {
            // Add to dictionary
            listActiveNetworkDevices[ipaddress] = hostname;
        }
        public void ClearActiveNetworkDevices()
        {
            listActiveNetworkDevices.Clear();
        }
        /// <summary> Network ping a range of network devices then store online devices in list</summary>
        public async Task ScanForNetworkDevicesAsync(string baseIpAddress, bool fastscan, int timeout = 1000, int startRange = 1, int endRange = 254) //254
        {
            Trace.WriteLine("Scan For Network Devices");
            listActiveNetworkDevices.Clear();
            var tasks = new Task[endRange - startRange + 1];
            for (var i = startRange; i <= endRange; i++)
            {
                var ipAddress = $"{baseIpAddress}.{i}";
                tasks[i - startRange] = ScanSingleAsync(ipAddress, timeout, fastscan);
            }
            await Task.WhenAll(tasks);
            Trace.WriteLine("Scan Completed");
            DiscoveryTask?.Invoke(this, new DeviceDiscoveryEventArgs(true));
        }
        /// <summary> Network ping a single device</summary>
        public async Task ScanSingleAsync(string ipAddress, int timeout, bool fastscan)
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, timeout);

            if (reply.Status == IPStatus.Success)
            {
                if (fastscan)
                {
#if Debug
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Device found: {ipAddress}");
#endif
                    AddActiveNetworkDevices(ipAddress, null);
                }
                else
                {
                    AddActiveNetworkDevices(ipAddress, GetHostNameByIPAddress(ipAddress));
                }
            }
            else
            {
                // If we want to do something for addresses that fail to ping.
                // These should be availalble addresses incase want to use
                // Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : No Device found: {ipAddress}");
            }
        }
        public string GetHostNameByIPAddress(string ipAddress)
        {
            try
            {
                var entry = Dns.GetHostEntry(ipAddress);
                return entry.HostName;
#if Debug
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Host Info: {deviceName}");
#endif
            }
            catch (SocketException ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but Unable to resolve host name: {ex.Message}");
                // Handle the exception gracefully, for example:
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but An unexpected error occurred: {ex.Message}");
                // Handle other exceptions if needed.
            }
            return null;
        }
#endregion
    }
}
