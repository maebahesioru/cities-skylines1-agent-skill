using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Electricity grid state and commands.
    /// Uses ElectricityManager.instance fields verified via monodis.
    /// </summary>
    public static class ElectricityCommands
    {
        public static CommandResult BuildElectricityJson()
        {
            ElectricityManager em = ElectricityManager.instance;
            if (em == null) return CommandResult.Fail("ElectricityManager not found.");

            BuildingManager bm = BuildingManager.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Grid info via private fields (accessible in same AppDomain)
            try
            {
                var fPulseGroupCount = typeof(ElectricityManager).GetField("m_pulseGroupCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fProcessedCells = typeof(ElectricityManager).GetField("m_processedCells",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fConductiveCells = typeof(ElectricityManager).GetField("m_conductiveCells",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var fCanContinue = typeof(ElectricityManager).GetField("m_canContinue",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                json.Append(",\"pulseGroupCount\":" + (fPulseGroupCount != null ? ((int)fPulseGroupCount.GetValue(em)).ToString() : "0"));
                json.Append(",\"processedCells\":" + (fProcessedCells != null ? ((int)fProcessedCells.GetValue(em)).ToString() : "0"));
                json.Append(",\"conductiveCells\":" + (fConductiveCells != null ? ((int)fConductiveCells.GetValue(em)).ToString() : "0"));
                json.Append(",\"stable\":" + JsonUtil.Bool(fCanContinue != null ? !(bool)fCanContinue.GetValue(em) : true));
            }
            catch { }

            // Count buildings with/without electricity
            int poweredCount = 0;
            int unpoweredCount = 0;
            int totalBuildings = 0;

            if (bm != null)
            {
                for (ushort i = 1; i < bm.m_buildings.m_size && totalBuildings < 5000; i++)
                {
                    Building b = bm.m_buildings.m_buffer[i];
                    if ((b.m_flags & Building.Flags.Created) == Building.Flags.None) continue;
                    totalBuildings++;

                    if ((b.m_problems & Notification.Problem.Electricity) != Notification.Problem.None)
                        unpoweredCount++;
                    else
                        poweredCount++;
                }
            }

            json.Append(",\"buildings\":{");
            json.Append("\"powered\":" + poweredCount);
            json.Append(",\"unpowered\":" + unpoweredCount);
            json.Append(",\"total\":" + totalBuildings);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
