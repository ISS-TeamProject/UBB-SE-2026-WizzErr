using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Tests.Integration.Repositories;

public class UserRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly IUserRepository _userRepository;

    public UserRepositoryIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepo = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, membershipRepo);
    }

    [Fact]
    public void TestThatUserCanBeAddedAndRetrieved()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var newUser = new User
        {
            Email = $"cristian.vlad_{code}@gmail.com",
            Username = $"CristiV_{code}",
            Phone = "0744112233",
            PasswordHash = "ParolaCristi99"
        };

        _userRepository.AddUser(newUser);
        var retrievedUser = _userRepository.GetByEmail(newUser.Email);

        retrievedUser.Should().NotBeNull();
        retrievedUser!.Username.Should().Be(newUser.Username);
    }

    [Fact]
    public void TestThatNonExistentUserReturnsNull()
    {
        var user = _userRepository.GetByEmail("nu.exista@exemplu.ro");
        user.Should().BeNull();
    }
}
