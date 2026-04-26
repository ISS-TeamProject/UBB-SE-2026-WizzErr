using Microsoft.Data.SqlClient;

namespace TicketManager.Repository
{
    public interface IDatabaseConnectionFactory
    {
        SqlConnection GetConnection();
    }
}
