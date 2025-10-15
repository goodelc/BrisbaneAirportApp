using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrisbaneAirportApp
{
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

        public bool isExistUser(string token) => _sessions.ContainsKey(token);


        public bool CheckPassword(string email, string password) => _users.Get(email)?.VerifyPassword(password) ?? false;

        public bool isExistEmail(string email) => _users.Get(email) != null;


        public void Logout(string token) => _sessions.Remove(token);
        public BaseUser? CurrentUser(string token) => _sessions.TryGetValue(token, out var e) ? _users.Get(e) : null;

        public void ChangePassword(string email, string oldPwd, string newPwd)
        { var u = _users.Get(email) ?? throw new InvalidOperationException("No such user");if (!Validators.ValidPassword(newPwd)) throw new InvalidOperationException("Invalid Password"); if (!u.VerifyPassword(oldPwd)) throw new InvalidOperationException("Entered password does not match existing password."); u.SetPassword(newPwd); }
    }
}
