using DataAccess.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Services
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "Service1" en el código y en el archivo de configuración a la vez.
    public class AuthenticationService : IAuthenticationManager
    {
        private static IUserDAO _userDAO = new UserDAO();
        public Guid? Login(string username, string password)
        {
            return _userDAO.Login(username, password);
        }

        public Guid SignIn(User user, Player player)
        {
            //TODO
            throw new NotImplementedException();
        }
    }
}
