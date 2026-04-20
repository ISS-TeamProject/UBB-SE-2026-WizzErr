using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using TicketManager.ViewModel;
using System;
namespace TicketManager.View
{
    /// <summary>
    /// Code-behind is now minimal: constructs the ViewModel from the composition root,
    /// delegates navigation/auth to the ViewModel, and only handles dialog display (UI concern).
    /// All cancellation logic and eligibility checking lives in DashboardViewModel.
    /// </summary>
    public sealed partial class DashboardPage : Page
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage()
        {
            this.InitializeComponent();

            // ViewModel is built with services from the composition root.
            _viewModel = new DashboardViewModel(App.DashboardService, App.CancellationService, App.NavigationService);
            this.DataContext = _viewModel;

            // React to ViewModel state changes to show dialogs (pure UI)
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // ViewModel handles the auth check and data loading
            _viewModel.OnNavigatedTo();
        }

        /// <summary>
        /// Reacts to ViewModel state changes by showing the appropriate dialog.
        /// The ViewModel decides WHAT happened; the View decides HOW to display it.
        /// </summary>
        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // When cancellation fails (eligibility check), show error dialog
            if (e.PropertyName == nameof(_viewModel.CancellationSucceeded) &&
                _viewModel.CancellationSucceeded == false)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Cannot cancel",
                    Content = _viewModel.CancellationMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }

            // When cancellation succeeds, show confirmation dialog
            if (e.PropertyName == nameof(_viewModel.CancellationSucceeded) &&
                _viewModel.CancellationSucceeded == true)
            {
                var resultDialog = new ContentDialog
                {
                    Title = "Ticket cancelled",
                    Content = _viewModel.CancellationMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await resultDialog.ShowAsync();
            }

            // When a ticket is pending cancellation, show confirmation prompt
            if (e.PropertyName == nameof(_viewModel.PendingCancelTicket) &&
                _viewModel.PendingCancelTicket != null)
            {
                var dialog = new ContentDialog
                {
                    Title = "Cancel ticket",
                    Content = $"Are you sure you want to cancel ticket #{_viewModel.PendingCancelTicket.TicketId}?",
                    PrimaryButtonText = "Yes, cancel",
                    CloseButtonText = "No",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _viewModel.ConfirmCancellation();
                }
                else
                {
                    _viewModel.DeclineCancellation();
                }
            }
        }
    }
}