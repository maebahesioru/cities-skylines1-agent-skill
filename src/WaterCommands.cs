using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class WaterCommands
    {
        public static CommandResult BuildWaterJson()
        {
            WaterManager wm = WaterManager.instance;
            if (wm == null) return CommandResult.Fail("WaterManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Water/sewage/heating pulse group counts
            try
            {
                var fw = typeof(WaterManager).GetField("m_waterPulseGroupCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fs = typeof(WaterManager).GetField("m_sewagePulseGroupCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fh = typeof(WaterManager).GetField("m_heatingPulseGroupCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (fw != null) json.Append(",\"waterPulseGroups\":" + (int)fw.GetValue(wm));
                if (fs != null) json.Append(",\"sewagePulseGroups\":" + (int)fs.GetValue(wm));
                if (fh != null) json.Append(",\"heatingPulseGroups\":" + (int)fh.GetValue(wm));
            }
            catch { }

            // Count buildings with water/sewage problems
            BuildingManager bm = BuildingManager.instance;
            int waterProblems = 0, sewageProblems = 0, totalBuildings = 0;
            if (bm != null)
            {
                for (ushort i = 1; i < bm.m_buildings.m_size && totalBuildings < 5000; i++)
                {
                    Building b = bm.m_buildings.m_buffer[i];
                    if ((b.m_flags & Building.Flags.Created) == Building.Flags.None) continue;
                    totalBuildings++;
                    if (!b.m_problems.IsNone)
                    {
                        string ps = b.m_problems.ToString();
                        if (ps.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0) waterProblems++;
                        if (ps.IndexOf("ewage", StringComparison.OrdinalIgnoreCase) >= 0) sewageProblems++;
                    }
                }
            }

            json.Append(",\"buildings\":{");
            json.Append("\"total\":" + totalBuildings);
            json.Append(",\"waterProblems\":" + waterProblems);
            json.Append(",\"sewageProblems\":" + sewageProblems);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
