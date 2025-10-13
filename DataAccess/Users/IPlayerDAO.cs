using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Users
{
    public interface IPlayerDAO
    {
        Player GetPlayerByUserID(Guid userID);
    }
}
