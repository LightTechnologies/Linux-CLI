using System;
using System.Collections.Generic;
using System.Linq;

namespace LightVPN.CLI.Utils.Events
{
    public static class ErrorEvent
    {
        public static void Error(object sender, string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[/] Connection error: {message}");
            Environment.Exit(1);
        }
    }
}
