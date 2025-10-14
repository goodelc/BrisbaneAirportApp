using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrisbaneAirportApp
{
    public class UserRepo
    {
        private readonly Dictionary<string, BaseUser> _users = new(StringComparer.OrdinalIgnoreCase);
        public void Add(BaseUser user)
        {
            if (_users.ContainsKey(user.Email)) throw new InvalidOperationException("Email already registered");
            _users[user.Email] = user;
        }
        public BaseUser? Get(string email) => _users.TryGetValue(email, out var u) ? u : null;
        public IEnumerable<BaseUser> All() => _users.Values;
    }
}
