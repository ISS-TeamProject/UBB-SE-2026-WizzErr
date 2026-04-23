using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IPricingService
    {
        PriceBreakdown CalculatePriceBreakdown(Flight flight, User user, List<Ticket> tickets);
    }
}
