using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TicketManager.Domain;
using TicketManager.Service;

namespace TicketManager.ViewModel
{
    public class MembershipDisplayModel
    {
        public int MembershipId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string DiscountText { get; set; } = string.Empty;

        public string CardColor { get; set; } = string.Empty;

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
                    AddonBenefits.Add($"• {discount.DiscountPercentage}% Off {discount.AddOn.Name}");
                }
            }
        }
    }

    public class MembershipViewModel : ViewModelBase
    {
        private readonly IMembershipService membershipService;
        private readonly INavigationService navigationService;

        private string purchaseResultMessage = string.Empty;

        private bool? purchaseSucceeded;

        public MembershipViewModel(IMembershipService membershipService, INavigationService navigationService)
        {
            this.membershipService = membershipService;
            this.navigationService = navigationService;
            this.Memberships = new ObservableCollection<MembershipDisplayModel>();

            this.PurchaseCommand = new RelayCommand(param => this.ExecutePurchase(param));

            this.LoadMemberships();
        }

        public ObservableCollection<MembershipDisplayModel> Memberships { get; set; }

        public string PurchaseResultMessage
        {
            get => this.purchaseResultMessage;
            set
            {
                this.purchaseResultMessage = value;
                this.OnPropertyChanged();
            }
        }

        public bool? PurchaseSucceeded
        {
            get => this.purchaseSucceeded;
            set
            {
                this.purchaseSucceeded = value;
                this.OnPropertyChanged();
            }
        }

        public ICommand PurchaseCommand { get; }

        private void LoadMemberships()
        {
            var memberships = this.membershipService.GetAllMemberships();
            foreach (var m in memberships)
            {
                this.Memberships.Add(new MembershipDisplayModel(m));
            }
        }

        private void ExecutePurchase(object? parameter)
        {
            this.PurchaseSucceeded = null;
            this.PurchaseResultMessage = string.Empty;

            if (UserSession.CurrentUser == null)
            {
                this.navigationService.NavigateTo(typeof(View.AuthPage));
                return;
            }

            if (parameter is not int membershipId)
            {
                return;
            }

            try
            {
                var updatedMembership = this.membershipService.UpgradeUserMembership(
                    UserSession.CurrentUser.UserId, membershipId);
                UserSession.CurrentUser.Membership = updatedMembership;

                this.PurchaseSucceeded = true;
                this.PurchaseResultMessage = "Your membership purchase was completed successfully.";
            }
            catch
            {
                this.PurchaseSucceeded = false;
                this.PurchaseResultMessage = "Membership purchase could not be completed. Please try again.";
            }
        }
    }
}