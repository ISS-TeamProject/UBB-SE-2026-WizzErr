using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Integration.Services;

public class AuthServiceIntegrationTests : BaseIntegrationTest
{
    private const int UniqueCodeStartIndex = 0;
    private const int UniqueCodeLength = 4;
    private const string AndreiEmail = "andrei.codreanu";
    private const string AndreiUsername = "AndreiC";
    private const string AndreiPassword = "Parola@Andrei123";
    private const string AndreiPhone = "0733887766";
    private const string ClaudiaEmail = "claudia.radu";
    private const string ClaudiaPassword = "ParolaClaudia1";
    private const string SorinEmail = "sorin.mihai";
    private const string SorinUsername = "SorinM";
    private const string SorinPassword = "MihaiSecret99!";
    private const string SorinPhone = "0766112233";
    private const string DomainYahoo = "@yahoo.ro";
    private const string DomainGmail = "@gmail.com";
    private const string DefaultPhone = "0744112233";
    private const string AltUsername = "AltUtilizator";
    private const string AltPassword = "AltaParola2";
    private readonly IUserRepository _userRepository;
    private readonly AuthService _authService;

    public AuthServiceIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, membershipRepository);
        _authService = new AuthService(_userRepository);
    }

    [Fact]
    public void TestThatUserCanRegisterAndLoginSuccessfully()
    {
        string uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        string email = $"{AndreiEmail}_{uniqueCode}{DomainGmail}";
        string phone = AndreiPhone;
        string username = $"{AndreiUsername}_{uniqueCode}";
        string password = AndreiPassword;

        _authService.Register(email, phone, username, password);
        var loginResult = _authService.Login(email, password);

        loginResult.Should().NotBeNull();
        loginResult.Email.Should().Be(email);
    }

    [Fact]
    public void TestThatDuplicateEmailRegistrationThrows()
    {
        string uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        string email = $"{ClaudiaEmail}_{uniqueCode}{DomainYahoo}";
        _authService.Register(email, DefaultPhone, $"ClaudiaR_{uniqueCode}", ClaudiaPassword);

        Action registerAction = () => _authService.Register(email, DefaultPhone, $"{AltUsername}_{uniqueCode}", AltPassword);
        registerAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TestThatPasswordIsHashedInDatabase()
    {
        string uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        string email = $"{SorinEmail}_{uniqueCode}{DomainGmail}";
        string password = SorinPassword;

        _authService.Register(email, SorinPhone, $"{SorinUsername}_{uniqueCode}", password);
        var user = _userRepository.GetByEmail(email);

        user!.PasswordHash.Should().NotBe(password);
    }
}


