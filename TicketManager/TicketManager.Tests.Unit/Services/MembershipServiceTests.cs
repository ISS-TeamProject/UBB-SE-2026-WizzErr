using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;
using Xunit;

namespace TicketManager.Tests.Unit.Services;

public class MembershipServiceTests 
{
    private const int TargetUserId = 1;
    private const int TargetMembershipId = 2;
    private const float DefaultDiscountPercentage = 15.0f;
    private const string PremiumMembershipName = "Premium";
    private const string ExpectedSuccessMessage = "Your membership purchase was completed successfully.";
    private const string ExpectedFailureMessage = "Membership purchase could not be completed. Please try again.";

    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMembershipRepository> _mockMembershipRepository;
    private readonly MembershipService _membershipService;

    public MembershipServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMembershipRepository = new Mock<IMembershipRepository>();
        _membershipService = new MembershipService(_mockUserRepository.Object, _mockMembershipRepository.Object);
    }

    [Fact]
    public void GetAllMemberships_ValidCall_PopulatesDiscounts()
    {
        var defaultMembership = new Membership { MembershipId = TargetMembershipId, Name = PremiumMembershipName };
        var defaultMemberships = new List<Membership> { defaultMembership };
        var defaultDiscounts = new List<MembershipAddonDiscount> 
        { 
            new MembershipAddonDiscount { DiscountPercentage = DefaultDiscountPercentage } 
        };

        _mockMembershipRepository.Setup(repoWithMemberships => repoWithMemberships.GetAllMemberships())
            .Returns(defaultMemberships);
        _mockMembershipRepository.Setup(repoWithDiscounts => repoWithDiscounts.GetAddonDiscounts(TargetMembershipId))
            .Returns(defaultDiscounts);

        var retrievedMemberships = _membershipService.GetAllMemberships().ToList();

        retrievedMemberships.Should().ContainSingle();
        retrievedMemberships.First().AddonDiscounts.Should().BeEquivalentTo(defaultDiscounts);
        _mockMembershipRepository.Verify(repoToVerifyDiscounts => repoToVerifyDiscounts.GetAddonDiscounts(TargetMembershipId), Times.Once);
    }

    [Fact]
    public void UpgradeUserMembership_ValidUser_UpdatesAndReturnsMembership()
    {
        var defaultMembership = new Membership { MembershipId = TargetMembershipId, Name = PremiumMembershipName };
        var defaultDiscounts = new List<MembershipAddonDiscount> 
        { 
            new MembershipAddonDiscount { DiscountPercentage = DefaultDiscountPercentage } 
        };

        _mockMembershipRepository.Setup(repoWithMembership => repoWithMembership.GetMembershipById(TargetMembershipId))
            .Returns(defaultMembership);
        _mockMembershipRepository.Setup(repoWithDiscounts => repoWithDiscounts.GetAddonDiscounts(TargetMembershipId))
            .Returns(defaultDiscounts);

        var upgradedMembership = _membershipService.UpgradeUserMembership(TargetUserId, TargetMembershipId);

        upgradedMembership.Should().NotBeNull();
        upgradedMembership!.Name.Should().Be(PremiumMembershipName);
        upgradedMembership.AddonDiscounts.Should().BeEquivalentTo(defaultDiscounts);
        
        _mockUserRepository.Verify(repoToVerifyUpdate => repoToVerifyUpdate.UpdateUserMembership(TargetUserId, TargetMembershipId), Times.Once);
    }

    [Fact]
    public void PurchaseMembership_ValidPurchase_SucceedsAndUpdatesSession()
    {
        var defaultSessionUser = UserFixture.CreateValidTestUser();
        UserSession.CurrentUser = defaultSessionUser;
        var defaultMembership = new Membership { MembershipId = TargetMembershipId, Name = PremiumMembershipName };

        _mockMembershipRepository.Setup(repoWithMembership => repoWithMembership.GetMembershipById(TargetMembershipId))
            .Returns(defaultMembership);
        _mockMembershipRepository.Setup(repoWithDiscounts => repoWithDiscounts.GetAddonDiscounts(TargetMembershipId))
            .Returns(new List<MembershipAddonDiscount>());

        var purchaseResult = _membershipService.PurchaseMembership(TargetUserId, TargetMembershipId);

        purchaseResult.Succeeded.Should().BeTrue();
        purchaseResult.Message.Should().Be(ExpectedSuccessMessage);
        UserSession.CurrentUser.Membership.Should().Be(defaultMembership);
    }

    [Fact]
    public void PurchaseMembership_ExceptionThrown_ReturnsFailureResult()
    {
        _mockUserRepository.Setup(repoWithException => repoWithException.UpdateUserMembership(TargetUserId, TargetMembershipId))
            .Throws(new Exception("Database connection failed"));

        var purchaseResult = _membershipService.PurchaseMembership(TargetUserId, TargetMembershipId);

        purchaseResult.Succeeded.Should().BeFalse();
        purchaseResult.Message.Should().Be(ExpectedFailureMessage);
    }
}
