using DataAccess.Users;
using DataAccess.DataRequests;
using DataAccess.Util;
using Services.Contracts.ServiceContracts.Managers;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;

namespace Services.Contracts.ServiceContracts.Services
{
    public class UserService : IUserManager
    {
        private readonly IUserDAO _userDAO;
        private readonly IPlayerDAO _playerDAO;

        public UserService() : this (new UserDAO(), new PlayerDAO()) { }
        
        public UserService(IUserDAO userDAO, IPlayerDAO playerDAO)
        {
            _userDAO = userDAO;
            _playerDAO = playerDAO;
        }

        public Player GetPlayerByUserID(Guid userID)
        {
            DataAccess.Player dbPlayer = _playerDAO.GetPlayerByUserID(userID);
            return Player.AssembleSvPlayer(dbPlayer);
        }

        public SignInRequest SignIn(Player svPlayer)
        {
            SignInRequest request = new SignInRequest();
            if (svPlayer != null && svPlayer.User != null)
            {
                svPlayer.Name = string.IsNullOrWhiteSpace(svPlayer.Name) ? null : svPlayer.Name.Trim();
                svPlayer.LastName = string.IsNullOrWhiteSpace(svPlayer.LastName) ? null : svPlayer.LastName.Trim();

                return RegisterNewPlayer(svPlayer);
            }
            else
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
            }
            return request;
        }

        private SignInRequest RegisterNewPlayer(Player svPlayer)
        {
            SignInRequest request = new SignInRequest();
            DataAccess.Player dbPlayer = Player.AssembleDbPlayer(svPlayer);
            string password = svPlayer.User.Password;
            PlayerRegistrationRequest dbRequest = _userDAO.SignIn(dbPlayer, password);
            if (dbRequest.IsSuccess)
            {
                request.IsSuccess = true;
            }
            else
            {
                request.IsSuccess = false;
                request.IsEmailDuplicate = dbRequest.IsEmailDuplicate;
                request.IsEmailValid = dbRequest.IsEmailValid;
                request.IsUsernameDuplicate = dbRequest.IsUsernameDuplicate;
                request.IsPasswordValid = dbRequest.IsPasswordValid;

                switch (dbRequest.ErrorType)
                {
                    case ErrorType.INVALID_DATA:
                        request.StatusCode = StatusCode.WRONG_DATA;
                        break;
                    case ErrorType.DUPLICATE:
                        request.StatusCode = StatusCode.UNALLOWED;
                        break;
                    case ErrorType.MISSING_DATA:
                        request.StatusCode = StatusCode.MISSING_DATA;
                        break;
                    default:
                        request.StatusCode = StatusCode.SERVER_ERROR;
                        break;
                }
            }
            return request;
        }

        public CommunicationRequest UpdateProfile(Player updatedPlayer)
        {
            DataAccess.Player dbUpdatedPlayer = Player.AssembleDbPlayer(updatedPlayer);
            OperationResult operationResult = _playerDAO.UpdateProfile(dbUpdatedPlayer);
            return AssembleUpdateResult(operationResult);
        }

        private static CommunicationRequest AssembleUpdateResult(OperationResult operationResult)
        {
            CommunicationRequest request = new CommunicationRequest();
            if (operationResult == null)
            {
                request.IsSuccess = false;
                request.StatusCode = StatusCode.MISSING_DATA;
                return request;
            }
            
            if (operationResult.Success)
            {
                request.IsSuccess = operationResult.Success;
                request.StatusCode = StatusCode.UPDATED;
                return request;
            }
            else
            {
                request.IsSuccess = false;
                switch (operationResult.ErrorType)
                {
                    case ErrorType.DB_ERROR:
                        request.StatusCode = StatusCode.SERVER_ERROR;
                        break;
                    case ErrorType.INVALID_DATA:
                        request.StatusCode = StatusCode.WRONG_DATA;
                        break;
                    case ErrorType.NOT_FOUND:
                        request.StatusCode = StatusCode.NOT_FOUND;
                        break;
                    case ErrorType.DUPLICATE:
                        request.StatusCode = StatusCode.UNALLOWED;
                        break;
                    default:
                        request.StatusCode = StatusCode.SERVER_ERROR;
                        break;
                }
            }
            return request;
        }
    }
}
