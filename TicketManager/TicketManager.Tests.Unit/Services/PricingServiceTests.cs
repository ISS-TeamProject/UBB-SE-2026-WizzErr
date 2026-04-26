using FluentAssertions;
using System;
using System.Collections.Generic;
using TicketManager.Domain;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;
using Xunit;

namespace TicketManager.Tests.Unit.Services;

public class PricingServiceTests
{
    private const float PricePerMinuteMultiplier = 1.25f;
    private const float ZeroPrice = 0f;
    private const int DefaultFlightCapacity = 180;
    private const float MinimumFlightPrice = 40.0f;
    private const float StandardFlightPrice = 100.0f;
    private const float StandardAddOnPrice1 = 50.0f;
    private const float StandardAddOnPrice2 = 25.0f;
    private const float StandardAddOnPrice3 = 30.0f;
    private const float StandardFlightDiscountPercentage = 10.0f;
    private const float StandardAddOnDiscountPercentage = 20.0f;
    private const int ShortFlightDurationMinutes = 10;
    private const int LongFlightDurationMinutes = 100;
    private const float PercentageDivisor = 100.0f;
    private const int BagageAddOnId = 1;
    private const int PriorityAddOnId = 2;

    private readonly PricingService _pricingService = new PricingService();

    private Flight CreateFlightWithBasePrice(float targetPrice)
    {
        int minutes = (int)(targetPrice / PricePerMinuteMultiplier);
        var now = DateTime.Now;
        return new Flight
        {
            Route = new Route
            {
                DepartureTime = now,
                ArrivalTime = now.AddMinutes(minutes),
                Capacity = DefaultFlightCapacity
            }
        };
    }

    [Fact]
    public void TestThatCalculateBasePriceReturnsZeroWhenFlightOrRouteIsNull()
    {
        var flightWithNoRoute = new Flight { Route = null };
        
        var resultForNullFlight = _pricingService.CalculateBasePrice(null!);
        var resultForNullRoute = _pricingService.CalculateBasePrice(flightWithNoRoute);

        resultForNullFlight.Should().Be(ZeroPrice);
        resultForNullRoute.Should().Be(ZeroPrice);
    }

    [Fact]
    public void TestThatCalculateBasePriceReturnsMinimumPriceForShortFlights()
    {
        var flight = new Flight
        {
            Route = new Route
            {
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddMinutes(ShortFlightDurationMinutes) 
            }
        };

        var price = _pricingService.CalculateBasePrice(flight);

        price.Should().Be(MinimumFlightPrice);
    }

    [Fact]
    public void TestThatCalculateBasePriceReturnsCalculatedPriceForLongFlights()
    {
        var flight = new Flight
        {
            Route = new Route
            {
                DepartureTime = DateTime.Now,
                ArrivalTime = DateTime.Now.AddMinutes(LongFlightDurationMinutes) 
            }
        };

        var price = _pricingService.CalculateBasePrice(flight);

        price.Should().Be(LongFlightDurationMinutes * PricePerMinuteMultiplier);
    }

    [Fact]
    public void TestThatCalculateTotalPriceReturnsBasePricePlusAddOnsWhenNoMembership()
    {
        var ticket = new Ticket
        {
            Price = StandardFlightPrice,
            User = UserFixture.CreateBasicTestUser(),
            SelectedAddOns = new List<AddOn>
            {
                new AddOn { BasePrice = StandardAddOnPrice1 },
                new AddOn { BasePrice = StandardAddOnPrice2 }
            }
        };

        var totalPrice = _pricingService.CalculateTotalPrice(ticket);

        totalPrice.Should().Be(StandardFlightPrice + StandardAddOnPrice1 + StandardAddOnPrice2); 
    }

    [Fact]
    public void TestThatCalculateTotalPriceAppliesFlightDiscountWhenMembershipExists()
    {
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = StandardFlightDiscountPercentage, AddonDiscounts = new List<MembershipAddonDiscount>() };
        var user = UserFixture.CreateValidTestUser(membership: membership);

        var ticket = new Ticket
        {
            Price = StandardFlightPrice,
            User = user,
            SelectedAddOns = new List<AddOn>()
        };

        var totalPrice = _pricingService.CalculateTotalPrice(ticket);

        float expectedPrice = StandardFlightPrice - (StandardFlightPrice * (StandardFlightDiscountPercentage / PercentageDivisor));
        totalPrice.Should().Be(expectedPrice); 
    }

    [Fact]
    public void TestThatCalculateTotalPriceAppliesAddOnDiscountsCorrectly()
    {
        var addon1 = new AddOn { AddOnId = BagageAddOnId, Name = "Bagaj", BasePrice = StandardAddOnPrice1 };
        var addon2 = new AddOn { AddOnId = PriorityAddOnId, Name = "Prioritate", BasePrice = StandardAddOnPrice3 };

        var addonDiscount = new MembershipAddonDiscount { AddOn = addon1, DiscountPercentage = StandardAddOnDiscountPercentage };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = StandardFlightDiscountPercentage,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount }
        };
        
        var user = UserFixture.CreateValidTestUser(membership: membership);

        var ticket = new Ticket
        {
            Price = StandardFlightPrice, 
            User = user,
            SelectedAddOns = new List<AddOn> { addon1, addon2 }
        };

        var totalPrice = _pricingService.CalculateTotalPrice(ticket);

        float expectedFlightPrice = StandardFlightPrice - (StandardFlightPrice * (StandardFlightDiscountPercentage / PercentageDivisor));
        float expectedAddon1Price = StandardAddOnPrice1 - (StandardAddOnPrice1 * (StandardAddOnDiscountPercentage / PercentageDivisor));
        float expectedPrice = expectedFlightPrice + expectedAddon1Price + StandardAddOnPrice3;

        totalPrice.Should().Be(expectedPrice);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownWorksForBasicUser()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var user = UserFixture.CreateBasicTestUser();
        var ticket = new Ticket { Price = StandardFlightPrice };
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.FinalTotal.Should().Be(StandardFlightPrice);
        breakdown.MembershipSavings.Should().Be(ZeroPrice);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownAppliesMembershipDiscount()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = StandardFlightDiscountPercentage, AddonDiscounts = new List<MembershipAddonDiscount>() };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        
        var ticket = new Ticket { Price = StandardFlightPrice };
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        float expectedDiscount = StandardFlightPrice * (StandardFlightDiscountPercentage / PercentageDivisor);
        breakdown.MembershipSavings.Should().Be(expectedDiscount);
        breakdown.FinalTotal.Should().Be(StandardFlightPrice - expectedDiscount);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownIncludesAddOns()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var user = UserFixture.CreateBasicTestUser();
        var ticket = new Ticket { Price = StandardFlightPrice };
        ticket.SelectedAddOns.Add(new AddOn { Name = "Bagaj", BasePrice = StandardAddOnPrice1 });
        var tickets = new List<Ticket> { ticket };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.AddOnsTotal.Should().Be(StandardAddOnPrice1);
        breakdown.FinalTotal.Should().Be(StandardFlightPrice + StandardAddOnPrice1);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownHandlesMultiplePassengersCorrectly()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var user = UserFixture.CreateBasicTestUser();
        var tickets = new List<Ticket> 
        { 
            new Ticket { Price = StandardFlightPrice },
            new Ticket { Price = StandardFlightPrice }
        };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.BasePriceTotal.Should().Be(StandardFlightPrice * tickets.Count);
        breakdown.FinalTotal.Should().Be(StandardFlightPrice * tickets.Count);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownCalculatesMembershipSavingsCorrectly()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var membership = new Membership { MembershipId = 1, Name = "Premium", FlightDiscountPercentage = StandardFlightDiscountPercentage, AddonDiscounts = new List<MembershipAddonDiscount>() };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var tickets = new List<Ticket>
        {
            new Ticket { Price = StandardFlightPrice },
            new Ticket { Price = StandardFlightPrice }
        };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        float expectedSavings = (StandardFlightPrice * (StandardFlightDiscountPercentage / PercentageDivisor)) * tickets.Count;
        breakdown.MembershipSavings.Should().Be(expectedSavings);
        breakdown.FinalTotal.Should().Be((StandardFlightPrice * tickets.Count) - expectedSavings);
    }

    [Fact]
    public void TestThatCalculatePriceBreakdownWithComplexDiscountScenario()
    {
        var flight = CreateFlightWithBasePrice(StandardFlightPrice);
        var addon = new AddOn { AddOnId = BagageAddOnId, Name = "Bagaj", BasePrice = StandardAddOnPrice1 };
        var addonDiscount = new MembershipAddonDiscount { AddOn = addon, DiscountPercentage = StandardAddOnDiscountPercentage };
        var membership = new Membership
        {
            MembershipId = 1,
            Name = "Premium",
            FlightDiscountPercentage = StandardFlightDiscountPercentage,
            AddonDiscounts = new List<MembershipAddonDiscount> { addonDiscount }
        };
        var user = UserFixture.CreateValidTestUser(membership: membership);
        var ticket1 = new Ticket { Price = StandardFlightPrice, SelectedAddOns = new List<AddOn> { addon } };
        var ticket2 = new Ticket { Price = StandardFlightPrice, SelectedAddOns = new List<AddOn> { addon } };
        var tickets = new List<Ticket> { ticket1, ticket2 };

        var breakdown = _pricingService.CalculatePriceBreakdown(flight, user, tickets);

        breakdown.BasePriceTotal.Should().Be(StandardFlightPrice * tickets.Count);
        breakdown.AddOnsTotal.Should().Be(StandardAddOnPrice1 * tickets.Count);
        breakdown.MembershipSavings.Should().BeGreaterThan(ZeroPrice);
    }
}
