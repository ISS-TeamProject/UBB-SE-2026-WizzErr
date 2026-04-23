using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Services;

public class CancellationServiceTests
{
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly CancellationService _cancellationService;

    public CancellationServiceTests()
    {
        _mockTicketRepository = new Mock<ITicketRepository>();
        _cancellationService = new CancellationService(_mockTicketRepository.Object);
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsTrueForValidActiveTicket()
    {

        var futureDate = DateTime.Now.AddDays(5);
        var flight = FlightFixture.CreateValidTestFlight(departureTime: futureDate);
        var ticket = new Ticket { TicketId = 1, Status = "Active", Flight = flight };

        var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);
        canCancel.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenTicketAlreadyCancelled()
    {
        var flight = FlightFixture.CreateValidTestFlight();
        var ticket = new Ticket { TicketId = 1, Status = "Cancelled", Flight = flight };
        var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);

        canCancel.Should().BeFalse();
        reason.Should().Contain("already cancelled");
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenFlightDateIsInPast()
    {

        var pastDate = DateTime.Now.AddDays(-1);
        var flight = FlightFixture.CreateValidTestFlight(departureTime: pastDate);
        var ticket = new Ticket { TicketId = 1, Status = "Active", Flight = flight };


        var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);
        canCancel.Should().BeFalse();
        reason.Should().Contain("in the past");
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenTicketIsNull()
    {
        var (canCancel, reason) = _cancellationService.CanCancelTicket(null!);

        canCancel.Should().BeFalse();

        reason.Should().Be("Ticket not found.");
    }

    [Fact]
    public void TestThatCancelTicketUpdatesRepositoryWithCancelledStatus()
    {
        int ticketId = 5;
        _cancellationService.CancelTicket(ticketId);
        _mockTicketRepository.Verify(
            r => r.UpdateTicketStatus(ticketId, "Cancelled"),
            Times.Once,
            "Repository should be called with correct ticket ID and Cancelled status");
    }
}

