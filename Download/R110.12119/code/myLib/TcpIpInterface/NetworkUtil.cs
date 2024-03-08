using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;



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
        #region Local Variables
        public static string TraceClass;
        public Dictionary<string, string> listActiveNetworkDevices;
        #endregion

        #region Constructor
        public event EventHandler<DeviceDiscoveryEventArgs> DiscoveryTask ;
        public NetworkUtil()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            listActiveNetworkDevices = new Dictionary<string, string>();
        }
        #endregion

        #region Local Methods
        /// <summary>
        /// Network ping a range of network devices then store online devices in list</summary>
        public async Task ScanForNetworkDevicesAsync(string baseIpAddress, bool fastscan, int timeout = 1000, int startRange = 1, int endRange = 254) //254
        {
#if DEBUG
            Trace.WriteLine("Scan For Network Devices");
#endif
            listActiveNetworkDevices.Clear();
            var tasks = new Task[endRange - startRange + 1];

                for (var i = startRange; i <= endRange; i++)
                {
                    var ipAddress = $"{baseIpAddress}.{i}";
                    tasks[i - startRange] = ScanSingleAsync(ipAddress, timeout, fastscan);
                }
            await Task.WhenAll(tasks);
#if DEBUG
            Trace.WriteLine("Scan Completed");
#endif
            // Sort dictionary, so easier to read log window for the online device will be in bottom
            var itemsWithValues = listActiveNetworkDevices.Where(kv => kv.Value != null)
                                                          .OrderBy(kv => kv.Value)
                                                          .ToList();
            var itemsWithNull = listActiveNetworkDevices.Where(kv => kv.Value == null)
                                                        .ToList();
            var sortedDictionary = itemsWithNull.Concat(itemsWithValues)
                                                 .ToDictionary(kv => kv.Key, kv => kv.Value);
            listActiveNetworkDevices = sortedDictionary;

            DiscoveryTask?.Invoke(this, new DeviceDiscoveryEventArgs(true)); // Update UI
        }
        /// <summary>
        /// Network ping a single device</summary>
        public async Task ScanSingleAsync(string ipAddress, int timeout, bool fastscan)
        {
            var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, timeout);

            if (reply.Status == IPStatus.Success)
            {
                if (fastscan)
                {
#if DEBUG
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Device found: {ipAddress}");
#endif
                    AddActiveNetworkDevices(ipAddress, null);
                }
                else
                {
                    // TODO: Optimize Done: No longer uses GetHostNameByIPAddress()
                    try
                    {
                        var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                        AddActiveNetworkDevices(ipAddress, hostEntry.HostName);
                    }
                    catch (SocketException ex)
                    {
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but Unable to resolve host name: {ex.Message}");
                        AddActiveNetworkDevices(ipAddress, null); // This is so both Quick Scan and Slow Scan will produce the same list.
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but An unexpected error occurred: {ex.Message}");
                    }
                }
            }
            else
            {
                // If we want to do something for ipAddress that fail to ping.
                // These should be availalble ipAddress incase want to use them (i.e assignment)
            }
        }
        /// <summary>
        /// Store Active Network Devices Items to Dictionary from successfully ping'ed devices</summary>
        public void AddActiveNetworkDevices(string ipaddress, string hostname)
        {
            // Add to dictionary
            listActiveNetworkDevices[ipaddress] = hostname;
        }
        /// <summary>
        /// Clear all Active Network Devices Items from Dictionary</summary>
        public void ClearActiveNetworkDevices()
        {
            listActiveNetworkDevices.Clear();
        }
        /// <summary>
        ///  Dictionary sorter that treats null values as greater than non-null values</summary>
        public static int CompareValuesWithNullLast<TValue>(TValue x, TValue y) where TValue : IComparable<TValue>
        {
            if (x == null && y == null)
                return 0;
            if (x == null)
                return 1; // x (null) should come after y (non-null)
            if (y == null)
                return -1; // x (non-null) should come before y (null)
            return x.CompareTo(y); // Compare non-null values normally
        }

        /// <summary>
        /// Get Host name with string IPAddress of device on network
        /// This was previously used by ScanSingleAsync which now uses async for speed.</summary>
        public string GetHostNameByIPAddress(string ipAddress)
        {
            try
            {
                var entry = Dns.GetHostEntry(ipAddress);
#if DEBUG
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Host Address='{ipAddress}' Name='{entry.HostName}'");
#endif
                return entry.HostName;
            }
            catch (SocketException ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but Unable to resolve host name: {ex.Message}");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : Error '{ipAddress}' : Ping Success but An unexpected error occurred: {ex.Message}");
            }
            return null;
        }
#endregion
    }
}
