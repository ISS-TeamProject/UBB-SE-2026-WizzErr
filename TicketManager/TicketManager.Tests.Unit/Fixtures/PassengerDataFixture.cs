using TicketManager.Domain;

namespace TicketManager.Tests.Unit.Fixtures;

public static class PassengerDataFixture
{
    public static PassengerData CreateValidPassengerData(
        string firstName = "Maria",
        string lastName = "Ionescu",
        string email = "maria.io@gmail.com",
        string phone = "0733112233",
        string selectedSeat = "1A")
    {
        return new PassengerData
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = phone,
            SelectedSeat = selectedSeat,
            SelectedAddOns = new List<AddOn>()
        };
    }

    public static List<PassengerData> CreateValidPassengerList(int count = 2)
    {
        var randomPrefix = Guid.NewGuid().ToString().Substring(0, 4);
        var passengers = new List<PassengerData>();
        var firstNames = new[] { "Andrei", "Elena", "Sorin", "Cristina", "Mihai" };
        var lastNames = new[] { "Radu", "Stan", "Stoica", "Dumitru", "Marin" };
        
        for (int i = 0; i < count; i++)
        {
            passengers.Add(CreateValidPassengerData(
                firstName: firstNames[i % firstNames.Length],
                lastName: lastNames[i % lastNames.Length],
                selectedSeat: $"{randomPrefix}_{i + 1}B"));
        }
        return passengers;
    }
}
