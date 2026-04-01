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
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            this.InitializeComponent();

            // 1. Inițializăm baza de date și serviciul
            var dbFactory = new DatabaseConnectionFactory();
            var ticketRepository = new TicketRepository(dbFactory);
            var dashboardService = new DashboardService(ticketRepository);

            // 2. Creăm ViewModel-ul
            _viewModel = new DashboardViewModel(dashboardService);

            // 3. CRITIC: Spunem interfeței (XAML) să folosească acest ViewModel pentru {Binding}
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
            if (sender is not Button button || button.Tag is not Ticket ticket || ticket.Status == "Cancelled")
            {
                return;
            }

            if (ticket.Flight != null && ticket.Flight.Date < DateTime.Now)
            {
                var pastFlightDialog = new ContentDialog
                {
                    Title = "Cannot cancel",
                    Content = "This flight is already in the past and cannot be cancelled.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await pastFlightDialog.ShowAsync();
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