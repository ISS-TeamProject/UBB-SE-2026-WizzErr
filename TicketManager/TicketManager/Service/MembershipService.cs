using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class MembershipService : IMembershipService
    {
        private readonly IUserRepository userRepository;
        private readonly IMembershipRepository membershipRepository;

        public MembershipService(IUserRepository userRepository, IMembershipRepository membershipRepository)
        {
            this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
        }

        public IEnumerable<Membership> GetAllMemberships()
        {
            var memberships = membershipRepository.GetAllMemberships().ToList();

            foreach (var membership in memberships)
            {
                membership.AddonDiscounts = membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();
            }

            return memberships;
        }

        public Membership UpgradeUserMembership(int userId, int newMembershipId)
        {
            userRepository.UpdateUserMembership(userId, newMembershipId);

            var membership = membershipRepository.GetMembershipById(newMembershipId);
            if (membership != null)
            {
                membership.AddonDiscounts = membershipRepository.GetAddonDiscounts(newMembershipId).ToList();
            }

            return membership;
        }
    }
}