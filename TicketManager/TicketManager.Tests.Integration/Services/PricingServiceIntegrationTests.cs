using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Services;

public class PricingServiceIntegrationTests : BaseIntegrationTest
{
    private readonly IPricingService _pricingService;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;

    public PricingServiceIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _membershipRepository = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, _membershipRepository);
        _pricingService = new PricingService();
    }

    [Fact]
    public void TestThatPricingCalculationWithMembershipDiscountsWorksEndToEnd()
    {
        var membership = _membershipRepository.GetAllMemberships().First();
        membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();

        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var user = new User
        {
            Email = $"mihai.popescu_{code}@gmail.com",
            Username = $"MihaiPopescu_{code}",
            Phone = "0722112233",
            PasswordHash = "Parolalamos123!",
            Membership = membership
        };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);
        dbUser!.Membership = membership;

        var flight = FlightFixture.CreateValidTestFlight();
        var ticket1 = new Ticket { Price = 100.0f, SelectedAddOns = new List<AddOn>() };
        var ticket2 = new Ticket { Price = 100.0f, SelectedAddOns = new List<AddOn>() };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, dbUser, tickets);

        breakdown.BasePriceTotal.Should().Be(200.0f);
        breakdown.FinalTotal.Should().BeLessThanOrEqualTo(200.0f);
        breakdown.MembershipSavings.Should().BeGreaterThanOrEqualTo(0);
    }
}

