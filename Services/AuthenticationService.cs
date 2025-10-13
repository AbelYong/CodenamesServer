using DataAccess.Users;
using DataAccess.Properties.Langs;
using Services.DTO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using DA = DataAccess;
using System.ServiceModel.Channels;

namespace Services
{
    public class AuthenticationService : IAuthenticationManager
    {
        private static IUserDAO _userDAO = new UserDAO();
        public Guid? Login(string username, string password)
        {
            return _userDAO.Login(username, password);
        }

        public Guid? SignIn(User svUser, Player svPlayer)
        {
            DataAccess.Player dbPlayer = Player.AssembleDbPlayer(svUser, svPlayer);
            string password = svUser.Password;

            return _userDAO.SignIn(dbPlayer, password);
        }

        public void BeginPasswordReset(string username, string email)
        {
            using (var db = new DA.codenamesEntities())
            {
                var userAgg = (from u in db.Users
                               join p in db.Players on u.userID equals p.userID
                               where p.username == username && u.email == email
                               select new { u.userID, u.email }).SingleOrDefault();

                if (userAgg == null) return;

                var code = GenerateCode6();

                var pr = new DA.PasswordReset
                {
                    resetID = Guid.NewGuid(),
                    userID = userAgg.userID,
                    codeHash = Sha512(code),
                    expiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                    used = false,
                    attempts = 0,
                    createdAt = DateTimeOffset.UtcNow
                };
                db.PasswordResets.Add(pr);
                db.SaveChanges();

                SendResetEmail(userAgg.email, code);
            }
        }

        public ResetResult CompletePasswordReset(string username, string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 10 || newPassword.Length > 16)
                return new ResetResult { Success = false, Message = Lang.resetPasswordLengthError };

            using (var db = new DA.codenamesEntities())
            {
                var u = (from usr in db.Users
                         join p in db.Players on usr.userID equals p.userID
                         where p.username == username
                         select usr).SingleOrDefault();

                if (u == null)
                    return new ResetResult { Success = false, Message = Lang.resetInvalidRequest };

                var now = DateTimeOffset.UtcNow;
                var req = db.PasswordResets
                            .Where(r => r.userID == u.userID && r.used == false && r.expiresAt > now)
                            .OrderByDescending(r => r.createdAt)
                            .FirstOrDefault();

                if (req == null)
                    return new ResetResult { Success = false, Message = Lang.resetCodeExpired };

                if (req.attempts >= 5)
                    return new ResetResult { Success = false, Message = Lang.resetTooManyAttempts };

                var ok = SlowEquals(req.codeHash, Sha512(code));
                if (!ok)
                {
                    req.attempts += 1;
                    db.SaveChanges();
                    return new ResetResult { Success = false, Message = Lang.resetIncorrectCode };
                }

                var newSalt = Guid.NewGuid();

                var pSalt = new SqlParameter("@salt", SqlDbType.UniqueIdentifier) { Value = newSalt };
                var pPwd = new SqlParameter("@pwd", SqlDbType.VarChar, 16) { Value = newPassword };
                var pUid = new SqlParameter("@uid", SqlDbType.UniqueIdentifier) { Value = u.userID };

                db.Database.ExecuteSqlCommand(@"
                UPDATE [dbo].[User]
                SET passwordSalt = @salt,
                    passwordHash = HASHBYTES('SHA2_512', @pwd + CAST(@salt AS VARCHAR(36)))
                WHERE userID = @uid", pSalt, pPwd, pUid);
                req.used = true;
                db.SaveChanges();

                return new ResetResult { Success = true, Message = Lang.resetPasswordUpdated };
            }
        }

        private static string GenerateCode6()
        {
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var b = new byte[4];
                rng.GetBytes(b);
                uint n = BitConverter.ToUInt32(b, 0) % 1_000_000u;
                return n.ToString("D6");
            }
        }

        private static byte[] Sha512(string input)
        {
            using (var sha = SHA512.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0; for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        private static void SendResetEmail(string to, string code)
        {
            var fromAddr = ConfigurationManager.AppSettings["SmtpFromAddress"] ?? "no-reply@example.com";
            var fromName = ConfigurationManager.AppSettings["SmtpFromName"] ?? "Codenames";

            var host = ConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
            int port = int.TryParse(ConfigurationManager.AppSettings["SmtpPort"], out var p) ? p : 587;
            bool ssl = bool.TryParse(ConfigurationManager.AppSettings["SmtpEnableSsl"], out var s) ? s : true;
            var user = ConfigurationManager.AppSettings["SmtpUser"];
            var pass = ConfigurationManager.AppSettings["SmtpPass"];

            using (var msg = new MailMessage())
            {
                msg.From = new MailAddress(fromAddr, fromName);
                msg.To.Add(to);
                msg.Subject = Lang.resetEmailSubject_Reset;
                msg.Body = string.Format(Lang.resetEmailBody_Reset, code);
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
