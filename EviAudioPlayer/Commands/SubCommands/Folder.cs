using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;
using VoiceChat;

namespace EviAudio.Commands.SubCommands;

public class Folder : ICommand, IUsageProvider
{
    public string Command => "folder";
    public string[] Aliases => ["dir", "directory", "playdir"];
    public string Description => "Play all tracks from a folder (relative to EviAudio/tracks/ or absolute).";
    public string[] Usage => ["Bot ID", "Folder Path", "[shuffle: true/false]"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio folder {Bot ID} {Folder Path} [shuffle: true/false]";
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

        string folderPath = arguments.At(1);
        bool shuffle = arguments.Count >= 3 && bool.TryParse(arguments.At(2), out bool sh) && sh;

        bot.PlayFolder(folderPath, shuffle: shuffle);
        response = $"Bot {id}: playing folder '{folderPath}' (shuffle={shuffle}).";
        return true;
    }
}
