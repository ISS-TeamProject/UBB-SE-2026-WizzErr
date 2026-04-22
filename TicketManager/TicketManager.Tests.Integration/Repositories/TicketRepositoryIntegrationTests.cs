using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Tests.Integration.Repositories;

public class TicketRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public TicketRepositoryIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _ticketRepository = new TicketRepository(dbFactory);
        var membershipRepo = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, membershipRepo);
    }

    [Fact]
    public void TestThatTicketCanBeCreatedAndRetrievedByUserId()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var user = new User { Email = $"adina.raducu_{code}@gmail.com", Username = $"AdinaR_{code}", PasswordHash = "ParolaAdina1" };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);

        var flightId = GetFirstAvailableFlightId();
        var seat = $"{code}_14F";
        var ticket = new Ticket
        {
            Flight = new Flight { FlightId = flightId },
            User = dbUser!,
            PassengerFirstName = "Adina",
            PassengerLastName = "Raducu",
            PassengerEmail = dbUser!.Email,
            PassengerPhone = "0755667788",
            Seat = seat,
            Price = 175.0f,
            Status = "Active"
        };

        _ticketRepository.AddTicket(ticket);
        var userTickets = _ticketRepository.GetTicketsByUserId(dbUser.UserId);

        userTickets.Should().Contain(t => t.Seat == seat);
    }

    [Fact]
    public void TestThatTicketStatusCanBeUpdated()
    {
        var code = Guid.NewGuid().ToString().Substring(0, 4);
        var user = new User { Email = $"george.popa_{code}@gmail.com", Username = $"GeorgeP_{code}", PasswordHash = "ParolaG99" };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);

        var ticket = new Ticket { Flight = new Flight { FlightId = GetFirstAvailableFlightId() }, User = dbUser!, Seat = $"{code}_2B", Status = "Active", PassengerFirstName = "George", PassengerLastName = "Popa", Price = 100 };
        _ticketRepository.AddTicket(ticket);

        _ticketRepository.UpdateTicketStatus(ticket.TicketId, "Cancelled");
        var updatedTickets = _ticketRepository.GetTicketsByUserId(dbUser!.UserId);
        
        updatedTickets.First(t => t.TicketId == ticket.TicketId).Status.Should().Be("Cancelled");
    }
}
