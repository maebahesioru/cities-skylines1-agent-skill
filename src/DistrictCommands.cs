using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// District and policy APIs for Cities: Skylines 1.
    /// Reads district information and policies.
    /// </summary>
    public static class DistrictCommands
    {
        public static CommandResult BuildDistrictsJson()
        {
            DistrictManager manager = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"districts\":[");
            int count = 0;

            for (byte i = 0; i < manager.m_districts.m_buffer.Length && count < 128; i++)
            {
                District district = manager.m_districts.m_buffer[i];
                if ((district.m_flags & District.Flags.Created) == District.Flags.None) continue;

                if (count > 0) json.Append(",");
                json.Append("{\"id\":").Append(i);
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(district.GetName())).Append("\"");
                json.Append(",\"population\":").Append((int)district.m_populationData.m_finalCount);
                json.Append(",\"policies\":{");

                // District policies
                AppendPolicy(json, district, DistrictPolicies.Taxation.SmokeDetector, "SmokeDetector");
                AppendPolicy(json, district, DistrictPolicies.Taxation.Recycling, "Recycling");
                AppendPolicy(json, district, DistrictPolicies.Taxation.PetBan, "PetBan");
                AppendPolicy(json, district, DistrictPolicies.Taxation.SmokingBan, "SmokingBan");
                AppendPolicy(json, district, DistrictPolicies.Taxation.EncourageBiking, "EncourageBiking");
                AppendPolicy(json, district, DistrictPolicies.Taxation.FreePublicTransport, "FreePublicTransport");
                AppendPolicy(json, district, DistrictPolicies.Taxation.HighTicketPrice, "HighTicketPrice");
                AppendPolicy(json, district, DistrictPolicies.Taxation.PreferBuses, "PreferBuses");
                AppendPolicy(json, district, DistrictPolicies.Taxation.WaterUsage, "WaterUsage");
                AppendPolicy(json, district, DistrictPolicies.Taxation.ElectricityUsage, "ElectricityUsage");
                AppendPolicy(json, district, DistrictPolicies.Taxation.OnlyElectricCars, "OnlyElectricCars");
                AppendPolicy(json, district, DistrictPolicies.Taxation.HeavyTrafficBan, "HeavyTrafficBan");

                // Remove trailing comma from policies object
                json.Length--;
                json.Append("}");
                json.Append(",\"areaSquares\":" + ((int)district.m_areaSquares));
                json.Append("}");
                count++;
            }
            json.Append("],\"total\":" + count + "}");
            return CommandResult.FromJson(json.ToString());
        }

        private static void AppendPolicy(StringBuilder json, District district, DistrictPolicies.Taxation policy, string name)
        {
            bool active = (district.m_policies & policy) != DistrictPolicies.Taxation.None;
            json.Append("\"").Append(name).Append("\":").Append(JsonUtil.Bool(active)).Append(",");
        }

        /// <summary>
        /// Toggle a district policy.
        /// POST /commands/set-policy
        /// Body: { "districtId": 0, "policy": "SmokeDetector", "active": true }
        /// </summary>
        public static CommandResult SetPolicy(string body)
        {
            byte districtId = (byte)JsonUtil.GetNumber(body, "districtId", 0f);
            string policyName = JsonUtil.GetString(body, "policy", "");
            bool active = JsonUtil.GetBool(body, "active", true);

            if (policyName.Length == 0) return CommandResult.Fail("policy name is required.");

            DistrictManager manager = DistrictManager.instance;
            District district = manager.m_districts.m_buffer[districtId];
            if ((district.m_flags & District.Flags.Created) == District.Flags.None)
                return CommandResult.Fail("District not found: " + districtId);

            DistrictPolicies.Taxation policy = ParsePolicy(policyName);
            if (policy == DistrictPolicies.Taxation.None)
                return CommandResult.Fail("Unknown policy: " + policyName);

            if (active)
                district.m_policies |= policy;
            else
                district.m_policies &= ~policy;

            manager.m_districts.m_buffer[districtId] = district;

            return CommandResult.FromJson(
                "{\"ok\":true,\"districtId\":" + districtId +
                ",\"policy\":\"" + JsonUtil.Escape(policyName) + "\"" +
                ",\"active\":" + JsonUtil.Bool(active) + "}");
        }

        private static DistrictPolicies.Taxation ParsePolicy(string name)
        {
            switch (name)
            {
                case "SmokeDetector": return DistrictPolicies.Taxation.SmokeDetector;
                case "Recycling": return DistrictPolicies.Taxation.Recycling;
                case "PetBan": return DistrictPolicies.Taxation.PetBan;
                case "SmokingBan": return DistrictPolicies.Taxation.SmokingBan;
                case "EncourageBiking": return DistrictPolicies.Taxation.EncourageBiking;
                case "FreePublicTransport": return DistrictPolicies.Taxation.FreePublicTransport;
                case "HighTicketPrice": return DistrictPolicies.Taxation.HighTicketPrice;
                case "PreferBuses": return DistrictPolicies.Taxation.PreferBuses;
                case "WaterUsage": return DistrictPolicies.Taxation.WaterUsage;
                case "ElectricityUsage": return DistrictPolicies.Taxation.ElectricityUsage;
                case "OnlyElectricCars": return DistrictPolicies.Taxation.OnlyElectricCars;
                case "HeavyTrafficBan": return DistrictPolicies.Taxation.HeavyTrafficBan;
                default: return DistrictPolicies.Taxation.None;
            }
        }
    }
}
