using AdminToys;
using EviAudio.API;
using Exiled.API.Features;
using MEC;
using Mirror;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VoiceChat.Codec;
using VoiceChat.Codec.Enums;
using VoiceChat.Networking;

namespace EviAudio.API.Spatial;

public sealed class SpatialAudioPlayer : MonoBehaviour
{
    private readonly Queue<QueueEntry> _queue = new();
    private readonly object _queueLock = new();
    private readonly float[] _pcmBuffer = new float[AudioClipPlayback.PacketSize];
    private readonly byte[] _encodedBuffer = new byte[512];
    private float[] _samples = Array.Empty<float>();
    private int _sampleOffset;
    private double _lastSendTime = -1;
    private volatile PendingPlay _pendingPlay;
    private volatile int _loadVersion;
    private OpusEncoder _encoder;
    private byte _allocatedId;
    private float _lifetimeRemaining;
    private bool _lifetimeActive;
    private CoroutineHandle _fadeHandle;
    private Transform _attachedTo;
    private int _registryId;

    public SpeakerToy Speaker { get; private set; }
    public float Volume { get; set; } = 1f;
    public bool Loop { get; set; }
    public float PitchShift { get; set; } = 0f;
    public float Lifetime { get; set; } = 0f;
    public bool IsPlaying => _samples.Length > 0 || _pendingPlay != null;
    public string CurrentFile { get; private set; } = string.Empty;
    public int RegistryId => _registryId;

    public event Action OnTrackFinished;

    public static SpatialAudioPlayer Create(
        Vector3 position,
        float volume = 1f,
        bool isSpatial = true,
        float minDistance = 5f,
        float maxDistance = 15f)
    {
        SpeakerToy prefab = null;
        foreach (var pref in NetworkClient.prefabs.Values)
            if (pref.TryGetComponent(out prefab))
                break;

        if (prefab == null)
        {
            Log.Error("SpatialAudioPlayer: SpeakerToy prefab not found.");
            return null;
        }

        byte id = AllocateControllerId();
        var toy = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
        toy.NetworkControllerId = id;
        toy.NetworkVolume = volume;
        toy.NetworkIsSpatial = isSpatial;
        toy.NetworkMinDistance = minDistance;
        toy.NetworkMaxDistance = maxDistance;
        NetworkServer.Spawn(toy.gameObject);

        var player = toy.gameObject.AddComponent<SpatialAudioPlayer>();
        player.Speaker = toy;
        player._allocatedId = id;
        player._encoder = new OpusEncoder(OpusApplicationType.Voip);
        player._registryId = SpatialAudioRegistry.Register(player);
        return player;
    }

    public void Play(string path, float volume = 1f, bool loop = false, float lifetime = 0f)
    {
        _samples = Array.Empty<float>();
        _sampleOffset = 0;
        Volume = volume;
        Loop = loop;
        CurrentFile = path;

        if (Lifetime <= 0f && lifetime > 0f)
            Lifetime = lifetime;

        if (Lifetime > 0f)
        {
            _lifetimeRemaining = Lifetime;
            _lifetimeActive = true;
        }

        int version = Interlocked.Increment(ref _loadVersion);
        float pitch = PitchShift;

        Task.Run(() =>
        {
            float[] samples;
            try
            {
                var data = PcmDecoder.DecodeFile(path, path, pitch);
                samples = data.Samples;
            }
            catch (Exception ex)
            {
                Log.Error($"SpatialAudioPlayer: failed to decode '{path}': {ex.Message}");
                return;
            }
            if (_loadVersion == version)
                _pendingPlay = new PendingPlay(samples, volume, loop);
        });
    }

    public void Enqueue(string path, float volume = 1f, bool loop = false)
    {
        lock (_queueLock)
            _queue.Enqueue(new QueueEntry(path, volume, loop));
    }

    public void Stop()
    {
        Interlocked.Increment(ref _loadVersion);
        _pendingPlay = null;
        _samples = Array.Empty<float>();
        _sampleOffset = 0;
        _lastSendTime = -1;
        _lifetimeActive = false;
        CurrentFile = string.Empty;
        lock (_queueLock) _queue.Clear();
    }

    public void AttachTo(Transform target) => _attachedTo = target;

    public void DetachFrom() => _attachedTo = null;

    public void FadeTo(float targetVolume, float duration)
    {
        if (_fadeHandle.IsRunning)
            Timing.KillCoroutines(_fadeHandle);
        _fadeHandle = Timing.RunCoroutine(FadeCoroutine(Volume, targetVolume, duration));
    }

    public void SetPosition(Vector3 position)
    {
        if (Speaker != null)
            Speaker.transform.position = position;
    }

    public void SetRotation(Quaternion rotation)
    {
        if (Speaker != null)
            Speaker.transform.rotation = rotation;
    }

    public void SetMinDistance(float min)
    {
        if (Speaker != null)
            Speaker.NetworkMinDistance = min;
    }

    public void SetMaxDistance(float max)
    {
        if (Speaker != null)
            Speaker.NetworkMaxDistance = max;
    }

    public void SetSpatial(bool spatial)
    {
        if (Speaker != null)
            Speaker.NetworkIsSpatial = spatial;
    }

    private IEnumerator<float> FadeCoroutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Volume = Mathf.Lerp(from, to, elapsed / duration);
            yield return Timing.WaitForOneFrame;
        }
        Volume = to;
    }

    private void Update()
    {
        if (_attachedTo != null && Speaker != null)
            Speaker.transform.position = _attachedTo.position;

        var pending = _pendingPlay;
        if (pending != null)
        {
            _pendingPlay = null;
            _samples = pending.Samples;
            Volume = pending.Volume;
            Loop = pending.Loop;
            _sampleOffset = 0;
            _lastSendTime = -1;
        }

        if (_samples.Length == 0) return;

        if (_lifetimeActive)
        {
            _lifetimeRemaining -= Time.deltaTime;
            if (_lifetimeRemaining <= 0f)
            {
                UnityEngine.Object.Destroy(gameObject);
                return;
            }
        }

        double now = (double)Time.unscaledTime;
        double interval = (double)AudioClipPlayback.PacketSize / AudioClipPlayback.SamplingRate;
        if (_lastSendTime < 0)
            _lastSendTime = now;

        while (now - _lastSendTime >= interval)
        {
            if (!SendPacket()) break;
            _lastSendTime += interval;
        }
    }

    private bool SendPacket()
    {
        if (_sampleOffset >= _samples.Length)
        {
            if (Loop)
            {
                _sampleOffset = 0;
            }
            else
            {
                bool hasNext;
                QueueEntry next = default;
                lock (_queueLock)
                {
                    hasNext = _queue.Count > 0;
                    if (hasNext) next = _queue.Dequeue();
                }

                OnTrackFinished?.Invoke();

                if (hasNext)
                    Play(next.Path, next.Volume, next.Loop);
                else
                {
                    CurrentFile = string.Empty;
                    _samples = Array.Empty<float>();
                    _sampleOffset = 0;
                    _lastSendTime = -1;
                }
                return false;
            }
        }

        int count = Math.Min(AudioClipPlayback.PacketSize, _samples.Length - _sampleOffset);
        Array.Copy(_samples, _sampleOffset, _pcmBuffer, 0, count);
        if (count < AudioClipPlayback.PacketSize)
            Array.Clear(_pcmBuffer, count, AudioClipPlayback.PacketSize - count);

        if (Volume != 1f)
            for (int i = 0; i < AudioClipPlayback.PacketSize; i++)
                _pcmBuffer[i] *= Volume;

        _sampleOffset += count;
        int encodedLen = _encoder.Encode(_pcmBuffer, _encodedBuffer);
        if (encodedLen <= 0) return true;

        var msg = new AudioMessage(Speaker.ControllerId, _encodedBuffer, encodedLen);
        foreach (ReferenceHub hub in ReferenceHub.AllHubs)
            hub.connectionToClient?.Send(msg);

        return true;
    }

    private void OnDestroy()
    {
        Interlocked.Increment(ref _loadVersion);
        _pendingPlay = null;
        _encoder?.Dispose();
        _encoder = null;
        FreeControllerId(_allocatedId);
        SpatialAudioRegistry.Unregister(_registryId);

        if (_fadeHandle.IsRunning)
            Timing.KillCoroutines(_fadeHandle);

        if (Speaker != null)
        {
            NetworkServer.UnSpawn(Speaker.gameObject);
            UnityEngine.Object.Destroy(Speaker.gameObject);
            Speaker = null;
        }
    }

    private static readonly bool[] _usedIds = new bool[256];
    private static byte _idCursor = 1;
    private static readonly object _idLock = new();

    private static byte AllocateControllerId()
    {
        lock (_idLock)
        {
            for (int attempts = 0; attempts < 255; attempts++)
            {
                byte id = _idCursor;
                _idCursor = _idCursor == 255 ? (byte)1 : (byte)(_idCursor + 1);
                if (!_usedIds[id])
                {
                    _usedIds[id] = true;
                    return id;
                }
            }
        }
        throw new InvalidOperationException("All 255 SpatialAudioPlayer controller IDs are in use.");
    }

    private static void FreeControllerId(byte id)
    {
        lock (_idLock)
            _usedIds[id] = false;
    }

    private sealed class PendingPlay
    {
        public readonly float[] Samples;
        public readonly float Volume;
        public readonly bool Loop;
        public PendingPlay(float[] samples, float volume, bool loop)
        {
            Samples = samples;
            Volume = volume;
            Loop = loop;
        }
    }

    private readonly struct QueueEntry
    {
        public readonly string Path;
        public readonly float Volume;
        public readonly bool Loop;
        public QueueEntry(string path, float volume, bool loop)
        {
            Path = path;
            Volume = volume;
            Loop = loop;
        }
    }
}
