using CommandSystem;
using EviAudio.API;
using EviAudio.API.Container;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Add : ICommand, IUsageProvider
{
    public string Command => "add";
    public string[] Aliases => ["create", "cr", "fake", "bot"];
    public string Description => "Spawn an EviAudio bot.";
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
            response = "Usage: audio add {Bot ID}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (id.IsAudioPlayer())
        {
            response = $"Bot with ID {id} already exists.";
            return false;
        }

        AudioPlayerBot.SpawnDummy(id: id);
        response = $"Spawned bot with ID {id}.";
        return true;
    }
}
