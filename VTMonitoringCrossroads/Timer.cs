using System;
using System.Collections;
using System.Timers;

namespace VTMonitoringCrossroads
{
    internal class Timer
    {
        public static void OnViewCameraStatusTimer(Object source, ElapsedEventArgs e)
        {
            ICollection viewCameraKeys = Service.ViewCamera.Keys;
            foreach (string ipViewCameraKey in viewCameraKeys)
            {
                Service.ViewCamera[ipViewCameraKey] = Request.GetPing(ipViewCameraKey.ToString()).ToString();
            }
        }

        public static void OnHostStatusTimer(Object source, ElapsedEventArgs e)
        {
            UInt32 upTimeUInt32 = Request.GetUpTime();
            Service.StatusJson["UpTime"] = upTimeUInt32.ToString();
            Logs.WriteLine($"Host uptime in seconds {upTimeUInt32}.");

            long diskSize = Request.GetDiskTotalSize() / 1_073_741_824;
            long diskFreeSpace = Request.GetDiskTotalFreeSpace() / 1_073_741_824;
            double diskPercentSize = Request.GetDiskUsagePercentage();
            double diskPercentFreeSpace = Request.GetDiskPercentFreeSpace();

            Service.StatusJson["DiskTotalSize"] = diskSize.ToString();
            Service.StatusJson["DiskTotalFreeSpace"] = diskFreeSpace.ToString();
            Service.StatusJson["DiskPercentSize"] = diskPercentSize.ToString();
            Service.StatusJson["DiskPercentFreeSpace"] = diskPercentFreeSpace.ToString();
            Logs.WriteLine($"Total disk size {diskSize} GB, free space size {diskFreeSpace} GB, disk size as a percentage {diskPercentSize}, free disk space percentage {diskPercentFreeSpace}.");

            double networkSent = Request.GetNetworkSent();
            double networkReceived = Request.GetNetworkReceived();

            Logs.WriteLine($"Interface loading incoming {networkReceived}, outgoing {networkSent}.");

            Service.StatusJson["ArchiveDepthSeconds"] = SqlLite.ArchiveDepthSeconds();
            Service.StatusJson["ArchiveDepthCount"] =  SqlLite.ArchiveDepthCount();
        }





    }
}
