using System;
using DataAccess.DataRequests;
using DataAccess.Util;

namespace DataAccess.Users
{
    public interface IPlayerDAO
    {
        Player GetPlayerByUserID(Guid userID);
        Player GetPlayerById(Guid playerId);
        OperationResult UpdateProfile(Player updatedPlayer);
        bool VerifyIsPlayerGuest(Guid playerID);
        bool ValidateEmailNotDuplicated(string email);
        DataVerificationRequest VerifyEmailInUse(string email);
        bool ValidateUsernameNotDuplicated(string username);
        string GetEmailByPlayerID(Guid playerId);
    }
}
