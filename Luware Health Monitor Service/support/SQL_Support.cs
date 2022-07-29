using Luware_Health_Monitor_Service.Classes;
using MWSupport;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luware_Health_Monitor_Service.support
{
    public class SQL_Support
    {
        public List<PSServices> Get_ConnectionString(List<PSServices> psServiceList)
        {

            List<PSServices> ConnectionString = new List<PSServices>();
            string psConfigFile = "LUCS.PS.Service.exe.config";

            if ((psServiceList != null) && (psServiceList.Count > 0))
            {
                foreach (PSServices ps in psServiceList)
                {
                    try
                    {
                        string psDirectory = string.Empty; //  Path.GetFullPath(ps.ServicePath);
                        string[] partially = ps.ServicePath.Split('\\');
                        psDirectory = "\\";
                        for (int x = 0; x < partially.Count() - 1; x++)
                        {
                            psDirectory = psDirectory + partially[x] + "\\";
                        }

                        string fileName = psDirectory.Substring(1) + psConfigFile;

                        MWLogger.WriteLog("PS Service found: " + ps.ServicePath.ToString() + " > " + fileName);

                        if (File.Exists(fileName))
                        {
                            MWLogger.WriteLog("File found - reading content");

                            string readLines = string.Empty;
                            using (StreamReader sr = new StreamReader(fileName))
                            {
                                readLines = sr.ReadToEnd();
                            }
                            /*                                var importantLines = File.ReadLines(fileName)
                                                                .SkipWhile(line => !line.Contains("Data Source="));
                                                            // https://stackoverflow.com/questions/12856471/c-sharp-search-string-in-txt-file
                            */
                            int x = readLines.IndexOf("Data Source=");
                            if (x > 0)
                            {
                                string inbetween = readLines.Substring(x);
                                int y = inbetween.IndexOf("&quot;");
                                if (y > 0)
                                {
                                    ps.ConnectionString = inbetween.Substring(0, y);
                                    ConnectionString.Add(ps);
                                    MWLogger.WriteLog("Connection String: " + inbetween.Substring(0, y));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MWLogger.WriteLog("Exception extracting path: " + ps.ServicePath.ToString() + " " + ex.Message);
                    }

                }
            }
            else
            {
                MWLogger.WriteLog("PS Service - List is empty... ");
            }

            return ConnectionString;
        }


        public PSServices Test_ConnectionString(PSServices ConnectionString)
        {
            MWLogger.WriteLog("Starting Test_SQL_Connection " + ConnectionString.ConnectionString);

            SqlConnection myConnection = new SqlConnection(ConnectionString.ConnectionString);
            try
            {
                myConnection.Open();
                ConnectionString.Status = "Ok";
                MWLogger.WriteLog("Connection exists for " + ConnectionString.ServiceName);
            }
            catch (Exception ex)
            {
                MWLogger.WriteLog("Error connecting to SQL: " + ex.Message);
                ConnectionString.Status = ex.Message;
                return ConnectionString;
            }

            return ConnectionString;
        }
    }

}

