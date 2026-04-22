using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Services;

public class BookingServiceIntegrationTests : BaseIntegrationTest
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAddOnRepository _addOnRepository;
    private readonly IUserRepository _userRepository;
    private readonly BookingService _bookingService;

    public BookingServiceIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _ticketRepository = new TicketRepository(dbFactory);
        _addOnRepository = new AddOnRepository(dbFactory);
        var membershipRepo = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, membershipRepo);
        _bookingService = new BookingService(_ticketRepository, _addOnRepository);
    }

    [Fact]
    public void TestThatTicketsCanBeCreatedAndSaved()
    {
        var flightId = GetFirstAvailableFlightId();
        var flight = new Flight { FlightId = flightId };
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var user = new User { Email = $"mrc.popa_{code}@gmail.com", Username = $"MirceaP_{code}", PasswordHash = "Mircea123!" };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);

        var passengers = new List<PassengerData> 
        { 
            new PassengerData { FirstName = "Mircea", LastName = "Popa", Email = user.Email, Phone = "0722334455", SelectedSeat = $"{code}_1A" }
        };

        var tickets = _bookingService.CreateTickets(flight, dbUser!, passengers, 150.0f);
        var saveResult = _bookingService.SaveTicketsAsync(tickets).Result;

        saveResult.Should().BeTrue();
        tickets.Should().HaveCount(1);
    }

    [Fact]
    public void TestThatAvailableAddOnsCanBeRetrieved()
    {
        var addOns = _bookingService.GetAvailableAddOnsAsync().Result;
        addOns.Should().NotBeNull();
    }

    [Fact]
    public void TestThatMaxPassengersIsCalculatedCorrectly()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(180, 50, 10);
        maxPassengers.Should().Be(10);
    }
}
