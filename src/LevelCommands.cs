using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Building level manipulation APIs.
    /// Force level up/down of buildings using verified CS1 API signatures.
    /// LevelUp(ushort buildingID, ref Building buildingData, ItemClass.Level targetLevel)
    /// </summary>
    public static class LevelCommands
    {
        /// <summary>
        /// Get level distribution across all growable buildings.
        /// </summary>
        public static CommandResult BuildLevelsJson()
        {
            BuildingManager bm = BuildingManager.instance;
            if (bm == null) return CommandResult.Fail("BuildingManager is not available.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            int[] levels = new int[6];
            int totalGrowables = 0;

            for (ushort i = 0; i < bm.m_buildings.m_size; i++)
            {
                Building b = bm.m_buildings.m_buffer[i];
                if (b.m_flags == Building.Flags.None) continue;
                if (b.Info == null || b.Info.m_buildingAI == null) continue;

                bool isGrowable = b.Info.m_buildingAI is ResidentialBuildingAI ||
                                  b.Info.m_buildingAI is CommercialBuildingAI ||
                                  b.Info.m_buildingAI is IndustrialBuildingAI ||
                                  b.Info.m_buildingAI is IndustrialExtractorAI ||
                                  b.Info.m_buildingAI is OfficeBuildingAI;

                if (isGrowable)
                {
                    int lvl = (int)b.m_level;
                    if (lvl >= 0 && lvl <= 5) levels[lvl]++;
                    totalGrowables++;
                }
            }

            json.Append(",\"levelDistribution\":{");
            json.Append("\"level0\":" + levels[0]);
            json.Append(",\"level1\":" + levels[1]);
            json.Append(",\"level2\":" + levels[2]);
            json.Append(",\"level3\":" + levels[3]);
            json.Append(",\"level4\":" + levels[4]);
            json.Append(",\"level5\":" + levels[5]);
            json.Append("}");
            json.Append(",\"totalGrowables\":" + totalGrowables);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Force a building to level up to a target level.
        /// Body: { "buildingId": ushort, "targetLevel": int (1-5) }
        /// Uses verified API: LevelUp(ushort buildingID, ref Building buildingData, ItemClass.Level targetLevel)
        /// </summary>
        public static CommandResult LevelUpBuilding(string body)
        {
            BuildingManager bm = BuildingManager.instance;
            if (bm == null) return CommandResult.Fail("BuildingManager is not available.");

            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);
            int targetLevel = Math.Max(1, Math.Min(5, (int)JsonUtil.GetNumber(body, "targetLevel", 5f)));

            if (buildingId >= bm.m_buildings.m_size)
                return CommandResult.Fail("Invalid building ID: " + buildingId);

            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None)
                return CommandResult.Fail("Building does not exist: " + buildingId);

            if (b.Info == null || b.Info.m_buildingAI == null)
                return CommandResult.Fail("Building has no AI: " + buildingId);

            int oldLevel = (int)b.m_level;

            // Use LevelUpWrapper from BuildingManager for controlled level-up
            // BuildingAI::LevelUp(ushort, ref Building, ItemClass.Level)
            BuildingAI ai = b.Info.m_buildingAI;
            ItemClass.Level target = (ItemClass.Level)(targetLevel);

            if (ai is ResidentialBuildingAI)
            {
                ((ResidentialBuildingAI)ai).LevelUp(buildingId, ref b, target);
            }
            else if (ai is CommercialBuildingAI)
            {
                ((CommercialBuildingAI)ai).LevelUp(buildingId, ref b, target);
            }
            else if (ai is OfficeBuildingAI)
            {
                ((OfficeBuildingAI)ai).LevelUp(buildingId, ref b, target);
            }
            else
            {
                // For Industrial and other AI types without LevelUp, manipulate level directly
                if (b.m_level < (byte)targetLevel)
                {
                    b.m_level = (byte)targetLevel;
                }
                else
                {
                    return CommandResult.Fail("Building AI does not have LevelUp and is already at or above target level: " + ai.GetType().Name);
                }
            }

            // Re-read building state
            b = bm.m_buildings.m_buffer[buildingId];
            int newLevel = (int)b.m_level;

            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + buildingId +
                ",\"oldLevel\":" + oldLevel + ",\"newLevel\":" + newLevel + ",\"targetLevel\":" + targetLevel + "}");
        }

        /// <summary>
        /// Decrease a building's level.
        /// Body: { "buildingId": ushort }
        /// </summary>
        public static CommandResult LevelDownBuilding(string body)
        {
            BuildingManager bm = BuildingManager.instance;
            if (bm == null) return CommandResult.Fail("BuildingManager is not available.");

            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);

            if (buildingId >= bm.m_buildings.m_size)
                return CommandResult.Fail("Invalid building ID: " + buildingId);

            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None)
                return CommandResult.Fail("Building does not exist: " + buildingId);

            if (b.Info == null || b.Info.m_buildingAI == null)
                return CommandResult.Fail("Building has no AI: " + buildingId);

            int oldLevel = (int)b.m_level;

            // CS1 does not have a public LevelDown method. Decrement the level directly.
            if (b.m_level > 0)
            {
                b.m_level--;
            }

            b = bm.m_buildings.m_buffer[buildingId];
            int newLevel = (int)b.m_level;

            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + buildingId +
                ",\"oldLevel\":" + oldLevel + ",\"newLevel\":" + newLevel + "}");
        }

        /// <summary>
        /// Get level info for a specific building.
        /// Body: { "buildingId": ushort }
        /// </summary>
        public static CommandResult GetBuildingLevelInfo(string body)
        {
            BuildingManager bm = BuildingManager.instance;
            if (bm == null) return CommandResult.Fail("BuildingManager is not available.");

            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);

            if (buildingId >= bm.m_buildings.m_size)
                return CommandResult.Fail("Invalid building ID: " + buildingId);

            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None || b.Info == null)
                return CommandResult.Fail("Building does not exist: " + buildingId);

            float progress = 0f;
            string levelUpInfo = "";
            if (b.Info.m_buildingAI != null)
            {
                levelUpInfo = b.Info.m_buildingAI.GetLevelUpInfo(buildingId, ref b, out progress) != null ? b.Info.m_buildingAI.GetLevelUpInfo(buildingId, ref b, out progress) : "";
            }

            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + buildingId +
                ",\"currentLevel\":" + (int)b.m_level +
                ",\"levelUpProgress\":" + JsonUtil.Number(progress) +
                ",\"levelUpInfo\":\"" + JsonUtil.Escape(levelUpInfo) +
                "\",\"buildingType\":\"" + JsonUtil.Escape(b.Info.name != null ? b.Info.name : "") + "\"}");
        }
    }
}
