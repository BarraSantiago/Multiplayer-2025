using System;
using MultiplayerLib.Utils;

public class UnityConsoleMessages
{
    public static void Initialize()
    {
        ConsoleMessages.LogAction = (message) => {
            Console.WriteLine($"[ConsoleMessages] {message}");
            UnityEngine.Debug.Log($"[ConsoleMessages] {message}");
        };
    }
}