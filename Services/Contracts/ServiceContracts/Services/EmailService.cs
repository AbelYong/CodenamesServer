using System.ServiceModel;
using System.Runtime.Caching;
using Services.Operations;
using System;
using Services.DTO;
using System.Net.Mail;
using DataAccess.Properties.Langs;
using DataAccess.Users;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class EmailService : IEmailManager
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private const int VERICATION_TIMEOUT_MINUTES = 15;
        private const int MAX_ATTEMPTS = 3;

        public CommunicationRequest SendVerificationCode(string email)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (UserDAO.ValidateEmailNotDuplicated(email))
            {
                try
                {
                    string code = EmailOperation.GenerateSixDigitCode();
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

                    EmailOperation.SendVerificationEmail(email, code);
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.OK;
                }
                catch (Exception ex) when (ex is SmtpException || ex is SmtpFailedRecipientException)
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.SERVER_ERROR;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNALLOWED;
            }
            return request;
        }

        public ConfirmEmailRequest ValidateVerificationCode(string email, string code)
        {
            ConfirmEmailRequest request = new ConfirmEmailRequest();
            VerificationInfo info = _cache.Get(email) as VerificationInfo;
            if (info != null && info.RemainingAttempts > 0)
            {
                if (info.Code == code)
                {
                    _cache.Remove(email);
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.OK;
                }
                else
                {
                    info.RemainingAttempts--;
                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = info.ExpirationTime
                    };
                    _cache.Set(email, info, policy);
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                    request.RemainingAttempts = info.RemainingAttempts;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.NOT_FOUND;
            }
            return request;
        }

        private sealed class VerificationInfo
        {
            public string Code { get; set; }
            public int RemainingAttempts { get; set; }
            public DateTimeOffset ExpirationTime { get; set; } 
        }
    }
}
