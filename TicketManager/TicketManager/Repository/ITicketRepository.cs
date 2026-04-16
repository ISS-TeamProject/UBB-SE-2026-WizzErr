using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public interface ITicketRepository
    {
        IEnumerable<Ticket> GetTicketsByUserId(int userId);
        void AddTicket(Ticket ticket);
        void UpdateTicketStatus(int ticketId, string status);
        void AddTicketAddOns(int ticketId, IEnumerable<int> addOnIds);
        IEnumerable<string> GetOccupiedSeats(int flightId);
    }
}