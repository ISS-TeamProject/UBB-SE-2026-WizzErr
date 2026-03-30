using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class AuthViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;

        private string _emailText;
        private string _passwordText;
        private string _usernameText;
        private string _phoneText;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoginMode = true;
        private bool _isAuthenticated;
        private User _authenticatedUser;

        public event PropertyChangedEventHandler PropertyChanged;

        public AuthViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            LoginCommand = new RelayCommand(_ => Login());
            RegisterCommand = new RelayCommand(_ => Register());
        }

        public string EmailText
        {
            get => _emailText;
            set
            {
                _emailText = value;
                OnPropertyChanged();
            }
        }

        public string PasswordText
        {
            get => _passwordText;
            set
            {
                _passwordText = value;
                OnPropertyChanged();
            }
        }

        public string UsernameText
        {
            get => _usernameText;
            set
            {
                _usernameText = value;
                OnPropertyChanged();
            }
        }

        public string PhoneText
        {
            get => _phoneText;
            set
            {
                _phoneText = value;
                OnPropertyChanged();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                _successMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoginMode
        {
            get => _isLoginMode;
            set
            {
                _isLoginMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                _isAuthenticated = value;
                OnPropertyChanged();
            }
        }

        public User AuthenticatedUser
        {
            get => _authenticatedUser;
            set
            {
                _authenticatedUser = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public void Login()
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

        public void Register()
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

