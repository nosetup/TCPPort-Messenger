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


namespace TcpIpInterface
{
    public class NetworkAdapter
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class NetworkAdapterMgr
    {
        public static string TraceClass;
        public Dictionary<string, string> adapterDictionary;
        public NetworkAdapterMgr()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
            adapterDictionary = new Dictionary<string, string>();
        }

        /// <summary> Initialize a list to store active network interfaces</summary>
        public void UpdateNetworkAdapterList()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            adapterDictionary.Clear();

            foreach (var networkInterface in networkInterfaces)
            {
                // Filter based on type and status
                if ((networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                    networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    string name = networkInterface.Name;
                    string description = networkInterface.Description;

                    // Add to dictionary
                    adapterDictionary[name] = description;

#if DEBUG
                    Trace.WriteLine($"  Adapter Name: {networkInterface.Name}");
                    Trace.WriteLine($"  Enabled: {networkInterface.OperationalStatus == OperationalStatus.Up}");
                    Trace.WriteLine($"  Interface: {networkInterface.Description}");
                    Trace.WriteLine($"    ID: {networkInterface.Id}");
                    Trace.WriteLine($"    Type: {networkInterface.NetworkInterfaceType}");
                    Trace.WriteLine($"    Status: {networkInterface.OperationalStatus}");
                    Trace.WriteLine($"    Speed: {networkInterface.Speed} bps");

                    // Display IPv4 addresses
                    var ipv4Addresses = networkInterface.GetIPProperties().UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(addr => addr.Address);
                    Trace.WriteLine($"    IPv4 Addresses: {string.Join(", ", ipv4Addresses)}");
#endif
                }
            }
        }
        /// <summary> Clear list of active network interfaces</summary>
        public void ClearNetworkAdapterList()
        {
            adapterDictionary.Clear();
        }
        /// <summary> Display all list of active network interfaces in Trace</summary>
        public void DisplayAdapters()
        {
            foreach (var kvp in adapterDictionary)
            {
                Trace.WriteLine($"Adapter Name: {kvp.Key}");
                Trace.WriteLine($"Description: {kvp.Value}");
            }
        }
        public string GetIPv4AddressForSelectedAdapter(string SelectedAdapter)
        {
            if (SelectedAdapter!=null)
            {
                // Find the network interface by name
                var selectedAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(adapter => adapter.Name == SelectedAdapter);

                // Retrieve the IPv4 address for the selected adapter
                if (selectedAdapter != null)
                {
                    var ipv4Address = selectedAdapter.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
                    // Return valid ipv4address otherwise return null
                    return ipv4Address?.ToString() ?? null;
                }
            }
            Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : No adapter selected");
            return null;
        }
        // Other methods to manipulate adapters in the dictionary
    }



}
