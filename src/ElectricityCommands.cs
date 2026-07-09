using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class ElectricityCommands
    {
        public static CommandResult BuildElectricityJson()
        {
            ElectricityManager em = ElectricityManager.instance;
            if (em == null) return CommandResult.Fail("ElectricityManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Grid info
            try
            {
                var fPulseGroupCount = typeof(ElectricityManager).GetField("m_pulseGroupCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fProcessed = typeof(ElectricityManager).GetField("m_processedCells",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fConductive = typeof(ElectricityManager).GetField("m_conductiveCells",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (fPulseGroupCount != null) json.Append(",\"pulseGroups\":" + (int)fPulseGroupCount.GetValue(em));
                if (fProcessed != null) json.Append(",\"processedCells\":" + (int)fProcessed.GetValue(em));
                if (fConductive != null) json.Append(",\"conductiveCells\":" + (int)fConductive.GetValue(em));
            }
            catch { }

            // Count buildings with electricity problems via ProblemStruct
            BuildingManager bm = BuildingManager.instance;
            int problemBuildings = 0;
            int totalBuildings = 0;
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
                        if (ps.IndexOf("lectric", StringComparison.OrdinalIgnoreCase) >= 0)
                            problemBuildings++;
                    }
                }
            }

            json.Append(",\"buildings\":{");
            json.Append("\"total\":" + totalBuildings);
            json.Append(",\"electricityProblems\":" + problemBuildings);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
