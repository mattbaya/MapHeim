using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapHeim
{
    /// <summary>
    /// Inside a dungeon the corner map draws a plan of the walls you've seen. Each tenth of a second a fan of rays goes out in front
    /// of the player; the floor it crosses and the walls it hits are written into a grid, which is drawn as a picture.
    /// Only what's within a few metres ahead is ever revealed, so the plan grows as you explore.
    /// </summary>
    internal static class DungeonMap
    {
        private const float CellSize = 0.5f;
        private const int Pixels = 128;                  // the picture covers Pixels * CellSize metres
        private const float PictureMeters = Pixels * CellSize;
        private const byte Floor = 1, Wall = 2;

        private static readonly Dictionary<long, byte> Cells = new Dictionary<long, byte>();
        private static readonly Color32 Unknown = new Color32(14, 11, 9, 235);
        private static readonly Color32 FloorColor = new Color32(98, 82, 60, 255);
        private static readonly Color32 WallColor = new Color32(232, 214, 168, 255);

        private static RawImage _image;
        private static Texture2D _texture;
        private static Color32[] _buffer;
        private static float _nextScan, _nextDraw;
        private static int _mask;

        public static bool Showing { get; private set; }
        public static float MetersAcross => Mathf.Max(8f, MapHeimPlugin.ShownMeters.Value / MapZoom.Level);

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        private static bool InDungeon(Player player) => player.InInterior();

        public static void Hide()
        {
            Showing = false;
            if (_image != null) _image.gameObject.SetActive(false);
        }

        public static void Tick(Player player, Minimap map)
        {
            if (!MapHeimPlugin.DungeonEnabled.Value || !InDungeon(player) || map.m_mapImageSmall == null)
            {
                if (Showing) RestoreMap(map);
                return;
            }

            if (_mask == 0) _mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
            EnsureImage(map);

            if (Time.time >= _nextScan)
            {
                _nextScan = Time.time + 0.1f;
                Scan(player);
            }

            if (Time.time >= _nextDraw)
            {
                _nextDraw = Time.time + 0.15f;
                Draw(player.transform.position);
            }

            Showing = true;
            _image.gameObject.SetActive(true);
            map.m_mapImageSmall.enabled = false;
            // Keep the picture centred on the player, showing as many metres as the zoom says.
            float size = Mathf.Clamp(MetersAcross / PictureMeters, 0.05f, 1f);
            var position = player.transform.position;
            var centre = CellCentre(position);
            var off = new Vector2(position.x - centre.x, position.z - centre.y) / PictureMeters;
            _image.uvRect = new Rect(0.5f + off.x - size / 2f, 0.5f + off.y - size / 2f, size, size);
        }

        private static void RestoreMap(Minimap map)
        {
            Showing = false;
            if (_image != null) _image.gameObject.SetActive(false);
            if (map.m_mapImageSmall != null) map.m_mapImageSmall.enabled = true;
        }

        private static Vector2 CellCentre(Vector3 position) =>
            new Vector2((Mathf.Floor(position.x / CellSize) + 0.5f) * CellSize, (Mathf.Floor(position.z / CellSize) + 0.5f) * CellSize);

        private static void EnsureImage(Minimap map)
        {
            if (_image != null && _image.transform.parent == map.m_mapImageSmall.transform.parent) return;
            if (_image != null) Object.Destroy(_image.gameObject);

            var source = map.m_mapImageSmall.rectTransform;
            var go = new GameObject("MapHeimDungeon", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(source.parent, false);
            // Exactly where the world map is drawn, just above it (so the player marker stays on top).
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
            rect.SetSiblingIndex(source.GetSiblingIndex() + 1);

            if (_texture == null)
            {
                _texture = new Texture2D(Pixels, Pixels, TextureFormat.RGBA32, false)
                {
                    name = "MapHeimDungeon",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                _texture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                _buffer = new Color32[Pixels * Pixels];
            }

            _image = go.GetComponent<RawImage>();
            _image.texture = _texture;
            _image.raycastTarget = false;
        }

        // ------------------------------------------------------------------ looking

        private static void Scan(Player player)
        {
            var origin = player.transform.position + Vector3.up * 1.1f;
            var look = player.GetLookDir();
            look.y = 0f;
            if (look.sqrMagnitude < 0.01f) look = player.transform.forward;
            look.Normalize();

            float reach = MapHeimPlugin.ViewFeet.Value * 0.3048f;
            // A small circle all round, so there are no holes at your back or sides.
            for (int i = 0; i < 16; i++) Cast(origin, Quaternion.Euler(0f, i * 22.5f, 0f) * Vector3.forward, 1.6f);
            // A fan ahead of you, as far as you can see.
            const int rays = 31;
            for (int i = 0; i < rays; i++)
            {
                float angle = Mathf.Lerp(-65f, 65f, i / (rays - 1f));
                Cast(origin, Quaternion.Euler(0f, angle, 0f) * look, reach);
            }
        }

        private static void Cast(Vector3 origin, Vector3 direction, float reach)
        {
            float length = reach;
            bool hit = false;
            if (Physics.Raycast(origin, direction, out var info, reach, _mask, QueryTriggerInteraction.Ignore))
            {
                length = info.distance;
                hit = true;
            }

            // The floor along the ray.
            for (float d = 0f; d < length - 0.05f; d += CellSize * 0.8f)
            {
                var p = origin + direction * d;
                Mark(p.x, p.z, Floor);
            }

            if (hit)
            {
                // The wall: the cell just inside what the ray struck.
                var p = origin + direction * (length + 0.1f);
                Mark(p.x, p.z, Wall);
            }
        }

        private static void Mark(float x, float z, byte kind)
        {
            long key = Key(Mathf.FloorToInt(x / CellSize), Mathf.FloorToInt(z / CellSize));
            if (Cells.TryGetValue(key, out var current) && current >= kind) return;
            Cells[key] = kind;
        }

        // ------------------------------------------------------------------ drawing

        private static void Draw(Vector3 position)
        {
            int cx = Mathf.FloorToInt(position.x / CellSize) - Pixels / 2;
            int cz = Mathf.FloorToInt(position.z / CellSize) - Pixels / 2;
            for (int y = 0; y < Pixels; y++)
            {
                for (int x = 0; x < Pixels; x++)
                {
                    Cells.TryGetValue(Key(cx + x, cz + y), out var kind);
                    _buffer[y * Pixels + x] = kind == Wall ? WallColor : kind == Floor ? FloorColor : Unknown;
                }
            }

            _texture.SetPixelData(_buffer, 0);
            _texture.Apply(false, false);
        }
    }
}
