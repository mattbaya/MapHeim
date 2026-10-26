using UnityEngine;

namespace MapHeim
{
    /// <summary>
    /// Versions 0.2.0 and 0.2.1 wrote a whole uncompressed map (about 4 MB) into cartographer's tables. A table that big can't be
    /// sent to a joining player (Steam refuses any single message over about half a megabyte), so the connection fails. Whoever
    /// hosts the world empties any table holding that much data; the table is otherwise left alone.
    /// </summary>
    internal static class TableRepair
    {
        private const int TooBig = 400 * 1024;
        private static readonly int TablePrefab = "piece_cartographytable".GetStableHashCode();
        private static float _next;

        public static void Tick()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZDOMan.instance == null || ZNetScene.instance == null)
            {
                _next = 0f;
                return;
            }

            // Give the world a moment to finish loading, then look again every few minutes.
            if (_next == 0f) _next = Time.time + 20f;
            if (Time.time < _next) return;
            _next = Time.time + 300f;

            int fixedTables = 0;
            foreach (var zdo in ZDOMan.instance.m_objectsByID.Values)
            {
                if (zdo == null || zdo.GetPrefab() != TablePrefab) continue;
                var data = zdo.GetByteArray(ZDOVars.s_data);
                if (data == null || data.Length < TooBig) continue;

                var p = zdo.GetPosition();
                MapHeimPlugin.Log.LogWarning($"A cartographer's table at ({p.x:0}, {p.y:0}, {p.z:0}) held {data.Length / 1024} KB of map data, too much to send to players. Emptying it.");
                zdo.SetOwner(ZDOMan.GetSessionID());
                zdo.Set(ZDOVars.s_data, new byte[0]);
                ZDOMan.instance.ForceSendZDO(zdo.m_uid);
                fixedTables++;
            }

            if (fixedTables > 0) MapHeimPlugin.Log.LogInfo($"Emptied {fixedTables} oversized cartographer's table(s).");
        }
    }
}
