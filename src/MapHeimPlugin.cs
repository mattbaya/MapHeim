using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MapHeim
{
    [BepInPlugin(Guid, Name, Version)]
    public class MapHeimPlugin : BaseUnityPlugin
    {
        public const string Guid = "mbaya.MapHeim";
        public const string Name = "MapHeim";
        public const string Version = "0.3.2";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> ZoomInKey;
        internal static ConfigEntry<KeyCode> ZoomOutKey;
        internal static ConfigEntry<bool> ScrollZoom;
        internal static ConfigEntry<float> Zoom;
        internal static ConfigEntry<bool> TrailEnabled;
        internal static ConfigEntry<float> TrailSeconds;
        internal static ConfigEntry<Color> TrailColor;
        internal static ConfigEntry<float> TrailWidth;
        internal static ConfigEntry<bool> DungeonEnabled;
        internal static ConfigEntry<float> ViewFeet;
        internal static ConfigEntry<float> ShownMeters;
        internal static ConfigEntry<bool> GpsEnabled;
        internal static ConfigEntry<KeyCode> GpsKey;
        internal static ConfigEntry<float> GpsArriveMeters;
        internal static ConfigEntry<bool> AutoPinsEnabled;
        internal static ConfigEntry<bool> PinRunestones;
        internal static ConfigEntry<bool> PinDungeons;
        internal static ConfigEntry<bool> PinStructures;
        internal static ConfigEntry<bool> PinOre;
        internal static ConfigEntry<float> LocationRadius;
        internal static ConfigEntry<float> DepositRadius;
        internal static ConfigEntry<string> OreKeywords;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            ZoomInKey = Config.Bind("Zoom", "ZoomInKey", KeyCode.RightBracket, "Zooms the minimap in.");
            ZoomOutKey = Config.Bind("Zoom", "ZoomOutKey", KeyCode.LeftBracket, "Zooms the minimap out.");
            ScrollZoom = Config.Bind("Zoom", "CtrlScrollZoom", true, "Hold Ctrl and use the mouse wheel to zoom the minimap.");
            Zoom = Config.Bind("Zoom", "Zoom", 1f, new ConfigDescription("The minimap's zoom, 1 = the game's own. Bigger shows less, closer up. (Set by the keys; saved.)", new AcceptableValueRange<float>(0.4f, 6f)));
            TrailEnabled = Config.Bind("Trail", "ShowTrail", true, "Show a trail of dots behind you on the minimap, where you've just been.");
            TrailSeconds = Config.Bind("Trail", "Seconds", 90f, new ConfigDescription("How long the trail lasts.", new AcceptableValueRange<float>(10f, 600f)));
            TrailColor = Config.Bind("Trail", "Color", new Color(1f, 0.82f, 0.25f), "The trail's colour (the oldest dots fade away).");
            TrailWidth = Config.Bind("Trail", "Width", 2f, new ConfigDescription("How thick the trail line is, in pixels.", new AcceptableValueRange<float>(1f, 8f)));
            DungeonEnabled = Config.Bind("Dungeon", "DungeonMap", true, "Inside a dungeon, the minimap draws a plan of the walls you've seen instead of the world map.");
            ViewFeet = Config.Bind("Dungeon", "ViewFeet", 10f, new ConfigDescription("How far ahead of you the dungeon map reveals, in feet.", new AcceptableValueRange<float>(5f, 40f)));
            ShownMeters = Config.Bind("Dungeon", "ShownMeters", 40f, new ConfigDescription("How many metres across the dungeon map shows at zoom 1.", new AcceptableValueRange<float>(15f, 64f)));

            GpsEnabled = Config.Bind("GPS", "ShowGps", true, "Show the GPS readout (name, distance, arrow and arrival time) for the pin you've chosen.");
            GpsKey = Config.Bind("GPS", "GpsKey", KeyCode.G, "On the big map, hover a pin and press this to make it the GPS target (press it again to clear it).");
            GpsArriveMeters = Config.Bind("GPS", "ArriveMeters", 8f, new ConfigDescription("The GPS switches off when you're this close.", new AcceptableValueRange<float>(2f, 50f)));
            AutoPinsEnabled = Config.Bind("AutoPins", "Enabled", true, "Put things on the map as you find them.");
            PinRunestones = Config.Bind("AutoPins", "Runestones", true, "Rune stones and Vegvisirs, when you get close.");
            PinDungeons = Config.Bind("AutoPins", "Dungeons", true, "Dungeon entrances (crypts, caves and the like), when you get close.");
            PinStructures = Config.Bind("AutoPins", "Structures", true, "Other large structures (ruins, camps, towers...), when you get close.");
            PinOre = Config.Bind("AutoPins", "OreDeposits", true, "Ore deposits, when you hit one or stand close to it.");
            LocationRadius = Config.Bind("AutoPins", "LocationRadius", 40f, new ConfigDescription("How close you must get to a rune stone, dungeon or structure, in metres.", new AcceptableValueRange<float>(10f, 150f)));
            DepositRadius = Config.Bind("AutoPins", "DepositRadius", 6f, new ConfigDescription("How close you must get to an ore deposit, in metres (hitting one always counts).", new AcceptableValueRange<float>(2f, 40f)));
            OreKeywords = Config.Bind("AutoPins", "OreKeywords", "copper,tin,silver,obsidian,meteorite", "Deposits whose name contains one of these are pinned (comma separated).");

            _harmony = new Harmony(Guid);
            SafePatch.All(_harmony, typeof(MapHeimPlugin).Assembly, Log);
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
            TableRepair.Tick();
            var player = Player.m_localPlayer;
            var map = Minimap.instance;
            if (player == null || map == null)
            {
                Trail.Clear();
                DungeonMap.Hide();
                return;
            }

            MapZoom.Tick(map);
            DungeonMap.Tick(player, map);
            Trail.Tick(player, map);
            Gps.Tick(player, map);
            AutoPins.Tick(player, map);
        }

        private void OnGUI() => Gps.Draw();

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
