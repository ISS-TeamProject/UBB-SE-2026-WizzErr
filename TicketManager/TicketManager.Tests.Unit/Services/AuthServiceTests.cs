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
    private const string ValidPassword = "ParolaAndrei2024!";
    private const string ValidSecretPassword = "parola_secreta_99";
    private const string InvalidPassword = "parola_incorecta_99";
    private const string ValidPhoneNumber = "0722334455";
    private const string ValidAlternatePhone = "0744112233";
    private const string InvalidPhoneFormat = "072211-2233";
    private const string ValidEmail = "test@gmail.com";
    private const string ValidUsername = "user123";
    private const int MinimumPasswordLength = 6;
    private const int MinimumUsernameLength = 3;

    private readonly Mock<IUserRepository> mockUserRepository;
    private readonly AuthService authentificationService;
    private readonly PasswordHasher<User> passwordHasher = new PasswordHasher<User>();

    public AuthServiceTests()
    {
        mockUserRepository = new Mock<IUserRepository>();
        authentificationService = new AuthService(mockUserRepository.Object);
    }

    [Fact]
    public void Login_ValidRomanianUser_ReturnsSuccess()
    {
        var user = new User { Email = "andrei.ionescu@gmail.com" };
        user.PasswordHash = passwordHasher.HashPassword(user, ValidPassword);

        mockUserRepository.Setup(repoWithExistingUser => repoWithExistingUser.GetByEmail(user.Email)).Returns(user);

        var loggedInUser = authentificationService.Login(user.Email, ValidPassword);

        loggedInUser.Should().NotBeNull();
        loggedInUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public void Login_InvalidPassword_ThrowsException()
    {
        var user = new User { Email = "george.popa@yahoo.ro" };
        user.PasswordHash = passwordHasher.HashPassword(user, ValidSecretPassword);

        mockUserRepository.Setup(repoWithRegisteredUser => repoWithRegisteredUser.GetByEmail(user.Email)).Returns(user);

        Action loginAction = () => authentificationService.Login(user.Email, InvalidPassword);
        loginAction.Should().Throw<InvalidOperationException>().WithMessage("Invalid email or password.");
    }

    [Fact]
    public void Register_DuplicateEmailAddress_ThrowsException()
    {
        string email = "bogdan.stefan@gmail.com";
        mockUserRepository.Setup(repoWithDuplicateEmail => repoWithDuplicateEmail.GetByEmail(email)).Returns(new User { Email = email });

        Action registerAction = () => authentificationService.Register(email, ValidPhoneNumber, "bogdan_s", "ParolaBogdan!");
        registerAction.Should().Throw<InvalidOperationException>().WithMessage("An account with this email already exists.");
    }

    [Fact]
    public void Register_InvalidEmailAddressFormat_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register("mariusPaguba", "0722", "marius", "Parola1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Email format is invalid.");
    }

    [Fact]
    public void Register_ValidRomanianUser_CreatesNewUser()
    {
        string email = "gabriela.stan@yahoo.ro";
        mockUserRepository.Setup(repoWithAvailableEmail => repoWithAvailableEmail.GetByEmail(email)).Returns((User?)null);

        authentificationService.Register(email, ValidAlternatePhone, "gabriela_s", "ParolaGabriela123!");

        mockUserRepository.Verify(repoToVerifyAddUser => repoToVerifyAddUser.AddUser(It.Is<User>(userToRegister => userToRegister.Email == email)), Times.Once);
    }

    [Fact]
    public void Login_NullEmailAddress_ThrowsException()
    {
        Action loginAction = () => authentificationService.Login(null!, "password");
        loginAction.Should().Throw<ArgumentException>().WithMessage("Email is required.");
    }

    [Fact]
    public void Login_EmptyEmailAddress_ThrowsException()
    {
        Action loginAction = () => authentificationService.Login(string.Empty, "password");
        loginAction.Should().Throw<ArgumentException>().WithMessage("Email is required.");
    }

    [Fact]
    public void Login_NullPassword_ThrowsException()
    {
        Action loginAction = () => authentificationService.Login(ValidEmail, null!);
        loginAction.Should().Throw<ArgumentException>().WithMessage("Password is required.");
    }

    [Fact]
    public void Login_EmptyPassword_ThrowsException()
    {
        Action loginAction = () => authentificationService.Login(ValidEmail, string.Empty);
        loginAction.Should().Throw<ArgumentException>().WithMessage("Password is required.");
    }

    [Fact]
    public void Login_UserNotFound_ThrowsException()
    {
        mockUserRepository.Setup(repoWithMissingUser => repoWithMissingUser.GetByEmail(It.IsAny<string>())).Returns((User?)null);

        Action loginAction = () => authentificationService.Login("nonexistent@gmail.com", "password");
        loginAction.Should().Throw<InvalidOperationException>().WithMessage("No account found with this email.");
    }

    [Fact]
    public void Register_PasswordTooShort_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register(ValidEmail, ValidPhoneNumber, ValidUsername, "12345");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Password must be at least 6 characters long.");
    }

    [Fact]
    public void Register_UsernameTooShort_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register(ValidEmail, ValidPhoneNumber, "ab", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Username must have at least 3 characters.");
    }

    [Fact]
    public void Register_InvalidUsername_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register(ValidEmail, ValidPhoneNumber, "user@#$", "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Username contains invalid characters.");
    }

    [Fact]
    public void Register_NullTelephoneNumber_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register(ValidEmail, null!, ValidUsername, "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Phone is required.");
    }

    [Fact]
    public void Register_InvalidTelephoneNumber_ThrowsException()
    {
        Action registerAction = () => authentificationService.Register(ValidEmail, InvalidPhoneFormat, ValidUsername, "ValidPass1");
        registerAction.Should().Throw<ArgumentException>().WithMessage("Phone number must contain only digits and have 10 to 15 digits.");
    }
}




