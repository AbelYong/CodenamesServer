using System;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;

namespace DataAccess
{
    public interface ICodenamesContext : IDisposable
    {
        DbSet<User> Users { get; set; }
        DbSet<Player> Players { get; set; }
        DbSet<Friendship> Friendships { get; set; }
        DbSet<Scoreboard> Scoreboards { get; set; }
        DbSet<Report> Reports { get; set; }
        DbSet<Ban> Bans { get; set; }

        int SaveChanges();
        int uspLogin(string username, string password, ObjectParameter userID);
        int uspSignIn(string email, string password, string username, string name, string lastName, ObjectParameter newUserID);
        int uspUpdatePassword(string email, string newPassword);
    }
}
