using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Service coverage and citizen welfare state APIs.
    /// Reads fire, police, health, education, garbage, deathcare coverage,
    /// plus land value, happiness, and education levels.
    /// </summary>
    public static class CoverageCommands
    {
        public static CommandResult BuildCoverageJson()
        {
            ImmaterialResourceManager resourceManager = Singleton<ImmaterialResourceManager>.instance;
            DistrictManager districtManager = DistrictManager.instance;
            CitizenManager citizenManager = CitizenManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Land value ---
            float avgLandValue = 0f;
            float maxLandValue = 0f;
            int landValueSamples = 0;
            if (resourceManager != null && resourceManager.m_resourceGrid != null)
            {
                for (int i = 0; i < resourceManager.m_resourceGrid.Length; i++)
                {
                    float v = resourceManager.m_resourceGrid[i];
                    if (v > 0f)
                    {
                        avgLandValue += v;
                        if (v > maxLandValue) maxLandValue = v;
                        landValueSamples++;
                    }
                }
                if (landValueSamples > 0) avgLandValue /= landValueSamples;
            }
            json.Append(",\"landValue\":{\"average\":").Append(JsonUtil.Number(avgLandValue));
            json.Append(",\"max\":").Append(JsonUtil.Number(maxLandValue));
            json.Append(",\"samples\":").Append(landValueSamples).Append("}");

            // --- Service coverage per district ---
            json.Append(",\"serviceCoverage\":{");
            if (districtManager != null)
            {
                byte districtId = 0; // main city district
                District district = districtManager.m_districts.m_buffer[districtId];
                if ((district.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    DistrictPark districtPark = districtManager.m_parks.m_buffer[districtId];
                    json.Append("\"fireHazard\":").Append(JsonUtil.Number(district.GetFireHazard()));
                    json.Append(",\"crimeRate\":").Append(JsonUtil.Number(districtPark.m_crimeAccumulation));
                    json.Append(",\"crimeBuffer\":").Append(JsonUtil.Number(districtPark.m_crimeBuffer));
                    json.Append(",\"garbageAccumulation\":").Append(JsonUtil.Number(district.m_garbageData.m_garbageAccumulation));
                    json.Append(",\"garbageBuffer\":").Append(JsonUtil.Number(district.m_garbageData.m_garbageBuffer));
                    json.Append(",\"groundPollution\":").Append(JsonUtil.Number(district.m_groundPollutionData.m_pollutionAccumulation));
                    json.Append(",\"noisePollution\":").Append(JsonUtil.Number(district.m_noisePollutionData.m_noiseAccumulation));
                    json.Append(",\"sickCount\":").Append(district.m_sickCount);
                    json.Append(",\"deadCount\":").Append(district.m_deadCount);
                    json.Append(",\"population\":").Append((int)district.m_populationData.m_finalCount);
                    json.Append(",\"averageHealth\":").Append(JsonUtil.Number(GetAverageHealth(citizenManager)));
                    json.Append(",\"averageEducation\":").Append(JsonUtil.Number(GetAverageEducation(citizenManager)));
                    json.Append(",\"averageWealth\":").Append(JsonUtil.Number(GetAverageWealth(citizenManager)));
                }
                else
                {
                    json.Append("\"error\":\"No main district\"");
                }
            }
            json.Append("}");

            // --- Citizen statistics ---
            json.Append(",\"citizenStats\":{");
            int employed = 0, unemployed = 0, educated = 0, uneducated = 0, happyTotal = 0, happyCount = 0;
            int adults = 0, seniors = 0, children = 0, teens = 0, youngAdults = 0;
            int wealthy = 0, poor = 0;

            for (ushort i = 1; i < citizenManager.m_citizens.m_buffer.Length; i++)
            {
                Citizen citizen = citizenManager.m_citizens.m_buffer[i];
                if ((citizen.m_flags & Citizen.Flags.Created) == Citizen.Flags.None) continue;
                if (citizen.Dead) continue;

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

                // Wealth
                if (citizen.Wealth >= Citizen.Wealth.Medium) wealthy++;
                else if (citizen.Wealth <= Citizen.Wealth.Low) poor++;

                // Happiness (wellbeing)
                happyTotal += (int)citizen.m_wellbeing;
                happyCount++;
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
            json.Append(",\"averageWellbeing\":").Append(happyCount > 0 ? JsonUtil.Number((float)happyTotal / happyCount) : "0");
            json.Append(",\"totalCitizens\":").Append(citizenManager.m_citizenCount);
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
            return count > 0 ? (float)total / (count * 3f) : 0f; // normalized 0-1
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
