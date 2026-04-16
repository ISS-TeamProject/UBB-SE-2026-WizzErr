using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class BookingService : IBookingService
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

        public List<Ticket> CreateTickets(Flight flight, User user, List<PassengerData> passengers, float basePrice)
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

        public string ValidatePassengers(List<PassengerData> passengers)
        {
            if (passengers == null || passengers.Count == 0)
            {
                return "At least one passenger is required.";
            }

            for (int i = 0; i < passengers.Count; i++)
            {
                var passenger = passengers[i];
                int passengerNumber = i + 1;

                if (string.IsNullOrWhiteSpace(passenger.FirstName))
                {
                    return $"Passenger {passengerNumber}: first name is required.";
                }

                if (string.IsNullOrWhiteSpace(passenger.LastName))
                {
                    return $"Passenger {passengerNumber}: last name is required.";
                }

                if (!string.IsNullOrWhiteSpace(passenger.Email) && !ValidationHelper.IsValidEmail(passenger.Email))
                {
                    return $"Passenger {passengerNumber}: email format is invalid.";
                }

                if (string.IsNullOrWhiteSpace(passenger.SelectedSeat))
                {
                    return $"Passenger {passengerNumber}: please select a seat.";
                }
            }

            return string.Empty;
        }

        public int CalculateMaxPassengers(int routeCapacity, int occupiedSeatCount, int requestedPassengerCount)
        {
            int remainingCapacity = routeCapacity - occupiedSeatCount;

            if (requestedPassengerCount > 0)
            {
                return Math.Min(requestedPassengerCount, remainingCapacity);
            }

            return remainingCapacity;
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