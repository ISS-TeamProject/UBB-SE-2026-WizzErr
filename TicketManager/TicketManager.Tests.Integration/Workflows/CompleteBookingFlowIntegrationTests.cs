using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Workflows;

public class CompleteBookingFlowIntegrationTests : BaseIntegrationTest
{
    private const int UniqueCodeStartIndex = 0;
    private const int UniqueCodeLength = 4;
    private const string DanEmail = "dan.ionescu";
    private const string DanUsername = "DanI";
    private const string DanPassword = "ParolaDan2024!";
    private const string DefaultPhone = "0722112233";
    private const float TargetPrice = 150.0f;
    private const float PriceMultiplier = 1.25f;
    private const int MembershipId = 1;
    private const string MembershipName = "Premium";
    private const int MembershipDiscount = 10;
    private const float TicketPrice = 100.0f;
    private const int SinglePassenger = 1;
    private const string DomainGmail = "@gmail.com";
    private readonly IUserRepository _userRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly AuthService _authentificationService;
    private readonly BookingService _bookingService;
    private readonly PricingService _pricingService;

    public CompleteBookingFlowIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, membershipRepository);
        _ticketRepository = new TicketRepository(databaseConnectionFactory);
        _authentificationService = new AuthService(_userRepository);
        _bookingService = new BookingService(_ticketRepository, new AddOnRepository(databaseConnectionFactory));
        _pricingService = new PricingService();
    }

    private Flight CreateFlightWithBasePrice(float targetPrice)
    {
        int minutes = (int)(targetPrice / PriceMultiplier);
        var now = DateTime.Now;
        return new Flight
        {
            FlightId = GetFirstAvailableFlightId(),
            Route = new Route { DepartureTime = now, ArrivalTime = now.AddMinutes(minutes) }
        };
    }

    [Fact]
    public async Task CompleteBookingFlow_ValidData_Succeeds()
    {
        string uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        string email = $"{DanEmail}_{uniqueCode}{DomainGmail}";
        string password = DanPassword;
        _authentificationService.Register(email, DefaultPhone, $"{DanUsername}_{uniqueCode}", password);

        var user = _authentificationService.Login(email, password);
        var flight = CreateFlightWithBasePrice(TargetPrice);
        var passengers = PassengerDataFixture.CreateValidPassengerList(SinglePassenger);

        var tickets = _bookingService.CreateTickets(flight, user, passengers, TargetPrice);
        var saveResult = await _bookingService.SaveTicketsAsync(tickets);

        saveResult.Should().BeTrue();
    }

    [Fact]
    public void CompleteBookingFlow_PremiumUser_GetsMembershipDiscount()
    {
        var membership = new Membership { MembershipId = MembershipId, Name = MembershipName, FlightDiscountPercentage = MembershipDiscount };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var flight = CreateFlightWithBasePrice(TicketPrice);
        var tickets = new List<Ticket> { new Ticket { Price = TicketPrice } };

        var priceBreakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        priceBreakdown.MembershipSavings.Should().BeGreaterThan(0);
        priceBreakdown.FinalTotal.Should().BeLessThan(TicketPrice);
    }
}


