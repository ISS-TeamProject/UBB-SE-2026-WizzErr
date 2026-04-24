using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IPricingService
    {
        float CalculateBasePrice(Flight flight);
        float CalculateTotalPrice(Ticket ticket);
        PriceBreakdown CalculatePriceBreakdown(Flight flight, User user, List<Ticket> tickets);
    }
}
