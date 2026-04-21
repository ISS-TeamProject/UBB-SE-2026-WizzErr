using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;

namespace TicketManager.Service
{
    public class MembershipService : IMembershipService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMembershipRepository _membershipRepository;

        public MembershipService(IUserRepository userRepository, IMembershipRepository membershipRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _membershipRepository = membershipRepository ?? throw new ArgumentNullException(nameof(membershipRepository));
        }

        public IEnumerable<Membership> GetAllMemberships()
        {

            var memberships = _membershipRepository.GetAllMemberships().ToList();

            foreach (var membership in memberships)
            {
                membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();
            }

            return memberships;
        }

        public Membership UpgradeUserMembership(int userId, int newMembershipId)
        {
            _userRepository.UpdateUserMembership(userId, newMembershipId);

            var membership = _membershipRepository.GetMembershipById(newMembershipId);
            if (membership != null)
            {
                membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(newMembershipId).ToList();
            }

            return membership;
        }
    }
}