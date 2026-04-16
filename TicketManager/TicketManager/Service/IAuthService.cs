using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IAuthService
    {
        User Login(string email, string password);
        void Register(string email, string phone, string username, string password);
        void Logout();
    }
}
