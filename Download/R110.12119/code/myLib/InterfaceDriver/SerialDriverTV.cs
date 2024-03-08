using System.Collections.Generic;



namespace InterfaceDriver
{
    public class SerialDriverTV
    {
        #region Public Enumerations

        #endregion

        #region Constructor
        public string CmdCode { get; }
        public string CmdAck { get; }

        // Static constructor to initialize the nested dictionary
        static SerialDriverTV()
        {
            InitializeRemoteCommands();
        }

        public SerialDriverTV(string cmdcode, string cmdack)
        {
            CmdCode = cmdcode;
            CmdAck = cmdack;
        }

        // Store commands using a static nested dictionary
        public static Dictionary<string, Dictionary<string, SerialDriverTV>> TvRemoteCommands =
        new Dictionary<string, Dictionary<string, SerialDriverTV>>();
        #endregion

        #region Local Methods

        /// <summary>
        /// Summary: Define Remote Commands, Mfg and Code
        /// </summary>
        public static void InitializeRemoteCommands()
        {
            TvRemoteCommands.Add("Mfg_Sample", new Dictionary<string, SerialDriverTV>
            {
                { "PowerOn",    new SerialDriverTV("00", "FF") },
                { "PowerOff",   new SerialDriverTV("00", "FF") },
                { "SrcHDMI1",   new SerialDriverTV("00", "FF") },
                { "SrcHDMI2",   new SerialDriverTV("00", "FF") },
                { "SrcHDMI3",   new SerialDriverTV("00", "FF") },
                { "SrcAnalog",  new SerialDriverTV("00", "FF") },
                { "VolUp",      new SerialDriverTV("00", "FF") },
                { "VolDown",    new SerialDriverTV("00", "FF") },
                { "VolMute",    new SerialDriverTV("00", "FF") }
            });
            TvRemoteCommands.Add("Mfg_LG", new Dictionary<string, SerialDriverTV>
            {
                // TX: [Command1][Command2][0x20][ID][0x20][Data][0x0D]
                // ACK: [Command2][0x20][ID][0x20][0x4F 0x4B][Data][0x78]
                // ERR: [Command2][ ][ID][ ][NG][Data][x]
                { "PowerOn",    new SerialDriverTV("6B 61 20 30 30 20 30 31 0D", "61 20 30 31 20 4F 4B 30 31 78") }, // k a
                // FE 52 E6 F3 73 E6 F3 73 E3 73 53 D2
                //    Q        s        s     s  S    
                { "PowerOff",   new SerialDriverTV("6B 61 20 30 30 20 30 30 0D", "61 20 30 31 20 4F 4B 30 30 78") }, // k a
                { "SrcHDMI1",   new SerialDriverTV("78 62 20 30 30 20 39 30 0D", "62 20 30 31 20 4F 4B 39 30 78") }, // x b
                { "SrcHDMI2",   new SerialDriverTV("78 62 20 30 30 20 39 31 0D", "62 20 30 31 20 4F 4B 39 31 78") }, // x b
                { "SrcHDMI3",   new SerialDriverTV("78 62 20 30 30 20 39 32 0D", "62 20 30 31 20 4F 4B 39 32 78") }, // x b
                { "SrcAnalog",  new SerialDriverTV("78 62 20 30 30 20 32 30 0D", "62 20 30 31 20 4F 4B 32 30 78") }, // x b
                { "VolUp",      new SerialDriverTV("6B 66 20 30 30 20 36 34 0D", "66 20 30 31 20 4F 4B 36 34 78") }, // k f
                { "VolDown",    new SerialDriverTV("6B 66 20 30 30 20 33 32 0D", "66 20 30 31 20 4F 4B 33 32 78") }, // k f
                { "VolMute",    new SerialDriverTV("6B 66 20 30 30 20 30 30 0D", "66 20 30 31 20 4O 4B 30 30 78") }  // k f
            });
            TvRemoteCommands.Add("Mfg_Samsung", new Dictionary<string, SerialDriverTV>
            {
                // TX: [0x08][0x20][Command1][Command2][Command3][Data][CRC]
                // ACK: [0x3C][0xF1]
                { "PowerOn",    new SerialDriverTV("08 22 00 00 00 02 D4", "3C F1") },
                { "PowerOff",   new SerialDriverTV("08 22 00 00 00 01 D5", "3C F1") },
                { "SrcHDMI1",   new SerialDriverTV("08 22 0A 00 05 00 C7", "3C F1") },
                { "SrcHDMI2",   new SerialDriverTV("08 22 0A 00 05 01 C6", "3C F1") },
                { "SrcHDMI3",   new SerialDriverTV("08 22 0A 00 05 02 C5", "3C F1") },
                { "SrcAnalog",  new SerialDriverTV("08 22 0A 00 01 00 CB", "3C F1") },
                { "VolUp",      new SerialDriverTV("08 22 01 00 01 00 D4", "3C F1") },
                { "VolDown",    new SerialDriverTV("08 22 01 00 02 00 D3", "3C F1") },
                { "VolMute",    new SerialDriverTV("08 22 02 00 00 00 D4", "3C F1") }
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
    #endregion // Local Methods
}
