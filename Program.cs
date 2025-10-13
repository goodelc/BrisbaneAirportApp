using System;

namespace BrisbaneAirportApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var users = new UserRepo();
            var flights = new FlightRepo();
            var tickets = new TicketRepo();

            var auth = new AuthService(users);
            var svc = new FlightService(flights, tickets, users);

            var ui = new CmdLineUI();
            var menu = new AirportMenu(ui);

            var app = new AirportController(auth, svc, menu);
            app.Run();
        }
    }
}
