using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class FlightSearchService : IFlightSearchService
    {
        private readonly IFlightRepository flightRepository;

        public FlightSearchService(IFlightRepository flightRepository)
        {
            this.flightRepository = flightRepository ?? throw new ArgumentNullException(nameof(flightRepository));
        }

        public IEnumerable<Flight> SearchFlights(string location, string flightType, DateTime? date, int? passengers)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return new List<Flight>();
            }

            var flights = this.flightRepository.GetFlightsByRoute(location, flightType, date);

            if (passengers.HasValue && passengers.Value > 0)
            {
                flights = flights.Where(flight =>
                {
                    int occupiedSeats = this.flightRepository.GetOccupiedSeatCount(flight.FlightId);
                    int availableSeats = flight.Route!.Capacity - occupiedSeats;
                    return availableSeats >= passengers.Value;
                });
            }

            return flights;
        }
    }
}
