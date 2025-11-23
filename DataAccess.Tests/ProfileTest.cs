using DataAccess.DataRequests;
using DataAccess.Users;
using DataAccess.Util;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;

namespace DataAccess.Test
{
    [TestFixture]
    public class ProfileTest
    {
        private UserDAO _userDAO;
        private PlayerDAO _playerDAO;
        private Player _player;
        private Player _auxPlayer;
        private Guid _userID;

        [OneTimeSetUp]
        public void OnetimeSetup()
        {
            _userDAO = new UserDAO();
            _playerDAO = new PlayerDAO();
            //Player to update
            _player = new Player();
            _player.User = new User();
            _player.User.email = "profile@test.com";
            _player.username = "profileTester";
            string password = "password";
            _userID = (Guid)_userDAO.SignIn(_player, password);

            //Helper player to test data duplications
            _auxPlayer = new Player();
            _auxPlayer.User = new User();
            _auxPlayer.User.email = "emailInUse@test.com";
            _auxPlayer.username = "duplicatedUsername";
            string auxPassword = "password";
            _userDAO.SignIn(_auxPlayer, auxPassword);
        }

        [OneTimeTearDown]
        public void OnetimeTeardown()
        {
            PlayerDAO.DeletePlayer(_player);
            PlayerDAO.DeletePlayer(_auxPlayer);
        }

        [SetUp]
        public void Setup()
        {
            _player = _playerDAO.GetPlayerByUserID(_userID);
        }

        [TearDown]
        public void Teardown()
        {
            _player = _playerDAO.GetPlayerByUserID(_userID);
            _player.User.email = "profile@test.com";
            _player.username = "profileTester";
            _player.name = null;
            _player.lastName = null;
            _player.facebookUsername = null;
            _player.instagramUsername = null;
            _player.discordUsername = null;
            _playerDAO.UpdateProfile(_player);
        }

        [Test]
        public void UpdateProfile_ToUsernameNotInUse_Success()
        {
            _player.username = "UsernameNotInUse";

            OperationResult result = _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);

            Assert.That(result.Success && _player.username == updatedPlayer.username);
        }

        [Test]
        public void UpdateProfile_ToUsernameInUseCaseMatch_Failure()
        {
            _player.username = _auxPlayer.username;

            OperationResult result = _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);

            Assert.That(
                result.ErrorType == ErrorType.DUPLICATE && updatedPlayer.username == "profileTester");
        }

        [Test]
        public void UpdateProfile_ToUsernameInUseCaseMissmatch_Failure()
        {
            _player.username = "DuPlicatedUsernamE";
            
            OperationResult result = _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);

            Assert.That(result.ErrorType == ErrorType.DUPLICATE && updatedPlayer.username == "profileTester");
        }

        [Test]
        public void UpdateProfile_ToUsernameTooLong_Failure()
        {
            _player.username = "Too long player username";
            
            OperationResult result = _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(result.ErrorType == ErrorType.INVALID_DATA && updatedPlayer.username == "profileTester");
        }

        [Test]
        public void UpdateProfile_ToEmailNotInUse_Success()
        {
            _player.User.email = "notInUse@email.com";

            OperationResult result = _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(result.Success && _player.User.email == updatedPlayer.User.email);
        }

        [Test]
        public void UpdateProfile_ToEmailInUse_Failure()
        {
            _player.User.email = _auxPlayer.User.email;
            
            OperationResult result = _playerDAO.UpdateProfile( _player);
            Player UpdatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(result.ErrorType == ErrorType.DUPLICATE && UpdatedPlayer.User.email == "profile@test.com");
        }

        [Test]
        public void UpdateProfile_UpdateName_Success()
        {
            _player.name = "profile";

            _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);

            Assert.That(_player.name == updatedPlayer.name);
        }

        [Test]
        public void UpdateProfile_UpdateLastname_Success()
        {
            _player.lastName = "last name test";
            
            _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(_player.lastName == updatedPlayer.lastName);
        }

        [Test]
        public void UpdateProfile_UpdateFacebook_Success()
        {
            _player.facebookUsername = "facebook";
            
            _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(_player.facebookUsername == updatedPlayer.facebookUsername);
        }

        [Test]
        public void UpdateProfile_UpdateInstagram_Success()
        {
            _player.instagramUsername = "instagram";
            
            _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(_player.instagramUsername == updatedPlayer.instagramUsername);
        }

        [Test]
        public void UpdateProfile_UpdateDiscord_Success()
        {
            _player.discordUsername = "discord";
            
            _playerDAO.UpdateProfile(_player);
            Player updatedPlayer = _playerDAO.GetPlayerByUserID(_userID);
            
            Assert.That(_player.discordUsername == updatedPlayer.discordUsername);
        }
    }
}
