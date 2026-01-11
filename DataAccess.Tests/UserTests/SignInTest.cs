using DataAccess.DataRequests;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;

namespace DataAccess.Test.UserTests
{
    [TestFixture]
    public class SignInTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<IPlayerRepository> _playerDAO;
        private UserRepository _userRepository;

        [SetUp]
        public void Setup()
        {
            _context = new Mock<ICodenamesContext>();

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerDAO = new Mock<IPlayerRepository>();

            _userRepository = new UserRepository(_contextFactory.Object, _playerDAO.Object);
        }

        [Test]
        public void SignIn_ValidData_CallsBDReturnsSuccessWithId()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            Guid expectedNewId = Guid.NewGuid();

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            _context.Setup(c => c.uspSignIn(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ObjectParameter>()))
            .Callback<string, string, string, string, string, ObjectParameter>((e, p, u, n, l, outParam) =>
            {
                outParam.Value = expectedNewId;
            })
            .Returns(0);
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = true,
                NewPlayerID = expectedNewId,
                IsEmailValid = true,
                IsEmailDuplicate = false,
                IsUsernameDuplicate = false,
                IsPasswordValid = true,
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
            _context.Verify(c => c.uspSignIn(
                player.User.email,
                password,
                player.username,
                player.name,
                player.lastName,
                It.IsAny<ObjectParameter>()), Times.Once);
        }

        [Test]
        public void SignIn_MissingData_ReturnsMissingDataError()
        {
            Player player = null;
            string password = "Password1!";
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.MISSING_DATA
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_EmailDuplicated_ReturnsDuplicateError()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(player.User.email)).Returns(false);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DUPLICATE,
                IsEmailDuplicate = true
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_UsernameDuplicated_ReturnsDuplicateError()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(player.username)).Returns(false);
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DUPLICATE,
                IsUsernameDuplicate = true
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_InvalidEmailFormat_ReturnsInvalidDataError()
        {
            var player = CreateValidPlayer();
            player.User.email = "invalid-email";
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.INVALID_DATA,
                IsEmailValid = false
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_InvalidPassword_ReturnsInvalidDataError()
        {
            var player = CreateValidPlayer();
            string password = "123";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.INVALID_DATA,
                IsPasswordValid = false
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_DbUpdateException_ReturnsDbError()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new DbUpdateException("Simulated DB Error"));
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DB_ERROR,
                IsEmailValid = true,
                IsPasswordValid = true,
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_EntityException_ReturnsDbError()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new EntityException("Simulated Entity Error"));
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DB_ERROR,
                IsEmailValid = true,
                IsPasswordValid = true,
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_InvalidCastException_ReturnsDbError()
        {
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);
            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new InvalidCastException("Simulated Cast Error"));
            PlayerRegistrationRequest expected = new PlayerRegistrationRequest
            {
                IsSuccess = false,
                ErrorType = ErrorType.DB_ERROR,
                IsEmailValid = true,
                IsPasswordValid = true,
            };

            var result = _userRepository.SignIn(player, password);

            Assert.That(result.Equals(expected));
        }

        private Player CreateValidPlayer()
        {
            return new Player
            {
                username = "ValidUser",
                name = "First",
                lastName = "Last",
                User = new User { email = "valid@gmail.com" }
            };
        }
    }
}
