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
using System.Security.Cryptography;
using System.Text;
using DA = DataAccess;
using Services.Operations;
using Services.DTO.Request;
using System.Data.Entity.Core;
using Services.Contracts.ServiceContracts.Managers;

namespace Services.Contracts.ServiceContracts.Services
{
    public class AuthenticationService : IAuthenticationManager
    {
        private static IUserDAO _userDAO = new UserDAO();
        public LoginRequest Login(string username, string password)
        {
            LoginRequest request = new LoginRequest();
            try
            {
                Guid? userID = _userDAO.Authenticate(username, password);
                if (userID != null)
                {
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.OK;
                    request.UserID = userID;
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                }
            }
            catch (Exception ex) when (ex is SqlException || ex is EntityException)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.SERVER_ERROR;
            }
            return request;
        }

        public Guid? SignIn(User svUser, Player svPlayer)
        {
            svPlayer.Name = string.IsNullOrWhiteSpace(svPlayer.Name) ? null : svPlayer.Name.Trim();
            svPlayer.LastName = string.IsNullOrWhiteSpace(svPlayer.LastName) ? null : svPlayer.LastName.Trim();

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

                string code = EmailOperation.GenerateSixDigitCode();

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
                    return new ResetResult { Success = false, Message = Lang.globalTooManyAttempts };

                var ok = SlowEquals(req.codeHash, Sha512(code));
                if (!ok)
                {
                    req.attempts += 1;
                    db.SaveChanges();
                    return new ResetResult { Success = false, Message = Lang.globalIncorrectCode };
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

        private static byte[] ComputePasswordHashInSql(DA.codenamesEntities db, string password, Guid salt)
        {
            var pPwd = new SqlParameter("@pwd", SqlDbType.VarChar, 16) { Value = password };
            var pSalt = new SqlParameter("@salt", SqlDbType.UniqueIdentifier) { Value = salt };

            // Devuelve varbinary(64)
            return db.Database.SqlQuery<byte[]>(
                "SELECT HASHBYTES('SHA2_512', @pwd + CAST(@salt AS VARCHAR(36)))", pPwd, pSalt
            ).Single();
        }

        public BeginRegistrationResult BeginRegistration(User svUser, Player svPlayer, string plainPassword)
        {
            var pwd = string.IsNullOrEmpty(plainPassword) ? svUser?.Password : plainPassword;

            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 10 || pwd.Length > 16)
                return new BeginRegistrationResult { Success = false, Message = Lang.resetPasswordLengthError };

            svUser.Email = svUser?.Email?.Trim();
            svPlayer.Username = svPlayer?.Username?.Trim();

            var cleanName = string.IsNullOrWhiteSpace(svPlayer?.Name) ? null : svPlayer.Name.Trim();
            var cleanLastName = string.IsNullOrWhiteSpace(svPlayer?.LastName) ? null : svPlayer.LastName.Trim();

            if (string.IsNullOrWhiteSpace(svUser?.Email) || string.IsNullOrWhiteSpace(svPlayer?.Username))
                return new BeginRegistrationResult { Success = false, Message = Lang.verifyRequiredFieldsMissing };

            using (var db = new DA.codenamesEntities())
            {
                bool emailTaken = db.Users.Any(u => u.email == svUser.Email);
                bool userTaken = db.Players.Any(p => p.username == svPlayer.Username);
                if (emailTaken || userTaken)
                    return new BeginRegistrationResult { Success = false, Message = Lang.verifyEmailOrUserInUse };

                var now = DateTimeOffset.UtcNow;
                bool hasActiveEmail = db.RegistrationRequests.Any(r => r.email == svUser.Email && !r.used && r.expiresAt > now);
                bool hasActiveUser = db.RegistrationRequests.Any(r => r.username == svPlayer.Username && !r.used && r.expiresAt > now);
                if (hasActiveEmail || hasActiveUser)
                    return new BeginRegistrationResult { Success = false, Message = Lang.verifyAlreadyActiveRequest };

                var salt = Guid.NewGuid();
                var passHash = ComputePasswordHashInSql(db, pwd, salt);

                string code = EmailOperation.GenerateSixDigitCode();
                var codeHash = Sha512(code);

                var req = new DA.RegistrationRequest
                {
                    requestID = Guid.NewGuid(),
                    email = svUser.Email,
                    username = svPlayer.Username,
                    name = cleanName,
                    lastName = cleanLastName,
                    passwordSalt = salt,
                    passwordHash = passHash,
                    codeHash = codeHash,
                    attempts = 0,
                    used = false,
                    createdAt = now,
                    expiresAt = now.AddMinutes(15)
                };

                db.RegistrationRequests.Add(req);
                db.SaveChanges();

                EmailOperation.SendVerificationEmail(svUser.Email, code);

                return new BeginRegistrationResult
                {
                    Success = true,
                    Message = Lang.verifyVerificationCodeSent,
                    RequestId = req.requestID
                };
            }
        }

        public ConfirmRegistrationResult ConfirmRegistration(Guid requestId, string code)
        {
            if (requestId == Guid.Empty || string.IsNullOrWhiteSpace(code))
                return new ConfirmRegistrationResult { Success = false, Message = Lang.globalInvalidData };

            using (var db = new DA.codenamesEntities())
            {
                var now = DateTimeOffset.UtcNow;

                var req = db.RegistrationRequests
                            .SingleOrDefault(r => r.requestID == requestId && !r.used && r.expiresAt > now);

                if (req == null)
                    return new ConfirmRegistrationResult { Success = false, Message = Lang.verifyInvalidOrExpiredRequest };

                if (req.attempts >= 5)
                    return new ConfirmRegistrationResult { Success = false, Message = Lang.globalTooManyAttempts };

                if (!SlowEquals(req.codeHash, Sha512(code)))
                {
                    req.attempts = (byte)Math.Min(255, req.attempts + 1);
                    db.SaveChanges();
                    return new ConfirmRegistrationResult { Success = false, Message = Lang.globalIncorrectCode };
                }

                using (var tx = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (db.Users.Any(u => u.email == req.email) || db.Players.Any(p => p.username == req.username))
                        {
                            req.used = true;
                            db.SaveChanges();
                            tx.Commit();
                            return new ConfirmRegistrationResult { Success = false, Message = Lang.verifyEmailOrUserInUse };
                        }

                        var newUser = new DA.User
                        {
                            userID = Guid.NewGuid(),
                            email = req.email,
                            passwordSalt = req.passwordSalt,
                            passwordHash = req.passwordHash
                        };
                        db.Users.Add(newUser);
                        db.SaveChanges();

                        var newPlayer = new DA.Player
                        {
                            playerID = Guid.NewGuid(),
                            userID = newUser.userID,
                            username = req.username,
                            name = req.name,
                            lastName = req.lastName
                        };
                        db.Players.Add(newPlayer);

                        req.used = true;
                        db.SaveChanges();

                        tx.Commit();

                        return new ConfirmRegistrationResult
                        {
                            Success = true,
                            Message = Lang.verifyAccountCreated,
                            UserId = newUser.userID
                        };
                    }
                    catch
                    {
                        tx.Rollback();
                        return new ConfirmRegistrationResult { Success = false, Message = Lang.verifyAccountCouldNotBeCreated };
                    }
                }
            }
        }

        public void CancelRegistration(Guid requestId)
        {
            if (requestId == Guid.Empty) return;

            using (var db = new DA.codenamesEntities())
            {
                var req = db.RegistrationRequests.SingleOrDefault(r => r.requestID == requestId && !r.used);
                if (req != null)
                {
                    req.used = true;
                    db.SaveChanges();
                }
            }
        }
    }
}
