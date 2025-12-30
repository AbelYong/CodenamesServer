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
        private readonly IUserDAO _userDAO;
        private readonly IBanDAO _banDAO;
        private readonly IEmailManager _emailManager;

        public AuthenticationService() : this(new UserDAO(), new BanDAO(), new EmailService()) { }

        public AuthenticationService(IUserDAO userDAO, IBanDAO banDAO, IEmailManager emailManager)
        {
            _userDAO = userDAO;
            _banDAO = banDAO;
            _emailManager = emailManager;
        }

        public AuthenticationRequest Authenticate(string username, string password)
        {
            AuthenticationRequest request = new AuthenticationRequest();
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
            catch (Exception ex) when (ex is EntityException || ex is SqlException)
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

        public PasswordResetRequest CompletePasswordReset(string email, string code, string newPassword)
        {
            PasswordResetRequest request = new PasswordResetRequest();

            ConfirmEmailRequest emailConfirmation = _emailManager.ValidateVerificationCode(email, code, EmailType.PASSWORD_RESET);
            if (emailConfirmation.IsSuccess)
            {
                UpdateRequest result = _userDAO.ResetPassword(email, newPassword);
                if (result.IsSuccess)
                {
                    request.IsSuccess = true;
                    request.StatusCode = StatusCode.UPDATED;
                    return request;
                }
                else
                {
                    return ConvertToPasswordReset(result);
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = emailConfirmation.StatusCode;
                request.RemainingAttempts = emailConfirmation.RemainingAttempts;
                return request;
            }
        }

        public CommunicationRequest UpdatePassword(string username, string currentPassword, string newPassword)
        {
            CommunicationRequest request = new CommunicationRequest();
            UpdateRequest result = _userDAO.UpdatePassword(username, currentPassword, newPassword);
            if (result.IsSuccess)
            {
                request.IsSuccess = true;
                request.StatusCode = StatusCode.UPDATED;
                return request;
            }
            else
            {
                return ConvertToCommunicationRequest(result);
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
                    return StatusCode.SERVER_ERROR;
                default:
                    return StatusCode.SERVER_ERROR;
            }
        }
    }
}
