using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Citizen demographics — age, education, health distributions.
    /// Uses CitizenManager.instance + Citizen struct fields.
    /// </summary>
    public static class CitizenCommands
    {
        public static CommandResult BuildCitizensJson()
        {
            CitizenManager cm = CitizenManager.instance;
            if (cm == null) return CommandResult.Fail("CitizenManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"citizenCount\":" + cm.m_citizenCount);
            json.Append(",\"unitCount\":" + cm.m_unitCount);
            json.Append(",\"instanceCount\":" + cm.m_instanceCount);

            int sampled = 0;
            int children = 0, teens = 0, youngAdults = 0, adults = 0, seniors = 0;
            int uneducated = 0, educated = 0, wellEducated = 0, highlyEducated = 0;
            int sick = 0, dead = 0;
            int employed = 0, unemployed = 0;

            Citizen[] buffer = cm.m_citizens.m_buffer;
            int size = (int)(cm.m_citizens.m_size);

            if (buffer != null)
            {
                for (int i = 1; i < size && sampled < 5000; i++)
                {
                    Citizen c = buffer[i];
                    if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None) continue;
                    sampled++;

                    // Age group
                    int age = (int)c.Age;
                    if (age < 30) children++;
                    else if (age < 60) teens++;
                    else if (age < 120) youngAdults++;
                    else if (age < 180) adults++;
                    else seniors++;

                    // Education
                    switch (c.EducationLevel)
                    {
                        case Citizen.Education.Uneducated: uneducated++; break;
                        case Citizen.Education.OneSchool: educated++; break;
                        case Citizen.Education.TwoSchools: wellEducated++; break;
                        case Citizen.Education.ThreeSchools: highlyEducated++; break;
                    }

                    // Health
                    if ((c.m_flags & Citizen.Flags.Sick) != Citizen.Flags.None) sick++;
                    if (c.Dead) dead++;

                    // Employment
                    if (c.m_workBuilding != 0) employed++;
                    else if (age >= 120 && age < 240) unemployed++;
                }
            }

            json.Append(",\"sampleSize\":" + sampled);
            json.Append(",\"ageDistribution\":{");
            json.Append("\"children\":" + children);
            json.Append(",\"teens\":" + teens);
            json.Append(",\"youngAdults\":" + youngAdults);
            json.Append(",\"adults\":" + adults);
            json.Append(",\"seniors\":" + seniors);
            json.Append("}");
            json.Append(",\"educationDistribution\":{");
            json.Append("\"uneducated\":" + uneducated);
            json.Append(",\"educated\":" + educated);
            json.Append(",\"wellEducated\":" + wellEducated);
            json.Append(",\"highlyEducated\":" + highlyEducated);
            json.Append("}");
            json.Append(",\"health\":{");
            json.Append("\"sick\":" + sick);
            json.Append(",\"dead\":" + dead);
            json.Append("}");
            json.Append(",\"employment\":{");
            json.Append("\"employed\":" + employed);
            json.Append(",\"unemployed\":" + unemployed);
            json.Append(",\"rate\":" + JsonUtil.Number((float)(sampled > 0 ? (double)employed / (employed + unemployed + 0.001) : 0)));
            json.Append("}");

            DistrictManager dm = DistrictManager.instance;
            if (dm != null && dm.m_districts.m_size > 0)
            {
                District d = dm.m_districts.m_buffer[0];
                json.Append(",\"happiness\":" + d.m_finalHappiness);
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
