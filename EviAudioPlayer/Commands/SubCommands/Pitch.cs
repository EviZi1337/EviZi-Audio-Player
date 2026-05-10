using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;

namespace EviAudio.Commands.SubCommands;

public class Pitch : ICommand, IUsageProvider
{
    public string Command => "pitch";
    public string[] Aliases => ["pt", "semitones"];
    public string Description => "Set the pitch shift in semitones for a bot (+12 = octave up, -12 = octave down).";
    public string[] Usage => ["Bot ID", "Semitones (-24 to 24)"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio pitch {Bot ID} {Semitones}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!float.TryParse(arguments.At(1), out float semitones) || semitones < -24 || semitones > 24)
        {
            response = "Semitones must be a number between -24 and 24.";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.PitchShift = semitones;
        response = semitones == 0
            ? $"Bot {id}: pitch reset to normal."
            : $"Bot {id}: pitch shift set to {semitones:+0.#;-0.#} semitones (takes effect on next track load).";
        return true;
    }
}
