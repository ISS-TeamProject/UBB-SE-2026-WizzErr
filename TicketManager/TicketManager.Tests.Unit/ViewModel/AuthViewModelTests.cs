using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class AuthViewModelTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly AuthViewModel _viewModel;

    public AuthViewModelTests()
    {
        UserSession.CurrentUser = null;
        UserSession.PendingBookingParameters = null;
        _mockAuthService = new Mock<IAuthService>();
        _mockNavigationService = new Mock<INavigationService>();
        _viewModel = new AuthViewModel(_mockAuthService.Object, _mockNavigationService.Object);
    }

    [Fact]
    public void ActionCommand_LoginSuccess_NavigatesToFlightSearch()
    {
        var user = new User { UserId = 1, Email = "mihai.ionescu@gmail.com", Username = "MihaiI" };
        _mockAuthService.Setup(authReturningValidUser => authReturningValidUser.Login(It.IsAny<string>(), It.IsAny<string>())).Returns(user);
        _viewModel.IsLoginMode = true;
        _viewModel.EmailText = "mihai.ionescu@gmail.com";
        _viewModel.PasswordText = "Parola@Mihai123";

        _viewModel.ActionCommand.Execute(null);

        _mockNavigationService.Verify(navToFlightSearch => navToFlightSearch.NavigateTo(typeof(View.FlightSearchPage), null), Times.Once);
        _mockNavigationService.Verify(navToBookingPage => navToBookingPage.NavigateTo(typeof(View.BookingPage), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void ActionCommand_LoginFailure_DoesNotNavigate()
    {
        _mockAuthService.Setup(authFailingLogin => authFailingLogin.Login(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Invalid email or password."));
        _viewModel.IsLoginMode = true;
        _viewModel.EmailText = "andrei.popescu@yahoo.ro";
        _viewModel.PasswordText = "GresilaParola";

        _viewModel.ActionCommand.Execute(null);

        _mockNavigationService.Verify(navToAnyPageWithParams => navToAnyPageWithParams.NavigateTo(It.IsAny<Type>(), It.IsAny<object>()), Times.Never);
        _mockNavigationService.Verify(navToAnyPageWithoutParams => navToAnyPageWithoutParams.NavigateTo(It.IsAny<Type>(), null), Times.Never);
    }

    [Fact]
    public void ActionCommand_RegisterSuccess_SwitchesToLoginMode()
    {
        _mockAuthService.Setup(authSucceedingRegistration => authSucceedingRegistration.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        _viewModel.IsLoginMode = false;
        _viewModel.EmailText = "cristina.radu@gmail.com";
        _viewModel.PhoneText = "0722334455";
        _viewModel.UsernameText = "CristinaR";
        _viewModel.PasswordText = "Parola@Cristina456";

        _viewModel.ActionCommand.Execute(null);

        _viewModel.IsLoginMode.Should().BeTrue();
    }

    [Fact]
    public void ActionCommand_RegisterFailure_StaysInRegisterMode()
    {
        _mockAuthService.Setup(authFailingRegistration => authFailingRegistration.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Email already exists"));
        _viewModel.IsLoginMode = false;
        _viewModel.EmailText = "sorin.mihai@yahoo.ro";
        _viewModel.PhoneText = "0733445566";
        _viewModel.UsernameText = "SorinM";
        _viewModel.PasswordText = "Parola@Sorin789";

        _viewModel.ActionCommand.Execute(null);

        _viewModel.IsLoginMode.Should().BeFalse();
    }

    [Fact]
    public void ActionCommand_LoginWithPendingBooking_NavigatesToBooking()
    {
        var user = new User { UserId = 2, Email = "elena.popescu@gmail.com", Username = "ElenaP" };
        var pendingParams = new object[] { new Flight { FlightId = 1 }, 2 };
        _mockAuthService.Setup(authReturningValidUser => authReturningValidUser.Login(It.IsAny<string>(), It.IsAny<string>())).Returns(user);

        UserSession.PendingBookingParameters = pendingParams;
        _viewModel.IsLoginMode = true;
        _viewModel.EmailText = "elena.popescu@gmail.com";
        _viewModel.PasswordText = "Parola@Elena321";

        _viewModel.ActionCommand.Execute(null);

        _mockNavigationService.Verify(navToBookingPage => navToBookingPage.NavigateTo(typeof(View.BookingPage), pendingParams), Times.Once);
        _mockNavigationService.Verify(navToFlightSearch => navToFlightSearch.NavigateTo(typeof(View.FlightSearchPage), null), Times.Never);
    }

    [Fact]
    public void ToggleModeCommand_SwitchesModesAndClearsErrors()
    {
        _viewModel.IsLoginMode = true;
        _viewModel.ErrorMessage = "Eroare anterior";

        _viewModel.ToggleModeCommand.Execute(null);

        _viewModel.IsLoginMode.Should().BeFalse();
        _viewModel.ErrorMessage.Should().BeEmpty();
    }
}




