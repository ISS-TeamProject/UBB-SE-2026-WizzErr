using System;
using System.Linq;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _passwordHasher = new PasswordHasher<User>();
        }

        public User Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.");

            User existingUser = _userRepo.GetByEmail(email.Trim());

            if (existingUser == null)
                throw new InvalidOperationException("No account found with this email.");

            PasswordVerificationResult result =
                _passwordHasher.VerifyHashedPassword(existingUser, existingUser.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
                throw new InvalidOperationException("Invalid email or password.");

            return existingUser;
        }

        public void Register(string email, string phone, string username, string password)
        {
            string normalizedEmail = email?.Trim();
            string normalizedUsername = username?.Trim();
            string normalizedPhone = phone?.Trim();

            ValidateRegistrationData(normalizedEmail, normalizedPhone, normalizedUsername, password);

            User existingUser = _userRepo.GetByEmail(normalizedEmail);
            if (existingUser != null)
                throw new InvalidOperationException("An account with this email already exists.");

            User newUser = new User
            {
                Email = normalizedEmail,
                Phone = normalizedPhone,
                Username = normalizedUsername,
                Membership = null
            };

            string hashedPassword = _passwordHasher.HashPassword(newUser, password);
            newUser.PasswordHash = hashedPassword;

            _userRepo.AddUser(newUser);
        }

        private void ValidateRegistrationData(string email, string phone, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            if (!ValidationHelper.IsValidEmail(email))
                throw new ArgumentException("Email format is invalid.");

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required.");

            if (username.Length < 3)
                throw new ArgumentException("Username must have at least 3 characters.");

            if (!username.All(c => char.IsLetter(c) || char.IsDigit(c) || c == '_' || c == ' '))
                throw new ArgumentException("Username contains invalid characters.");

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone is required.");

            if (!ValidationHelper.IsValidPhone(phone))
                throw new ArgumentException("Phone number must contain only digits and have 10 to 15 digits.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.");

            if (password.Length < 6)
                throw new ArgumentException("Password must be at least 6 characters long.");
        }

        public void Logout()
        {
            UserSession.CurrentUser = null;
            UserSession.PendingBookingParameters = null;
        }
    }
}