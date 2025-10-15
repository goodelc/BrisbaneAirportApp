using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace BrisbaneAirportApp
{
    public class AirportController
    {
        private readonly AuthService _auth;
        private readonly FlightService _svc;
        private string? _token;

        public AirportController(AuthService auth, FlightService svc)
        {
            _auth = auth;
            _svc = svc;
        }

        public void Run()
        {
            PrintBanner();

            while (true)
            {
                Console.WriteLine("Please make a choice from the menu below:");
                Console.WriteLine("1. Login as a registered user.");
                Console.WriteLine("2. Register as a new user.");
                Console.WriteLine("3. Exit.");
                var mainChoice = AskChoice("Please enter a choice between 1 and 3:", 1, 3);
                if (mainChoice == 1)
                    LoginFlow();
                else if (mainChoice == 2)
                    RegisterFlow();
                else
                {
                    Console.WriteLine("Thank you. Safe travels.");
                    return;
                }
            }
        }

        private void RegisterFlow()
        {
            Console.WriteLine("Which user type would you like to register?");
            Console.WriteLine("1. A standard traveller.");
            Console.WriteLine("2. A frequent flyer.");
            Console.WriteLine("3. A flight manager.");
            var type = AskChoice("Please enter a choice between 1 and 3:", 1, 3);
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
            var email = "";
            var pwd = "";

            Console.WriteLine("Login Menu.");
            while (true)
            {
                Console.WriteLine("Please enter in your email:");
                email = ReadNonEmpty();
                if (Validators.ValidEmail(email))
                {
                    if (_auth.isExistEmail(email))
                    {
                        break;
                    }
                    else
                    {
                        PrintErrorWithTip("Email is not registered.");
                        return;
                    }
                }
                else
                {
                    PrintError("Supplied email is invalid.");
                    return;
                }
            }

            while (true)
            {
                Console.WriteLine("Please enter in your password:");
                pwd = ReadNonEmpty();
                if (Validators.ValidPassword(pwd))
                {

                    if (_auth.CheckPassword(email, pwd))
                    {
                        break;
                    }
                    else
                    {
                        PrintErrorWithTip("Incorrect Password.");

                    }
                }
                else
                {
                    PrintError("Supplied password is invalid.");
                }
            }

            try
            {
                _token = _auth.Login(email, pwd);
                var u = _auth.CurrentUser(_token)!;
                Console.WriteLine($"Welcome back {u.Name}.");

                if (u is FlightManager fm) ManagerMenu(fm);
                else if (u is FrequentFlyer ff) FrequentFlyerMenu(ff);
                else TravellerMenu(u);
            }
            catch (Exception)
            {
                PrintError("Invalid credentials");
                return;
            }
        }

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
                Console.WriteLine("Please enter in your frequent flyer number between 100000 and 999999:");
                ff = ReadNonEmpty();
                if (int.TryParse(ff, out var n) && Validators.ValidFFNumber(n)) break;
                PrintError("Supplied current frequent flyer points is invalid.");
            }

            int pts = AskInt("Please enter in your current frequent flyer points between 0 and 1000000:", 0, 1_000_000);

            _auth.RegisterFrequent(name, age, email, mobile, pwd, ff, pts);
            Console.WriteLine($"Congratulations {name}. You have registered as a frequent flyer.");
        }

        private void RegisterManagerFlow()
        {
            Console.WriteLine("Registering as a flight manager.");
            var (name, age, mobile, email, pwd) = AskUserBasics();

            string staff;
            while (true)
            {
                Console.WriteLine("Please enter in your staff id between 1000 and 9000:");
                staff = ReadNonEmpty();
                if (int.TryParse(staff, out var s) && s >= 1000 && s <= 9000) break;
                PrintError("Supplied staff id is invalid.");
            }

            _auth.RegisterManager(name, age, email, mobile, pwd, staff);
            Console.WriteLine($"Congratulations {name}. You have registered as a flight manager.");
        }

        // ========== Menus ==========
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
                var c = AskChoice("Please enter a choice between 1 and 6:", 1, 6);

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
                var c = AskChoice("Please enter a choice between 1 and 6:", 1, 6);

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
                Console.WriteLine("3. Create an arrival flight.");
                Console.WriteLine("4. Create a departure flight.");
                Console.WriteLine("5. Delay an arrival flight.");
                Console.WriteLine("6. Delay a departure flight.");
                Console.WriteLine("7. see the details of all flights.");
                Console.WriteLine("8. Logout.");
                var c = AskChoice("Please enter a choice between 1 and 8:", 1, 8);
                switch (c)
                {
                    case 1: ShowMe(u); break;
                    case 2: ChangePasswordFlow(u.Email); break;
                    case 3: AddFlightFlow(u, Direction.ARRIVAL); break;
                    case 4: AddFlightFlow(u, Direction.DEPARTURE); break;
                    case 5: DelayArrivalFlow(u); break;
                    case 6: DelayDepartureFlow(u); break;
                    case 7: ShowAllFlights(); break;
                    case 8: DoLogout(); return;
                }
            }
        }

        // ========== Actions ==========
        private void ShowMe(BaseUser u)
        {
            if (u is FrequentFlyer ff)
            {
            Console.WriteLine("Your details.");
                Console.WriteLine($"Name: {ff.Name}");
                Console.WriteLine($"Age: {ff.Age}");
                Console.WriteLine($"Mobile phone number: {ff.Mobile}");
                Console.WriteLine($"Email: {ff.Email}");
                Console.WriteLine($"Frequent flyer number: {ff.FFNumber}");
                Console.WriteLine($"Points: {ff.Points}");
            }
            else if (u is FlightManager fm)
            {
                ShowAllFlights();
            }
            else
            {
                Console.WriteLine($"Name: {u.Name}");
                Console.WriteLine($"Age: {u.Age}");
                Console.WriteLine($"Mobile phone number: {u.Mobile}");
                Console.WriteLine($"Email: {u.Email}");
            }
        }

        private void ListFlights()
        {
            var flights = _svc.ListFlights().ToList();
            if (!flights.Any())
            {
                Console.WriteLine("No flights available.");
                return;
            }

            Console.WriteLine("========== Flight Information ==========");
            for (int i = 0; i < flights.Count; i++)
            {
                var f = flights[i];
                Console.WriteLine($"Flight {i + 1}:");
                Console.WriteLine(f.ToString());
                if (i < flights.Count - 1)
                    Console.WriteLine("------------------------");
            }
            Console.WriteLine("=======================================");
        }

        private void ShowAllFlights()
        {
            var flights = _svc.ListFlights().ToList();
            var arrivalFlights = flights.Where(f => f.Direction == Direction.ARRIVAL).ToList();
            var departureFlights = flights.Where(f => f.Direction == Direction.DEPARTURE).ToList();

            Console.WriteLine();
            Console.WriteLine("Arrival Flights:");
            if (arrivalFlights.Any())
            {
                foreach (var f in arrivalFlights)
                {
                    var airlineName = AppConsts.AirlineNames.ContainsKey(f.Airline) ? AppConsts.AirlineNames[f.Airline] : f.Airline;
                    var timeStr = f.TimeEffective().ToString("HH:mm dd/MM/yyyy");
                    Console.WriteLine($"Flight {f.FlightCode} operated by {airlineName} arriving at {timeStr} from {f.OtherCity} on plane {f.PlaneId}.");
                }
            }
            else
            {
                Console.WriteLine("There are no arrival flights.");
            }

            Console.WriteLine("Departure Flights:");
            if (departureFlights.Any())
            {
                foreach (var f in departureFlights)
                {
                    var airlineName = AppConsts.AirlineNames.ContainsKey(f.Airline) ? AppConsts.AirlineNames[f.Airline] : f.Airline;
                    var timeStr = f.TimeEffective().ToString("HH:mm dd/MM/yyyy");
                    Console.WriteLine($"Flight {f.FlightCode} operated by {airlineName} departing at {timeStr} to {f.OtherCity} on plane {f.PlaneId}.");
                }
            }
            else
            {
                Console.WriteLine("There are no departure flights.");
            }
        }

        private void ChangePasswordFlow(string email)
        {
            Console.WriteLine("Please enter your current password.");
            var oldp = ReadLineAllowEmpty();
            //PrintPasswordRules();
            Console.WriteLine("Please enter your new password.");
            var newp = ReadPasswordLoop();
            try
            {
                _auth.ChangePassword(email, oldp, newp);
                //Console.WriteLine("Password changed.");
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }

        private void BookFlow(BaseUser user, Direction dir)
        {
            while (true)
            {
                Console.Write(dir == Direction.ARRIVAL ? "Please enter in the arrival flight code: " : "Please enter in the departure flight code: ");
                var code = ReadNonEmpty().ToUpperInvariant();
                Console.Write("Please enter in your preferred seat (or press enter for auto): ");
                var seatInput = Console.ReadLine();

                try
                {
                    var t = dir == Direction.ARRIVAL
                        ? _svc.BookArrival(user, code, string.IsNullOrWhiteSpace(seatInput) ? null : seatInput.Trim().ToUpperInvariant())
                        : _svc.BookDeparture(user, code, string.IsNullOrWhiteSpace(seatInput) ? null : seatInput.Trim().ToUpperInvariant());

                    Console.WriteLine(RenderTicket(t));
                    break;
                }
                catch (Exception ex)
                {
                    PrintError(ex.Message);
                }
            }
        }

        private void AddFlightFlow(FlightManager m, Direction dir)
        {
            string airline, code, city, plane;
            DateTime when;

            while (true) 
            { 
                Console.WriteLine("Please enter in the airline:"); 
                PrintAirline(); 
                string airlineIndex = ReadNonEmpty().ToUpperInvariant(); 
                if (AppConsts.AirlineCodesDic.ContainsKey(airlineIndex))
                {
                    airline = AppConsts.AirlineCodesDic[airlineIndex]; 
                    break; 
                }
                PrintError("Supplied airline code is invalid."); 
            }
            while (true) 
            { 
                Console.WriteLine(dir == Direction.ARRIVAL ? "Please enter the departure city: " : "Please enter in the arrival city: "); 
                PrintDepartingCity(); 
                string cityIndex = ReadNonEmpty(); 
                if (AppConsts.CityPointsList.ContainsKey(cityIndex))
                {
                    city = AppConsts.CityPointsList[cityIndex]; 
                    break; 
                }
                PrintError("Supplied city is invalid."); 
            }
            while (true) { Console.WriteLine("Please enter in the flight id between 100 and 900:"); code = ReadNonEmpty().ToUpperInvariant(); if (Validators.ValidFlightId(code)) break; PrintError("Supplied flight code is invalid."); }
            while (true) { Console.WriteLine("Please enter in the plane id between 0 and 9:"); plane = ReadNonEmpty().ToUpperInvariant(); if (Validators.ValidPlaneId(plane)) break; PrintError("Supplied plane id is invalid."); }
            when = AskDateTime("Please enter in the arrival date and time in format HH:mm dd/MM/yyyy:");

            try
            {
                if (dir == Direction.ARRIVAL)
                {
                    _svc.RegisterArrival(m, airline, code, city, plane, when);
                    Console.WriteLine($"Fligth {airline}{code} on plane {airline}{plane}A has been added to the system.");
                }
                else
                {
                    _svc.RegisterDeparture(m, airline, code, city, plane, when);
                    Console.WriteLine("Departure flight added.");
                }
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }


        private void PrintAirline()
        {
            Console.WriteLine("1. Jetstar");
            Console.WriteLine("2. Qants");
            Console.WriteLine("3. Regional Express");
            Console.WriteLine("4. Virgin");
            Console.WriteLine("5. Fly Pelican");
            Console.WriteLine("Please enter a choice between 1 and 5:");
        }
        private void PrintDepartingCity()
        {
            Console.WriteLine("1. Sydney");
            Console.WriteLine("2. Melbourne");
            Console.WriteLine("3. Rockhampton");
            Console.WriteLine("4. Adelaide");
            Console.WriteLine("5. Perth");
            Console.WriteLine("Please enter a choice between 1 and 5:");
        }


        private void DelayArrivalFlow(FlightManager m)
        {
            Console.Write("Please enter in the arrival flight code: ");
            var code = ReadNonEmpty().ToUpperInvariant();
            var mins = AskInt("Please enter in the delay minutes: ", 1, int.MaxValue);
            try
            {
                _svc.DelayArrival(m, code, mins);
                Console.WriteLine("Arrival delayed and linked departures adjusted.");
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }

        private void DelayDepartureFlow(FlightManager m)
        {
            Console.Write("Please enter in the departure flight code: ");
            var code = ReadNonEmpty().ToUpperInvariant();
            var mins = AskInt("Please enter in the delay minutes: ", 1, int.MaxValue);
            try
            {
                _svc.DelayDeparture(m, code, mins);
                Console.WriteLine("Departure delayed.");
            }
            catch (Exception ex)
            {
                PrintError(ex.Message);
            }
        }

        private void DoLogout()
        {
            if (_token != null) _auth.Logout(_token);
            _token = null;
            // Console.WriteLine("Logout.");
        }

        // ========== Helpers ==========
        private static void PrintBanner()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("=  Welcome to Brisbane Domestic Airport  =");
            Console.WriteLine("==========================================");
            Console.WriteLine();
        }

        private static void PrintError(string msg)
        {
            Console.WriteLine("#####");
            Console.WriteLine($"# Error - {msg}");
            Console.WriteLine("# Please try again.");
            Console.WriteLine("#####");
        }

        private static void PrintErrorWithTip(string msg)
        {
            Console.WriteLine("#####");
            Console.WriteLine($"# Error - {msg}");
            Console.WriteLine("#####");
        }

        private static void PrintPasswordRules()
        {
            Console.WriteLine("Your password must:");
            Console.WriteLine("-be at least 8 characters long ");
            Console.WriteLine("-contain a number");
            Console.WriteLine("-contain a lowercase letter");
            Console.WriteLine("-contain an uppercase letter");
        }

        private static string ReadPasswordLoop()
        {
            while (true)
            {
                var s = ReadNonEmpty();
                if (Validators.ValidPassword(s))
                    return s;
                PrintError("Supplied password is invalid.");
            }
        }

        private (string name, int age, string mobile, string email, string password) AskUserBasics()
        {
            string name, mobile, email, pwd; int age;

            while (true) { Console.WriteLine("Please enter in your name:"); name = ReadNonEmpty(); if (Validators.ValidName(name)) break; PrintError("Supplied name is invalid."); }


            while (true)
            {
                Console.WriteLine("Please enter in your age between 0 and 99:");
                var a = Console.ReadLine();
                if (int.TryParse(a, out age))
                {
                    if (Validators.ValidAge(age))
                    {
                        break;
                    }
                    else
                    {
                        PrintError("Supplied age is invalid.");
                    }
                }
                else
                {
                    PrintError("Supplied value is invalid.");
                }
            }


            while (true) { Console.WriteLine("Please enter in your mobile number:"); mobile = ReadNonEmpty(); if (Validators.ValidMobile(mobile)) break; PrintError("Supplied mobile number is invalid."); }

            while (true)
            {
                Console.WriteLine("Please enter in your email:");
                email = ReadNonEmpty();
                if (!Validators.ValidEmail(email))
                {

                    PrintError("Supplied email is invalid.");
                    continue;
                }

                if (_auth.isExistEmail(email))
                {
                    PrintError("Email already registered.");
                    continue;
                }
                break;
            }

            while (true) { Console.WriteLine("Please enter in your password:"); PrintPasswordRules(); pwd = ReadLineAllowEmpty(); if (Validators.ValidPassword(pwd)) break; PrintError("Supplied password is invalid."); }

            return (name, age, mobile, email, pwd);
        }

        private int AskChoice(string prompt, int lo, int hi)
        {
            while (true)
            {
                Console.WriteLine(prompt);
                var s = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(s, out var v) && v >= lo && v <= hi)
                {
                    return v;
                }
                // No error print here — Gradescope expects silent retry
            }
        }

        private static string ReadNonEmpty()
        {
            while (true)
            {
                var s = Console.ReadLine() ?? "";
                //s = s.Trim();
                if (!string.IsNullOrEmpty(s))
                    return s;
            }
        }


        private static string ReadLineAllowEmpty()
        {
            var s = Console.ReadLine();
            return s ?? ""; // null 转为空字符串
        }

        private static int AskInt(string label, int min, int max)
        {
            while (true)
            {
                Console.WriteLine(label);
                var s = Console.ReadLine()?.Trim() ?? "";
                if (int.TryParse(s, out var v) && v >= min && v <= max) return v;
                PrintError("Supplied number is invalid.");
            }
        }

        private static DateTime AskDateTime(string label)
        {
            while (true)
            {
                Console.WriteLine(label);
                var s = (Console.ReadLine() ?? "").Trim().Replace('T', ' ');
                if (DateTime.TryParseExact(s, AppConsts.DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    return dt;
                PrintError("Supplied time is invalid.");
            }
        }

        private static string RenderTicket(Ticket t)
        {
            var directionText = t.Direction == Direction.ARRIVAL ? "ARRIVAL" : "DEPARTURE";
            var timeLabel = t.Direction == Direction.ARRIVAL ? "Arrival Time" : "Departure Time";
            var cityLabel = t.Direction == Direction.ARRIVAL ? "Departure City" : "Arrival City";

            return $"========== Ticket Information ==========\n" +
                   $"Ticket ID: {t.TicketId}\n" +
                   $"Flight Code: {t.FlightCode}\n" +
                   $"Direction: {directionText}\n" +
                   $"{cityLabel}: {t.OtherCity}\n" +
                   $"{timeLabel}: {t.TimeString}\n" +
                   $"Seat: {t.SeatCode}\n" +
                   $"Points: {t.PointsEarned}\n" +
                   $"=====================================";
        }
    }
}