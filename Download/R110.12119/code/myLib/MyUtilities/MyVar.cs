using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;


namespace MyUtilities
{
    class MyVar
    {
        #region Local Variables
        public static string TraceClass;
        #endregion

        #region Constructor
        public MyVar()
        {
            TraceClass = GetType().Name; // Assign the class name to the static variable
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
        /// <summary> Convert a string (ex: "123") to int. </summary>
        /// <param name="s"> The string containing the digits </param>
        /// <returns> Returns an int value. </returns>
        public int StringtoIntOrDefaultTo(string s, string defaultval)
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
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : fail to parse: var='{s}' return='{defaultval}'");
                return StringtoInt(defaultval);
            }
        }
        /// <summary> Convert a string of hex digits (ex: E4 CA B2) to a byte array. </summary>
        /// <param name="s"> The string containing the hex digits (with or without spaces). </param>
        /// <returns> Returns an array of bytes. </returns>
        public byte[] HexStringToByteArray(string s)
        {
            // Trim trailing spaces from the input string
            s = s.TrimEnd();
            // Remove spaces from the modified string
            s = s.Replace(" ", "");
            // If the input string is empty or has an odd length, return null
            if (s.Length == 0 || s.Length % 2 != 0)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : 'input string is empty or has an odd length' : var='{s}'");
                return null;
            }
            var buffer = new byte[s.Length / 2];
            for (var i = 0; i < s.Length; i += 2)
            {
                var hexByte = s.Substring(i, 2);
                if (!byte.TryParse(hexByte, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var result))
                {
                    // Unable to parse the substring as a byte
                    Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : 'Unable to parse the substring as a byte' : var='{s}'");
                    return null;
                }
                buffer[i / 2] = result;
            }
            return buffer;
        }
        /// <summary> Converts an array of bytes into a formatted string of hex digits (ex: E4 CA B2)</summary>
        /// <param name="data"> The array of bytes to be translated into a string of hex digits. </param>
        /// <returns> Returns a well formatted string of hex digits with spacing. </returns>
        public string ByteArrayToHexString(byte[] data)
        {
            if (data == null)
            {
                Trace.WriteLine($"{TraceClass} : {MethodBase.GetCurrentMethod().Name} : ' Input byte array cannot be null'");
                return null;
            }

            var sb = new StringBuilder(data.Length * 3);
            foreach (var b in data)
            {
                sb.Append(b.ToString("X2").PadRight(3)); // Pad each byte with spaces to ensure each occupies 3 characters
            }
            return sb.ToString().ToUpper().TrimEnd(); // Convert to uppercase and trim trailing spaces
        }
        /// <summary> 
        /// Network determine if a string ipaddress is valid</summary>
        public bool TryParseIpAddress(string input, out string baseIpAddress)
        {
            // Split the input into octets
            var octets = input.Split('.');

            // Ensure there are exactly four octets
            if (octets.Length == 4)
            {
                // Attempt to parse each octet
                if (int.TryParse(octets[0], out var octet1) &&
                    int.TryParse(octets[1], out var octet2) &&
                    int.TryParse(octets[2], out var octet3))
                {
                    // Check octet ranges
                    if (octet1 >= 0 && octet1 <= 255 &&
                        octet2 >= 0 && octet2 <= 255 &&
                        octet3 >= 0 && octet3 <= 255)
                    {
                        // Combine the first three octets
                        baseIpAddress = $"{octet1}.{octet2}.{octet3}";
                        return true;
                    }
                }
            }

            // Default if parsing fails
            baseIpAddress = null;
            return false;
        }
        #endregion //Local Methods
    }
}
