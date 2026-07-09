using System;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class TreeCommands
    {
        public static CommandResult BuildTreesJson()
        {
            TreeManager tm = TreeManager.instance;
            if (tm == null) return CommandResult.Fail("TreeManager not found.");
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            int treeCount = 0;
            for (uint i = 1; i < tm.m_trees.m_size && treeCount < 50000; i++)
            {
                TreeInstance tree = tm.m_trees.m_buffer[i];
                if ((tree.m_flags & (ushort)TreeInstance.Flags.Created) != 0) treeCount++;
            }
            json.Append(",\"treeCount\":" + treeCount);
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult PlantTree(string body)
        {
            string prefabName = JsonUtil.GetString(body, "prefab", "");
            float x = JsonUtil.GetPointNumber(body, "position", "x", 0f);
            float z = JsonUtil.GetPointNumber(body, "position", "z", 0f);
            if (string.IsNullOrEmpty(prefabName)) return CommandResult.Fail("Tree prefab name required.");

            TreeManager tm = TreeManager.instance;
            if (tm == null) return CommandResult.Fail("TreeManager not found.");

            TreeInfo info = null;
            int pc = PrefabCollection<TreeInfo>.LoadedCount();
            for (int i = 0; i < pc; i++) { TreeInfo ti = PrefabCollection<TreeInfo>.GetLoaded((uint)i); if (ti != null && ti.name.IndexOf(prefabName, StringComparison.OrdinalIgnoreCase) >= 0) { info = ti; break; } }
            if (info == null) return CommandResult.Fail("Tree not found: " + prefabName);

            try
            {
                TerrainManager terrain = TerrainManager.instance;
                float y = 0f;
                if (terrain != null) y = terrain.GetDetailHeight((int)x, (int)z);
                Vector3 pos = new Vector3(x, y, z);
                Randomizer r = new Randomizer((uint)System.DateTime.Now.Ticks);

                uint treeId;
                bool created = tm.CreateTree(out treeId, ref r, info, pos, true);
                if (created)
                    return CommandResult.FromJson("{\"ok\":true,\"treeId\":" + treeId + ",\"prefab\":\"" + JsonUtil.Escape(prefabName) + "\",\"position\":{\"x\":" + JsonUtil.Number(x) + ",\"y\":" + JsonUtil.Number(y) + ",\"z\":" + JsonUtil.Number(z) + "}}");
                return CommandResult.Fail("Failed to plant tree.");
            }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
