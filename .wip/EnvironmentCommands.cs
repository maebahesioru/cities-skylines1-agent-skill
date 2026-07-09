using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Environment, pollution, and natural resource state APIs.
    /// Reads ground/water/noise pollution, natural resources, terrain, and flooding.
    /// </summary>
    public static class EnvironmentCommands
    {
        public static CommandResult BuildEnvironmentJson()
        {
            DistrictManager districtManager = DistrictManager.instance;
            NaturalResourceManager resourceManager = Singleton<NaturalResourceManager>.instance;
            TerrainManager terrainManager = TerrainManager.instance;
            WaterManager waterManager = Singleton<WaterManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Main district pollution ---
            json.Append(",\"pollution\":{");
            if (districtManager != null)
            {
                District district = districtManager.m_districts.m_buffer[0];
                if ((district.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    json.Append("\"groundPollution\":").Append(JsonUtil.Number(district.m_groundPollutionData.m_pollutionAccumulation));
                    json.Append(",\"groundPollutionRate\":").Append(JsonUtil.Number(district.m_groundPollutionData.m_pollutionRate));
                    json.Append(",\"noisePollution\":").Append(JsonUtil.Number(district.m_noisePollutionData.m_noiseAccumulation));
                    json.Append(",\"noisePollutionRate\":").Append(JsonUtil.Number(district.m_noisePollutionData.m_noiseRate));
                }
            }
            json.Append("}");

            // --- Water simulation ---
            json.Append(",\"water\":{");
            if (waterManager != null)
            {
                json.Append("\"waterSources\":").Append(CountWaterSources(waterManager));
                json.Append(",\"sewageSources\":").Append(CountSewageSources(waterManager));
            }
            json.Append("}");

            // --- Natural resources ---
            json.Append(",\"naturalResources\":{");
            if (resourceManager != null)
            {
                int oil = 0, ore = 0, forest = 0, fertile = 0;
                int totalCells = 0;

                for (int i = 0; i < resourceManager.m_naturalResources.Length; i++)
                {
                    NaturalResourceManager.ResourceCell cell = resourceManager.m_naturalResources[i];
                    totalCells++;
                    if (cell.m_oil > 0) oil++;
                    if (cell.m_ore > 0) ore++;
                    if (cell.m_forest > 0) forest++;
                    if (cell.m_fertility > 0) fertile++;
                }

                json.Append("\"oilCellCount\":").Append(oil);
                json.Append(",\"oreCellCount\":").Append(ore);
                json.Append(",\"forestCellCount\":").Append(forest);
                json.Append(",\"fertileCellCount\":").Append(fertile);
                json.Append(",\"totalCells\":").Append(totalCells);
            }
            json.Append("}");

            // --- Terrain info ---
            json.Append(",\"terrain\":{");
            if (terrainManager != null)
            {
                // Sample terrain at the map center region
                float minHeight = float.MaxValue;
                float maxHeight = float.MinValue;
                float waterLevel = waterManager != null ? waterManager.m_waterLevel : 0f;
                int samples = 0;

                for (int x = 0; x < 500; x += 50)
                {
                    for (int z = 0; z < 500; z += 50)
                    {
                        Vector3 pos = new Vector3(x * 4f, 0f, z * 4f);
                        float height = terrainManager.SampleRawHeightSmooth(pos);
                        if (height < minHeight) minHeight = height;
                        if (height > maxHeight) maxHeight = height;
                        samples++;
                    }
                }

                json.Append("\"minHeight\":").Append(JsonUtil.Number(minHeight));
                json.Append(",\"maxHeight\":").Append(JsonUtil.Number(maxHeight));
                json.Append(",\"waterLevel\":").Append(JsonUtil.Number(waterLevel));
                json.Append(",\"floodableAreaPercent\":").Append(JsonUtil.Number(minHeight < waterLevel ? ((waterLevel - minHeight) / Math.Max(1f, maxHeight - minHeight)) * 100f : 0f));
                json.Append(",\"sampleCount\":").Append(samples);
                json.Append(",\"mapSize\":\"2km x 2km\"");
            }
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static int CountWaterSources(WaterManager wm)
        {
            int count = 0;
            for (ushort i = 1; i < wm.m_waterSources.m_buffer.Length && count < 200; i++)
            {
                if ((wm.m_waterSources.m_buffer[i].m_flags & 1) != 0) count++;
            }
            return count;
        }

        private static int CountSewageSources(WaterManager wm)
        {
            int count = 0;
            for (ushort i = 1; i < wm.m_sewageSources.m_buffer.Length && count < 200; i++)
            {
                if ((wm.m_sewageSources.m_buffer[i].m_flags & 1) != 0) count++;
            }
            return count;
        }

        /// <summary>
        /// Detect flooding by checking buildings below water level.
        /// </summary>
        public static CommandResult BuildFloodingJson(int limit)
        {
            if (limit < 0) limit = 0;
            if (limit > 500) limit = 500;

            WaterManager waterManager = Singleton<WaterManager>.instance;
            BuildingManager buildingManager = BuildingManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"floodedBuildings\":[");
            int emitted = 0;
            float waterLevel = waterManager != null ? waterManager.m_waterLevel : 0f;

            for (ushort i = 1; i < buildingManager.m_buildings.m_buffer.Length && emitted < limit; i++)
            {
                Building building = buildingManager.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None) continue;

                // Check if building is below water level
                if (building.m_position.y < waterLevel)
                {
                    if (emitted > 0) json.Append(",");
                    BuildingInfo info = building.Info;
                    json.Append("{\"id\":").Append(i);
                    json.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info != null ? info.name : "?")).Append("\"");
                    json.Append(",\"height\":").Append(JsonUtil.Number(building.m_position.y));
                    json.Append(",\"waterLevel\":").Append(JsonUtil.Number(waterLevel));
                    json.Append(",\"depthUnderWater\":").Append(JsonUtil.Number(waterLevel - building.m_position.y));
                    json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(building.m_position.x));
                    json.Append(",\"z\":").Append(JsonUtil.Number(building.m_position.z)).Append("}}");
                    emitted++;
                }
            }
            json.Append("],\"total\":" + emitted + ",\"waterLevel\":" + JsonUtil.Number(waterLevel) + "}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
