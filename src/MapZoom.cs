using UnityEngine;

namespace MapHeim
{
    /// <summary>Zooms the little map in the corner with a pair of keys (or Ctrl and the mouse wheel).</summary>
    internal static class MapZoom
    {
        private const float Step = 1.25f;
        private static float? _vanilla;

        public static float Level => Mathf.Clamp(MapHeimPlugin.Zoom.Value, 0.4f, 6f);

        public static void Tick(Minimap map)
        {
            if (_vanilla == null) _vanilla = map.m_smallZoom;

            if (CanZoom())
            {
                int change = 0;
                if (Input.GetKeyDown(MapHeimPlugin.ZoomInKey.Value)) change++;
                if (Input.GetKeyDown(MapHeimPlugin.ZoomOutKey.Value)) change--;
                if (MapHeimPlugin.ScrollZoom.Value && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    float scroll = Input.mouseScrollDelta.y;
                    if (scroll > 0f) change++;
                    else if (scroll < 0f) change--;
                }

                if (change != 0)
                {
                    MapHeimPlugin.Zoom.Value = Mathf.Clamp(Level * Mathf.Pow(Step, change), 0.4f, 6f);
                    Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, $"Map zoom {Level:0.##}x");
                }
            }

            // The small map shows more the bigger this is, so zooming in makes it smaller.
            if (!Minimap.IsOpen()) map.m_smallZoom = _vanilla.Value / Level;
        }

        private static bool CanZoom()
        {
            if (Minimap.IsOpen() || Console.IsVisible() || Chat.instance != null && Chat.instance.HasFocus()) return false;
            return !(Menu.instance != null && Menu.IsVisible()) && !TextInput.IsVisible();
        }
    }
}
