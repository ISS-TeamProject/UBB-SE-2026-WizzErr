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
        private readonly INavigationService _navigationService;

        public ObservableCollection<MembershipDisplayModel> Memberships { get; set; }

        // ── Result state exposed to the View ──────────────────────
        private string _purchaseResultMessage;
        public string PurchaseResultMessage
        {
            get => _purchaseResultMessage;
            set { _purchaseResultMessage = value; OnPropertyChanged(); }
        }

        private bool? _purchaseSucceeded;
        /// <summary>
        /// null = no purchase attempted yet, true = success, false = failure.
        /// The View observes this to decide whether to show a success or error dialog.
        /// </summary>
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

        /// <summary>
        /// Handles the purchase flow. If the user is not logged in, navigates to auth.
        /// On success/failure, sets PurchaseResultMessage and PurchaseSucceeded so the View
        /// can react (e.g., show a dialog). The View never does try/catch or business logic.
        /// </summary>
        private void ExecutePurchase(object parameter)
        {
            // Reset previous result
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