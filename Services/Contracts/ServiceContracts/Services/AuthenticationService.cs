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

        public CommunicationRequest CompletePasswordReset(string email, string code, string newPassword)
        {
            CommunicationRequest request = new CommunicationRequest();

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
                    return ConvertFromUpdateRequest(result);
                }
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.UNAUTHORIZED;
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
                return ConvertFromUpdateRequest(result);
            }
        }

        private static CommunicationRequest ConvertFromUpdateRequest(UpdateRequest updateRequest)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (updateRequest == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            request.IsSuccess = updateRequest.IsSuccess;
            switch (updateRequest.ErrorType)
            {
                case ErrorType.INVALID_DATA:
                    request.StatusCode = StatusCode.WRONG_DATA;
                    break;
                case ErrorType.NOT_FOUND:
                    request.StatusCode = StatusCode.NOT_FOUND;
                    break;
                case ErrorType.UNALLOWED:
                    request.StatusCode = StatusCode.UNAUTHORIZED;
                    break;
                case ErrorType.DB_ERROR:
                    request.StatusCode = StatusCode.SERVER_ERROR;
                    break;
            }
            return request;
        }
    }
}
