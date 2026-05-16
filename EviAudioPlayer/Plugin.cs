using EviAudio.API.Container;
using EviAudio.API;
using EviAudio.API.Spatial;
using EviAudio.API.Preset;
using EviAudio.API.Zone;
using EviAudio.Other;
using EviAudio.Other.DLC;
using Exiled.API.Features;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace EviAudio;

public class Plugin : Plugin<Config>
{
    public static readonly ConcurrentDictionary<int, AudioPlayerBot> AudioPlayerList = new();
    public readonly string PluginFolder = Path.Combine(Paths.Plugins, "EviAudio");
    public string AudioPath => Path.Combine(PluginFolder, "tracks");
    public static DateTime RoundStartTime { get; internal set; } = DateTime.MinValue;

    public override string Prefix => "EviAudio";
    public override string Name => "EviAudio";
    public override string Author => "EviZi1337";
    public override Version Version => new(2, 1, 0);

    public static Plugin Instance { get; private set; }

    internal static EventHandler EventHandlers;
    internal static SpecialEvents SpecialEvents;
    internal static WarheadEvents WarheadEvents;
    internal static LobbyEvents LobbyEvents;
    internal static CassieDucking CassieDucking;
    internal static AudioZoneManager ZoneManager;

    public override void OnEnabled()
    {
        try
        {
            Instance = this;
            AudioClipCache.MaxBytes = Math.Max(16, Config.AudioCacheMaxMegabytes) * 1024L * 1024L;
            EventHandlers = new EventHandler();

            if (Config.SpecialEventsEnable)
            {
                SpecialEvents = new SpecialEvents();
                WarheadEvents = new WarheadEvents();
                LobbyEvents = new LobbyEvents();
            }

            if (Config.EnableCassieDucking)
                CassieDucking = new CassieDucking();

            if (Config.EnableAudioZones)
                ZoneManager = new AudioZoneManager();

            Extensions.CreateDirectory();
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to enable: {ex}");
        }

        Log.Info("Hello World!");
        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        SceneManager.DeactivateAll();

        EventHandlers?.Dispose();
        SpecialEvents?.Dispose();
        WarheadEvents?.Dispose();
        LobbyEvents?.Dispose();
        CassieDucking?.Dispose();
        ZoneManager?.Dispose();

        foreach (var bot in AudioPlayerList.Values.ToList())
            bot.SafeDestroy();

        AudioPlayerList.Clear();
        SpatialAudioRegistry.Clear();
        ControllerIdPool.Clear();
        AudioClipCache.Clear();

        EventHandlers = null;
        SpecialEvents = null;
        WarheadEvents = null;
        LobbyEvents = null;
        CassieDucking = null;
        ZoneManager = null;
        Instance = null;

        base.OnDisabled();
    }
}
