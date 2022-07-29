using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luware_Health_Monitor_Service.Classes
{
    public class EventViewer
    {
        public string HostName { get; set; }
        public string ServiceName { get; set; }
        public string Directory { get; set; }
        public string ServiceDescription { get; set; }
        public DateTime LastAccessTime { get; set; }
        public double ServiceStatus { get; set; }


        public EventViewer()
        {
            ServiceName = string.Empty;
            HostName = string.Empty;
            Directory = string.Empty;
            LastAccessTime = new DateTime(2015, 01, 01);
            ServiceDescription = string.Empty;
            ServiceStatus = 0;
        }

    }
}
