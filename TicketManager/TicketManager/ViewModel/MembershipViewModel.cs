using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class MembershipDisplayModel
    {
        public int MembershipId { get; set; }
        public string Name { get; set; }
        public string DiscountText { get; set; }
        public string CardColor { get; set; }

        // NOU: O listă specială pentru UI care va conține textele reducerilor
        public ObservableCollection<string> AddonBenefits { get; set; }

        public MembershipDisplayModel(Membership m)
        {
            MembershipId = m.MembershipId;
            Name = m.Name;
            DiscountText = $"{m.FlightDiscountPercentage}% Off Flights";

            CardColor = Name.ToLower() switch
            {
                "bronze" => "#CD7F32",
                "silver" => "#A9A9A9",
                "gold" => "#DAA520",
                _ => "#2bb8c0"
            };

            // NOU: Generăm textele dinamic citind din lista adusă de Service
            AddonBenefits = new ObservableCollection<string>();
            if (m.AddonDiscounts != null)
            {
                foreach (var discount in m.AddonDiscounts)
                {
                    AddonBenefits.Add($"• {discount.DiscountPercentage}% Off {discount.AddOn.Name}");
                }
            }
        }
    }

    public class MembershipViewModel : ViewModelBase
    {
        private readonly IMembershipService _membershipService;
        public ObservableCollection<MembershipDisplayModel> Memberships { get; set; }

        public MembershipViewModel(IMembershipService membershipService)
        {
            _membershipService = membershipService;
            Memberships = new ObservableCollection<MembershipDisplayModel>();
            LoadMemberships();
        }

        private void LoadMemberships()
        {
            var memberships = _membershipService.GetAllMemberships();
            foreach (var m in memberships)
            {
                Memberships.Add(new MembershipDisplayModel(m));
            }
        }

        public void ExecutePurchase(int membershipId)
        {
            // Verificăm folosind clasa voastră
            if (UserSession.CurrentUser == null) return;

            _membershipService.UpgradeUserMembership(UserSession.CurrentUser.UserId, membershipId);
        }
    }
}