using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class FlightSearchService : IFlightSearchService
    {
        private readonly IFlightRepository _flightRepository;

        // Injectăm repository-ul prin constructor
        public FlightSearchService(IFlightRepository flightRepository)
        {
            _flightRepository = flightRepository ?? throw new ArgumentNullException(nameof(flightRepository));
        }

        public IEnumerable<Flight> SearchFlights(string location, string flightType, DateTime? date, int? passengers)
        {
            // Validări de bază (Business Logic)
            if (string.IsNullOrWhiteSpace(location))
            {
                return new List<Flight>();
            }

            // Dacă datele sunt ok, apelăm interogarea SQL deja scrisă în Repository
            return _flightRepository.GetFlightsByRoute(location, flightType, date, passengers);
        }
    }
}
