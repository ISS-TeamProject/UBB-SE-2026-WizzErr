using TicketManager.Domain;

namespace TicketManager.Tests.Unit.Fixtures;

public static class UserFixture
{
    public static User CreateValidTestUser(
        string email = "ion.popescu@gmail.com",
        string username = "ionPopescu99",
        string phone = "0722112233",
        Membership? membership = null)
    {
        return new User
        {
            UserId = 1,
            Email = email,
            Username = username,
            Phone = phone,
            Membership = membership
        };
    }

    public static User CreateBasicTestUser()
    {
        return CreateValidTestUser(
            email: "gheorghe.vasile@yahoo.ro",
            username: "gVasile_77",
            phone: "0744556677");
    }
}
