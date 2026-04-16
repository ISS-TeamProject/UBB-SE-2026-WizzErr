using System.Collections.Generic;

namespace TicketManager.Domain
{
    public class PassengerData
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string SelectedSeat { get; set; }
        public List<AddOn> SelectedAddOns { get; set; } = new List<AddOn>();
    }
}
