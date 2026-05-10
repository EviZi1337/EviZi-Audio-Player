using System.ComponentModel;

namespace EviAudio.Other;

public class BotsList
{
    [Description("Display name of the bot NPC in-game.")]
    public string BotName { get; set; } = "Dedicated Server";

    [Description("Unique numeric ID used in all audio commands for this bot.")]
    public int BotId { get; set; } = 99;

    [Description("Badge text shown in Remote Admin next to the bot name.")]
    public string BadgeText { get; set; } = "AudioPlayer BOT";

    [Description("Badge colour. Supports EXILED colour names, e.g. orange, red, green.")]
    public string BadgeColor { get; set; } = "orange";
}
