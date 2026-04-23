using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public class PricingService : IPricingService
    {
        public PriceBreakdown CalculatePriceBreakdown(Flight flight, User user, List<Ticket> tickets)
        {
            if (flight == null || tickets == null || tickets.Count == 0)
            {
                return new PriceBreakdown();
            }

            float basePrice = flight.GetBasePrice();
            float basePriceTotal = basePrice * tickets.Count;

            float addOnsWithoutMembership = tickets.Sum(ticket => ticket.SelectedAddOns.Sum(addOn => addOn.GetBasePrice()));
            float totalWithoutMembership = basePriceTotal + addOnsWithoutMembership;

            float finalTotal = 0f;
            foreach (var ticket in tickets)
            {
                ticket.User = user;
                finalTotal += ticket.CalculateTotalPrice();
            }

            float membershipSavings = Math.Max(0, totalWithoutMembership - finalTotal);

            return new PriceBreakdown
            {
                BasePricePerPerson = basePrice,
                BasePriceTotal = basePriceTotal,
                AddOnsTotal = addOnsWithoutMembership,
                MembershipSavings = membershipSavings,
                FinalTotal = finalTotal
            };
        }
    }
}
