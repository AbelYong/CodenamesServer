using DataAccess.Properties.Langs;
using Services.DTO.DataContract;
using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Services.Operations
{
    public class EmailOperation : IEmailOperation
    {
        private static readonly Regex _gmailRegex =
            new Regex(@"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@gmail\.com$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex _outlookRegex =
            new Regex(@"^[A-Za-z0-9.!#$%&'*+/=?^_`{|}~-]+@outlook\.com$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex _uvEstudiantesMxRegex =
            new Regex(@"^zS\d{8}@estudiantes\.uv\.mx$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        public string GenerateSixDigitCode()
        {
            using (var code = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                code.GetBytes(bytes);
                uint n = BitConverter.ToUInt32(bytes, 0) % 1_000_000u;
                return n.ToString("D6");
            }
        }

        public bool SendVerificationEmail(string toAddress, string code, EmailType type)
        {
            if (ValidateEmailFormat(toAddress))
            {
                string subject = type == EmailType.EMAIL_VERIFICATION ? Lang.verifyEmailSubjectVerify : Lang.resetEmailSubject_Reset;
                string body = type == EmailType.EMAIL_VERIFICATION ? Lang.verifyEmailBodyVerify : Lang.resetEmailBody_Reset;
                body = string.Format(body, code);
                return SendEmail(toAddress, subject, body);
            }
            return false;
        }

        public bool SendGameInvitationEmail(string fromUsername, string toAddress, string lobbyCode)
        {
            if (ValidateEmailFormat(toAddress))
            {
                string subject = "Invitation to play Codenames";
                string body = string.Format("{0} has invited you to play a match, use the code {1} to join them", fromUsername, lobbyCode);
                return SendEmail(toAddress, subject, body);
            }
            return false;
        }

        private static bool SendEmail(string address, string subject, string body)
        {
            using (var msg = new MailMessage())
            {
                try
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
                        return true;
                    }
                }
                catch (Exception ex) when (ex is SmtpException || ex is SmtpFailedRecipientException)
                {
                    ServerLogger.Log.Warn("Exception while trying to send an email: ", ex);
                }
                catch (Exception ex) when (ex is FormatException || ex is ArgumentNullException || ex is ArgumentException)
                {
                    ServerLogger.Log.Debug("Argument exception while trying to send an email: ", ex);
                }
                catch (Exception ex)
                {
                    ServerLogger.Log.Error("Unexpected exception while trying to send an email: ", ex);
                }
                return false;
            }
        }

        public bool ValidateEmailFormat(string email)
        {
            return (_gmailRegex.IsMatch(email) || _outlookRegex.IsMatch(email) || _uvEstudiantesMxRegex.IsMatch(email));
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
