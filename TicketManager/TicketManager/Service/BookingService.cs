using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class BookingService : IBookingService
    {
        private readonly DatabaseConnectionFactory _connectionFactory;
        private readonly ITicketRepository _ticketRepository;
        private readonly IAddOnRepository _addOnRepository;

        public BookingService(DatabaseConnectionFactory connectionFactory, ITicketRepository ticketRepository, IAddOnRepository addOnRepository)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
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
                    Status = "Active",
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
                .Where(t => !string.IsNullOrWhiteSpace(t.Seat))
                .GroupBy(t => t.Seat)
                .Any(g => g.Count() > 1);

            if (duplicateSeatInRequest)
            {
                return false;
            }

            using var connection = _connectionFactory.GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var ticket in tickets)
                {
                    if (!string.IsNullOrWhiteSpace(ticket.Seat))
                    {
                        string seatLockCheckQuery = @"
                            SELECT COUNT(*)
                            FROM Tickets WITH (UPDLOCK, HOLDLOCK)
                            WHERE flight_id = @flightId
                              AND seat = @seat
                              AND status <> 'Cancelled';
                        ";

                        using var seatCheckCmd = new SqlCommand(seatLockCheckQuery, connection, transaction);
                        seatCheckCmd.Parameters.AddWithValue("@flightId", ticket.Flight.FlightId);
                        seatCheckCmd.Parameters.AddWithValue("@seat", ticket.Seat);

                        int existingSeatCount = (int)await seatCheckCmd.ExecuteScalarAsync();
                        if (existingSeatCount > 0)
                        {
                            throw new InvalidOperationException("Selected seat is no longer available.");
                        }
                    }

                    string insertTicketQuery = @"
                        INSERT INTO Tickets (user_id, flight_id, seat, price, status, passenger_first_name, passenger_last_name, passenger_email, passenger_phone)
                        OUTPUT INSERTED.ticket_id
                        VALUES (@userId, @flightId, @seat, @price, @status, @fName, @lName, @email, @phone);
                    ";

                    using var cmd = new SqlCommand(insertTicketQuery, connection, transaction);
                    float persistedPrice = ticket.CalculateTotalPrice();
                    cmd.Parameters.AddWithValue("@userId", ticket.User.UserId);
                    cmd.Parameters.AddWithValue("@flightId", ticket.Flight.FlightId);
                    cmd.Parameters.AddWithValue("@seat", ticket.Seat ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", (decimal)persistedPrice);
                    cmd.Parameters.AddWithValue("@status", ticket.Status);
                    cmd.Parameters.AddWithValue("@fName", ticket.PassengerFirstName);
                    cmd.Parameters.AddWithValue("@lName", ticket.PassengerLastName);
                    cmd.Parameters.AddWithValue("@email", ticket.PassengerEmail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", ticket.PassengerPhone ?? (object)DBNull.Value);

                    var newTicketId = (int)await cmd.ExecuteScalarAsync();
                    ticket.TicketId = newTicketId;

                    if (ticket.SelectedAddOns != null && ticket.SelectedAddOns.Any())
                    {
                        string insertAddonQuery = @"
                            INSERT INTO Tickets_AddOns (ticket_id, addon_id)
                            VALUES (@ticketId, @addonId);
                        ";
                        foreach (var addon in ticket.SelectedAddOns)
                        {
                            using var addonCmd = new SqlCommand(insertAddonQuery, connection, transaction);
                            addonCmd.Parameters.AddWithValue("@ticketId", newTicketId);
                            addonCmd.Parameters.AddWithValue("@addonId", addon.AddOnId);
                            await addonCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
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
