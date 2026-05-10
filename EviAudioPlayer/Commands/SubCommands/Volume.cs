using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Volume : ICommand, IUsageProvider
{
    public string Command => "volume";
    public string[] Aliases => ["vol", "v"];
    public string Description => "Set playback volume for a bot (0–100).";
    public string[] Usage => ["Bot ID", "Volume (0-100)"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio volume {Bot ID} {0-100}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!float.TryParse(arguments.At(1), out float volume) || volume < 0 || volume > 100)
        {
            response = "Volume must be a number between 0 and 100.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.Volume = volume;
        response = $"Bot {id}: volume set to {volume}.";
        return true;
    }
}
