using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class EnhancedCommands
    {
        // === DISASTER ===
        public static CommandResult BuildActiveDisastersJson()
        {
            DisasterManager dm = Singleton<DisasterManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (dm != null)
            {
                json.Append(",\"activeDisasters\":[");
                bool first = true;
                for (uint i = 0; i < dm.m_disasters.m_size; i++)
                {
                    DisasterData dd = dm.m_disasters.m_buffer[i];
                    if (dd.m_flags == 0) continue;
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("{\"id\":").Append(i);
                    json.Append(",\"flags\":").Append(dd.m_flags);
                    json.Append(",\"intensity\":").Append(dd.m_intensity);
                    json.Append("}");
                }
                json.Append("]");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult EvacuateBuilding(string body)
        {
            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);
            BuildingManager bm = BuildingManager.instance;
            if (buildingId >= bm.m_buildings.m_size) return CommandResult.Fail("Invalid building ID");
            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None) return CommandResult.Fail("Building does not exist");

            DisasterManager dm = Singleton<DisasterManager>.instance;
            Vector3 pos = b.m_position;
            dm.IsEvacuating(pos);

            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + buildingId + "}");
        }

        // === DISTRICT ===
        public static CommandResult BuildDistrictDetail(string query)
        {
            int distId = 0;
            if (query != null) { foreach (string pair in query.Split('&')) { string[] kv = pair.Split('='); if (kv.Length == 2 && kv[0] == "id") int.TryParse(kv[1], out distId); } }
            DistrictManager dm = DistrictManager.instance;
            if (distId >= dm.m_districts.m_size) return CommandResult.Fail("Invalid district ID");
            District d = dm.m_districts.m_buffer[distId];
            if ((d.m_flags & District.Flags.Created) == District.Flags.None) return CommandResult.Fail("District does not exist");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"id\":").Append(distId);
            json.Append(",\"name\":\"District " + distId + "\"");
            json.Append(",\"policies\":{");
            json.Append("\"services\":").Append((int)d.m_servicePolicies);
            json.Append(",\"taxation\":").Append((int)d.m_taxationPolicies);
            json.Append(",\"cityPlanning\":").Append((int)d.m_cityPlanningPolicies);
            json.Append(",\"specialization\":").Append((int)d.m_specializationPolicies);
            json.Append("}");
            json.Append(",\"population\":").Append((int)d.m_populationData.m_finalCount);
            json.Append(",\"landValue\":").Append(JsonUtil.Number(d.m_groundData.m_finalLandvalue));
            json.Append(",\"pollution\":").Append(JsonUtil.Number(d.m_groundData.m_finalPollution));
            json.Append(",\"happiness\":").Append(d.m_finalHappiness);
            json.Append(",\"crimeRate\":").Append(d.m_finalCrimeRate);
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === STATS ===
        public static CommandResult BuildStatsDetail(string query)
        {
            StatisticsManager sm = Singleton<StatisticsManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (sm != null)
            {
                json.Append(",\"note\":\"Stats via StatisticsManager.\"");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === BUILDING UPGRADE ===
        public static CommandResult BuildUpgradesJson(string query)
        {
            ushort buildingId = 0;
            if (query != null) { foreach (string pair in query.Split('&')) { string[] kv = pair.Split('='); if (kv.Length == 2 && kv[0] == "id") ushort.TryParse(kv[1], out buildingId); } }
            BuildingManager bm = BuildingManager.instance;
            if (buildingId >= bm.m_buildings.m_size) return CommandResult.Fail("Invalid building ID");
            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None) return CommandResult.Fail("Building does not exist");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"buildingId\":").Append(buildingId);
            json.Append(",\"currentLevel\":").Append((int)b.m_level);
            json.Append(",\"levelUpProgress\":").Append((int)b.m_levelUpProgress);
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult UpgradeBuilding(string body)
        {
            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);
            int targetLevel = Math.Max(1, Math.Min(5, (int)JsonUtil.GetNumber(body, "targetLevel", 1f)));
            BuildingManager bm = BuildingManager.instance;
            if (buildingId >= bm.m_buildings.m_size) return CommandResult.Fail("Invalid building ID");
            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None) return CommandResult.Fail("Building does not exist");
            if (b.Info == null || b.Info.m_buildingAI == null) return CommandResult.Fail("Building has no AI");

            int oldLevel = (int)b.m_level;
            ItemClass.Level target = (ItemClass.Level)(targetLevel);
            BuildingAI ai = b.Info.m_buildingAI;

            if (ai is ResidentialBuildingAI) ((ResidentialBuildingAI)ai).LevelUp(buildingId, ref b, target);
            else if (ai is CommercialBuildingAI) ((CommercialBuildingAI)ai).LevelUp(buildingId, ref b, target);
            else if (ai is OfficeBuildingAI) ((OfficeBuildingAI)ai).LevelUp(buildingId, ref b, target);
            else b.m_level = (byte)targetLevel;

            b = bm.m_buildings.m_buffer[buildingId];
            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + buildingId + ",\"oldLevel\":" + oldLevel + ",\"newLevel\":" + (int)b.m_level + "}");
        }
    }
}
