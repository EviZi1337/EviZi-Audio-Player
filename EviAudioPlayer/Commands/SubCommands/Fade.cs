using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Fade : ICommand, IUsageProvider
{
    public string Command => "fade";
    public string[] Aliases => ["fadeto", "fv"];
    public string Description => "Fade a bot's volume to a target level over a duration.";
    public string[] Usage => ["Bot ID", "Target Volume (0-100)", "Duration (seconds)"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = "Usage: audio fade {Bot ID} {Target Volume 0-100} {Duration seconds}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!float.TryParse(arguments.At(1), out float target) || target < 0 || target > 100)
        {
            response = "Target volume must be a number between 0 and 100.";
            return false;
        }

        if (!float.TryParse(arguments.At(2), out float duration) || duration <= 0)
        {
            response = "Duration must be a positive number (seconds).";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.FadeTo(target, duration);
        response = $"Bot {id}: fading volume to {target:F0}% over {duration:F1}s.";
        return true;
    }
}
