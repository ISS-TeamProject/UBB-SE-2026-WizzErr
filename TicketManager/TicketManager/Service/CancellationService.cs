using System;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class CancellationService : ICancellationService
    {
        private readonly ITicketRepository _ticketRepository;

        public CancellationService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        }

        public (bool CanCancel, string Reason) CanCancelTicket(Ticket ticket)
        {
            if (ticket == null)
            {
                return (false, "Ticket not found.");
            }

            if (string.Equals(ticket.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "This ticket is already cancelled.");
            }

            if (ticket.Flight != null && ticket.Flight.Date < DateTime.Now)
            {
                return (false, "This flight is already in the past and cannot be cancelled.");
            }

            return (true, string.Empty);
        }

        public void CancelTicket(int ticketId)
        {
            _ticketRepository.UpdateTicketStatus(ticketId, "Cancelled");
        }
    }
}
