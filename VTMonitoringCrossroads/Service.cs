using System;
using System.IO;
using System.Xml;
using System.Data;
using Microsoft.Win32;
using System.Collections;
using System.Data.SQLite;
using System.Configuration;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Text.RegularExpressions;



namespace VTMonitoringCrossroads
{
    public partial class Service : ServiceBase
    {
        public static string version = "1.5";

        public Service()
        {
            InitializeComponent();
        }

        public static TimeSpan localZone = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);

        public static Hashtable StatusJson = new Hashtable();
        
        public static Hashtable RecognizingCamera = new Hashtable();
        public static Hashtable RecognizingCameraStatus = new Hashtable();
        public static Hashtable RecognizingCameraViewCount = new Hashtable();

        public static Hashtable ViewCamera = new Hashtable();
        
        public static Hashtable TimeAccuracys = new Hashtable();
        
        public static Hashtable RedZona = new Hashtable();
        public static Hashtable RedZonaStatus = new Hashtable();

        public static Hashtable RecognizingCameraTrafficLight = new Hashtable();
        public static Hashtable TrafficLightStatus = new Hashtable();

        public static int storageDays = 35;
        public static bool statusWeb = true;
        public static string installDir = "C:\\Vocord\\Vocord.Traffic Crossroads\\";
        public static string diskMonitoring = "E:\\";
        public static string networkMonitoring = "vEthernet (LAN)";
        public static int dataUpdateInterval = 5;
        public static string ipTrafficLight = "192.168.88.39";
        public static string ipTahiont = "192.168.88.20";
        public static string ipCA = "192.168.88.30";

        public static int inputTrafficLight = 8;
        public static string trafficLightType = "inode";
        public static int trafficLightRequestIntervalSeconds = 10;
        public static int trafficLightSignalBrokenMinutes = 5;
        public static int trafficLightSignalCount = ((trafficLightSignalBrokenMinutes * 60) / trafficLightRequestIntervalSeconds) -1;
        public static bool[] statusTrafficLight = new bool[inputTrafficLight];
        public static bool[] oldInputTrafficLight = new bool[inputTrafficLight];
        public static int[] countTrafficLight = new int[inputTrafficLight];

        static string RoadLineNumber(string id)
        {
            string response = "-1";
            string sqlRoadLine = $"SELECT ROADLINE_ID FROM ROADLINES WHERE ROADLINE_GUID = '{id}'";
            using (var connection = new SQLiteConnection($@"URI=file:{installDir}Database\vtsettingsdb.sqlite"))
            {
                try
                {
                    connection.Open();
                    SQLiteCommand command = new SQLiteCommand(sqlRoadLine, connection);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                response = reader.GetValue(0).ToString();
                            }
                        }
                    }
                }
                catch (SqlException)
                {
                    connection.Close();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
            return response;
        }

        static string GetRoadLine(string channelId)
        {
            string response = "-1";
            string sqlRoadLine = $"SELECT ID, XML FROM ZONE WHERE CHANNELID = '{channelId}' AND TYPE = 11";
            using (var connection = new SQLiteConnection($@"URI=file:{installDir}Database\bpm.db"))
            {
                try
                {
                    connection.Open();
                    SQLiteCommand command = new SQLiteCommand(sqlRoadLine, connection);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                xmlDoc.LoadXml(reader.GetValue(1).ToString());
                                if (xmlDoc.SelectSingleNode("//LineNumber").InnerText != "0")
                                {
                                    response = RoadLineNumber(reader.GetValue(0).ToString());
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (SqlException)
                {
                    connection.Close();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
            return response;
        }


        void LoadConfig()
        {
            Logs.WriteLine("------------------------- Monitoring Service Settings -------------------------");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\VTMonitoringCrossroads", true))
            {
                if (key.GetValue("FailureActions") == null)
                {
                    key.SetValue("FailureActions", new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x60, 0xea, 0x00, 0x00 });
                }
            }

            if (ConfigurationManager.AppSettings.Count != 0)
            {
                networkMonitoring = ConfigurationManager.AppSettings["NetworkMonitoring"];
                dataUpdateInterval = Convert.ToInt32(ConfigurationManager.AppSettings["DataUpdateIntervalMinutes"]);

                ipTahiont = ConfigurationManager.AppSettings["IpTahiont"];
                ipCA = ConfigurationManager.AppSettings["IpCA"];

                trafficLightType = ConfigurationManager.AppSettings["TrafficLightType"];
                trafficLightRequestIntervalSeconds = Convert.ToInt32(ConfigurationManager.AppSettings["TrafficLightRequestIntervalSeconds"]);
                trafficLightSignalBrokenMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["TrafficLightSignalBrokenMinutes"]);
                trafficLightSignalCount = ((trafficLightSignalBrokenMinutes * 60) / trafficLightRequestIntervalSeconds) -1;
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Vocord\VOCORD Traffic CrossRoads Server"))
            {
                if (key != null)
                {
                    if (key.GetValue("InstallDir") != null)
                    {
                        installDir = key.GetValue("InstallDir").ToString();
                    }
                    if (key.GetValue("ScreenshotDir") != null)
                    {
                        diskMonitoring = key.GetValue("ScreenshotDir").ToString();
                    }
                }
            }

            if (File.Exists(installDir + @"Database\bpm.db"))
            {
                string sqlRecognizingCamera = "SELECT Id, IPAddress FROM Channel";
                
                using (var connection = new SQLiteConnection($@"URI=file:{installDir}Database\bpm.db"))
                {
                    try
                    {
                        connection.Open();
                        SQLiteCommand command = new SQLiteCommand(sqlRecognizingCamera, connection);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    RecognizingCamera.Add(reader.GetValue(1).ToString(), reader.GetValue(0).ToString());

                                    string cars = SqlLite.NumberOfCars(reader.GetValue(0).ToString());
                                    RecognizingCameraStatus.Add(reader.GetValue(1).ToString(), cars);

                                    string imgCount = Request.NumberOfOverviewImages(reader.GetValue(0).ToString());
                                    RecognizingCameraViewCount.Add(reader.GetValue(1).ToString(), imgCount);

                                    TimeAccuracy.AddFactorTimes(reader.GetValue(1).ToString());

                                    RedZona.Add(reader.GetValue(1).ToString(), GetRoadLine(reader.GetValue(0).ToString()));
                                    string roudline = SqlLite.CheckingTheRedZone(reader.GetValue(0).ToString(), RedZona[reader.GetValue(1).ToString()].ToString());
                                    RedZonaStatus.Add(reader.GetValue(1).ToString(), roudline);

                                    TrafficLight.AddSignalsCamera(reader.GetValue(1).ToString(), reader.GetValue(0).ToString());

                                    TrafficLightStatus.Add(reader.GetValue(1).ToString(), "0");

                                    Logs.WriteLine($">>>>> Recognizing Camera {reader.GetValue(1)} added to status monitoring, number of cars {cars}, number of overview photos {imgCount}, {roudline} percentage in the red light zone.");
                                }
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        Logs.WriteLine($"********** No connection to SQL Server **********");
                        connection.Close();
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                Logs.WriteLine($"There is no database file {installDir} Database\\bpm.db or it is in a different folder.");
            }

            TimeAccuracy.AddWinTime(ipTahiont, $"http://{ipTahiont}:8020");
            TimeAccuracy.AddWinTime(ipCA, $"http://{ipCA}:8030");

            if (File.Exists(installDir + @"Database\bpm.db"))
            {
                string sqlViewCamera = "SELECT Connection FROM ViewCamera";
                string viewCameraIP = @"\b((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\.)){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\b";
                Regex reg = new Regex(viewCameraIP);

                using (var connection = new SQLiteConnection($@"URI=file:{installDir}Database\bpm.db"))
                {
                    try
                    {
                        connection.Open();
                        SQLiteCommand command = new SQLiteCommand(sqlViewCamera, connection);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    foreach (Match ipCamera in reg.Matches(reader.GetValue(0).ToString()))
                                    {
                                        ViewCamera.Add(ipCamera.ToString(), Request.GetPing(ipCamera.ToString()).ToString());
                                        Logs.WriteLine($">>>>> Overview camera {ipCamera} added to status monitoring");
                                    }
                                }
                            }
                        }
                    }
                    catch (SqlException)
                    {
                        Logs.WriteLine($"********** No connection to SQL Server **********");
                        connection.Close();
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                Logs.WriteLine($"There is no database file {installDir} Database\\bpm.db or it is in a different folder.");
            }

            if (File.Exists(installDir + @"Database\bpm.db"))
            {
                string sqlTrafficLight = "SELECT Ip, PinCount FROM Modbus";

                using (var connection = new SQLiteConnection($@"URI=file:{installDir}Database\bpm.db"))
                {
                    try
                    {
                        connection.Open();
                        SQLiteCommand command = new SQLiteCommand(sqlTrafficLight, connection);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    ipTrafficLight = reader.GetValue(0).ToString();
                                    inputTrafficLight = int.Parse(reader.GetValue(1).ToString());
                                }
                            }
                            Array.Resize(ref statusTrafficLight, inputTrafficLight);
                            Array.Resize(ref oldInputTrafficLight, inputTrafficLight);
                            Array.Resize(ref countTrafficLight, inputTrafficLight);
                            Logs.WriteLine($">>>>> Traffic light {ipTrafficLight} added to status monitoring, {inputTrafficLight} signals, signal loss interval {trafficLightSignalBrokenMinutes} minute.");
                        }
                    }
                    catch (SqlException)
                    {
                        Logs.WriteLine($"********** No connection to SQL Server **********");
                        connection.Close();
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                    }
                }
            }
            else
            {
                Logs.WriteLine($"There is no database file {installDir} Database\\bpm.db or it is in a different folder.");
            }

            var pingTimer = new System.Timers.Timer(5 * 60000);
            pingTimer.Elapsed += Timer.OnPingTimer;
            pingTimer.AutoReset = true;
            pingTimer.Enabled = true;

            var hostStatusTimer = new System.Timers.Timer(dataUpdateInterval * 60000);
            hostStatusTimer.Elapsed += Timer.OnHostStatusTimer;
            hostStatusTimer.AutoReset = true;
            hostStatusTimer.Enabled = true;

            var trafficLightTimer = new System.Timers.Timer(trafficLightRequestIntervalSeconds * 1000);
            trafficLightTimer.Elapsed += TrafficLight.OnTrafficLightTimer;
            trafficLightTimer.AutoReset = true;
            trafficLightTimer.Enabled = true;

            Logs.WriteLine($">>>>> Monitoring host parameters at {dataUpdateInterval} minute intervals.");
            Logs.WriteLine("-------------------------------------------------------------------------------");
        }

        void CreatedStatusJson()
        {
            StatusJson.Add("UpTime", Request.GetUpTime().ToString());

            StatusJson.Add("DiskTotalSize", (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskTotalFreeSpace", (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskPercentTotalSize", Request.GetDiskUsagePercentage().ToString());
            StatusJson.Add("DiskPercentTotalFreeSpace", Request.GetDiskPercentFreeSpace().ToString());

            StatusJson.Add("ArchiveDepthSeconds", SqlLite.ArchiveDepthSeconds());
            StatusJson.Add("ArchiveDepthCount", SqlLite.ArchiveDepthCount());

            StatusJson.Add("ArchiveNumberOfCarsOfTheFuture", SqlLite.ArchiveNumberOfCarsOfTheFuture());
            StatusJson.Add("ArchiveNumberOfCarsOfThePast", SqlLite.ArchiveNumberOfCarsOfThePast());

            string[] network = Request.GetNetwork();
            StatusJson.Add("NetworkNetspeed", network[0]);
            StatusJson.Add("NetworkReceived", network[1]);
            StatusJson.Add("NetworkSent", network[2]);

            StatusJson.Add("TrafficLight", Request.TrafficLight());
        }

        protected override void OnStart(string[] args)
        {
            Logs.WriteLine("*******************************************************************************");
            Logs.WriteLine("************************** Service Monitoring START ***************************");
            Logs.WriteLine("*******************************************************************************");
            LoadConfig();
            CreatedStatusJson();
            Web.WEBServer.Start();
        }

        protected override void OnStop()
        {
            statusWeb = false;
            Web.WEBServer.Interrupt();
            Logs.WriteLine("*******************************************************************************");
            Logs.WriteLine("*************************** Service Monitoring STOP ***************************");
            Logs.WriteLine("*******************************************************************************");
        }
    }
}
