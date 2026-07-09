using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// District and policy APIs for Cities: Skylines 1.
    /// Reads district information and policies.
    /// Uses DistrictPolicies.Policies enum for district/city policies
    /// and DistrictManager.SetDistrictPolicy/SetCityPolicy/UnsetDistrictPolicy/UnsetCityPolicy.
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

                // District policies — all use DistrictPolicies.Policies enum
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.SmokeDetector, "SmokeDetector");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.Recycling, "Recycling");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.PetBan, "PetBan");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.SmokingBan, "SmokingBan");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.EncourageBiking, "EncourageBiking");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.FreePublicTransport, "FreePublicTransport");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.HighTicketPrice, "HighTicketPrice");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.PreferBuses, "PreferBuses");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.WaterUsage, "WaterUsage");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.ElectricityUsage, "ElectricityUsage");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.OnlyElectricCars, "OnlyElectricCars");
                AppendCityPolicy(json, manager, DistrictPolicies.Policies.HeavyTrafficBan, "HeavyTrafficBan");

                // Remove trailing comma from policies object
                json.Length--;
                json.Append("}");

                // District-level policies from m_policies bitfield
                json.Append(",\"districtPolicies\":{");
                AppendDistrictPolicy(json, district, DistrictPolicies.Policies.SmokeDetector, "SmokeDetector");
                AppendDistrictPolicy(json, district, DistrictPolicies.Policies.Recycling, "Recycling");
                AppendDistrictPolicy(json, district, DistrictPolicies.Policies.PetBan, "PetBan");
                json.Length--; // remove trailing comma
                json.Append("}");

                json.Append(",\"areaSquares\":" + ((int)district.m_areaSquares));
                json.Append("}");
                count++;
            }
            json.Append("],\"total\":" + count + "}");
            return CommandResult.FromJson(json.ToString());
        }

        private static void AppendCityPolicy(StringBuilder json, DistrictManager manager, DistrictPolicies.Policies policy, string name)
        {
            bool active = manager.IsCityPolicySet(policy);
            json.Append("\"").Append(name).Append("\":").Append(JsonUtil.Bool(active)).Append(",");
        }

        private static void AppendDistrictPolicy(StringBuilder json, District district, DistrictPolicies.Policies policy, string name)
        {
            bool active = (district.m_policies & policy) != 0;
            json.Append("\"").Append(name).Append("\":").Append(JsonUtil.Bool(active)).Append(",");
        }

        /// <summary>
        /// Toggle a city-wide or district policy.
        /// POST /commands/set-policy
        /// Body: { "districtId": 0, "policy": "SmokeDetector", "active": true, "scope": "city" }
        /// scope: "city" (default) or "district"
        /// </summary>
        public static CommandResult SetPolicy(string body)
        {
            byte districtId = (byte)JsonUtil.GetNumber(body, "districtId", 0f);
            string policyName = JsonUtil.GetString(body, "policy", "");
            bool active = JsonUtil.GetBool(body, "active", true);
            string scope = JsonUtil.GetString(body, "scope", "city");

            if (policyName.Length == 0) return CommandResult.Fail("policy name is required.");

            DistrictPolicies.Policies policy = ParsePolicy(policyName);
            if (policy == (DistrictPolicies.Policies)0)
                return CommandResult.Fail("Unknown policy: " + policyName);

            DistrictManager manager = DistrictManager.instance;

            if (scope == "district")
            {
                District district = manager.m_districts.m_buffer[districtId];
                if ((district.m_flags & District.Flags.Created) == District.Flags.None)
                    return CommandResult.Fail("District not found: " + districtId);

                if (active)
                    manager.SetDistrictPolicy(districtId, policy);
                else
                    manager.UnsetDistrictPolicy(districtId, policy);
            }
            else // city
            {
                if (active)
                    manager.SetCityPolicy(policy);
                else
                    manager.UnsetCityPolicy(policy);
            }

            return CommandResult.FromJson(
                "{\"ok\":true,\"districtId\":" + districtId +
                ",\"policy\":\"" + JsonUtil.Escape(policyName) + "\"" +
                ",\"active\":" + JsonUtil.Bool(active) +
                ",\"scope\":\"" + JsonUtil.Escape(scope) + "\"}");
        }

        private static DistrictPolicies.Policies ParsePolicy(string name)
        {
            switch (name)
            {
                case "SmokeDetector": return DistrictPolicies.Policies.SmokeDetector;
                case "Recycling": return DistrictPolicies.Policies.Recycling;
                case "PetBan": return DistrictPolicies.Policies.PetBan;
                case "SmokingBan": return DistrictPolicies.Policies.SmokingBan;
                case "EncourageBiking": return DistrictPolicies.Policies.EncourageBiking;
                case "FreePublicTransport": return DistrictPolicies.Policies.FreePublicTransport;
                case "HighTicketPrice": return DistrictPolicies.Policies.HighTicketPrice;
                case "PreferBuses": return DistrictPolicies.Policies.PreferBuses;
                case "WaterUsage": return DistrictPolicies.Policies.WaterUsage;
                case "ElectricityUsage": return DistrictPolicies.Policies.ElectricityUsage;
                case "OnlyElectricCars": return DistrictPolicies.Policies.OnlyElectricCars;
                case "HeavyTrafficBan": return DistrictPolicies.Policies.HeavyTrafficBan;
                default: return (DistrictPolicies.Policies)0;
            }
        }
    }
}
