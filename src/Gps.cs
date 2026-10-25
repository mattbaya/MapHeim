using System.Collections.Generic;
using UnityEngine;

namespace MapHeim
{
    /// <summary>
    /// A GPS: pick a pin on the big map (hover it and press the GPS key) and a readout shows its name, how far away it is, an arrow
    /// pointing to it and when you'll get there, worked out from how fast you've actually been going.
    /// </summary>
    internal static class Gps
    {
        private const float WindowSeconds = 20f;
        private static readonly List<(float time, Vector3 position)> Samples = new List<(float, Vector3)>();
        private static bool _active;
        private static Vector3 _target;
        private static string _name = "";
        private static float _nextSample;
        private static GUIStyle _style;
        private static Texture2D _arrow;

        public static bool Active => _active;

        public static void Clear() => _active = false;

        public static void Set(Vector3 position, string name)
        {
            _active = true;
            _target = position;
            _name = string.IsNullOrWhiteSpace(name) ? "Marker" : name;
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, "GPS set: " + _name);
        }

        public static void Tick(Player player, Minimap map)
        {
            float now = Time.time;
            if (now >= _nextSample)
            {
                _nextSample = now + 0.5f;
                Samples.Add((now, player.transform.position));
                while (Samples.Count > 0 && now - Samples[0].time > WindowSeconds) Samples.RemoveAt(0);
            }

            // Hover a pin on the big map and press the key: that's the target (the same pin again clears it).
            if (Minimap.IsOpen() && Input.GetKeyDown(MapHeimPlugin.GpsKey.Value))
            {
                var pin = map.GetClosestPin(map.ScreenToWorldPoint(ZInput.pointerPosition), map.PinInteractRadius);
                if (pin != null)
                {
                    if (_active && (pin.m_pos - _target).sqrMagnitude < 4f) { _active = false; player.Message(MessageHud.MessageType.TopLeft, "GPS off"); }
                    else Set(pin.m_pos, pin.m_name);
                }
                else if (_active)
                {
                    _active = false;
                    player.Message(MessageHud.MessageType.TopLeft, "GPS off");
                }
            }

            if (_active && Distance(player.transform.position) < MapHeimPlugin.GpsArriveMeters.Value)
            {
                _active = false;
                player.Message(MessageHud.MessageType.Center, "Arrived: " + _name);
            }
        }

        private static float Distance(Vector3 from)
        {
            var d = _target - from;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>Metres per second over the last stretch (0 if you've barely moved).</summary>
        private static float Speed()
        {
            if (Samples.Count < 2) return 0f;
            float travelled = 0f;
            for (int i = 1; i < Samples.Count; i++)
            {
                var d = Samples[i].position - Samples[i - 1].position;
                d.y = 0f;
                travelled += d.magnitude;
            }

            float seconds = Samples[Samples.Count - 1].time - Samples[0].time;
            return seconds > 1f ? travelled / seconds : 0f;
        }

        public static void Draw()
        {
            var player = Player.m_localPlayer;
            if (!_active || player == null || !MapHeimPlugin.GpsEnabled.Value || Hud.instance == null || !Hud.instance.m_rootObject.activeSelf) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.8f, 2.5f);
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, richText = true };
            _style.fontSize = Mathf.RoundToInt(20f * scale);

            float distance = Distance(player.transform.position);
            float speed = Speed();
            string eta = speed > 0.4f ? Format(distance / speed) : "--";
            string far = distance >= 1000f ? $"{distance / 1000f:0.0} km" : $"{Mathf.RoundToInt(distance)} m";

            // The arrow turns to point at the target, relative to where the camera looks.
            var toTarget = _target - player.transform.position;
            float bearing = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            var camera = GameCamera.instance != null ? GameCamera.instance.transform : null;
            float facing = camera != null ? camera.eulerAngles.y : player.transform.eulerAngles.y;
            float angle = bearing - facing;

            float size = 46f * scale;
            var arrowBox = new Rect(Screen.width / 2f - 230f * scale, 12f * scale, size, size);
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, arrowBox.center);
            GUI.color = new Color(1f, 0.85f, 0.3f);
            GUI.DrawTexture(arrowBox, Arrow());
            GUI.color = Color.white;
            GUI.matrix = old;

            GUI.Label(new Rect(arrowBox.xMax + 10f * scale, arrowBox.y, 420f * scale, size),
                $"<color=#ffd966>{_name}</color>   {far}   ETA {eta}", _style);
        }

        private static string Format(float seconds)
        {
            int total = Mathf.RoundToInt(Mathf.Min(seconds, 359999f));
            return total >= 3600 ? $"{total / 3600}:{total % 3600 / 60:00}:{total % 60:00}" : $"{total / 60}:{total % 60:00}";
        }

        private static Texture2D Arrow()
        {
            if (_arrow != null) return _arrow;
            const int n = 64;
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // An arrowhead pointing up, with a dark outline.
                    float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                    float half = Mathf.Lerp(0.0f, 0.34f, 1f - Mathf.Clamp01((v - 0.12f) / 0.8f));
                    bool inside = v > 0.12f && v < 0.92f && Mathf.Abs(u - 0.5f) < half && !(v < 0.34f && Mathf.Abs(u - 0.5f) < (0.34f - v) * 0.6f);
                    pixels[y * n + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }

            _arrow = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "MapHeimArrow", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            _arrow.SetPixelData(pixels, 0);
            _arrow.Apply(false, true);
            _arrow.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return _arrow;
        }
    }
}
