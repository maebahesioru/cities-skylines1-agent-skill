using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Natural resource state and manipulation APIs.
    /// Covers ore, oil, forest, and fertility resources across the map.
    /// Uses verified CS1 NaturalResourceManager fields.
    /// </summary>
    public static class NaturalResourceCommands
    {
        public static CommandResult BuildResourcesJson()
        {
            NaturalResourceManager rm = Singleton<NaturalResourceManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (rm != null)
            {
                int oil = 0, ore = 0, forest = 0, fertile = 0;
                float totalOil = 0f, totalOre = 0f, totalForest = 0f, totalFertile = 0f;
                var resources = rm.m_naturalResources;
                int totalCells = resources != null ? resources.Length : 0;

                for (int i = 0; i < totalCells; i++)
                {
                    if (resources[i].m_oil > 0) { oil++; totalOil += resources[i].m_oil; }
                    if (resources[i].m_ore > 0) { ore++; totalOre += resources[i].m_ore; }
                    if (resources[i].m_forest > 0) { forest++; totalForest += resources[i].m_forest; }
                    if (resources[i].m_fertility > 0) { fertile++; totalFertile += resources[i].m_fertility; }
                }

                // Get extraction stats from industries
                DistrictManager dm = DistrictManager.instance;
                int oilExt = 0, oreExt = 0, forestExt = 0, farmExt = 0;
                if (dm != null)
                {
                    for (int i = 0; i < dm.m_districts.m_size; i++)
                    {
                        District d = dm.m_districts.m_buffer[i];
                        if ((d.m_flags & District.Flags.Created) == District.Flags.None) continue;
                        if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Oil) != 0) oilExt++;
                        if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Ore) != 0) oreExt++;
                        if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Forest) != 0) forestExt++;
                        if ((d.m_specializationPolicies & DistrictPolicies.Specialization.Farming) != 0) farmExt++;
                    }
                }

                json.Append(",\"resources\":{");
                json.Append("\"oil\":{\"cells\":" + oil + ",\"totalAmount\":" + JsonUtil.Number(totalOil) + ",\"extractors\":" + oilExt + "}");
                json.Append(",\"ore\":{\"cells\":" + ore + ",\"totalAmount\":" + JsonUtil.Number(totalOre) + ",\"extractors\":" + oreExt + "}");
                json.Append(",\"forest\":{\"cells\":" + forest + ",\"totalAmount\":" + JsonUtil.Number(totalForest) + ",\"extractors\":" + forestExt + "}");
                json.Append(",\"fertility\":{\"cells\":" + fertile + ",\"totalAmount\":" + JsonUtil.Number(totalFertile) + ",\"extractors\":" + farmExt + "}");
                json.Append("}");
                json.Append(",\"totalCells\":" + totalCells);
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set natural resource value at a position.
        /// Body: { "type": "oil"|"ore"|"forest"|"fertility", "x": float, "z": float, "value": byte(0-255), "radius": int }
        /// </summary>
        public static CommandResult SetResource(string body)
        {
            NaturalResourceManager rm = Singleton<NaturalResourceManager>.instance;
            if (rm == null) return CommandResult.Fail("NaturalResourceManager is not available.");

            string type = JsonUtil.GetString(body, "type", "").ToLowerInvariant();
            float x = JsonUtil.GetNumber(body, "x", 0f);
            float z = JsonUtil.GetNumber(body, "z", 0f);
            byte value = (byte)Math.Min(255, Math.Max(0, (int)JsonUtil.GetNumber(body, "value", 0f)));
            int radius = Math.Max(0, (int)JsonUtil.GetNumber(body, "radius", 0f));

            if (!(type == "oil" || type == "ore" || type == "forest" || type == "fertility"))
                return CommandResult.Fail("Unknown resource type. Use: oil, ore, forest, fertility");

            // Natural resource grid: map is 1081 x 1081 vertices, resources at ~64-unit spacing
            int gridSize = (int)Math.Sqrt(rm.m_naturalResources.Length);
            float cellSize = 1081f * 4f / gridSize; // approximate world units per cell

            int cx = (int)(x / cellSize);
            int cz = (int)(z / cellSize);
            int cells = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int tx = cx + dx;
                    int tz = cz + dz;
                    if (tx >= 0 && tz >= 0 && tx < gridSize && tz < gridSize)
                    {
                        int index = tz * gridSize + tx;
                        if (index < rm.m_naturalResources.Length)
                        {
                            switch (type)
                            {
                                case "oil": rm.m_naturalResources[index].m_oil = value; break;
                                case "ore": rm.m_naturalResources[index].m_ore = value; break;
                                case "forest": rm.m_naturalResources[index].m_forest = value; break;
                                case "fertility": rm.m_naturalResources[index].m_fertility = value; break;
                            }
                            cells++;
                        }
                    }
                }
            }

            return CommandResult.FromJson("{\"ok\":true,\"type\":\"" + JsonUtil.Escape(type) +
                "\",\"cellsModified\":" + cells + ",\"value\":" + value + "}");
        }
    }
}
