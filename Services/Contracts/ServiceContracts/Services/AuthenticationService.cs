using DataAccess.DataRequests;
using DataAccess.Users;
using DataAccess.Moderation;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.Request;
using Services.DTO.DataContract;
using Services.Operations;
using System;
using System.Data.Entity.Core;
using System.Data.SqlClient;

namespace Services.Contracts.ServiceContracts.Services
{
    public class AuthenticationService : IAuthenticationManager
    {
        private readonly IUserRepository _userRepository;
        private readonly IBanDAO _banDAO;
        private readonly IEmailManager _emailManager;

        public AuthenticationService() : this(new UserRepository(), new BanDAO(), new EmailService()) { }

        public AuthenticationService(IUserRepository userDAO, IBanDAO banDAO, IEmailManager emailManager)
        {
            _userRepository = userDAO;
            _banDAO = banDAO;
            _emailManager = emailManager;
        }

        public AuthenticationRequest Authenticate(string username, string password)
        {
            AuthenticationRequest request = new AuthenticationRequest();
            try
            {
                Guid? userID = _userRepository.Authenticate(username, password);

                if (userID != null)
                {
                    var activeBan = _banDAO.GetActiveBan(userID.Value);

                    if (activeBan != null)
                    {
                        request.IsSuccess = false;
                        request.StatusCode = StatusCode.ACCOUNT_BANNED;
                        request.BanExpiration = activeBan.timeout;
                        
                    }
                    else
                    {
                        request.IsSuccess = true;
                        request.StatusCode = StatusCode.OK;
                        request.UserID = userID;
                    }

                    string audit = string.Format("Authentication procesed with code {0} for user: {1}", request.StatusCode, userID);
                    ServerLogger.Log.Info(audit);
                    return request;
                }
                else
                {
                    request.IsSuccess = false;
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                    string audit = string.Format("Authentication failed for username: {0}", username);
                    ServerLogger.Log.Info(audit);
                }
            }
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.DATABASE_ERROR;
                ServerLogger.Log.Warn("Exception while tryining to authenticate an user: ", ex);
            }
            catch (Exception ex)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.DATABASE_ERROR;
                ServerLogger.Log.Error("Unexpected exception while trying to authenticate an user: ", ex);
            }
            return request;
        }

        public PasswordResetRequest CompletePasswordReset(string email, string code, string newPassword)
        {
            PasswordResetRequest request = new PasswordResetRequest();

            ConfirmEmailRequest emailConfirmation = _emailManager.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET);
            if (emailConfirmation.IsSuccess)
            {
                UpdateRequest result = _userRepository.ResetPassword(email, newPassword);
                if (result.IsSuccess)
                {
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.UPDATED;
                    string audit = string.Format("Password reset completed for user with email: {0}", email);
                    ServerLogger.Log.Info(audit);
                    return request;
                }
                else
                {
                    request = ConvertToPasswordReset(result);
                    string audit = string.Format("Password reset failed with code {0} for email {1}", request.StatusCode, email);
                    ServerLogger.Log.Info(audit);
                    return request;
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = emailConfirmation.StatusCode;
                request.RemainingAttempts = emailConfirmation.RemainingAttempts;
                string audit = string.Format("Password reset rejected due to email confirmation failure with code {0} for email {1}", request.StatusCode, email);
                ServerLogger.Log.Info(audit);
                return request;
            }
        }

        public CommunicationRequest UpdatePassword(string username, string currentPassword, string newPassword)
        {
            CommunicationRequest request = new CommunicationRequest();
            UpdateRequest result = _userRepository.UpdatePassword(username, currentPassword, newPassword);
            if (result.IsSuccess)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.UPDATED;
                string audit = string.Format("Password updated for username: {0}", username);
                ServerLogger.Log.Info(audit);
                return request;
            }
            else
            {
                request = ConvertToCommunicationRequest(result);
                string audit = string.Format("Password update failed with code: {0} for username: {1}", request.StatusCode, username);
                ServerLogger.Log.Info(audit);
                return request;
            }
        }

        private static PasswordResetRequest ConvertToPasswordReset(UpdateRequest updateRequest)
        {
            PasswordResetRequest request = new PasswordResetRequest();
            if (updateRequest == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            request.IsSuccess = updateRequest.IsSuccess;
            request.StatusCode = GetStatusCodeFromDbError(updateRequest.ErrorType);
            return request;
        }

        private static CommunicationRequest ConvertToCommunicationRequest(UpdateRequest updateRequest)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (updateRequest == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            request.IsSuccess = updateRequest.IsSuccess;
            request.StatusCode = GetStatusCodeFromDbError(updateRequest.ErrorType);
            return request;
        }

        private static StatusCode GetStatusCodeFromDbError(DataAccess.DataRequests.ErrorType errorType)
        {
            switch (errorType)
            {
                case ErrorType.INVALID_DATA:
                    return StatusCode.WRONG_DATA;
                case ErrorType.NOT_FOUND:
                    return StatusCode.NOT_FOUND;
                case ErrorType.UNALLOWED:
                    return StatusCode.UNAUTHORIZED;
                case ErrorType.DB_ERROR:
                    return StatusCode.DATABASE_ERROR;
                default:
                    return StatusCode.SERVER_ERROR;
            }
        }
    }
}
