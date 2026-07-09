using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// District and policy APIs for Cities: Skylines 1.
    /// Verified against CS1 Assembly-CSharp.dll decompiled fields.
    /// District.m_servicePolicies uses DistrictPolicies.Services enum.
    /// DistrictManager has SetDistrictPolicy/UnsetDistrictPolicy and SetCityPolicy/UnsetCityPolicy.
    /// </summary>
    public static class DistrictCommands
    {
        public static CommandResult BuildDistrictsJson()
        {
            DistrictManager manager = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"districts\":[");
            int count = 0;

            for (int i = 0; i < manager.m_districts.m_size && count < 128; i++)
            {
                District district = manager.m_districts.m_buffer[i];
                if ((district.m_flags & District.Flags.Created) == District.Flags.None) continue;

                if (count > 0) json.Append(",");
                json.Append("{\"id\":").Append(i);
                json.Append(",\"name\":\"").Append(JsonUtil.Escape("District " + i)).Append("\"");
                json.Append(",\"population\":").Append((int)district.m_populationData.m_finalCount);
                json.Append(",\"servicePolicies\":" + ((int)district.m_servicePolicies));
                json.Append(",\"taxationPolicies\":" + ((int)district.m_taxationPolicies));
                json.Append(",\"cityPlanningPolicies\":" + ((int)district.m_cityPlanningPolicies));
                json.Append(",\"crimeRate\":" + district.m_finalCrimeRate);
                json.Append(",\"happiness\":" + district.m_finalHappiness);
                json.Append("}");
                count++;
            }
            json.Append("],\"total\":" + count + "}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Toggle a city-wide policy.
        /// POST /commands/set-policy
        /// Body: { "policyType": "Services", "policy": "SmokeDetector", "active": true, "scope": "city" }
        /// policyType: "Services" | "Taxation" | "CityPlanning" | "Special"
        /// scope: "city" (default) or "district"
        /// </summary>
        public static CommandResult SetPolicy(string body)
        {
            byte districtId = (byte)JsonUtil.GetNumber(body, "districtId", 0f);
            string policyType = JsonUtil.GetString(body, "policyType", "Services");
            string policyName = JsonUtil.GetString(body, "policy", "");
            bool active = JsonUtil.GetBool(body, "active", true);
            string scope = JsonUtil.GetString(body, "scope", "city");

            if (policyName.Length == 0) return CommandResult.Fail("policy name is required.");
            if (policyType.Length == 0) return CommandResult.Fail("policyType is required (Services/Taxation/CityPlanning/Special).");

            DistrictManager manager = DistrictManager.instance;

            if (scope == "district")
            {
                District district = manager.m_districts.m_buffer[districtId];
                if ((district.m_flags & District.Flags.Created) == District.Flags.None)
                    return CommandResult.Fail("District not found: " + districtId);
            }

            // For city-wide policies, use SetCityPolicy/UnsetCityPolicy
            // For district policies, use SetDistrictPolicy/UnsetDistrictPolicy
            // The actual policy value comes from the enum types
            bool applied = false;

            try
            {
                if (scope == "city")
                {
                    if (active)
                        manager.SetCityPolicy(ParsePolicyValue(policyType, policyName));
                    else
                        manager.UnsetCityPolicy(ParsePolicyValue(policyType, policyName));
                }
                else
                {
                    if (active)
                        manager.SetDistrictPolicy(ParsePolicyValue(policyType, policyName), districtId);
                    else
                        manager.UnsetDistrictPolicy(ParsePolicyValue(policyType, policyName), districtId);
                }
                applied = true;
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Policy operation failed: " + ex.Message);
            }

            return CommandResult.FromJson(
                "{\"ok\":true,\"applied\":" + JsonUtil.Bool(applied) +
                ",\"policyType\":\"" + JsonUtil.Escape(policyType) +
                "\",\"policy\":\"" + JsonUtil.Escape(policyName) +
                "\",\"active\":" + JsonUtil.Bool(active) +
                ",\"scope\":\"" + JsonUtil.Escape(scope) + "\"}");
        }

        // Parse policy name to enum value. Uses the actual DistrictPolicies enum types.
        // SetCityPolicy/SetDistrictPolicy take DistrictPolicies.Policies which is an umbrella type.
        private static DistrictPolicies.Policies ParsePolicyValue(string policyType, string name)
        {
            // Try common policy names across all Policy enum types
            string[] enumTypes = { "DistrictPolicies+Services", "DistrictPolicies+Taxation", 
                "DistrictPolicies+CityPlanning", "DistrictPolicies+Special" };
            
            foreach (string etName in enumTypes)
            {
                try
                {
                    System.Type t = System.Type.GetType(etName + ",Assembly-CSharp");
                    if (t == null) continue;
                    object val = System.Enum.Parse(t, name);
                    return (DistrictPolicies.Policies)Convert.ToInt32(val);
                }
                catch { }
            }
            return (DistrictPolicies.Policies)0;
        }
    }
}
