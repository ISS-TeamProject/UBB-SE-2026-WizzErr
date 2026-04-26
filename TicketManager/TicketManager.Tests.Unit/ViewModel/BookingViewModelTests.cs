using FluentAssertions;
using Moq;
using System.Collections.ObjectModel;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class BookingViewModelTests
{
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
        _mockBookingService.Setup(serviceReturningMockedCapacity => serviceReturningMockedCapacity.CalculateMaxPassengers(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(5);
        _viewModel = new BookingViewModel(_mockBookingService.Object, _mockPricingService.Object, _mockNavigationService.Object);
    }

    [Fact]
    public void AddPassengerCommand_AddsPassengerRespectingCapacity()
    {
        _viewModel.MaxPassengers = 2;
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
        var flight = new Flight { FlightId = 1, Route = new Route { Capacity = 180 } };
        var user = new User { UserId = 1, Email = "andrei.tudor@gmail.com" };

        _mockBookingService.Setup(bookingServiceReturningEmptyAddOns => bookingServiceReturningEmptyAddOns.GetAvailableAddOnsAsync()).ReturnsAsync(new List<AddOn>());
        _mockBookingService.Setup(bookingServiceReturningEmptyOccupiedSeats => bookingServiceReturningEmptyOccupiedSeats.GetOccupiedSeatsAsync(It.IsAny<int>())).ReturnsAsync(new List<string>());
        _mockBookingService.Setup(bookingServiceReturningMaxPassengers => bookingServiceReturningMaxPassengers.CalculateMaxPassengers(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(5);
        _mockBookingService.Setup(bookingServiceReturningCreatedTickets => bookingServiceReturningCreatedTickets.CreateTickets(It.IsAny<Flight>(), It.IsAny<User>(), It.IsAny<List<PassengerData>>(), It.IsAny<float>()))
            .Returns(new List<Ticket> { new Ticket() });
        _mockBookingService.Setup(bookingServiceReturningSuccessfulSave => bookingServiceReturningSuccessfulSave.SaveTicketsAsync(It.IsAny<List<Ticket>>())).ReturnsAsync(true);
        _mockBookingService.Setup(bookingServiceReturningValidPassengers => bookingServiceReturningValidPassengers.ValidatePassengers(It.IsAny<List<PassengerData>>())).Returns("");
        _mockPricingService.Setup(pricingServiceReturningBreakdown => pricingServiceReturningBreakdown.CalculatePriceBreakdown(It.IsAny<Flight>(), It.IsAny<User>(), It.IsAny<List<Ticket>>()))
            .Returns(new PriceBreakdown { FinalTotal = 100 });

        await _viewModel.InitializeAsync(flight, user, 1);

        var passenger = _viewModel.Passengers[0];
        passenger.FirstName = "Andrei";
        passenger.LastName = "Tudor";
        passenger.Email = "andrei@gmail.com";
        passenger.SelectedSeat = "1A";

        var bookingConfirmedRaised = false;
        _viewModel.BookingConfirmed += (s, e) => bookingConfirmedRaised = true;

        _viewModel.ConfirmBookingCommand.Execute(null);

        int retries = 10;
        while (!bookingConfirmedRaised && retries > 0)
        {
            await Task.Delay(50);
            retries--;
        }

        _mockBookingService.Verify(bookingServiceToVerifySave => bookingServiceToVerifySave.SaveTicketsAsync(It.IsAny<List<Ticket>>()), Times.Once);
        bookingConfirmedRaised.Should().BeTrue();
    }

    [Fact]
    public async Task OnNavigatedToAsync_RedirectsToAuthWhenNotAuthenticated()
    {
        UserSession.CurrentUser = null;
        var flight = new Flight { FlightId = 1, Route = new Route() };

        await _viewModel.OnNavigatedToAsync(new object[] { flight });

        _mockNavigationService.Verify(navServiceToVerifyAuthRedirect => navServiceToVerifyAuthRedirect.NavigateTo(typeof(View.AuthPage), null), Times.Once);
    }

    [Fact]
    public async Task OnNavigatedToAsync_ReturnsFalseWhenNoFlight()
    {
        var result = await _viewModel.OnNavigatedToAsync(new object?[] { null });

        result.Should().BeFalse();
    }
}
