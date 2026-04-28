using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class DashboardViewModelTests
{
    private const int ActiveTicketId = 1;
    private const int CancelledTicketId = 1;
    private const int TargetTicketIdToCancel = 5;
    private const int PendingTicketId = 3;
    private const int TestUserId = 1;
    private const string TestEmail = "bogdan.ionescu@gmail.com";
    private const string UpcomingFilter = "Upcoming";
    private const string ActiveStatus = "Active";
    private const string CancelledStatus = "Cancelled";
    private const string CancellationReasonMessage = "Cannot cancel within 24 hours of departure";

    private readonly Mock<IDashboardService> _mockDashboardService;
    private readonly Mock<ICancellationService> _mockCancellationService;
    private readonly Mock<INavigationService> _mockNavigationService;
    private readonly DashboardViewModel _viewModel;

    public DashboardViewModelTests()
    {
        _mockDashboardService = new Mock<IDashboardService>();
        _mockCancellationService = new Mock<ICancellationService>();
        _mockNavigationService = new Mock<INavigationService>();
        _viewModel = new DashboardViewModel(_mockDashboardService.Object, _mockCancellationService.Object, _mockNavigationService.Object);
    }

    [Fact]
    public void CancelTicketCommand_CancelableTicket_SetsPending()
    {
        var ticket = new Ticket { TicketId = ActiveTicketId, Status = ActiveStatus };
        _mockCancellationService.Setup(serviceAllowingCancel => serviceAllowingCancel.CanCancelTicket(ticket)).Returns((true, ""));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().Be(ticket);
    }

    [Fact]
    public void CancelTicketCommand_NotCancelableTicket_SetsCancellationFailed()
    {
        var ticket = new Ticket { TicketId = ActiveTicketId, Status = ActiveStatus };
        _mockCancellationService.Setup(serviceDenyingCancel => serviceDenyingCancel.CanCancelTicket(ticket))
            .Returns((false, CancellationReasonMessage));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.CancellationSucceeded.Should().BeFalse();
        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void CancelTicketCommand_CancelledTicket_Ignores()
    {
        var ticket = new Ticket { TicketId = CancelledTicketId, Status = CancelledStatus };

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void ConfirmCancellation_Invoked_CallsServiceAndClearsState()
    {
        UserSession.CurrentUser = new User { UserId = TestUserId, Email = TestEmail };
        var ticket = new Ticket { TicketId = TargetTicketIdToCancel, Status = ActiveStatus };
        _viewModel.PendingCancelTicket = ticket;
        _mockDashboardService.Setup(dashboardServiceReturningNoTickets => dashboardServiceReturningNoTickets.GetUserTickets(It.IsAny<int>(), It.IsAny<string>())).Returns(new List<Ticket>());

        _viewModel.ConfirmCancellation();

        _mockCancellationService.Verify(cancellationServiceToVerifyCancel => cancellationServiceToVerifyCancel.CancelTicket(TargetTicketIdToCancel), Times.Once);
        _viewModel.PendingCancelTicket.Should().BeNull();
        _viewModel.CancellationSucceeded.Should().BeTrue();
    }

    [Fact]
    public void DeclineCancellation_Invoked_ClearsPendingTicket()
    {
        var ticket = new Ticket { TicketId = PendingTicketId, Status = ActiveStatus };
        _viewModel.PendingCancelTicket = ticket;

        _viewModel.DeclineCancellation();

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void OnNavigatedTo_NotAuthenticated_RedirectsToAuthentication()
    {
        UserSession.CurrentUser = null;

        var navigationResult = _viewModel.OnNavigatedTo();

        navigationResult.Should().BeFalse();
        _mockNavigationService.Verify(navServiceToVerifyAuthRedirect => navServiceToVerifyAuthRedirect.NavigateTo(typeof(View.AuthPage), null), Times.Once);
    }
}

