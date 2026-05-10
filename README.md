# EviAudio

An EXILED plugin that turns SCP:SL into a schizophrenic radio station with spatial audio, music bots, ambient zones, event sounds, scene presets and enough noise pollution to make CASSIE file a lawsuit.
Made because normal silent servers feel like abandoned parking garages.

Needs EXILED. Lol. And NVorbis.

## What does this thing even do

This plugin lets you:

* Spawn audio bots that blast music through voice chat
* Create full 3D spatial audio zones around the map
* Trigger sounds when players enter rooms
* Play music during lobby, round start, MTF spawns, Chaos spawns, warhead events, deaths, joins, whatever your unstable brain comes up with
* Build reusable audio scenes with multiple speakers across the facility
* Make HCZ sound like a horror movie basement made by sleep deprived Soviet engineers

Basically if you ever thought:

"what if SCP:SL but the facility had dynamic sound design and psychological warfare"

yeah, that's what this thing does.

## Features

### Audio Bots

Bots can be spawned automatically or manually.

They:

* Play audio files through voice chat
* Can use different IDs
* Support multiple channels
* Work as trigger broadcasters
* Exist purely to violate silence

Default auto-spawn bot:

| Property    | Default      |
| ----------- | ------------ |
| Name        | EviAudio Bot |
| Bot ID      | 99           |
| Badge       | Music Bot    |
| Badge Color | blue         |

Tracks go here:

```text
EXILED/Plugins/EviAudio/tracks/
```

Supported formats depend on your audio pipeline, but just use `.ogg` unless you enjoy suffering.

---

### Event Audio System

The plugin can automatically play sounds during:

* Lobby
* Round start
* Round end
* MTF spawn
* Chaos spawn
* Warhead start
* Warhead stop
* Player death
* Player kill
* Player join

You can randomize clips because hearing the same sound 400 times is how people become clinically insane.

Also:

* Optional suppression of default MTF CASSIE
* Grace delay after round start to stop sound overlap disasters
* Private sounds for specific players

Yes, you can absolutely make every death play a Vine boom effect. Nobody can stop you.

---

### Audio Presets / Scenes

Scenes are collections of spatial speakers.

Example use cases:

* Warhead sirens in every zone
* SCP themed ambient rooms
* Horror basement ambience
* Dynamic raid music
* Fake reactor meltdowns
* Stalker underground vibes
* Psychological warfare against your playerbase

Commands:

```text
audio scene activate <PresetName>
audio scene deactivate <PresetName>
audio scene list
```

Each speaker supports:

| Setting       | What it does                |
| ------------- | --------------------------- |
| file          | Audio file                  |
| volume        | 0-100 volume                |
| loop          | Infinite suffering          |
| min_distance  | Close hearing range         |
| max_distance  | Max audible range           |
| lifetime      | Auto destroy timer          |
| pitch_shift   | Voice corruption slider     |
| spawn_in_room | Spawn in specific room      |
| spawn_in_zone | Spawn in every room of zone |
| position      | Manual coordinates          |

You can literally turn Heavy Containment into a live horror soundtrack.

---

### Audio Zones

Per-room sound systems.

Two types:

#### Ambient Audio

Real 3D spatial looping audio at room center.

Example:

* HCZ industrial hum
* SCP breathing noises
* Reactor ambience
* Sirens
* Wind
* Soviet bunker vibes

#### Trigger Audio

Private sounds that play when entering rooms.

Example:

* SCP screams entering 173
* Voice lines
* Warnings
* Music stingers
* Whispering in dark hallways because apparently therapy is expensive

Supports:

* Once per round triggers
* Per-room targeting
* Zone targeting
* Custom bot IDs
* Custom channels
* Looping audio

---

### Cassie Ducking

Optional automatic volume ducking while CASSIE talks.

Meaning:

* Music lowers itself during announcements
* Players can actually hear important information
* Your server becomes 3% less unbearable

Miracles do happen.

## Commands

Default command aliases:

```text
audio
au
```

Exact command list depends on your build but includes scene management, bot spawning and audio controls.

If you somehow manage to forget the commands after typing `audio help`, that's on you.

## Installation

1. Grab the `.dll`
2. Drop it into:

```text
EXILED/Plugins/
```

3. Start the server
4. Plugin auto-generates configs and folders
5. Put your audio files inside:

```text
EXILED/Plugins/EviAudio/tracks/
```

6. Restart server
7. Congratulations, your SCP server is now an audio engineering experiment

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

lobby_playlist:
- file: "lobby.ogg"
  volume: 60
  loop: true

round_start_clip:
- file: "roundstart.ogg"
  volume: 80

round_end_clip:
- file: "roundend.ogg"
  volume: 80
```

## Included Example Presets

### Warhead_Emergency

Spawns looping sirens across containment zones.

Basically transforms the facility into a Cold War panic simulator.

### Horror_Basement

Heavy Containment ambient horror loops with pitch shifting.

For when your players aren't paranoid enough.

### SCP_Breach_173

Dedicated ambient sound system for SCP-173 chamber.

Because silence in that room is somehow worse.

## Dependencies

* entity["company","EXILED","EXILED Framework"]
* Unity audio not exploding
* Your players tolerating the noise

## Known Issues

* Badly encoded audio files can explode playback
* Extremely large scenes can become performance terrorism
* If you put 900 speakers in HCZ your server deserves what happens next
* Audio chaos caused by your config is your fault, not mine

## Disclaimer

This plugin works on my machine. Maybe on yours too. Maybe your server immediately starts sounding like a haunted shopping mall from 2007. I genuinely do not know.

If your audio breaks:

* check file paths
* check formats
* check your configs
* stop downloading random mp3s from shady websites run by a toaster in Belarus

If the plugin causes:

* lag
* audio distortion
* ghost noises
* existential dread
* psychological damage
* war crimes.

then congratulations, you configured it wrong.

Open an issue if something actually breaks. I might fix it. I might disappear for six months. Nobody knows.

Use it however you want, modify it, fork it, rewrite it, turn it into a military propaganda, I don't care.

Just don't DM me because you spawned 400 looping sirens and your server entered the shadow realm.

## Author

**EviZi1337**
