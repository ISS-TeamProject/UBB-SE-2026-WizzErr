using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Identity;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly AuthService _authService;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _authService = new AuthService(_mockUserRepository.Object);
    }

    [Fact]
    public void TestThatLoginWorksForValidRomanianUser()
    {
        var pass = "ParolaIonut123!";
        var user = new User { Email = "ionut.popescu@gmail.com" };
        user.PasswordHash = _passwordHasher.HashPassword(user, pass);

        _mockUserRepository.Setup(r => r.GetByEmail(user.Email)).Returns(user);

        var result = _authService.Login(user.Email, pass);

        result.Should().NotBeNull();
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public void TestThatLoginFailsWithInvalidPassword()
    {
        var user = new User { Email = "gigel.frone@yahoo.ro" };
        user.PasswordHash = _passwordHasher.HashPassword(user, "parola_corecta");

        _mockUserRepository.Setup(r => r.GetByEmail(user.Email)).Returns(user);

        Action act = () => _authService.Login(user.Email, "parola_incorecta_99");
        act.Should().Throw<InvalidOperationException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public void TestThatRegisterFailsForDuplicateEmail()
    {
        string email = "costel.biju@gmail.com";
        _mockUserRepository.Setup(r => r.GetByEmail(email)).Returns(new User { Email = email });

        Action act = () => _authService.Register(email, "0722334455", "costel_b", "ParolaBiju!");
        act.Should().Throw<InvalidOperationException>().WithMessage("An account with this email already exists.");
    }

    [Fact]
    public void TestThatRegisterFailsForInvalidEmailFormat()
    {
        Action act = () => _authService.Register("mariusPaguba", "0722", "marius", "Parola1");
        act.Should().Throw<ArgumentException>().WithMessage("Email format is invalid.");
    }

    [Fact]
    public void TestThatRegisterCreatesNewRomanianUser()
    {
        string email = "andreea.marin@yahoo.ro";
        _mockUserRepository.Setup(r => r.GetByEmail(email)).Returns((User?)null);

        _authService.Register(email, "0744112233", "andreeam", "ZanaSurprizelor1!");

        _mockUserRepository.Verify(r => r.AddUser(It.Is<User>(u => u.Email == email)), Times.Once);
    }
}
