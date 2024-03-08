using System;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace InterfaceDriver
{ 
    public class SerialDriverBK1685
    {
        #region Public Enumerations

        #endregion

        #region Constructor
        public string CmdCode { get; }
        public string CmdAck { get; }

        public SerialDriverBK1685(string cmdcode, string cmdack)
        {
            CmdCode = cmdcode;
            CmdAck = cmdack;
        }
        #endregion
        // Static constructor to initialize the nested dictionary
        static SerialDriverBK1685()
        {
            InitializeRemoteCommands();
        }

        // Define TV remote commands using a static nested dictionary
        public static Dictionary<string, Dictionary<string, SerialDriverBK1685>> TvRemoteCommands =
        new Dictionary<string, Dictionary<string, SerialDriverBK1685>>();

        public static void InitializeRemoteCommands()
        {
            TvRemoteCommands.Add("Mfg_Sample", new Dictionary<string, SerialDriverBK1685>
            {
                { "SET VOLTAGE",    new SerialDriverBK1685("VOLT", "OK/r") },
                { "SET CURRENT",    new SerialDriverBK1685("CURR", "OK/r") },
                { "PRESET MEMORY",  new SerialDriverBK1685("PROM", "OK/r") },
                { "GET STATUS",     new SerialDriverBK1685("GET" , "OK/r") },
                { "DISPLAY STATUS", new SerialDriverBK1685("GETD", "OK/r") },
                { "GET MEMORY",     new SerialDriverBK1685("GETD", "OK/r") },
                { "SET PRESET",     new SerialDriverBK1685("RUNM", "OK/r") },
                { "OUTPUT ENABLE",  new SerialDriverBK1685("SOUT", "OK/r") },
                { "SET OVP",        new SerialDriverBK1685("SOVP", "OK/r") },
                { "SET OCP",        new SerialDriverBK1685("SOCP", "OK/r") },
                { "GET OVP",        new SerialDriverBK1685("GOVP", "OK/r") },
                { "GET OCP",        new SerialDriverBK1685("GOCP", "OK/r") },
                { "GET MAX VALUES", new SerialDriverBK1685("GMAX", "OK/r") }
            });
            TvRemoteCommands.Add("Mfg_BK1685B", new Dictionary<string, SerialDriverBK1685>
            {
                { "SET VOLTAGE",    new SerialDriverBK1685("VOLT", "OK/r") },
                { "SET CURRENT",    new SerialDriverBK1685("CURR", "OK/r") },
                { "PRESET MEMORY",  new SerialDriverBK1685("PROM", "OK/r") },
                { "GET STATUS",     new SerialDriverBK1685("GET" , "OK/r") },
                { "DISPLAY STATUS", new SerialDriverBK1685("GETD", "OK/r") },
                { "GET MEMORY",     new SerialDriverBK1685("GETD", "OK/r") },
                { "SET PRESET",     new SerialDriverBK1685("RUNM", "OK/r") },
                { "OUTPUT ENABLE",  new SerialDriverBK1685("SOUT", "OK/r") },
                { "SET OVP",        new SerialDriverBK1685("SOVP", "OK/r") },
                { "SET OCP",        new SerialDriverBK1685("SOCP", "OK/r") },
                { "GET OVP",        new SerialDriverBK1685("GOVP", "OK/r") },
                { "GET OCP",        new SerialDriverBK1685("GOCP", "OK/r") },
                { "GET MAX VALUES", new SerialDriverBK1685("GMAX", "OK/r") }
            });
            TvRemoteCommands.Add("Mfg_BK1687B", new Dictionary<string, SerialDriverBK1685>
            {
                { "SET VOLTAGE",    new SerialDriverBK1685("VOLT", "OK/r") },
                { "SET CURRENT",    new SerialDriverBK1685("CURR", "OK/r") },
                { "PRESET MEMORY",  new SerialDriverBK1685("PROM", "OK/r") },
                { "GET STATUS",     new SerialDriverBK1685("GET" , "OK/r") },
                { "DISPLAY STATUS", new SerialDriverBK1685("GETD", "OK/r") },
                { "GET MEMORY",     new SerialDriverBK1685("GETD", "OK/r") },
                { "SET PRESET",     new SerialDriverBK1685("RUNM", "OK/r") },
                { "OUTPUT ENABLE",  new SerialDriverBK1685("SOUT", "OK/r") },
                { "SET OVP",        new SerialDriverBK1685("SOVP", "OK/r") },
                { "SET OCP",        new SerialDriverBK1685("SOCP", "OK/r") },
                { "GET OVP",        new SerialDriverBK1685("GOVP", "OK/r") },
                { "GET OCP",        new SerialDriverBK1685("GOCP", "OK/r") },
                { "GET MAX VALUES", new SerialDriverBK1685("GMAX", "OK/r") }
            });
        }

         /// <summary>
        /// Summary: Used primary for decoding messages
        /// Input: Code
        /// Output: Command Name
        /// </summary>
        public string GetCmdNameByCode(string codeToFind)
        {
            foreach (var manufacturerCommands in TvRemoteCommands.Values)
            {
                foreach (var commandPair in manufacturerCommands)
                {
                    var command = commandPair.Value;

                    if (command.CmdCode == codeToFind)
                    {
                        return commandPair.Key; // This will return the command name
                    }
                }
            }
            return "Command Name not found";
        }

        public string GetCmdMfgByCode(string codeToFind)
        {
            foreach (var manufacturerCommands in TvRemoteCommands)
            {
                foreach (var commandPair in manufacturerCommands.Value)
                {
                    var command = commandPair.Value;

                    if (command.CmdCode == codeToFind)
                    {
                        return manufacturerCommands.Key.ToString(); // This will return the manufacturer
                    }
                }
            }
            return "Mfg not found";
        }

        /// <summary>
        /// Summary: Used primary for decoding messages
        /// Input: Code
        /// Output: Command Ack
        /// </summary>
        public string GetCmdAckByCode(string codeToFind)
        {
            foreach (var commands in TvRemoteCommands.Values)
            {
                foreach (var command in commands.Values)
                {
                    if (command.CmdCode == codeToFind)
                    {
                        return command.CmdAck;
                    }
                }
            }
            return "";
        }

        /// <summary>
        /// Summary: Used primary for decoding messages
        /// No immediate use for this for there could be duplicate ACK
        /// </summary>
        public string GetCmdNameByAck(string ackToFind)
        {
            foreach (var manufacturerCommands in TvRemoteCommands.Values)
            {
                foreach (var commandPair in manufacturerCommands)
                {
                    var command = commandPair.Value;

                    if (command.CmdAck == ackToFind)
                    {
                        return commandPair.Key; // This will return the command name
                    }
                }
            }
            return "Command Name not found";
        }

        /// <summary>
        /// Summary: Used primary for Sending Data
        /// Input: MFG and Command Name
        /// Output: Command Code
        /// </summary>
        public string GetCmdCodeByName(string mfg, string nameToFind)
        {
            if (TvRemoteCommands.TryGetValue(mfg, out var manufacturerCommands))
            {
                if (manufacturerCommands.TryGetValue(nameToFind, out var command))
                {
                    return command.CmdCode;
                }
            }

            return "Command Code not found";
        }

        /// <summary>
        /// /// Summary: Used primary for Sending Data
        /// Input: MFG and Command Name
        /// Output: Ack Code
        /// </summary>
        public string GetCmdAckByName(string mfg, string nameToFind)
        {
            if (TvRemoteCommands.TryGetValue(mfg, out var manufacturerCommands))
            {
                if (manufacturerCommands.TryGetValue(nameToFind, out var command))
                {
                    return command.CmdAck;
                }
            }

            return "";
        }
    }

}
