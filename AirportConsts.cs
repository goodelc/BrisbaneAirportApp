using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BrisbaneAirportApp
{
    public static class AppConsts
    {
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm";
        public const string SeatColumnsDefault = "ABCD";
        public const int SeatRowsDefault = 10;

        public static readonly string[] AirlineCodes = { "JST", "QFA", "RXA", "VOZ", "FRE" };

        public static readonly Dictionary<string, string> AirlineNames = new()
        {
            ["JST"] = "Jetstar",
            ["QFA"] = "Qantas",
            ["RXA"] = "Regional Express",
            ["VOZ"] = "Virgin",
            ["FRE"] = "Fly Pelican",
        };

        public static readonly Dictionary<string, int> CityPoints = new()
        {
            ["Sydney"] = 1200,
            ["Melbourne"] = 1750,
            ["Rockhampton"] = 1400,
            ["Adelaide"] = 1950,
            ["Perth"] = 3375,
        };
    }

    public static class Validators
    {
        static readonly Regex NameRx = new(@"^[A-Za-z][A-Za-z '\-]*$");
        static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+$");
        static readonly Regex MobileRx = new(@"^0\d{9}$");
        static readonly Regex PwdDigit = new(@"\d");
        static readonly Regex PwdLower = new(@"[a-z]");
        static readonly Regex PwdUpper = new(@"[A-Z]");
        static readonly Regex FlightIdRx = new(@"^[A-Z]{3}\d{3}$");
        static readonly Regex PlaneIdRx = new(@"^[A-Z]{3}\d[AD]$");
        static readonly Regex SeatRx = new(@"^([1-9]|10)[A-D]$");

        public static bool ValidName(string s) => !string.IsNullOrEmpty(s) && NameRx.IsMatch(s);
        public static bool ValidAge(int a) => a >= 0 && a <= 99;
        public static bool ValidEmail(string s) => EmailRx.IsMatch(s);
        public static bool ValidMobile(string s) => MobileRx.IsMatch(s);
        public static bool ValidPassword(string s) =>
            s.Length >= 8 && PwdDigit.IsMatch(s) && PwdLower.IsMatch(s) && PwdUpper.IsMatch(s);
        public static bool ValidFFNumber(int n) => n >= 100000 && n <= 999999;
        public static bool ValidFFPoints(int n) => n >= 0 && n <= 1_000_000;
        public static bool ValidAirlineCode(string code) => Array.Exists(AppConsts.AirlineCodes, c => c == code);
        public static bool ValidCity(string city) => AppConsts.CityPoints.ContainsKey(city);
        public static bool ValidFlightId(string id) => FlightIdRx.IsMatch(id) && ValidAirlineCode(id[..3]);
        public static bool ValidPlaneId(string id) => PlaneIdRx.IsMatch(id) && ValidAirlineCode(id[..3]);
        public static bool ValidSeat(string seat) => SeatRx.IsMatch(seat);
    }
}
