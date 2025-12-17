using System;

namespace DataAccess.Moderation
{
    public interface IBanDAO
    {
        Ban GetActiveBan(Guid userID);
        void ApplyBan(Ban ban);
    }
}