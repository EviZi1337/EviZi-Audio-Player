using EviAudio.API.Container;
using EviAudio.API.Preset;
using EviAudio.API.Spatial;
using EviAudio.Other;
using EviAudio.Other.DLC;
using Exiled.Events.EventArgs.Player;
using System.Collections.Generic;
using static EviAudio.Plugin;

namespace EviAudio;

internal sealed class EventHandler
{
    internal EventHandler()
    {
        Exiled.Events.Handlers.Player.Destroying += OnDestroying;
        Exiled.Events.Handlers.Map.Generated += OnGenerated;
        Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
    }

    internal void Dispose()
    {
        Exiled.Events.Handlers.Player.Destroying -= OnDestroying;
        Exiled.Events.Handlers.Map.Generated -= OnGenerated;
        Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
    }

    private static void OnDestroying(DestroyingEventArgs ev)
    {
        foreach (KeyValuePair<int, AudioPlayerBot> kvp in AudioPlayerList)
        {
            if (ReferenceEquals(kvp.Value.Player, ev.Player))
            {
                kvp.Value.HandleExternalNpcDestroy();
                break;
            }
        }
    }

    private static void OnGenerated()
    {
        SceneManager.DeactivateAll();
        ZoneManager?.CleanupAmbient();
        SpatialAudioRegistry.Clear();

        var bots = new List<AudioPlayerBot>(AudioPlayerList.Values);
        foreach (var bot in bots)
            bot.SafeDestroy();

        AudioPlayerList.Clear();

        // LobbyEvents self-destructs after first round, gotta revive it every new map been chasing this for four days straight, holy shit
        if (Instance.Config.SpecialEventsEnable && Plugin.LobbyEvents == null)
            Plugin.LobbyEvents = new LobbyEvents();

        if (!Instance.Config.SpawnBot) return;

        foreach (BotsList cfg in Instance.Config.BotsList)
            AudioPlayerBot.SpawnDummy(cfg.BotName, cfg.BadgeText, cfg.BadgeColor, cfg.BotId);

        if (Instance.Config.EnableAudioZones && ZoneManager != null)
            ZoneManager.SpawnAmbientSpeakers();
    }

    private static void OnRoundStarted()
    {
        Plugin.RoundStartTime = System.DateTime.UtcNow;
    }
}