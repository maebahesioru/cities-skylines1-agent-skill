using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class BuildingDetailCommands
    {
        public static CommandResult BuildBuildingDetail(string query)
        {
            ushort buildingId = 0;
            if (query != null && query.Length > 0)
            {
                string[] pairs = query.Split('&');
                foreach (string pair in pairs)
                {
                    string[] kv = pair.Split('=');
                    if (kv.Length == 2 && kv[0] == "id")
                    {
                        ushort.TryParse(kv[1], out buildingId);
                        break;
                    }
                }
            }

            BuildingManager bm = BuildingManager.instance;
            if (bm == null) return CommandResult.Fail("BuildingManager is not available.");
            if (buildingId >= bm.m_buildings.m_size) return CommandResult.Fail("Invalid building ID: " + buildingId);

            Building b = bm.m_buildings.m_buffer[buildingId];
            if (b.m_flags == Building.Flags.None) return CommandResult.Fail("Building does not exist: " + buildingId);

            BuildingInfo info = b.Info;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"id\":").Append(buildingId);

            // Name
            json.Append(",\"name\":\"").Append(JsonUtil.Escape(info != null ? info.name ?? "" : "")).Append("\"");

            // Position
            json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(b.m_position.x));
            json.Append(",\"y\":").Append(JsonUtil.Number(b.m_position.y));
            json.Append(",\"z\":").Append(JsonUtil.Number(b.m_position.z)).Append("}");
            json.Append(",\"angle\":").Append(JsonUtil.Number(b.m_angle));
            json.Append(",\"width\":").Append(b.Width);
            json.Append(",\"length\":").Append(b.Length);

            // Flags
            json.Append(",\"flags\":").Append((int)b.m_flags);
            json.Append(",\"flags2\":").Append((int)b.m_flags2);

            // Level
            json.Append(",\"level\":").Append((int)b.m_level);
            json.Append(",\"levelUpProgress\":").Append((int)b.m_levelUpProgress);

            // Problems
            json.Append(",\"problems\":{");
            json.Append("\"isNone\":").Append(JsonUtil.Bool(b.m_problems.IsNone));
            json.Append("}");

            // Citizens
            json.Append(",\"citizenCount\":").Append((int)b.m_citizenCount);

            // Service state
            json.Append(",\"services\":{");
            json.Append("\"fireHazard\":").Append(JsonUtil.Number(b.m_fireHazard));
            json.Append(",\"garbageBuffer\":").Append((int)b.m_garbageBuffer);
            json.Append(",\"crimeBuffer\":").Append((int)b.m_crimeBuffer);
            json.Append(",\"electricityBuffer\":").Append((int)b.m_electricityBuffer);
            json.Append(",\"waterBuffer\":").Append((int)b.m_waterBuffer);
            json.Append(",\"sewageBuffer\":").Append((int)b.m_sewageBuffer);
            json.Append(",\"heatingBuffer\":").Append((int)b.m_heatingBuffer);
            json.Append(",\"mailBuffer\":").Append((int)b.m_mailBuffer);
            json.Append("}");

            // Production
            json.Append(",\"productionRate\":").Append((int)b.m_productionRate);

            // AI type
            if (info != null && info.m_buildingAI != null)
            {
                json.Append(",\"aiType\":\"").Append(JsonUtil.Escape(info.m_buildingAI.GetType().Name)).Append("\"");
                json.Append(",\"isGrowable\":").Append(JsonUtil.Bool(
                    info.m_buildingAI is ResidentialBuildingAI ||
                    info.m_buildingAI is CommercialBuildingAI ||
                    info.m_buildingAI is IndustrialBuildingAI ||
                    info.m_buildingAI is IndustrialExtractorAI ||
                    info.m_buildingAI is OfficeBuildingAI));
            }

            // Connections
            if (b.m_accessSegment != 0) json.Append(",\"accessSegment\":").Append(b.m_accessSegment);
            if (b.m_waterSource != 0) json.Append(",\"waterSource\":").Append(b.m_waterSource);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
