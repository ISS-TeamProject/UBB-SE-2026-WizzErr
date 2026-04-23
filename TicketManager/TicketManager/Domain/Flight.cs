using System;

namespace TicketManager.Domain
{
    public class Flight
    {
        private const float PricePerMinuteMultiplier = 1.25f;
        private const float MinimumFlightPrice = 40f;

        public int FlightId { get; set; }
        public Route? Route { get; set; }
        public Gate? Gate { get; set; }
        public DateTime Date { get; set; }
        public string? FlightNr { get; set; }

        public Flight()
        {
        }

        public Flight(Route? route, Gate? gate, DateTime date, string? flightNr)
        {
            Route = route;
            Gate = gate;
            Date = date;
            FlightNr = flightNr;
        }

        public Flight(int flightId, Route route, Gate gate, DateTime date, string flightNr)
        {
            FlightId = flightId;
            Route = route;
            Gate = gate;
            Date = date;
            FlightNr = flightNr;
        }

        public float GetBasePrice()
        {
            if (Route == null)
            {
                return 0f;
            }

            TimeSpan duration = Route.ArrivalTime - Route.DepartureTime;
            float calculatedPrice = (float)duration.TotalMinutes * PricePerMinuteMultiplier;
            return Math.Max(calculatedPrice, MinimumFlightPrice);
        }
    }
}
