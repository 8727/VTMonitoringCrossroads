using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data;
using System;
using System.Runtime.Remoting.Messaging;


namespace VTMonitoringCrossroads
{
    internal class SqlLite
    {
        static object SQLQuery(string query)
        {
            object response = -1;
            using (var connection = new SQLiteConnection($@"URI=file:{Service.installDir}Database\vtvehicledb.sqlite"))
            {
                try
                {
                    connection.Open();
                    SQLiteCommand command = new SQLiteCommand(query, connection);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                response = reader.GetValue(0);
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
            return response;
        }

        public static string ArchiveDepthSeconds()
        {
            //string oldEntry = "SELECT CHECKTIME FROM CARS LIMIT 1";
            string lastEntry = "SELECT CHECKTIME FROM CARS ORDER BY CHECKTIME DESC LIMIT 1";

            //DateTime archiveOld = DateTime.FromFileTime((long)SQLQuery(oldEntry));

            return lastEntry;
        }

        public static string ArchiveDepthCount()
        {
            string sqlQuery = "SELECT COUNT(CARS_ID) FROM CARS";
            return SQLQuery(sqlQuery).ToString();
        }



    }
}
