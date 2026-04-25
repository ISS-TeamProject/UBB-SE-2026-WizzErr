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
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = "Active", Flight = flight };

        var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);
        canCancel.Should().BeTrue();
        reason.Should().BeEmpty();
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenTicketAlreadyCancelled()
    {
        var flight = FlightFixture.CreateValidTestFlight();
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = "Cancelled", Flight = flight };
        var (canCancel, reason) = _cancellationService.CanCancelTicket(ticket);

        canCancel.Should().BeFalse();
        reason.Should().Contain("already cancelled");
    }

    [Fact]
    public void TestThatCanCancelTicketReturnsFalseWhenFlightDateIsInPast()
    {

        var pastDate = DateTime.Now.AddDays(PastFlightDaysOffset);
        var flight = FlightFixture.CreateValidTestFlight(departureTime: pastDate);
        var ticket = new Ticket { TicketId = DefaultTicketId, Status = "Active", Flight = flight };


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

}

