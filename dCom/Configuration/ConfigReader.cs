using Common;
using dCom.Exceptions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dCom.Configuration
{
    internal class ConfigReader : IConfiguration
    {
        private ushort transactionId = 0;

        private byte unitAddress;
        private int tcpPort;
        private ConfigItemEqualityComparer confItemEqComp = new ConfigItemEqualityComparer();

        private Dictionary<string, IConfigItem> pointTypeToConfiguration = new Dictionary<string, IConfigItem>();

        private string path = "RtuCfg.txt";

        public ConfigReader()
        {
            if (!File.Exists(path))
            {
                OpenConfigFile();
            }

            ReadConfiguration();
        }

        public int GetAcquisitionInterval(string pointDescription)
        {
            IConfigItem ci;
            if (pointTypeToConfiguration.TryGetValue(pointDescription, out ci))
            {
                return ci.AcquisitionInterval;
            }
            throw new ArgumentException(string.Format("Invalid argument:{0}", nameof(pointDescription)));
        }

        public ushort GetStartAddress(string pointDescription)
        {
            IConfigItem ci;
            if (pointTypeToConfiguration.TryGetValue(pointDescription, out ci))
            {
                return ci.StartAddress;
            }
            throw new ArgumentException(string.Format("Invalid argument:{0}", nameof(pointDescription)));
        }

        public ushort GetNumberOfRegisters(string pointDescription)
        {
            IConfigItem ci;
            if (pointTypeToConfiguration.TryGetValue(pointDescription, out ci))
            {
                return ci.NumberOfRegisters;
            }
            throw new ArgumentException(string.Format("Invalid argument:{0}", nameof(pointDescription)));
        }

        private void OpenConfigFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            dlg.FileOk += Dlg_FileOk;
            dlg.ShowDialog();
        }

        private void Dlg_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            path = (sender as OpenFileDialog).FileName;
        }

        private void ReadConfiguration()
        {
            using (TextReader tr = new StreamReader(path))
            {
                string s = string.Empty;
                while ((s = tr.ReadLine()) != null)
                {
                    string[] splited = s.Split(' ', '\t');
                    List<string> filtered = splited.ToList().FindAll(t => !string.IsNullOrEmpty(t));
                    if (filtered.Count == 0) continue;

                    if (s.StartsWith("STA"))
                    {
                        unitAddress = Convert.ToByte(filtered[filtered.Count - 1]);
                        continue;
                    }
                    if (s.StartsWith("TCP"))
                    {
                        TcpPort = Convert.ToInt32(filtered[filtered.Count - 1]);
                        continue;
                    }
                    if (s.StartsWith("DBC"))
                    {
                        DelayBetweenCommands = Convert.ToInt32(filtered[filtered.Count - 1]);
                        continue;
                    }

                    try
                    {
                        ConfigItem tempCi = new ConfigItem(filtered);

                        if (tempCi.NumberOfRegisters > 1)
                        {
                            for (int i = 0; i < tempCi.NumberOfRegisters; i++)
                            {
                                List<string> dupParams = new List<string>(filtered);

                                dupParams[1] = "1";
                                dupParams[2] = (tempCi.StartAddress + i).ToString();

                                string baseDesc = filtered[8].TrimStart('@');
                                dupParams[8] = "@" + baseDesc + (i + 1);

                                ConfigItem ci = new ConfigItem(dupParams);

                                if (!pointTypeToConfiguration.ContainsKey(ci.Description))
                                {
                                    pointTypeToConfiguration.Add(ci.Description, ci);
                                }
                            }
                        }
                        else
                        {
                            ConfigItem ci = new ConfigItem(filtered);
                            if (!pointTypeToConfiguration.ContainsKey(ci.Description))
                            {
                                pointTypeToConfiguration.Add(ci.Description, ci);
                            }
                        }
                    }
                    catch (ArgumentException argEx)
                    {
                        throw new ConfigurationException($"Configuration error: {argEx.Message}", argEx);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                if (pointTypeToConfiguration.Count == 0)
                {
                    throw new ConfigurationException("Configuration error! Check RtuCfg.txt file!");
                }
            }
        }

        public ushort GetTransactionId()
        {
            return transactionId++;
        }

        public byte UnitAddress
        {
            get
            {
                return unitAddress;
            }

            private set
            {
                unitAddress = value;
            }
        }

        public int TcpPort
        {
            get
            {
                return tcpPort;
            }

            private set
            {
                tcpPort = value;
            }
        }
        private int dbc;
        public int DelayBetweenCommands
        {
            get
            {
                return dbc;
            }

            private set
            {
                dbc = value;
            }
        }

        public List<IConfigItem> GetConfigurationItems()
        {
            return new List<IConfigItem>(pointTypeToConfiguration.Values);
        }
    }
}