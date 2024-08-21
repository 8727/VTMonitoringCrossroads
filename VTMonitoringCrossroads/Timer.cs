using System;
using System.Collections;
using System.Threading;
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
            Service.StatusJson["UpTime"] = Request.GetUpTime().ToString();
            Logs.WriteLine($"Host uptime in seconds {Service.StatusJson["UpTime"]}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["DiskTotalSize"] = (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskTotalFreeSpace"] = (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskPercentSize"] = (Request.GetDiskUsagePercentage()).ToString();
            Service.StatusJson["DiskPercentFreeSpace"] = (Request.GetDiskPercentFreeSpace()).ToString();
            Logs.WriteLine($"Total disk size {Service.StatusJson["DiskTotalSize"]} GB, free space size {Service.StatusJson["DiskTotalFreeSpace"]} GB, disk size as a percentage {Service.StatusJson["DiskPercentSize"]}, free disk space percentage {Service.StatusJson["DiskPercentFreeSpace"]}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["NetworkSent"] = (Request.GetNetworkSent()).ToString();
            Service.StatusJson["NetworkReceived"] = (Request.GetNetworkReceived()).ToString();
            Logs.WriteLine($"Interface loading incoming {Service.StatusJson["NetworkReceived"]}, outgoing {Service.StatusJson["NetworkSent"]}.");
//-------------------------------------------------------------------------------------------------
            Service.StatusJson["ArchiveDepthSeconds"] = SqlLite.ArchiveDepthSeconds();
            Service.StatusJson["ArchiveDepthCount"] =  SqlLite.ArchiveDepthCount();
            TimeSpan depthSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["ArchiveDepthSeconds"]));
            Logs.WriteLine($"Storage depth: time {depthSeconds}, number {Service.StatusJson["ArchiveDepthCount"]}.");
        }





    }
}
