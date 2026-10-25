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
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyCode> ZoomInKey;
        internal static ConfigEntry<KeyCode> ZoomOutKey;
        internal static ConfigEntry<bool> ScrollZoom;
        internal static ConfigEntry<float> Zoom;
        internal static ConfigEntry<bool> TrailEnabled;
        internal static ConfigEntry<float> TrailSeconds;
        internal static ConfigEntry<Color> TrailColor;
        internal static ConfigEntry<bool> DungeonEnabled;
        internal static ConfigEntry<float> ViewFeet;
        internal static ConfigEntry<float> ShownMeters;

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
            DungeonEnabled = Config.Bind("Dungeon", "DungeonMap", true, "Inside a dungeon, the minimap draws a plan of the walls you've seen instead of the world map.");
            ViewFeet = Config.Bind("Dungeon", "ViewFeet", 10f, new ConfigDescription("How far ahead of you the dungeon map reveals, in feet.", new AcceptableValueRange<float>(5f, 40f)));
            ShownMeters = Config.Bind("Dungeon", "ShownMeters", 40f, new ConfigDescription("How many metres across the dungeon map shows at zoom 1.", new AcceptableValueRange<float>(15f, 64f)));

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(MapHeimPlugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
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
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
