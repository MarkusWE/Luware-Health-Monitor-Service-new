using EASendMail;
using Luware_Health_Monitor_Service.Classes;
using mwsupport.Classes;
using MWSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luware_Health_Monitor_Service.support
{
        // https://github.com/iHouger/MailKit/tree/Abstract-logging
        internal class Mail_Support
        {
            public bool Send_Mail(Dashboard_Configuration dbConfig, string message)
            {
                PasswordEncoder mws = new PasswordEncoder();
                // MailMessage mail_message = new MailMessage();

                SmtpMail mail_message = new SmtpMail("TryIt");

                mail_message.From = dbConfig.MailUser; //  new MailAddress(dbConfig.MailUser);
                string[] receiver = dbConfig.MailReceiverAddress.Split(';');
                if (receiver.Length > 0)
                {
                    for (int i = 0; i < receiver.Length; i++)
                    {
                        MWLogger.WriteLog("Receipient (" + i.ToString() + "): " + receiver[i]);
                        mail_message.To = receiver[i]; /// Add(recevier[i]);//  new MailAddress(receiver[i]));
                    }
                }
                else
                {
                    MWLogger.WriteLog("Receipient: " + dbConfig.MailReceiverAddress);
                    mail_message.To = dbConfig.MailReceiverAddress;// .Add(recevier[i]);// new MailAddress(dbConfig.MailReceiverAddress));
                }

                mail_message.Subject = dbConfig.MailSubject;

                mail_message.Priority = EASendMail.MailPriority.High;
                mail_message.HtmlBody = message;
                // mail_message.TextBody = message;

                SmtpServer oServer = new SmtpServer(dbConfig.MailServer);
                oServer.Port = dbConfig.MailServerPort;

                switch (dbConfig.MailDecoding)
                {
                    case 0: // AutoSSL
                        oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;
                        break;
                    case 1: // StartTLS
                        oServer.ConnectType = SmtpConnectType.ConnectSTARTTLS;
                        break;
                    case 2: // AutoSSL
                    default:
                        oServer.ConnectType = SmtpConnectType.ConnectNormal;
                        break;
                    case 3: // AutoSSL
                        oServer.ConnectType = SmtpConnectType.ConnectDirectSSL;
                        break;
                }

                oServer.User = dbConfig.MailUser;
                oServer.Password = mws.DecryptWithByteArray(dbConfig.MailUserPassword);
                MWLogger.WriteLog("Mailconfiguration: " + dbConfig.MailServer + " " + dbConfig.MailServerPort.ToString() + " " + dbConfig.MailUser + " (" + dbConfig.MailDecoding.ToString() + ")");

                try
                {
                    SmtpClient oSmtp = new SmtpClient();
                    oSmtp.SendMail(oServer, mail_message);
                    MWLogger.WriteLog("Message send with: " + dbConfig.MailUser + " to: " + dbConfig.MailReceiverAddress);
                }
                catch (Exception ex)
                {
                    MWLogger.WriteLog("Error sending mail: " + ex.Message);
                    return false;
                }
                return true;

            }

        }
    }
