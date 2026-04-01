using System.Collections.Generic;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class DashboardService
    {
        private readonly ITicketRepository _ticketRepository;

        public DashboardService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public IEnumerable<Ticket> GetUserTickets(int userId)
        {
            return _ticketRepository.GetTicketsByUserId(userId);
        }

        public void CancelUserTicket(int ticketId)
        {
            _ticketRepository.UpdateTicketStatus(ticketId, "Cancelled");
        }
    }
}