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
    public void CancelTicketCommand_SetsPendingWhenCancelable()
    {
        var ticket = new Ticket { TicketId = ActiveTicketId, Status = "Active" };
        _mockCancellationService.Setup(serviceAllowingCancel => serviceAllowingCancel.CanCancelTicket(ticket)).Returns((true, ""));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().Be(ticket);
    }

    [Fact]
    public void CancelTicketCommand_SetsCancellationFailedWhenNotCancelable()
    {
        var ticket = new Ticket { TicketId = ActiveTicketId, Status = "Active" };
        _mockCancellationService.Setup(serviceDenyingCancel => serviceDenyingCancel.CanCancelTicket(ticket))
            .Returns((false, "Cannot cancel within 24 hours of departure"));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.CancellationSucceeded.Should().BeFalse();
        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void CancelTicketCommand_IgnoresCancelledTicket()
    {
        var ticket = new Ticket { TicketId = CancelledTicketId, Status = "Cancelled" };

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void ConfirmCancellation_CallsServiceAndClearsState()
    {
        UserSession.CurrentUser = new User { UserId = TestUserId, Email = "bogdan.ionescu@gmail.com" };
        var ticket = new Ticket { TicketId = TargetTicketIdToCancel, Status = "Active" };
        _viewModel.PendingCancelTicket = ticket;
        _mockDashboardService.Setup(dashboardServiceReturningNoTickets => dashboardServiceReturningNoTickets.GetUserTickets(It.IsAny<int>(), It.IsAny<string>())).Returns(new List<Ticket>());

        _viewModel.ConfirmCancellation();

        _mockCancellationService.Verify(cancellationServiceToVerifyCancel => cancellationServiceToVerifyCancel.CancelTicket(TargetTicketIdToCancel), Times.Once);
        _viewModel.PendingCancelTicket.Should().BeNull();
        _viewModel.CancellationSucceeded.Should().BeTrue();
    }

    [Fact]
    public void DeclineCancellation_ClearsPendingTicket()
    {
        var ticket = new Ticket { TicketId = PendingTicketId, Status = "Active" };
        _viewModel.PendingCancelTicket = ticket;

        _viewModel.DeclineCancellation();

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void OnNavigatedTo_RedirectsToAuthWhenNotAuthenticated()
    {
        UserSession.CurrentUser = null;

        var navigationResult = _viewModel.OnNavigatedTo();

        navigationResult.Should().BeFalse();
        _mockNavigationService.Verify(navServiceToVerifyAuthRedirect => navServiceToVerifyAuthRedirect.NavigateTo(typeof(View.AuthPage), null), Times.Once);
    }
}

