using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Stop : ICommand, IUsageProvider
{
    public string Command => "stop";
    public string[] Aliases => [];
    public string Description => "Stop audio playback on a bot.";
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
            response = "Usage: audio stop {Bot ID}";
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

        bot.StopAudio(clearQueue: true);
        response = $"Bot {id}: stopped.";
        return true;
    }
}
