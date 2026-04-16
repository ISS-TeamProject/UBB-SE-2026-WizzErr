using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class DashboardPage : Page
    {
        private const string CancelledStatus = "Cancelled";

        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            this.InitializeComponent();

            var dbFactory = new DatabaseConnectionFactory();
            var ticketRepository = new TicketRepository(dbFactory);
            var dashboardService = new DashboardService(ticketRepository);
            var cancellationService = new CancellationService(ticketRepository);

            _viewModel = new DashboardViewModel(dashboardService, cancellationService);

            this.DataContext = _viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (UserSession.CurrentUser == null)
            {
                Frame.Navigate(typeof(AuthPage));
                return;
            }

            _viewModel.LoadUserTickets();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not Ticket ticket ||
                string.Equals(ticket.Status, CancelledStatus, StringComparison.OrdinalIgnoreCase))
                return;

            // Delegate cancellation eligibility check to ViewModel/Service
            var (canCancel, reason) = _viewModel.CanCancelTicket(ticket);
            if (!canCancel)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Cannot cancel",
                    Content = reason,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Cancel ticket",
                Content = $"Are you sure you want to cancel ticket #{ticket.TicketId}?",
                PrimaryButtonText = "Yes, cancel",
                CloseButtonText = "No",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _viewModel.CancelTicket(ticket);

                var resultDialog = new ContentDialog
                {
                    Title = "Ticket cancelled",
                    Content = "The ticket status was updated to Cancelled.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await resultDialog.ShowAsync();
            }
        }
    }
}