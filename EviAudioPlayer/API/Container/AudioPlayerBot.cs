using EviAudio.Other;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VoiceChat;
using Object = UnityEngine.Object;

namespace EviAudio.API.Container;

public sealed class AudioPlayerBot
{
    private BotAudioStreamer _streamer;

    public int ID { get; }
    public string Name { get; }
    public Player Player { get; private set; }

    public bool IsPlaying => _streamer != null && _streamer.IsPlaying;
    public bool IsSpawned => Player is Npc npc && npc.IsConnected;

    public event Action<string> OnTrackFinished
    {
        add { if (_streamer != null) _streamer.OnTrackFinished += value; }
        remove { if (_streamer != null) _streamer.OnTrackFinished -= value; }
    }

    public VoiceChatChannel VoiceChatChannel
    {
        get => _streamer?.Channel ?? VoiceChatChannel.Intercom;
        set { if (_streamer != null) _streamer.Channel = value; }
    }

    public float Volume
    {
        get => _streamer?.Volume ?? 100f;
        set { if (_streamer != null) _streamer.Volume = value; }
    }

    public bool Loop
    {
        get => _streamer?.Loop ?? false;
        set { if (_streamer != null) _streamer.Loop = value; }
    }

    public bool Continue
    {
        get => _streamer?.ContinueAfterTrack ?? false;
        set { if (_streamer != null) _streamer.ContinueAfterTrack = value; }
    }

    public bool Shuffle
    {
        get => _streamer?.Shuffle ?? false;
        set { if (_streamer != null) _streamer.Shuffle = value; }
    }

    public bool IsPaused
    {
        get => _streamer?.IsPaused ?? false;
        set { if (_streamer != null) _streamer.IsPaused = value; }
    }

    public float PitchShift
    {
        get => _streamer?.PitchShift ?? 0f;
        set { if (_streamer != null) _streamer.PitchShift = value; }
    }

    public string CurrentTrack => _streamer?.CurrentPlay ?? string.Empty;
    public HashSet<int> BroadcastTo => _streamer?.BroadcastTo;

    private AudioPlayerBot(int id, string name, BotAudioStreamer streamer, Player player)
    {
        ID = id;
        Name = name;
        _streamer = streamer;
        Player = player;
    }

    public static AudioPlayerBot SpawnDummy(
        string name = "[BOT] EviAudio",
        string badgeText = "AudioBot",
        string badgeColor = "red",
        int id = 99,
        RoleTypeId role = RoleTypeId.Tutorial,
        bool ignored = true)
    {
        if (Plugin.AudioPlayerList.TryGetValue(id, out var existing))
        {
            Log.Debug($"Bot ID={id} already exists.");
            return existing;
        }

        Npc npc = Npc.Spawn(name, role, ignored);
        npc.ReferenceHub.nicknameSync.MyNick = name;
        npc.RankName = badgeText;
        npc.RankColor = badgeColor;

        var go = new GameObject($"EviAudio.Bot.{id}");
        go.hideFlags = HideFlags.DontUnloadUnusedAsset;
        var streamer = go.AddComponent<BotAudioStreamer>();
        streamer.Init(npc.ReferenceHub);

        var bot = new AudioPlayerBot(id, name, streamer, npc);
        Plugin.AudioPlayerList[id] = bot;

        Log.Debug($"Bot '{name}' (ID={id}) spawned.");
        return bot;
    }

    public void PlayFile(
        string filePath,
        float volume = 100f,
        bool loop = false,
        VoiceChatChannel? channel = null,
        IEnumerable<int> targetPlayerIds = null,
        bool shuffle = false,
        bool continueQueue = false)
    {
        if (_streamer == null)
        {
            Log.Error($"PlayFile: streamer is null for bot '{Name}' (ID={ID}).");
            return;
        }

        string resolvedPath = Extensions.PathCheck(filePath);
        if (!File.Exists(resolvedPath))
        {
            Log.Warn($"File not found: {resolvedPath}");
            return;
        }

        volume = Math.Clamp(volume, 0f, 100f);

        float graceDelay = Plugin.Instance?.Config?.RoundStartGraceDelay ?? 0f;
        if (graceDelay > 0f && Plugin.RoundStartTime != DateTime.MinValue)
        {
            float elapsed = (float)(DateTime.UtcNow - Plugin.RoundStartTime).TotalSeconds;
            float remaining = graceDelay - elapsed;
            if (remaining > 0f)
            {
                Log.Debug($"Grace-delay {remaining:F2}s before '{Path.GetFileName(resolvedPath)}'.");
                Timing.CallDelayed(remaining,
                    () => PlayFile(filePath, volume, loop, channel, targetPlayerIds, shuffle, continueQueue));
                return;
            }
        }

        _streamer.Stop(clearQueue: true);

        if (channel.HasValue) _streamer.Channel = channel.Value;
        _streamer.Volume = volume;
        _streamer.Loop = loop;
        _streamer.Shuffle = shuffle;
        _streamer.ContinueAfterTrack = continueQueue;

        _streamer.BroadcastTo.Clear();
        if (targetPlayerIds != null)
            foreach (int pid in targetPlayerIds)
                _streamer.BroadcastTo.Add(pid);

        _streamer.Enqueue(resolvedPath);
        _streamer.Play();

        Log.Debug($"▶ {Path.GetFileName(resolvedPath)} ch={_streamer.Channel} vol={volume} loop={loop}");
    }

    public void PlayFolder(
        string folderPath,
        float volume = 100f,
        bool shuffle = false,
        VoiceChatChannel? channel = null,
        IEnumerable<int> targetPlayerIds = null)
    {
        if (_streamer == null) return;

        string resolvedFolder = Directory.Exists(folderPath)
            ? folderPath
            : Path.Combine(Plugin.Instance.AudioPath, folderPath);

        if (!Directory.Exists(resolvedFolder))
        {
            Log.Warn($"PlayFolder: folder not found '{resolvedFolder}'.");
            return;
        }

        _streamer.Stop(clearQueue: true);

        if (channel.HasValue) _streamer.Channel = channel.Value;
        _streamer.Volume = Math.Clamp(volume, 0f, 100f);
        _streamer.ContinueAfterTrack = true;
        _streamer.Shuffle = shuffle;

        _streamer.BroadcastTo.Clear();
        if (targetPlayerIds != null)
            foreach (int pid in targetPlayerIds)
                _streamer.BroadcastTo.Add(pid);

        _streamer.EnqueueFolder(resolvedFolder, shuffle);
        _streamer.Play();

        Log.Debug($"▶ folder '{resolvedFolder}' shuffle={shuffle}");
    }

    public void PlayM3U(
        string m3uPath,
        float volume = 100f,
        VoiceChatChannel? channel = null,
        IEnumerable<int> targetPlayerIds = null)
    {
        if (_streamer == null) return;

        string resolved = Extensions.PathCheck(m3uPath);
        if (!File.Exists(resolved))
        {
            Log.Warn($"PlayM3U: file not found '{resolved}'.");
            return;
        }

        _streamer.Stop(clearQueue: true);

        if (channel.HasValue) _streamer.Channel = channel.Value;
        _streamer.Volume = Math.Clamp(volume, 0f, 100f);
        _streamer.ContinueAfterTrack = true;

        _streamer.BroadcastTo.Clear();
        if (targetPlayerIds != null)
            foreach (int pid in targetPlayerIds)
                _streamer.BroadcastTo.Add(pid);

        _streamer.EnqueueM3U(resolved);
        _streamer.Play();
    }

    public void Enqueue(string filePath, int position = -1)
    {
        string resolvedPath = Extensions.PathCheck(filePath);
        if (!File.Exists(resolvedPath))
        {
            Log.Warn($"Enqueue: file not found '{resolvedPath}'.");
            return;
        }
        _streamer?.Enqueue(resolvedPath, position);
    }

    public void Skip() => _streamer?.Skip();

    public List<string> GetQueue() => _streamer?.GetQueue() ?? new List<string>();

    public void FadeTo(float targetVolume, float duration) => _streamer?.FadeTo(targetVolume, duration);

    public void PlayIntercom(string filePath, float volume = 100f, bool loop = false)
        => PlayFile(filePath, volume, loop, VoiceChatChannel.Intercom);

    public void StopAudio(bool clearQueue = true)
    {
        try { _streamer?.Stop(clearQueue); } catch { }
    }

    internal void Duck(float duckVolume, float fadeTime) => _streamer?.Duck(duckVolume, fadeTime);

    internal void Unduck(float fadeTime) => _streamer?.Unduck(fadeTime);

    public void HandleExternalNpcDestroy()
    {
        if (_streamer != null)
        {
            _streamer.Stop(clearQueue: true);
            Object.Destroy(_streamer.gameObject);
            _streamer = null;
        }
        Player = null;
        Log.Debug($"Bot '{Name}' (ID={ID}): NPC destroyed externally.");
    }

    public void SafeDestroy()
    {
        Plugin.AudioPlayerList.Remove(ID);
        StopAudio(clearQueue: true);

        if (Player is Npc npc)
            try { if (npc.IsConnected) npc.Destroy(); } catch { }

        if (_streamer != null)
        {
            Object.Destroy(_streamer.gameObject);
            _streamer = null;
        }
        Player = null;

        Log.Debug($"Bot '{Name}' (ID={ID}): destroyed.");
    }
}
