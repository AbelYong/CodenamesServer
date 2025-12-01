using DataAccess.DataRequests;
using DataAccess.Properties.Langs;
using DataAccess.Users;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO;
using Services.DTO.Request;
using Services.Operations;
using System;
using System.Data;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Services.Contracts.ServiceContracts.Services
{
    public class AuthenticationService : IAuthenticationManager
    {
        private readonly IUserDAO _userDAO;
        private readonly IBanDAO _banDAO;
        private readonly IEmailOperation _emailOperation;

        public AuthenticationService() : this(new UserDAO(), new BanDAO(), new EmailOperation()) { }

        public AuthenticationService(IUserDAO userDAO, IBanDAO banDAO, IEmailOperation emailOperation)
        {
            _userDAO = userDAO;
            _banDAO = banDAO;
            _emailOperation = emailOperation;
        }

        public LoginRequest Login(string username, string password)
        {
            LoginRequest request = new LoginRequest();
            try
            {
                Guid? userID = _userDAO.Authenticate(username, password);

                if (userID != null)
                {
                    var activeBan = _banDAO.GetActiveBan(userID.Value);

                    if (activeBan != null)
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.ACCOUNT_BANNED;
                        return request;
                    }

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
            catch (Exception ex) when (ex is EntityException || ex is DbUpdateException || ex is SqlException)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.SERVER_ERROR;
                ServerLogger.Log.Warn("Exception while tryining to authenticate an user: ", ex);
            }
            catch (Exception ex)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.SERVER_ERROR;
                ServerLogger.Log.Error("Unexpected exception while trying to authenticate an user: ", ex);
            }
            return request;
        }

        public void BeginPasswordReset(string username, string email)
        {
            using (var db = new DataAccess.codenamesEntities())
            {
                var userAgg = (from u in db.Users
                               join p in db.Players on u.userID equals p.userID
                               where p.username == username && u.email == email
                               select new { u.userID, u.email }).SingleOrDefault();

                if (userAgg == null) return;

                string code = _emailOperation.GenerateSixDigitCode();

                var pr = new DataAccess.PasswordReset
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

                EmailOperation.SendResetEmail(userAgg.email, code);
            }
        }

        public ResetResult CompletePasswordReset(string username, string code, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 10 || newPassword.Length > 16)
            {
                return new ResetResult { Success = false, Message = Lang.resetPasswordLengthError };
            }

            using (var db = new DataAccess.codenamesEntities())
            {
                var u = (from usr in db.Users
                         join p in db.Players on usr.userID equals p.userID
                         where p.username == username
                         select usr).SingleOrDefault();

                if (u == null)
                {
                    return new ResetResult { Success = false, Message = Lang.resetInvalidRequest };
                }

                var now = DateTimeOffset.UtcNow;
                var req = db.PasswordResets
                            .Where(r => r.userID == u.userID && !r.used && r.expiresAt > now)
                            .OrderByDescending(r => r.createdAt)
                            .FirstOrDefault();

                if (req == null)
                {
                    return new ResetResult { Success = false, Message = Lang.resetCodeExpired };
                }

                if (req.attempts >= 5)
                {
                    return new ResetResult { Success = false, Message = Lang.globalTooManyAttempts };
                }

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
    }
}
