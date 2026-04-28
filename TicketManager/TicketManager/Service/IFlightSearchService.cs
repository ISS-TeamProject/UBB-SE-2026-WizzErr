using System;
using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IFlightSearchService
    {
        IEnumerable<Flight> SearchFlights(string location, bool isDeparture, DateTime? date, int? passengers);
        int? ParsePassengerCount(string input);
    }
}
