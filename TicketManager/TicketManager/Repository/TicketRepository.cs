using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private const string CancelledStatus = "Cancelled";

        private readonly DatabaseConnectionFactory dbFactory;

        public TicketRepository(DatabaseConnectionFactory dbFactory)
        {
            this.dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public IEnumerable<Ticket> GetTicketsByUserId(int userId)
        {
            var tickets = new List<Ticket>();
            var ticketById = new Dictionary<int, Ticket>();
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT t.ticket_id, t.user_id, t.flight_id, t.seat, t.price, t.status,
                           t.passenger_first_name, t.passenger_last_name, t.passenger_email, t.passenger_phone,
                           f.flight_number, f.date as flight_date,
                           r.route_type, r.departure_time, r.arrival_time,
                           a.city, a.code as airport_code,
                           g.name as gate_name
                    FROM Tickets t
                    INNER JOIN Flights f ON t.flight_id = f.id
                    INNER JOIN Routes r ON f.route_id = r.id
                    INNER JOIN Airports a ON r.airport_id = a.id
                    LEFT JOIN Gates g ON f.gate_id = g.id
                    WHERE t.user_id = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var user = new User { UserId = reader.GetInt32(reader.GetOrdinal("user_id")) };

                            var airport = new Airport
                            {
                                City = reader.IsDBNull(reader.GetOrdinal("city")) ? null : reader.GetString(reader.GetOrdinal("city")),
                                AirportCode = reader.IsDBNull(reader.GetOrdinal("airport_code")) ? null : reader.GetString(reader.GetOrdinal("airport_code"))
                            };

                            var route = new Route
                            {
                                RouteType = reader.IsDBNull(reader.GetOrdinal("route_type")) ? null : reader.GetString(reader.GetOrdinal("route_type")),
                                DepartureTime = reader.GetDateTime(reader.GetOrdinal("departure_time")),
                                ArrivalTime = reader.GetDateTime(reader.GetOrdinal("arrival_time")),
                                Airport = airport
                            };

                            Gate? gate = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("gate_name")))
                            {
                                gate = new Gate
                                {
                                    GateName = reader.GetString(reader.GetOrdinal("gate_name"))
                                };
                            }

                            var flight = new Flight
                            {
                                FlightId = reader.GetInt32(reader.GetOrdinal("flight_id")),
                                FlightNr = reader.IsDBNull(reader.GetOrdinal("flight_number")) ? null : reader.GetString(reader.GetOrdinal("flight_number")),
                                Date = reader.GetDateTime(reader.GetOrdinal("flight_date")),
                                Route = route,
                                Gate = gate
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
                            ticketById[ticket.TicketId] = ticket;
                        }
                    }
                }

                if (ticketById.Count > 0)
                {
                    var parameters = ticketById.Keys
                        .Select((identifier, index) => new { ParameterName = $"@TicketId{index}", Value = identifier })
                        .ToList();

                    string inClause = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
                    string addOnQuery = $@"
                        SELECT ta.ticket_id, a.addon_id, a.name, a.base_price
                        FROM Tickets_AddOns ta
                        INNER JOIN AddOns a ON ta.addon_id = a.addon_id
                        WHERE ta.ticket_id IN ({inClause})";

                    using (var addOnCommand = new SqlCommand(addOnQuery, connection))
                    {
                        foreach (var param in parameters)
                        {
                            addOnCommand.Parameters.AddWithValue(param.ParameterName, param.Value);
                        }

                        using (var addOnReader = addOnCommand.ExecuteReader())
                        {
                            while (addOnReader.Read())
                            {
                                int ticketId = addOnReader.GetInt32(addOnReader.GetOrdinal("ticket_id"));
                                if (!ticketById.TryGetValue(ticketId, out var ticket))
                                {
                                    continue;
                                }

                                ticket.SelectedAddOns.Add(new AddOn
                                {
                                    AddOnId = addOnReader.GetInt32(addOnReader.GetOrdinal("addon_id")),
                                    Name = addOnReader.GetString(addOnReader.GetOrdinal("name")),
                                    BasePrice = (float)addOnReader.GetDecimal(addOnReader.GetOrdinal("base_price"))
                                });
                            }
                        }
                    }
                }
            }

            return tickets;
        }

        public void AddTicket(Ticket ticket)
        {
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO Tickets (user_id, flight_id, seat, price, status, passenger_first_name, passenger_last_name, passenger_email, passenger_phone) 
                    OUTPUT INSERTED.ticket_id
                    VALUES (@UserId, @FlightId, @Seat, @Price, @Status, @PassengerFirstName, @PassengerLastName, @PassengerEmail, @PassengerPhone)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", ticket.User!.UserId);
                    command.Parameters.AddWithValue("@FlightId", ticket.Flight!.FlightId);
                    command.Parameters.AddWithValue("@Seat", ticket.Seat ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Price", ticket.Price);
                    command.Parameters.AddWithValue("@Status", ticket.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerFirstName", ticket.PassengerFirstName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerLastName", ticket.PassengerLastName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerEmail", ticket.PassengerEmail ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@PassengerPhone", ticket.PassengerPhone ?? (object)DBNull.Value);

                    ticket.TicketId = (int)command.ExecuteScalar() !;
                }
            }
        }

        public void UpdateTicketStatus(int ticketId, string status)
        {
            using (var connection = this.dbFactory.GetConnection())
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
            if (addOnIds == null)
            {
                return;
            }

            using (var connection = this.dbFactory.GetConnection())
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
                    catch (Exception)
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
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = $"SELECT seat FROM Tickets WHERE flight_id = @FlightId AND status != '{CancelledStatus}' AND seat IS NOT NULL";

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

        public async Task<bool> SaveTicketsWithAddOnsAsync(List<Ticket> tickets)
        {
            using var connection = this.dbFactory.GetConnection();
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
                              AND status <> @cancelledStatus";

                        using var seatCheckCmd = new SqlCommand(seatLockCheckQuery, connection, transaction);
                        seatCheckCmd.Parameters.AddWithValue("@flightId", ticket.Flight?.FlightId ?? 0);
                        seatCheckCmd.Parameters.AddWithValue("@seat", ticket.Seat);
                        seatCheckCmd.Parameters.AddWithValue("@cancelledStatus", CancelledStatus);

                        int existingSeatCount = Convert.ToInt32(await seatCheckCmd.ExecuteScalarAsync() ?? 0);
                        if (existingSeatCount > 0)
                        {
                            throw new InvalidOperationException("Selected seat is no longer available.");
                        }
                    }

                    string insertTicketQuery = @"
                        INSERT INTO Tickets (user_id, flight_id, seat, price, status, passenger_first_name, passenger_last_name, passenger_email, passenger_phone)
                        OUTPUT INSERTED.ticket_id
                        VALUES (@userId, @flightId, @seat, @price, @status, @fName, @lName, @email, @phone)";

                    using var cmd = new SqlCommand(insertTicketQuery, connection, transaction);
                    float persistedPrice = ticket.CalculateTotalPrice();
                    cmd.Parameters.AddWithValue("@userId", ticket.User?.UserId ?? 0);
                    cmd.Parameters.AddWithValue("@flightId", ticket.Flight?.FlightId ?? 0);
                    cmd.Parameters.AddWithValue("@seat", ticket.Seat ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@price", (decimal)persistedPrice);
                    cmd.Parameters.AddWithValue("@status", ticket.Status ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@fName", ticket.PassengerFirstName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@lName", ticket.PassengerLastName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", ticket.PassengerEmail ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@phone", ticket.PassengerPhone ?? (object)DBNull.Value);

                    var newTicketId = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                    ticket.TicketId = newTicketId;

                    if (ticket.SelectedAddOns != null && ticket.SelectedAddOns.Any())
                    {
                        string insertAddonQuery = @"
                            INSERT INTO Tickets_AddOns (ticket_id, addon_id)
                            VALUES (@ticketId, @addonId)";

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
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
