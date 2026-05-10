using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Pause : ICommand, IUsageProvider
{
    public string Command => "pause";
    public string[] Aliases => ["resume", "togglepause"];
    public string Description => "Toggle pause/resume for a bot.";
    public string[] Usage => ["Bot ID"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "Usage: audio pause {Bot ID}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.IsPaused = !bot.IsPaused;
        response = bot.IsPaused ? $"Bot {id}: paused." : $"Bot {id}: resumed.";
        return true;
    }
}
