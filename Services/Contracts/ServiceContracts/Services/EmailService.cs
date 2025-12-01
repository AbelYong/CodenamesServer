using System.ServiceModel;
using System.Runtime.Caching;
using Services.Operations;
using System;
using Services.DTO;
using DataAccess.Users;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.Request;

namespace Services.Contracts.ServiceContracts.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class EmailService : IEmailManager
    {
        private readonly IPlayerDAO _playerDAO;
        private readonly IEmailOperation _emailOperation;
        private static readonly MemoryCache _cache = MemoryCache.Default;
        private const int VERICATION_TIMEOUT_MINUTES = 15;
        private const int MAX_ATTEMPTS = 3;

        public EmailService() : this (new PlayerDAO(), new EmailOperation()) { }

        public EmailService(IPlayerDAO playerDAO, IEmailOperation emailOperation)
        {
            _playerDAO = playerDAO;
            _emailOperation = emailOperation;
        }

        public CommunicationRequest SendVerificationCode(string email)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (!EmailOperation.ValidateEmailFormat(email))
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.WRONG_DATA;
                return request;
            }

            if (_playerDAO.ValidateEmailNotDuplicated(email))
            {
                string code = _emailOperation.GenerateSixDigitCode();
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

                bool wasEmailSent = _emailOperation.SendVerificationEmail(email, code);
                request.IsSuccess = wasEmailSent;
                request.StatusCode = wasEmailSent ? StatusCode.OK : StatusCode.SERVER_ERROR;
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
