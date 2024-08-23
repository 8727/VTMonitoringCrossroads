using System;
using System.Collections;
using System.Timers;

namespace VTMonitoringCrossroads
{
    internal class Timer
    {
        public static void OnRecognizingCameraStatusTimer(Object source, ElapsedEventArgs e)
        {
            ICollection recognizingCameraKeys = Service.RecognizingCamera.Keys;
            foreach (string ipRecognizingCameraKey in recognizingCameraKeys)
            {
                string id = Service.RecognizingCamera[ipRecognizingCameraKey].ToString();
                string imgCount = Request.NumberOfOverviewImages(id);
                Service.RecognizingCameraStatus[ipRecognizingCameraKey] = SqlLite.NumberOfCars(id);
                Service.RecognizingCameraViewCount[ipRecognizingCameraKey] = imgCount;
            }
        }

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
            Logs.WriteLine($"Interface loading incoming {Service.StatusJson["NetworkReceived"]}, outgoing {Service.StatusJson["NetworkSent"]}.");
//-------------------------------------------------------------------------------------------------

            Service.StatusJson["TrafficLight"] = Request.TrafficLight();

            Logs.WriteLine("-------------------------------------------------------------------------------");
        }
    }
}
