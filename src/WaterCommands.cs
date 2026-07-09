using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Water, sewage, and heating (Snowfall) state.
    /// Uses WaterManager.instance fields verified via monodis.
    /// </summary>
    public static class WaterCommands
    {
        public static CommandResult BuildWaterJson()
        {
            WaterManager wm = WaterManager.instance;
            if (wm == null) return CommandResult.Fail("WaterManager not found.");

            BuildingManager bm = BuildingManager.instance;

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

                json.Append(",\"waterPulseGroups\":" + (fw != null ? ((int)fw.GetValue(wm)).ToString() : "0"));
                json.Append(",\"sewagePulseGroups\":" + (fs != null ? ((int)fs.GetValue(wm)).ToString() : "0"));
                json.Append(",\"heatingPulseGroups\":" + (fh != null ? ((int)fh.GetValue(wm)).ToString() : "0"));
            }
            catch { }

            // Count buildings with water/sewage problems
            int waterProblems = 0;
            int sewageProblems = 0;
            int totalBuildings = 0;

            if (bm != null)
            {
                for (ushort i = 1; i < bm.m_buildings.m_size && totalBuildings < 5000; i++)
                {
                    Building b = bm.m_buildings.m_buffer[i];
                    if ((b.m_flags & Building.Flags.Created) == Building.Flags.None) continue;
                    totalBuildings++;

                    if ((b.m_problems & Notification.Problem.Water) != Notification.Problem.None)
                        waterProblems++;
                    if ((b.m_problems & Notification.Problem.Sewage) != Notification.Problem.None)
                        sewageProblems++;
                }
            }

            json.Append(",\"buildings\":{");
            json.Append("\"waterProblems\":" + waterProblems);
            json.Append(",\"sewageProblems\":" + sewageProblems);
            json.Append(",\"total\":" + totalBuildings);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
