using TicketManager.Domain;

namespace TicketManager.Service
{
    public static class UserSession
    {
        public static User CurrentUser { get; set; }
        public static object[] PendingBookingParameters { get; set; }
    }
}
