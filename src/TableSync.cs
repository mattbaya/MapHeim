using UnityEngine;

namespace MapHeim
{
    /// <summary>
    /// Keeps your map and a nearby cartographer's table in step without pressing E: when you're within range, the table's map is
    /// merged into yours and yours into the table's, again every so often while you stay.
    /// </summary>
    internal static class TableSync
    {
        private static float _nextCheck;
        private static readonly System.Collections.Generic.Dictionary<int, float> LastSync = new System.Collections.Generic.Dictionary<int, float>();

        public static void Tick(Player player, Minimap map)
        {
            if (!MapHeimPlugin.TableSyncEnabled.Value || Time.time < _nextCheck) return;
            _nextCheck = Time.time + 3f;

            float range = MapHeimPlugin.TableRange.Value;
            foreach (var table in Object.FindObjectsOfType<MapTable>())
            {
                if (table.m_nview == null || !table.m_nview.IsValid()) continue;
                if (Vector3.Distance(table.transform.position, player.transform.position) > range) continue;

                int id = table.GetInstanceID();
                if (LastSync.TryGetValue(id, out float last) && Time.time - last < MapHeimPlugin.TableSeconds.Value) continue;
                LastSync[id] = Time.time;

                try
                {
                    Sync(table, map);
                    player.Message(MessageHud.MessageType.TopLeft, "Cartographer's table: map updated");
                }
                catch (System.Exception e)
                {
                    MapHeimPlugin.Log.LogWarning("Couldn't sync with a cartographer's table: " + e.Message);
                }
            }
        }

        private static void Sync(MapTable table, Minimap map)
        {
            // What the table knows comes into your map...
            byte[] data = table.m_nview.GetZDO().GetByteArray(ZDOVars.s_data);
            if (data != null && data.Length > 0) map.AddSharedMapData(data);

            // ...and your map (merged with it) goes back to the table.
            byte[] merged = map.GetSharedMapData(data);
            table.m_nview.InvokeRPC("MapData", new ZPackage(merged));
        }
    }
}
