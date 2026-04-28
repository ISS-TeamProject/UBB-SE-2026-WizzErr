using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Services;

public class PricingServiceIntegrationTests : BaseIntegrationTest
{
    private const int UniqueCodeStartIndex = 0;
    private const int UniqueCodeLength = 4;
    private const string MihaiEmail = "mihai.popescu";
    private const string MihaiUsername = "MihaiPopescu";
    private const string MihaiPassword = "Parolalamos123!";
    private const string MihaiPhone = "0722112233";
    private const float TicketPrice1 = 100.0f;
    private const float TicketPrice2 = 100.0f;
    private const int MembershipId = 1;
    private const string MembershipName = "Premium";
    private readonly IPricingService _pricingService;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;

    public PricingServiceIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, _membershipRepository);
        _pricingService = new PricingService();
    }

    [Fact]
    public void PricingCalculation_WithMembership_WorksEndToEnd()
    {
        var membership = _membershipRepository.GetAllMemberships().First();
        membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();

        var uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        var user = new User
        {
            Email = $"{MihaiEmail}_{uniqueCode}@gmail.com",
            Username = $"{MihaiUsername}_{uniqueCode}",
            Phone = MihaiPhone,
            PasswordHash = MihaiPassword,
            Membership = membership
        };
        _userRepository.AddUser(user);
        var databaseUser = _userRepository.GetByEmail(user.Email);
        databaseUser!.Membership = membership;

        var flight = FlightFixture.CreateValidTestFlight();
        var ticket1 = new Ticket { Price = TicketPrice1, SelectedAddOns = new List<AddOn>() };
        var ticket2 = new Ticket { Price = TicketPrice2, SelectedAddOns = new List<AddOn>() };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, databaseUser, tickets);

        breakdown.BasePriceTotal.Should().Be(200.0f);
        breakdown.FinalTotal.Should().BeLessThanOrEqualTo(200.0f);
        breakdown.MembershipSavings.Should().BeGreaterThanOrEqualTo(0);
    }
}



