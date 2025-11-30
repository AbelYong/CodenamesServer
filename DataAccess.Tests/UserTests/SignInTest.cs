using DataAccess.DataRequests;
using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        private Mock<IPlayerDAO> _playerDAO;
        private UserDAO _userDAO;

        [SetUp]
        public void Setup()
        {
            _context = new Mock<ICodenamesContext>();

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerDAO = new Mock<IPlayerDAO>();

            _userDAO = new UserDAO(_contextFactory.Object, _playerDAO.Object);
        }

        [Test]
        public void SignIn_ValidData_ReturnsSuccessWithId()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";
            Guid expectedNewId = Guid.NewGuid();

            // Mock dependencies to pass validation
            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            // Mock Stored Procedure execution
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

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.NewPlayerID, Is.EqualTo(expectedNewId));
            Assert.That(result.IsEmailValid, Is.True);
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
            // Arrange
            Player player = null; // Invalid
            string password = "Password1!";

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.MISSING_DATA));
        }

        [Test]
        public void SignIn_EmailDuplicated_ReturnsDuplicateError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(player.User.email)).Returns(false);
            // Assume username is valid to isolate error
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
            Assert.That(result.IsEmailDuplicate, Is.True);
            _context.Verify(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()), Times.Never);
        }

        [Test]
        public void SignIn_UsernameDuplicated_ReturnsDuplicateError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(player.username)).Returns(false);

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DUPLICATE));
            Assert.That(result.IsUsernameDuplicate, Is.True);
        }

        [Test]
        public void SignIn_InvalidEmailFormat_ReturnsInvalidDataError()
        {
            // Arrange
            var player = CreateValidPlayer();
            player.User.email = "invalid-email"; // No @ or domain
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
            Assert.That(result.IsEmailValid, Is.False);
        }

        [Test]
        public void SignIn_InvalidPassword_ReturnsInvalidDataError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "123"; // Too short

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.INVALID_DATA));
            Assert.That(result.IsPasswordValid, Is.False);
        }

        [Test]
        public void SignIn_DbUpdateException_ReturnsDbError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new DbUpdateException("Simulated DB Error"));

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SignIn_EntityException_ReturnsDbError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new EntityException("Simulated Entity Error"));

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
        }

        [Test]
        public void SignIn_InvalidCastException_ReturnsDbError()
        {
            // Arrange
            var player = CreateValidPlayer();
            string password = "ValidPassword1!";

            _playerDAO.Setup(p => p.ValidateEmailNotDuplicated(It.IsAny<string>())).Returns(true);
            _playerDAO.Setup(p => p.ValidateUsernameNotDuplicated(It.IsAny<string>())).Returns(true);

            _context.Setup(c => c.uspSignIn(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new InvalidCastException("Simulated Cast Error"));

            // Act
            var result = _userDAO.SignIn(player, password);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ErrorType.DB_ERROR));
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
