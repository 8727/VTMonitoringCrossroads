using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VTMonitoringCrossroads
{
    internal class TimeAccuracy
    {
        static async Task<string> GetFactorTime(string ip)
        {
            string content = "";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{ip}/systemmanager/api/Time/current");
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    string factorTime = await response.Content.ReadAsStringAsync();
                    DateTime endDateTime = DateTime.ParseExact(factorTime.Remove(19), "yyyy-M-d H:mm:ss", CultureInfo.InvariantCulture);
                    content = DateTime.Now.Subtract(endDateTime).TotalSeconds.ToString();
                }
            }
            catch
            {
                content = "ERROR";
            }
            return content;
        }

        static async Task<string> GetWinTime(string url)
        {
            string content = "";
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    var datajson = new JavaScriptSerializer().Deserialize<dynamic>(json);
                    string winTime = datajson["dateTime"];
                    DateTime endDateTime = DateTime.ParseExact(winTime, "d.M.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                    content = DateTime.Now.Subtract(endDateTime).TotalSeconds.ToString();
                }
            }
            catch
            {
                content = "ERROR";
            }
            return content;
        }

        public static async void SetFactorTimes(string ip)
        {
            string dt = await GetFactorTime(ip);
            Service.TimeAccuracys[ip] = dt;
        }

        public static async void SetWinTime(string ip, string url)
        {
            string dt = await GetWinTime(url);
            Service.TimeAccuracys[ip] = dt;
        }

        public static async void AddFactorTimes(string ip)
        {
            string dt = await GetFactorTime(ip);
            Service.TimeAccuracys.Add(ip, dt);
        }

        public static async void AddWinTime(string ip, string url)
        {
            string dt = await GetWinTime(url);
            Service.TimeAccuracys.Add(ip, dt);
        }
    }
}
