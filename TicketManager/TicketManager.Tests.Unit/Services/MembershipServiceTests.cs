using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;

namespace TicketManager.Tests.Unit.Services;

public class MembershipServiceTests
{
    private const int SilverMembershipId = 1;
    private const int GoldMembershipId = 2;
    private const int PremiumMembershipId = 3;
    private const int InvalidMembershipId = 99;
    private const int TestUserId = 10;
    private const int AlternativeTestUserId = 1;
    private const float SilverFlightDiscount = 5.0f;
    private const float GoldFlightDiscount = 15.0f;
    private const float PremiumFlightDiscount = 20.0f;
    private const float Addon1BasePrice = 25.0f;
    private const float Addon2BasePrice = 10.0f;
    private const float Discount1Percentage = 10.0f;
    private const float Discount2Percentage = 20.0f;
    private const float PremiumAddonDiscountPercentage = 15.0f;
    private const int AddonId1 = 1;
    private const int AddonId2 = 2;

    private readonly Mock<IMembershipRepository> _mockMembershipRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly MembershipService _service;

    public MembershipServiceTests()
    {
        _mockMembershipRepository = new Mock<IMembershipRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _service = new MembershipService(_mockUserRepository.Object, _mockMembershipRepository.Object);
    }

    [Fact]
    public void TestThatGetAllMembershipsReturnsExistingTiers()
    {
        var memberships = new List<Membership>
        {
            new Membership { MembershipId = SilverMembershipId, Name = "Argint", FlightDiscountPercentage = SilverFlightDiscount },
            new Membership { MembershipId = GoldMembershipId, Name = "Aur", FlightDiscountPercentage = GoldFlightDiscount }
        };
        _mockMembershipRepository.Setup(repoWithExistingTiers => repoWithExistingTiers.GetAllMemberships()).Returns(memberships);
        _mockMembershipRepository.Setup(repoWithNoDiscounts => repoWithNoDiscounts.GetAddonDiscounts(It.IsAny<int>())).Returns(new List<MembershipAddonDiscount>());

        var membershipsList = _service.GetAllMemberships();

        membershipsList.Should().HaveCount(memberships.Count);
    }

    [Fact]
    public void TestThatUpgradeUserMembershipCallsRepositoryWithCorrectId()
    {
        var membership = new Membership { MembershipId = PremiumMembershipId, Name = "Premium Plus" };
        _mockMembershipRepository.Setup(repoWithPremiumPlus => repoWithPremiumPlus.GetMembershipById(PremiumMembershipId)).Returns(membership);
        _mockMembershipRepository.Setup(repoWithNoAddonDiscounts => repoWithNoAddonDiscounts.GetAddonDiscounts(PremiumMembershipId)).Returns(new List<MembershipAddonDiscount>());

        var upgradedMembership = _service.UpgradeUserMembership(TestUserId, PremiumMembershipId);

        upgradedMembership!.Name.Should().Be("Premium Plus");
        _mockUserRepository.Verify(repoToVerifyUpdate => repoToVerifyUpdate.UpdateUserMembership(TestUserId, PremiumMembershipId), Times.Once);
    }

    [Fact]
    public void TestThatUpgradeUserMembershipReturnsNullIfNotFoundInRepo()
    {
        _mockMembershipRepository.Setup(repoWithMissingMembership => repoWithMissingMembership.GetMembershipById(InvalidMembershipId)).Returns((Membership?)null);

        var upgradedMembership = _service.UpgradeUserMembership(AlternativeTestUserId, InvalidMembershipId);

        upgradedMembership.Should().BeNull();
    }

    [Fact]
    public void TestThatGetAllMembershipsLoadsAddOnDiscountsForEachMembership()
    {
        var addon1 = new AddOn { AddOnId = AddonId1, Name = "Bagaj", BasePrice = Addon1BasePrice };
        var addon2 = new AddOn { AddOnId = AddonId2, Name = "Prioritate", BasePrice = Addon2BasePrice };
        var discount1 = new MembershipAddonDiscount { AddOn = addon1, DiscountPercentage = Discount1Percentage };
        var discount2 = new MembershipAddonDiscount { AddOn = addon2, DiscountPercentage = Discount2Percentage };

        var memberships = new List<Membership>
        {
            new Membership { MembershipId = SilverMembershipId, Name = "Argint", FlightDiscountPercentage = SilverFlightDiscount },
            new Membership { MembershipId = GoldMembershipId, Name = "Aur", FlightDiscountPercentage = GoldFlightDiscount }
        };

        _mockMembershipRepository.Setup(repoWithMemberships => repoWithMemberships.GetAllMemberships()).Returns(memberships);
        _mockMembershipRepository.Setup(repoWithSilverDiscounts => repoWithSilverDiscounts.GetAddonDiscounts(SilverMembershipId)).Returns(new List<MembershipAddonDiscount> { discount1 });
        _mockMembershipRepository.Setup(repoWithGoldDiscounts => repoWithGoldDiscounts.GetAddonDiscounts(GoldMembershipId)).Returns(new List<MembershipAddonDiscount> { discount2 });

        var membershipsList = _service.GetAllMemberships().ToList();

        membershipsList.Should().HaveCount(memberships.Count);
        membershipsList.First(silverMembership => silverMembership.MembershipId == SilverMembershipId).AddonDiscounts.Should().HaveCount(1);
        membershipsList.First(goldMembership => goldMembership.MembershipId == GoldMembershipId).AddonDiscounts.Should().HaveCount(1);
    }

    [Fact]
    public void TestThatUpgradeUserMembershipLoadsAddonDiscounts()
    {
        var addon = new AddOn { AddOnId = AddonId1, Name = "Bagaj", BasePrice = Addon1BasePrice };
        var discount = new MembershipAddonDiscount { AddOn = addon, DiscountPercentage = PremiumAddonDiscountPercentage };
        var membership = new Membership { MembershipId = PremiumMembershipId, Name = "Premium Plus", FlightDiscountPercentage = PremiumFlightDiscount };

        _mockMembershipRepository.Setup(repoWithMembershipToUpgrade => repoWithMembershipToUpgrade.GetMembershipById(PremiumMembershipId)).Returns(membership);
        _mockMembershipRepository.Setup(repoWithMembershipDiscounts => repoWithMembershipDiscounts.GetAddonDiscounts(PremiumMembershipId)).Returns(new List<MembershipAddonDiscount> { discount });

        var upgradedMembership = _service.UpgradeUserMembership(TestUserId, PremiumMembershipId);

        upgradedMembership.Should().NotBeNull();
        upgradedMembership!.AddonDiscounts.Should().HaveCount(1);
        upgradedMembership.AddonDiscounts.First().DiscountPercentage.Should().Be(PremiumAddonDiscountPercentage);
        _mockUserRepository.Verify(repoToVerifyMembershipUpdate => repoToVerifyMembershipUpdate.UpdateUserMembership(TestUserId, PremiumMembershipId), Times.Once);
    }
}
