using System;

namespace DataAccess.Moderation
{
    public interface IBanRepository
    {
        Ban GetActiveBan(Guid userID);
        void ApplyBan(Ban ban);
    }
}