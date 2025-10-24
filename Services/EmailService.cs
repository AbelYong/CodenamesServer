using System.ServiceModel;
using System.Runtime.Caching;
using Services.Operations;
using System;
using Services.DTO;
using System.Net.Mail;
using DataAccess.Properties.Langs;
using DataAccess.Users;

namespace Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class EmailService : IEmailManager
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private const int VERICATION_TIMEOUT_MINUTES = 15;
        private const int MAX_ATTEMPTS = 3;

        public RequestResult SendVerificationCode(string email)
        {
            RequestResult result = new RequestResult();
            if (UserDAO.ValidateEmailNotDuplicated(email))
            {
                try
                {
                    string code = EmailOperations.GenerateCode();
                    DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(VERICATION_TIMEOUT_MINUTES);

                    var info = new VerificationInfo
                    {
                        Code = code,
                        RemainingAttempts = MAX_ATTEMPTS,
                        ExpirationTime = expiration
                    };

                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = expiration
                    };
                    _cache.Set(email, info, policy);

                    EmailOperations.SendVerificationEmail(email, code);
                    result.IsSuccess = true;
                }
                catch (Exception ex) when (ex is SmtpException || ex is SmtpFailedRecipientException)
                {
                    result.IsSuccess = false;
                    result.Message = Lang.verifyEmailSubjectVerify;
                }
            }
            else
            {
                result.IsSuccess = false;
                result.Message = Lang.errorEmailAddressInUse;
            }
            return result;
        }

        public RequestResult ValidateVerificationCode(string email, string code)
        {
            RequestResult result = new RequestResult();
            var info = _cache.Get(email) as VerificationInfo;
            if (info != null && info.RemainingAttempts > 0)
            {
                if (info.Code == code)
                {
                    _cache.Remove(email);
                    result.IsSuccess = true;
                }
                else
                {
                    info.RemainingAttempts--;
                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = info.ExpirationTime
                    };
                    _cache.Set(email, info, policy);
                    result.IsSuccess = false;
                    result.Message = SetResultMessage(info.RemainingAttempts);
                }
            }
            else
            {
                result.IsSuccess = false;
                result.Message = Lang.resetCodeExpired;
            }
            return result;
        }

        private static string SetResultMessage(int remainingAttempts)
        {
            if (remainingAttempts > 0)
            {
                return string.Format(Lang.verifyRemainingAttempts, remainingAttempts);
            }
            else
            {
                return Lang.globalTooManyAttempts;
            }
        }

        private sealed class VerificationInfo
        {
            public string Code { get; set; }
            public int RemainingAttempts { get; set; }
            public DateTimeOffset ExpirationTime { get; set; } 
        }
    }
}
