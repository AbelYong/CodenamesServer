using DataAccess;
using DataAccess.Properties.Langs;
using Services.DTO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Services.Operations
{
    public static class EmailOperation
    {
        public static string GenerateSixDigitCode()
        {
            using (var code = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                code.GetBytes(bytes);
                uint n = BitConverter.ToUInt32(bytes, 0) % 1_000_000u;
                return n.ToString("D6");
            }
        }

        public static void SendVerificationEmail(string address, string code)
        {
            string subject = Lang.verifyEmailSubjectVerify;
            string body = string.Format(Lang.verifyEmailBodyVerify, code);
            SendEmail(address, subject, body);
        }

        public static void SendGameInvitationEmail(string fromUsername, string toAddress, string lobbyCode)
        {
            string subject = "Invitation to play Codenames";
            string body = string.Format("{0} has invited you to play a match, use the code {1} to join them", fromUsername, lobbyCode);
            SendEmail(toAddress, subject, body);
        }

        private static void SendEmail(string address, string subject, string body)
        {
            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(EmailConfig.fromAddr, EmailConfig.fromName);
                msg.To.Add(address);
                msg.Subject = subject;
                msg.Body = body;
                msg.IsBodyHtml = false;

                using (var smtp = new SmtpClient(EmailConfig.host, EmailConfig.port))
                {
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Credentials = new NetworkCredential(EmailConfig.user, EmailConfig.pass);
                    smtp.Send(msg);
                }
            }
        }

        private static class EmailConfig
        {
            public static readonly string fromAddr = ConfigurationManager.AppSettings["SmtpFromAddress"] ?? "no-reply@example.com";
            public static readonly string fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "Codenames";

            public static readonly string host = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            public static readonly int port = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p) ? p : 587;
            public static readonly string user = ConfigurationManager.AppSettings["SmtpUser"];
            public static readonly string pass = ConfigurationManager.AppSettings["SmtpPass"];
        }
    }
}
