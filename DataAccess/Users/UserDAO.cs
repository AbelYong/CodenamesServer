using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class UserDAO : IUserDAO
    {
        public bool Login(User user)
        {
            throw new NotImplementedException();
        }

        private Guid GetUserFromEmail(string email)
        {
            throw new NotImplementedException();
        }
    }
}
