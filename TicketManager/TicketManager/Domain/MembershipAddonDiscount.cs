namespace TicketManager.Domain
{
    public class MembershipAddonDiscount
    {
        public Membership Membership { get; set; }
        public AddOn AddOn { get; set; }
        public float DiscountPercentage { get; set; }

        public MembershipAddonDiscount() { }

        public MembershipAddonDiscount(Membership membership, AddOn addOn, float discountPercentage)
        {
            Membership = membership;
            AddOn = addOn;
            DiscountPercentage = discountPercentage;
        }
    }
}