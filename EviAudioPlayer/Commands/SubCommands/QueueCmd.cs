using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;
using System.IO;
using System.Text;

namespace EviAudio.Commands.SubCommands;

public class QueueCmd : ICommand, IUsageProvider
{
    public string Command => "queue";
    public string[] Aliases => ["q", "playlist"];
    public string Description => "Show the track queue for a bot.";
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
            response = "Usage: audio queue {Bot ID}";
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

        var sb = new StringBuilder();
        sb.AppendLine($"\n<b>Bot {id} — Queue</b>");

        if (bot.IsPlaying)
            sb.AppendLine($"  <b>▶ NOW:</b> {Path.GetFileName(bot.CurrentTrack)}");

        var queue = bot.GetQueue();
        if (queue.Count == 0)
        {
            sb.AppendLine("  (queue is empty)");
        }
        else
        {
            int show = Math.Min(queue.Count, 20);
            for (int i = 0; i < show; i++)
                sb.AppendLine($"  {i + 1}. {Path.GetFileName(queue[i])}");

            if (queue.Count > show)
                sb.AppendLine($"  ... and {queue.Count - show} more.");
        }

        response = sb.ToString();
        return true;
    }
}
