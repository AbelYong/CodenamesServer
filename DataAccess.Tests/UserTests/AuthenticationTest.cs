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
        private Mock<IPlayerRepository> _playerRepository;
        private UserRepository _userRepository;

        [SetUp]
        public void Setup()
        {
            _context = new Mock<ICodenamesContext>();

            _contextFactory = new Mock<IDbContextFactory>();
            _contextFactory.Setup(f => f.Create()).Returns(_context.Object);

            _playerRepository = new Mock<IPlayerRepository>();

            _userRepository = new UserRepository(_contextFactory.Object, _playerRepository.Object);
        }

        [Test]
        public void Authenticate_CredentialsAreCorrect_ReturnsUserId()
        {
            string username = "ValidUser";
            string password = "ValidPassword";
            Guid expectedUserId = Guid.NewGuid();
            
            _context.Setup(c => c.uspLogin(
                It.Is<string>(s => s == username),
                It.Is<string>(s => s == password),
                It.IsAny<ObjectParameter>()))
            .Callback<string, string, ObjectParameter>((u, p, outParam) =>
            {
                outParam.Value = expectedUserId;
            })
            .Returns(0);

            Guid? result = _userRepository.Authenticate(username, password);

            Assert.That(result, Is.EqualTo(expectedUserId));
        }

        [Test]
        public void Authenticate_CredentialsAreIncorrect_ReturnsNull()
        {
            string username = "ValidUser";
            string password = "WrongPassword";

            _context.Setup(c => c.uspLogin(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ObjectParameter>()))
            .Callback<string, string, ObjectParameter>((u, p, outParam) =>
            {
                outParam.Value = DBNull.Value;
            })
            .Returns(0);

            Guid? result = _userRepository.Authenticate(username, password);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Authenticate_DatabaseThrowsException_RethrowsException()
        {
            string username = "User";
            string password = "Pass";

            _context.Setup(c => c.uspLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ObjectParameter>()))
                .Throws(new EntityException("Connection failed"));

            Assert.Throws<EntityException>(() => _userRepository.Authenticate(username, password));
        }
    }
}