using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IFlightSearchService
    {
        IEnumerable<Flight> SearchFlights(string location, string flightType, DateTime? date, int? passengers);
    }
}