# Changelog

## 0.3.1
- **Fixed: the trail wasn't visible.** It's now made of small map pins that fade as they age (90 seconds by default), so it shows on both the corner map and the big map and follows the map exactly. Inside a dungeon it's still drawn on the dungeon plan.

## 0.3.0
- **Fixed: players being dropped when they join.** 0.2.0 and 0.2.1 wrote a whole uncompressed map (about 4 MB) into cartographer's tables. A table that big can't be sent to a joining player, so the connection fails after about 45 seconds. **Whoever hosts the world now empties any table holding that much data** (the log says `A cartographer's table ... Emptying it`). The map stored on that table is lost; your own map is not touched.
- **Removed: cartographer's table auto-sync.** It was the cause. It may come back later, done safely. Everyone playing in the world should update to 0.3.0, because an older version can fill a table again.

## 0.2.1
- Each patch is now applied on its own, so one the game has changed can't stop the others (the log names the one that failed).

## 0.2.0
- **GPS.** On the big map, hover a pin and press **G**: a readout at the top of the screen shows its name, the distance, an arrow pointing to it and your estimated arrival time, worked out from how fast you've really been moving. Press **G** again on it (or on empty map) to clear it. It switches off when you arrive.
- **Auto pins.** Rune stones, dungeon entrances and other large structures are put on your map when you get within 40 m of them, and ore deposits (copper, tin, silver, obsidian, meteorite) when you hit one or stand within 6 m. Nothing is added twice, and each kind can be switched off.
- **Cartographer's table sync.** Within 25 m of a cartographer's table your map and the table's are merged without pressing E, again every 2 minutes while you stay.

## 0.1.0
- First version: a dungeon map on the corner map, zoom keys (`[` and `]`, or Ctrl and the mouse wheel) and a 90-second trail.
