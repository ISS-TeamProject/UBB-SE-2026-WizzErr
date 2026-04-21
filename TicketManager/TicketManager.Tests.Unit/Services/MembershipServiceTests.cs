using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Unit.Services;

public class MembershipServiceTests
{
    private readonly Mock<IMembershipRepository> _mockMembershipRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly MembershipService _service;

    public MembershipServiceTests()
    {
        _mockMembershipRepository = new Mock<IMembershipRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _service = new MembershipService(_mockUserRepository.Object, _mockMembershipRepository.Object);
    }

    [Fact]
    public void TestThatGetAllMembershipsReturnsExistingTiers()
    {
        var memberships = new List<Membership>
        {
            new Membership { MembershipId = 1, Name = "Argint", FlightDiscountPercentage = 5 },
            new Membership { MembershipId = 2, Name = "Aur", FlightDiscountPercentage = 15 }
        };
        _mockMembershipRepository.Setup(r => r.GetAllMemberships()).Returns(memberships);
        _mockMembershipRepository.Setup(r => r.GetAddonDiscounts(It.IsAny<int>())).Returns(new List<MembershipAddonDiscount>());

        var result = _service.GetAllMemberships();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void TestThatUpgradeUserMembershipCallsRepositoryWithCorrectId()
    {
        var membership = new Membership { MembershipId = 3, Name = "Premium Plus" };
        _mockMembershipRepository.Setup(r => r.GetMembershipById(3)).Returns(membership);
        _mockMembershipRepository.Setup(r => r.GetAddonDiscounts(3)).Returns(new List<MembershipAddonDiscount>());

        var result = _service.UpgradeUserMembership(10, 3);

        result.Name.Should().Be("Premium Plus");
        _mockUserRepository.Verify(r => r.UpdateUserMembership(10, 3), Times.Once);
    }

    [Fact]
    public void TestThatUpgradeUserMembershipReturnsNullIfNotFoundInRepo()
    {
        _mockMembershipRepository.Setup(r => r.GetMembershipById(99)).Returns((Membership)null);
        
        var result = _service.UpgradeUserMembership(1, 99);
        
        result.Should().BeNull();
    }
}
