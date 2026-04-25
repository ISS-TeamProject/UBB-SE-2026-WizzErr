using System;

namespace TicketManager.Domain
{
    public class Flight
    {
        public int FlightId { get; set; }
        public Route? Route { get; set; }
        public Gate? Gate { get; set; }
        public DateTime Date { get; set; }
        public string? FlightNumber { get; set; }

        public Flight()
        {
        }

        public Flight(Route? route, Gate? gate, DateTime date, string? flightNr)
        {
            Route = route;
            Gate = gate;
            Date = date;
            FlightNumber = flightNr;
        }

        public Flight(int flightId, Route route, Gate gate, DateTime date, string flightNr)
        {
            FlightId = flightId;
            Route = route;
            Gate = gate;
            Date = date;
            FlightNumber = flightNr;
        }
    }
}
