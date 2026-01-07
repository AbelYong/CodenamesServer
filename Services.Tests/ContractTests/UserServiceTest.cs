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
            Guid userId = Guid.NewGuid();
            Guid playerId = Guid.NewGuid();
            var dbPlayer = new DataAccess.Player { playerID = playerId };
            _playerDaoMock.Setup(p => p.GetPlayerByUserID(userId))
                .Returns(dbPlayer);
            Player expected = new Player { PlayerID = playerId };

            var result = _userService.GetPlayerByUserID(userId);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void GetPlayerByUserID_UserNotExists_ReturnsPlayerWithEmptyID()
        {
            Guid notFoundUserId = Guid.NewGuid();
            _playerDaoMock.Setup(p => p.GetPlayerByUserID(notFoundUserId))
                .Returns((DataAccess.Player)null);

            var result = _userService.GetPlayerByUserID(notFoundUserId);

            Assert.That(result.PlayerID.Equals(Guid.Empty));
        }

        [Test]
        public void SignIn_NullPlayer_ReturnsMissingData()
        {
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            var result = _userService.SignIn(null);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_NullUser_ReturnsMissingData()
        {
            var player = new Player { User = null };
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_Success_ReturnsSuccess()
        {
            var player = CreateTestPlayer();
            PlayerRegistrationRequest dbResponse = new PlayerRegistrationRequest
            {
                IsSuccess = true,
                IsEmailDuplicate = false,
                IsUsernameDuplicate = false,
                IsEmailValid = true,
                IsPasswordValid = true
            };
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(dbResponse);
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.OK,
                IsEmailDuplicate = false,
                IsUsernameDuplicate = false,
                IsEmailValid = true,
                IsPasswordValid = true
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_InvalidData_ReturnsWrongData()
        {
            var player = CreateTestPlayer();
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.INVALID_DATA,
                    IsPasswordValid = false
                });
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.WRONG_DATA,
                IsPasswordValid = false
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_DuplicateEntry_ReturnsUnallowed()
        {
            var player = CreateTestPlayer();
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.DUPLICATE,
                    IsUsernameDuplicate = true
                });
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED,
                IsUsernameDuplicate = true
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_DaoMissingData_ReturnsMissingData()
        {
            var player = CreateTestPlayer();
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.MISSING_DATA
                });
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void SignIn_DbError_ReturnsServerError()
        {
            var player = CreateTestPlayer();
            _userDaoMock.Setup(u => u.SignIn(It.IsAny<DataAccess.Player>(), It.IsAny<string>()))
                .Returns(new PlayerRegistrationRequest
                {
                    IsSuccess = false,
                    ErrorType = ErrorType.DB_ERROR
                });
            SignInRequest expected = new SignInRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _userService.SignIn(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_Success_ReturnsUpdated()
        {
            var player = CreateTestPlayer();
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = true });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = true,
                StatusCode = StatusCode.UPDATED
            };

            var result = _userService.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_DbError_ReturnsServerError()
        {
            var player = CreateTestPlayer();
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.DB_ERROR });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.SERVER_ERROR
            };

            var result = _userService.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_DaoNotFound_ReturnsNotFound()
        {
            var player = CreateTestPlayer();
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.NOT_FOUND });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.NOT_FOUND
            };

            var result = _userService.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_DaoDuplicate_ReturnsUnallowed()
        {
            var player = CreateTestPlayer();
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns(new OperationResult { Success = false, ErrorType = ErrorType.DUPLICATE });
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.UNALLOWED
            };

            var result = _userService.UpdateProfile(player);

            Assert.That(result.Equals(expected));
        }

        [Test]
        public void UpdateProfile_NullResult_ReturnsMissingData()
        {
            var player = CreateTestPlayer();
            _playerDaoMock.Setup(p => p.UpdateProfile(It.IsAny<DataAccess.Player>()))
                .Returns((OperationResult)null);
            CommunicationRequest expected = new CommunicationRequest
            {
                IsSuccess = false,
                StatusCode = StatusCode.MISSING_DATA
            };

            var result = _userService.UpdateProfile(player);

            Assert.That(result.Equals(expected));
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
