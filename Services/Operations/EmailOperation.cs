using DataAccess.Properties.Langs;
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
            var fromAddr = ConfigurationManager.AppSettings["SmtpFromAddress"] ?? "no-reply@example.com";
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "Codenames";

            var host = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            int port = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p) ? p : 587;
            bool ssl = !bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out var s) || s;
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPass"];

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(fromAddr, fromName);
                msg.To.Add(address);
                msg.Subject = Lang.verifyEmailSubjectVerify;
                msg.Body = string.Format(Lang.verifyEmailBodyVerify, code);
                msg.IsBodyHtml = false;

                using (var smtp = new SmtpClient(host, port))
                {
                    smtp.EnableSsl = ssl;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Credentials = new NetworkCredential(user, pass);
                    smtp.Send(msg);
                }
            }
        }
    }
}
