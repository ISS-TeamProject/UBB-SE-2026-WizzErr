using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketManager.Domain;

namespace TicketManager.ViewModel
{
    public class FlightDisplayModel
    {
        public Flight Flight { get; }

        public string FlightNr { get; set; }
        public string RouteCity { get; set; }
        public string DisplayDate { get; set; }
        public string DisplayPrice { get; set; }

        public FlightDisplayModel(Flight flight)
        {
            Flight = flight;
            FlightNr = flight.FlightNr;
            RouteCity = flight.Route?.Airport?.City ?? "Unknown";
            DisplayDate = flight.Date.ToString("g"); // Aici facem formatarea datei
            DisplayPrice = $"{flight.GetBasePrice():0.00} € / person";
        }
    }
}
