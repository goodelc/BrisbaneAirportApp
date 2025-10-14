using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrisbaneAirportApp
{
    public class FlightService
    {
        private readonly FlightRepo _flights; private readonly TicketRepo _tickets; private readonly UserRepo _users;
        public FlightService(FlightRepo fr, TicketRepo tr, UserRepo ur) { _flights = fr; _tickets = tr; _users = ur; }

        private void CheckFlightIds(string airline, string flightCode, string city, string planeId)
        {
            if (!Validators.ValidAirlineCode(airline)) throw new InvalidOperationException("Invalid Airline Code");
            if (!Validators.ValidFlightId(flightCode)) throw new InvalidOperationException("Invalid Flight ID");
            if (!Validators.ValidPlaneId(planeId)) throw new InvalidOperationException("Invalid Plane ID");
            if (airline != flightCode[..3] || airline != planeId[..3]) throw new InvalidOperationException("Airline code mismatch");
            if (!Validators.ValidCity(city)) throw new InvalidOperationException("Invalid City");
        }

        public Flight RegisterArrival(FlightManager m, string airline, string code, string fromCity, string planeId, DateTime time)
        { _ = m ?? throw new ArgumentNullException(nameof(m)); CheckFlightIds(airline, code, fromCity, planeId); var f = new Flight(airline, code, fromCity, planeId, time, Direction.ARRIVAL); _flights.Add(f); return f; }

        public Flight RegisterDeparture(FlightManager m, string airline, string code, string toCity, string planeId, DateTime time)
        { _ = m ?? throw new ArgumentNullException(nameof(m)); CheckFlightIds(airline, code, toCity, planeId); var f = new Flight(airline, code, toCity, planeId, time, Direction.DEPARTURE); _flights.Add(f); return f; }

        public IEnumerable<Flight> ListFlights() => _flights.AllSorted();

        private int PointsFor(Flight f) => AppConsts.CityPoints[f.OtherCity];

        private void CheckUserBooking(BaseUser user, Flight target)
        {
            var ts = _tickets.ForUser(user.Email).ToList();
            bool hasArr = ts.Any(t => t.Direction == Direction.ARRIVAL);
            bool hasDep = ts.Any(t => t.Direction == Direction.DEPARTURE);
            if (target.Direction == Direction.ARRIVAL && hasArr) throw new InvalidOperationException("User already has an arrival flight");
            if (target.Direction == Direction.DEPARTURE && hasDep) throw new InvalidOperationException("User already has a departure flight");
            if (target.Direction == Direction.DEPARTURE && hasArr)
            {
                var at = ts.First(t => t.Direction == Direction.ARRIVAL);
                var af = _flights.Get(at.FlightCode, Direction.ARRIVAL)!;
                if (target.TimeEffective() <= af.TimeEffective()) throw new InvalidOperationException("Departing Flight Must be After the Arrival Flight");
            }
        }

        static IEnumerable<string> AllSeats()
        {
            for (int r = 1; r <= AppConsts.SeatRowsDefault; r++)
                foreach (var c in AppConsts.SeatColumnsDefault)
                    yield return $"{r}{c}";
        }

        static string? NextIncrementalSeat(Flight f, string original)
        {
            var cols = AppConsts.SeatColumnsDefault.ToCharArray();
            var row = int.Parse(original[..^1]);
            var col = original[^1];
            var start = Array.IndexOf(cols, col);

            for (int r = row; r <= AppConsts.SeatRowsDefault; r++)
            {
                int cStart = r == row ? start + 1 : 0;
                for (int i = cStart; i < cols.Length; i++)
                {
                    var cand = $"{r}{cols[i]}";
                    if (!f.Bookings.ContainsKey(cand)) return cand;
                }
            }
            for (int r = 1; r <= AppConsts.SeatRowsDefault; r++)
            {
                for (int i = 0; i <= start && i < cols.Length; i++)
                {
                    var cand = $"{r}{cols[i]}";
                    if (!f.Bookings.ContainsKey(cand)) return cand;
                }
            }
            return null;
        }

        (string seat, string? displaced) BookCore(BaseUser user, Flight f, string? wantSeat)
        {
            if (string.IsNullOrWhiteSpace(wantSeat))
            {
                var free = AllSeats().FirstOrDefault(s => !f.Bookings.ContainsKey(s)) ?? throw new InvalidOperationException("Flight is full");
                f.Bookings[free] = user.Email; return (free, null);
            }
            var seat = wantSeat!.Trim().ToUpperInvariant();
            if (!Validators.ValidSeat(seat)) throw new InvalidOperationException("Invalid Seat");
            if (!f.Bookings.TryGetValue(seat, out var who))
            { f.Bookings[seat] = user.Email; return (seat, null); }
            if (user is FrequentFlyer)
            {
                f.Bookings[seat] = user.Email;
                var next = NextIncrementalSeat(f, seat);
                if (next is null) throw new InvalidOperationException("Cannot reassign displaced traveller; flight full");
                f.Bookings[next] = who;
                return (seat, who);
            }
            throw new InvalidOperationException("Seat already taken. Choose another seat or use auto-assign.");
        }

        Ticket MakeTicket(BaseUser user, Flight f, string seat)
        {
            int pts = user is FrequentFlyer ? PointsFor(f) : 0;
            if (user is FrequentFlyer ff) ff.Points += pts;
            var t = new Ticket(user.Email, f.FlightCode, seat, f.Direction, f.OtherCity, f.TimeEffective(), pts);
            _tickets.Add(t);
            return t;
        }

        public Ticket BookArrival(BaseUser user, string flightCode, string? seat = null)
        { var f = _flights.Get(flightCode, Direction.ARRIVAL) ?? throw new InvalidOperationException("Arrival flight not found"); CheckUserBooking(user, f); var (s, _) = BookCore(user, f, seat); return MakeTicket(user, f, s); }

        public Ticket BookDeparture(BaseUser user, string flightCode, string? seat = null)
        { var f = _flights.Get(flightCode, Direction.DEPARTURE) ?? throw new InvalidOperationException("Departure flight not found"); CheckUserBooking(user, f); var (s, _) = BookCore(user, f, seat); return MakeTicket(user, f, s); }

        public IEnumerable<Ticket> UserTickets(string email) => _tickets.ForUser(email);

        public void DelayArrival(FlightManager m, string flightCode, int minutes)
        {
            _ = m ?? throw new ArgumentNullException(nameof(m));
            var f = _flights.Get(flightCode, Direction.ARRIVAL) ?? throw new InvalidOperationException("Arrival flight not found");
            f.Status = FlightStatus.DELAYED;
            f.DelayMinutes += minutes;
            foreach (var d in _flights.FindByPlane(f.PlaneId, Direction.DEPARTURE))
            {
                d.DelayMinutes += minutes;
                if (d.Status == FlightStatus.SCHEDULED) d.Status = FlightStatus.DELAYED;
            }
        }
    }

}
