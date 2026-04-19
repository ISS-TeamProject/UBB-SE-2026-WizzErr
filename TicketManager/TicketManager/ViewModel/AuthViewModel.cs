using System;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class AuthViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        // ── Input fields (bound two-way from XAML) ──────────────────
        private string _emailText;
        private string _passwordText;
        private string _usernameText;
        private string _phoneText;

        // ── Internal state ──────────────────────────────────────────
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoginMode = true;
        private bool _isAuthenticated;
        private User _authenticatedUser;

        // ── UI text properties (View binds to these instead of setting them directly) ──
        private string _titleText = "Welcome to WizzErr";
        private string _subtitleText = "Please sign in to manage your tickets";
        private string _actionButtonLabel = "Sign In";
        private string _togglePromptLabel = "Don't have an account?";
        private string _toggleButtonLabel = "Create one";
        private bool _isRegisterFieldsVisible = false;

        public AuthViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Single command for both Login and Register — the ViewModel decides which to execute
            ActionCommand = new RelayCommand(_ => ExecuteAction(), _ => IsFormValid);
            ToggleModeCommand = new RelayCommand(_ => ToggleMode());
        }

        // ── Input properties ────────────────────────────────────────

        public string EmailText
        {
            get => _emailText;
            set { _emailText = value; OnPropertyChanged(); RaiseActionCanExecuteChanged(); }
        }

        public string PasswordText
        {
            get => _passwordText;
            set { _passwordText = value; OnPropertyChanged(); RaiseActionCanExecuteChanged(); }
        }

        public string UsernameText
        {
            get => _usernameText;
            set { _usernameText = value; OnPropertyChanged(); RaiseActionCanExecuteChanged(); }
        }

        public string PhoneText
        {
            get => _phoneText;
            set { _phoneText = value; OnPropertyChanged(); RaiseActionCanExecuteChanged(); }
        }

        // ── State properties ────────────────────────────────────────

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set { _successMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoginMode
        {
            get => _isLoginMode;
            set { _isLoginMode = value; OnPropertyChanged(); }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set { _isAuthenticated = value; OnPropertyChanged(); }
        }

        public User AuthenticatedUser
        {
            get => _authenticatedUser;
            set { _authenticatedUser = value; OnPropertyChanged(); }
        }

        // ── UI text properties (replacing code-behind UI manipulation) ──

        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(); }
        }

        public string SubtitleText
        {
            get => _subtitleText;
            set { _subtitleText = value; OnPropertyChanged(); }
        }

        public string ActionButtonLabel
        {
            get => _actionButtonLabel;
            set { _actionButtonLabel = value; OnPropertyChanged(); }
        }

        public string TogglePromptLabel
        {
            get => _togglePromptLabel;
            set { _togglePromptLabel = value; OnPropertyChanged(); }
        }

        public string ToggleButtonLabel
        {
            get => _toggleButtonLabel;
            set { _toggleButtonLabel = value; OnPropertyChanged(); }
        }

        public bool IsRegisterFieldsVisible
        {
            get => _isRegisterFieldsVisible;
            set { _isRegisterFieldsVisible = value; OnPropertyChanged(); }
        }

        // ── Validation (replaces ValidateInput() from code-behind) ──

        /// <summary>
        /// Determines if the form has enough input to attempt the action.
        /// Previously this logic lived in AuthPage.xaml.cs ValidateInput().
        /// </summary>
        public bool IsFormValid
        {
            get
            {
                if (IsLoginMode)
                {
                    return !string.IsNullOrWhiteSpace(EmailText) &&
                           !string.IsNullOrWhiteSpace(PasswordText);
                }
                else
                {
                    return !string.IsNullOrWhiteSpace(EmailText) &&
                           !string.IsNullOrWhiteSpace(UsernameText) &&
                           !string.IsNullOrWhiteSpace(PhoneText) &&
                           !string.IsNullOrWhiteSpace(PasswordText);
                }
            }
        }

        // ── Commands ────────────────────────────────────────────────

        public ICommand ActionCommand { get; }
        public ICommand ToggleModeCommand { get; }

        // ── Command implementations ─────────────────────────────────

        /// <summary>
        /// Handles the main action button (Sign In or Register).
        /// After successful login, navigates to the appropriate page.
        /// Previously this orchestration lived in AuthPage.xaml.cs ActionButton_Click.
        /// </summary>
        private void ExecuteAction()
        {
            if (IsLoginMode)
            {
                Login();

                if (IsAuthenticated)
                {
                    UserSession.CurrentUser = AuthenticatedUser;

                    if (UserSession.PendingBookingParameters != null)
                    {
                        var pendingParameters = UserSession.PendingBookingParameters;
                        UserSession.PendingBookingParameters = null;
                        _navigationService.NavigateTo(typeof(View.BookingPage), pendingParameters);
                    }
                    else
                    {
                        _navigationService.NavigateTo(typeof(View.FlightSearchPage));
                    }
                }
            }
            else
            {
                Register();

                if (string.IsNullOrWhiteSpace(ErrorMessage))
                {
                    // Switch back to login mode after successful registration
                    SetLoginMode();
                }
            }
        }

        /// <summary>
        /// Toggles between Login and Register modes.
        /// Previously this logic lived in AuthPage.xaml.cs ToggleMode_Click,
        /// including the direct UI element manipulation.
        /// </summary>
        private void ToggleMode()
        {
            if (IsLoginMode)
            {
                SetRegisterMode();
            }
            else
            {
                SetLoginMode();
            }

            ClearMessages();
            RaiseActionCanExecuteChanged();
        }

        private void SetLoginMode()
        {
            IsLoginMode = true;
            TitleText = "Welcome to WizzErr";
            SubtitleText = "Please sign in to manage your tickets";
            ActionButtonLabel = "Sign In";
            TogglePromptLabel = "Don't have an account?";
            ToggleButtonLabel = "Create one";
            IsRegisterFieldsVisible = false;
        }

        private void SetRegisterMode()
        {
            IsLoginMode = false;
            TitleText = "Create a WizzErr Account";
            SubtitleText = "Fill in the details to register";
            ActionButtonLabel = "Register";
            TogglePromptLabel = "Already have an account?";
            ToggleButtonLabel = "Sign in";
            IsRegisterFieldsVisible = true;
        }

        // ── Core auth methods (unchanged, just made private) ────────

        private void Login()
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                User user = _authService.Login(EmailText, PasswordText);

                AuthenticatedUser = user;
                IsAuthenticated = true;
                SuccessMessage = "Login successful.";
            }
            catch (Exception ex)
            {
                IsAuthenticated = false;
                AuthenticatedUser = null;
                ErrorMessage = ex.Message;
            }
        }

        private void Register()
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                _authService.Register(EmailText, PhoneText, UsernameText, PasswordText);

                SuccessMessage = "Registration successful. You can now sign in.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        public void ClearMessages()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        private void RaiseActionCanExecuteChanged()
        {
            OnPropertyChanged(nameof(IsFormValid));
            (ActionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
