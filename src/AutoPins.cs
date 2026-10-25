using System.Collections.Generic;
using UnityEngine;

namespace MapHeim
{
    /// <summary>
    /// Puts things on the map as you find them: rune stones, dungeons and large structures when you walk close to them, and ore
    /// deposits when you hit them or stand next to them. Pins are saved with the map, and nothing is added twice.
    /// </summary>
    internal static class AutoPins
    {
        private enum Category { Runestone, Dungeon, Structure }

        private static float _nextScan;

        private static Category Classify(string prefab)
        {
            string n = prefab.ToLowerInvariant();
            if (n.Contains("runestone") || n.Contains("vegvisir")) return Category.Runestone;
            if (n.Contains("crypt") || n.Contains("cave") || n.Contains("dungeon") || n.Contains("burialchamber") || n.Contains("grave") || n.Contains("forge") && n.Contains("cave"))
                return Category.Dungeon;
            return Category.Structure;
        }

        private static string PrettyName(string prefab)
        {
            // "SunkenCrypt4" -> "Sunken Crypt", "Runestone_Meadows" -> "Runestone"
            string n = System.Text.RegularExpressions.Regex.Replace(prefab, "[0-9_]+$", "");
            n = System.Text.RegularExpressions.Regex.Replace(n, "(?<=[a-z])(?=[A-Z])", " ");
            return n.Replace('_', ' ').Trim();
        }

        public static void Tick(Player player, Minimap map)
        {
            if (!MapHeimPlugin.AutoPinsEnabled.Value || Time.time < _nextScan) return;
            _nextScan = Time.time + 2f;

            var origin = player.transform.position;
            ScanLocations(origin, map);
            ScanDeposits(origin, map);
        }

        private static void ScanLocations(Vector3 origin, Minimap map)
        {
            var zones = ZoneSystem.instance;
            if (zones == null) return;
            float radius = MapHeimPlugin.LocationRadius.Value;
            float radiusSquared = radius * radius;

            foreach (var instance in zones.m_locationInstances.Values)
            {
                if (!instance.m_placed) continue;
                var offset = instance.m_position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSquared) continue;

                string prefab = instance.m_location.m_prefabName;
                var category = Classify(prefab);
                if (category == Category.Runestone && !MapHeimPlugin.PinRunestones.Value) continue;
                if (category == Category.Dungeon && !MapHeimPlugin.PinDungeons.Value) continue;
                if (category == Category.Structure && !MapHeimPlugin.PinStructures.Value) continue;

                var icon = category == Category.Runestone ? Minimap.PinType.Icon3 : category == Category.Dungeon ? Minimap.PinType.Icon4 : Minimap.PinType.Icon1;
                Add(map, instance.m_position, icon, PrettyName(prefab), 30f);
            }
        }

        private static void ScanDeposits(Vector3 origin, Minimap map)
        {
            if (!MapHeimPlugin.PinOre.Value || ZDOMan.instance == null) return;
            float radius = MapHeimPlugin.DepositRadius.Value;
            float radiusSquared = radius * radius;
            var keywords = MapHeimPlugin.OreKeywords.Value.ToLowerInvariant().Split(',');

            foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                var position = zdo.GetPosition();
                float dx = position.x - origin.x, dz = position.z - origin.z;
                if (dx * dx + dz * dz > radiusSquared) continue;

                string name = DepositName(zdo.GetPrefab(), keywords);
                if (name == null) continue;
                Add(map, position, Minimap.PinType.Icon2, name, 12f);
            }
        }

        private static readonly Dictionary<int, string> DepositNames = new Dictionary<int, string>();

        private static string DepositName(int prefabHash, string[] keywords)
        {
            if (DepositNames.TryGetValue(prefabHash, out var cached)) return cached;

            string result = null;
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabHash) : null;
            if (prefab != null && (prefab.GetComponent<MineRock5>() != null || prefab.GetComponent<MineRock>() != null || prefab.GetComponent<Destructible>() != null))
            {
                string lower = prefab.name.ToLowerInvariant();
                foreach (var keyword in keywords)
                {
                    string k = keyword.Trim();
                    if (k.Length > 0 && lower.Contains(k)) { result = char.ToUpperInvariant(k[0]) + k.Substring(1) + " deposit"; break; }
                }
            }

            DepositNames[prefabHash] = result;
            return result;
        }

        /// <summary>A pin only goes down if there isn't already one close by (yours, or one it made earlier).</summary>
        private static void Add(Minimap map, Vector3 position, Minimap.PinType type, string name, float spacing)
        {
            foreach (var pin in map.m_pins)
            {
                var offset = pin.m_pos - position;
                offset.y = 0f;
                if (offset.sqrMagnitude < spacing * spacing && (pin.m_type == type || pin.m_name == name)) return;
            }

            map.AddPin(position, type, name, true, false);
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, "Map: " + name);
        }
    }

    /// <summary>Hitting an ore deposit puts it on the map right away.</summary>
    [HarmonyLib.HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    internal static class HitDepositPatch
    {
        private static void Postfix(MineRock5 __instance, HitData hit)
        {
            if (!MapHeimPlugin.AutoPinsEnabled.Value || !MapHeimPlugin.PinOre.Value || Minimap.instance == null) return;
            if (hit == null || hit.GetAttacker() != Player.m_localPlayer) return;
            AutoPinsHit.Pin(__instance.gameObject, __instance.transform.position);
        }
    }

    internal static class AutoPinsHit
    {
        public static void Pin(GameObject go, Vector3 position)
        {
            string lower = Utils.GetPrefabName(go).ToLowerInvariant();
            foreach (var keyword in MapHeimPlugin.OreKeywords.Value.ToLowerInvariant().Split(','))
            {
                string k = keyword.Trim();
                if (k.Length == 0 || !lower.Contains(k)) continue;
                foreach (var pin in Minimap.instance.m_pins)
                {
                    var offset = pin.m_pos - position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude < 144f && pin.m_type == Minimap.PinType.Icon2) return;
                }

                Minimap.instance.AddPin(position, Minimap.PinType.Icon2, char.ToUpperInvariant(k[0]) + k.Substring(1) + " deposit", true, false);
                return;
            }
        }
    }
}
