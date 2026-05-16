# EviAudio

An EXILED plugin that turns SCP:SL into a schizophrenic radio station with spatial audio, music bots, ambient zones, event sounds, scene presets, YouTube streaming, and enough noise pollution to make CASSIE file a lawsuit.

Made because normal silent servers feel like abandoned parking garages.

Now with actual YouTube support. Yes. Real YouTube. In SCP:SL. I don't know either.

Needs EXILED. FFmpeg. NVorbis. yt-dlp if you want streaming. We're building infrastructure here.

---

## What does this thing even do

This plugin lets you:

* Spawn audio bots that blast music through voice chat
* Stream YouTube, SoundCloud, Twitch, Vimeo, or any internet radio directly in-game with zero disk usage
* Create full 3D spatial audio zones across the entire facility
* Trigger private sounds when players enter specific rooms
* Crossfade between tracks like a DJ who made questionable life choices
* Give different players different volumes from the same audio source for reasons nobody asked about
* Seek to any position in any track
* Read track metadata — title, artist, album — because your SCP server deserves better music software than your phone
* Play sounds during lobby, round start, MTF spawns, Chaos spawns, warhead events, deaths, kills, joins, whatever your sleep-deprived brain invents at 3am
* Build reusable audio scenes with dozens of spatial speakers across the map
* Make HCZ sound like a horror movie basement designed by Soviet engineers on a deadline

Basically if you ever thought:

> *"what if SCP:SL but the facility had dynamic sound design and psychological warfare against its own playerbase"*

yeah. that's what this is.

---

## Installation

1. Grab the `.dll`
2. Drop it into:
```
EXILED/Plugins/
```

3. Drop `ffmpeg.exe` (Windows) or `ffmpeg` (Linux) into:
```
EXILED/Plugins/EviAudio/ffmpeg/
```

FFmpeg handles every audio format known to mankind. Without it you're limited to `.ogg` like a caveman with taste issues. Get it at [ffmpeg.org](https://ffmpeg.org/download.html). Free. Always been free. Stop using that converter website that also installs a toolbar.

4. For YouTube / SoundCloud / Twitch — drop `yt-dlp.exe` (Windows) or `yt-dlp` (Linux) into the same folder:
```
EXILED/Plugins/EviAudio/ffmpeg/
```

Single binary, no install, no setup. [github.com/yt-dlp/yt-dlp/releases](https://github.com/yt-dlp/yt-dlp/releases). yt-dlp gets the real audio URL, FFmpeg streams it. Nothing touches your disk. Your server stays clean.

5. Start server — plugin auto-generates all configs and folders
6. Put your local audio files in:
```
EXILED/Plugins/EviAudio/tracks/
```
7. Restart server
8. Congratulations, your SCP server is now a streaming platform that happens to also have SCPs in it

---

## Supported Formats

With FFmpeg installed:

| Format | Notes |
|--------|-------|
| `.ogg` | Native, fastest, recommended |
| `.mp3` | Works fine, stop feeling guilty about it |
| `.wav` | Supported including RF64 for files over 4GB because why not |
| `.flac` | Audiophile mode |
| `.aac` | Sure |
| `.opus` | Based format |
| YouTube URL | `https://youtube.com/watch?v=...` |
| SoundCloud URL | `https://soundcloud.com/...` |
| Twitch VOD/stream | `https://twitch.tv/...` |
| Vimeo | `https://vimeo.com/...` |
| HTTP radio stream | `https://stream.example.com/radio` |

Without FFmpeg: `.ogg` only. You made your choice.

---

## Features

### Audio Bots

Bots are fake NPCs that stream audio through voice chat. Spawned automatically on map generation or manually via command.

They:
* Play local files or stream from the internet
* Support multiple IDs for different purposes simultaneously
* Work as private trigger broadcasters for room events
* Can follow a specific player around the map
* Exist purely to violate silence and server owners' expectations

Default bot:

| Property | Default |
|----------|---------|
| Name | EviAudio Bot |
| Bot ID | 99 |
| Badge | Music Bot |
| Badge Color | blue |

Local tracks go here:
```
EXILED/Plugins/EviAudio/tracks/
```

---

### YouTube & Internet Streaming

```
audio play 99 https://youtube.com/watch?v=dQw4w9WgXcQ
audio play 99 https://soundcloud.com/artist/track
audio play 99 https://stream.example.com/radio.ogg
```

How it works: yt-dlp extracts the real CDN URL in the background, FFmpeg pipes raw PCM audio directly into the streaming buffer. Nothing is downloaded. No temp files. No cleanup. The audio starts playing and keeps playing until you stop it or the stream ends.

Twitch takes about 20 seconds to resolve because Twitch is Twitch. It works. Be patient.

Playlists are not supported. One source at a time. This is a containment facility.

---

### Seek, Crossfade & Playback Control

**Seek to any position:**
```
audio seek 99 1:23
audio seek 99 83
```
Jump anywhere in the track. Skip intros. Find the drop. Skip the part of Bohemian Rhapsody your players somehow hate.

**Start/end parameters:**
```
audio play 99 song.ogg --start 30 --end 90
```
Play only a specific section. For highlights, stingers, or when the full track is 40 minutes and your players have the attention span of a caffeinated SCP-096.

**Crossfade:**
```
audio crossfade 99 nextsong.ogg 100
```
Smooth transition with real sample mixing. Not a fade out then fade in. Actual crossfade. The difference between a server that sounds intentional and one that sounds like a corrupted CD from 2003.

**Follow a player:**
```
audio follow 99 PlayerName
audio follow 99 stop
```
Bot teleports to the player every tick. Useful for personal death sounds, personal victory music, or pure psychological torment depending on your server's vibe.

---

### Per-Player Volume

```
audio playervolume 99 PlayerID 50
```

Different volumes for different players from the same audio source. Player A hears full volume. Player B hears half. They are standing next to each other. Neither knows why. This should not be possible. It is.

---

### Track Metadata & Status

```
audio status 99
```

Shows: title, artist, album, current position, total duration. Read from ID3 tags (mp3) and Vorbis comments (ogg/flac) automatically. Your SCP server now has better music info display than some actual music apps. We don't discuss how this happened.

---

### Diagnose Command

```
audio diagnose
```

Shows in one command:
* FFmpeg — found or not found, exact path
* yt-dlp — found or not found, exact path
* Audio clip cache — items loaded, megabytes used
* Controller IDs — how many in use out of 255
* Active bots — count
* Active spatial speakers — count

For when something breaks and you need to know what's actually running without reading server logs for 10 minutes.

---

### Event Audio System

Automatic sounds during game events. Enable with `special_events_enable: true`.

Supported events:
* Lobby — random playlist, loops until round starts
* Round start
* Round end
* MTF spawn
* Chaos Insurgency spawn
* Warhead arming
* Warhead disarm
* Player death — played privately only to the person who died
* Player kill — played privately only to the killer
* Player join

You can randomize clips per event because hearing the same sound 400 times a round is how players develop opinions about your server that they express publicly.

Also:
* Optional suppression of default CASSIE MTF announcement
* Grace delay after round start to prevent sound collision disasters
* Private audio delivered directly to specific players regardless of position

Yes you can make every death play a Vine boom. Nobody will stop you. I've accepted this.

---

### Audio Presets / Scenes

Named collections of spatial speakers that activate and deactivate together with one command.

```
audio scene activate Warhead_Emergency
audio scene deactivate Warhead_Emergency
audio scene list
```

Use cases:
* Warhead sirens across every zone simultaneously
* SCP-themed ambient for specific rooms
* Horror soundtrack for HCZ
* Fake reactor meltdown soundscape
* Dynamic raid music
* Psychological warfare against your playerbase
* Making new players question whether the sounds are base game (they are not)

Each speaker supports:

| Setting | What it does |
|---------|-------------|
| file | Local file or URL |
| volume | 0–100 |
| loop | Infinite suffering |
| min_distance | Close hearing range |
| max_distance | Maximum audible range |
| lifetime | Auto-destroy timer in seconds |
| pitch_shift | Semitones. Positive = higher. Negative = cursed. |
| spawn_in_room | Specific room by RoomType name |
| spawn_in_zone | Every room in a zone |
| position | Manual world coordinates |

---

### Audio Zones

Per-room automatic sound systems. Two types that can coexist in the same room:

#### Ambient Audio

Looping 3D spatial audio spawned at room center when the map generates. Gets louder as players approach, quieter as they leave. Real positional audio.

Examples:
* HCZ industrial hum
* Reactor ambience
* SCP breathing in specific rooms
* Soviet bunker vibes
* Wind on surface
* Whatever is wrong with you creatively

#### Trigger Audio

Private sound played to a specific player the moment they enter a room. Nobody else hears it.

Examples:
* Tense sting when entering 173
* Warning voice line entering armory
* Whispering in dark hallways because apparently your players needed more anxiety
* Jump scare with `trigger_once_per_round: false` for maximum ethical violations

Options:
* `trigger_once_per_round: true` — plays once per player per round
* Per-room or per-zone targeting
* Custom bot ID and voice channel
* Looping trigger audio

---

### Cassie Ducking

When CASSIE makes an announcement, all bot volumes automatically lower. When she finishes, they restore.

Configurable fade speed, target volume, and which channels to duck. Meaning players can actually hear that the warhead is armed instead of figuring it out visually 30 seconds later. Functional server design. Surprisingly rare.

---

### Speaker Visualizer

```
audio visualize
```

Spawns admin toy spheres showing min and max audible range of every active spatial speaker. Green = minimum distance. Blue = maximum distance.

For when you need to understand why nobody hears your ambient horror soundtrack and it turns out you spawned it inside a wall.

---

## Commands

```
audio play <BotID> <file or URL> [--start seconds] [--end seconds]
audio stop <BotID>
audio pause <BotID>
audio resume <BotID>
audio skip <BotID>
audio loop <BotID> <true/false>
audio volume <BotID> <0-100>
audio playervolume <BotID> <PlayerID> <0-100>
audio seek <BotID> <time>
audio crossfade <BotID> <file> <volume>
audio status <BotID>
audio fade <BotID> <volume> <duration>
audio follow <BotID> <player|stop> [interval]
audio add <BotID>
audio remove <BotID>
audio nickname <BotID> <name>
audio voicechannel <BotID> <channel>
audio pitch <BotID> <semitones>
audio enqueue <BotID> <file>
audio queue <BotID>
audio folder <BotID> <path>
audio scene activate <name>
audio scene deactivate <name>
audio scene list
audio speaker spawn
audio speaker list
audio speaker remove <ID>
audio speaker spatial <ID> <true/false>
audio visualize
audio diagnose
audio help
```

`audio help` exists. Use it before opening an issue.

---

## Example Config

```yaml
is_enabled: true
spawn_bot: true
special_events_enable: true
enable_audio_zones: true
enable_cassie_ducking: true

bots_list:
- bot_name: "EviAudio Bot"
  bot_id: 99
  badge_text: "Music Bot"
  badge_color: "blue"

round_start_grace_delay: 5
suppress_cassie_mtf_announcement: false

mtf_spawn_enabled: true
chaos_spawn_enabled: true
warhead_enabled: true

cassie_duck_volume: 20
cassie_duck_fade_in: 0.4
cassie_duck_fade_out: 0.8

audio_cache_max_megabytes: 512
max_active_speakers: 64
```

---

## Included Example Presets

### Warhead_Emergency
Looping sirens spawned across LCZ and HCZ simultaneously. One command transforms the entire facility into a Cold War panic simulator. Players will feel something. What exactly depends on your audio files and your relationship with mercy.

### Horror_Basement
Heavy Containment ambient horror loops with pitch shifted downward. For when your players aren't paranoid enough and you've decided to personally fix that.

### SCP_Breach_173
Dedicated spatial ambient for the 173 chamber. Because silence in that room is somehow worse and you wanted to make it worse differently.

---

## Dependencies

* EXILED
* NVorbis
* FFmpeg *(required for mp3 / flac / aac / opus / all streaming)*
* yt-dlp *(required for YouTube / SoundCloud / Twitch / Vimeo)*

---

## Known Issues

* Badly encoded audio files will corrupt playback. Use FFmpeg to convert them. Stop using that website.
* Twitch takes ~20 seconds to resolve. This is Twitch's fault. I am not Twitch.
* YouTube CDN URLs expire after a few hours. This is Google's fault. I am not Google.
* If you spawn 400 looping spatial speakers in HCZ your server will have opinions about that and none of them will be positive. This is not a bug. This is physics.
* Audio chaos caused entirely by your own config is your fault and I will not be accepting support tickets for it.

---

## Disclaimer

This plugin works on my machine. Probably on yours too.

If your audio breaks:
* Check FFmpeg and yt-dlp are in `EXILED/Plugins/EviAudio/ffmpeg/`
* Check file paths
* Check your config
* Stop downloading mp3s from websites operated by a toaster in Belarus

If this plugin causes:
* Server lag
* Audio distortion
* Ghost noises
* Existential dread
* Psychological damage to your playerbase
* A sudden appreciation for Soviet engineering
* Players leaving because the 173 room ambience was too effective
* War crimes

Then congratulations. You configured it. Whether correctly or not is between you and your players.

Open an issue if something actually breaks. I will probably fix it. I might fix it immediately at 2am. I might disappear for a week and come back with two new features nobody asked for. The outcome is genuinely uncertain.

Use it however you want. Fork it. Modify it. Build plugins on top of the public API. Stream the Soviet national anthem at full volume on a Tuesday. Turn it into military propaganda. I don't care.

Just don't DM me because you gave a YouTube URL to 50 spatial speakers simultaneously and your server entered a dimension I can't help you exit.

---

## Author

**EviZi1337**

*"Made because someone had to."*
