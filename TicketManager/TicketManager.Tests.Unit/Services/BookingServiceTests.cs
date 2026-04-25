using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;
using Xunit;

namespace TicketManager.Tests.Unit.Services;

public class BookingServiceTests
{
    private const int DefaultFlightId = 1;
    private const float DefaultBasePrice = 150.0f;
    private const float StandardTicketPrice = 100.0f;
    private const int LargeFlightCapacity = 200;
    private const int HighOccupiedSeats = 195;
    private const int ModerateOccupiedSeats = 100;
    private const int ZeroRequestedPassengers = 0;
    private const int NormalRequestedPassengers = 5;
    private const int ExpectedMaxPassengers = 5;

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
        var flight = new Flight { FlightId = DefaultFlightId };
        var user = UserFixture.CreateValidTestUser();
        var passenger = PassengerDataFixture.CreateValidPassengerData(
            firstName: "Ionel",
            lastName: "Gheorghe",
            email: "ionel.ghe@gmail.com");
        var passengers = new List<PassengerData> { passenger };
        float basePrice = DefaultBasePrice;

        var tickets = _bookingService.CreateTickets(flight, user, passengers, basePrice);

        tickets[0].PassengerFirstName.Should().Be("Ionel");
        tickets[0].PassengerLastName.Should().Be("Gheorghe");
    }

    [Fact]
    public void TestThatValidatePassengersReturnsErrorWhenListIsEmpty()
    {
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData>());
        validationErrorMessage.Should().Be("At least one passenger is required.");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenFirstNameIsMissing()
    {
        var passenger = new PassengerData { LastName = "Pop", SelectedSeat = "1A" };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("first name is required");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenLastNameIsMissing()
    {
        var passenger = new PassengerData { FirstName = "Vasile", SelectedSeat = "1A" };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("last name is required");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenSeatIsNotSelected()
    {
        var passenger = new PassengerData { FirstName = "Vasile", LastName = "Pop", SelectedSeat = "" };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("please select a seat");
    }

    [Fact]
    public void TestThatCalculateMaxPassengersReturnsRemainingCapacity()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(LargeFlightCapacity, HighOccupiedSeats, ZeroRequestedPassengers);
        maxPassengers.Should().Be(ExpectedMaxPassengers);
    }

    [Fact]
    public void TestThatCalculateMaxPassengersCapsAtRequestedCount()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(LargeFlightCapacity, ModerateOccupiedSeats, NormalRequestedPassengers);
        maxPassengers.Should().Be(ExpectedMaxPassengers);
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenTicketListIsNull()
    {
        var saveTicketsSucceeded = await _bookingService.SaveTicketsAsync(null!);
        saveTicketsSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenTicketListIsEmpty()
    {
        var saveTicketsSucceeded = await _bookingService.SaveTicketsAsync(new List<Ticket>());
        saveTicketsSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsFalseWhenDuplicateSeatsInRequest()
    {
        var ticket1 = new Ticket { Seat = "1A", Price = StandardTicketPrice, Status = "Active" };
        var ticket2 = new Ticket { Seat = "1A", Price = StandardTicketPrice, Status = "Active" };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var saveTicketsSucceeded = await _bookingService.SaveTicketsAsync(tickets);

        saveTicketsSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsTrueWhenAllTicketsValid()
    {
        _mockTicketRepository.Setup(mockTicketRepository => mockTicketRepository.IsSeatAvailable(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockTicketRepository.Setup(mockTicketRepository => mockTicketRepository.SaveTicketsWithAddOnsAsync(It.IsAny<List<Ticket>>())).ReturnsAsync(true);
        
        var ticket1 = new Ticket { Seat = "1A", Price = StandardTicketPrice, Status = "Active" };
        var ticket2 = new Ticket { Seat = "1B", Price = StandardTicketPrice, Status = "Active" };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var saveTicketsSucceeded = await _bookingService.SaveTicketsAsync(tickets);

        saveTicketsSucceeded.Should().BeTrue();
        _mockTicketRepository.Verify(mockTicketRepository => mockTicketRepository.SaveTicketsWithAddOnsAsync(It.IsAny<List<Ticket>>()), Times.Once);
    }

    [Fact]
    public void TestThatValidatePassengersAcceptsValidEmailWhenProvided()
    {
        var passenger = PassengerDataFixture.CreateValidPassengerData(email: "test@gmail.com");
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void TestThatValidatePassengersRejectsInvalidEmailWhenProvided()
    {
        var passenger = new PassengerData { FirstName = "Ion", LastName = "Pop", SelectedSeat = "1A", Email = "not-an-email" };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("email format is invalid");
    }
}

