using System.ServiceModel;
using System.Runtime.Caching;
using Services.Operations;
using System;
using DataAccess.Users;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.Request;
using Services.DTO.DataContract;
using DataAccess.DataRequests;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class EmailService : IEmailManager
    {
        private readonly IPlayerRepository _playerRepository;
        private readonly IEmailOperation _emailOperation;
        private static readonly MemoryCache _emailVerificationCache = MemoryCache.Default;
        private static readonly MemoryCache _passwordResetCache = MemoryCache.Default;
        private const int VERICATION_TIMEOUT_MINUTES = 15;
        private const int MAX_ATTEMPTS = 3;

        public EmailService() : this (new PlayerRepository(), new EmailOperation()) { }

        public EmailService(IPlayerRepository playerRepository, IEmailOperation emailOperation)
        {
            _playerRepository = playerRepository;
            _emailOperation = emailOperation;
        }

        public CommunicationRequest SendVerificationCode(string email, EmailType emailType)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (!_emailOperation.ValidateEmailFormat(email))
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.WRONG_DATA;
                return request;
            }

            DataVerificationRequest verification = _playerRepository.VerifyEmailInUse(email);
            if (verification.ErrorType == ErrorType.DB_ERROR)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.DATABASE_ERROR;
                return request;
            }
            switch (emailType)
            {
                case EmailType.EMAIL_VERIFICATION:
                    if (!verification.IsSuccess)
                    {
                        request = SendEmail(email, emailType);
                    }
                    else
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.UNALLOWED;
                    }
                    break;
                case EmailType.PASSWORD_RESET:
                    if (!verification.IsSuccess)
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.NOT_FOUND;
                    }
                    else
                    {
                        request = SendEmail(email, emailType);
                    }
                    break;
            }
            return request;
        }
        private CommunicationRequest SendEmail(string email, EmailType emailType)
        {
            CommunicationRequest request = new CommunicationRequest();
            
            string code = _emailOperation.GenerateSixDigitCode();
            SetNewVerificationInfo(email, code, emailType);

            bool wasEmailSent = _emailOperation.SendVerificationEmail(email, code, emailType);
            request.IsSuccess = wasEmailSent;
            request.StatusCode = wasEmailSent ? StatusCode.OK : StatusCode.SERVER_ERROR;
            return request;
        }

        private void SetNewVerificationInfo(string email, string code, EmailType emailType)
        {
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

            switch (emailType)
            {
                case EmailType.EMAIL_VERIFICATION:
                    _emailVerificationCache.Set(email, info, policy);
                    break;
                case EmailType.PASSWORD_RESET:
                    _passwordResetCache.Set(email, info, policy);
                    break;
            }
        }

        public ConfirmEmailRequest ValidateVerificationCode(string email, string code, EmailType emailType)
        {
            ConfirmEmailRequest request = new ConfirmEmailRequest();
            VerificationInfo info = GetVerificationInfo(email, emailType);
            if (info == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.NOT_FOUND;
                return request;
            }

            if (info.RemainingAttempts > 0)
            {
                if (info.Code == code)
                {
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.OK;
                }
                else
                {
                    UpdateVerificationInfo(email, info, emailType);
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                    request.RemainingAttempts = info.RemainingAttempts;
                    if (info.RemainingAttempts == 0)
                    {
                        ServerLogger.Log.WarnFormat("Too many attempts for the email: {0}, Type: {1}", email, emailType);
                        RemoveVerificationInfo(email, emailType);
                    }
                }
            }
            else
            {
                RemoveVerificationInfo(email, emailType);
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNAUTHORIZED;
                request.RemainingAttempts = info.RemainingAttempts;
            }
            return request;
        }

        private static VerificationInfo GetVerificationInfo(string email, EmailType emailType)
        {
            VerificationInfo info = null;
            switch (emailType)
            {
                case EmailType.EMAIL_VERIFICATION:
                    info = _emailVerificationCache.Get(email) as VerificationInfo;
                    break;
                case EmailType.PASSWORD_RESET:
                    info = _passwordResetCache.Get(email) as VerificationInfo;
                    break;
            }
            return info;
        }

        private static void RemoveVerificationInfo(string email, EmailType emailType)
        {
            switch (emailType)
            {
                case EmailType.EMAIL_VERIFICATION:
                    _emailVerificationCache.Remove(email);
                    break;
                case EmailType.PASSWORD_RESET:
                    _passwordResetCache.Remove(email);
                    break;
            }
        }

        private static void UpdateVerificationInfo(string email, VerificationInfo info, EmailType emailType)
        {
            info.RemainingAttempts--;
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = info.ExpirationTime
            };
            switch (emailType)
            {
                case EmailType.EMAIL_VERIFICATION:
                    _emailVerificationCache.Set(email, info, policy);
                    break;
                case EmailType.PASSWORD_RESET:
                    _passwordResetCache.Set(email, info, policy);
                    break;
            }
        }

        public void DeleteVerificationCode(string email, EmailType emailType)
        {
            RemoveVerificationInfo(email, emailType);
        }
        private sealed class VerificationInfo
        {
            public string Code { get; set; }
            public int RemainingAttempts { get; set; }
            public DateTimeOffset ExpirationTime { get; set; } 
        }
    }
}
