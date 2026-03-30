namespace TicketManager.Repository;


using TicketManager.Domain;

public interface IUserRepository
{ 
    User GetById(int id); 
    User GetByEmail(string email); 
    void AddUser(User user);
    void UpdateUserMembership(int userId, int newMembershipId);
}
