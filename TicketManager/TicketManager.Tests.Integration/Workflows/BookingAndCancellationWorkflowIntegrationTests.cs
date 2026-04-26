using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Workflows;

public class BookingAndCancellationWorkflowIntegrationTests : BaseIntegrationTest
{
    private readonly IUserRepository _userRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IAddOnRepository _addOnRepository;
    private readonly AuthService _authService;
    private readonly BookingService _bookingService;
    private readonly PricingService _pricingService;
    private readonly CancellationService _cancellationService;

    public BookingAndCancellationWorkflowIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        var membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, membershipRepository);
        _ticketRepository = new TicketRepository(databaseConnectionFactory);
        _addOnRepository = new AddOnRepository(databaseConnectionFactory);
        _authService = new AuthService(_userRepository);
        _bookingService = new BookingService(_ticketRepository, _addOnRepository);
        _pricingService = new PricingService();
        _cancellationService = new CancellationService(_ticketRepository);
    }

    [Fact]
    public async Task TestThatCompleteBookingWorkflowWithValidationSucceeds()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var email = $"rezervare.zbor_{code}@gmail.com";
        var password = "ParolaRezervare123!";

        _authService.Register(email, "0722112233", $"Utilizator_{code}", password);
        var user = _authService.Login(email, password);

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var passengers = PassengerDataFixture.CreateValidPassengerList(2);

        var validationResult = _bookingService.ValidatePassengers(passengers);
        validationResult.Should().BeEmpty();

        var tickets = _bookingService.CreateTickets(flight, user, passengers, 100.0f);
        tickets.Should().HaveCount(2);

        var saveResult = await _bookingService.SaveTicketsAsync(tickets);
        saveResult.Should().BeTrue();

        var userTickets = _ticketRepository.GetTicketsByUserId(user.UserId);
        userTickets.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestThatUserCannotSaveTicketsWithDuplicateSeatsInSameRequest()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var email = $"locuri.duplicate_{code}@gmail.com";
        _authService.Register(email, "0722112233", $"Gigel_{code}", "ParolaGigel123!");
        var user = _authService.Login(email, "ParolaGigel123!");

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var ticket1 = new Ticket { Flight = flight, User = user, Seat = "1A", Price = 100.0f, Status = "Active", PassengerFirstName = "Gigel", PassengerLastName = "Frone" };
        var ticket2 = new Ticket { Flight = flight, User = user, Seat = "1A", Price = 100.0f, Status = "Active", PassengerFirstName = "Vasile", PassengerLastName = "Traian" };

        var saveTicketsResult = await _bookingService.SaveTicketsAsync(new List<Ticket> { ticket1, ticket2 });

        saveTicketsResult.Should().BeFalse();
    }

    [Fact]
    public void TestThatUserCanValidateCancellationBeforeCancellingTicket()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var email = $"anulare.zbor_{code}@gmail.com";
        _authService.Register(email, "0722112233", $"Anulare_{code}", "ParolaAnulare123!");
        var user = _authService.Login(email, "ParolaAnulare123!");

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var ticket = new Ticket
        {
            Flight = flight,
            User = user,
            Seat = $"{code}_3D",
            Price = 150.0f,
            Status = "Active",
            PassengerFirstName = "Marius",
            PassengerLastName = "Lacatus",
            PassengerEmail = email
        };
        _ticketRepository.AddTicket(ticket);

        var createdTicket = _ticketRepository.GetTicketsByUserId(user.UserId).First();

        var (canCancel, reason) = _cancellationService.CanCancelTicket(createdTicket);
        canCancel.Should().BeTrue();
        reason.Should().BeEmpty();

        _cancellationService.CancelTicket(createdTicket.TicketId);

        var cancelledTicket = _ticketRepository.GetTicketsByUserId(user.UserId).First();
        cancelledTicket.Status.Should().Be("Cancelled");

        var (canCancelAgain, reasonAgain) = _cancellationService.CanCancelTicket(cancelledTicket);
        canCancelAgain.Should().BeFalse();
        reasonAgain.Should().Contain("already cancelled");
    }
}



