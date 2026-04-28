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
    private const string ActiveStatus = "Active";
    private const string Seat1A = "1A";
    private const string Seat1C = "1C";
    private const string Seat1D = "1D";
    private const string Seat1F = "1F";
    private const string Seat1B = "1B";
    private const string Seat2B = "2B";
    private const string invalidParameter = "NotAnArray";
    private const int ExactMultipleCapacity = 180;
    private const int PartialMultipleCapacity = 182;
    private const int MinimumFlightCapacity = 6;
    private const int ExpectedExactMultipleRows = 30;
    private const int ExpectedPartialMultipleRows = 31;
    private const int ExpectedMinimumCapacityRows = 1;
    private const int Column0Index = 0;
    private const int Column2Index = 2;
    private const int Column4Index = 4;
    private const int Column6Index = 6;
    private const int ExpectedExactMultipleLayoutCount = 180;
    private const int ExpectedPartialMultipleLayoutCount = 186;

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

        var tickets = _bookingService.CreateTickets(flight, user, passengers, DefaultBasePrice);

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
        var passenger = new PassengerData { LastName = "Pop", SelectedSeat = Seat1A };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("first name is required");
    }

    [Fact]
    public void TestThatValidatePassengersFailsWhenLastNameIsMissing()
    {
        var passenger = new PassengerData { FirstName = "Vasile", SelectedSeat = Seat1A };
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
        var ticket1 = new Ticket { Seat = Seat1A, Price = StandardTicketPrice, Status = ActiveStatus };
        var ticket2 = new Ticket { Seat = Seat1A, Price = StandardTicketPrice, Status = ActiveStatus };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var saveTicketsSucceeded = await _bookingService.SaveTicketsAsync(tickets);

        saveTicketsSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task TestThatSaveTicketsAsyncReturnsTrueWhenAllTicketsValid()
    {
        _mockTicketRepository.Setup(mockTicketRepository => mockTicketRepository.IsSeatAvailable(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockTicketRepository.Setup(mockTicketRepository => mockTicketRepository.SaveTicketsWithAddOnsAsync(It.IsAny<List<Ticket>>())).ReturnsAsync(true);

        var ticket1 = new Ticket { Seat = Seat1A, Price = StandardTicketPrice, Status = ActiveStatus };
        var ticket2 = new Ticket { Seat = Seat1B, Price = StandardTicketPrice, Status = ActiveStatus };
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
        var passenger = new PassengerData { FirstName = "Ion", LastName = "Pop", SelectedSeat = Seat1A, Email = "not-an-email" };
        var validationErrorMessage = _bookingService.ValidatePassengers(new List<PassengerData> { passenger });
        validationErrorMessage.Should().Contain("email format is invalid");
    }

    [Fact]
    public void TestThatParseBookingParametersReturnsParsedResultWithArguments()
    {
        var defaultFlight = new Flight { FlightId = DefaultFlightId };
        var defaultTestUser = UserFixture.CreateValidTestUser();
        object[] bookingArguments = { defaultFlight, defaultTestUser, NormalRequestedPassengers };

        var parsedBookingParameters = _bookingService.ParseBookingParameters(bookingArguments);

        parsedBookingParameters.Flight.Should().Be(defaultFlight);
        parsedBookingParameters.User.Should().Be(defaultTestUser);
        parsedBookingParameters.RequestedPassengers.Should().Be(NormalRequestedPassengers);
    }

    [Fact]
    public void TestThatStorePendingBookingStoresPendingBooking()
    {
        var defaultFlight = new Flight { FlightId = DefaultFlightId };

        _bookingService.StorePendingBooking(defaultFlight, NormalRequestedPassengers);

        UserSession.PendingBookingParameters.Should().NotBeNull();
        UserSession.PendingBookingParameters.Should().HaveCount(2);
        UserSession.PendingBookingParameters[0].Should().Be(defaultFlight);
        UserSession.PendingBookingParameters[1].Should().Be(NormalRequestedPassengers);
    }

    [Fact]
    public void TestThatApplySeatSelectionRemovesSeatIfAlreadyAssigned()
    {
        var seats = new List<string> { Seat1A, Seat1B };

        var updated = _bookingService.ApplySeatSelection(seats, 0, Seat1A);

        updated[0].Should().BeEmpty();
        updated[1].Should().Be(Seat1B);
    }

    [Fact]
    public void TestThatApplySeatSelectionAssignsSeatAndClearsDuplicate()
    {
        var seats = new List<string> { Seat1A, Seat2B };

        var updated = _bookingService.ApplySeatSelection(seats, 1, Seat1A);

        updated[0].Should().BeEmpty();
        updated[1].Should().Be(Seat1A);
    }

    [Fact]
    public void TestThatApplyAddOnUpdatesCorrectlyAdjustsList()
    {
        var priorityBoarding = new AddOn { AddOnId = 1, Name = "Priority Boarding" };
        var extraLuggage = new AddOn { AddOnId = 2, Name = "Extra Luggage" };

        var currentAddOns = new List<AddOn> { priorityBoarding };
        var toAdd = new List<AddOn> { extraLuggage };
        var toRemove = new List<AddOn> { priorityBoarding };

        _bookingService.ApplyAddOnUpdates(currentAddOns, toAdd, toRemove);

        currentAddOns.Should().Contain(extraLuggage);
        currentAddOns.Should().NotContain(priorityBoarding);
    }

    [Fact]
    public void TestThatGetInitialPassengerCountReturnsMinValue()
    {
        var initialPassengerCountRequested = _bookingService.GetInitialPassengerCount(ExpectedMaxPassengers, NormalRequestedPassengers);
        initialPassengerCountRequested.Should().Be(NormalRequestedPassengers);

        var limitedCount = _bookingService.GetInitialPassengerCount(2, 5);
        limitedCount.Should().Be(2);

        var fallbackCount = _bookingService.GetInitialPassengerCount(5, 0);
        fallbackCount.Should().Be(1);
    }

    [Fact]
    public void TestThatParseBookingParametersHandlesNullOrInvalidObject()
    {
        var parsedBookingParameters = _bookingService.ParseBookingParameters(invalidParameter);

        parsedBookingParameters.Flight.Should().BeNull();
        parsedBookingParameters.RequestedPassengers.Should().Be(ZeroRequestedPassengers);
    }

    [Fact]
    public void TestThatParseBookingParametersHandlesOnlyFlightArgument()
    {
        var defaultFlight = new Flight { FlightId = DefaultFlightId };
        object[] bookingArguments = { defaultFlight };

        var parsedBookingParameters = _bookingService.ParseBookingParameters(bookingArguments);

        parsedBookingParameters.Flight.Should().Be(defaultFlight);
        parsedBookingParameters.RequestedPassengers.Should().Be(ZeroRequestedPassengers);
    }

    [Fact]
    public void TestThatParseBookingParametersHandlesFlightAndPassengerCountArgument()
    {
        var defaultFlight = new Flight { FlightId = DefaultFlightId };
        object[] bookingArguments = { defaultFlight, NormalRequestedPassengers };

        var parsedBookingParameters = _bookingService.ParseBookingParameters(bookingArguments);

        parsedBookingParameters.Flight.Should().Be(defaultFlight);
        parsedBookingParameters.RequestedPassengers.Should().Be(NormalRequestedPassengers);
    }

    [Fact]
    public void TestThatParseBookingParametersFallsBackToUserSession()
    {
        var defaultSessionUser = UserFixture.CreateValidTestUser();
        UserSession.CurrentUser = defaultSessionUser;
        var defaultFlight = new Flight { FlightId = DefaultFlightId };
        object[] bookingArguments = { defaultFlight };

        var parsedBookingParameters = _bookingService.ParseBookingParameters(bookingArguments);

        parsedBookingParameters.User.Should().Be(defaultSessionUser);

        UserSession.CurrentUser = null;
    }

    [Fact]
    public void TestThatBuildSeatMapLayoutCalculatesRowsCorrectlyForExactMultiple()
    {
        var (layout, rowCount) = _bookingService.BuildSeatMapLayout(ExactMultipleCapacity);

        rowCount.Should().Be(ExpectedExactMultipleRows);
        layout.Should().HaveCount(ExpectedExactMultipleLayoutCount);
    }

    [Fact]
    public void TestThatBuildSeatMapLayoutCalculatesRowsCorrectlyForNonMultiple()
    {
        var (layout, rowCount) = _bookingService.BuildSeatMapLayout(PartialMultipleCapacity);

        rowCount.Should().Be(ExpectedPartialMultipleRows);
        layout.Should().HaveCount(ExpectedPartialMultipleLayoutCount);
    }

    [Fact]
    public void TestThatBuildSeatMapLayoutAssignsCorrectLabelsAndColumns()
    {
        var (layout, rowCount) = _bookingService.BuildSeatMapLayout(MinimumFlightCapacity);

        rowCount.Should().Be(ExpectedMinimumCapacityRows);

        layout[0].Label.Should().Be(Seat1A);
        layout[2].Label.Should().Be(Seat1C);
        layout[3].Label.Should().Be(Seat1D);
        layout[5].Label.Should().Be(Seat1F);

        layout[0].Column.Should().Be(Column0Index);
        layout[2].Column.Should().Be(Column2Index);
        layout[3].Column.Should().Be(Column4Index);
        layout[5].Column.Should().Be(Column6Index);
    }
}
