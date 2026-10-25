# MapHeim

**Three upgrades for the little map in the corner: a plan of the dungeon you're in, zoom, and a trail showing where you've just been.**

## Contents

- [Dungeon map](#dungeon-map)
- [Zoom](#zoom)
- [Trail](#trail)
- [Configuration](#configuration)
- [Troubleshooting](#troubleshooting)

## Dungeon map

Inside a crypt, cave or other dungeon the world map is no use to you, so the corner map switches to a **plan of the walls you've seen**. It starts blank and grows as you explore: only what you can see **about 10 feet ahead of you** (and a small circle around you) is drawn, never the whole dungeon. Walls are pale, the floor you've crossed is darker, and anything you haven't seen is left dark. The plan is kept while you stay in the dungeon, so if you backtrack you still see where you've been.

It looks at the real walls and doors around you, so it works in every dungeon, and it goes away when you leave. Switch it off with `Dungeon / DungeonMap`.

## Zoom

Zoom the corner map with **`]`** (in) and **`[`** (out), or hold **Ctrl** and use the mouse wheel. It works on the world map and on the dungeon plan, and your zoom is remembered. Change the keys, or turn off the wheel, in the config.

## Trail

A trail of fading gold dots shows where you've been for the last **90 seconds**: the newest dots are bright and the oldest fade away. It works on the world map and in dungeons. Change how long it lasts and its colour in the config, or turn it off with `Trail / ShowTrail`.

## Configuration

`BepInEx/config/mbaya.MapHeim.cfg`, or Gale's config editor:

| Section | Key | Default | Notes |
| --- | --- | --- | --- |
| Zoom | ZoomInKey / ZoomOutKey | `]` / `[` | |
| Zoom | CtrlScrollZoom | true | Ctrl and the mouse wheel zoom the map |
| Zoom | Zoom | 1 | The current zoom (saved) |
| Trail | ShowTrail | true | |
| Trail | Seconds | 90 | How long the trail lasts |
| Trail | Color | gold | |
| Dungeon | DungeonMap | true | |
| Dungeon | ViewFeet | 10 | How far ahead the plan reveals |
| Dungeon | ShownMeters | 40 | Metres across the plan at zoom 1 |

## Troubleshooting

- **The dungeon plan is blank.** It only draws what it has seen: walk around. If nothing appears at all, tell me which dungeon and attach `BepInEx/LogOutput.log`.
- **The trail is in the wrong place.** Tell me your screen resolution and the game's GUI scale, with a screenshot.
- **The keys do nothing.** Another mod may use `[` and `]`: change `ZoomInKey` and `ZoomOutKey`.
- **Something else.** Open an issue at [github.com/mattbaya/MapHeim](https://github.com/mattbaya/MapHeim) and attach `BepInEx/LogOutput.log`.

## Works with

- **[PingHeim](https://thunderstore.io/c/valheim/p/Svaha/PingHeim/)**: ping marks show on the same map.
