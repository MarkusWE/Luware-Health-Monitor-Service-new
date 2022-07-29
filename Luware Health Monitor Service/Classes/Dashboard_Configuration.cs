using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Luware_Health_Monitor_Service.Classes
{
    public class Dashboard_Configuration
    {
        [XmlElement("StandardServerConfig")]
        public string StandardServerConfig { get; set; }


        [XmlElement("RefreshIntervall")]
        public int RefreshInterval { get; set; }

        [XmlElement("MailEnabled")]
        public bool MailEnabled { get; set; }

        [XmlElement("MailServer")]
        public string MailServer { get; set; }

        [XmlElement("MailServerPort")]
        public int MailServerPort { get; set; }
        [XmlElement("MailSenderAddress")]
        public string MailSenderAddress { get; set; }
        [XmlElement("MailReceiverAddress")]
        public string MailReceiverAddress { get; set; }

        [XmlElement("MailUser")]
        public string MailUser { get; set; }

        [XmlElement("MailDecoding")]
        public int MailDecoding { get; set; }

        [XmlElement("MailUserPassword")]
        public string MailUserPassword { get; set; }

        [XmlElement("MailBody")]
        public string MailBody { get; set; }


        [XmlElement("MailSubject")]
        public string MailSubject { get; set; }
        public Dashboard_Configuration()
        {
            StandardServerConfig = string.Empty;
            RefreshInterval = 0;
            MailEnabled = false;
            MailServer = string.Empty;
            MailServerPort = 0;
            MailSenderAddress = string.Empty;
            MailReceiverAddress = string.Empty;
            MailDecoding = 0;
            MailSubject = string.Empty;
        }
    }
}

