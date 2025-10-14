using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrisbaneAirportApp
{
    public class TicketRepo
    {
        private readonly Dictionary<string, Ticket> _tickets = new();
        public void Add(Ticket t) => _tickets[t.TicketId] = t;
        public IEnumerable<Ticket> ForUser(string email) => _tickets.Values.Where(t => t.UserEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

}
