using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using TicketManager.ViewModel;
using System;

namespace TicketManager.View
{
    /// <summary>
    /// Code-behind is now minimal: constructs the ViewModel from the composition root
    /// and reacts to ViewModel state changes to show dialogs (a pure-UI concern).
    /// All purchase logic lives in MembershipViewModel.
    /// </summary>
    public sealed partial class MembershipsPage : Page
    {
        public MembershipViewModel ViewModel { get; }

        public MembershipsPage()
        {
            this.InitializeComponent();

            // ViewModel is built with services from the composition root.
            // This View no longer knows about DatabaseConnectionFactory, repos, or concrete services.
            ViewModel = new MembershipViewModel(App.MembershipService, App.NavigationService);
            this.DataContext = ViewModel;

            // React to purchase result changes to show a dialog (UI-only concern)
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// When the ViewModel reports a purchase result, show the appropriate dialog.
        /// Showing a ContentDialog is a UI concern a€” the ViewModel only sets the state.
        /// </summary>
        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ViewModel.PurchaseSucceeded) || ViewModel.PurchaseSucceeded == null)
                return;

            var dialog = new ContentDialog
            {
                Title = ViewModel.PurchaseSucceeded == true ? "Membership updated" : "Purchase failed",
                Content = ViewModel.PurchaseResultMessage,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
