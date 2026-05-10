using CommandSystem;
using EviAudio.API;
using Exiled.Permissions.Extensions;
using System;
using VoiceChat;

namespace EviAudio.Commands.SubCommands;

public class VoiceChannel : ICommand, IUsageProvider
{
    public string Command => "voicechannel";
    public string[] Aliases => ["voice", "channel", "chan", "audiochannel"];
    public string Description => "Change the voice channel a bot broadcasts on.";
    public string[] Usage => ["Bot ID", "VoiceChatChannel"];

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.CheckPermission($"audioplayer.{Command}"))
        {
            response = $"No permission: audioplayer.{Command}";
            return false;
        }

        if (arguments.Count < 2)
        {
            response = "Usage: audio voicechannel {Bot ID} {VoiceChatChannel}";
            return false;
        }

        if (!int.TryParse(arguments.At(0), out int id))
        {
            response = "Bot ID must be a number.";
            return false;
        }

        if (!Enum.TryParse(arguments.At(1), out VoiceChatChannel channel))
        {
            response = $"Unknown VoiceChatChannel: {arguments.At(1)}. Valid values: {string.Join(", ", Enum.GetNames(typeof(VoiceChatChannel)))}";
            return false;
        }

        var bot = AudioController.TryGetAudioPlayerContainer(id);
        if (bot == null)
        {
            response = $"Bot with ID {id} not found.";
            return false;
        }

        bot.VoiceChatChannel = channel;
        response = $"Bot {id}: channel set to {channel}.";
        return true;
    }
}
