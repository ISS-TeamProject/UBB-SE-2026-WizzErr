using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Services;

public class BookingServiceTests
{
    private readonly Mock<ITicketRepository> _mockTicketRepository;
    private readonly Mock<IAddOnRepository> _mockAddOnRepository;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _mockTicketRepository = new Mock<ITicketRepository>();
        _mockAddOnRepository = new Mock<IAddOnRepository>();
        _bookingService = new BookingService(_mockTicketRepository.Object, _mockAddOnRepository.Object);
    }

    [Fact]
    public void TestThatCreateTicketsAssignsPassengerDataCorrectly()
    {
        var flight = new Flight { FlightId = 1 };
        var user = UserFixture.CreateValidTestUser();
        var passenger = PassengerDataFixture.CreateValidPassengerData(
            firstName: "Ionel",
            lastName: "Gheorghe",
            email: "ionel.ghe@gmail.com");
        var passengers = new List<PassengerData> { passenger };
        float basePrice = 150.0f;

        var tickets = _bookingService.CreateTickets(flight, user, passengers, basePrice);

        tickets[0].PassengerFirstName.Should().Be("Ionel");
        tickets[0].PassengerLastName.Should().Be("Gheorghe");
    }

    [Fact]
    public void TestThatValidatePassengersReturnsErrorWhenListIsEmpty()
    {
        var result = _bookingService.ValidatePassengers(new List<PassengerData>());
        result.Should().Be("At least one passenger is required.");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenFirstNameIsMissing()
    {
        var passenger = new PassengerData { LastName = "Pop", SelectedSeat = "1A" };
        var result = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        result.Should().Contain("first name is required");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenLastNameIsMissing()
    {
        var passenger = new PassengerData { FirstName = "Vasile", SelectedSeat = "1A" };
        var result = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        result.Should().Contain("last name is required");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenSeatIsNotSelected()
    {
        var passenger = new PassengerData { FirstName = "Vasile", LastName = "Pop", SelectedSeat = "" };
        var result = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        result.Should().Contain("please select a seat");
    }

    [Fact]
    public void TestThatCalculateMaxPassengersReturnsRemainingCapacity()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(200, 195, 0);
        maxPassengers.Should().Be(5);
    }

    [Fact]
    public void TestThatCalculateMaxPassengersCapsAtRequestedCount()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(200, 100, 5);
        maxPassengers.Should().Be(5);
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenTicketListIsNull()
    {
        var result = await _bookingService.SaveTicketsAsync(null!);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenTicketListIsEmpty()
    {
        var result = await _bookingService.SaveTicketsAsync(new List<Ticket>());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenDuplicateSeatsInRequest()
    {
        var ticket1 = new Ticket { Seat = "1A", Price = 100.0f, Status = "Active" };
        var ticket2 = new Ticket { Seat = "1A", Price = 100.0f, Status = "Active" };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var result = await _bookingService.SaveTicketsAsync(tickets);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsTrueWhenAllTicketsValid()
    {
            _mockTicketRepository.Setup(repoThatSuccessfullySavesTickets => repoThatSuccessfullySavesTickets.SaveTicketsWithAddOnsAsync(It.IsAny<List<Ticket>>())).ReturnsAsync(true);
        var ticket1 = new Ticket { Seat = "1A", Price = 100.0f, Status = "Active" };
        var ticket2 = new Ticket { Seat = "1B", Price = 100.0f, Status = "Active" };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var result = await _bookingService.SaveTicketsAsync(tickets);

        result.Should().BeTrue();
        _mockTicketRepository.Verify(repoToVerifySaveTickets => repoToVerifySaveTickets.SaveTicketsWithAddOnsAsync(It.IsAny<List<Ticket>>()), Times.Once);
    }

    [Fact]
    public void TestThatValidatePassengersAcceptsValidEmailWhenProvided()
    {
        var passenger = PassengerDataFixture.CreateValidPassengerData(email: "test@gmail.com");
        var result = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        result.Should().BeEmpty();
    }

    [Fact]
    public void TestThatValidatePassengersRejectsInvalidEmailWhenProvided()
    {
        var passenger = new PassengerData { FirstName = "Ion", LastName = "Pop", SelectedSeat = "1A", Email = "not-an-email" };
        var result = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        result.Should().Contain("email format is invalid");
    }
}
