using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Repository
{
    public interface IMembershipRepository
    {
        Membership GetMembershipById(int id);
        IEnumerable<MembershipAddonDiscount> GetAddonDiscounts(int membershipId);
    }
}