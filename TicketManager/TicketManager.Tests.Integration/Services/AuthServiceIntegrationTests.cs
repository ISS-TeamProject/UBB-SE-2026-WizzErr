using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Integration.Services;

public class AuthServiceIntegrationTests : BaseIntegrationTest
{
    private readonly IUserRepository _userRepository;
    private readonly AuthService _authService;

    public AuthServiceIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepo = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, membershipRepo);
        _authService = new AuthService(_userRepository);
    }

    [Fact]
    public void TestThatUserCanRegisterAndLoginSuccessfully()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"andrei.codreanu_{code}@gmail.com";
        string phone = "0733887766";
        string username = $"AndreiC_{code}";
        string password = "Parola@Andrei123";

        _authService.Register(email, phone, username, password);
        var loginResult = _authService.Login(email, password);

        loginResult.Should().NotBeNull();
        loginResult.Email.Should().Be(email);
    }

    [Fact]
    public void TestThatDuplicateEmailRegistrationThrows()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"claudia.radu_{code}@yahoo.ro";
        _authService.Register(email, "0744112233", $"ClaudiaR_{code}", "ParolaClaudia1");

        Action act = () => _authService.Register(email, "0744112233", $"AltUtilizator_{code}", "AltaParola2");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TestThatPasswordIsHashedInDatabase()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"sorin.mihai_{code}@gmail.com";
        string password = "MihaiSecret99!";

        _authService.Register(email, "0766112233", $"SorinM_{code}", password);
        var user = _userRepository.GetByEmail(email);

        user!.PasswordHash.Should().NotBe(password);
    }
}
