using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class AuthViewModelTests
{
    private const int PrimaryTestUserId = 1;
    private const int SecondaryTestUserId = 2;
    private const int TestFlightId = 1;
    private const int RequestedPassengerCount = 2;
    private const string PrimaryUserEmail = "mihai.ionescu@gmail.com";
    private const string PrimaryUsername = "MihaiI";
    private const string PrimaryPassword = "Parola@Mihai123";
    private const string SecondaryUserEmail = "elena.popescu@gmail.com";
    private const string SecondaryUsername = "ElenaP";
    private const string SecondaryPassword = "Parola@Elena321";

    private readonly Mock<IAuthService> mockAuthentificationService;
    private readonly Mock<INavigationService> mockNavigationService;
    private readonly AuthViewModel viewModel;

    public AuthViewModelTests()
    {
        UserSession.CurrentUser = null;
        UserSession.PendingBookingParameters = null;
        mockAuthentificationService = new Mock<IAuthService>();
        mockNavigationService = new Mock<INavigationService>();
        viewModel = new AuthViewModel(mockAuthentificationService.Object, mockNavigationService.Object);
    }

    [Fact]
    public void ActionCommand_AuthenticationSuccess_NavigatesToFlightSearch()
    {
        var user = new User { UserId = PrimaryTestUserId, Email = PrimaryUserEmail, Username = PrimaryUsername };
        mockAuthentificationService.Setup(authReturningValidUser => authReturningValidUser.Login(It.IsAny<string>(), It.IsAny<string>())).Returns(user);
        viewModel.IsLoginMode = true;
        viewModel.EmailText = PrimaryUserEmail;
        viewModel.PasswordText = PrimaryPassword;

        viewModel.ActionCommand.Execute(null);

        mockNavigationService.Verify(navToFlightSearch => navToFlightSearch.NavigateTo(typeof(View.FlightSearchPage), null), Times.Once);
        mockNavigationService.Verify(navToBookingPage => navToBookingPage.NavigateTo(typeof(View.BookingPage), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void ActionCommand_AuthenticationFailure_DoesNotNavigate()
    {
        mockAuthentificationService.Setup(authFailingLogin => authFailingLogin.Login(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Invalid email or password."));
        viewModel.IsLoginMode = true;
        viewModel.EmailText = "andrei.popescu@yahoo.ro";
        viewModel.PasswordText = "GresilaParola";

        viewModel.ActionCommand.Execute(null);

        mockNavigationService.Verify(navToAnyPageWithParams => navToAnyPageWithParams.NavigateTo(It.IsAny<Type>(), It.IsAny<object>()), Times.Never);
        mockNavigationService.Verify(navToAnyPageWithoutParams => navToAnyPageWithoutParams.NavigateTo(It.IsAny<Type>(), null), Times.Never);
    }

    [Fact]
    public void ActionCommand_RegistrationSuccess_SwitchesToAuthenticationMode()
    {
        mockAuthentificationService.Setup(authSucceedingRegistration => authSucceedingRegistration.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        viewModel.IsLoginMode = false;
        viewModel.EmailText = "cristina.radu@gmail.com";
        viewModel.PhoneText = "0722334455";
        viewModel.UsernameText = "CristinaR";
        viewModel.PasswordText = "Parola@Cristina456";

        viewModel.ActionCommand.Execute(null);

        viewModel.IsLoginMode.Should().BeTrue();
    }

    [Fact]
    public void ActionCommand_RegistrationFailure_StaysInRegistrationMode()
    {
        mockAuthentificationService.Setup(authFailingRegistration => authFailingRegistration.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Email already exists"));
        viewModel.IsLoginMode = false;
        viewModel.EmailText = "sorin.mihai@yahoo.ro";
        viewModel.PhoneText = "0733445566";
        viewModel.UsernameText = "SorinM";
        viewModel.PasswordText = "Parola@Sorin789";

        viewModel.ActionCommand.Execute(null);

        viewModel.IsLoginMode.Should().BeFalse();
    }

    [Fact]
    public void ActionCommand_AuthenticationWithPendingBooking_NavigatesToBooking()
    {
        var user = new User { UserId = SecondaryTestUserId, Email = SecondaryUserEmail, Username = SecondaryUsername };
        var pendingParams = new object[] { new Flight { FlightId = TestFlightId }, RequestedPassengerCount };
        mockAuthentificationService.Setup(authReturningValidUser => authReturningValidUser.Login(It.IsAny<string>(), It.IsAny<string>())).Returns(user);

        UserSession.PendingBookingParameters = pendingParams;
        viewModel.IsLoginMode = true;
        viewModel.EmailText = SecondaryUserEmail;
        viewModel.PasswordText = SecondaryPassword;

        viewModel.ActionCommand.Execute(null);

        mockNavigationService.Verify(navToBookingPage => navToBookingPage.NavigateTo(typeof(View.BookingPage), pendingParams), Times.Once);
        mockNavigationService.Verify(navToFlightSearch => navToFlightSearch.NavigateTo(typeof(View.FlightSearchPage), null), Times.Never);
    }

    [Fact]
    public void ToggleModeCommand_Invoked_SwitchesModesAndClearsErrors()
    {
        viewModel.IsLoginMode = true;
        viewModel.ErrorMessage = "Eroare anterior";

        viewModel.ToggleModeCommand.Execute(null);

        viewModel.IsLoginMode.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeEmpty();
    }
}




