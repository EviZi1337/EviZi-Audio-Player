using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Skip : ICommand, IUsageProvider
{
    public string Command => "skip";
    public string[] Aliases => ["next", "sk"];
    public string Description => "Skip the current track and play the next one in the queue.";
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
            response = "Usage: audio skip {Bot ID}";
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

        if (!bot.IsPlaying)
        {
            response = $"Bot {id} is not playing anything.";
            return false;
        }

        bot.Skip();
        response = bot.IsPlaying
            ? $"Bot {id}: skipped — now playing '{System.IO.Path.GetFileName(bot.CurrentTrack)}'."
            : $"Bot {id}: skipped — queue is empty.";
        return true;
    }
}
