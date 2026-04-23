using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Domain;

public class TicketTests
{
    [Fact]
    public void TestThatCalculateTotalPriceWithoutMembershipReturnsBasePrice()
    {
        var ticket = new Ticket { Price = 100.0f, User = null };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(100.0f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceWithMembershipAppliesFlightDiscount()
    {
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = 10 };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket = new Ticket { Price = 100.0f, User = user, SelectedAddOns = new List<AddOn>() };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(90.0f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceIncludesAddOnsWithoutMembership()
    {
        var addon1 = AddOnFixture.CreateValidAddOn(1, "Bagaj mana", 25.0f);
        var addon2 = AddOnFixture.CreateValidAddOn(2, "Bagaj cala", 15.0f);
        var ticket = new Ticket { Price = 100.0f, User = null, SelectedAddOns = new List<AddOn> { addon1, addon2 } };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(140.0f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceAppliesAddOnDiscountWhenMembershipExists()
    {
        var addon = AddOnFixture.CreateValidAddOn(1, "Bagaj", 25.0f);
        var addonDiscount = new MembershipAddonDiscount { AddOn = addon, DiscountPercentage = 20.0f };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = 0,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount }
        };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket = new Ticket { Price = 100.0f, User = user, SelectedAddOns = new List<AddOn> { addon } };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(120.0f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceCombinesFlightAndAddOnDiscounts()
    {
        var addon = AddOnFixture.CreateValidAddOn(1, "Bagaj", 50.0f);
        var addonDiscount = new MembershipAddonDiscount { AddOn = addon, DiscountPercentage = 20.0f };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = 10.0f,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount }
        };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket = new Ticket { Price = 100.0f, User = user, SelectedAddOns = new List<AddOn> { addon } };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(130.0f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceWithMultipleAddOnsAppliesIndividualDiscounts()
    {
        var addon1 = AddOnFixture.CreateValidAddOn(1, "Bagaj mana", 25.0f);
        var addon2 = AddOnFixture.CreateValidAddOn(2, "Prioritate", 15.0f);
        var addonDiscount1 = new MembershipAddonDiscount { AddOn = addon1, DiscountPercentage = 20.0f };
        var addonDiscount2 = new MembershipAddonDiscount { AddOn = addon2, DiscountPercentage = 50.0f };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = 0,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount1, addonDiscount2 }
        };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket = new Ticket { Price = 100.0f, User = user, SelectedAddOns = new List<AddOn> { addon1, addon2 } };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(127.5f);
    }

    [Fact]
    public void TestThatCalculateTotalPriceHandlesEmptyAddOnsListWithMembership()
    {
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = 10 };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket = new Ticket { Price = 100.0f, User = user, SelectedAddOns = new List<AddOn>() };

        var result = ticket.CalculateTotalPrice();

        result.Should().Be(90.0f);
    }
}

