using FluentAssertions;
using Moq;
using System;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;
using Xunit;

namespace TicketManager.Tests.Unit.Services;

public class CancellationServiceTests
{
    private const int DefaultTicketId = 1;
    private const int FutureFlightDaysOffset = 5;
    private const int PastFlightDaysOffset = -1;
    private const string ActiveStatus = "Active";
    private const string CancelledStatus = "Cancelled";
    private const string AlreadyCancelledMessage = "already cancelled";
    private const string PastFlightMessage = "in the past";

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
        var futureDate = DateTime.Now.AddDays(FutureFlightDaysOffset);
        var flight = FlightFixture.CreateValidTestFlight(departureTime: futureDate);
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = ActiveStatus, Flight = flight };

        var (canCancelResult, cancelReason) = _cancellationService.CanCancelTicket(ticket);

        canCancelResult.Should().BeTrue();
        cancelReason.Should().BeEmpty();
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenTicketAlreadyCancelled()
    {
        var flight = FlightFixture.CreateValidTestFlight();
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = CancelledStatus, Flight = flight };

        var (canCancelResult, cancelReason) = _cancellationService.CanCancelTicket(ticket);

        canCancelResult.Should().BeFalse();
        cancelReason.Should().Contain(AlreadyCancelledMessage);
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenFlightDateIsInPast()
    {
        var pastDate = DateTime.Now.AddDays(PastFlightDaysOffset);
        var flight = FlightFixture.CreateValidTestFlight(departureTime: pastDate);
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = ActiveStatus, Flight = flight };

        var (canCancelResult, cancelReason) = _cancellationService.CanCancelTicket(ticket);

        canCancelResult.Should().BeFalse();
        cancelReason.Should().Contain(PastFlightMessage);
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenTicketIsNull()
    {
        var (canCancelResult, cancelReason) = _cancellationService.CanCancelTicket(null!);

        canCancelResult.Should().BeFalse();
        cancelReason.Should().Be("Ticket not found.");
    }
}


