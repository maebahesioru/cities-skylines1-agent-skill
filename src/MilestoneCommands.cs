using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Milestone and unlock state APIs for Cities: Skylines 1.
    /// Reads current milestone, unlock status, and available services.
    /// </summary>
    public static class MilestoneCommands
    {
        public static CommandResult BuildMilestoneJson()
        {
            MilestoneInfo[] milestones = UnityEngine.Object.FindObjectOfType<MilestoneCollection>()?.m_milestones;
            UnlockManager unlockManager = Singleton<UnlockManager>.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Current milestone level
            int currentMilestone = 0;
            int prevPopulationRequired = 0;

            if (milestones != null)
            {
                DistrictManager districtManager = DistrictManager.instance;
                int population = (int)districtManager.m_districts.m_buffer[0].m_populationData.m_finalCount;

                for (int i = 0; i < milestones.Length; i++)
                {
                    MilestoneInfo mi = milestones[i];
                    if (mi == null) continue;
                    if (population >= mi.m_populationRequired)
                    {
                        if (mi.m_populationRequired >= prevPopulationRequired)
                        {
                            currentMilestone = i;
                            prevPopulationRequired = mi.m_populationRequired;
                        }
                    }
                }

                json.Append(",\"currentMilestoneIndex\":").Append(currentMilestone);
                json.Append(",\"totalMilestones\":").Append(milestones.Length);
                json.Append(",\"milestones\":[");
                for (int i = 0; i < milestones.Length; i++)
                {
                    MilestoneInfo mi = milestones[i];
                    if (i > 0) json.Append(",");
                    json.Append("{\"index\":").Append(i);
                    json.Append(",\"populationRequired\":").Append(mi.m_populationRequired);
                    json.Append(",\"unlocked\":").Append(JsonUtil.Bool(i <= currentMilestone));
                    json.Append(",\"name\":\"").Append(JsonUtil.Escape(mi.m_name ?? "Milestone " + i)).Append("\"}");
                }
                json.Append("]");
            }
            else
            {
                json.Append(",\"currentMilestoneIndex\":0,\"totalMilestones\":0,\"milestones\":[]");
            }

            // Unlock status
            json.Append(",\"unlockedServices\":[");
            if (unlockManager != null)
            {
                System.Array serviceValues = System.Enum.GetValues(typeof(ItemClass.Service));
                System.Array subServiceValues = System.Enum.GetValues(typeof(ItemClass.SubService));
                bool firstUnlock = true;

                foreach (ItemClass.Service service in serviceValues)
                {
                    if (service == ItemClass.Service.None || (int)service >= 128) continue;
                    foreach (ItemClass.SubService subService in subServiceValues)
                    {
                        if (subService == ItemClass.SubService.None || (int)subService >= 128) continue;
                        try
                        {
                            if (unlockManager.Unlocked(service, subService, ItemClass.Level.Level1))
                            {
                                if (!firstUnlock) json.Append(",");
                                json.Append("{\"service\":\"").Append(JsonUtil.Escape(service.ToString())).Append("\"");
                                json.Append(",\"subService\":\"").Append(JsonUtil.Escape(subService.ToString())).Append("\"}");
                                firstUnlock = false;
                            }
                        }
                        catch { }
                    }
                }
            }
            json.Append("]");

            // Unlocked building count
            int unlockedBuildings = 0;
            int totalPrefabBuildings = PrefabCollection<BuildingInfo>.LoadedCount();
            for (int i = 0; i < totalPrefabBuildings; i++)
            {
                BuildingInfo info = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);
                if (info != null && info.m_class != null && info.m_class.m_service != ItemClass.Service.None)
                {
                    try
                    {
                        if (unlockManager != null && unlockManager.Unlocked(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level))
                            unlockedBuildings++;
                    }
                    catch { }
                }
            }
            json.Append(",\"unlockedBuildingCount\":").Append(unlockedBuildings);
            json.Append(",\"totalPrefabBuildingCount\":").Append(totalPrefabBuildings);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
