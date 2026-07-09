using System;
using System.Text;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class PropCommands
    {
        public static CommandResult BuildPropsJson()
        {
            PropManager pm = PropManager.instance;
            if (pm == null) return CommandResult.Fail("PropManager not found.");
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            int propCount = 0;
            for (ushort i = 1; i < pm.m_props.m_size && propCount < 50000; i++)
            {
                PropInstance prop = pm.m_props.m_buffer[i];
                if ((prop.m_flags & (ushort)PropInstance.Flags.Created) != 0) propCount++;
            }
            json.Append(",\"propCount\":" + propCount);
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult PlaceProp(string body)
        {
            string prefabName = JsonUtil.GetString(body, "prefab", "");
            float x = JsonUtil.GetPointNumber(body, "position", "x", 0f);
            float z = JsonUtil.GetPointNumber(body, "position", "z", 0f);
            float angle = JsonUtil.GetNumber(body, "angle", 0f);
            if (string.IsNullOrEmpty(prefabName)) return CommandResult.Fail("Prop prefab name required.");

            PropManager pm = PropManager.instance;
            if (pm == null) return CommandResult.Fail("PropManager not found.");

            PropInfo info = null;
            int pc = PrefabCollection<PropInfo>.LoadedCount();
            for (int i = 0; i < pc; i++) { PropInfo pi = PrefabCollection<PropInfo>.GetLoaded((uint)i); if (pi != null && pi.name.IndexOf(prefabName, StringComparison.OrdinalIgnoreCase) >= 0) { info = pi; break; } }
            if (info == null) return CommandResult.Fail("Prop not found: " + prefabName);

            try
            {
                TerrainManager terrain = TerrainManager.instance;
                float y = 0f;
                if (terrain != null) y = terrain.GetDetailHeight((int)x, (int)z);
                Vector3 pos = new Vector3(x, y, z);
                Randomizer r = new Randomizer((uint)System.DateTime.Now.Ticks);

                ushort propId;
                bool created = pm.CreateProp(out propId, ref r, info, pos, angle, true);
                if (created)
                    return CommandResult.FromJson("{\"ok\":true,\"propId\":" + propId + ",\"prefab\":\"" + JsonUtil.Escape(prefabName) + "\",\"position\":{\"x\":" + JsonUtil.Number(x) + ",\"y\":" + JsonUtil.Number(y) + ",\"z\":" + JsonUtil.Number(z) + "},\"angle\":" + JsonUtil.Number(angle) + "}");
                return CommandResult.Fail("Failed to place prop.");
            }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
