using EviAudio.API.Container;
using EviAudio.API.Preset;
using EviAudio.API.Spatial;
using EviAudio.Other;
using Exiled.API.Features;
using System.Collections.Generic;
using UnityEngine;
using VoiceChat;

namespace EviAudio.API;

public static class EviAudioAPI
{
    private static bool EnsureReady(out string error)
    {
        if (Plugin.Instance == null)
        {
            error = "EviAudio plugin is not loaded.";
            return false;
        }
        error = null;
        return true;
    }

    public static AudioPlayerBot SpawnBot(
        string name = "[BOT] EviAudio",
        string badgeText = "AudioBot",
        string badgeColor = "red",
        int id = 99)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return null; }
        return AudioController.SpawnDummy(id, badgeText, badgeColor, name);
    }

    public static void DestroyBot(int id)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        AudioController.DisconnectDummy(id);
    }

    public static AudioPlayerBot GetBot(int id)
    {
        if (!EnsureReady(out _)) return null;
        return AudioController.TryGetAudioPlayerContainer(id);
    }

    public static IReadOnlyCollection<AudioPlayerBot> GetAllBots()
    {
        if (!EnsureReady(out _)) return System.Array.Empty<AudioPlayerBot>();
        return (IReadOnlyCollection<AudioPlayerBot>)AudioController.GetAllAudioPlayers();
    }

    public static void Play(
        int botId,
        string filePath,
        float volume = 100f,
        bool loop = false,
        VoiceChatChannel? channel = null,
        IEnumerable<int> targetPlayerIds = null)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        var bot = AudioController.TryGetAudioPlayerContainer(botId);
        if (bot == null) { Log.Warn($"Bot {botId} not found."); return; }
        bot.PlayFile(filePath, volume, loop, channel, targetPlayerIds);
    }

    public static void PlayFolder(
        int botId,
        string folderPath,
        float volume = 100f,
        bool shuffle = false,
        VoiceChatChannel? channel = null)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        var bot = AudioController.TryGetAudioPlayerContainer(botId);
        if (bot == null) { Log.Warn($"Bot {botId} not found."); return; }
        bot.PlayFolder(folderPath, volume, shuffle, channel);
    }

    public static void PlayM3U(int botId, string m3uPath, float volume = 100f, VoiceChatChannel? channel = null)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        var bot = AudioController.TryGetAudioPlayerContainer(botId);
        if (bot == null) { Log.Warn($"Bot {botId} not found."); return; }
        bot.PlayM3U(m3uPath, volume, channel);
    }

    public static void Stop(int botId, bool clearQueue = true)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        AudioController.TryGetAudioPlayerContainer(botId)?.StopAudio(clearQueue);
    }

    public static void Fade(int botId, float targetVolume, float duration)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return; }
        AudioController.TryGetAudioPlayerContainer(botId)?.FadeTo(targetVolume, duration);
    }

    public static SpatialAudioPlayer CreateSpatialPlayer(
        Vector3 position,
        float volume = 1f,
        bool isSpatial = true,
        float minDistance = 5f,
        float maxDistance = 15f,
        float lifetime = 0f,
        float pitchShift = 0f)
    {
        if (!EnsureReady(out string err)) { Log.Error($"{err}"); return null; }
        var player = SpatialAudioPlayer.Create(position, volume, isSpatial, minDistance, maxDistance);
        if (player == null) return null;
        player.Lifetime = lifetime;
        player.PitchShift = pitchShift;
        return player;
    }

    public static (bool success, string message, List<SpatialAudioPlayer> players) ActivateScene(string presetName)
    {
        if (!EnsureReady(out string err)) return (false, err, null);
        return SceneManager.ActivateScene(presetName);
    }

    public static (bool success, string message) DeactivateScene(string presetName)
    {
        if (!EnsureReady(out string err)) return (false, err);
        return SceneManager.DeactivateScene(presetName);
    }

    public static void DeactivateAllScenes()
    {
        if (!EnsureReady(out _)) return;
        SceneManager.DeactivateAll();
    }

    public static SpatialAudioPlayer GetSpatialPlayer(int registryId)
    {
        if (!EnsureReady(out _)) return null;
        return SpatialAudioRegistry.Get(registryId);
    }

    public static IReadOnlyDictionary<int, SpatialAudioPlayer> GetAllSpatialPlayers()
    {
        if (!EnsureReady(out _)) return new System.Collections.Generic.Dictionary<int, SpatialAudioPlayer>();
        return SpatialAudioRegistry.All;
    }
}
