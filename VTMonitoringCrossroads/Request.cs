using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Messaging;


namespace VTMonitoringCrossroads
{
    internal class Request
    {
        static DriveInfo driveInfo = new DriveInfo(Service.diskMonitoring);

        public static byte GetPing(string ip)
        {
            byte result = 0;
            PingReply p = new Ping().Send(ip, 5000);
            if (p.Status == IPStatus.Success)
            {
                result = 1;
            }
            return result;
        }

        public static UInt32 GetUpTime()
        {
            PerformanceCounter uptime = new PerformanceCounter("System", "System Up Time");
            uptime.NextValue();
            return Convert.ToUInt32(uptime.NextValue());
        }

        public static long GetDiskTotalSize()
        {
            return driveInfo.TotalSize;
        }

        public static long GetDiskTotalFreeSpace()
        {
            return driveInfo.TotalFreeSpace; ;
        }

        public static double GetDiskUsagePercentage()
        {
            return (driveInfo.TotalFreeSpace / (driveInfo.TotalSize / 100.0));
        }

        public static double GetDiskPercentFreeSpace()
        {
            return (100 - (driveInfo.TotalFreeSpace / (driveInfo.TotalSize / 100.0)));
        }

        public static double GetNetworkSent()
        {
            PerformanceCounter counterSent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", Service.networkInterfaceForMonitoring);
            counterSent.NextValue();
            counterSent.NextValue();
            return (counterSent.NextValue() / 131_072.0);
        }

        public static double GetNetworkReceived()
        {
            PerformanceCounter counterReceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", Service.networkInterfaceForMonitoring);
            counterReceived.NextValue();
            counterReceived.NextValue();
            return (counterReceived.NextValue() / 131_072.0);
        }

        public static string NumberOfOverviewImages(string id)
        {
            string folder = Service.diskMonitoring + "\\" + SqlLite.PathToLastFolder(id);
            if (Directory.Exists(folder))
            {
                return Directory.GetFiles(folder, "*View*").Length.ToString();
            }
            return "ERROR";
        }


    }
}
