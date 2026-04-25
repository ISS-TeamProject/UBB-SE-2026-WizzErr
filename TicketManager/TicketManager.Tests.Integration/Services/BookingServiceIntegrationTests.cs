using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TicketManager.Domain;
using TicketManager.Repository;
using TicketManager.Service;
using TicketManager.Tests.Unit.Fixtures;

namespace TicketManager.Tests.Integration.Services;

public class BookingServiceIntegrationTests : BaseIntegrationTest
{
    private const float DefaultBasePrice = 150.0f;
    private const int DefaultFlightCapacity = 180;
    private const int DefaultOccupiedSeats = 50;
    private const int DefaultRequestedPassengers = 10;
    private const int UniqueCodeStartIndex = 0;
    private const int UniqueCodeLength = 4;
    private const int ExpectedTicketCount = 1;

    private readonly ITicketRepository _ticketRepository;
    private readonly IAddOnRepository _addOnRepository;
    private readonly IUserRepository _userRepository;
    private readonly BookingService _bookingService;

    public BookingServiceIntegrationTests()
    {
        var databaseConnectionFactory = new DatabaseConnectionFactory(GetTestConnectionString());
        _ticketRepository = new TicketRepository(databaseConnectionFactory);
        _addOnRepository = new AddOnRepository(databaseConnectionFactory);
        var membershipRepository = new MembershipRepository(databaseConnectionFactory);
        _userRepository = new UserRepository(databaseConnectionFactory, membershipRepository);
        _bookingService = new BookingService(_ticketRepository, _addOnRepository);
    }

    [Fact]
    public async Task TestThatTicketsCanBeCreatedAndSaved()
    {
        var flightId = GetFirstAvailableFlightId();
        var flight = new Flight { FlightId = flightId };
        var code = Guid.NewGuid().ToString().Substring(UniqueCodeStartIndex, UniqueCodeLength);
        var user = new User { Email = $"mrc.popa_{code}@gmail.com", Username = $"MirceaP_{code}", PasswordHash = "Mircea123!" };
        _userRepository.AddUser(user);
        var dbUser = _userRepository.GetByEmail(user.Email);

        var passengers = new List<PassengerData>
        {
            new PassengerData { FirstName = "Mircea", LastName = "Popa", Email = user.Email, Phone = "0722334455", SelectedSeat = $"{code}_1A" }
        };

        var tickets = _bookingService.CreateTickets(flight, dbUser!, passengers, DefaultBasePrice);
        var saveResult = await _bookingService.SaveTicketsAsync(tickets);

        saveResult.Should().BeTrue();
        tickets.Should().HaveCount(ExpectedTicketCount);
    }

    [Fact]
    public void TestThatMaxPassengersIsCalculatedCorrectly()
    {
        var maxPassengers = _bookingService.CalculateMaxPassengers(DefaultFlightCapacity, DefaultOccupiedSeats, DefaultRequestedPassengers);
        maxPassengers.Should().Be(DefaultRequestedPassengers);
    }
}


