namespace TicketManager.Domain
{
    public class Company
    {
        public int CompanyId { get; set; }
        public string Name { get; set; }

        public Company() { }

        public Company(string name)
        {
            Name = name;
        }

        public Company(int companyId, string name)
        {
            CompanyId = companyId;
            Name = name;
        }
    }
}