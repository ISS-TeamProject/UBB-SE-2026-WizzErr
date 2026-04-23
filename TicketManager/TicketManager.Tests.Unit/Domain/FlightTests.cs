using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Domain;

public class FlightTests
{
    [Fact]
    public void TestThatGetBasePriceCalculatesPriceBasedOnDuration()
    {
        var flight = FlightFixture.CreateFlightWithDurationMinutes(32);
        var result = flight.GetBasePrice();
        result.Should().Be(40.0f);
    }

    [Fact]
    public void TestThatGetBasePriceEnforcesMinimumFlightPrice()
    {
        var flight = FlightFixture.CreateFlightWithShortDuration();
        var result = flight.GetBasePrice();
        result.Should().Be(40.0f);
    }

    [Fact]
    public void TestThatGetBasePriceCalculatesCorrectlyForLongFlights()
    {
        var flight = FlightFixture.CreateFlightWithLongDuration();
        var result = flight.GetBasePrice();
        result.Should().Be(600.0f);
    }

    [Fact]
    public void TestThatGetBasePriceReturnsZeroWhenRouteIsNull()
    {
        var flight = FlightFixture.CreateFlightWithoutRoute();
        var result = flight.GetBasePrice();
        result.Should().Be(0f);
    }

    [Fact]
    public void TestThatGetBasePriceCalculatesCorrectlyForExactMinimumDuration()
    {
        var flight = FlightFixture.CreateFlightWithDurationMinutes(32);
        var result = flight.GetBasePrice();
        result.Should().Be(40.0f);
    }
}

