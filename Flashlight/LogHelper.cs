using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Flashlight;

public static class LogHelper
{
    public static void LogToConsole(string messageToLog)
    {
        Console.WriteLine($"[{Flashlight.Instance?.ModuleName}]  -> {messageToLog}");
    }

    public static void LogToConsole(ConsoleColor color, string messageToLog)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"[{Flashlight.Instance?.ModuleName}]  -> {messageToLog}");
        Console.ResetColor();
    }

    public static void LogToChat(CCSPlayerController? player, string messageToLog)
    {
        player?.PrintToChat($"[{ChatColors.Purple}{Flashlight.Instance?.ModuleName}{ChatColors.Default}]{ReplaceTags(messageToLog + "{DEFAULT}")}");
    }

    public static void LogToChatAll(string messageToLog)
    {
        Server.PrintToChatAll($"[{ChatColors.Purple}{Flashlight.Instance?.ModuleName}{ChatColors.Default}]{ReplaceTags(messageToLog + "{DEFAULT}")}");
    }

    public static string ReplaceTags(this string text)
    {
        if (text.StartsWith("{")) text = $" {text}";
        text = text.Replace("{DEFAULT}", $"{ChatColors.Default}");
        text = text.Replace("{WHITE}", $"{ChatColors.White}");
        text = text.Replace("{DARKRED}", $"{ChatColors.Darkred}");
        text = text.Replace("{GREEN}", $"{ChatColors.Green}");
        text = text.Replace("{LIGHTYELLOW}", $"{ChatColors.LightYellow}");
        text = text.Replace("{LIGHTBLUE}", $"{ChatColors.LightBlue}");
        text = text.Replace("{OLIVE}", $"{ChatColors.Olive}");
        text = text.Replace("{LIME}", $"{ChatColors.Lime}");
        text = text.Replace("{RED}", $"{ChatColors.Red}");
        text = text.Replace("{PURPLE}", $"{ChatColors.Purple}");
        text = text.Replace("{GREY}", $"{ChatColors.Grey}");
        text = text.Replace("{YELLOW}", $"{ChatColors.Yellow}");
        text = text.Replace("{GOLD}", $"{ChatColors.Gold}");
        text = text.Replace("{SILVER}", $"{ChatColors.Silver}");
        text = text.Replace("{BLUE}", $"{ChatColors.Blue}");
        text = text.Replace("{DARKBLUE}", $"{ChatColors.DarkBlue}");
        text = text.Replace("{BLUEGREY}", $"{ChatColors.BlueGrey}");
        text = text.Replace("{MAGENTA}", $"{ChatColors.Magenta}");
        text = text.Replace("{LIGHTRED}", $"{ChatColors.LightRed}");

        return text;
    }
}