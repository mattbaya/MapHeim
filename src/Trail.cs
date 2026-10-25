using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapHeim
{
    /// <summary>A trail of fading dots behind the player on the small map: where you've been for the last minute and a half.</summary>
    internal static class Trail
    {
        private const float SampleSeconds = 0.5f;
        private const float MinMove = 0.75f;

        private struct Point { public Vector3 Position; public float Time; }

        private static readonly List<Point> Points = new List<Point>();
        private static readonly List<Image> Dots = new List<Image>();
        private static RectTransform _holder;
        private static Sprite _dot;
        private static float _nextSample;

        public static void Clear()
        {
            Points.Clear();
            foreach (var dot in Dots)
                if (dot != null) dot.gameObject.SetActive(false);
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

            var area = Area(map);
            if (area == null)
            {
                Clear();
                return;
            }

            EnsureHolder(area);
            // How many pixels a metre is on the map as it's drawn now.
            float metersAcross = DungeonMap.Showing ? DungeonMap.MetersAcross : map.m_mapImageSmall.uvRect.width * map.m_textureSize * map.m_pixelSize;
            float pixelsPerMeter = area.rect.width / Mathf.Max(0.01f, metersAcross);
            var origin = player.transform.position;
            var color = MapHeimPlugin.TrailColor.Value;
            float halfW = area.rect.width / 2f, halfH = area.rect.height / 2f;

            while (Dots.Count < Points.Count) Dots.Add(MakeDot());
            for (int i = 0; i < Dots.Count; i++)
            {
                var dot = Dots[i];
                if (dot == null) continue;
                if (i >= Points.Count) { dot.gameObject.SetActive(false); continue; }

                var offset = (Points[i].Position - origin) * pixelsPerMeter;
                bool inside = Mathf.Abs(offset.x) < halfW - 3f && Mathf.Abs(offset.z) < halfH - 3f;
                dot.gameObject.SetActive(inside);
                if (!inside) continue;

                float age = (now - Points[i].Time) / lifetime;
                dot.rectTransform.anchoredPosition = new Vector2(offset.x, offset.z);
                dot.color = new Color(color.r, color.g, color.b, Mathf.Clamp01(1f - age) * 0.9f);
            }
        }

        private static RectTransform Area(Minimap map) => map.m_mapImageSmall != null ? map.m_mapImageSmall.rectTransform : null;

        private static void EnsureHolder(RectTransform area)
        {
            if (_holder != null && _holder.parent == area) return;
            foreach (var dot in Dots) if (dot != null) Object.Destroy(dot.gameObject);
            Dots.Clear();

            var go = new GameObject("MapHeimTrail", typeof(RectTransform));
            _holder = (RectTransform)go.transform;
            _holder.SetParent(area, false);
            _holder.anchorMin = _holder.anchorMax = new Vector2(0.5f, 0.5f);
            _holder.sizeDelta = Vector2.zero;
        }

        private static Image MakeDot()
        {
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_holder, false);
            var image = go.GetComponent<Image>();
            image.sprite = DotSprite();
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(7f, 7f);
            return image;
        }

        private static Sprite DotSprite()
        {
            if (_dot != null) return _dot;
            const int size = 16;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f, dy = y + 0.5f - size / 2f;
                    float edge = Mathf.Clamp01(size / 2f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(edge * 255f));
                }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "MapHeimDot", filterMode = FilterMode.Bilinear };
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, true);
            texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            _dot = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            _dot.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return _dot;
        }
    }
}
