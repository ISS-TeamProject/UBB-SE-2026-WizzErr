using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketManager.Domain;

namespace TicketManager.Service
{
    public interface IMembershipService
    {
        IEnumerable<Membership> GetAllMemberships();

        Membership? UpgradeUserMembership(int userId, int newMembershipId);
    }
}
