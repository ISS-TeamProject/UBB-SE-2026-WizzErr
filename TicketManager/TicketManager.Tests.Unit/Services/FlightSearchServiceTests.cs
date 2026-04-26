using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Unit.Services;

public class FlightSearchServiceTests
{
    private const int FlightId1 = 1;
    private const int FlightId2 = 2;
    private const int FlightId3 = 3;
    private const int DefaultCapacity = 100;
    private const int OccupiedSeatsLow = 10;
    private const int SinglePassenger = 1;
    private const int GroupPassengers = 10;
    private const int DaysOffsetTarget = 3;
    private const int DaysOffsetNoMatch = 5;
    private const int ExpectedFlightsCountMatching = 2;
    private const int ExpectedFlightsCountFiltered = 1;
    private const int Flight1OccupiedSeats = 95;
    private const int Flight2OccupiedSeats = 99;
    private const int Flight3OccupiedSeats = 90;

    private readonly Mock<IFlightRepository> _mockFlightRepository;
    private readonly FlightSearchService _flightSearchService;

    public FlightSearchServiceTests()
    {
        _mockFlightRepository = new Mock<IFlightRepository>();
        _flightSearchService = new FlightSearchService(_mockFlightRepository.Object);
    }

    [Fact]
    public void TestThatSearchFlightsReturnsFlightsMatchingCriteria()
    {
        var flights = new List<Flight>
        {
            new Flight { FlightId = FlightId1, FlightNumber = "RO101", Route = new Route { Capacity = DefaultCapacity } },
            new Flight { FlightId = FlightId2, FlightNumber = "RO102", Route = new Route { Capacity = DefaultCapacity } }
        };
        _mockFlightRepository.Setup(repoWithMatchingFlights => repoWithMatchingFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(flights);
        _mockFlightRepository.Setup(repoWithAvailableSeats => repoWithAvailableSeats.GetOccupiedSeatCount(It.IsAny<int>())).Returns(OccupiedSeatsLow);

        var foundFlights = _flightSearchService.SearchFlights("Bucuresti", true, DateTime.Now.AddDays(DaysOffsetTarget), SinglePassenger);

        foundFlights.Should().NotBeNull();
        foundFlights.Should().HaveCount(ExpectedFlightsCountMatching);
    }

    [Fact]
    public void TestThatSearchFlightsReturnsEmptyListWhenNoMatches()
    {
        _mockFlightRepository.Setup(repoWithNoMatchingFlights => repoWithNoMatchingFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(new List<Flight>());

        var foundFlights = _flightSearchService.SearchFlights("Cluj-Napoca", true, DateTime.Now.AddDays(DaysOffsetNoMatch), SinglePassenger);

        foundFlights.Should().NotBeNull();
        foundFlights.Should().BeEmpty();
    }

    [Fact]
    public void TestThatSearchFlightsFiltersFlightsByCapacity()
    {
        var flight1 = new Flight { FlightId = FlightId1, FlightNumber = "RO101", Route = new Route { Capacity = DefaultCapacity } };
        var flight2 = new Flight { FlightId = FlightId2, FlightNumber = "RO102", Route = new Route { Capacity = DefaultCapacity } };
        var flight3 = new Flight { FlightId = FlightId3, FlightNumber = "RO103", Route = new Route { Capacity = DefaultCapacity } };
        var flights = new List<Flight> { flight1, flight2, flight3 };

        _mockFlightRepository.Setup(repoWithMultipleFlights => repoWithMultipleFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(flights);
        _mockFlightRepository.Setup(repoWithFlight1Seats => repoWithFlight1Seats.GetOccupiedSeatCount(FlightId1)).Returns(Flight1OccupiedSeats);
        _mockFlightRepository.Setup(repoWithFlight2Seats => repoWithFlight2Seats.GetOccupiedSeatCount(FlightId2)).Returns(Flight2OccupiedSeats);
        _mockFlightRepository.Setup(repoWithFlight3Seats => repoWithFlight3Seats.GetOccupiedSeatCount(FlightId3)).Returns(Flight3OccupiedSeats);

        var foundFlights = _flightSearchService.SearchFlights("location", true, null, GroupPassengers);

        foundFlights.Should().HaveCount(ExpectedFlightsCountFiltered);
        foundFlights.First().FlightId.Should().Be(FlightId3);
    }
}

