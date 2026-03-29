namespace TicketManager.Domain
{
    public class Gate
    {
        public int GateId { get; set; }
        public string GateName { get; set; }

        public Gate() { }

        public Gate(string gateName)
        {
            GateName = gateName;
        }

        public Gate(int gateId, string gateName)
        {
            GateId = gateId;
            GateName = gateName;
        }
    }
}