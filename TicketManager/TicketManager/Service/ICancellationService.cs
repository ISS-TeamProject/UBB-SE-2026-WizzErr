using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface ICancellationService
    {
        (bool CanCancel, string Reason) CanCancelTicket(Ticket ticket);
        void CancelTicket(int ticketId);
    }
}
