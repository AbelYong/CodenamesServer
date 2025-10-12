using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Services
{
    public class AuthenticationService : IAuthenticationManager
    {
        private static IUserDAO _userDAO = new UserDAO();
        public Guid? Login(string username, string password)
        {
            return _userDAO.Login(username, password);
        }

        public Guid? SignIn(User svUser, Player svPlayer)
        {
            DataAccess.Player player = AssemblePlayer(svUser, svPlayer);
            string password = svUser.Password;

            return _userDAO.SignIn(player, password);
        }

        private DataAccess.Player AssemblePlayer(User svUser, Player svPlayer)
        {
            DataAccess.User dbUser = new DataAccess.User();
            dbUser.email = svUser.Email;

            DataAccess.Player dbPlayer = new DataAccess.Player();
            dbPlayer.username = svPlayer.Username;
            dbPlayer.name = svPlayer.Name;
            dbPlayer.lastName = svPlayer.LastName;
            dbPlayer.User = dbUser;

            return dbPlayer;
        }
    }
}
