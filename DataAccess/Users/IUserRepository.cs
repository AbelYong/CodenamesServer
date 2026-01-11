using DataAccess.DataRequests;
using System;

namespace DataAccess.Users
{
    public interface IUserRepository
    {
        Guid? Authenticate(string username, string password);
        PlayerRegistrationRequest SignIn(Player player, string password);
        UpdateRequest ResetPassword(string email, string newPassword);
        UpdateRequest UpdatePassword(string username, string currentPassword, string newPassword);
    }
}
