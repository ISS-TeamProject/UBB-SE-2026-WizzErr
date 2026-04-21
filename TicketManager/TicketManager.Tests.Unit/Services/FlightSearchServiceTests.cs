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
            new Flight { FlightId = 1, FlightNr = "RO101", Route = new Route { Capacity = 100 } },
            new Flight { FlightId = 2, FlightNr = "RO102", Route = new Route { Capacity = 100 } }
        };
        _mockFlightRepository.Setup(r => r.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(flights);
        _mockFlightRepository.Setup(r => r.GetOccupiedSeatCount(It.IsAny<int>())).Returns(10);

        var result = _flightSearchService.SearchFlights("Bucuresti", "OneWay", DateTime.Now.AddDays(3), 1);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public void TestThatSearchFlightsReturnsEmptyListWhenNoMatches()
    {
        _mockFlightRepository.Setup(r => r.GetFlightsByRoute(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
            .Returns(new List<Flight>());

        var result = _flightSearchService.SearchFlights("Cluj-Napoca", "OneWay", DateTime.Now.AddDays(5), 1);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
