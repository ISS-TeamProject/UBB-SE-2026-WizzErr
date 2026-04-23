using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly DatabaseConnectionFactory dbFactory;

        public MembershipRepository(DatabaseConnectionFactory dbFactory)
        {
            this.dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public Membership? GetMembershipById(int id)
        {
            Membership? membership = null;
            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();
                string query = "SELECT membership_id, name, flight_discount_percentage FROM Memberships WHERE membership_id = @MembershipId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MembershipId", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            membership = new Membership
                            {
                                MembershipId = reader.GetInt32(reader.GetOrdinal("membership_id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                FlightDiscountPercentage = (float)reader.GetByte(reader.GetOrdinal("flight_discount_percentage"))
                            };
                        }
                    }
                }
            }

            return membership;
        }

        public IEnumerable<Membership> GetAllMemberships()
        {
            var memberships = new List<Membership>();
            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();
                string query = "SELECT membership_id, name, flight_discount_percentage FROM Memberships";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        memberships.Add(new Membership
                        {
                            MembershipId = reader.GetInt32(reader.GetOrdinal("membership_id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            FlightDiscountPercentage = (float)reader.GetByte(reader.GetOrdinal("flight_discount_percentage"))
                        });
                    }
                }
            }
            return memberships;
        }

        public IEnumerable<MembershipAddonDiscount> GetAddonDiscounts(int membershipId)
        {
            var discounts = new List<MembershipAddonDiscount>();
            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();
                string query = @"
                    SELECT mad.discount_percentage, 
                           m.membership_id, m.name as membership_name, m.flight_discount_percentage,
                           a.addon_id, a.name as addon_name, a.base_price
                    FROM Memberships_AddOns_Discounts mad
                    INNER JOIN Memberships m ON mad.membership_id = m.membership_id
                    INNER JOIN AddOns a ON mad.addon_id = a.addon_id
                    WHERE mad.membership_id = @MembershipId";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MembershipId", membershipId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var membership = new Membership
                            {
                                MembershipId = reader.GetInt32(reader.GetOrdinal("membership_id")),
                                Name = reader.GetString(reader.GetOrdinal("membership_name")),
                                FlightDiscountPercentage = (float)reader.GetByte(reader.GetOrdinal("flight_discount_percentage"))
                            };

                            var addon = new AddOn
                            {
                                AddOnId = reader.GetInt32(reader.GetOrdinal("addon_id")),
                                Name = reader.GetString(reader.GetOrdinal("addon_name")),
                                BasePrice = (float)reader.GetDecimal(reader.GetOrdinal("base_price"))
                            };

                            var discount = new MembershipAddonDiscount(
                                membership,
                                addon,
                                (float)reader.GetByte(reader.GetOrdinal("discount_percentage")));

                            discounts.Add(discount);
                        }
                    }
                }
            }
            return discounts;
        }
    }
}
