using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.ViewModel;

namespace TicketManager.View
{
    public sealed partial class AuthPage : Page
    {
        private readonly AuthViewModel _viewModel;

        public AuthPage()
        {
            this.InitializeComponent();

            var dbFactory = new DatabaseConnectionFactory();
            var userRepository = new UserRepository(dbFactory);
            var authService = new AuthService(userRepository);
            _viewModel = new AuthViewModel(authService);

            this.DataContext = _viewModel;
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PasswordText = passwordInput.Password;

            if (_viewModel.IsLoginMode)
            {
                _viewModel.Login();

                if (_viewModel.IsAuthenticated)
                {
                    this.Frame.Navigate(typeof(FlightSearchPage));
                }
            }
            else
            {
                _viewModel.Register();

                if (string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
                {
                    _viewModel.IsLoginMode = true;

                    TitleTextBlock.Text = "Welcome to WizzErr";
                    SubtitleTextBlock.Text = "Please sign in to manage your tickets";
                    loginButton.Content = "Sign In";
                    TogglePromptText.Text = "Don't have an account?";
                    ToggleModeButton.Content = "Create one";
                    usernameInput.Visibility = Visibility.Collapsed;
                    phoneInput.Visibility = Visibility.Collapsed;
                }
            }

            ShowMessages();
        }

        private void ToggleMode_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsLoginMode = !_viewModel.IsLoginMode;

            if (_viewModel.IsLoginMode)
            {
                TitleTextBlock.Text = "Welcome to WizzErr";
                SubtitleTextBlock.Text = "Please sign in to manage your tickets";
                loginButton.Content = "Sign In";
                TogglePromptText.Text = "Don't have an account?";
                ToggleModeButton.Content = "Create one";
                usernameInput.Visibility = Visibility.Collapsed;
                phoneInput.Visibility = Visibility.Collapsed;
            }
            else
            {
                TitleTextBlock.Text = "Create a WizzErr Account";
                SubtitleTextBlock.Text = "Fill in the details to register";
                loginButton.Content = "Register";
                TogglePromptText.Text = "Already have an account?";
                ToggleModeButton.Content = "Sign in";
                usernameInput.Visibility = Visibility.Visible;
                phoneInput.Visibility = Visibility.Visible;
            }

            _viewModel.ClearMessages();
            ShowMessages();
            ValidateInput();
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            SyncInputsToViewModel();
            ValidateInput();
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.PasswordText = passwordInput.Password;
            ValidateInput();
        }

        private void SyncInputsToViewModel()
        {
            _viewModel.EmailText = emailInput.Text;
            _viewModel.UsernameText = usernameInput.Text;
            _viewModel.PhoneText = phoneInput.Text;
        }

        private void ValidateInput()
        {
            if (loginButton == null) return;

            if (_viewModel.IsLoginMode)
            {
                loginButton.IsEnabled =
                    !string.IsNullOrWhiteSpace(emailInput.Text) &&
                    !string.IsNullOrWhiteSpace(passwordInput.Password);
            }
            else
            {
                loginButton.IsEnabled =
                    !string.IsNullOrWhiteSpace(emailInput.Text) &&
                    !string.IsNullOrWhiteSpace(usernameInput.Text) &&
                    !string.IsNullOrWhiteSpace(phoneInput.Text) &&
                    !string.IsNullOrWhiteSpace(passwordInput.Password);
            }
        }

        private async void ShowMessages()
        {
            if (!string.IsNullOrWhiteSpace(_viewModel.ErrorMessage))
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = _viewModel.ErrorMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
            else if (!string.IsNullOrWhiteSpace(_viewModel.SuccessMessage) && !_viewModel.IsAuthenticated)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Success",
                    Content = _viewModel.SuccessMessage,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }
    }
}