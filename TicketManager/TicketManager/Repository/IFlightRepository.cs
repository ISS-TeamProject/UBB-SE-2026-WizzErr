using System;
using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Domain.Repositories
{
    public interface IFlightRepository
    {
        IEnumerable<Flight> GetFlightsByRoute(string departure, string destination, DateTime date);
        Flight GetFlightById(int id);
    }
}