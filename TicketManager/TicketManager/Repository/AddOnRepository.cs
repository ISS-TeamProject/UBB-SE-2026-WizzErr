using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public class AddOnRepository : IAddOnRepository
    {
        private readonly DatabaseConnectionFactory dbFactory;

        public AddOnRepository(DatabaseConnectionFactory dbFactory)
        {
            this.dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        public IEnumerable<AddOn> GetAllAddOns()
        {
            var addons = new List<AddOn>();
            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();
                string query = "SELECT addon_id, name, base_price FROM AddOns";

                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        addons.Add(new AddOn
                        {
                            AddOnId = reader.GetInt32(reader.GetOrdinal("addon_id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            BasePrice = (float)reader.GetDecimal(reader.GetOrdinal("base_price"))
                        });
                    }
                }
            }

            return addons;
        }

        public IEnumerable<AddOn> GetAddOnsByIds(IEnumerable<int> ids)
        {
            var addons = new List<AddOn>();

            if (ids == null || !ids.Any())
            {
                return addons;
            }

            using (var connection = dbFactory.GetConnection())
            {
                connection.Open();

                var parameters = ids.Select((identifier, index) => new { ParameterName = $"@Id{index}", Value = identifier }).ToList();
                string inClause = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));

                string query = $"SELECT addon_id, name, base_price FROM AddOns WHERE addon_id IN ({inClause})";

                using (var command = new SqlCommand(query, connection))
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.ParameterName, param.Value);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            addons.Add(new AddOn
                            {
                                AddOnId = reader.GetInt32(reader.GetOrdinal("addon_id")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                BasePrice = (float)reader.GetDecimal(reader.GetOrdinal("base_price"))
                            });
                        }
                    }
                }
            }

            return addons;
        }
    }
}
