namespace TicketManager.Domain
{
    public static class UserSession
    {
        public static User CurrentUser { get; set; }
        public static object[] PendingBookingParameters { get; set; }
    }
}
