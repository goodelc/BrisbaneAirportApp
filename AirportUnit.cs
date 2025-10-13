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
            return $"{Direction,-9} {FlightCode,-8} {Airline,-10} plane={PlaneId,-6} other_city={OtherCity,-12} " +
                   $"time={TimeEffective().ToString(AppConsts.DateTimeFormat)} status={Status} delay={DelayMinutes}m";
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
}
