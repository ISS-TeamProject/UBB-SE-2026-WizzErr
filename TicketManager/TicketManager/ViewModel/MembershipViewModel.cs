using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
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

            AddonBenefits = new ObservableCollection<string>();
            if (m.AddonDiscounts != null)
            {
                foreach (var discount in m.AddonDiscounts)
                {
                    AddonBenefits.Add($"a€¢ {discount.DiscountPercentage}% Off {discount.AddOn.Name}");
                }
            }
        }
    }

    public class MembershipViewModel : ViewModelBase
    {
        private readonly IMembershipService _membershipService;
        private readonly INavigationService _navigationService;

        public ObservableCollection<MembershipDisplayModel> Memberships { get; set; }

        private string _purchaseResultMessage;
        public string PurchaseResultMessage
        {
            get => _purchaseResultMessage;
            set { _purchaseResultMessage = value; OnPropertyChanged(); }
        }

        private bool? _purchaseSucceeded;




        public bool? PurchaseSucceeded
        {
            get => _purchaseSucceeded;
            set { _purchaseSucceeded = value; OnPropertyChanged(); }
        }

        public ICommand PurchaseCommand { get; }

        public MembershipViewModel(IMembershipService membershipService, INavigationService navigationService)
        {
            _membershipService = membershipService;
            _navigationService = navigationService;
            Memberships = new ObservableCollection<MembershipDisplayModel>();

            PurchaseCommand = new RelayCommand(param => ExecutePurchase(param));

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





        private void ExecutePurchase(object parameter)
        {

            PurchaseSucceeded = null;
            PurchaseResultMessage = string.Empty;

            if (UserSession.CurrentUser == null)
            {
                _navigationService.NavigateTo(typeof(View.AuthPage));
                return;
            }

            if (parameter is not int membershipId)
                return;

            try
            {
                var updatedMembership = _membershipService.UpgradeUserMembership(
                    UserSession.CurrentUser.UserId, membershipId);
                UserSession.CurrentUser.Membership = updatedMembership;

                PurchaseSucceeded = true;
                PurchaseResultMessage = "Your membership purchase was completed successfully.";
            }
            catch
            {
                PurchaseSucceeded = false;
                PurchaseResultMessage = "Membership purchase could not be completed. Please try again.";
            }
        }
    }
}