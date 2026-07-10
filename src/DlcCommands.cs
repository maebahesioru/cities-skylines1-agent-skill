using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class DlcCommands
    {
        // === AIRPORTS ===
        public static CommandResult BuildAirportJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (dm != null)
            {
                json.Append(",\"airportAreas\":[");
                bool first = true;
                for (int i = 0; i < dm.m_parks.m_size; i++)
                {
                    DistrictPark park = dm.m_parks.m_buffer[i];
                    if (park.m_flags == 0 || !park.IsAirport) continue;
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("{\"id\":").Append(i);
                    json.Append(",\"isFunctioning\":").Append(JsonUtil.Bool(park.isFunctioning));
                    json.Append(",\"passengers\":" + park.totalPassengerCount);
                    json.Append(",\"airplaneFullness\":").Append(JsonUtil.Number(park.airplaneFullness));
                    json.Append(",\"cargoFlow\":" + park.totalCargoFlow);
                    json.Append("}");
                }
                json.Append("]");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === INDUSTRIES SUPPLY CHAIN ===
        public static CommandResult BuildSupplyChainJson()
        {
            BuildingManager bm = BuildingManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (bm != null)
            {
                int extractors = 0, warehouses = 0, processors = 0, factories = 0;
                for (ushort i = 0; i < bm.m_buildings.m_size; i++)
                {
                    Building b = bm.m_buildings.m_buffer[i];
                    if (b.m_flags == Building.Flags.None || b.Info == null || b.Info.m_buildingAI == null) continue;
                    BuildingAI ai = b.Info.m_buildingAI;
                    if (ai is IndustrialExtractorAI) extractors++;
                    else if (ai is ExtractingFacilityAI) warehouses++;
                    else if (ai is UniqueFactoryAI) factories++;
                    else
                    {
                        string an = ai.GetType().Name;
                        if (an.Contains("Processing")) processors++;
                    }
                }
                json.Append(",\"industry\":{");
                json.Append("\"extractors\":" + extractors);
                json.Append(",\"warehouses\":" + warehouses);
                json.Append(",\"processors\":" + processors);
                json.Append(",\"uniqueFactories\":" + factories);
                json.Append("}");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === CAMPUS ===
        public static CommandResult BuildCampusDetailJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (dm != null)
            {
                json.Append(",\"campuses\":[");
                bool first = true;
                for (int i = 0; i < dm.m_parks.m_size; i++)
                {
                    DistrictPark park = dm.m_parks.m_buffer[i];
                    if (park.m_flags == 0 || !park.IsCampus) continue;
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("{\"id\":").Append(i);
                    json.Append(",\"isFunctioning\":").Append(JsonUtil.Bool(park.isFunctioning));
                    json.Append(",\"students\":" + park.totalResidentCount);
                    json.Append(",\"workers\":" + park.totalWorkerCount);
                    json.Append(",\"happiness\":").Append(JsonUtil.Number(park.normalizedAverageHappiness));
                    json.Append("}");
                }
                json.Append("]");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === PARKLIFE ===
        public static CommandResult BuildParkDetailJson()
        {
            DistrictManager dm = DistrictManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (dm != null)
            {
                json.Append(",\"parks\":[");
                bool first = true;
                for (int i = 0; i < dm.m_parks.m_size; i++)
                {
                    DistrictPark park = dm.m_parks.m_buffer[i];
                    if (park.m_flags == 0 || !park.IsPark) continue;
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("{\"id\":").Append(i);
                    json.Append(",\"isFunctioning\":").Append(JsonUtil.Bool(park.isFunctioning));
                    json.Append(",\"visitors\":" + park.currentWeeklyVisitors);
                    json.Append(",\"tourists\":" + park.weeklyTouristCount);
                    json.Append(",\"entertainment\":" + park.entertainmentPerCell);
                    json.Append(",\"landValue\":" + park.landValueBonus);
                    json.Append(",\"happiness\":").Append(JsonUtil.Number(park.normalizedAverageHappiness));
                    json.Append(",\"size\":").Append(JsonUtil.Number(park.sizeInCells));
                    json.Append(",\"ticketPrice\":" + park.m_ticketPrice);
                    json.Append(",\"goodsSold\":" + park.weeklyGoodsSold);
                    json.Append("}");
                }
                json.Append("]");
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        // === MISC ===
        public static CommandResult BuildGuidesJson()
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"GuideManager API limited.\"}");
        }

        public static CommandResult BuildToolJson()
        {
            ToolManager tlm = Singleton<ToolManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            if (tlm != null && tlm.m_properties != null)
            {
                json.Append(",\"mode\":").Append((int)tlm.m_properties.m_mode);
            }
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildEffectsJson()
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"EffectManager API limited.\"}");
        }
    }
}
