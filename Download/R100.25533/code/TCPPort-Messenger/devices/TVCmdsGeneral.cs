using System;


    public class TVCmdsGeneral
    {
        public string CmdCode { get; }
        public string CmdAck { get; }

        public TVCmdsGeneral(string cmdcode, string cmdack)
        {
            CmdCode = cmdcode;
            CmdAck = cmdack;
        }
    }

    // Define TV remote commands using nested dictionaries
    private Dictionary<RemoteName, Dictionary<string, TVCmdsGeneral>> tvRemoteCommands = new Dictionary<RemoteName, Dictionary<string, TVCmdsGeneral>>
    {
        {
            RemoteName.sampleTVRem, new Dictionary<string, TVCmdsGeneral>
            {
                { "PowerOn",    new TVCmdsGeneral("00", "FF") },
                { "PowerOff",   new TVCmdsGeneral("00", "FF") },
                { "SrcHDMI1",   new TVCmdsGeneral("00", "FF") },
                { "SrcHDMI2",   new TVCmdsGeneral("00", "FF") },
                { "SrcHDMI3",   new TVCmdsGeneral("00", "FF") },
                { "SrcAnalog",  new TVCmdsGeneral("00", "FF") },
                { "VolUp",      new TVCmdsGeneral("00", "FF") },
                { "VolDown",    new TVCmdsGeneral("00", "FF") },
                { "VolMute",    new TVCmdsGeneral("00", "FF") }

            }
        },
        {
            RemoteName.LGTVRem, new Dictionary<string, TVCmdsGeneral>
            {
                // TX: [Command1][Command2][0x20][ID][0x20][Data][0x0D]
                // ACK: [Command2][0x20][ID][0x20][0x4F 0x4B][Data][0x78]
                // ERR: [Command2][ ][ID][ ][NG][Data][x]
                { "PowerOn",    new TVCmdsGeneral("6B 61 20 30 30 20 30 31 0D", "61 20 30 31 20 4F 4B 30 31 78") }, // k a
                // FE 52 E6 F3 73 E6 F3 73 E3 73 53 D2
                //    Q        s        s     s  S    
                { "PowerOff",   new TVCmdsGeneral("6B 61 20 30 30 20 30 30 0D", "61 20 30 31 20 4F 4B 30 30 78") }, // k a
                { "SrcHDMI1",   new TVCmdsGeneral("78 62 20 30 30 20 39 30 0D", "62 20 30 31 20 4F 4B 39 30 78") }, // x b
                { "SrcHDMI2",   new TVCmdsGeneral("78 62 20 30 30 20 39 31 0D", "62 20 30 31 20 4F 4B 39 31 78") }, // x b
                { "SrcHDMI3",   new TVCmdsGeneral("78 62 20 30 30 20 39 32 0D", "62 20 30 31 20 4F 4B 39 32 78") }, // x b
                { "SrcAnalog",  new TVCmdsGeneral("78 62 20 30 30 20 32 30 0D", "62 20 30 31 20 4F 4B 32 30 78") }, // x b
                { "VolUp",      new TVCmdsGeneral("6B 66 20 30 30 20 36 34 0D", "66 20 30 31 20 4F 4B 36 34 78") }, // k f
                { "VolDown",    new TVCmdsGeneral("6B 66 20 30 30 20 33 32 0D", "66 20 30 31 20 4F 4B 33 32 78") }, // k f
                { "VolMute",    new TVCmdsGeneral("6B 66 20 30 30 20 30 30 0D", "66 20 30 31 20 4F 4B 30 30 78") }, // k f
            }
        },
        {
            RemoteName.SamsungTVRem, new Dictionary<string, TVCmdsGeneral>
            {
                // TX: [0x08][0x20][Command1][Command2][Command3][Data][CRC]
                // ACK: [0x3C][0xF1]
                { "PowerOn",    new TVCmdsGeneral("08 22 00 00 00 02 D4", "3C F1") },
                { "PowerOff",   new TVCmdsGeneral("08 22 00 00 00 01 D5", "3C F1") },
                { "SrcHDMI1",   new TVCmdsGeneral("08 22 0A 00 05 00 C7", "3C F1") },
                { "SrcHDMI2",   new TVCmdsGeneral("08 22 0A 00 05 01 C6", "3C F1") },
                { "SrcHDMI3",   new TVCmdsGeneral("08 22 0A 00 05 02 C5", "3C F1") },
                { "SrcAnalog",  new TVCmdsGeneral("08 22 0A 00 01 00 CB", "3C F1") },
                { "VolUp",      new TVCmdsGeneral("08 22 01 00 01 00 D4", "3C F1") },
                { "VolDown",    new TVCmdsGeneral("08 22 01 00 02 00 D3", "3C F1") },
                { "VolMute",    new TVCmdsGeneral("08 22 02 00 00 00 D4", "3C F1") }
            }
        }
    };

    public string FindAckByCode(string commandToFind)
    {
        foreach (var manufacturerCommands in tvRemoteCommands.Values)
        {
            foreach (var command in manufacturerCommands.Values)
            {
                if (command.CmdCode == commandToFind)
                {
                    return command.CmdAck;

                }
            }
        }
        return "Command not found";
    }

    public string FindCommandStringByAck(string commandToFind)
    {
        foreach (var manufacturerPair in tvRemoteCommands)
        {
            RemoteName manufacturer = manufacturerPair.Key;
            var commands = manufacturerPair.Value;

            foreach (var commandPair in commands)
            {
                string commandName = commandPair.Key;
                RemoteCommand command = commandPair.Value;

                if (command.CmdAck == commandToFind)
                {
                    //return $"{manufacturer}: {commandName}";
                    return $"{commandName}";
                }
            }
        }

        return "Command not found";
    }

    public string FindCommandStringByCode(string commandToFind)
    {
        foreach (var manufacturerPair in tvRemoteCommands)
        {
            RemoteName manufacturer = manufacturerPair.Key;
            var commands = manufacturerPair.Value;

            foreach (var commandPair in commands)
            {
                string commandName = commandPair.Key;
                RemoteCommand command = commandPair.Value;

                if (command.CmdCode == commandToFind)
                {
                    //return $"{manufacturer}: {commandName}";
                    return $"{commandName}";
                }
            }
        }

        return "Command not found";
    }

