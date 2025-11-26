using DataAccess.DataRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Users
{
    public interface IUserDAO
    {
        Guid? Authenticate(string username, string password);

        PlayerRegistrationRequest SignIn(Player player, string password);
    }
}
