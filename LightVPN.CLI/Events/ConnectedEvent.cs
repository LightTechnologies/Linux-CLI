using System;
using System.Collections.Generic;
using System.Linq;

namespace LightVPN.CLI.Utils.Events
{
    public static class ConnectedEvent
    {
        public static void Connected(object sender)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[!] Connected to the server, run 'lightvpn disconnect' to disconnect from the server!");
            Environment.Exit(0);
        }
    }
}
