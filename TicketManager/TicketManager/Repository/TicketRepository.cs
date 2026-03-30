using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly DatabaseConnectionFactory _dbFactory;

        public TicketRepository(DatabaseConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public IEnumerable<Ticket> GetTicketsByUserId(int userId)
        {
            var tickets = new List<Ticket>();
            using (var connection = _dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT t.ticket_id, t.user_id, t.flight_id, t.seat, t.price, t.status, 
                           t.passenger_first_name, t.passenger_last_name, t.passenger_email, t.passenger_phone,
                           f.flight_number, f.date as flight_date
                    FROM Tickets t
                    INNER JOIN Flights f ON t.flight_id = f.id
                    WHERE t.user_id = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User { UserId = reader.GetInt32(reader.GetOrdinal("user_id")) };
                            
                            var flight = new Flight 
                            { 
                                FlightId = reader.GetInt32(reader.GetOrdinal("flight_id")),
                                FlightNr = reader.IsDBNull(reader.GetOrdinal("flight_number")) ? null : reader.GetString(reader.GetOrdinal("flight_number")),
                                Date = reader.GetDateTime(reader.GetOrdinal("flight_date"))
                            };

                            var ticket = new Ticket
                            {
                                TicketId = reader.GetInt32(reader.GetOrdinal("ticket_id")),
                                User = user,
                                Flight = flight,
                                Seat = reader.IsDBNull(reader.GetOrdinal("seat")) ? null : reader.GetString(reader.GetOrdinal("seat")),
                                Price = (float)reader.GetDecimal(reader.GetOrdinal("price")),
                                Status = reader.IsDBNull(reader.GetOrdinal("status")) ? null : reader.GetString(reader.GetOrdinal("status")),
                                PassengerFirstName = reader.IsDBNull(reader.GetOrdinal("passenger_first_name")) ? null : reader.GetString(reader.GetOrdinal("passenger_first_name")),
                                PassengerLastName = reader.IsDBNull(reader.GetOrdinal("passenger_last_name")) ? null : reader.GetString(reader.GetOrdinal("passenger_last_name")),
                                PassengerEmail = reader.IsDBNull(reader.GetOrdinal("passenger_email")) ? null : reader.GetString(reader.GetOrdinal("passenger_email")),
                                PassengerPhone = reader.IsDBNull(reader.GetOrdinal("passenger_phone")) ? null : reader.GetString(reader.GetOrdinal("passenger_phone"))
                            };

                            tickets.Add(ticket);
                        }
                    }
                }
            }
            return tickets;
        }

        public void AddTicket(Ticket ticket)
        {
            using (var connection = _dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO Tickets (user_id, flight_id, seat, price, status, passenger_first_name, passenger_last_name, passenger_email, passenger_phone) 
                    OUTPUT INSERTED.ticket_id
                    VALUES (@UserId, @FlightId, @Seat, @Price, @Status, @PassengerFirstName, @PassengerLastName, @PassengerEmail, @PassengerPhone)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", ticket.User.UserId);
                    command.Parameters.AddWithValue("@FlightId", ticket.Flight.FlightId);
                    command.Parameters.AddWithValue("@Seat", ticket.Seat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Price", ticket.Price);
                    command.Parameters.AddWithValue("@Status", ticket.Status ?? "Active");
                    command.Parameters.AddWithValue("@PassengerFirstName", ticket.PassengerFirstName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerLastName", ticket.PassengerLastName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerEmail", ticket.PassengerEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerPhone", ticket.PassengerPhone ?? (object)DBNull.Value);

                    ticket.TicketId = (int)command.ExecuteScalar();
                }
            }
        }

        public void UpdateTicketStatus(int ticketId, string status)
        {
            using (var connection = _dbFactory.GetConnection())
            {
                connection.Open();
                string query = "UPDATE Tickets SET status = @Status WHERE ticket_id = @TicketId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TicketId", ticketId);
                    command.Parameters.AddWithValue("@Status", status);
                    
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddTicketAddOns(int ticketId, IEnumerable<int> addOnIds)
        {
            if (addOnIds == null) return;

            using (var connection = _dbFactory.GetConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string query = "INSERT INTO Tickets_AddOns (ticket_id, addon_id) VALUES (@TicketId, @AddOnId)";
                        
                        using (var command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.Add("@TicketId", System.Data.SqlDbType.Int);
                            command.Parameters.Add("@AddOnId", System.Data.SqlDbType.Int);

                            foreach (var addOnId in addOnIds)
                            {
                                command.Parameters["@TicketId"].Value = ticketId;
                                command.Parameters["@AddOnId"].Value = addOnId;
                                command.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public IEnumerable<string> GetOccupiedSeats(int flightId)
        {
            var seats = new List<string>();
            using (var connection = _dbFactory.GetConnection())
            {
                connection.Open();
                // Assumes "Cancelled" tickets free up the seat
                string query = "SELECT seat FROM Tickets WHERE flight_id = @FlightId AND status != 'Cancelled' AND seat IS NOT NULL";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FlightId", flightId);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            seats.Add(reader.GetString(reader.GetOrdinal("seat")));
                        }
                    }
                }
            }
            return seats;
        }
    }
}