using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using TicketManager.ViewModel;
using System;

namespace TicketManager.View
{
    /// <summary>
    /// Code-behind is now minimal: constructs the ViewModel and reacts to state
    /// changes by showing message dialogs... The ViewModel handles all login/register
    /// logic, form validation, mode switching, and navigation.
    /// 
    /// Compare with the original 170-line version that manually set TitleTextBlock.Text,
    /// usernameInput.Visibility, validated forms, managed sessions, and navigated.
    /// </summary>
    public sealed partial class AuthPage : Page
    {
        public AuthViewModel ViewModel { get; }

        public AuthPage()
        {
            this.InitializeComponent();

            // ViewModel is built with services from the composition root.
            ViewModel = new AuthViewModel(App.AuthService, App.NavigationService);

            this.DataContext = ViewModel;

            // React to result messages (showing dialogs is a UI concern)
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Shows error/success dialogs when the ViewModel reports them.
        /// This is the only "logic" left in the View — and it's pure UI.
        /// </summary>
        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.ErrorMessage) &&
                !string.IsNullOrWhiteSpace(ViewModel.ErrorMessage))
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = ViewModel.ErrorMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else if (e.PropertyName == nameof(ViewModel.SuccessMessage) &&
                     !string.IsNullOrWhiteSpace(ViewModel.SuccessMessage) &&
                     !ViewModel.IsAuthenticated)
            {
                var dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = ViewModel.SuccessMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// PasswordBox doesn't support two-way binding in WinUI, so we sync it manually.
        /// This is a known WinUI limitation and is an acceptable UI concern.
        /// </summary>
        private void Password_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModel.PasswordText = passwordInput.Password;
        }
    }
}