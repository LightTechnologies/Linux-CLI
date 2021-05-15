using LightVPN.Auth.Models;
using LightVPN.CLI.Utils.Exceptions;
using LightVPN.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LightVPN.CLI.Utils
{
    public static class ConfigResolver
    {
        public static string GetConfigPath(string serverName)
        {
            var files = Directory.GetFiles(Globals.LinuxConfigPath);

            if (!files.Any(x => x.Contains(serverName)))
            {
                throw new ConfigNotFoundException("Failed to resolve configuration, the server cache may be out of date");
            }

            var ovpnFn = files.First(x => x.Contains(serverName));

            if (string.IsNullOrWhiteSpace(ovpnFn))
            {
                throw new ConfigNotFoundException("Failed to resolve configuration, the server cache may be out of date (whitespace)");
            }

            return ovpnFn;
        }
    }
}