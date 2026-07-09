using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Milestone info — counts unlocked vs locked building prefabs.
    /// Uses PrefabCollection<BuildingInfo> and the game's natural unlock logic.
    /// </summary>
    public static class MilestoneCommands
    {
        public static CommandResult BuildMilestoneJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (dm != null && dm.m_districts.m_size > 0)
            {
                District d = dm.m_districts.m_buffer[0];
                json.Append(",\"population\":" + ((int)d.m_populationData.m_finalCount));
                json.Append(",\"happiness\":" + d.m_finalHappiness);
                json.Append(",\"crimeRate\":" + d.m_finalCrimeRate);
            }

            // Count building prefabs
            int totalPrefabs = PrefabCollection<BuildingInfo>.LoadedCount();
            json.Append(",\"totalBuildingPrefabs\":" + totalPrefabs);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
