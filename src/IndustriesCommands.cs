using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Industry / Park / Campus / District Style management APIs.
    /// Uses verified CS1 Assembly-CSharp field names and getter properties.
    /// </summary>
    public static class IndustriesCommands
    {
        // === INDUSTRY AREAS (District with specialization) ===

        public static CommandResult BuildIndustryAreasJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (dm != null)
            {
                json.Append(",\"industryAreas\":[");
                bool first = true;

                for (int i = 0; i < dm.m_districts.m_size; i++)
                {
                    District d = dm.m_districts.m_buffer[i];
                    if ((d.m_flags & District.Flags.Created) == District.Flags.None) continue;

                    if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Oil) != 0 ||
                        (d.m_specializationPolicies & DistrictPolicies.Specialization.Ore) != 0 ||
                        (d.m_specializationPolicies & DistrictPolicies.Specialization.Forest) != 0 ||
                        (d.m_specializationPolicies & DistrictPolicies.Specialization.Farming) != 0)
                    {
                        if (!first) json.Append(",");
                        first = false;

                        json.Append("{");
                        json.Append("\"id\":" + i);
                        json.Append(",\"name\":\"District " + i + "\"");

                        string areaType = "GenericIndustry";
                        if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Oil) != 0) areaType = "Oil";
                        else if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Ore) != 0) areaType = "Ore";
                        else if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Forest) != 0) areaType = "Forest";
                        else if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Farming) != 0) areaType = "Farming";

                        json.Append(",\"type\":\"" + areaType + "\"");
                        json.Append(",\"happiness\":" + d.m_finalHappiness);
                        json.Append(",\"population\":" + (int)d.m_populationData.m_finalCount);
                        json.Append("}");
                    }
                }

                json.Append("]");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult SetIndustryType(string body)
        {
            DistrictManager dm = DistrictManager.instance;
            if (dm == null) return CommandResult.Fail("DistrictManager is not available.");

            int districtId = (int)JsonUtil.GetNumber(body, "districtId", -1f);
            if (districtId < 0 || districtId >= dm.m_districts.m_size)
                return CommandResult.Fail("Invalid district ID: " + districtId);

            District d = dm.m_districts.m_buffer[districtId];
            if ((d.m_flags & District.Flags.Created) == District.Flags.None)
                return CommandResult.Fail("District does not exist: " + districtId);

            string type = JsonUtil.GetString(body, "type", "").ToLowerInvariant();

            // Clear existing specializations
            d.m_specializationPolicies &= ~(DistrictPolicies.Specialization.Oil |
                                            DistrictPolicies.Specialization.Ore |
                                            DistrictPolicies.Specialization.Forest |
                                            DistrictPolicies.Specialization.Farming);

            switch (type)
            {
                case "oil":
                    d.m_specializationPolicies |= DistrictPolicies.Specialization.Oil;
                    break;
                case "ore":
                    d.m_specializationPolicies |= DistrictPolicies.Specialization.Ore;
                    break;
                case "forest":
                    d.m_specializationPolicies |= DistrictPolicies.Specialization.Forest;
                    break;
                case "farming":
                    d.m_specializationPolicies |= DistrictPolicies.Specialization.Farming;
                    break;
                case "generic":
                    break;
                default:
                    return CommandResult.Fail("Unknown industry type. Use: Oil, Ore, Forest, Farming, Generic");
            }

            return CommandResult.FromJson("{\"ok\":true,\"districtId\":" + districtId +
                ",\"type\":\"" + JsonUtil.Escape(type) + "\"}");
        }

        // === PARK AREAS (Parklife DLC) ===

        public static CommandResult BuildParkAreasJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (dm != null)
            {
                json.Append(",\"parkAreas\":[");
                bool first = true;

                for (int i = 0; i < dm.m_parks.m_size; i++)
                {
                    DistrictPark park = dm.m_parks.m_buffer[i];
                    if (park.m_flags == 0) continue;
                    if (!park.IsPark) continue;

                    if (!first) json.Append(",");
                    first = false;

                    json.Append("{");
                    json.Append("\"id\":" + i);
                    json.Append(",\"isFunctioning\":" + JsonUtil.Bool(park.isFunctioning));
                    json.Append(",\"isConstructionCompleted\":" + JsonUtil.Bool(park.isConstructionCompleted));
                    json.Append(",\"group\":" + (int)park.Group);
                    json.Append(",\"mainGate\":" + park.m_mainGate);
                    json.Append(",\"weeklyVisitors\":" + park.currentWeeklyVisitors);
                    json.Append(",\"weeklyTourists\":" + park.weeklyTouristCount);
                    json.Append(",\"entertainmentPerCell\":" + park.entertainmentPerCell);
                    json.Append(",\"landValueBonus\":" + park.landValueBonus);
                    json.Append(",\"happiness\":" + JsonUtil.Number(park.normalizedAverageHappiness));
                    json.Append(",\"sizeInCells\":" + JsonUtil.Number(park.sizeInCells));
                    json.Append(",\"ticketPrice\":" + park.m_ticketPrice);
                    json.Append("}");
                }

                json.Append("]");
                json.Append(",\"totalParks\":" + dm.m_parks.m_size);
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult SetParkBudget(string body)
        {
            DistrictManager dm = DistrictManager.instance;
            if (dm == null) return CommandResult.Fail("DistrictManager is not available.");

            int parkId = (int)JsonUtil.GetNumber(body, "parkId", -1f);
            if (parkId < 0 || parkId >= dm.m_parks.m_size)
                return CommandResult.Fail("Invalid park ID: " + parkId);

            DistrictPark park = dm.m_parks.m_buffer[parkId];
            if (park.m_flags == 0)
                return CommandResult.Fail("Park does not exist: " + parkId);

            ushort oldPrice = park.m_ticketPrice;
            ushort newPrice = (ushort)Math.Max(0, Math.Min(1000, (int)JsonUtil.GetNumber(body, "ticketPrice", (float)oldPrice)));
            park.m_ticketPrice = newPrice;

            return CommandResult.FromJson("{\"ok\":true,\"parkId\":" + parkId +
                ",\"oldTicketPrice\":" + oldPrice + ",\"newTicketPrice\":" + newPrice + "}");
        }

        // === CAMPUS AREAS (Campus DLC) ===

        public static CommandResult BuildCampusAreasJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (dm != null)
            {
                json.Append(",\"campusAreas\":[");
                bool first = true;

                for (int i = 0; i < dm.m_parks.m_size; i++)
                {
                    DistrictPark park = dm.m_parks.m_buffer[i];
                    if (park.m_flags == 0) continue;
                    if (!park.IsCampus) continue;

                    if (!first) json.Append(",");
                    first = false;

                    json.Append("{");
                    json.Append("\"id\":" + i);
                    json.Append(",\"isFunctioning\":" + JsonUtil.Bool(park.isFunctioning));
                    json.Append(",\"mainGate\":" + park.m_mainGate);
                    json.Append(",\"group\":" + (int)park.Group);
                    json.Append(",\"residents\":" + park.totalResidentCount);
                    json.Append(",\"workers\":" + park.totalWorkerCount);
                    json.Append(",\"happiness\":" + JsonUtil.Number(park.normalizedAverageHappiness));
                    json.Append("}");
                }

                json.Append("]");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === DISTRICT STYLES ===

        public static CommandResult BuildDistrictStylesJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (dm != null)
            {
                json.Append(",\"districts\":[");
                bool first = true;

                for (int i = 0; i < dm.m_districts.m_size; i++)
                {
                    District d = dm.m_districts.m_buffer[i];
                    if ((d.m_flags & District.Flags.Created) == District.Flags.None) continue;

                    if (!first) json.Append(",");
                    first = false;

                    json.Append("{");
                    json.Append("\"id\":" + i);
                    json.Append(",\"name\":\"District " + i + "\"");
                    json.Append(",\"happiness\":" + d.m_finalHappiness);
                    json.Append(",\"population\":" + (int)d.m_populationData.m_finalCount);
                    json.Append("}");
                }

                json.Append("]");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult SetDistrictStyle(string body)
        {
            // Note: District.m_style/m_styleVariation are not public fields in CS1.
            // District styles are managed via the DistrictManager's district styles system.
            // This is a stub that validates the district exists.
            DistrictManager dm = DistrictManager.instance;
            if (dm == null) return CommandResult.Fail("DistrictManager is not available.");

            int districtId = (int)JsonUtil.GetNumber(body, "districtId", -1f);
            if (districtId < 0 || districtId >= dm.m_districts.m_size)
                return CommandResult.Fail("Invalid district ID: " + districtId);

            District d = dm.m_districts.m_buffer[districtId];
            if ((d.m_flags & District.Flags.Created) == District.Flags.None)
                return CommandResult.Fail("District does not exist: " + districtId);

            // District style modification is limited in CS1 public API
            return CommandResult.FromJson("{\"ok\":true,\"districtId\":" + districtId +
                ",\"note\":\"District style changes via public API are limited in CS1. Use in-game tools for full control.\"}");
        }
    }
}
