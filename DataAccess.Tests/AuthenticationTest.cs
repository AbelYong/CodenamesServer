using DataAccess.Users;
using Moq;
using NUnit.Framework;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Objects;

namespace DataAccess.Tests.UserTests
{
    [TestFixture]
    public class AuthenticationTest
    {
        private Mock<IDbContextFactory> _contextFactory;
        private Mock<ICodenamesContext> _context;
        private Mock<IPlayerDAO> _playerDAO;
        private UserDAO _userDAO;

        [SetUp]
        public void Setup()
        {
            // 1. Mock the Context
            _context = new Mock<ICodenamesContext>();

            // 2. Mock the Factory to return the Mock Context
            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            // 3. Mock the dependency IPlayerDAO (UserDAO needs it in constructor)
            _playerDAO = new Mock<IPlayerDAO>();

            // 4. Instantiate SUT
            _userDAO = new UserDAO(_contextFactory.Object, _playerDAO.Object);
        }

        [Test]
        public void Authenticate_CredentialsAreCorrect_ReturnsUserId()
        {
            // Arrange
            string username = "ValidUser";
            string password = "ValidPassword";
            Guid expectedUserId = Guid.NewGuid();

            // We use Callback to simulate the Stored Procedure setting the output parameter value
            _context.Setup(c => c.uspLogin(
                It.Is<string>(s => s == username),
                It.Is<string>(s => s == password),
                It.IsAny<ObjectParameter>()))
            .Callback<string, string, ObjectParameter>((u, p, outParam) =>
            {
                outParam.Value = expectedUserId;
            })
            .Returns(0); // Return integer status code (0 usually means success in SPs)

            // Act
            Guid? result = _userDAO.Authenticate(username, password);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedUserId));
            _context.Verify(c => c.uspLogin(username, password, It.IsAny<ObjectParameter>()), Times.Once);
        }

        [Test]
        public void Authenticate_CredentialsAreIncorrect_ReturnsNull()
        {
            // Arrange
            string username = "ValidUser";
            string password = "WrongPassword";

            // Simulate DB returning DBNull.Value (user not found)
            _context.Setup(c => c.uspLogin(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ObjectParameter>()))
            .Callback<string, string, ObjectParameter>((u, p, outParam) =>
            {
                outParam.Value = DBNull.Value;
            })
            .Returns(0);

            // Act
            Guid? result = _userDAO.Authenticate(username, password);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Authenticate_DatabaseThrowsEntityException_ThrowsException()
        {
            // Arrange
            string username = "User";
            string password = "Pass";

            _context.Setup(c => c.uspLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new EntityException("Connection failed"));

            // Act & Assert
            Assert.Throws<EntityException>(() => _userDAO.Authenticate(username, password));
        }
    }
}