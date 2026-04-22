using System;
using System.Linq;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DatabaseConnectionFactory dbFactory;
        private readonly IMembershipRepository membershipRepository;

        public UserRepository(DatabaseConnectionFactory dbFactory, IMembershipRepository membershipRepository)
        {
            this.dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            this.membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
        }

        public User? GetById(int id)
        {
            User? user = null;
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT u.user_id, u.email, u.phone, u.username, u.password_hash, 
                           u.membership_id, m.name as membership_name, m.flight_discount_percentage
                    FROM Users u
                    LEFT JOIN Memberships m ON u.membership_id = m.membership_id
                    WHERE u.user_id = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = this.MapUser(reader);
                        }
                    }
                }
            }

            return user;
        }

        public User? GetByEmail(string email)
        {
            User? user = null;
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT u.user_id, u.email, u.phone, u.username, u.password_hash, 
                           u.membership_id, m.name as membership_name, m.flight_discount_percentage
                    FROM Users u
                    LEFT JOIN Memberships m ON u.membership_id = m.membership_id
                    WHERE u.email = @Email";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = this.MapUser(reader);
                        }
                    }
                }
            }

            return user;
        }

        public void AddUser(User user)
        {
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    INSERT INTO Users (email, phone, username, password_hash, membership_id) 
                    VALUES (@Email, @Phone, @Username, @PasswordHash, @MembershipId)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Phone", user.Phone ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Username", user.Username);
                    command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                    command.Parameters.AddWithValue("@MembershipId", user.Membership?.MembershipId ?? (object)DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateUserMembership(int userId, int newMembershipId)
        {
            using (var connection = this.dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    UPDATE Users 
                    SET membership_id = @MembershipId
                    WHERE user_id = @UserId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@MembershipId", newMembershipId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private User MapUser(SqlDataReader reader)
        {
            int membershipIdOrdinal = reader.GetOrdinal("membership_id");
            Membership? membership = null;

            if (!reader.IsDBNull(membershipIdOrdinal))
            {
                membership = new Membership
                {
                    MembershipId = reader.GetInt32(membershipIdOrdinal),
                    Name = reader.GetString(reader.GetOrdinal("membership_name")),
                    FlightDiscountPercentage = (float)reader.GetByte(reader.GetOrdinal("flight_discount_percentage"))
                };

                membership.AddonDiscounts = this.membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();
            }

            return new User(
                reader.GetInt32(reader.GetOrdinal("user_id")),
                reader.GetString(reader.GetOrdinal("email")),
                reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                reader.GetString(reader.GetOrdinal("username")),
                reader.GetString(reader.GetOrdinal("password_hash")),
                membership);
        }
    }
}