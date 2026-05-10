using EviAudio.API.Spatial;
using EviAudio.Other;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EviAudio.API.Zone;

public sealed class AudioZoneManager
{
    private readonly List<SpatialAudioPlayer> _ambientPlayers = new();
    private readonly HashSet<string> _triggeredPlayers = new();

    public AudioZoneManager()
    {
        Exiled.Events.Handlers.Player.RoomChanged += OnRoomChanged;
        Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
    }

    public void Dispose()
    {
        Exiled.Events.Handlers.Player.RoomChanged -= OnRoomChanged;
        Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
        CleanupAmbient();
    }

    public void SpawnAmbientSpeakers()
    {
        CleanupAmbient();

        if (Plugin.Instance?.Config?.AudioZones == null) return;

        foreach (var zone in Plugin.Instance.Config.AudioZones)
        {
            if (string.IsNullOrEmpty(zone.AmbientFile)) continue;

            string path = Extensions.PathCheck(zone.AmbientFile);
            if (!File.Exists(path))
            {
                Log.Warn($"AudioZoneManager: ambient file not found '{path}'.");
                continue;
            }

            foreach (var room in GetMatchingRooms(zone))
            {
                var player = SpatialAudioPlayer.Create(
                    room.Position + Vector3.up,
                    zone.AmbientVolume,
                    true,
                    zone.AmbientMinDistance,
                    zone.AmbientMaxDistance);

                if (player == null) continue;

                player.PitchShift = zone.AmbientPitchShift;
                player.Play(path, zone.AmbientVolume, true);
                _ambientPlayers.Add(player);
            }
        }

        Log.Debug($"AudioZoneManager: spawned {_ambientPlayers.Count} ambient speaker(s).");
    }

    private void OnRoomChanged(RoomChangedEventArgs ev)
    {
        if (ev.Player == null || ev.Player.IsNPC || ev.NewRoom == null) return;
        if (Plugin.Instance?.Config?.AudioZones == null) return;

        foreach (var zone in Plugin.Instance.Config.AudioZones)
        {
            if (string.IsNullOrEmpty(zone.TriggerFile)) continue;
            if (!RoomMatches(ev.NewRoom, zone)) continue;

            string triggerKey = $"{ev.Player.UserId}:{zone.RoomType}:{zone.AmbientZone}";
            if (zone.TriggerOncePerRound && _triggeredPlayers.Contains(triggerKey)) continue;

            var bot = AudioController.TryGetAudioPlayerContainer(zone.TriggerBotId);
            if (bot == null) continue;

            string path = Extensions.PathCheck(zone.TriggerFile);
            if (!File.Exists(path)) continue;

            bot.PlayFile(path, zone.TriggerVolume, zone.TriggerLoop, zone.TriggerChannel, new[] { ev.Player.Id });

            if (zone.TriggerOncePerRound)
                _triggeredPlayers.Add(triggerKey);
        }
    }

    private void OnRoundEnded(Exiled.Events.EventArgs.Server.RoundEndedEventArgs _)
    {
        _triggeredPlayers.Clear();
    }

    public void CleanupAmbient()
    {
        foreach (var player in _ambientPlayers)
            try { UnityEngine.Object.Destroy(player.gameObject); } catch { }
        _ambientPlayers.Clear();
    }

    private static IEnumerable<Room> GetMatchingRooms(AudioZoneConfig zone)
    {
        if (!string.IsNullOrWhiteSpace(zone.RoomType) && Enum.TryParse(zone.RoomType, out RoomType roomType))
            return Room.List.Where(r => r.Type == roomType);

        if (!string.IsNullOrWhiteSpace(zone.AmbientZone) && Enum.TryParse(zone.AmbientZone, out ZoneType zoneType))
            return Room.List.Where(r => r.Zone == zoneType);

        return Array.Empty<Room>();
    }

    private static bool RoomMatches(Room room, AudioZoneConfig zone)
    {
        if (!string.IsNullOrWhiteSpace(zone.RoomType) && Enum.TryParse(zone.RoomType, out RoomType roomType))
            return room.Type == roomType;

        if (!string.IsNullOrWhiteSpace(zone.AmbientZone) && Enum.TryParse(zone.AmbientZone, out ZoneType zoneType))
            return room.Zone == zoneType;

        return false;
    }
}
