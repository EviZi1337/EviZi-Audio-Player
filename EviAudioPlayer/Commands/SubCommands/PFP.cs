using CommandSystem;
using EviAudio.API;
using EviAudio.Other;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using System;
using System.Linq;

namespace EviAudio.Commands.SubCommands;

public class PFP : ICommand, IUsageProvider
{
    public string Command => "playfromplayers";
    public string[] Aliases => ["pfp"];
    public string Description => "Play audio audible only to specific players.";
    public string[] Usage => ["Bot ID", "Player1.Player2.etc", "Path"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = "Usage: audio pfp {Bot ID} {Player1.Player2} {Path}";
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

        var players = arguments.At(1).Trim('.').Split('.')
            .Select(Player.Get)
            .Where(p => p != null)
            .ToList();

        if (players.Count == 0)
        {
            response = "No players found.";
            return false;
        }

        string path = Extensions.PathCheck(string.Join(" ", arguments.Skip(2)));
        bot.PlayFile(path, targetPlayerIds: players.Select(p => p.Id));
        response = $"Bot {id}: playing '{path}' for players {string.Join(", ", players.Select(p => p.Id))}.";
        return true;
    }
}
