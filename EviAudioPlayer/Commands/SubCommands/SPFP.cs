using CommandSystem;
using EviAudio.API;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using System;
using System.Linq;

namespace EviAudio.Commands.SubCommands;

public class SPFP : ICommand, IUsageProvider
{
    public string Command => "stopplayfromplayers";
    public string[] Aliases => ["spfp", "stoppfp"];
    public string Description => "Remove players from a bot's broadcast list.";
    public string[] Usage => ["Bot ID", "Player1.Player2.etc"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio spfp {Bot ID} {Player1.Player2}";
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

        foreach (var player in players)
            bot.BroadcastTo.Remove(player.Id);

        response = $"Bot {id}: removed players {string.Join(", ", players.Select(p => p.Id))} from broadcast list.";
        return true;
    }
}
