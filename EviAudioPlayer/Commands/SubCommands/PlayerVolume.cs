using CommandSystem;
using EviAudio.API;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class PlayerVolume : ICommand, IUsageProvider
{
    public string Command => "playervolume";
    public string[] Aliases => ["pvol", "pv"];
    public string Description => "Set personal playback volume for one listener.";
    public string[] Usage => ["Bot ID", "Player", "Volume (0-100)"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 3)
        {
            response = "Usage: audio playervolume <botId> <player> <0-100>";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        Player player = Player.Get(arguments.At(1));
        if (player == null)
        {
            response = "Player not found.";
            return false;
        }

        if (!float.TryParse(arguments.At(2), out float volume) || volume < 0 || volume > 100)
        {
            response = "Volume must be 0-100.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.SetPlayerVolume(player.Id, volume * 0.01f);
        response = $"Bot {id}: personal volume for {player.Nickname} set to {volume:F0}%.";
        return true;
    }
}
