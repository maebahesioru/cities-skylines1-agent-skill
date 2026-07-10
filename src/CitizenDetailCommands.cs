using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class CitizenDetailCommands
    {
        public static CommandResult BuildCitizenDetail(string query)
        {
            uint citizenId = 0;
            if (query != null && query.Length > 0)
            {
                string[] pairs = query.Split('&');
                foreach (string pair in pairs)
                {
                    string[] kv = pair.Split('=');
                    if (kv.Length == 2 && kv[0] == "id")
                    {
                        uint.TryParse(kv[1], out citizenId);
                        break;
                    }
                }
            }

            CitizenManager cm = CitizenManager.instance;
            if (cm == null) return CommandResult.Fail("CitizenManager is not available.");
            if (citizenId >= cm.m_citizens.m_size) return CommandResult.Fail("Invalid citizen ID: " + citizenId);

            Citizen c = cm.m_citizens.m_buffer[citizenId];
            if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None)
                return CommandResult.Fail("Citizen does not exist: " + citizenId);

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"id\":").Append(citizenId);

            // Flags & status
            json.Append(",\"flags\":").Append((int)c.m_flags);
            json.Append(",\"dead\":").Append(JsonUtil.Bool(c.Dead));
            json.Append(",\"arrested\":").Append(JsonUtil.Bool(c.Arrested));
            json.Append(",\"sick\":").Append(JsonUtil.Bool(c.Sick));
            json.Append(",\"criminal\":").Append(JsonUtil.Bool(c.Criminal));
            json.Append(",\"collapsed\":").Append(JsonUtil.Bool(c.Collapsed));

            // Demographics
            json.Append(",\"age\":").Append(c.Age);
            json.Append(",\"education1\":").Append(JsonUtil.Bool(c.Education1));
            json.Append(",\"education2\":").Append(JsonUtil.Bool(c.Education2));
            json.Append(",\"education3\":").Append(JsonUtil.Bool(c.Education3));
            json.Append(",\"wealthLevel\":").Append((int)c.WealthLevel);
            json.Append(",\"educationLevel\":").Append((int)c.EducationLevel);

            // Status counters
            json.Append(",\"unemployed\":" + c.Unemployed);
            json.Append(",\"badHealth\":" + c.BadHealth);
            json.Append(",\"noElectricity\":" + c.NoElectricity);
            json.Append(",\"noWater\":" + c.NoWater);
            json.Append(",\"noSewage\":" + c.NoSewage);

            // Buildings
            json.Append(",\"homeBuilding\":").Append(c.m_homeBuilding);
            json.Append(",\"workBuilding\":").Append(c.m_workBuilding);
            json.Append(",\"visitBuilding\":").Append(c.m_visitBuilding);
            json.Append(",\"hotelBuilding\":").Append(c.m_hotelBuilding);

            // Transport
            json.Append(",\"vehicle\":").Append(c.m_vehicle);
            json.Append(",\"parkedVehicle\":").Append(c.m_parkedVehicle);
            json.Append(",\"instance\":").Append(c.m_instance);

            // Location
            json.Append(",\"location\":").Append((int)c.CurrentLocation);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildCitizensSearchJson(int limit, string employed, string ageFilter, string educationFilter)
        {
            CitizenManager cm = CitizenManager.instance;
            if (cm == null) return CommandResult.Fail("CitizenManager is not available.");
            if (limit < 0 || limit > 500) limit = 100;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"citizens\":[");
            bool first = true;
            int matched = 0;
            int totalChecked = 0;

            bool filterEmployed = employed != null && employed.Length > 0 && employed != "false";
            bool filterAge = ageFilter != null && ageFilter.Length > 0;
            bool filterEducation = educationFilter != null && educationFilter.Length > 0;

            for (uint i = 0; i < cm.m_citizens.m_size && matched < limit; i++)
            {
                Citizen c = cm.m_citizens.m_buffer[i];
                if ((c.m_flags & Citizen.Flags.Created) == Citizen.Flags.None) continue;
                if (c.Dead) continue;
                totalChecked++;

                if (filterEmployed && c.m_workBuilding == 0) continue;
                if (filterAge)
                {
                    int age = c.Age;
                    if (ageFilter == "child" && age > 30) continue;
                    if (ageFilter == "teen" && (age < 31 || age > 65)) continue;
                    if (ageFilter == "young" && (age < 66 || age > 130)) continue;
                    if (ageFilter == "adult" && (age < 131 || age > 200)) continue;
                    if (ageFilter == "senior" && age < 201) continue;
                }
                if (filterEducation)
                {
                    if (educationFilter == "uneducated" && c.Education1) continue;
                    if (educationFilter == "educated" && !c.Education2 && !c.Education3) continue;
                    if (educationFilter == "well" && !c.Education3) continue;
                }

                if (!first) json.Append(",");
                first = false;
                json.Append("{\"id\":").Append(i);
                json.Append(",\"age\":").Append(c.Age);
                json.Append(",\"homeBuilding\":").Append(c.m_homeBuilding);
                json.Append(",\"workBuilding\":").Append(c.m_workBuilding);
                json.Append(",\"wealthLevel\":").Append((int)c.WealthLevel);
                json.Append(",\"educationLevel\":").Append((int)c.EducationLevel);
                json.Append(",\"sick\":").Append(JsonUtil.Bool(c.Sick));
                json.Append("}");
                matched++;
            }

            json.Append("]");
            json.Append(",\"matched\":" + matched);
            json.Append(",\"totalChecked\":" + totalChecked);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }
    }
}
