using FluentAssertions;
using Moq;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.Tests.Unit.ViewModel;

public class DashboardViewModelTests
{
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
        var ticket = new Ticket { TicketId = 1, Status = "Active" };
        _mockCancellationService.Setup(s => s.CanCancelTicket(ticket)).Returns((true, ""));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().Be(ticket);
    }

    [Fact]
    public void CancelTicketCommand_SetsCancellationFailedWhenNotCancelable()
    {
        var ticket = new Ticket { TicketId = 1, Status = "Active" };
        _mockCancellationService.Setup(s => s.CanCancelTicket(ticket))
            .Returns((false, "Cannot cancel within 24 hours of departure"));

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.CancellationSucceeded.Should().BeFalse();
        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void CancelTicketCommand_IgnoresCancelledTicket()
    {
        var ticket = new Ticket { TicketId = 1, Status = "Cancelled" };

        _viewModel.CancelTicketCommand.Execute(ticket);

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void ConfirmCancellation_CallsServiceAndClearsState()
    {
        UserSession.CurrentUser = new User { UserId = 1, Email = "bogdan.ionescu@gmail.com" };
        var ticket = new Ticket { TicketId = 5, Status = "Active" };
        _viewModel.PendingCancelTicket = ticket;
        _mockDashboardService.Setup(s => s.GetUserTickets(It.IsAny<int>(), It.IsAny<string>())).Returns(new List<Ticket>());

        _viewModel.ConfirmCancellation();

        _mockCancellationService.Verify(s => s.CancelTicket(5), Times.Once);
        _viewModel.PendingCancelTicket.Should().BeNull();
        _viewModel.CancellationSucceeded.Should().BeTrue();
    }

    [Fact]
    public void DeclineCancellation_ClearsPendingTicket()
    {
        var ticket = new Ticket { TicketId = 3, Status = "Active" };
        _viewModel.PendingCancelTicket = ticket;

        _viewModel.DeclineCancellation();

        _viewModel.PendingCancelTicket.Should().BeNull();
    }

    [Fact]
    public void OnNavigatedTo_RedirectsToAuthWhenNotAuthenticated()
    {
        UserSession.CurrentUser = null;

        var result = _viewModel.OnNavigatedTo();

        result.Should().BeFalse();
        _mockNavigationService.Verify(n => n.NavigateTo(typeof(View.AuthPage), null), Times.Once);
    }
}

