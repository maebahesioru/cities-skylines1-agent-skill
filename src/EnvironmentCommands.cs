using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Environment and natural resource state APIs.
    /// Verified against CS1 Assembly-CSharp.dll decompiled fields.
    /// </summary>
    public static class EnvironmentCommands
    {
        public static CommandResult BuildEnvironmentJson()
        {
            DistrictManager dm = DistrictManager.instance;
            NaturalResourceManager rm = Singleton<NaturalResourceManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // District pollution
            if (dm != null && dm.m_districts.m_size > 0)
            {
                District d = dm.m_districts.m_buffer[0];
                if ((d.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    json.Append(",\"groundPollution\":" + d.m_groundData.m_finalPollution);
                    json.Append(",\"landValue\":" + d.m_groundData.m_finalLandvalue);
                    json.Append(",\"happiness\":" + d.m_finalHappiness);
                }
            }

            // Natural resources
            if (rm != null)
            {
                int oil = 0, ore = 0, forest = 0, fertile = 0;
                var resources = rm.m_naturalResources;
                int totalCells = resources != null ? resources.Length : 0;

                for (int i = 0; i < totalCells; i++)
                {
                    if (resources[i].m_oil > 0) oil++;
                    if (resources[i].m_ore > 0) ore++;
                    if (resources[i].m_forest > 0) forest++;
                    if (resources[i].m_fertility > 0) fertile++;
                }

                json.Append(",\"naturalResources\":{");
                json.Append("\"oilCells\":" + oil);
                json.Append(",\"oreCells\":" + ore);
                json.Append(",\"forestCells\":" + forest);
                json.Append(",\"fertileCells\":" + fertile);
                json.Append(",\"totalCells\":" + totalCells);
                json.Append("}");
            }

            // Terrain info
            TerrainManager tm = TerrainManager.instance;
            if (tm != null)
            {
                float minH = float.MaxValue, maxH = float.MinValue;
                for (int x = 0; x < 500; x += 50)
                    for (int z = 0; z < 500; z += 50)
                    {
                        float h = tm.SampleRawHeightSmooth(new Vector3(x * 4f, 0f, z * 4f));
                        if (h < minH) minH = h;
                        if (h > maxH) maxH = h;
                    }
                json.Append(",\"terrain\":{");
                json.Append("\"minHeight\":" + JsonUtil.Number(minH));
                json.Append(",\"maxHeight\":" + JsonUtil.Number(maxH));
                json.Append("}");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
