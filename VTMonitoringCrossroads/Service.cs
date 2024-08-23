using System;
using System.IO;
using Microsoft.Win32;
using System.Collections;
using System.Configuration;
using System.Data.SQLite;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;


namespace VTMonitoringCrossroads
{
    public partial class Service : ServiceBase
    {
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

        public static int storageDays = 30;
        public static bool statusWeb = true;
        public static string installDir = "C:\\Vocord\\Vocord.Traffic Crossroads\\";
        public static string diskMonitoring = "E:\\";
        public static string networkMonitoring = "vEthernet (LAN)";
        public static int dataUpdateInterval = 5;
        public static string ipTrafficLight = "192.168.88.39";

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
                                    Logs.WriteLine($">>>>> Recognizing Camera {reader.GetValue(1)} added to status monitoring");
                                    
                                    string cars = SqlLite.NumberOfCars(reader.GetValue(0).ToString());
                                    RecognizingCameraStatus.Add(reader.GetValue(1).ToString(), cars);
                                    Logs.WriteLine($">>>>> The recognition camera {reader.GetValue(1)} recorded {cars} cars");

                                    string imgCount = Request.NumberOfOverviewImages(reader.GetValue(0).ToString());
                                    RecognizingCameraViewCount.Add(reader.GetValue(1).ToString(), imgCount);
                                    //Logs.WriteLine($">>>>> Number of overview photos: {imgCount}, camera {reader.GetValue(1)}");
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
                string sqlTrafficLight = "SELECT Ip FROM Modbus";

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
                                    Logs.WriteLine($">>>>> Traffic light {ipTrafficLight} added to status monitoring.");
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

            var viewCameraStatusTimer = new System.Timers.Timer(5 * 60000);
            viewCameraStatusTimer.Elapsed += Timer.OnPingTimer;
            viewCameraStatusTimer.AutoReset = true;
            viewCameraStatusTimer.Enabled = true;

            var hostStatusTimer = new System.Timers.Timer(dataUpdateInterval * 60000);
            hostStatusTimer.Elapsed += Timer.OnHostStatusTimer;
            hostStatusTimer.AutoReset = true;
            hostStatusTimer.Enabled = true;

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
