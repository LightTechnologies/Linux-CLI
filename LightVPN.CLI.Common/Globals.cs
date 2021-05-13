using System;
using System.IO;

namespace LightVPN.CLI.Common
{
    public static class Globals
    {
        // This is so it saves settings in the users home folder
        public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "LightVPN");

        public static readonly string AuthPath = Path.Combine(SettingsPath, "auth.bin");

        public static Guid SessionId = Guid.Empty;

        public static readonly string ConfigPath = Path.Combine(SettingsPath, "cache");

        public static readonly string OpenVpnDriversPath = Path.Combine(SettingsPath, "drivers");

        public static readonly string OpenVpnPath = Path.Combine(SettingsPath, "ovpn");
    }
}
