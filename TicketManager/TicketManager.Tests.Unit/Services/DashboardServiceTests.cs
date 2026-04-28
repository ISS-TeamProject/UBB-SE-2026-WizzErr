using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using Xunit;

namespace TicketManager.Tests.Unit.Services;

public class DashboardServiceTests
{
    private const int TargetUserId = 1;
    private const string PastFilter = "Past";
    private const string UpcomingFilter = "Upcoming";
    private const int PastDaysOffset = -5;
    private const int UpcomingDaysOffset = 5;

    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _mockTicketRepository = new Mock<ITicketRepository>();
        _dashboardService = new DashboardService(_mockTicketRepository.Object);
    }

    [Fact]
    public void TestThatGetUserTicketsExcludesTicketsWithNoFlight()
    {
        var ticketWithFlight = new Ticket { Flight = new Flight { Date = DateTime.Now.AddDays(UpcomingDaysOffset) } };
        var ticketWithoutFlight = new Ticket { Flight = null };

        _mockTicketRepository.Setup(repo => repo.GetTicketsByUserId(TargetUserId))
            .Returns(new List<Ticket> { ticketWithFlight, ticketWithoutFlight });

        var results = _dashboardService.GetUserTickets(TargetUserId, UpcomingFilter).ToList();

        results.Should().ContainSingle();
        results.First().Should().Be(ticketWithFlight);
    }

    [Fact]
    public void TestThatGetUserTicketsFiltersPastFlightsAndSortsDescending()
    {
        var olderFlight = new Flight { Date = DateTime.Now.AddDays(PastDaysOffset - 2) };
        var recentPastFlight = new Flight { Date = DateTime.Now.AddDays(PastDaysOffset) };
        var futureFlight = new Flight { Date = DateTime.Now.AddDays(UpcomingDaysOffset) };

        var ticketOlder = new Ticket { Flight = olderFlight };
        var ticketRecentPast = new Ticket { Flight = recentPastFlight };
        var ticketFuture = new Ticket { Flight = futureFlight };

        _mockTicketRepository.Setup(repo => repo.GetTicketsByUserId(TargetUserId))
            .Returns(new List<Ticket> { ticketOlder, ticketFuture, ticketRecentPast });

        var results = _dashboardService.GetUserTickets(TargetUserId, PastFilter).ToList();

        results.Should().HaveCount(2);
        results.First().Should().Be(ticketRecentPast);
        results.Last().Should().Be(ticketOlder);
    }

    [Fact]
    public void TestThatGetUserTicketsFiltersUpcomingFlightsAndSortsAscending()
    {
        var olderFlight = new Flight { Date = DateTime.Now.AddDays(PastDaysOffset) };
        var nearFutureFlight = new Flight { Date = DateTime.Now.AddDays(UpcomingDaysOffset) };
        var farFutureFlight = new Flight { Date = DateTime.Now.AddDays(UpcomingDaysOffset + 2) };

        var ticketOlder = new Ticket { Flight = olderFlight };
        var ticketNearFuture = new Ticket { Flight = nearFutureFlight };
        var ticketFarFuture = new Ticket { Flight = farFutureFlight };

        _mockTicketRepository.Setup(repo => repo.GetTicketsByUserId(TargetUserId))
            .Returns(new List<Ticket> { ticketFarFuture, ticketOlder, ticketNearFuture });

        var results = _dashboardService.GetUserTickets(TargetUserId, UpcomingFilter).ToList();

        results.Should().HaveCount(2);
        results.First().Should().Be(ticketNearFuture); 
        results.Last().Should().Be(ticketFarFuture);
    }
}