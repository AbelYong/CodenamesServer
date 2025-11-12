using DataAccess.Users;
using DataAccess.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Services.DTO;
using DataAccess.Properties.Langs;
using Services.Contracts.ServiceContracts.Managers;

namespace Services.Contracts.ServiceContracts.Services
{
    public class UserService : IUserManager
    {
        private static IPlayerDAO _playerDAO = new PlayerDAO();
        public Player GetPlayerByUserID(Guid userID)
        {
            DataAccess.Player dbPlayer = _playerDAO.GetPlayerByUserID(userID);
            return Player.AssembleSvPlayer(dbPlayer);
        }

        public UpdateResult UpdateProfile(Player updatedPlayer)
        {
            DataAccess.Player dbUpdatedPlayer = Player.AssembleDbPlayer(updatedPlayer.User, updatedPlayer);
            DataAccess.Util.OperationResult operationResult = _playerDAO.UpdateProfile(dbUpdatedPlayer);
            return AssembleUpdateResult(operationResult);
        }

        private static UpdateResult AssembleUpdateResult(OperationResult operationResult)
        {
            UpdateResult updateResult = new UpdateResult();
            if (operationResult != null)
            {
                updateResult.Success = operationResult.Success;
                updateResult.Message = operationResult.Message;
            }
            else
            {
                updateResult.Success = false;
                updateResult.Message = Lang.profileUpdateServerSideIssue;
            }
                return updateResult;
        }
    }
}
