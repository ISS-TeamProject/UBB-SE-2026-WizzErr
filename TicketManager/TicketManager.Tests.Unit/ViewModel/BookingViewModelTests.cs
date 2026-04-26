using FluentAssertions;
using Moq;
using System.Collections.ObjectModel;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class BookingViewModelTests
{
    private const int MockedMaxPassengers = 5;
    private const int LimitedMaxPassengers = 2;
    private const int DefaultFlightCapacity = 180;
    private const int TestFlightId = 1;
    private const int TestUserId = 1;
    private const float BaseTicketPrice = 100.0f;
    private const int DefaultRequestedPassengers = 1;
    private const int EventWaitDelayMs = 50;
    private const int MaxEventWaitRetries = 10;

    private readonly Mock<IBookingService> _mockBookingService;
    private readonly Mock<IPricingService> _mockPricingService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly BookingViewModel _viewModel;

    public BookingViewModelTests()
    {
        UserSession.CurrentUser = null;
        UserSession.PendingBookingParameters = null;
        _mockBookingService = new Mock<IBookingService>();
        _mockPricingService = new Mock<IPricingService>();
        _mockNavigationService = new Mock<INavigationService>();
        _mockBookingService.Setup(serviceReturningMockedCapacity => serviceReturningMockedCapacity.CalculateMaxPassengers(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(MockedMaxPassengers);
        _viewModel = new BookingViewModel(_mockBookingService.Object, _mockPricingService.Object, _mockNavigationService.Object);
    }

    [Fact]
    public void AddPassengerCommand_AddsPassengerRespectingCapacity()
    {
        _viewModel.MaxPassengers = LimitedMaxPassengers;
        _viewModel.Passengers.Clear();

        _viewModel.AddPassengerCommand.Execute(null);
        _viewModel.Passengers.Count.Should().Be(1);

        _viewModel.AddPassengerCommand.Execute(null);
        _viewModel.Passengers.Count.Should().Be(2);

        _viewModel.AddPassengerCommand.Execute(null);
        _viewModel.Passengers.Count.Should().Be(2);
    }

    [Fact]
    public void RemovePassengerCommand_RemovesPassengerWhenMultipleExist()
    {
        var passenger1 = new PassengerFormViewModel();
        var passenger2 = new PassengerFormViewModel();
        _viewModel.Passengers.Clear();
        _viewModel.Passengers.Add(passenger1);
        _viewModel.Passengers.Add(passenger2);

        _viewModel.RemovePassengerCommand.Execute(passenger1);

        _viewModel.Passengers.Count.Should().Be(1);
        _viewModel.Passengers[0].Should().Be(passenger2);
    }

    [Fact]
    public void RemovePassengerCommand_DoesNotRemoveWhenOnlyOnePassenger()
    {
        var passenger = new PassengerFormViewModel();
        _viewModel.Passengers.Clear();
        _viewModel.Passengers.Add(passenger);

        _viewModel.RemovePassengerCommand.Execute(passenger);

        _viewModel.Passengers.Count.Should().Be(1);
    }

    [Fact]
    public async Task ConfirmBookingCommand_CallsServiceAndRaisesEvent()
    {
        var flight = new Flight { FlightId = TestFlightId, Route = new Route { Capacity = DefaultFlightCapacity } };
        var user = new User { UserId = TestUserId, Email = "andrei.tudor@gmail.com" };

        _mockBookingService.Setup(bookingServiceReturningEmptyAddOns => bookingServiceReturningEmptyAddOns.GetAvailableAddOnsAsync()).ReturnsAsync(new List<AddOn>());
        _mockBookingService.Setup(bookingServiceReturningEmptyOccupiedSeats => bookingServiceReturningEmptyOccupiedSeats.GetOccupiedSeatsAsync(It.IsAny<int>())).ReturnsAsync(new List<string>());
        _mockBookingService.Setup(bookingServiceReturningMaxPassengers => bookingServiceReturningMaxPassengers.CalculateMaxPassengers(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(MockedMaxPassengers);
        _mockBookingService.Setup(bookingServiceReturningCreatedTickets => bookingServiceReturningCreatedTickets.CreateTickets(It.IsAny<Flight>(), It.IsAny<User>(), It.IsAny<List<PassengerData>>(), It.IsAny<float>()))
            .Returns(new List<Ticket> { new Ticket() });
        _mockBookingService.Setup(bookingServiceReturningSuccessfulSave => bookingServiceReturningSuccessfulSave.SaveTicketsAsync(It.IsAny<List<Ticket>>())).ReturnsAsync(true);
        _mockBookingService.Setup(bookingServiceReturningValidPassengers => bookingServiceReturningValidPassengers.ValidatePassengers(It.IsAny<List<PassengerData>>())).Returns("");
        _mockPricingService.Setup(pricingServiceReturningBreakdown => pricingServiceReturningBreakdown.CalculatePriceBreakdown(It.IsAny<Flight>(), It.IsAny<User>(), It.IsAny<List<Ticket>>()))
            .Returns(new PriceBreakdown { FinalTotal = BaseTicketPrice });

        await _viewModel.InitializeAsync(flight, user, DefaultRequestedPassengers);

        var passenger = _viewModel.Passengers[0];
        passenger.FirstName = "Andrei";
        passenger.LastName = "Tudor";
        passenger.Email = "andrei@gmail.com";
        passenger.SelectedSeat = "1A";

        var bookingConfirmedRaised = false;
        _viewModel.BookingConfirmed += (sender, eventArgs) => bookingConfirmedRaised = true;

        _viewModel.ConfirmBookingCommand.Execute(null);

        int retries = MaxEventWaitRetries;
        while (!bookingConfirmedRaised && retries > 0)
        {
            await Task.Delay(EventWaitDelayMs);
            retries--;
        }

        _mockBookingService.Verify(bookingServiceToVerifySave => bookingServiceToVerifySave.SaveTicketsAsync(It.IsAny<List<Ticket>>()), Times.Once);
        bookingConfirmedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task OnNavigatedToAsync_RedirectsToAuthWhenNotAuthenticated()
    {
        UserSession.CurrentUser = null;
        var flight = new Flight { FlightId = TestFlightId, Route = new Route() };

        await _viewModel.OnNavigatedToAsync(new object[] { flight });

        _mockNavigationService.Verify(navServiceToVerifyAuthRedirect => navServiceToVerifyAuthRedirect.NavigateTo(typeof(View.AuthPage), null), Times.Once);
    }

    [Fact]
    public async Task OnNavigatedToAsync_ReturnsFalseWhenNoFlight()
    {
        var navigationResult = await _viewModel.OnNavigatedToAsync(new object?[] { null });

        navigationResult.Should().BeFalse();
    }
}

