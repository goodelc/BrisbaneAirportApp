using System;

namespace BrisbaneAirportApp
{
    public class AirportMenu
    {
        private readonly CmdLineUI _ui;
        public AirportMenu(CmdLineUI ui) { _ui = ui; }

        public void ShowHelp()
        {
            _ui.WriteLine("Commands:");
            _ui.WriteLine("  register traveller <name> <age> <email> <mobile> <password>");
            _ui.WriteLine("  register frequent  <name> <age> <email> <mobile> <password> <ff_number> [points]");
            _ui.WriteLine("  register manager   <name> <age> <email> <mobile> <password> <staff_id>");
            _ui.WriteLine("  login <email> <password>");
            _ui.WriteLine("  logout");
            _ui.WriteLine("  me");
            _ui.WriteLine("  changepwd <old> <new>");
            _ui.WriteLine("  add arrival   <airline> <flight_code> <departure_city> <plane_id> <YYYY-MM-DDTHH:MM>");
            _ui.WriteLine("  add departure <airline> <flight_code> <arrival_city>   <plane_id> <YYYY-MM-DDTHH:MM>");
            _ui.WriteLine("  flights");
            _ui.WriteLine("  book arrival   <flight_code> [SEAT]");
            _ui.WriteLine("  book departure <flight_code> [SEAT]");
            _ui.WriteLine("  my tickets");
            _ui.WriteLine("  delay arrival <flight_code> <minutes>");
            _ui.WriteLine("  help");
            _ui.WriteLine("  quit | exit");
        }

        public string Prompt()
        {
            _ui.Write("> ");
            return _ui.ReadLine().Trim();
        }

        public void Print(string s) => _ui.WriteLine(s);
    }
}