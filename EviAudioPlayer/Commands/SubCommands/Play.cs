using CommandSystem;
using EviAudio.API;
using EviAudio.Other;
using Exiled.Permissions.Extensions;
using System;
using System.Linq;

namespace EviAudio.Commands.SubCommands;

public class Play : ICommand, IUsageProvider
{
    public string Command => "play";
    public string[] Aliases => ["playback", "replay"];
    public string Description => "Play a file on a bot.";
    public string[] Usage => ["Bot ID", "Path"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio play {Bot ID} {Path}";
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

        string path = Extensions.PathCheck(string.Join(" ", arguments.Skip(1)));
        bot.PlayFile(path);
        response = $"Bot {id}: playing '{path}' on channel {bot.VoiceChatChannel}.";
        return true;
    }
}
