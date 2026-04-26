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

        private readonly ITicketRepository ticketRepository;
        private readonly IAddOnRepository addOnRepository;

        public BookingService(ITicketRepository ticketRepository, IAddOnRepository addOnRepository)
        {
            this.ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            this.addOnRepository = addOnRepository ?? throw new ArgumentNullException(nameof(addOnRepository));
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
            {
                return false;
            }

            bool duplicateSeatInRequest = tickets
                .Where(ticket => !string.IsNullOrWhiteSpace(ticket.Seat))
                .GroupBy(ticket => ticket.Seat)
                .Any(group => group.Count() > 1);

            if (duplicateSeatInRequest)
            {
                return false;
            }

            foreach (var ticket in tickets)
            {
                if (!string.IsNullOrWhiteSpace(ticket.Seat))
                {
                    bool seatAvailable = await ticketRepository.IsSeatAvailable(ticket.Flight?.FlightId ?? 0, ticket.Seat);
                    if (!seatAvailable)
                    {
                        return false;
                    }
                }
            }

            return await ticketRepository.SaveTicketsWithAddOnsAsync(tickets);
        }

        public async Task<List<AddOn>> GetAvailableAddOnsAsync()
        {
            return await Task.FromResult(addOnRepository.GetAllAddOns().ToList());
        }

        public async Task<List<string>> GetOccupiedSeatsAsync(int flightId)
        {
            return await Task.FromResult(ticketRepository.GetOccupiedSeats(flightId).ToList());
        }

        public BookingParametersResult ParseBookingParameters(object parameter)
        {
            Flight selectedFlight = null;
            User user = null;
            int requestedPassengers = 0;

            if (parameter is object[] args && args.Length > 0)
            {
                selectedFlight = args[0] as Flight;

                if (args.Length >= 3)
                {
                    user = args[1] as User;
                    if (args[2] is int count)
                    {
                        requestedPassengers = count;
                    }
                }
                else if (args.Length >= 2)
                {
                    if (args[1] is int count)
                    {
                        requestedPassengers = count;
                    }
                    else
                    {
                        user = args[1] as User;
                    }
                }
            }

            user ??= UserSession.CurrentUser;

            return new BookingParametersResult
            {
                Flight = selectedFlight,
                User = user,
                RequestedPassengers = requestedPassengers
            };
        }

        public void StorePendingBooking(Flight flight, int requestedPassengers)
        {
            UserSession.PendingBookingParameters = new object[] { flight, requestedPassengers };
        }
    }
}
