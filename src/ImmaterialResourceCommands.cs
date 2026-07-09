using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Immaterial resource state — land value, noise, health, attractiveness, etc.
    /// Covers all 35 resource types tracked by ImmaterialResourceManager.
    /// </summary>
    public static class ImmaterialResourceCommands
    {
        // Resource enum names matching ImmaterialResourceManager.Resource
        private static readonly string[] ResourceNames = {
            "HealthCare", "FireDepartment", "PoliceDepartment",
            "EducationElementary", "EducationHighSchool", "EducationUniversity",
            "DeathCare", "PublicTransport", "NoisePollution", "CrimeRate",
            "Health", "Wellbeing", "Density", "Entertainment",
            "LandValue", "Attractiveness", "Coverage", "FireHazard",
            "Abandonment", "CargoTransport", "RadioCoverage", "FirewatchCoverage",
            "EarthquakeCoverage", "DisasterCoverage", "TourCoverage", "PostService",
            "EducationLibrary", "ChildCare", "ElderCare", "CashCollecting",
            "TaxBonus", "Sightseeing", "Shopping", "Business", "Nature"
        };

        public static CommandResult BuildResourceJson()
        {
            ImmaterialResourceManager irm = ImmaterialResourceManager.instance;
            if (irm == null) return CommandResult.Fail("ImmaterialResourceManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"resources\":{");

            try
            {
                // m_totalFinalResources is private but accessible from mod assembly
                var field = typeof(ImmaterialResourceManager).GetField("m_totalFinalResources",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    int[] resources = (int[])field.GetValue(irm);
                    if (resources != null)
                    {
                        bool first = true;
                        int count = Math.Min(resources.Length, ResourceNames.Length);
                        for (int i = 0; i < count; i++)
                        {
                            string name = i < ResourceNames.Length ? ResourceNames[i] : ("Resource" + i);
                            if (!first) json.Append(",");
                            json.Append("\"" + JsonUtil.Escape(name) + "\":" + resources[i]);
                            first = false;
                        }
                    }
                }
            }
            catch { /* fallback: empty */ }

            json.Append("}}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
