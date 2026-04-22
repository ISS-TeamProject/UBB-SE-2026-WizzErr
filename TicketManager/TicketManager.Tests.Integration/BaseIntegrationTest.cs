using Microsoft.Data.SqlClient;
using TicketManager.Repository;

namespace TicketManager.Tests.Integration;

public abstract class BaseIntegrationTest
{
    protected string GetTestConnectionString()
    {
        return "Server=DESKTOP-NENJ194\\SQLEXPRESS;Database=TicketsDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    protected int GetFirstAvailableFlightId()
    {
        using var connection = new SqlConnection(GetTestConnectionString());
        connection.Open();
        using var command = new SqlCommand("SELECT TOP 1 id FROM Flights", connection);
        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value)
            throw new Exception("Nu s-au gasit zboruri in baza de date. Va rugam sa rulati scriptul de seed.");
        return Convert.ToInt32(result);
    }
}
