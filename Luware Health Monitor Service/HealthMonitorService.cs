using Luware_Health_Monitor_Service.Classes;
using Luware_Health_Monitor_Service.support;
using mwsupport.Classes;
using MWSupport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace Luware_Health_Monitor_Service
{
    public partial class HealthMonitorService : ServiceBase
    {
        List<PSServices> PSStrings = new List<PSServices>();
        List<Services> SList = new List<Services>();
        List<Services> SfBList = new List<Services>();
        List<EventViewer> EvViewer = new List<EventViewer>();
        List<LogSystemEvents> evRecs = new List<LogSystemEvents>();
        List<LogSystemEvents> oldevRecs = new List<LogSystemEvents>();
        bool evRecsChecked = false;

        List<S2Start> oldstartList = new List<S2Start>();
        List<S2Start> sChanged = new List<S2Start>();
        public List<S2Start> StartList = new List<S2Start>();

        public Language_Settings LSettings = new Language_Settings();
        Dashboard_Configuration DashbConfig = new Dashboard_Configuration();
        public List<Server> ServerList = new List<Server>();
        
        public HealthMonitorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Service_Support sps = new Service_Support();

            string logPath = MWLogger.Get_FileFolderPath("regularLog.txt", "\\LUWARE AG\\Luware Health Monitor Service\\Logs", false, true);
            string lPath = string.Empty;
            
            try
            {
                lPath = MWLogger.WriteLog("Service Luware Health Monitor Service started", true, "\\Luware AG\\Luware Health Monitor Service\\Logs", true);
            }
            catch (Exception ex)
            {
                WriteToFile("Fehler: " + ex.Message);
            }
            
            LSettings = MWLocalize.Init_Localize(LSettings, "Luware_Health_Monitor.Resources", Assembly.GetExecutingAssembly());

            string cfgFile = sps.Get_Config_File(false);
            MWLogger.WriteLog("CfgFile located: " + cfgFile);
            if (cfgFile != null)
            {
                DashbConfig = sps.Load_XmlFile(cfgFile);
            }

            MWLogger.WriteLog("CfgFile read: " + cfgFile + "  >" + DashbConfig.StandardServerConfig + "<");
            if (DashbConfig.StandardServerConfig != string.Empty)
            {
                GetServices supsvc = new GetServices();
                ServerList = supsvc.Load_ServerList_File(DashbConfig.StandardServerConfig);
            }

            foreach (Server svr in ServerList)
            {
                MWLogger.WriteLog("Server: " + svr.HostName);
            }
            Check_Values();

            int timerValue = 10;

            if (DashbConfig.RefreshInterval > 0)
            {
                timerValue = DashbConfig.RefreshInterval;
            }

            TimerStarted = DateTime.Now;
            // calculate TimeSpan to 1/1000 secs

            timer.Interval = timerValue;
            timer.Elapsed += time_over;
            timer.Enabled = true;
            timer.Start();
        }

        DateTime TimerStarted;
        System.Timers.Timer timer = new System.Timers.Timer();
        private void Check_Values()
        {
            
            List<Services> complList = new List<Services>();
            Service_Support sps = new Service_Support();
            SQL_Support sqlS = new SQL_Support();

            complList = Retrieve_Luware_Services();

            MWLogger.WriteLog("List of Services with " + complList.Count.ToString() + " entries.");

            SList.Clear();
            SfBList.Clear();
            PSStrings.Clear();

            foreach (Services compl in complList)
            {
                if (compl.Category == "Luware")
                {
                    if (compl.ServiceUPN == String.Empty)
                    {
                        compl.ServiceUPN = compl.ServiceHost;
                        // MWLogger.WriteLog("ServiceUPN empty... " + compl.ServiceUPN + " " + compl.ServiceName);
                        compl.ServiceUPN = compl.ServiceHost;
                    }
                    SList.Add(compl);
                    if (compl.ServiceHost != string.Empty)
                        MWLogger.WriteLog("Service added to Luware (" + compl.ServiceName + " on Host: " + compl.ServiceHost + " Status: " + compl.ServiceStatus.ToString() + ")");
                    else
                        MWLogger.WriteLog("Service added to Luware (" + compl.ServiceName + " on localhost)");
                }
                else if ((compl.Category == "SfB") || (compl.Category == "SQL"))
                {
                    SfBList.Add(compl);
                    if (compl.ServiceHost != string.Empty)
                        MWLogger.WriteLog("Service added to SfB (" + compl.ServiceName + " on Host: " + compl.ServiceHost + " with status "+ compl.ServiceStatus.ToString() + ")");
                    else
                        MWLogger.WriteLog("Service added to SfB (" + compl.ServiceName + " on localhost)");
                }
                else
                    MWLogger.WriteLog("Service not added to any category (" + compl.ServiceName + ")");

            }

            MWLogger.WriteLog("Services count: " + SList.Count.ToString());

            if (SList.Count > 0)
            {
                MWLogger.WriteLog("Services found - working on it... ");
                foreach (Services service in SList)
                {
                    if (service.ServiceName.ToUpper().Contains("-PS"))
                    {
                        PSServices ps = new PSServices();
                        ps.ServiceHost = service.ServiceHost;
                        ps.ServiceName = service.ServiceName;
                        ps.ServicePath = service.ServicePath;
                        MWLogger.WriteLog(ps.ServiceName + " < > " + ps.ServicePath);
                        PSStrings.Add(ps);
                        MWLogger.WriteLog("PSService found: " + ps.ServiceHost + "  " + ps.ServiceName);
                    }
                }
            }

            MWLogger.WriteLog("Before Get_ConnectionString");
            PSStrings = sqlS.Get_ConnectionString(PSStrings);
            MWLogger.WriteLog("Connection Strings retrieved: " + PSStrings.Count.ToString());

            Recheck_Value_Graph();

        }

        int RefreshCycle = 0;
        private void time_over(object sender, ElapsedEventArgs e)
        {
            DateTime LastTick = DateTime.Now;
            Service_Support sps = new Service_Support();
            TimeSpan tSpan = LastTick - TimerStarted;

            int timerValue = 10;
            if (DashbConfig.RefreshInterval > 0)
                timerValue = DashbConfig.RefreshInterval;
            
            if (tSpan.TotalMinutes.CompareTo(Convert.ToDouble(timerValue)) > 0)
            {
                TimerStarted = DateTime.Now;
                RefreshCycle++;
                MWLogger.WriteLog(Environment.NewLine);
                MWLogger.WriteLog("---------------------------");
                MWLogger.WriteLog("Starting new cycle (" + RefreshCycle + ")");
                MWLogger.WriteLog("---------------------------");
                MWLogger.WriteLog(Environment.NewLine);

                Check_Values();
            }

        }

        private void Recheck_Value_Graph()
        {
            Service_Support sps = new Service_Support();
            SQL_Support sqlS = new SQL_Support();

            if (SList.Count > 0)
            {
                int compSum = SList.Count;
                int realSum = 0;

                // int serviceOk = 0;
                foreach (Services s in SList)
                {
                    // realSum = realSum + s.ServiceStatus;
                    if (s.ServiceStatus == 5)
                    {
                         //MWLogger.WriteLog("LUCS/TM-Service properly running: " + s.ServiceUPN + " / " + s.ServiceName + " -  " + s.ServiceStatus);
                         realSum++;
                    }
                    else
                        MWLogger.WriteLog("LUCS/TM-Service not properly running: " + s.ServiceUPN + " / " + s.ServiceName + " -  " + s.ServiceStatus);
                }

            }

            int sqlError = 0;

            for (int x = 0; x < PSStrings.Count; x++)
            {
                MWLogger.WriteLog("Checking Connection String " + PSStrings[x].ServiceName);

                PSStrings[x] = sqlS.Test_ConnectionString(PSStrings[x]);
                if (PSStrings[x].Status != "Ok")
                {
                    MWLogger.WriteLog("PSService: " + PSStrings[x].ServiceName + " not working");
                    sqlError++;
                }
            }

            if (SfBList.Count > 0)
            {

                int compSum = SfBList.Count * 5;
                int realSum = 0;

                int serviceOk = 0;
                foreach (Services s in SfBList)
                {
                    realSum = realSum + s.ServiceStatus;
                    if (s.ServiceStatus == 5)
                        serviceOk++;
                }

            }

            MWLogger.WriteLog("Scanning for Eventviewer Files");
            EvViewer = EVENTV_Handler.Get_EventViewer(ServerList);
            List<LogSystemEvents> evRecsChanged = new List<LogSystemEvents>();

            if ((EvViewer.Count > 0)&& (DashbConfig.EVENTV_Check))
            {
                evRecs.Clear();
                evRecs = EVENTV_Handler.Get_Events(EvViewer);

                if (evRecsChecked)
                {
                    if (evRecs.Count > oldevRecs.Count)
                    {
                        foreach (LogSystemEvents ev in evRecs)
                        {
                            var oldEvts = oldevRecs.Where(s => (s.Id == ev.Id) && (s.Machine == ev.Machine) && (s.TimeCreated == ev.TimeCreated)).ToList();
                            // MWLogger.WriteLog("Searched for  " + svc.ServerUPN + " / " + svc.ServiceName + ": " + svc.ServiceStatus.ToString() + " (" + oldSvc.Count.ToString() + ")");

                            if (oldEvts.Count == 0)
                            {

                                if (DashbConfig.EVENTV_Exclude.Contains(ev.Id.ToString()))
                                {
                                    MWLogger.WriteLog("Event " + ev.Id.ToString() + " on " + ev.Machine + " detected, but excluded by exclusion list");
                                }
                                else
                                {
                                    LogSystemEvents lEvent = new LogSystemEvents();
                                    lEvent = ev;
                                    evRecsChanged.Add(lEvent);
                                }
                            }
                        }
                    }
                }
            }
            else
                MWLogger.WriteLog("NO Eventlogs found... ");


            if ((sChanged.Count > 0) || (evRecsChanged.Count > 0))
            {
                MWLogger.WriteLog("Mail will be sent: Services changes: " + sChanged.Count.ToString() + " Eventlogs changed: " + evRecsChanged.Count.ToString());
                string service_text = Mail_Handler.Create_Service_Mail(DashbConfig, sChanged, evRecsChanged);

                if (DashbConfig.MailEnabled)
                {
                    MWLogger.WriteLog("Mail settings enabled");

                    string mailMessage = DashbConfig.MailBody;
                    if (mailMessage.ToUpper().Contains("#SERVICETABLE#"))
                    {
                        mailMessage = mailMessage.Replace("#ServiceTable#", service_text);
                    }
                    else
                        mailMessage += service_text;

                    MWLogger.WriteLog("Error-Mailbody: " + Environment.NewLine + mailMessage);
                    Mail_Handler.Send_Mail(DashbConfig, service_text);
                }
                else
                    MWLogger.WriteLog("Mail settings not enabled");
            }
            else
                MWLogger.WriteLog("No changed services/events flagged");

            evRecsChecked = true;
            oldevRecs = evRecs;
            oldstartList = StartList;

        }
        /*        private void Recheck_Value_Graph()
                {
                    Service_Support sps = new Service_Support();
                    SQL_Support sqlS = new SQL_Support();

                    if (SList.Count > 0)
                    {
                        int compSum = SList.Count;
                        int realSum = 0;

                        foreach (Services s in SList)
                        {
                            // realSum = realSum + s.ServiceStatus;
                            if (s.ServiceStatus == 5)
                            {
                                MWLogger.WriteLog("LUCS/TM-Service properly running: " + s.ServiceHost + " / " + s.ServiceName + " -  " + s.ServiceStatus);
                                realSum++;
                            }
                        }

                        MWLogger.WriteLog("LUCS / TM Overall: " + realSum.ToString() + " from " + compSum.ToString() + " properly working");
                    }

                    int sqlError = 0;

                    for (int x = 0; x < PSStrings.Count; x++)
                    {
                        MWLogger.WriteLog("Checking Connection String " + PSStrings[x].ServiceName);

                        PSStrings[x] = sqlS.Test_ConnectionString(PSStrings[x]);

                        if (PSStrings[x].Status != "Ok")
                        {
                            MWLogger.WriteLog("PSService: " + PSStrings[x].ServiceName + " not working");
                            sqlError++;
                        }
                        else
                        {
                            MWLogger.WriteLog("PSService: " + PSStrings[x].ServiceName + " working");
                        }
                    }

                    if (SfBList.Count > 0)
                    {

                        int compSum = SfBList.Count * 5;
                        int realSum = 0;

                        int serviceOk = 0;
                        foreach (Services s in SfBList)
                        {
                            realSum = realSum + s.ServiceStatus;
                            if (s.ServiceStatus == 5)
                                serviceOk++;
                        }

                    }

                    MWLogger.WriteLog("Scanning for Eventviewer Files");
                    EvViewer = sps.Get_EventViewer(ServerList);

                    if (EvViewer.Count > 0)
                    {
                        MWLogger.WriteLog("Eventviewer Logs found ... ");
                        evRecs = sps.Get_Events(EvViewer);

                    }
                    else
                        MWLogger.WriteLog("NO Eventlogs found... ");

                }
        */

        private List<Services> Retrieve_Luware_Services()
        {
            GetServices GServices = new GetServices();


            MWLogger.WriteLog("Start searching for Services... ");

            if (ServerList.Count == 0)
            {
                Server localServer = new Server();
                localServer.HostName = System.Net.Dns.GetHostName().ToUpper();
                localServer.HostType = "Luware";
                ServerList.Add(localServer);
            }

            RichTextBox tbResult = new RichTextBox();
            StartList = GServices.Get_Luware_Services(ServerList, tbResult);

            // MWLogger.WriteLog("Searching old list " + oldstartList.Count.ToString() + "(" + StartList.Count.ToString() + ")");
            sChanged.Clear();

            foreach (S2Start svc in StartList)
            {
                var oldSvc = oldstartList.Where(s => (s.ServiceName == svc.ServiceName) && (s.ServerUPN == svc.ServerUPN)).ToList();
                // MWLogger.WriteLog("Searched for  " + svc.ServerUPN + " / " + svc.ServiceName + ": " + svc.ServiceStatus.ToString() + " (" + oldSvc.Count.ToString() + ")");

                if (oldSvc.Count > 0)
                {
                    if (oldSvc[0].ServiceStatus != svc.ServiceStatus)
                    {
                        S2Start lsvc = new S2Start();
                        lsvc.ServerUPN = svc.ServerUPN;
                        lsvc.ServiceName = svc.ServiceName;
                        lsvc.ServiceStatus = svc.ServiceStatus;
                        MWLogger.WriteLog("Service status changed " + oldSvc[0].ServerUPN + " / " + oldSvc[0].ServiceName + ": " + oldSvc[0].ServiceStatus.ToString() + " -> " + svc.ServerUPN + " / " + svc.ServiceName + ": " + svc.ServiceStatus.ToString());
                        if (lsvc.ServiceStatus < oldSvc[0].ServiceStatus)
                        {
                            sChanged.Add(lsvc);
                            MWLogger.WriteLog("Service " + lsvc.ServerUPN + " / " + lsvc.ServiceName + " added: " + sChanged.Count.ToString());
                        }
                        else
                            MWLogger.WriteLog("Service " + lsvc.ServerUPN + " / " + lsvc.ServiceName + " not added: " + sChanged.Count.ToString());
                    }
                }
                else
                    MWLogger.WriteLog("Service " + svc.ServerUPN + " / " + svc.ServiceName + " not found in old List");
            }

            /*
            if (sChanged.Count > 0)
            {
                string service_text = Mail_Handler.Create_Service_Mail(DashbConfig, sChanged);

                if (DashbConfig.MailEnabled)
                {
                    MWLogger.WriteLog("Mail settings enabled");

                    string mailMessage = DashbConfig.MailBody;
                    if (mailMessage.ToUpper().Contains("#SERVICETABLE#"))
                    {
                        mailMessage = mailMessage.Replace("#ServiceTable#", "<br>" + service_text + "<br>");
                    }
                    else
                        mailMessage += "<br>" + service_text + "<br>";

                    MWLogger.WriteLog("Error-Mailbody: " + Environment.NewLine + mailMessage);
                    Mail_Handler.Send_Mail(DashbConfig, service_text);
                }
                else
                    MWLogger.WriteLog("Mail settings not enabled");
            }
            else
                MWLogger.WriteLog("No changed services flagged");
                oldstartList = StartList;
            */



            MWLogger.WriteLog("Finished searching for Services " + StartList.Count.ToString());

            List<Services> complList = new List<Services>();

            foreach (S2Start serv in StartList)
            {
                Services service = new Services();

                service.ServiceHost = serv.ServerUPN;
                service.ServiceName = serv.ServiceName;
                service.ServicePath = serv.ServicePath;
                service.ServiceDescription = serv.ServiceDescription;
                service.Category = serv.Category;
                service.ServiceStatus = serv.ServiceStatus;
                service.ServiceDisplayName = serv.ServiceDisplayName;
                complList.Add(service);
            }

            MWLogger.WriteLog("List updated: " + complList.Count.ToString());
            return complList;
        }


        protected override void OnStop()
        {
            MWLogger.WriteLog("Service Luware Health Monitor Service stopped");
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
