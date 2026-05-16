using CommandSystem;
using EviAudio.API;
using EviAudio.Other;
using Exiled.Permissions.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EviAudio.Commands.SubCommands;

public class Play : ICommand, IUsageProvider
{
    public string Command => "play";
    public string[] Aliases => ["playback", "replay"];
    public string Description => "Play a file on a bot.";
    public string[] Usage => ["Bot ID", "Path", "[--start seconds]", "[--end seconds]"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio play {Bot ID} {Path} [--start seconds] [--end seconds]";
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

        var pathParts = new List<string>();
        TimeSpan? startAt = null;
        TimeSpan? endAt = null;

        for (int i = 1; i < arguments.Count; i++)
        {
            string arg = arguments.At(i);
            if ((arg.Equals("--start", StringComparison.OrdinalIgnoreCase) || arg.Equals("start", StringComparison.OrdinalIgnoreCase))
                && i + 1 < arguments.Count
                && double.TryParse(arguments.At(i + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double startSeconds))
            {
                startAt = TimeSpan.FromSeconds(Math.Max(0, startSeconds));
                i++;
                continue;
            }

            if ((arg.Equals("--end", StringComparison.OrdinalIgnoreCase) || arg.Equals("end", StringComparison.OrdinalIgnoreCase))
                && i + 1 < arguments.Count
                && double.TryParse(arguments.At(i + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out double endSeconds))
            {
                endAt = TimeSpan.FromSeconds(Math.Max(0, endSeconds));
                i++;
                continue;
            }

            pathParts.Add(arg);
        }

        string path = Extensions.PathCheck(string.Join(" ", pathParts));
        bot.PlayFile(path, startAt: startAt, endAt: endAt);
        response = $"Bot {id}: playing '{path}' on channel {bot.VoiceChatChannel}.";
        return true;
    }
}
