namespace TicketManager.Domain
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public Membership Membership { get; set; }

        public User() { }

        public User(string email, string phone, string username, string passwordHash, Membership membership)
        {
            Email = email;
            Phone = phone;
            Username = username;
            PasswordHash = passwordHash;
            Membership = membership;
        }

        public User(int userId, string email, string phone, string username, string passwordHash, Membership membership)
        {
            UserId = userId;
            Email = email;
            Phone = phone;
            Username = username;
            PasswordHash = passwordHash;
            Membership = membership;
        }

        public int GetId()
        {
            return UserId;
        }

        public string GetEmail()
        {
            return Email;
        }
    }
}