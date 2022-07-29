using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luware_Health_Monitor_Service.Classes
{
    public class LogSystemEvents
    {
        public int Id { get; set; }
        public byte Level { get; set; }

        public string ProviderName { get; set; }
        public string LogNameDirectory { get; set; }
        public string Machine { get; set; }
        public DateTime TimeCreated { get; set; }
        public string LevelDisplayName { get; set; }
        public string ErrorMessage { get; set; }

    }
}

