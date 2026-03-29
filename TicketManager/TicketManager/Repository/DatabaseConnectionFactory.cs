using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TicketManager.Repository
{
    public class DatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public DatabaseConnectionFactory()
        {
            // Build the configuration to read from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // optional: false strictly requires the file to exist. The app will crash early if it is missing.
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();

            // Fetch the connection string named "DefaultConnection"
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new System.InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}