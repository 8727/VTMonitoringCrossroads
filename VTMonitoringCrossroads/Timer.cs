using System;
using System.Collections;
using System.Threading;
using System.Timers;

namespace VTMonitoringCrossroads
{
    internal class Timer
    {
        public static void OnPingTimer(Object source, ElapsedEventArgs e)
        {
            Service.StatusJson["TrafficLight"] = Request.TrafficLight();
            ICollection viewCameraKeys = Service.ViewCamera.Keys;
            foreach (string ipViewCameraKey in viewCameraKeys)
            {
                Service.ViewCamera[ipViewCameraKey] = Request.GetPing(ipViewCameraKey).ToString();
            }
        }

        public static void OnHostStatusTimer(Object source, ElapsedEventArgs e)
        {
            ICollection recognizingCameraKeys = Service.RecognizingCamera.Keys;
            ICollection viewCameraKeys = Service.ViewCamera.Keys;

            Service.StatusJson["UpTime"] = Request.GetUpTime().ToString();
            TimeSpan uptime = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["UpTime"]));
            Logs.WriteLine($"Host uptime {uptime}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["DiskTotalSize"] = (Request.GetDiskTotalSize() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskTotalFreeSpace"] = (Request.GetDiskTotalFreeSpace() / 1_073_741_824.0).ToString();
            Service.StatusJson["DiskPercentSize"] = (Request.GetDiskUsagePercentage()).ToString();
            Service.StatusJson["DiskPercentFreeSpace"] = (Request.GetDiskPercentFreeSpace()).ToString();
            Logs.WriteLine($"Total disk size {Service.StatusJson["DiskTotalSize"]} GB, free space size {Service.StatusJson["DiskTotalFreeSpace"]} GB, disk size as a percentage {Service.StatusJson["DiskPercentSize"]}, free disk space percentage {Service.StatusJson["DiskPercentFreeSpace"]}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["ArchiveDepthSeconds"] = SqlLite.ArchiveDepthSeconds();
            Service.StatusJson["ArchiveDepthCount"] =  SqlLite.ArchiveDepthCount();
            TimeSpan depthSeconds = TimeSpan.FromSeconds(Convert.ToDouble(Service.StatusJson["ArchiveDepthSeconds"]));
            Logs.WriteLine($"Storage depth: time {depthSeconds}, number {Service.StatusJson["ArchiveDepthCount"]}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["ArchiveNumberOfCarsOfTheFuture"] = SqlLite.ArchiveNumberOfCarsOfTheFuture();
            Service.StatusJson["ArchiveNumberOfCarsOfThePast"] = SqlLite.ArchiveNumberOfCarsOfThePast();
            Logs.WriteLine($"Archive number of cars from the future {Service.StatusJson["ArchiveNumberOfCarsOfTheFuture"]}, archive number of cars from the past {Service.StatusJson["ArchiveNumberOfCarsOfThePast"]}.");
//-------------------------------------------------------------------------------------------------

            string[] network = Request.GetNetwork();
            Service.StatusJson["NetworkNetspeed"] = network[0];
            Service.StatusJson["NetworkReceived"] = network[1];
            Service.StatusJson["NetworkSent"] = network[2];
            Logs.WriteLine($"Interface speed {Service.StatusJson["NetworkNetspeed"]}, incoming load {Service.StatusJson["NetworkReceived"]}, outgoing load {Service.StatusJson["NetworkSent"]}.");
//-------------------------------------------------------------------------------------------------

            if (Service.StatusJson["TrafficLight"].ToString() == "1")
            {
                Logs.WriteLine($"Traffic light controller available.");
            }
            else
            {
                Logs.WriteLine($"Traffic light controller is not available.");
            }
//-------------------------------------------------------------------------------------------------

            foreach (string ipRecognizingCameraKey in recognizingCameraKeys)
            {
                string id = Service.RecognizingCamera[ipRecognizingCameraKey].ToString();
                string imgCount = Request.NumberOfOverviewImages(id);
                Service.RecognizingCameraStatus[ipRecognizingCameraKey] = SqlLite.NumberOfCars(id);
                Service.RecognizingCameraViewCount[ipRecognizingCameraKey] = imgCount;

                Logs.WriteLine($"Camera recognition {ipRecognizingCameraKey}, number of cars {Service.RecognizingCameraStatus[ipRecognizingCameraKey]}, number of overview photos {imgCount}.");
            }
//-------------------------------------------------------------------------------------------------

            foreach (string ipViewCameraKey in viewCameraKeys)
            {
                if (Service.ViewCamera[ipViewCameraKey].ToString() == "1")
                {
                    Logs.WriteLine($"Camera Status Overview {ipViewCameraKey} available.");
                }
                else
                {
                    Logs.WriteLine($"Camera Status Overview {ipViewCameraKey} is not available.");
                }
            }
//-------------------------------------------------------------------------------------------------

            Logs.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
