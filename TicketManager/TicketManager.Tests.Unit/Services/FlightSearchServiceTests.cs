using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Unit.Services;

public class FlightSearchServiceTests
{
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
            new Flight { FlightId = 1, FlightNumber = "RO101", Route = new Route { Capacity = 100 } },
            new Flight { FlightId = 2, FlightNumber = "RO102", Route = new Route { Capacity = 100 } }
        };
        _mockFlightRepository.Setup(repoWithMatchingFlights => repoWithMatchingFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(flights);
        _mockFlightRepository.Setup(repoWithAvailableSeats => repoWithAvailableSeats.GetOccupiedSeatCount(It.IsAny<int>())).Returns(10);

        var result = _flightSearchService.SearchFlights("Bucuresti", true, DateTime.Now.AddDays(3), 1);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void TestThatSearchFlightsReturnsEmptyListWhenNoMatches()
    {
        _mockFlightRepository.Setup(repoWithNoMatchingFlights => repoWithNoMatchingFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(new List<Flight>());

        var result = _flightSearchService.SearchFlights("Cluj-Napoca", true, DateTime.Now.AddDays(5), 1);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void TestThatSearchFlightsFiltersFlightsByCapacity()
    {
        var flight1 = new Flight { FlightId = 1, FlightNumber = "RO101", Route = new Route { Capacity = 100 } };
        var flight2 = new Flight { FlightId = 2, FlightNumber = "RO102", Route = new Route { Capacity = 100 } };
        var flight3 = new Flight { FlightId = 3, FlightNumber = "RO103", Route = new Route { Capacity = 100 } };
        var flights = new List<Flight> { flight1, flight2, flight3 };

        _mockFlightRepository.Setup(repoWithMultipleFlights => repoWithMultipleFlights.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(flights);
        _mockFlightRepository.Setup(repoWithFlight1Seats => repoWithFlight1Seats.GetOccupiedSeatCount(1)).Returns(95);
        _mockFlightRepository.Setup(repoWithFlight2Seats => repoWithFlight2Seats.GetOccupiedSeatCount(2)).Returns(99);
        _mockFlightRepository.Setup(repoWithFlight3Seats => repoWithFlight3Seats.GetOccupiedSeatCount(3)).Returns(90);

        var result = _flightSearchService.SearchFlights("location", true, null, 10);

        result.Should().HaveCount(1);
        result.First().FlightId.Should().Be(3);
    }
}
