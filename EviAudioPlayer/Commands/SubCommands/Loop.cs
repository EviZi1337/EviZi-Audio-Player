using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Loop : ICommand, IUsageProvider
{
    public string Command => "loop";
    public string[] Aliases => [];
    public string Description => "Toggle loop playback for a bot.";
    public string[] Usage => ["Bot ID", "true/false"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio loop {Bot ID} {true/false}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!bool.TryParse(arguments.At(1), out bool loop))
        {
            response = "Second argument must be true or false.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.Loop = loop;
        response = $"Bot {id}: loop = {loop}.";
        return true;
    }
}
