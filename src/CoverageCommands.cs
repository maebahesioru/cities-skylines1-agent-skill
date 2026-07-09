using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Service coverage and citizen welfare state APIs.
    /// Verified against CS1 Assembly-CSharp.dll decompiled fields.
    /// </summary>
    public static class CoverageCommands
    {
        public static CommandResult BuildCoverageJson()
        {
            DistrictManager dm = DistrictManager.instance;
            CitizenManager cm = CitizenManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // District-level metrics from main district (index 0)
            if (dm != null && dm.m_districts.m_size > 0)
            {
                District d = dm.m_districts.m_buffer[0];
                if ((d.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    json.Append(",\"population\":" + ((int)d.m_populationData.m_finalCount));
                    json.Append(",\"crimeRate\":" + d.m_finalCrimeRate);
                    json.Append(",\"happiness\":" + d.m_finalHappiness);
                    json.Append(",\"groundPollution\":" + d.m_groundData.m_finalPollution);
                    json.Append(",\"landValue\":" + d.m_groundData.m_finalLandvalue);

                    // Consumption data (sewage, garbage, sick, dead)
                    var cons = d.m_residentialConsumption;
                    json.Append(",\"sewageAccumulation\":" + cons.m_finalSewageAccumulation);
                    json.Append(",\"garbageAccumulation\":" + cons.m_finalGarbageAccumulation);
                    json.Append(",\"deadCount\":" + cons.m_finalDeadCount);
                    json.Append(",\"sickCount\":" + cons.m_finalSickCount);
                    json.Append(",\"electricityConsumption\":" + cons.m_finalElectricityConsumption);
                    json.Append(",\"waterConsumption\":" + cons.m_finalWaterConsumption);

                    // Education data
                    json.Append(",\"uneducated\":" + d.m_educated0Data.m_finalCount);
                    json.Append(",\"educated1\":" + d.m_educated1Data.m_finalCount);
                    json.Append(",\"educated2\":" + d.m_educated2Data.m_finalCount);
                    json.Append(",\"educated3\":" + d.m_educated3Data.m_finalCount);
                    json.Append(",\"unemployed\":" + d.m_educated0Data.m_finalUnemployed);

                    // Average wellbeing sample from citizens
                    json.Append(",\"averageWellbeing\":" + JsonUtil.Number(GetAverageWellbeing(cm)));
                }
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static float GetAverageWellbeing(CitizenManager cm)
        {
            int total = 0, count = 0;
            for (ushort i = 1; i < cm.m_citizens.m_buffer.Length && count < 500; i++)
            {
                Citizen c = cm.m_citizens.m_buffer[i];
                if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None || c.Dead) continue;
                total += (int)c.m_wellbeing;
                count++;
            }
            return count > 0 ? (float)total / count : 0f;
        }
    }
}
