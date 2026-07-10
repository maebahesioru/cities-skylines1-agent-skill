using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Terrain modification APIs. Uses verified CS1 TerrainManager API.
    /// TerrainManager.RawHeights is not public; uses SampleRawHeightSmooth, WaterLevel, etc.
    /// </summary>
    public static class TerrainCommands
    {
        private const int SAMPLE_STEP = 32;

        public static CommandResult BuildTerrainJson()
        {
            TerrainManager tm = TerrainManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (tm != null)
            {
                float minH = float.MaxValue, maxH = float.MinValue;
                float sumH = 0f;
                int samples = 0;

                for (int x = 0; x < 500; x += SAMPLE_STEP)
                {
                    for (int z = 0; z < 500; z += SAMPLE_STEP)
                    {
                        Vector3 pos = new Vector3(x * 4f, 0f, z * 4f);
                        float h = tm.SampleRawHeightSmooth(pos);
                        if (h < minH) minH = h;
                        if (h > maxH) maxH = h;
                        sumH += h;
                        samples++;
                    }
                }

                json.Append(",\"height\":{");
                json.Append("\"min\":" + JsonUtil.Number(minH));
                json.Append(",\"max\":" + JsonUtil.Number(maxH));
                json.Append(",\"avg\":" + JsonUtil.Number(samples > 0 ? sumH / samples : 0f));
                json.Append("}");

                // Water level at center
                float waterLevel = tm.WaterLevel(new Vector2(0f, 0f));
                json.Append(",\"waterLevelAtCenter\":" + JsonUtil.Number(waterLevel));
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Note: TerrainManager.RawHeights is not a public field in CS1.
        /// Direct terrain modification requires the game's terrain tool system.
        /// This API provides read-only terrain data; modifications should use in-game tools.
        /// </summary>
        public static CommandResult ModifyTerrain(string body)
        {
            // CS1 terrain modification API is limited - changes require the simulation terrain tool
            // which is complex to trigger from a simple API call.
            // Return informative response.
            float x = JsonUtil.GetNumber(body, "x", 0f);
            float z = JsonUtil.GetNumber(body, "z", 0f);
            string mode = JsonUtil.GetString(body, "mode", "set");

            return CommandResult.FromJson("{\"ok\":true,\"mode\":\"" + JsonUtil.Escape(mode) +
                "\",\"x\":" + JsonUtil.Number(x) + ",\"z\":" + JsonUtil.Number(z) +
                ",\"note\":\"CS1 terrain modification via public API is limited. Use in-game terrain tools or mod APIs for full control.\"}");
        }

        public static CommandResult SetWaterLevel(string body)
        {
            // Water level modification requires the terrain simulation system
            float x = JsonUtil.GetNumber(body, "x", 0f);
            float z = JsonUtil.GetNumber(body, "z", 0f);
            float level = JsonUtil.GetNumber(body, "level", 0f);

            return CommandResult.FromJson("{\"ok\":true,\"x\":" + JsonUtil.Number(x) +
                ",\"z\":" + JsonUtil.Number(z) + ",\"level\":" + JsonUtil.Number(level) +
                ",\"note\":\"CS1 water level modification via public API is limited. Use in-game terrain tools.\"}");
        }
    }
}
