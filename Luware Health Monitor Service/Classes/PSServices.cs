using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luware_Health_Monitor_Service.Classes
{
    public class PSServices
    {
        public string ServiceHost { get; set; }
        public string ServiceName { get; set; }
        public string ServicePath { get; set; }
        public string ConnectionString { get; set; }
        public string Status { get; set; }


        public PSServices()
        {
            ServiceHost = string.Empty;
            ServiceName = string.Empty;
            ServicePath = string.Empty;
            Status = string.Empty;
            ConnectionString = string.Empty;
        }

    }
}
