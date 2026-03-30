using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Domain.Repositories
{
    public interface IAddOnRepository
    {
        IEnumerable<AddOn> GetAllAddOns();
        IEnumerable<AddOn> GetAddOnsByIds(IEnumerable<int> ids);
    }
}