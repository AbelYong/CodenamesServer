using System;

namespace DataAccess.Users
{
    public interface IBanDAO
    {
        Ban GetActiveBan(Guid userID);
        void ApplyBan(Ban ban);
    }
}