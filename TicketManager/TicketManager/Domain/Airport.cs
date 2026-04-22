namespace TicketManager.Domain
{
    public class Airport
    {
        public int AirportId { get; set; }
        public string AirportCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public Airport()
        {
        }

        public Airport(string airportCode, string city)
        {
            AirportCode = airportCode;
            City = city;
        }

        public Airport(int airportId, string airportCode, string city)
        {
            AirportId = airportId;
            AirportCode = airportCode;
            City = city;
        }
    }
}
