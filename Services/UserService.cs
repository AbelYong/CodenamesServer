using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using Services.DTO;

namespace Services
{
    public class UserService : IUserManager
    {
        private static IPlayerDAO _playerDAO = new PlayerDAO();
        public Player GetPlayerByUserID(Guid userID)
        {
            DataAccess.Player dbPlayer = _playerDAO.GetPlayerByUserID(userID);
            return Player.AssembleSvPlayer(dbPlayer);
        }
    }
}
