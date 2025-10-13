using System;

namespace BrisbaneAirportApp
{
    public class CmdLineUI
    {
        public void Write(string text) => Console.Write(text);
        public void WriteLine(string text = "") => Console.WriteLine(text);
        public string ReadLine() => Console.ReadLine() ?? string.Empty;
    }
}