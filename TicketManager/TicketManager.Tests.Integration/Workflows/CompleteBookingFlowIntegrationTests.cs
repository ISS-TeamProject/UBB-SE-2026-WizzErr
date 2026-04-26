using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Workflows;

public class CompleteBookingFlowIntegrationTests : BaseIntegrationTest
{
    private readonly IUserRepository _userRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly AuthService _authService;
    private readonly BookingService _bookingService;
    private readonly PricingService _pricingService;

    public CompleteBookingFlowIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, membershipRepository);
        _ticketRepository = new TicketRepository(databaseConnectionFactory);
        _authService = new AuthService(_userRepository);
        _bookingService = new BookingService(_ticketRepository, new AddOnRepository(databaseConnectionFactory));
        _pricingService = new PricingService();
    }

    private Flight CreateFlightWithBasePrice(float targetPrice)
    {
        int minutes = (int)(targetPrice / 1.25f);
        var now = DateTime.Now;
        return new Flight
        {
            FlightId = GetFirstAvailableFlightId(),
            Route = new Route { DepartureTime = now, ArrivalTime = now.AddMinutes(minutes) }
        };
    }

    [Fact]
    public async Task TestThatCompleteBookingFlowSucceeds()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"dan.ionescu_{code}@gmail.com";
        string pass = "ParolaDan2024!";
        _authService.Register(email, "0722112233", $"DanI_{code}", pass);

        var user = _authService.Login(email, pass);
        var flight = CreateFlightWithBasePrice(150.0f);
        var passengers = PassengerDataFixture.CreateValidPassengerList(1);

        var tickets = _bookingService.CreateTickets(flight, user, passengers, 150.0f);
        var saveResult = await _bookingService.SaveTicketsAsync(tickets);

        saveResult.Should().BeTrue();
    }

    [Fact]
    public void TestThatPremiumUserGetsMembershipDiscount()
    {
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = 10 };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var flight = CreateFlightWithBasePrice(100.0f);
        var tickets = new List<Ticket> { new Ticket { Price = 100.0f } };

        var priceBreakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        priceBreakdown.MembershipSavings.Should().BeGreaterThan(0);
        priceBreakdown.FinalTotal.Should().BeLessThan(100.0f);
    }
}


