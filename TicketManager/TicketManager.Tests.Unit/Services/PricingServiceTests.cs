using FluentAssertions;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Unit.Services;

public class PricingServiceTests
{
    private readonly PricingService _pricingService = new PricingService();

    private Flight CreateFlightWithBasePrice(float targetPrice)
    {
        int minutes = (int)(targetPrice / 1.25f);
        var now = DateTime.Now;
        return new Flight
        {
            Route = new Route
            {
                DepartureTime = now,
                ArrivalTime = now.AddMinutes(minutes),
                Capacity = 180
            }
        };
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownWorksForBasicUser()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var user = UserFixture.CreateBasicTestUser();
        var ticket = new Ticket { Price = 100.0f };
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.FinalTotal.Should().Be(100.0f);
        breakdown.MembershipSavings.Should().Be(0);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownAppliesMembershipDiscount()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = 10, AddonDiscounts = new List<MembershipAddonDiscount>() };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        
        var ticket = new Ticket { Price = 100.0f };
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.MembershipSavings.Should().Be(10.0f);
        breakdown.FinalTotal.Should().Be(90.0f);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownIncludesAddOns()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var user = UserFixture.CreateBasicTestUser();
        var ticket = new Ticket { Price = 100.0f };
        ticket.SelectedAddOns.Add(new AddOn { Name = "Bagaj", BasePrice = 50.0f });
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.AddOnsTotal.Should().Be(50.0f);
        breakdown.FinalTotal.Should().Be(150.0f);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownHandlesMultiplePassengersCorrectly()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var user = UserFixture.CreateBasicTestUser();
        var tickets = new List<Ticket> 
        { 
            new Ticket { Price = 100.0f },
            new Ticket { Price = 100.0f }
        };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.BasePriceTotal.Should().Be(200.0f);
        breakdown.FinalTotal.Should().Be(200.0f);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownCalculatesMembershipSavingsCorrectly()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = 10, AddonDiscounts = new List<MembershipAddonDiscount>() };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var tickets = new List<Ticket>
        {
            new Ticket { Price = 100.0f },
            new Ticket { Price = 100.0f }
        };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.MembershipSavings.Should().Be(20.0f);
        breakdown.FinalTotal.Should().Be(180.0f);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownWithComplexDiscountScenario()
    {
        var flight = CreateFlightWithBasePrice(100.0f);
        var addon = new AddOn { AddOnId = 1, Name = "Bagaj", BasePrice = 50.0f };
        var addonDiscount = new MembershipAddonDiscount { AddOn = addon, DiscountPercentage = 20.0f };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = 10.0f,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount }
        };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket1 = new Ticket { Price = 100.0f, SelectedAddOns = new List<AddOn> { addon } };
        var ticket2 = new Ticket { Price = 100.0f, SelectedAddOns = new List<AddOn> { addon } };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.BasePriceTotal.Should().Be(200.0f);
        breakdown.AddOnsTotal.Should().Be(100.0f);
        breakdown.MembershipSavings.Should().BeGreaterThan(0);
    }
}
