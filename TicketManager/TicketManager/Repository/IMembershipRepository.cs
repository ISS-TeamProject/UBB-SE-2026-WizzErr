using System.Collections.Generic;
using TicketManager.Domain;

namespace TicketManager.Domain.Repositories
{
    public interface IMembershipRepository
    {
        Membership GetMembershipById(int id);
        IEnumerable<MembershipAddonDiscount> GetAddonDiscounts(int membershipId);
    }
}