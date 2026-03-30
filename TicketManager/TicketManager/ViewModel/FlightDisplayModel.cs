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
        // Acestea sunt proprietățile pe care le va citi XAML-ul
        public string FlightNr { get; set; }
        public string RouteCity { get; set; }
        public string DisplayDate { get; set; }
        public string DisplayPrice { get; set; }

        // Constructorul care preia un Flight din baza de date și îl formatează
        public FlightDisplayModel(Flight flight)
        {
            FlightNr = flight.FlightNr;
            RouteCity = flight.Route?.Airport?.City ?? "Unknown";
            DisplayDate = flight.Date.ToString("g"); // Aici facem formatarea datei
            DisplayPrice = $"{flight.GetBasePrice():0.00} €"; // Aici adăugăm simbolul Euro
        }
    }
}
