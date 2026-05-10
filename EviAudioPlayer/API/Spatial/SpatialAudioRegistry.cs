using System.Collections.Generic;

namespace EviAudio.API.Spatial;

public static class SpatialAudioRegistry
{
    private static readonly Dictionary<int, SpatialAudioPlayer> _players = new();
    private static int _nextId = 1;

    public static IReadOnlyDictionary<int, SpatialAudioPlayer> All => _players;

    internal static int Register(SpatialAudioPlayer player)
    {
        int id = _nextId++;
        _players[id] = player;
        return id;
    }

    internal static void Unregister(int id) => _players.Remove(id);

    public static SpatialAudioPlayer Get(int id)
        => _players.TryGetValue(id, out var p) ? p : null;

    internal static void Clear() => _players.Clear();
}
