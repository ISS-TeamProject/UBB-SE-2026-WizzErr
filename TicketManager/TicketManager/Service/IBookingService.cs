using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IBookingService
    {
        List<Ticket> CreateTickets(Flight flight, User user, List<PassengerData> passengers, float basePrice);
        Task<bool> SaveTicketsAsync(List<Ticket> tickets);
        Task<List<AddOn>> GetAvailableAddOnsAsync();
        Task<List<string>> GetOccupiedSeatsAsync(int flightId);
        string ValidatePassengers(List<PassengerData> passengers);
        int CalculateMaxPassengers(int routeCapacity, int occupiedSeatCount, int requestedPassengerCount);
    }
}
