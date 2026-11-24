<div align="center">

# 2xFlaw's Chest Counter

[![release](https://img.shields.io/github/v/release/2xFlaw/ChestCounter?style=flat-square&label=version&color=ff66b2)](https://github.com/2xFlaw/ChestCounter/releases/latest) [![downloads](https://img.shields.io/github/downloads/2xFlaw/ChestCounter/total?style=flat-square&label=downloads&color=ff66b2)](https://github.com/2xFlaw/ChestCounter/releases) ![Windows 10 | 11](https://img.shields.io/badge/Windows-10%20%7C%2011-3a7bd5?style=flat-square) [![license](https://img.shields.io/github/license/2xFlaw/ChestCounter?style=flat-square&label=license&color=555)](LICENSE)

**Counts every chest you open in Fortnite, match after match, and tells you how lucky your week has been.**

[**Download the latest release**](https://github.com/2xFlaw/ChestCounter/releases/latest) · Windows 10 / 11 · free · no install · 10 languages

</div>

---

ChestCounter reads the replay files Fortnite saves for every match and keeps count of the chests you open, the treasure hunt treasures and, if you want, your teammates' chests. Everything is stored on your PC, so your history survives even after Fortnite deletes old replays.

## Features

- **Main window**: total, last match, today, best day, average, best match, treasures, a 7-day chart and your team.
- **Weekly chances**: the cumulative chance, since the weekly reset, of having found each weekly prize, with a goal card, a Treasure Hour planner and the Treasure Hour schedule.
- **Weekly report**: one self-contained web page with your goal, the odds table, the planner, this week's chests and your matches.
- **Report** in your browser with tabs for overview, chests, treasures, matches and teammates.
- **Overlay** on top of the game (green while a match is tracked, red in the lobby) and a local **OBS page**.
- **Teammates**: who you play with, wins together and each one's chests per match.
- **Online (optional)**: link your PC to the website to see your stats there, join leaderboards and compete with friends. It is off until you turn it on.

> It counts the chests **you** opened, and the ones your team was seen opening. It does not show where chests are on the map.

## Getting started

1. Download `ChestCounter.exe` from the [latest release](https://github.com/2xFlaw/ChestCounter/releases/latest). That one file is all you need.
2. In Fortnite turn on **Settings > Game > Replays > Record Replays**, and Large Team Modes if you play them. Without replays there is nothing to read.
3. Run `ChestCounter.exe`. Windows may show "Windows protected your PC" because the exe is not signed: choose **More info > Run anyway**.
4. If you already have replays on your PC, the welcome screen lets you count them or start with your next matches only.
5. Leave the window open (minimised is fine) while you play. The counter reads the replay being recorded and adds chests live, then confirms them when the match ends.

> **Tip:** matches played while the counter is closed are counted the next time you open it, as long as Fortnite has not deleted those replays. Turn on **Open with Fortnite** in Settings so you never have to think about it.

## Weekly chances

The weekly reset is every **Sunday at 14:00 GMT**. For every chest you open after it, each prize has a 1-in-N chance:

| Prize | Chance per chest |
|---|---|
| Weekly Gold Outfit | 1 in 1,562,500 |
| Pirate Shop Access | 1 in 137,500 |
| Weekly Vehicle & Decal | 1 in 7,813 |
| Weekly Pickaxe or Back Bling | 1 in 568 |
| Weekly Jam Track | 1 in 391 |
| Weekly Loading Screen | 1 in 272 |
| Gizmo (any) | 1 in 127 |

Chests opened during a **Treasure Hour** use the boosted rate. The percentage shown is the chance of at least one drop so far, `1 - product(1 - rate)`, so it only rises when you open chests. It is a probability, not a promise. Prizes you tick as owned in Weekly rewards are hidden, except Gizmos, which can drop again.

Treasure Hours come from your own `2xflaw-odds.json`. Hours whose boosts have not been announced count as normal odds until you confirm them.

## Overlay and OBS

Set Fortnite to **Windowed Fullscreen** to see the overlay on top of the game. It starts off: turn it on with the **Overlay** button or **Ctrl+Shift+O**.

| Shortcut | What it does |
|---|---|
| Ctrl+Shift+O | Show or hide the overlay |
| Ctrl+Shift+L | Lock or unlock it (locked = click-through) |
| Ctrl+Shift+K | Compact mode |
| Right-click on the overlay | Hide it |

For streaming, the same overlay is served at `http://localhost:7373/` while the counter is open. Add it in OBS as a browser source.

## Online sync (optional)

The website is **chestcounter.io**. Nothing is uploaded unless you choose to:

1. On the website, open **My stats** and press **Get link code**.
2. In the app, open **Settings > Online connection**, type the code and link the PC.
3. Pick a public username if you want to appear on the leaderboard.

Only the match id, start time, game mode, chest counts, chest types and chest times are uploaded. Never your name, Epic account id, teammates or replay files. By default only matches played after linking are sent. You can unlink at any time, switch private mode on, or delete your data from the website.

## Settings

- **Language**: Windows language by default, with ten to choose from.
- **Open with Fortnite**: opens the counter minimised when Fortnite starts and closes it 30 seconds after Fortnite closes. It only looks at the list of running programs.
- **Teammates in the report** and **Add their chests to your total** (both optional).
- **Also count as a chest**: ammo boxes, food boxes and loot cannons. Chests you destroy always count.
- **Chests before the counter**: an estimate added to your total.
- **Updates**, **overlay**, **replays folder** and **Reset statistics** (with a backup).

## Is it safe?

ChestCounter only reads the `.replay` files in `%LOCALAPPDATA%\FortniteGame\Saved\Demos`, read-only.

- It never modifies Fortnite or its installation folder.
- It never reads, writes or injects anything into the game's memory.
- It needs no login and no Epic account, and sends no telemetry.
- Network use: checking GitHub for new versions (can be turned off), and the optional online sync above.
- It runs at low priority and sleeps between matches.

## Your data

Everything is saved in `%APPDATA%\ChestCounter`.

| File | Contents |
|---|---|
| `data.json` | Your whole match history. Back it up if you change PC. |
| `config.json` | Settings, profile, replays folder and overlay position. |
| `report.html` and `2xflaw-report.html` | The report and the weekly report. |
| `matches.csv` | One line per match, for Excel or Google Sheets. |
| `2xflaw-ledger.json` | Manually added chests. |
| `2xflaw-odds.json` | Treasure Hour schedule used by Weekly chances. |
| `2xflaw-online.json` | Online sync settings (off by default). |
| `treasures.json` | Names of treasure hunt prizes. |
| `errors.log` | Replays that could not be read, for bug reports. |

## FAQ

**Nothing is counted.** Check that replays are on in Fortnite. The window tells you if the replays folder is missing or empty.

**Why are my teammates' chests low?** Fortnite only records what happens within about 1,087 m of you, so teammate counts are a minimum, not a total.

**How do I start over?** Settings > Reset statistics. It asks first and saves a backup.

**Creative modes?** Only Battle Royale matches are counted.

**"Cannot read this replay".** A Fortnite update may have changed the format. Matches already counted are kept. Open an [issue](https://github.com/2xFlaw/ChestCounter/issues) and attach `errors.log`.

## Command line options

```
ChestCounter.exe --demos <folder>   another replays folder
ChestCounter.exe --data <folder>    another data folder
ChestCounter.exe --once             analyse finished replays and quit
ChestCounter.exe --reset-config     run the first-time setup again
```

## Building from source

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (Windows x64) and double-click `build.bat`. The exe is written to `dist\ChestCounter.exe`.

The website lives in [`web/`](web/README.md): Cloudflare Workers, D1 and static pages.

## License and credits

[MIT](LICENSE). Replay parsing is based on [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) by Shiqan (MIT), lightly modified: see [`lib/PATCHES.md`](lib/PATCHES.md).

The colour emoji in the app come from [Noto Emoji](https://github.com/googlefonts/noto-emoji) by Google ([Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)).

Not affiliated with Epic Games, content is not official or endorsed by Epic Games in any way.

Made by 2xFlaw.
