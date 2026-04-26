using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Workflows;

public class BookingAndCancellationWorkflowIntegrationTests : BaseIntegrationTest
{
    private const int UniqueCodeStartIndex = 0;
    private const int UniqueCodeLength = 4;
    private const float BasePrice = 100.0f;
    private const int TwoPassengers = 2;
    private const string ReservationEmail = "rezervare.zbor";
    private const string ReservationUsername = "Utilizator";
    private const string ReservationPassword = "ParolaRezervare123!";
    private const string ReservationPhone = "0722112233";
    private const string DuplicateSeatsEmail = "locuri.duplicate";
    private const string GigelUsername = "Gigel";
    private const string GigelPassword = "ParolaGigel123!";
    private const string GigelFirstName = "Gigel";
    private const string GigelLastName = "Frone";
    private const string VasileFirstName = "Vasile";
    private const string VasileLastName = "Traian";
    private const string Seat1A = "1A";
    private const string CancellationEmail = "anulare.zbor";
    private const string CancellationUsername = "Anulare";
    private const string CancellationPassword = "ParolaAnulare123!";
    private const string MariusFirstName = "Marius";
    private const string MariusLastName = "Lacatus";
    private const string CancellationSeatSuffix = "_3D";
    private const float CancellationPrice = 150.0f;
    private const string ActiveStatus = "Active";
    private const string CancelledStatus = "Cancelled";
    private const string DomainGmail = "@gmail.com";
    private const string AlreadyCancelledMessage = "already cancelled";
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
        var uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        var email = $"{ReservationEmail}_{uniqueCode}{DomainGmail}";
        var password = ReservationPassword;

        _authService.Register(email, ReservationPhone, $"{ReservationUsername}_{uniqueCode}", password);
        var user = _authService.Login(email, password);

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var passengers = PassengerDataFixture.CreateValidPassengerList(TwoPassengers);

        var validationResult = _bookingService.ValidatePassengers(passengers);
        validationResult.Should().BeEmpty();

        var tickets = _bookingService.CreateTickets(flight, user, passengers, BasePrice);
        tickets.Should().HaveCount(TwoPassengers);

        var saveResult = await _bookingService.SaveTicketsAsync(tickets);
        saveResult.Should().BeTrue();

        var userTickets = _ticketRepository.GetTicketsByUserId(user.UserId);
        userTickets.Should().HaveCount(TwoPassengers);
    }

    [Fact]
    public async Task TestThatUserCannotSaveTicketsWithDuplicateSeatsInSameRequest()
    {
        var uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        var email = $"{DuplicateSeatsEmail}_{uniqueCode}{DomainGmail}";
        _authService.Register(email, ReservationPhone, $"{GigelUsername}_{uniqueCode}", GigelPassword);
        var user = _authService.Login(email, GigelPassword);

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var ticket1 = new Ticket { Flight = flight, User = user, Seat = Seat1A, Price = BasePrice, Status = ActiveStatus, PassengerFirstName = GigelFirstName, PassengerLastName = GigelLastName };
        var ticket2 = new Ticket { Flight = flight, User = user, Seat = Seat1A, Price = BasePrice, Status = ActiveStatus, PassengerFirstName = VasileFirstName, PassengerLastName = VasileLastName };

        var saveTicketsResult = await _bookingService.SaveTicketsAsync(new List<Ticket> { ticket1, ticket2 });

        saveTicketsResult.Should().BeFalse();
    }

    [Fact]
    public void TestThatUserCanValidateCancellationBeforeCancellingTicket()
    {
        var uniqueCode = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        var email = $"{CancellationEmail}_{uniqueCode}{DomainGmail}";
        _authService.Register(email, ReservationPhone, $"{CancellationUsername}_{uniqueCode}", CancellationPassword);
        var user = _authService.Login(email, CancellationPassword);

        var flightId = GetFirstAvailableFlightId();
        var flight = FlightFixture.CreateValidTestFlight(flightId: flightId);
        var ticket = new Ticket
        {
            Flight = flight,
            User = user,
            Seat = $"{uniqueCode}{CancellationSeatSuffix}",
            Price = CancellationPrice,
            Status = ActiveStatus,
            PassengerFirstName = MariusFirstName,
            PassengerLastName = MariusLastName,
            PassengerEmail = email
        };
        _ticketRepository.AddTicket(ticket);

        var createdTicket = _ticketRepository.GetTicketsByUserId(user.UserId).First();

        var (canCancel, reason) = _cancellationService.CanCancelTicket(createdTicket);
        canCancel.Should().BeTrue();
        reason.Should().BeEmpty();

        _cancellationService.CancelTicket(createdTicket.TicketId);

        var cancelledTicket = _ticketRepository.GetTicketsByUserId(user.UserId).First();
        cancelledTicket.Status.Should().Be(CancelledStatus);

        var (canCancelAgain, reasonAgain) = _cancellationService.CanCancelTicket(cancelledTicket);
        canCancelAgain.Should().BeFalse();
        reasonAgain.Should().Contain(AlreadyCancelledMessage);
    }
}
