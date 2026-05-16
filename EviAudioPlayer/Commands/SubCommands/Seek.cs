using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Seek : ICommand, IUsageProvider
{
    public string Command => "seek";
    public string[] Aliases => ["pos"];
    public string Description => "Seek a bot track to a position in seconds.";
    public string[] Usage => ["Bot ID", "Seconds"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio seek <botId> <seconds>";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!double.TryParse(arguments.At(1), out double seconds) || seconds < 0)
        {
            response = "Seconds must be a positive number.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        if (!bot.SeekTo(TimeSpan.FromSeconds(seconds)))
        {
            response = "Seek failed. The track may not be loaded yet or FFmpeg could not reopen the stream.";
            return false;
        }

        response = $"Bot {id}: seeked to {TimeSpan.FromSeconds(seconds):m\\:ss}.";
        return true;
    }
}
