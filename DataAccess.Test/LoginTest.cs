using DataAccess.Users;
using NUnit.Framework;
using System;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class LoginTest
    {
        private UserDAO _userDAO;
        private Player _player;

        [SetUp]
        public void Setup()
        {
            _userDAO = new UserDAO();
            _player = new Player();
            _player.User = new User();
            _player.User.email = "email";
            _player.username = "tester";
            string password = "password";
            _userDAO.SignIn(_player, password);
        }

        [TearDown]
        public void TearDown()
        {
            if (_userDAO != null)
            {
                UserDAO.DeleteUser(_player);
            }
        }

        [Test]
        public void Login_CorrectCredentials_ReturnId()
        {
            string username = "tester";
            string password = "password";
            if (_userDAO != null)
            {
                Guid? userID = _userDAO.Authenticate(username, password);
                Assert.That(userID, Is.Not.Null);
            }
        }

        [Test]
        [TestCase("wrongUsername", "password", TestName = "Incorrect Username")]
        [TestCase("tester", "wrongPassword", TestName ="Incorrect Password")]
        [TestCase("Tester", "password", TestName="Wrongly cased username")]
        [TestCase
            (
            "really extremelly astonishingly unpractically long username", 
            "password that exceeds DB character limit",
            TestName ="Too long name and password"
            )
        ]
        public void Login_IncorrectCredentials_ReturnNull(string username, string password)
        {
            username = "Tester";
            password = "password";
            if (_userDAO != null)
            {
                Guid? userID = _userDAO.Authenticate(username, password);
                Assert.That(userID, Is.Null);
            }
        }
    }
}