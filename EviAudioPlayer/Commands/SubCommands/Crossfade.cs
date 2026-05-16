using CommandSystem;
using EviAudio.API;
using EviAudio.Other;
using Exiled.Permissions.Extensions;
using System;
using System.Linq;

namespace EviAudio.Commands.SubCommands;

public class Crossfade : ICommand, IUsageProvider
{
    public string Command => "crossfade";
    public string[] Aliases => ["xfade"];
    public string Description => "Crossfade a bot into another track.";
    public string[] Usage => ["Bot ID", "Path", "Volume (0-100)", "Loop true/false"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio crossfade <botId> <path> [volume] [loop]";
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

        float volume = bot.Volume;
        bool loop = bot.Loop;
        int pathEnd = arguments.Count;

        if (arguments.Count > 2 && bool.TryParse(arguments.At(arguments.Count - 1), out bool parsedLoop))
        {
            loop = parsedLoop;
            pathEnd--;
        }

        if (pathEnd > 2 && float.TryParse(arguments.At(pathEnd - 1), out float parsedVolume))
        {
            volume = parsedVolume;
            pathEnd--;
        }

        string path = string.Join(" ", arguments.Skip(1).Take(pathEnd - 1));
        string resolved = PcmDecoder.IsUrl(path) ? path : Extensions.PathCheck(path);
        bot.CrossfadeTo(resolved, volume, loop);
        response = $"Bot {id}: crossfading to '{resolved}'.";
        return true;
    }
}
