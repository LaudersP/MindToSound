using System;
using System.Runtime.CompilerServices;

namespace CortexAccess
{
    public class Utils
    {
        public static Int64 GetEpochTimeNow()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            Int64 timeSinceEpoch = (Int64)t.TotalMilliseconds;
            return timeSinceEpoch;

        }
        public static string GenerateUuidProfileName(string prefix)
        {
            return prefix + "-" + GetEpochTimeNow();
        }

        // Print a colored message
        private void SendColoredMessage(string messageType, ConsoleColor typeColor, string messageContent)
        {
            Console.ForegroundColor = typeColor;
            Console.Write($"[{messageType}] ");
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(messageContent);

        }

        // Print a error message
        public void SendErrorMessage(string message)
        {
            SendColoredMessage("ERROR", ConsoleColor.Red, message);
        }

        // Print a success message
        public void SendSuccessMessage(string message)
        {
            SendColoredMessage("SUCCESS", ConsoleColor.Green, message);
        }

        // Print a warning message
        public void SendWarningMessage(string message)
        {
            SendColoredMessage("WARNING", ConsoleColor.DarkYellow, message);
        }
    }
}
