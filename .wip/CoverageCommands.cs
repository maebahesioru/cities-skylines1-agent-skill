using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Service coverage and citizen welfare state APIs.
    /// Reads district-level coverage metrics and samples citizen data.
    /// </summary>
    public static class CoverageCommands
    {
        public static CommandResult BuildCoverageJson()
        {
            DistrictManager districtManager = DistrictManager.instance;
            CitizenManager citizenManager = CitizenManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Main district coverage ---
            json.Append(",\"serviceCoverage\":{");
            if (districtManager != null)
            {
                District district = districtManager.m_districts.m_buffer[0];
                if ((district.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    DistrictPark districtPark = districtManager.m_parks.m_buffer[0];
                    json.Append("\"population\":").Append((int)district.m_populationData.m_finalCount);
                    json.Append(",\"fireHazard\":").Append(JsonUtil.Number(district.GetFireHazard()));
                    json.Append(",\"crimeRate\":").Append(JsonUtil.Number(districtPark.m_crimeAccumulation));
                    json.Append(",\"crimeBuffer\":").Append(JsonUtil.Number(districtPark.m_crimeBuffer));
                    json.Append(",\"garbageAccumulation\":").Append(JsonUtil.Number(district.m_garbageData.m_garbageAccumulation));
                    json.Append(",\"garbageBuffer\":").Append(JsonUtil.Number(district.m_garbageData.m_garbageBuffer));
                    json.Append(",\"groundPollution\":").Append(JsonUtil.Number(district.m_groundPollutionData.m_pollutionAccumulation));
                    json.Append(",\"noisePollution\":").Append(JsonUtil.Number(district.m_noisePollutionData.m_noiseAccumulation));
                    json.Append(",\"sickCount\":").Append(district.m_sickCount);
                    json.Append(",\"deadCount\":").Append(district.m_deadCount);
                    json.Append(",\"attractiveness\":").Append(district.m_totalAttractiveness);
                    json.Append(",\"averageHealth\":").Append(JsonUtil.Number(GetAverageHealth(citizenManager)));
                    json.Append(",\"averageEducation\":").Append(JsonUtil.Number(GetAverageEducation(citizenManager)));
                    json.Append(",\"averageWealth\":").Append(JsonUtil.Number(GetAverageWealth(citizenManager)));
                }
                else
                {
                    json.Append("\"error\":\"No main district\"");
                }
            }
            else
            {
                json.Append("\"error\":\"DistrictManager unavailable\"");
            }
            json.Append("}");

            // --- Citizen statistics ---
            json.Append(",\"citizenStats\":{");
            int employed = 0, unemployed = 0, educated = 0, uneducated = 0;
            int adults = 0, seniors = 0, children = 0, teens = 0, youngAdults = 0;
            int wealthy = 0, poor = 0, totalWellbeing = 0, totalHealth = 0, sampleCount = 0;
            int maxSamples = 2000;

            for (ushort i = 1; i < citizenManager.m_citizens.m_buffer.Length && sampleCount < maxSamples; i++)
            {
                Citizen citizen = citizenManager.m_citizens.m_buffer[i];
                if ((citizen.m_flags & Citizen.Flags.Created) == Citizen.Flags.None) continue;
                if (citizen.Dead) continue;
                sampleCount++;

                // Age
                uint age = citizen.Age;
                if (age < 40) children++;
                else if (age < 80) teens++;
                else if (age < 120) youngAdults++;
                else if (age < 180) adults++;
                else seniors++;

                // Education
                if (citizen.Education3 != Citizen.Education.Uneducated) educated++;
                else uneducated++;

                // Employment
                if (citizen.m_workBuilding != 0) employed++;
                else if (age >= 80) unemployed++;

                // Wealth (enum: Low=0, Medium=1, High=2)
                if ((int)citizen.Wealth >= 1) wealthy++;
                else poor++;

                // Wellbeing & health
                totalWellbeing += (int)citizen.m_wellbeing;
                totalHealth += citizen.m_health;
            }

            json.Append("\"children\":").Append(children);
            json.Append(",\"teens\":").Append(teens);
            json.Append(",\"youngAdults\":").Append(youngAdults);
            json.Append(",\"adults\":").Append(adults);
            json.Append(",\"seniors\":").Append(seniors);
            json.Append(",\"employed\":").Append(employed);
            json.Append(",\"unemployed\":").Append(unemployed);
            json.Append(",\"educated\":").Append(educated);
            json.Append(",\"uneducated\":").Append(uneducated);
            json.Append(",\"wealthy\":").Append(wealthy);
            json.Append(",\"poor\":").Append(poor);
            json.Append(",\"averageWellbeing\":" + (sampleCount > 0 ? JsonUtil.Number((float)totalWellbeing / sampleCount) : "0"));
            json.Append(",\"averageHealth\":" + (sampleCount > 0 ? JsonUtil.Number((float)totalHealth / sampleCount) : "0"));
            json.Append(",\"totalCitizens\":").Append(citizenManager.m_citizenCount);
            json.Append(",\"sampleSize\":").Append(sampleCount);
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static float GetAverageHealth(CitizenManager cm)
        {
            int total = 0, count = 0;
            for (ushort i = 1; i < cm.m_citizens.m_buffer.Length && count < 2000; i++)
            {
                Citizen c = cm.m_citizens.m_buffer[i];
                if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None || c.Dead) continue;
                total += c.m_health;
                count++;
            }
            return count > 0 ? (float)total / count : 0f;
        }

        private static float GetAverageEducation(CitizenManager cm)
        {
            int total = 0, count = 0;
            for (ushort i = 1; i < cm.m_citizens.m_buffer.Length && count < 2000; i++)
            {
                Citizen c = cm.m_citizens.m_buffer[i];
                if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None || c.Dead) continue;
                total += (int)c.Education3;
                count++;
            }
            return count > 0 ? (float)total / (count * 3f) : 0f;
        }

        private static float GetAverageWealth(CitizenManager cm)
        {
            int total = 0, count = 0;
            for (ushort i = 1; i < cm.m_citizens.m_buffer.Length && count < 2000; i++)
            {
                Citizen c = cm.m_citizens.m_buffer[i];
                if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None || c.Dead) continue;
                total += (int)c.Wealth;
                count++;
            }
            return count > 0 ? (float)total / count : 0f;
        }
    }
}
