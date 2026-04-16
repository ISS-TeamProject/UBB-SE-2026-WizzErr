using System;
using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public interface IFlightRepository
    {
        IEnumerable<Flight> GetFlightsByRoute(string location, string routeType, DateTime? date);
        Flight GetFlightById(int id);
        int GetOccupiedSeatCount(int flightId);
    }
}