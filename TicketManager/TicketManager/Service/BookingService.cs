using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;
using TicketManager.Repository;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace TicketManager.Service
{
    public class BookingService
    {
        private readonly DatabaseConnectionFactory _connectionFactory;
        private readonly ITicketRepository _ticketRepository;
        private readonly IAddOnRepository _addOnRepository;

        public BookingService(DatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _ticketRepository = new TicketRepository(connectionFactory);
            _addOnRepository = new AddOnRepository(connectionFactory);
        }

        // CalculateFinalPrice looping through multiple passengers and applying membership discounts
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

        // CreateTickets mapping the Dictionary of AddOns to Tickets 
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
                    Status = "Active",
                    SelectedAddOns = pass.SelectedAddOns.ToList()
                };
                tickets.Add(ticket);
            }

            return tickets;
        }

        // Save tickets to DB
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
                    cmd.Parameters.AddWithValue("@seat", ticket.Seat ?? (object)System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", (decimal)persistedPrice);
                    cmd.Parameters.AddWithValue("@status", ticket.Status);
                    cmd.Parameters.AddWithValue("@fName", ticket.PassengerFirstName);
                    cmd.Parameters.AddWithValue("@lName", ticket.PassengerLastName);
                    cmd.Parameters.AddWithValue("@email", ticket.PassengerEmail ?? (object)System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", ticket.PassengerPhone ?? (object)System.DBNull.Value);

                    var newTicketId = (int)await cmd.ExecuteScalarAsync();
                    ticket.TicketId = newTicketId;

                    // Addons
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

        // CancelTicket database update logic for partial cancellations
        public async Task<bool> CancelTicketAsync(int ticketId)
        {
            try
            {
                _ticketRepository.UpdateTicketStatus(ticketId, "Cancelled");
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
