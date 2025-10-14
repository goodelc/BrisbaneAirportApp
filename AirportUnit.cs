using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BrisbaneAirportApp
{
    public abstract class BaseUser
    {
        public string Name { get; }
        public int Age { get; }
        public string Email { get; }
        public string Mobile { get; }
        private string PasswordHash { get; set; }

        protected BaseUser(string name, int age, string email, string mobile, string password)
        {
            Name = name; Age = age; Email = email; Mobile = mobile;
            PasswordHash = Sha256(password);
        }

        public bool VerifyPassword(string password) => PasswordHash == Sha256(password);
        public void SetPassword(string newPwd) => PasswordHash = Sha256(newPwd);
        private static string Sha256(string text)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }

        public abstract string Role { get; }
    }

    public class Traveller : BaseUser
    {
        public Traveller(string name, int age, string email, string mobile, string password)
            : base(name, age, email, mobile, password) { }
        public override string Role => "traveller";
    }

    public class FrequentFlyer : Traveller
    {
        public string FFNumber { get; }
        public int Points { get; set; }

        public FrequentFlyer(string name, int age, string email, string mobile, string password, string ffNumber, int points = 0)
            : base(name, age, email, mobile, password)
        {
            FFNumber = ffNumber;
            Points = points;
        }

        public override string Role => "frequent_flyer";
    }

    public class FlightManager : BaseUser
    {
        public string StaffId { get; }
        public FlightManager(string name, int age, string email, string mobile, string password, string staffId)
            : base(name, age, email, mobile, password)
        {
            StaffId = staffId;
        }

        public override string Role => "flight_manager";
    }

    public enum Direction { ARRIVAL, DEPARTURE }
    public enum FlightStatus { SCHEDULED, DELAYED }

    public class Seat
    {
        public int Row { get; }
        public char Col { get; }

        public Seat(int row, char col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString() => $"{Row}{Col}";
    }

    public class Flight
    {
        public string Airline { get; }
        public string FlightCode { get; }
        public string OtherCity { get; }
        public string PlaneId { get; }
        public DateTime ScheduledTime { get; }
        public Direction Direction { get; }
        public FlightStatus Status { get; set; } = FlightStatus.SCHEDULED;
        public int DelayMinutes { get; set; } = 0;

        public Dictionary<string, string> Bookings { get; } = new();

        public Flight(string airline, string code, string city, string planeId, DateTime time, Direction dir)
        {
            Airline = airline;
            FlightCode = code;
            OtherCity = city;
            PlaneId = planeId;
            ScheduledTime = time;
            Direction = dir;
        }

        public DateTime TimeEffective() => ScheduledTime.AddMinutes(DelayMinutes);

        public override string ToString()
        {
            var statusText = Status == FlightStatus.DELAYED ? "DELAYED" : "SCHEDULED";
            var timeStr = TimeEffective().ToString("yyyy-MM-dd HH:mm");
            var delayStr = DelayMinutes > 0 ? $" (delayed {DelayMinutes} minutes)" : "";
            
            return $"Flight Code: {FlightCode}\n" +
                   $"Airline: {Airline}\n" +
                   $"Aircraft: {PlaneId}\n" +
                   $"{(Direction == Direction.ARRIVAL ? "Departure City" : "Arrival City")}: {OtherCity}\n" +
                   $"{(Direction == Direction.ARRIVAL ? "Arrival Time" : "Departure Time")}: {timeStr}{delayStr}\n" +
                   $"Status: {statusText}\n" +
                   $"Direction: {(Direction == Direction.ARRIVAL ? "ARRIVAL" : "DEPARTURE")}";
        }
    }

    public class Ticket
    {
        public string TicketId { get; }
        public string UserEmail { get; }
        public string FlightCode { get; }
        public string SeatCode { get; }
        public Direction Direction { get; }
        public string OtherCity { get; }
        public string TimeString { get; }
        public int PointsEarned { get; }

        public Ticket(string userEmail, string flightCode, string seatCode, Direction direction, string otherCity, DateTime time, int points)
        {
            TicketId = $"T-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            UserEmail = userEmail;
            FlightCode = flightCode;
            SeatCode = seatCode;
            Direction = direction;
            OtherCity = otherCity;
            TimeString = time.ToString(AppConsts.DateTimeFormat);
            PointsEarned = points;
        }
    }

    public class Airport
    {
        public string Name { get; }
        public string Country { get; }
        public string City { get; }
        public double Longitude { get; }
        public double Latitude { get; }

        public Airport(string name, string country, string city, double longitude, double latitude)
        {
            Name = name;
            Country = country;
            City = city;
            Longitude = longitude;
            Latitude = latitude;
        }

        public override string ToString()
        {
            return $"Airport Name: {Name}\n" +
                   $"Country: {Country}\n" +
                   $"City: {City}\n" +
                   $"Coordinates: {Longitude:F3} {Latitude:F3}";
        }
    }

    public class Airline
    {
        public string Name { get; }
        public string Airport1 { get; }
        public string Airport2 { get; }
        public double Length { get; }
        public List<Point> Route { get; }

        public Airline(string name, string airport1, string airport2, double length, List<Point> route)
        {
            Name = name;
            Airport1 = airport1;
            Airport2 = airport2;
            Length = length;
            Route = route;
        }

        public override string ToString()
        {
            var routeStr = string.Join(" -> ", Route.Select(p => $"({p.Longitude:F3}, {p.Latitude:F3})"));
            return $"Route Name: {Name}\n" +
                   $"Departure Airport: {Airport1}\n" +
                   $"Arrival Airport: {Airport2}\n" +
                   $"Route Length: {Length:F2} km\n" +
                   $"Route Path: {routeStr}";
        }
    }

    public class Point
    {
        public double Longitude { get; }
        public double Latitude { get; }

        public Point(double longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
        }
    }
}
