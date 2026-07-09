using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Milestone and unlock state APIs for Cities: Skylines 1.
    /// Uses UnlockManager to query unlock status.
    /// BuildingManager for building counts.
    /// </summary>
    public static class MilestoneCommands
    {
        public static CommandResult BuildMilestoneJson()
        {
            UnlockManager unlockManager = Singleton<UnlockManager>.instance;
            BuildingManager buildingManager = BuildingManager.instance;
            DistrictManager districtManager = DistrictManager.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Population for milestone context
            int population = districtManager != null
                ? (int)districtManager.m_districts.m_buffer[0].m_populationData.m_finalCount
                : 0;
            json.Append(",\"population\":" + population);

            // Unlocked services — scan all service/subservice/level combos
            json.Append(",\"unlockedServices\":[");
            System.Array serviceValues = System.Enum.GetValues(typeof(ItemClass.Service));
            System.Array subServiceValues = System.Enum.GetValues(typeof(ItemClass.SubService));
            ItemClass.Level[] levels = new ItemClass.Level[] {
                ItemClass.Level.Level1, ItemClass.Level.Level2,
                ItemClass.Level.Level3, ItemClass.Level.Level4, ItemClass.Level.Level5
            };
            bool firstUnlock = true;
            int unlockedCount = 0;

            foreach (ItemClass.Service service in serviceValues)
            {
                if ((int)service <= 0 || (int)service >= 128) continue;
                foreach (ItemClass.SubService subService in subServiceValues)
                {
                    if ((int)subService <= 0 || (int)subService >= 128) continue;
                    bool anyUnlocked = false;
                    foreach (ItemClass.Level level in levels)
                    {
                        try
                        {
                            if (unlockManager.Unlocked(service, subService, level))
                            {
                                anyUnlocked = true;
                                break;
                            }
                        }
                        catch { }
                    }
                    if (anyUnlocked)
                    {
                        if (!firstUnlock) json.Append(",");
                        json.Append("{\"service\":\"").Append(JsonUtil.Escape(service.ToString())).Append("\"");
                        json.Append(",\"subService\":\"").Append(JsonUtil.Escape(subService.ToString())).Append("\"}");
                        firstUnlock = false;
                        unlockedCount++;
                    }
                }
            }
            json.Append("]");
            json.Append(",\"unlockedServiceCount\":" + unlockedCount);

            // Building counts
            int unlockedBuildings = 0;
            int totalPrefabBuildings = PrefabCollection<BuildingInfo>.LoadedCount();
            int lockedBuildings = 0;

            for (int i = 0; i < totalPrefabBuildings; i++)
            {
                BuildingInfo info = PrefabCollection<BuildingInfo>.GetLoaded((uint)i);
                if (info == null || info.m_class == null || info.m_class.m_service == ItemClass.Service.None)
                    continue;

                bool unlocked = false;
                try
                {
                    unlocked = unlockManager.Unlocked(info.m_class.m_service, info.m_class.m_subService, info.m_class.m_level);
                }
                catch { }

                if (unlocked) unlockedBuildings++;
                else lockedBuildings++;
            }

            json.Append(",\"unlockedBuildingCount\":" + unlockedBuildings);
            json.Append(",\"lockedBuildingCount\":" + lockedBuildings);
            json.Append(",\"totalBuildingCount\":" + (unlockedBuildings + lockedBuildings));

            // Milestones — find via MilestoneInfo if available, otherwise approximate
            MilestoneInfo[] milestones = null;
            try
            {
                // MilestoneCollection is in ColossalFramework and accessible via FindObjectOfType
                var collection = UnityEngine.Object.FindObjectOfType<MilestoneCollection>();
                if (collection != null)
                    milestones = collection.m_milestones;
            }
            catch { }

            if (milestones != null && milestones.Length > 0)
            {
                json.Append(",\"milestones\":[");
                int currentIndex = 0;
                for (int i = 0; i < milestones.Length; i++)
                {
                    MilestoneInfo mi = milestones[i];
                    if (mi == null) continue;
                    if (i > 0) json.Append(",");
                    json.Append("{\"index\":").Append(i);
                    json.Append(",\"populationRequired\":").Append(mi.m_populationRequired);
                    json.Append(",\"name\":\"").Append(JsonUtil.Escape(mi.m_name ?? ("Milestone " + i))).Append("\"");
                    bool unlocked = population >= mi.m_populationRequired;
                    json.Append(",\"unlocked\":").Append(JsonUtil.Bool(unlocked));
                    if (unlocked) currentIndex = i;
                    json.Append("}");
                }
                json.Append("]");
                json.Append(",\"currentMilestoneIndex\":" + currentIndex);
                json.Append(",\"totalMilestones\":" + milestones.Length);
            }
            else
            {
                json.Append(",\"milestones\":[]");
                json.Append(",\"currentMilestoneIndex\":-1");
                json.Append(",\"totalMilestones\":0");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
