using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapHeim
{
    /// <summary>
    /// A thin line behind the player on the maps showing where they've been for the last minute and a half, fading toward its old
    /// end. It's drawn on the corner map, on the big map, and on the dungeon plan inside a dungeon.
    /// </summary>
    internal static class Trail
    {
        private const float SampleSeconds = 0.5f;
        private const float MinMove = 0.75f;

        private struct Point { public Vector3 Position; public float Time; }

        /// <summary>A pool of thin rotated rectangles that make up the line on one map.</summary>
        private class Layer
        {
            public RectTransform Parent, Holder;
            public readonly List<Image> Segments = new List<Image>();

            public void Attach(RectTransform parent)
            {
                if (Parent == parent && Holder != null) return;
                foreach (var s in Segments) if (s != null) Object.Destroy(s.gameObject);
                Segments.Clear();
                if (Holder != null) Object.Destroy(Holder.gameObject);

                Parent = parent;
                var go = new GameObject("MapHeimTrail", typeof(RectTransform));
                Holder = (RectTransform)go.transform;
                Holder.SetParent(parent, false);
                Holder.anchorMin = Holder.anchorMax = new Vector2(0.5f, 0.5f);
                Holder.sizeDelta = Vector2.zero;
            }

            public void Hide()
            {
                foreach (var s in Segments)
                    if (s != null && s.gameObject.activeSelf) s.gameObject.SetActive(false);
            }

            /// <summary>Draws a line through these points (in this layer's own pixels), each with its own opacity.</summary>
            public void Draw(List<Vector2> points, List<float> alphas, float width, Color color)
            {
                int needed = Mathf.Max(0, points.Count - 1);
                while (Segments.Count < needed) Segments.Add(MakeSegment());
                for (int i = 0; i < Segments.Count; i++)
                {
                    var segment = Segments[i];
                    if (segment == null) continue;
                    if (i >= needed) { if (segment.gameObject.activeSelf) segment.gameObject.SetActive(false); continue; }

                    var a = points[i];
                    var b = points[i + 1];
                    var d = b - a;
                    float length = d.magnitude;
                    if (length < 0.01f) { segment.gameObject.SetActive(false); continue; }

                    segment.gameObject.SetActive(true);
                    var rect = segment.rectTransform;
                    rect.anchoredPosition = (a + b) / 2f;
                    rect.sizeDelta = new Vector2(length + 0.6f, width);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    segment.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(alphas[i + 1]));
                }
            }

            private Image MakeSegment()
            {
                var go = new GameObject("Segment", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(Holder, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                return image;
            }
        }

        private static readonly List<Point> Points = new List<Point>();
        private static readonly Layer Small = new Layer();
        private static readonly Layer Large = new Layer();
        private static readonly List<Vector2> Local = new List<Vector2>();
        private static readonly List<float> Alpha = new List<float>();
        private static float _nextSample;

        public static void Clear()
        {
            Points.Clear();
            Small.Hide();
            Large.Hide();
        }

        public static void Tick(Player player, Minimap map)
        {
            if (!MapHeimPlugin.TrailEnabled.Value)
            {
                Clear();
                return;
            }

            float now = Time.time;
            if (now >= _nextSample)
            {
                _nextSample = now + SampleSeconds;
                var position = player.transform.position;
                if (Points.Count == 0 || (Points[Points.Count - 1].Position - position).sqrMagnitude > MinMove * MinMove)
                    Points.Add(new Point { Position = position, Time = now });
            }

            float lifetime = MapHeimPlugin.TrailSeconds.Value;
            while (Points.Count > 0 && now - Points[0].Time > lifetime) Points.RemoveAt(0);

            float width = MapHeimPlugin.TrailWidth.Value;
            var color = MapHeimPlugin.TrailColor.Value;

            if (Minimap.IsOpen() && map.m_mapImageLarge != null)
            {
                Small.Hide();
                Large.Attach(map.m_mapImageLarge.rectTransform);
                DrawLarge(map, now, lifetime, width, color);
            }
            else if (map.m_mapImageSmall != null)
            {
                Large.Hide();
                Small.Attach(map.m_mapImageSmall.rectTransform);
                DrawSmall(map, player, now, lifetime, width, color);
            }
        }

        private static void Fill(float now, float lifetime)
        {
            Alpha.Clear();
            foreach (var p in Points) Alpha.Add(Mathf.Clamp01(1f - (now - p.Time) / lifetime) * 0.9f);
        }

        /// <summary>The corner map is centred on the player; the dungeon plan uses the same area at its own scale.</summary>
        private static void DrawSmall(Minimap map, Player player, float now, float lifetime, float width, Color color)
        {
            var area = Small.Parent;
            float metersAcross = DungeonMap.Showing ? DungeonMap.MetersAcross : map.m_mapImageSmall.uvRect.width * map.m_textureSize * map.m_pixelSize;
            float pixelsPerMeter = area.rect.width / Mathf.Max(0.01f, metersAcross);
            var origin = player.transform.position;

            Local.Clear();
            foreach (var p in Points)
            {
                var offset = (p.Position - origin) * pixelsPerMeter;
                Local.Add(new Vector2(offset.x, offset.z));
            }

            Fill(now, lifetime);
            Small.Draw(Local, Alpha, width, color);
        }

        /// <summary>
        /// The big map can be dragged and zoomed, so work out where world points land on it from the game's own screen-to-world
        /// conversion (sampled at three points, which is enough to know the scale and offset).
        /// </summary>
        private static void DrawLarge(Minimap map, float now, float lifetime, float width, Color color)
        {
            var area = Large.Parent;
            var centre = RectTransformUtility.WorldToScreenPoint(null, area.position);
            var w0 = map.ScreenToWorldPoint(new Vector3(centre.x, centre.y, 0f));
            var wx = map.ScreenToWorldPoint(new Vector3(centre.x + 100f, centre.y, 0f));
            var wy = map.ScreenToWorldPoint(new Vector3(centre.x, centre.y + 100f, 0f));
            float metersPerPixelX = (wx.x - w0.x) / 100f;
            float metersPerPixelY = (wy.z - w0.z) / 100f;
            if (Mathf.Abs(metersPerPixelX) < 1e-5f || Mathf.Abs(metersPerPixelY) < 1e-5f)
            {
                Large.Hide();
                return;
            }

            Local.Clear();
            foreach (var p in Points)
            {
                var screen = new Vector2(centre.x + (p.Position.x - w0.x) / metersPerPixelX, centre.y + (p.Position.z - w0.z) / metersPerPixelY);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, null, out var local);
                Local.Add(local);
            }

            Fill(now, lifetime);
            Large.Draw(Local, Alpha, width, color);
        }
    }
}
