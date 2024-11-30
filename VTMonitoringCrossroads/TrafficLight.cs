using System;
using System.IO;
using System.Xml;
using System.Data;
using System.Timers;
using System.Net.Http;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace VTMonitoringCrossroads
{
    internal class TrafficLight
    {
        static async void GetInodeXML()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{Service.ipTrafficLight}/dinputs.xml");
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    var xml = await response.Content.ReadAsStringAsync();
                    xmlDoc.LoadXml(xml);
                    XmlNodeList dinput = xmlDoc.SelectNodes("//dinput");

                    int number = 0;
                    bool status = false;
                    foreach (XmlNode input in dinput)
                    {
                        foreach (XmlNode name in input.ChildNodes)
                        {
                            if (name.Name == "number") { number = int.Parse(name.InnerText) - 1; }
                            if (name.Name == "status") { status = (name.InnerText == "1"); }
                        }

                        if (number < Service.inputTrafficLight)
                        {
                            Service.countTrafficLight[number]++;
                            if (Service.oldInputTrafficLight[number] != status)
                            {
                                Service.countTrafficLight[number] = 0;
                                Service.oldInputTrafficLight[number] = status;
                                Service.statusTrafficLight[number] = false;
                            }
                            if (Service.countTrafficLight[number] > Service.trafficLightSignalCount)
                            {
                                Service.countTrafficLight[number] = Service.trafficLightSignalCount;
                                Service.statusTrafficLight[number] = true;
                            }
                        }

                    }
                }
            }
            catch
            {
                for (int i = 0; i < Service.inputTrafficLight; i++)
                {
                    Service.countTrafficLight[i]++;
                    if (Service.countTrafficLight[i] > Service.trafficLightSignalCount)
                    {
                        Service.countTrafficLight[i] = Service.trafficLightSignalCount;
                        Service.statusTrafficLight[i] = true;
                    }
                }
            }
        }

        static async void GetInodeJson()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{Service.ipTrafficLight}/dinputs.json");
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    
                }
            }
            catch
            {
                for (int i = 0; i < Service.inputTrafficLight; i++)
                {
                    Service.countTrafficLight[i]++;
                    if (Service.countTrafficLight[i] > Service.trafficLightSignalCount)
                    {
                        Service.countTrafficLight[i] = Service.trafficLightSignalCount;
                        Service.statusTrafficLight[i] = true;
                    }
                }
            }
        }

        static async void GetKorenix()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{Service.ipTrafficLight}/dinputs.json");
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                }
            }
            catch
            {
                for (int i = 0; i < Service.inputTrafficLight; i++)
                {
                    Service.countTrafficLight[i]++;
                    if (Service.countTrafficLight[i] > Service.trafficLightSignalCount)
                    {
                        Service.countTrafficLight[i] = Service.trafficLightSignalCount;
                        Service.statusTrafficLight[i] = true;
                    }
                }
            }
        }

        static async void GetModbus()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"http://{Service.ipTrafficLight}/dinputs.json");
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();

                }
            }
            catch
            {
                for (int i = 0; i < Service.inputTrafficLight; i++)
                {
                    Service.countTrafficLight[i]++;
                    if (Service.countTrafficLight[i] > Service.trafficLightSignalCount)
                    {
                        Service.countTrafficLight[i] = Service.trafficLightSignalCount;
                        Service.statusTrafficLight[i] = true;
                    }
                }
            }
        }

        public static void SetSignalsCamera(string ip, string ch)
        {
            if (File.Exists(Service.installDir + @"Database\bpm.db"))
            {
                string sqlTrafficLighetChannelID = $"SELECT Expression FROM TrafficLightSignal WHERE LIGHTID = '{ch}'";

                using (var connection = new SQLiteConnection($@"URI=file:{Service.installDir}Database\bpm.db"))
                {
                    try
                    {
                        int[] signals = new int[] { };
                        connection.Open();
                        SQLiteCommand command = new SQLiteCommand(sqlTrafficLighetChannelID, connection);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var matches = Regex.Matches(reader.GetString(0).ToString(), @"\d+");
                                    foreach (var item in matches)
                                    {
                                        Array.Resize(ref signals, signals.Length + 1);
                                        signals[signals.Length - 1] = int.Parse(item.ToString());
                                    }
                                }
                                Service.RecognizingCameraTrafficLight.Add(ip, signals);
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
                Logs.WriteLine($"There is no database file {Service.installDir} Database\\bpm.db or it is in a different folder.");
            }
        }

        public static void AddSignalsCamera(string ip, string ch)
        {
            if (File.Exists(Service.installDir + @"Database\bpm.db"))
            {
                string sqlTrafficLighetChannelID = $"SELECT ID FROM TrafficLight WHERE CHANNELID = '{ch}'";

                using (var connection = new SQLiteConnection($@"URI=file:{Service.installDir}Database\bpm.db"))
                {
                    try
                    {
                        int[] signals = new int[] { };
                        connection.Open();
                        SQLiteCommand command = new SQLiteCommand(sqlTrafficLighetChannelID, connection);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                if (reader.Read())
                                {
                                    if (reader.GetValue(0) != null)
                                    {
                                        SetSignalsCamera(ip, reader.GetString(0).ToString());
                                    }
                                }
                            }
                            else
                            {
                                Service.RecognizingCameraTrafficLight.Add(ip, signals);
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
                Logs.WriteLine($"There is no database file {Service.installDir} Database\\bpm.db or it is in a different folder.");
            }
        }

        public static void OnTrafficLightTimer(Object source, ElapsedEventArgs e)
        {
            switch (Service.trafficLightType)
            {
                case "inode":
                    GetInodeXML();
                    break;
                case "inodejson":
                    GetInodeJson();
                    break;
                case "korenix":
                    GetKorenix();
                    break;
                default:
                    GetModbus();
                    break;
            }

            //string message = "";
            //int messagebit = 0;
            //for (int i = 0; i < Service.statusTrafficLight.Length; i++)
            //{
            //    message += (Service.statusTrafficLight[i]) ? $", 'DI-{i} ERROR' Count > {Service.countTrafficLight[i]}" : $", 'DI-{i} OK' Count > {Service.countTrafficLight[i]}";

            //    messagebit += (Service.statusTrafficLight[i]) ? 1 << i : 0;
            //}
            //string sd = Convert.ToString(messagebit, 2);

            //var result = string.Join("", Convert.ToString(messagebit, 2).Reverse());


            //Logs.WriteLine($"Traffic light controller signal statuses{message}. {sd} <-> {result}");
        }
    }
}
