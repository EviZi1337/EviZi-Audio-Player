using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;
using System.Text;

namespace EviAudio.Commands.SubCommands;

public class Status : ICommand
{
    public string Command => "status";
    public string[] Aliases => ["info", "list", "bots"];
    public string Description => "Show status of all active audio bots.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        var bots = AudioController.GetAllAudioPlayers();

        if (bots.Count == 0)
        {
            response = "No audio bots are currently spawned.";
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"\n<b>EviAudio — {bots.Count} bot(s) active</b>\n");

        foreach (var bot in bots)
        {
            string state = bot.IsPlaying
                ? bot.IsPaused ? "⏸ PAUSED" : "▶ PLAYING"
                : "⏹ IDLE";

            sb.AppendLine($"  <b>Bot {bot.ID}</b>  \"{bot.Name}\"  [{state}]");
            sb.AppendLine($"    Channel: {bot.VoiceChatChannel}  Volume: {bot.Volume:F0}%  Loop: {bot.Loop}  Pitch: {bot.PitchShift:+0.#;-0.#;0} st");

            if (bot.IsPlaying)
                sb.AppendLine($"    Track: {System.IO.Path.GetFileName(bot.CurrentTrack)}");

            var queue = bot.GetQueue();
            if (queue.Count > 0)
                sb.AppendLine($"    Queue: {queue.Count} track(s) — next: {System.IO.Path.GetFileName(queue[0])}");

            if (bot.BroadcastTo?.Count > 0)
                sb.AppendLine($"    Targets: [{string.Join(", ", bot.BroadcastTo)}]");
        }

        response = sb.ToString();
        return true;
    }
}
