using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public interface IAddOnRepository
    {
        IEnumerable<AddOn> GetAllAddOns();
        IEnumerable<AddOn> GetAddOnsByIds(IEnumerable<int> ids);
    }
}
