using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Services;

public class CancellationServiceIntegrationTests : BaseIntegrationTest
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly CancellationService _cancellationService;

    public CancellationServiceIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _ticketRepository = new TicketRepository(dbFactory);
        var membershipRepo = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, membershipRepo);
        _cancellationService = new CancellationService(_ticketRepository);
    }

    [Fact]
    public void TestThatCompleteTicketCancellationWorkflowSucceeds()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var user = new User
        {
            Email = $"stefan.andrei_{code}@gmail.com",
            Username = $"StefanAndrei_{code}",
            Phone = "0722112233",
            PasswordHash = "ParolaStefan123!"
        };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);

        var flightId = GetFirstAvailableFlightId();
        var ticket = new Ticket
        {
            Flight = new Flight { FlightId = flightId },
            User = dbUser!,
            PassengerFirstName = "Stefan",
            PassengerLastName = "Andrei",
            PassengerEmail = dbUser!.Email,
            PassengerPhone = "0722112233",
            Seat = $"{code}_5C",
            Price = 150.0f,
            Status = "Active"
        };
        _ticketRepository.AddTicket(ticket);

        var createdTicket = _ticketRepository.GetTicketsByUserId(dbUser.UserId).First();

        var (canCancel, reason) = _cancellationService.CanCancelTicket(createdTicket);
        canCancel.Should().BeTrue();

        _cancellationService.CancelTicket(createdTicket.TicketId);

        var updatedTicket = _ticketRepository.GetTicketsByUserId(dbUser.UserId).First();
        updatedTicket.Status.Should().Be("Cancelled");
    }
}

