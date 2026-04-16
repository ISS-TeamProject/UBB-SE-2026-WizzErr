using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class BookingService
    {
        private const string CancelledStatus = "Cancelled";
        private const string ActiveStatus = "Active";

        private readonly ITicketRepository _ticketRepository;
        private readonly IAddOnRepository _addOnRepository;

        public BookingService(ITicketRepository ticketRepository, IAddOnRepository addOnRepository)
        {
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _addOnRepository = addOnRepository ?? throw new ArgumentNullException(nameof(addOnRepository));
        }

        public float CalculateFinalPrice(List<Ticket> tickets, User bookingUser)
        {
            float total = 0f;
            foreach (var ticket in tickets)
            {
                ticket.User = bookingUser;
                total += ticket.CalculateTotalPrice();
            }
            return total;
        }

        public List<Ticket> CreateTickets(Flight flight, User user, List<ViewModel.PassengerFormViewModel> passengers, float basePrice)
        {
            var tickets = new List<Ticket>();

            foreach (var pass in passengers)
            {
                var ticket = new Ticket
                {
                    Flight = flight,
                    User = user,
                    PassengerFirstName = pass.FirstName,
                    PassengerLastName = pass.LastName,
                    PassengerEmail = pass.Email,
                    PassengerPhone = pass.Phone,
                    Seat = pass.SelectedSeat,
                    Price = basePrice,
                    Status = ActiveStatus,
                    SelectedAddOns = pass.SelectedAddOns.ToList()
                };
                tickets.Add(ticket);
            }

            return tickets;
        }

        public async Task<bool> SaveTicketsAsync(List<Ticket> tickets)
        {
            if (tickets == null || tickets.Count == 0)
                return false;

            bool duplicateSeatInRequest = tickets
                .Where(t => !string.IsNullOrWhiteSpace(t.Seat))
                .GroupBy(t => t.Seat)
                .Any(g => g.Count() > 1);

            if (duplicateSeatInRequest)
                return false;

            return await _ticketRepository.SaveTicketsWithAddOnsAsync(tickets);
        }

        public async Task<bool> CancelTicketAsync(int ticketId)
        {
            try
            {
                _ticketRepository.UpdateTicketStatus(ticketId, CancelledStatus);
                return await Task.FromResult(true);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        public async Task<List<AddOn>> GetAvailableAddOnsAsync()
        {
            return await Task.FromResult(_addOnRepository.GetAllAddOns().ToList());
        }

        public async Task<List<string>> GetOccupiedSeatsAsync(int flightId)
        {
            return await Task.FromResult(_ticketRepository.GetOccupiedSeats(flightId).ToList());
        }
    }
}