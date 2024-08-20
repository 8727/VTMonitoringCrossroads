﻿using System;
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
        public static Hashtable ViewCamera = new Hashtable();



        public static int storageDays = 30;
        public static bool statusWeb = true;
        public static string installDir = "C:\\Vocord\\Vocord.Traffic Crossroads\\";
        public static string diskMonitoring = "E:\\";
        public static string networkInterfaceForMonitoring = "Intel[R] I211 Gigabit Network Connection _2";



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
                networkInterfaceForMonitoring = ConfigurationManager.AppSettings["NetworkInterfaceForMonitoring"];


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
                Logs.WriteLine($"There is no database file {installDir} Database\\vtsettingsdb.sqlite or it is in a different folder.");
            }


            var viewCameraStatusTimer = new System.Timers.Timer(5 * 60000);
            viewCameraStatusTimer.Elapsed += Timer.OnViewCameraStatusTimer;
            viewCameraStatusTimer.AutoReset = true;
            viewCameraStatusTimer.Enabled = true;
            Logs.WriteLine($">>>>> Monitoring of surveillance cameras is enabled at intervals of 5 minutes");

            var hostStatusTimer = new System.Timers.Timer(5 * 60000);
            hostStatusTimer.Elapsed += Timer.OnHostStatusTimer;
            hostStatusTimer.AutoReset = true;
            hostStatusTimer.Enabled = true;
            Logs.WriteLine($">>>>> Host parameters monitoring is enabled at 5 minute intervals.");



            Logs.WriteLine("-------------------------------------------------------------------------------");

        }

        void CreatedStatusJson()
        {
            StatusJson.Add("UpTime", Request.GetUpTime().ToString());

            StatusJson.Add("DiskTotalSize", (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskTotalFreeSpace", (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString());
            StatusJson.Add("DiskPercentTotalSize", Request.GetDiskUsagePercentage().ToString());
            StatusJson.Add("DiskPercentTotalFreeSpace", Request.GetDiskPercentFreeSpace().ToString());

            StatusJson.Add("NetworkSent", Request.GetNetworkSent().ToString());
            StatusJson.Add("NetworkReceived", Request.GetNetworkReceived().ToString());

            StatusJson.Add("ArchiveDepthSeconds", SqlLite.ArchiveDepthSeconds());
            StatusJson.Add("ArchiveDepthCount", SqlLite.ArchiveDepthCount());

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