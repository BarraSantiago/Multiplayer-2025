using System;
using MultiplayerLib.Utils;

public class UnityConsoleMessages : ConsoleMessages
{
    public static void Initialize()
    {
        
        LogAction = (message) => {
            Console.WriteLine($"[ConsoleMessages] {message}");
            UnityEngine.Debug.Log($"[ConsoleMessages] {message}");
        };
    }
}