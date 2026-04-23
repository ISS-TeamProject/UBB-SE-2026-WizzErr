using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IDashboardService
    {
        IEnumerable<Ticket> GetUserTickets(int userId, string ticketFilter);
        string GenerateTicketPdf(Ticket ticket);
    }
}
