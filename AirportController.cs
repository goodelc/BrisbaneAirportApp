using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BrisbaneAirportApp
{
    // ---------------- Repositories ----------------
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

    public class TicketRepo
    {
        private readonly Dictionary<string, Ticket> _tickets = new();
        public void Add(Ticket t) => _tickets[t.TicketId] = t;
        public IEnumerable<Ticket> ForUser(string email) => _tickets.Values.Where(t => t.UserEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------- Services ----------------
    public class AuthService
    {
        private readonly UserRepo _users;
        private readonly Dictionary<string, string> _sessions = new();
        public AuthService(UserRepo users) { _users = users; }

        private static void CheckBase(string name, int age, string email, string mobile, string password)
        {
            if (!Validators.ValidName(name)) throw new InvalidOperationException("Invalid Name");
            if (!Validators.ValidAge(age)) throw new InvalidOperationException("Invalid Age");
            if (!Validators.ValidEmail(email)) throw new InvalidOperationException("Invalid Email");
            if (!Validators.ValidMobile(mobile)) throw new InvalidOperationException("Invalid Mobile");
            if (!Validators.ValidPassword(password)) throw new InvalidOperationException("Invalid Password");
        }

        public Traveller RegisterTraveller(string name, int age, string email, string mobile, string password)
        { CheckBase(name, age, email, mobile, password); var u = new Traveller(name, age, email, mobile, password); _users.Add(u); return u; }

        public FrequentFlyer RegisterFrequent(string name, int age, string email, string mobile, string password, string ffNumber, int points = 0)
        {
            CheckBase(name, age, email, mobile, password);
            if (!int.TryParse(ffNumber, out var ff) || !Validators.ValidFFNumber(ff)) throw new InvalidOperationException("Invalid Frequent Flyer Number");
            if (!Validators.ValidFFPoints(points)) throw new InvalidOperationException("Invalid Frequent Flyer Points");
            var u = new FrequentFlyer(name, age, email, mobile, password, ffNumber, points); _users.Add(u); return u;
        }

        public FlightManager RegisterManager(string name, int age, string email, string mobile, string password, string staffId)
        { CheckBase(name, age, email, mobile, password); if (string.IsNullOrWhiteSpace(staffId)) throw new InvalidOperationException("Invalid Staff ID"); var u = new FlightManager(name, age, email, mobile, password, staffId); _users.Add(u); return u; }

        public string Login(string email, string password)
        { var u = _users.Get(email); if (u is null || !u.VerifyPassword(password)) throw new InvalidOperationException("Invalid credentials"); var token = Guid.NewGuid().ToString("N"); _sessions[token] = email; return token; }

        public void Logout(string token) => _sessions.Remove(token);
        public BaseUser? CurrentUser(string token) => _sessions.TryGetValue(token, out var e) ? _users.Get(e) : null;

        public void ChangePassword(string email, string oldPwd, string newPwd)
        { var u = _users.Get(email) ?? throw new InvalidOperationException("No such user"); if (!u.VerifyPassword(oldPwd)) throw new InvalidOperationException("Invalid credentials"); if (!Validators.ValidPassword(newPwd)) throw new InvalidOperationException("Invalid Password"); u.SetPassword(newPwd); }
    }

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

    // ---------------- Controller (video-style UI) ----------------
    public class AirportController
    {
        private readonly AuthService _auth;
        private readonly FlightService _svc;
        private readonly AirportMenu _menu;
        private string? _token;

        public AirportController(AuthService auth, FlightService svc, AirportMenu menu)
        { _auth = auth; _svc = svc; _menu = menu; }

        public void Run()
        {
            PrintBanner();

            while (true)
            {
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. Login as a registered user.");
                Console.WriteLine("2. Register as a new user.");
                Console.WriteLine("3. Exit.");
                var mainChoice = AskChoice("Please enter a choice between 1 and 3: ", 1, 3);

                if (mainChoice == 1) LoginFlow();
                else if (mainChoice == 2) RegisterFlow();
                else { Console.WriteLine("Goodbye!"); return; }

                Console.WriteLine();
            }
        }

        // ---------- Main flows ----------
        private void RegisterFlow()
        {
            Console.WriteLine();
            Console.WriteLine("Which user type would you like to register?");
            Console.WriteLine("1. A standard traveller.");
            Console.WriteLine("2. A frequent flyer.");
            Console.WriteLine("3. A flight manager.");
            var type = AskChoice("Please enter a choice between 1 and 3: ", 1, 3);

            try
            {
                if (type == 1) RegisterTravellerFlow();
                else if (type == 2) RegisterFrequentFlow();
                else RegisterManagerFlow();
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }

        private void LoginFlow()
        {
            Console.WriteLine();
            Console.WriteLine("Login Menu.");
            Console.Write("Please enter in your email: ");
            var email = ReadNonEmpty();
            Console.Write("Please enter in your password: ");
            var pwd = ReadNonEmpty();

            try
            {
                _token = _auth.Login(email, pwd);
                var u = _auth.CurrentUser(_token)!;
                Console.WriteLine($"Welcome back {u.Name}.");

                if (u is FlightManager fm) ManagerMenu(fm);
                else if (u is FrequentFlyer ff) FrequentFlyerMenu(ff);
                else TravellerMenu(u);
            }
            catch (Exception ex) { PrintError(ex.Message); }
        }

        // ---------- Register detail flows (video wording) ----------
        private void RegisterTravellerFlow()
        {
            Console.WriteLine("Registering as a traveller.");
            var (name, age, mobile, email, pwd) = AskUserBasics();

            _auth.RegisterTraveller(name, age, email, mobile, pwd);
            Console.WriteLine($"Congratulations {name}. You have registered as a traveller.");
        }

        private void RegisterFrequentFlow()
        {
            Console.WriteLine("Registering as a frequent flyer.");
            var (name, age, mobile, email, pwd) = AskUserBasics();

            string ff;
            while (true)
            {
                Console.Write("Please enter in your frequent flyer number: ");
                ff = ReadNonEmpty();
                if (int.TryParse(ff, out var n) && Validators.ValidFFNumber(n)) break;
                PrintError("Supplied frequent flyer number is invalid.");
                Console.WriteLine("# Please try again.");
            }
            int pts = AskInt("Please enter in your frequent flyer points (0-1000000): ", 0, 1_000_000);

            _auth.RegisterFrequent(name, age, email, mobile, pwd, ff, pts);
            Console.WriteLine($"Congratulations {name}. You have registered as a frequent flyer.");
        }

        private void RegisterManagerFlow()
        {
            Console.WriteLine("Registering as a flight manager.");
            var (name, age, mobile, email, pwd) = AskUserBasicsStrict(); // 严格重复提示，匹配视频演示的错误循环

            Console.Write("Please enter in your staff id: ");
            var staff = ReadNonEmpty();

            _auth.RegisterManager(name, age, email, mobile, pwd, staff);
            Console.WriteLine($"Congratulations {name}. You have registered as a flight manager.");
        }

        // ---------- Role Menus (wording as video) ----------
        private void TravellerMenu(BaseUser u)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Traveller Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Book an arrival flight.");
                Console.WriteLine("4. Book a departure flight.");
                Console.WriteLine("5. See flight details.");
                Console.WriteLine("6. Logout.");
                var c = AskChoice("Please enter a choice between 1 and 6: ", 1, 6);

                switch (c)
                {
                    case 1: ShowMe(u); break;
                    case 2: ChangePasswordFlow(u.Email); break;
                    case 3: BookFlow(u, Direction.ARRIVAL); break;
                    case 4: BookFlow(u, Direction.DEPARTURE); break;
                    case 5: ListFlights(); break;
                    case 6: DoLogout(); return;
                }
            }
        }

        private void FrequentFlyerMenu(FrequentFlyer u)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Frequent Flyer Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Book an arrival flight.");
                Console.WriteLine("4. Book a departure flight.");
                Console.WriteLine("5. See flight details.");
                Console.WriteLine("6. Logout.");
                var c = AskChoice("Please enter a choice between 1 and 6: ", 1, 6);

                switch (c)
                {
                    case 1: ShowMe(u); break;
                    case 2: ChangePasswordFlow(u.Email); break;
                    case 3: BookFlow(u, Direction.ARRIVAL); break;
                    case 4: BookFlow(u, Direction.DEPARTURE); break;
                    case 5: ListFlights(); break;
                    case 6: DoLogout(); return;
                }
            }
        }

        private void ManagerMenu(FlightManager u)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Flight Manager Menu.");
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. See my details.");
                Console.WriteLine("2. Change password.");
                Console.WriteLine("3. Add an arrival flight.");
                Console.WriteLine("4. Add a departure flight.");
                Console.WriteLine("5. See flight details.");
                Console.WriteLine("6. Delay an arrival flight.");
                Console.WriteLine("7. Logout.");
                var c = AskChoice("Please enter a choice between 1 and 7: ", 1, 7);

                switch (c)
                {
                    case 1: ShowMe(u); break;
                    case 2: ChangePasswordFlow(u.Email); break;
                    case 3: AddFlightFlow(u, Direction.ARRIVAL); break;
                    case 4: AddFlightFlow(u, Direction.DEPARTURE); break;
                    case 5: ListFlights(); break;
                    case 6: DelayArrivalFlow(u); break;
                    case 7: DoLogout(); return;
                }
            }
        }

        // ---------- Actions ----------
        private void ShowMe(BaseUser u)
        {
            Console.WriteLine(u is FrequentFlyer ff ? ff.ToString() : u is FlightManager fm ? fm.ToString() : u.ToString());
        }

        private void ListFlights()
        {
            foreach (var f in _svc.ListFlights()) Console.WriteLine(f.ToString());
        }

        private void ChangePasswordFlow(string email)
        {
            Console.Write("Please enter in your old password: ");
            var oldp = ReadNonEmpty();
            PrintPasswordRules();
            Console.Write("Please enter in your new password: ");
            var newp = ReadPasswordLoop();
            try { _auth.ChangePassword(email, oldp, newp); Console.WriteLine("Password changed."); }
            catch (Exception ex) { PrintError(ex.Message); }
        }

        private void BookFlow(BaseUser user, Direction dir)
        {
            Console.Write(dir == Direction.ARRIVAL ? "Please enter in the arrival flight code: " : "Please enter in the departure flight code: ");
            var code = ReadNonEmpty().ToUpperInvariant();
            Console.Write("Please enter in your preferred seat (or press enter for auto): ");
            var seatInput = Console.ReadLine();

            try
            {
                var t = dir == Direction.ARRIVAL
                    ? _svc.BookArrival(user, code, string.IsNullOrWhiteSpace(seatInput) ? null : seatInput!.Trim().ToUpperInvariant())
                    : _svc.BookDeparture(user, code, string.IsNullOrWhiteSpace(seatInput) ? null : seatInput!.Trim().ToUpperInvariant());

                Console.WriteLine(RenderTicket(t));
            }
            catch (Exception ex) { PrintError(ex.Message); }
        }

        private void AddFlightFlow(FlightManager m, Direction dir)
        {
            string airline, code, city, plane;
            DateTime when;

            while (true) { Console.Write("Please enter in the airline code: "); airline = ReadNonEmpty().ToUpperInvariant(); if (Validators.ValidAirlineCode(airline)) break; PrintError("Supplied airline code is invalid."); Console.WriteLine("# Please try again."); }
            while (true) { Console.Write("Please enter in the flight code: "); code = ReadNonEmpty().ToUpperInvariant(); if (Validators.ValidFlightId(code)) break; PrintError("Supplied flight code is invalid."); Console.WriteLine("# Please try again."); }
            while (true) { Console.Write(dir == Direction.ARRIVAL ? "Please enter in the departure city: " : "Please enter in the arrival city: "); city = ReadNonEmpty(); if (Validators.ValidCity(city)) break; PrintError("Supplied city is invalid."); Console.WriteLine("# Please try again."); }
            while (true) { Console.Write("Please enter in the plane id: "); plane = ReadNonEmpty().ToUpperInvariant(); if (Validators.ValidPlaneId(plane)) break; PrintError("Supplied plane id is invalid."); Console.WriteLine("# Please try again."); }
            when = AskDateTime("Please enter in the scheduled time (yyyy-MM-dd HH:mm or yyyy-MM-ddTHH:mm): ");

            try
            {
                if (dir == Direction.ARRIVAL) { _svc.RegisterArrival(m, airline, code, city, plane, when); Console.WriteLine("Arrival flight added."); }
                else { _svc.RegisterDeparture(m, airline, code, city, plane, when); Console.WriteLine("Departure flight added."); }
            }
            catch (Exception ex) { PrintError(ex.Message); }
        }

        private void DelayArrivalFlow(FlightManager m)
        {
            Console.Write("Please enter in the arrival flight code: ");
            var code = ReadNonEmpty().ToUpperInvariant();
            var mins = AskInt("Please enter in the delay minutes: ", 1, int.MaxValue);
            try { _svc.DelayArrival(m, code, mins); Console.WriteLine("Arrival delayed and linked departures adjusted."); }
            catch (Exception ex) { PrintError(ex.Message); }
        }

        private void DoLogout()
        {
            if (_token != null) _auth.Logout(_token);
            _token = null;
            Console.WriteLine("Logout.");
        }

        // ---------- Helpers (video-style prompts) ----------
        private static void PrintBanner()
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("=   Welcome to Brisbane Domestic Airport          =");
            Console.WriteLine("===================================================");
            Console.WriteLine();
        }
        private static string ReadPasswordLoop()
        {
            // 一直循环，直到输入的密码符合规则
            while (true)
            {
                var s = ReadNonEmpty();  // 复用已有的输入函数
                if (Validators.ValidPassword(s))
                    return s;

                Console.WriteLine("#####");
                Console.WriteLine("# Error - Supplied password is invalid.");
                Console.WriteLine("#####");
                Console.WriteLine("# Please try again.");
                Console.Write("Please enter in your new password: ");
            }
        }
        

        private static void PrintPasswordRules()
        {
            Console.WriteLine("Your password must:");
            Console.WriteLine("- be at least 8 characters long");
            Console.WriteLine("- contain a number");
            Console.WriteLine("- contain a lowercase letter");
            Console.WriteLine("- contain an uppercase letter");
        }

        private static (string name, int age, string mobile, string email, string password) AskUserBasics()
        {
            string name, mobile, email, pwd; int age;

            Console.Write("Please enter in your name: ");
            while (true) { name = ReadNonEmpty(); if (Validators.ValidName(name)) break; PrintError("Supplied name is invalid."); Console.WriteLine("# Please try again."); Console.Write("Please enter in your name: "); }

            Console.Write("Please enter in your age between 0 and 99: ");
            while (true) { var a = Console.ReadLine(); if (int.TryParse(a, out age) && Validators.ValidAge(age)) break; PrintError("Supplied age is invalid."); Console.WriteLine("# Please try again."); Console.Write("Please enter in your age between 0 and 99: "); }

            Console.Write("Please enter in your mobile number: ");
            while (true) { mobile = ReadNonEmpty(); if (Validators.ValidMobile(mobile)) break; PrintError("Supplied mobile number is invalid."); Console.WriteLine("# Please try again."); Console.Write("Please enter in your mobile number: "); }

            Console.Write("Please enter in your email: ");
            while (true) { email = ReadNonEmpty(); if (Validators.ValidEmail(email)) break; PrintError("Supplied email is invalid."); Console.WriteLine("# Please try again."); Console.Write("Please enter in your email: "); }

            PrintPasswordRules();
            Console.Write("Please enter in your password: ");
            while (true) { pwd = ReadNonEmpty(); if (Validators.ValidPassword(pwd)) break; PrintError("Supplied password is invalid."); Console.WriteLine("# Please try again."); Console.Write("Please enter in your password: "); }

            return (name, age, mobile, email, pwd);
        }

        // 经理注册里，演示频繁重输手机号，因此用更“严格”的重复提示封装
        private static (string name, int age, string mobile, string email, string password) AskUserBasicsStrict()
        {
            var x = AskUserBasics(); // 当前规则与普通一致；保留这个方法便于以后单独调整经理输入节奏
            return x;
        }

        private static int AskChoice(string prompt, int lo, int hi)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(s, out var v) && v >= lo && v <= hi) return v;
            }
        }

        private static string ReadNonEmpty()
        {
            while (true)
            {
                var s = Console.ReadLine() ?? "";
                s = s.Trim();
                if (s.Length > 0) return s;
            }
        }

        private static int AskInt(string label, int min, int max)
        {
            while (true)
            {
                Console.Write(label);
                var s = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(s, out var v) && v >= min && v <= max) return v;
                PrintError("Supplied number is invalid.");
                Console.WriteLine("# Please try again.");
            }
        }

        private static DateTime AskDateTime(string label)
        {
            while (true)
            {
                Console.Write(label);
                var s = (Console.ReadLine() ?? "").Trim().Replace('T', ' ');
                if (DateTime.TryParseExact(s, AppConsts.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
                PrintError("Supplied time is invalid.");
                Console.WriteLine("# Please try again.");
            }
        }

        private static string RenderTicket(Ticket t)
        {
            return t.Direction == Direction.ARRIVAL
                ? $"[TICKET {t.TicketId}] ARRIVAL to Brisbane | Flight {t.FlightCode} from {t.OtherCity}\n  Arrive: {t.TimeString}\n  Seat: {t.SeatCode}\n  Points: {t.PointsEarned}"
                : $"[TICKET {t.TicketId}] DEPARTURE from Brisbane | Flight {t.FlightCode} to {t.OtherCity}\n  Depart: {t.TimeString}\n  Seat: {t.SeatCode}\n  Points: {t.PointsEarned}";
        }

        private static void PrintError(string msg)
        {
            Console.WriteLine("#####");
            Console.WriteLine($"# Error - {msg}");
            Console.WriteLine("#####");
        }
    }
}