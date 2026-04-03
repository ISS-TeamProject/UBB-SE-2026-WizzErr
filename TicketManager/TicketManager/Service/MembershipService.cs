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
            // 1. Luăm toate abonamentele
            var memberships = _membershipRepository.GetAllMemberships().ToList();

            // 2. Pentru fiecare abonament, mergem în baza de date și îi aducem și reducerile extra
            foreach (var membership in memberships)
            {
                membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(membership.MembershipId).ToList();
            }

            return memberships;
        }

        public void UpgradeUserMembership(int userId, int newMembershipId)
        {
            _userRepository.UpdateUserMembership(userId, newMembershipId);

            // Folosim clasa voastră existentă: UserSession
            if (UserSession.CurrentUser != null && UserSession.CurrentUser.UserId == userId)
            {
                var membership = _membershipRepository.GetMembershipById(newMembershipId);
                if (membership != null)
                {
                    membership.AddonDiscounts = _membershipRepository.GetAddonDiscounts(newMembershipId).ToList();
                }

                UserSession.CurrentUser.Membership = membership;
            }
        }
    }
}