using MWSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Luware_Health_Monitor_Service.Classes;
using System.Windows.Forms;
using System.Xml.Serialization;
using mwsupport.Classes;
using System.Diagnostics.Eventing.Reader;

namespace Luware_Health_Monitor_Service.support
{
    public class Service_Support
    {
        public readonly string _cfgFileName = "Luware Health Monitor.xml";
        public string Get_Config_File(bool exists)
        {
            string cfgFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Luware AG\\Luware Health Monitor Service\\" + _cfgFileName);

            MWLogger.WriteLog("CFG-File: " + cfgFile);

            if (!exists)
            {
                if (cfgFile != string.Empty)
                    if (!File.Exists(cfgFile))
                        return string.Empty;
            }

            cfgFile = Get_Config_File(cfgFile);

            return cfgFile;
        }
        public string Get_Config_File(string cfgFile)
        {
            // string cfgFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Luware AG\\Luware Health  Monitor Service\\" + _cfgFileName);

            MWLogger.WriteLog("CFG-File: " + cfgFile);

            if (!File.Exists(cfgFile))
            {

                cfgFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Luware AG\\Luware Health Monitor Service\\" + _cfgFileName);
/*                if (!File.Exists(cfgFile))
                {
                    OpenFileDialog ofd = new OpenFileDialog()
                    {
                        DefaultExt = "xml",
                        Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*",
                        Title = "Select Configuration File",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        CheckFileExists = true
                    };
                    DialogResult ofdResult = ofd.ShowDialog();
                    if (ofdResult == DialogResult.OK)
                    {
                        cfgFile = ofd.FileName;
                        if (File.Exists(cfgFile))
                        {
                            Properties.Settings.Default.F1ConfigFile = cfgFile;
                            Properties.Settings.Default.Save();
                        }
                    }
                    else
                        return string.Empty;
                } */
            }
            return cfgFile;

        }
        public Dashboard_Configuration Save_xmlFile(string FileName, Dashboard_Configuration dashbConf)
        {
            Dashboard_Configuration DConf = new Dashboard_Configuration();

            try
            {

                using (var writer = new FileStream(FileName, FileMode.Create))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Dashboard_Configuration), new XmlRootAttribute("Dashboard_Configuration"));
                    ser.Serialize(writer, dashbConf);
                }

            }
            catch (Exception ex)
            {
                MWLogger.WriteLog("Error saving XML File: " + ex.Message);
            }

            return DConf;
        }
        public Dashboard_Configuration Load_XmlFile(string FileName)
        {
            Dashboard_Configuration DConf = new Dashboard_Configuration();

            try
            {
                using (var reader = new StreamReader(FileName))
                {
                    XmlSerializer deserialize = new XmlSerializer(typeof(Dashboard_Configuration), new XmlRootAttribute("Dashboard_Configuration"));
                    var stList = (Dashboard_Configuration)deserialize.Deserialize(reader);
                    DConf = stList;
                }
            }
            catch (Exception ex)
            {
                MWLogger.WriteLog("Error loading config: " + ex.Message);
            }

            return DConf;
        }

        public List<EventViewer> Get_EventViewer_per_Server(List<EventViewer> ev, string Host)
        {
            try
            {
                DirectoryInfo d;
                if (Host == string.Empty)
                    d = new DirectoryInfo("c:\\windows\\Sysnative\\winevt\\logs");
                else
                    d = new DirectoryInfo("\\\\" + Host + "\\c$\\windows\\System32\\winevt\\logs");

                FileInfo[] Files = d.GetFiles("*.evt");

                foreach (FileInfo file in Files)
                {

                    if ((file.Name.ToUpper().Contains("LUWARE")) || (file.Name.ToUpper().Contains("Skype")))
                    {
                        EventViewer fileName = new EventViewer()
                        {
                            ServiceName = file.Name,
                            Directory = Path.Combine("c:\\windows\\system32\\winevt\\logs", file.Name),
                            LastAccessTime = file.LastAccessTime,
                            HostName = Host
                        };

                        MWLogger.WriteLog("Get_EventViewer: " + fileName.HostName + " " + fileName.Directory + " " + fileName.LastAccessTime.ToString());
                        ev.Add(fileName);
                    }

                }

            }
            catch (Exception ex)
            {
                MWLogger.WriteLog("Fehler bei Eventviewer: " + ex.Message);
            }

            return ev;
        }
        public List<EventViewer> Get_EventViewer(List<Server> ServerList)
        {
            MWLogger.WriteLog("Start scanning EventViewer Logs");
            List<EventViewer> ev = new List<EventViewer>();

            if (ServerList.Count > 0)
            {
                foreach (Server s in ServerList)
                {
                    ev = Get_EventViewer_per_Server(ev, s.HostName);
                }
            }
            else
            {
                ev = Get_EventViewer_per_Server(ev, string.Empty);
            }

            return ev;
        }
        public List<LogSystemEvents> Get_Events(List<EventViewer> evs)
        {
            // List<EventRecord> evRecs = new List<EventRecord>();
            List<LogSystemEvents> sysEvents = new List<LogSystemEvents>();

            MWLogger.WriteLog("Start scanning Eventviewer...");

            foreach (EventViewer ev in evs)
            {
                // https://codewala.net/2013/10/04/reading-event-logs-efficiently-using-c/
                string query = "*[System/Level=2 and System/TimeCreated/@SystemTime >= '" + DateTime.Now.AddDays(-1).ToUniversalTime().ToString("o") + "' and System/TimeCreated/@SystemTime <= '" + DateTime.Now.ToUniversalTime().ToString("o") + "']";

                MWLogger.WriteLog("Get_Events: " + ev.HostName + "  " + ev.ServiceName + "  " + query);
                EventLogQuery eventsQuery = new EventLogQuery(ev.Directory, PathType.FilePath, query);

                try
                {
                    EventLogReader logReader;
                    if (ev.HostName != string.Empty)
                    {
                        // https://stackoverflow.com/questions/7966993/eventlogquery-reader-for-remote-computer

                        EventLogSession session = new EventLogSession(
                            ev.HostName,
                            null, null, null,
                            SessionAuthentication.Default);

                        eventsQuery.Session = session;
                        logReader = new EventLogReader(eventsQuery);
                    }
                    else
                        logReader = new EventLogReader(eventsQuery);


                    for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                    {

                        // evRecs.Add(eventdetail);
                        // MWLogger.WriteLog("Log: " + ev.ServiceName + " " + eventdetail.Level.ToString() + "  " + eventdetail.OpcodeDisplayName.ToString());
                        LogSystemEvents lEvent = new LogSystemEvents()
                        {
                            Level = (byte)eventdetail.Level,
                            Machine = eventdetail.MachineName,
                            ProviderName = eventdetail.ProviderName,
                            Id = eventdetail.Id,
                            TimeCreated = (DateTime)eventdetail.TimeCreated,
                            LevelDisplayName = eventdetail.LevelDisplayName,
                            LogNameDirectory = eventdetail.LogName
                        };
                        foreach (EventProperty prop in eventdetail.Properties)
                        {
                            lEvent.ErrorMessage += prop.Value;
                            // MWLogger.WriteLog(prop.Value.ToString());
                        }

                        sysEvents.Add(lEvent);
                        // MWLogger.WriteLog("Events added: " + lEvent.Id + " - " + lEvent.Machine);
                    }
                }
                catch (Exception ex)
                {
                    MWLogger.WriteLog("Log: " + ev.ServiceName + " " + ex.Message);
                }
            }

            return (sysEvents);
        }
    }
}
