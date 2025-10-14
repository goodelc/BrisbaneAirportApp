using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrisbaneAirportApp
{

    public class FlightRepo
    {
        private readonly Dictionary<string, Flight> _flights = new();
        private readonly HashSet<string> _planeIds = new(StringComparer.OrdinalIgnoreCase);
        private static string Key(string code, Direction d) => $"{code}|{d}";

        public void Add(Flight f)
        {
            if (_planeIds.Contains(f.PlaneId)) throw new InvalidOperationException("plane_id must be unique across all flights");
            var k = Key(f.FlightCode, f.Direction);
            if (_flights.ContainsKey(k)) throw new InvalidOperationException("Flight already exists");
            _flights[k] = f; _planeIds.Add(f.PlaneId);
        }
        public Flight? Get(string code, Direction d) => _flights.TryGetValue(Key(code, d), out var f) ? f : null;
        public IEnumerable<Flight> AllSorted() => _flights.Values.OrderBy(f => f.TimeEffective());
        public IEnumerable<Flight> FindByPlane(string planeId, Direction? d = null)
            => _flights.Values.Where(f => f.PlaneId.Equals(planeId, StringComparison.OrdinalIgnoreCase) && (!d.HasValue || f.Direction == d.Value));
    }
}
