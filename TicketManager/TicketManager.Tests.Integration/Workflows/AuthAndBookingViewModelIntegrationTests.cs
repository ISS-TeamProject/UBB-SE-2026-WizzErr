using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Integration.Workflows;

public class AuthAndBookingViewModelIntegrationTests : BaseIntegrationTest
{
    private readonly IUserRepository _userRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IAddOnRepository _addOnRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly AuthService _authService;
    private readonly BookingService _bookingService;
    private readonly PricingService _pricingService;
    private readonly NavigationService _navigationService;

    public AuthAndBookingViewModelIntegrationTests()
    {
        var dbFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _membershipRepository = new MembershipRepository(dbFactory);
        _userRepository = new UserRepository(dbFactory, _membershipRepository);
        _flightRepository = new FlightRepository(dbFactory);
        _ticketRepository = new TicketRepository(dbFactory);
        _addOnRepository = new AddOnRepository(dbFactory);

        _authService = new AuthService(_userRepository);
        _bookingService = new BookingService(_ticketRepository, _addOnRepository);
        _pricingService = new PricingService();
        _navigationService = new NavigationService();
    }

    [Fact]
    public void TestAuthViewModel_RegisterAndLoginFlow()
    {
        var authVM = new AuthViewModel(_authService, _navigationService);
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"vasile.mihai_{code}@gmail.com";
        string password = "Parola@Vasile123";

        authVM.IsLoginMode = false;
        authVM.EmailText = email;
        authVM.PhoneText = "0733445566";
        authVM.UsernameText = $"VasileM_{code}";
        authVM.PasswordText = password;

        authVM.ActionCommand.Execute(null);
        authVM.SuccessMessage.Should().Contain("Registration successful");

        authVM.IsLoginMode = true;
        authVM.EmailText = email;
        authVM.PasswordText = password;
        authVM.ErrorMessage = "";

        authVM.ActionCommand.Execute(null);
        authVM.IsAuthenticated.Should().BeTrue();
        authVM.AuthenticatedUser.Should().NotBeNull();
        authVM.AuthenticatedUser!.Email.Should().Be(email);
    }

    [Fact]
    public void TestAuthViewModel_LoginFails_WithInvalidPassword()
    {
        var authVM = new AuthViewModel(_authService, _navigationService);
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"georgeta.popescu_{code}@gmail.com";
        string correctPassword = "Parola@Georgeta456";

        _authService.Register(email, "0722556677", $"GeorgetaP_{code}", correctPassword);

        authVM.IsLoginMode = true;
        authVM.EmailText = email;
        authVM.PasswordText = "WrongPassword123";

        authVM.ActionCommand.Execute(null);
        authVM.IsAuthenticated.Should().BeFalse();
        authVM.ErrorMessage.Should().Contain("Invalid");
    }

    [Fact]
    public async Task TestBookingViewModel_InitializeAndUpdatePrices()
    {
        var bookingVM = new BookingViewModel(_bookingService, _pricingService, _navigationService);

        var user = new User { UserId = 1, Email = "test@gmail.com", Username = "test" };
        var flight = new Flight
        {
            FlightId = GetFirstAvailableFlightId(),
            Route = new Route { Capacity = 180, DepartureTime = DateTime.Now.AddDays(5), ArrivalTime = DateTime.Now.AddDays(5).AddHours(2) }
        };

        await bookingVM.InitializeAsync(flight, user);

        bookingVM.CurrentFlight.Should().Be(flight);
        bookingVM.CurrentUser.Should().Be(user);
        bookingVM.Passengers.Count.Should().Be(1);
    }

    [Fact]
    public async Task TestBookingViewModel_AddPassengers_UpdatesState()
    {
        var bookingVM = new BookingViewModel(_bookingService, _pricingService, _navigationService);

        var user = new User { UserId = 1, Email = "rares.ionescu@gmail.com", Username = "rares" };
        var flight = new Flight
        {
            FlightId = GetFirstAvailableFlightId(),
            Route = new Route { Capacity = 180, DepartureTime = DateTime.Now.AddDays(3), ArrivalTime = DateTime.Now.AddDays(3).AddHours(1) }
        };

        await bookingVM.InitializeAsync(flight, user, 3);

        bookingVM.Passengers.Count.Should().Be(3);
        bookingVM.CanAddPassenger.Should().BeFalse();
    }

    [Fact]
    public async Task TestBookingViewModel_RemovePassenger_UpdatesCapacity()
    {
        var bookingVM = new BookingViewModel(_bookingService, _pricingService, _navigationService);

        var user = new User { UserId = 1, Email = "adrian.stefan@gmail.com", Username = "adrian" };
        var flight = new Flight
        {
            FlightId = GetFirstAvailableFlightId(),
            Route = new Route { Capacity = 180, DepartureTime = DateTime.Now.AddDays(2), ArrivalTime = DateTime.Now.AddDays(2).AddHours(2) }
        };

        await bookingVM.InitializeAsync(flight, user, 2);
        int passengerCountBefore = bookingVM.Passengers.Count;

        var passengerToRemove = bookingVM.Passengers[0];
        bookingVM.RemovePassengerCommand.Execute(passengerToRemove);

        bookingVM.Passengers.Count.Should().Be(passengerCountBefore - 1);
        bookingVM.CanAddPassenger.Should().BeTrue();
    }

    [Fact]
    public void TestDashboardViewModel_LoadTickets_AfterBooking()
    {
        string code = Guid.NewGuid().ToString().Substring(0, 4);
        string email = $"cosmin.tudor_{code}@gmail.com";
        string password = "Parola@Cosmin789";

        _authService.Register(email, "0733667788", $"CosminT_{code}", password);
        var user = _authService.Login(email, password);

        UserSession.CurrentUser = user;
        var dashboardVM = new DashboardViewModel(
            new DashboardService(_ticketRepository),
            new CancellationService(_ticketRepository),
            _navigationService
        );

        dashboardVM.LoadUserTickets();

        dashboardVM.OnNavigatedTo().Should().BeTrue();
    }

    [Fact]
    public void TestDashboardViewModel_FilterTickets_ByUpcoming()
    {
        var user = new User { UserId = 1, Email = "test@gmail.com", Username = "test" };
        UserSession.CurrentUser = user;

        var dashboardVM = new DashboardViewModel(
            new DashboardService(_ticketRepository),
            new CancellationService(_ticketRepository),
            _navigationService
        );

        dashboardVM.SelectedTicketFilter = "Upcoming";

        dashboardVM.MyTickets.Should().NotBeNull();
    }
}
