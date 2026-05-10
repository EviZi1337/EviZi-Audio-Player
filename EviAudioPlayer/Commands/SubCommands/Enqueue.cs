using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Enqueue : ICommand, IUsageProvider
{
    public string Command => "enqueue";
    public string[] Aliases => [];
    public string Description => "Add a track to the queue at a given position.";
    public string[] Usage => ["Bot ID", "Path", "Position (-1 = end)"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio enqueue {Bot ID} {Path} [Position]";
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

        string path = arguments.At(1);
        int position = arguments.Count >= 3 && int.TryParse(arguments.At(2), out int pos) ? pos : -1;

        bot.Enqueue(path, position);
        response = $"Enqueued '{path}' for bot {id} at position {position}.";
        return true;
    }
}
