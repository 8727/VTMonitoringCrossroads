using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;

namespace VTMonitoringCrossroads
{
    internal class Web
    {
        static HttpListener serverWeb;
        public static Thread WEBServer = new Thread(ThreadWEBServer);

        static void ThreadWEBServer()
        {
            serverWeb = new HttpListener();
            serverWeb.Prefixes.Add(@"http://+:8010/");
            serverWeb.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            serverWeb.Start();
            while (Service.statusWeb)
            {
                ProcessRequest();
            }
        }

        static void ProcessRequest()
        {
            var result = serverWeb.BeginGetContext(ListenerCallback, serverWeb);
            var startNew = Stopwatch.StartNew();
            result.AsyncWaitHandle.WaitOne();
            startNew.Stop();
        }

        static void ListenerCallback(IAsyncResult result)
        {
            var HttpResponse = serverWeb.EndGetContext(result);
            //string key = HttpResponse.Request.QueryString["key"];
            string json = "{\n\t\"version\":\"" + Service.version + "\"";

            json += ",\n\t\"dateTime\":\"" + DateTime.Now.ToString() + "\"";

            json += ",\n\t\"upTime\":\"" + Service.StatusJson["UpTime"] + "\"";

            json += ",\n\t\"diskTotalSize\":\"" + Service.StatusJson["DiskTotalSize"] + "\"";
            json += ",\n\t\"diskTotalFreeSpace\":\"" + Service.StatusJson["DiskTotalFreeSpace"] + "\"";
            json += ",\n\t\"diskPercentTotalSize\":\"" + Service.StatusJson["DiskPercentTotalSize"] + "\"";
            json += ",\n\t\"diskPercentTotalFreeSpace\":\"" + Service.StatusJson["DiskPercentTotalFreeSpace"] + "\"";

            json += ",\n\t\"archiveDepthSeconds\":\"" + Service.StatusJson["ArchiveDepthSeconds"] + "\"";
            json += ",\n\t\"archiveDepthCount\":\"" + Service.StatusJson["ArchiveDepthCount"] + "\"";

            json += ",\n\t\"archiveNumberOfCarsOfTheFuture\":\"" + Service.StatusJson["ArchiveNumberOfCarsOfTheFuture"] + "\"";
            json += ",\n\t\"archiveNumberOfCarsOfThePast\":\"" + Service.StatusJson["ArchiveNumberOfCarsOfThePast"] + "\"";

            json += ",\n\t\"networkNetspeed\":\"" + Service.StatusJson["NetworkNetspeed"] + "\"";
            json += ",\n\t\"networkSent\":\"" + Service.StatusJson["NetworkSent"] + "\"";
            json += ",\n\t\"networkReceived\":\"" + Service.StatusJson["NetworkReceived"] + "\"";

            json += ",\n\t\"trafficLight\":\"" + Service.StatusJson["TrafficLight"] + "\"";

            json += ",\n\t\"recognizingCamera\":[";
            int r = 0;
            foreach (DictionaryEntry RecognizingCameraStatusKey in Service.RecognizingCameraStatus)
            {
                r++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + RecognizingCameraStatusKey.Key + "\",\n\t\t\t\"quantity\":\"" + RecognizingCameraStatusKey.Value + "\"\n\t\t}";
                if (r < Service.RecognizingCameraStatus.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += ",\n\t\"trafficLightStatus\":[";
            int tl = 0;
            foreach (DictionaryEntry TrafficLightStatusKey in Service.TrafficLightStatus)
            {
                tl++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + TrafficLightStatusKey.Key + "\",\n\t\t\t\"status\":\"" + TrafficLightStatusKey.Value + "\"\n\t\t}";
                if (tl < Service.TrafficLightStatus.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += ",\n\t\"timeAccuracy\":[";
            int t = 0;
            foreach (DictionaryEntry TimeAccuracysKey in Service.TimeAccuracys)
            {
                t++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + TimeAccuracysKey.Key + "\",\n\t\t\t\"quantity\":\"" + TimeAccuracysKey.Value + "\"\n\t\t}";
                if (t < Service.TimeAccuracys.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += ",\n\t\"percentageRedZone\":[";
            int z = 0;
            foreach (DictionaryEntry RedZonaStatusKey in Service.RedZonaStatus)
            {
                z++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + RedZonaStatusKey.Key + "\",\n\t\t\t\"quantity\":\"" + RedZonaStatusKey.Value + "\"\n\t\t}";
                if (z < Service.RedZonaStatus.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += ",\n\t\"viewCamera\":[";
            int c = 0;
            foreach (DictionaryEntry ViewCameraKey in Service.ViewCamera)
            {
                c++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + ViewCameraKey.Key + "\",\n\t\t\t\"status\":\"" + ViewCameraKey.Value + "\"\n\t\t}";
                if (c < Service.ViewCamera.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += ",\n\t\"numberOfOverviewImages\":[";
            int v = 0;
            foreach (DictionaryEntry RecognizingCameraViewCountKey in Service.RecognizingCameraViewCount)
            {
                v++;
                json += "\n\t\t{\n\t\t\t\"ip\":\"" + RecognizingCameraViewCountKey.Key + "\",\n\t\t\t\"quantity\":\"" + RecognizingCameraViewCountKey.Value + "\"\n\t\t}";
                if (v < Service.RecognizingCameraViewCount.Count)
                {
                    json += ",";
                }
            }
            json += "\n\t]";

            json += "\n}";

            HttpResponse.Response.Headers.Add("Content-Type", "application/json");
            HttpResponse.Response.StatusCode = 200;
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            HttpResponse.Response.ContentLength64 = buffer.Length;
            HttpResponse.Response.OutputStream.Write(buffer, 0, buffer.Length);
            HttpResponse.Response.Close();
        }
    }
}
