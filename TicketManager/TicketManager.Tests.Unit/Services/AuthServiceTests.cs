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
        var pass = "ParolaAndrei2024!";
        var user = new User { Email = "andrei.ionescu@gmail.com" };
        user.PasswordHash = _passwordHasher.HashPassword(user, pass);

        _mockUserRepository.Setup(repoWithExistingUser => repoWithExistingUser.GetByEmail(user.Email)).Returns(user);

        var loggedInUser = _authService.Login(user.Email, pass);

        loggedInUser.Should().NotBeNull();
        loggedInUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public void TestThatLoginFailsWithInvalidPassword()
    {
        var user = new User { Email = "george.popa@yahoo.ro" };
        user.PasswordHash = _passwordHasher.HashPassword(user, "parola_secreta_99");

        _mockUserRepository.Setup(repoWithRegisteredUser => repoWithRegisteredUser.GetByEmail(user.Email)).Returns(user);

        Action loginAction = () => _authService.Login(user.Email, "parola_incorecta_99");
        loginAction.Should().Throw<InvalidOperationException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public void TestThatRegisterFailsForDuplicateEmail()
    {
        string email = "bogdan.stefan@gmail.com";
        _mockUserRepository.Setup(repoWithDuplicateEmail => repoWithDuplicateEmail.GetByEmail(email)).Returns(new User { Email = email });

        Action registerAction = () => _authService.Register(email, "0722334455", "bogdan_s", "ParolaBogdan!");
        registerAction.Should().Throw<InvalidOperationException>().WithMessage("An account with this email already exists.");
    }

    [Fact]
    public void TestThatRegisterFailsForInvalidEmailFormat()
    {
        Action registerAction = () => _authService.Register("mariusPaguba", "0722", "marius", "Parola1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Email format is invalid.");
    }

    [Fact]
    public void TestThatRegisterCreatesNewRomanianUser()
    {
        string email = "gabriela.stan@yahoo.ro";
        _mockUserRepository.Setup(repoWithAvailableEmail => repoWithAvailableEmail.GetByEmail(email)).Returns((User?)null);

        _authService.Register(email, "0744112233", "gabriela_s", "ParolaGabriela123!");

        _mockUserRepository.Verify(repoToVerifyAddUser => repoToVerifyAddUser.AddUser(It.Is<User>(userToRegister => userToRegister.Email == email)), Times.Once);
    }

    [Fact]
    public void TestThatLoginThrowsExceptionWhenEmailIsNull()
    {
        Action loginAction = () => _authService.Login(null!, "password");
        loginAction.Should().Throw<ArgumentException>().WithMessage("Email is required.");
    }

    [Fact]
    public void TestThatLoginThrowsExceptionWhenEmailIsEmpty()
    {
        Action loginAction = () => _authService.Login("", "password");
        loginAction.Should().Throw<ArgumentException>().WithMessage("Email is required.");
    }

    [Fact]
    public void TestThatLoginThrowsExceptionWhenPasswordIsNull()
    {
        Action loginAction = () => _authService.Login("test@gmail.com", null!);
        loginAction.Should().Throw<ArgumentException>().WithMessage("Password is required.");
    }

    [Fact]
    public void TestThatLoginThrowsExceptionWhenPasswordIsEmpty()
    {
        Action loginAction = () => _authService.Login("test@gmail.com", "");
        loginAction.Should().Throw<ArgumentException>().WithMessage("Password is required.");
    }

    [Fact]
    public void TestThatLoginThrowsExceptionWhenUserNotFound()
    {
        _mockUserRepository.Setup(repoWithMissingUser => repoWithMissingUser.GetByEmail(It.IsAny<string>())).Returns((User?)null);

        Action loginAction = () => _authService.Login("nonexistent@gmail.com", "password");
        loginAction.Should().Throw<InvalidOperationException>().WithMessage("No account found with this email.");
    }

    [Fact]
    public void TestThatRegisterThrowsExceptionWhenPasswordTooShort()
    {
        Action registerAction = () => _authService.Register("test@gmail.com", "0722112233", "user123", "12345");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Password must be at least 6 characters long.");
    }

    [Fact]
    public void TestThatRegisterThrowsExceptionWhenUsernameTooShort()
    {
        Action registerAction = () => _authService.Register("test@gmail.com", "0722112233", "ab", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Username must have at least 3 characters.");
    }

    [Fact]
    public void TestThatRegisterThrowsExceptionWhenUsernameContainsInvalidCharacters()
    {
        Action registerAction = () => _authService.Register("test@gmail.com", "0722112233", "user@#$", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Username contains invalid characters.");
    }

    [Fact]
    public void TestThatRegisterThrowsExceptionWhenPhoneIsNull()
    {
        Action registerAction = () => _authService.Register("test@gmail.com", null!, "user123", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Phone is required.");
    }

    [Fact]
    public void TestThatRegisterThrowsExceptionWhenPhoneIsInvalid()
    {
        Action registerAction = () => _authService.Register("test@gmail.com", "072211-2233", "user123", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Phone number must contain only digits and have 10 to 15 digits.");
    }
}


