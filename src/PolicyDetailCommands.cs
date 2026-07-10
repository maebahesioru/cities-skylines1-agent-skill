using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Complete policy enumeration and management.
    /// Covers all DistrictPolicies subtypes: Services, Taxation, CityPlanning, Specialization, Special, Event, Park
    /// </summary>
    public static class PolicyDetailCommands
    {
        /// <summary>
        /// List all available policies with their types and current district/city state.
        /// Query: ?districtId=int (optional, 0 for city-wide)
        /// </summary>
        public static CommandResult BuildPoliciesJson(int districtId)
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            District d;
            bool isDistrict = districtId > 0;
            if (isDistrict)
            {
                if (districtId >= dm.m_districts.m_size)
                    return CommandResult.Fail("Invalid district ID: " + districtId);
                d = dm.m_districts.m_buffer[districtId];
                if ((d.m_flags & District.Flags.Created) == District.Flags.None)
                    return CommandResult.Fail("District does not exist: " + districtId);
                json.Append(",\"districtId\":").Append(districtId);
                json.Append(",\"districtName\":\"District " + districtId + "\"");
            }
            else
            {
                d = dm.m_districts.m_buffer[0]; // City-wide uses district 0
            }

            // Service policies
            json.Append(",\"services\":{");
            json.Append("\"raw\":" + (int)d.m_servicePolicies);
            json.Append(",\"effect\":" + (int)d.m_servicePoliciesEffect);
            json.Append(",\"active\":[");
            AppendServicePolicies(json, d.m_servicePolicies);
            json.Append("]");
            json.Append("}");

            // Taxation policies
            json.Append(",\"taxation\":{");
            json.Append("\"raw\":" + (int)d.m_taxationPolicies);
            json.Append(",\"effect\":" + (int)d.m_taxationPoliciesEffect);
            json.Append("}");

            // City Planning policies
            json.Append(",\"cityPlanning\":{");
            json.Append("\"raw\":" + (int)d.m_cityPlanningPolicies);
            json.Append(",\"effect\":" + (int)d.m_cityPlanningPoliciesEffect);
            json.Append("}");

            // Specialization policies
            json.Append(",\"specialization\":{");
            json.Append("\"raw\":" + (int)d.m_specializationPolicies);
            json.Append(",\"effect\":" + (int)d.m_specializationPoliciesEffect);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set a policy with full type support.
        /// Body: { "policyType": "Services", "policy": "SmokeDetector", "active": true, "districtId": 0 }
        /// policyType: Services | Taxation | CityPlanning | Specialization | Special | Event | Park
        /// </summary>
        public static CommandResult SetPolicyFull(string body)
        {
            int districtId = (int)JsonUtil.GetNumber(body, "districtId", 0f);
            string policyType = JsonUtil.GetString(body, "policyType", "Services");
            string policyName = JsonUtil.GetString(body, "policy", "");
            bool active = JsonUtil.GetBool(body, "active", true);
            string scope = JsonUtil.GetString(body, "scope", "city");

            if (policyName.Length == 0) return CommandResult.Fail("policy name is required.");
            if (policyType.Length == 0) return CommandResult.Fail("policyType is required.");

            DistrictManager dm = DistrictManager.instance;

            if (scope == "district" && districtId > 0)
            {
                District d = dm.m_districts.m_buffer[districtId];
                if ((d.m_flags & District.Flags.Created) == District.Flags.None)
                    return CommandResult.Fail("District not found: " + districtId);
            }

            try
            {
                DistrictPolicies.Policies policyVal = ParsePolicy(policyType, policyName);
                if (scope == "city")
                {
                    if (active)
                        dm.SetCityPolicy(policyVal);
                    else
                        dm.UnsetCityPolicy(policyVal);
                }
                else
                {
                    if (active)
                        dm.SetDistrictPolicy(policyVal, (byte)districtId);
                    else
                        dm.UnsetDistrictPolicy(policyVal, (byte)districtId);
                }
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Policy operation failed: " + ex.Message);
            }

            return CommandResult.FromJson("{\"ok\":true,\"policyType\":\"" + JsonUtil.Escape(policyType) +
                "\",\"policy\":\"" + JsonUtil.Escape(policyName) +
                "\",\"active\":" + JsonUtil.Bool(active) +
                ",\"scope\":\"" + JsonUtil.Escape(scope) + "\"}");
        }

        private static void AppendServicePolicies(StringBuilder json, DistrictPolicies.Services policies)
        {
            bool first = true;
            string[] names = Enum.GetNames(typeof(DistrictPolicies.Services));
            int[] values = (int[])Enum.GetValues(typeof(DistrictPolicies.Services));
            for (int i = 0; i < names.Length; i++)
            {
                if (values[i] == 0) continue;
                if (((int)policies & values[i]) != 0)
                {
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("\"" + names[i] + "\"");
                }
            }
        }

        private static DistrictPolicies.Policies ParsePolicy(string policyType, string name)
        {
            // Try each sub-enum type
            string[] subTypes = { "Services", "Taxation", "CityPlanning", "Specialization", "Special", "Event", "Park" };
            foreach (string st in subTypes)
            {
                try
                {
                    System.Type t = System.Type.GetType("DistrictPolicies+" + st + ",Assembly-CSharp");
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
