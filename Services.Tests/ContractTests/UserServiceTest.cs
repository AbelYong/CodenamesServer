using DataAccess.DataRequests;
using DataAccess.Users;
using DataAccess.Util;
using Moq;
using NUnit.Framework;
using Services.Contracts.ServiceContracts.Services;
using Services.DTO.DataContract;
using Services.DTO.Request;
using System;

namespace Services.Tests.ContractTests
{
    [TestFixture]
    public class UserServiceTest
    {
        private Mock<IUserDAO> _userDaoMock;
        private Mock<IPlayerDAO> _playerDaoMock;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _userDaoMock = new Mock<IUserDAO>();
            _playerDaoMock = new Mock<IPlayerDAO>();
            _userService = new UserService(_userDaoMock.Object, _playerDaoMock.Object);
        }

        [Test]
        public void GetPlayerByUserID_UserExists_ReturnsPlayer()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            var dbPlayer = new DataAccess.Player
            {
                playerID = userId,
                User = new DataAccess.User { email = "test@test.com" }
            };

            _playerDaoMock.Setup(p => p.GetPlayerByUserID(userId))
                .Returns(dbPlayer);

            // Act
            var result = _userService.GetPlayerByUserID(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(userId.Equals(result.PlayerID));
        }

        [Test]
        public void GetPlayerByUserID_UserNotExists_ReturnsPlayerWithEmptyID()
        {
            // Arrange
            Guid notFoundUserId = Guid.NewGuid();

            _playerDaoMock.Setup(p => p.GetPlayerByUserID(notFoundUserId))
                .Returns((DataAccess.Player)null);

            // Act
            var result = _userService.GetPlayerByUserID(notFoundUserId);

            // Assert
            Assert.That(result.PlayerID.Equals(Guid.Empty));
        }

        [Test]
        public void SignIn_NullPlayer_ReturnsMissingData()
        {
            // Act
            var result = _userService.SignIn(null);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void SignIn_NullUser_ReturnsMissingData()
        {
            // Arrange
            var player = new Player { User = null };

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void SignIn_Success_ReturnsTrue()
        {
            // Arrange
            var player = CreateTestPlayer();

            // DAO returns success
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest { IsSuccess = true });

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess);
        }

        [Test]
        public void SignIn_InvalidData_ReturnsWrongData()
        {
            // Arrange
            var player = CreateTestPlayer();

            // DAO returns Invalid Data (e.g. Password too short, invalid email format)
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.INVALID_DATA,
                    IsPasswordValid = false
                });

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.WRONG_DATA.Equals(result.StatusCode));
            Assert.That(result.IsPasswordValid, Is.False);
        }

        [Test]
        public void SignIn_DuplicateEntry_ReturnsUnallowed()
        {
            // Arrange
            var player = CreateTestPlayer();

            // DAO returns Duplicate (e.g. Username taken)
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.DUPLICATE,
                    IsUsernameDuplicate = true
                });

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
            Assert.That(result.IsUsernameDuplicate);
        }

        [Test]
        public void SignIn_DaoMissingData_ReturnsMissingData()
        {
            // Arrange
            var player = CreateTestPlayer();

            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.MISSING_DATA
                });

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        [Test]
        public void SignIn_DbError_ReturnsServerError()
        {
            // Arrange
            var player = CreateTestPlayer();

            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.DB_ERROR
                });

            // Act
            var result = _userService.SignIn(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void UpdateProfile_Success_ReturnsUpdated()
        {
            // Arrange
            var player = CreateTestPlayer();

            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = true });

            // Act
            var result = _userService.UpdateProfile(player);

            // Assert
            Assert.That(result.IsSuccess);
            Assert.That(StatusCode.UPDATED.Equals(result.StatusCode));
        }

        [Test]
        public void UpdateProfile_DbError_ReturnsServerError()
        {
            // Arrange
            var player = CreateTestPlayer();

            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.DB_ERROR });

            // Act
            var result = _userService.UpdateProfile(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.SERVER_ERROR.Equals(result.StatusCode));
        }

        [Test]
        public void UpdateProfile_NotFound_ReturnsNotFound()
        {
            // Arrange
            var player = CreateTestPlayer();

            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.NOT_FOUND });

            // Act
            var result = _userService.UpdateProfile(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.NOT_FOUND.Equals(result.StatusCode));
        }

        [Test]
        public void UpdateProfile_Duplicate_ReturnsUnallowed()
        {
            // Arrange (e.g. Trying to change email to one that already exists)
            var player = CreateTestPlayer();

            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.DUPLICATE });

            // Act
            var result = _userService.UpdateProfile(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.UNALLOWED.Equals(result.StatusCode));
        }

        [Test]
        public void UpdateProfile_NullResult_ReturnsMissingData()
        {
            // Arrange
            var player = CreateTestPlayer();

            // Simulate DAO returning null (unexpected, but handled by defensive code)
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns((OperationResult)null);

            // Act
            var result = _userService.UpdateProfile(player);

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(StatusCode.MISSING_DATA.Equals(result.StatusCode));
        }

        private Player CreateTestPlayer()
        {
            return new Player
            {
                Username = "TestUser",
                Name = "John",
                LastName = "Doe",
                User = new User
                {
                    Email = "test@example.com",
                    Password = "SecurePass123!"
                }
            };
        }
    }
}
