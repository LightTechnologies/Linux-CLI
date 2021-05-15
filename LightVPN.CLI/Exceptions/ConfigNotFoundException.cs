using System;

namespace LightVPN.CLI.Utils.Exceptions
{
    /// <summary>
    /// Thrown when the API is offline
    /// </summary>
    public class ConfigNotFoundException : Exception
    {
        public ConfigNotFoundException()
        {
        }

        public ConfigNotFoundException(string message)
            : base(message)
        {
        }

        public ConfigNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}