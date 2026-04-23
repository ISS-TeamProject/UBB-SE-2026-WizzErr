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

        public string FlightNumber { get; set; } = string.Empty;

        public string RouteCity { get; set; } = string.Empty;

        public string DisplayDate { get; set; } = string.Empty;

        public string DisplayPrice { get; set; } = string.Empty;

        public FlightDisplayModel(Flight flight)
        {
            this.Flight = flight;
            this.FlightNumber = flight.FlightNumber ?? string.Empty;
            this.RouteCity = flight.Route?.Airport?.City ?? "Unknown";
            this.DisplayDate = flight.Date.ToString("g");
            this.DisplayPrice = $"{flight.GetBasePrice():0.00} € / person";
        }
    }
}
