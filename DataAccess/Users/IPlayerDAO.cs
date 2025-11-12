using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Util;

namespace DataAccess.Users
{
    public interface IPlayerDAO
    {
        Player GetPlayerByUserID(Guid userID);
        Player GetPlayerById(Guid playerId);
        OperationResult UpdateProfile(Player updatedPlayer);
    }
}
