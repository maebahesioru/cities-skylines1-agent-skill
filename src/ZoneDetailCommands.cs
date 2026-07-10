using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class ZoneDetailCommands
    {
        public static CommandResult BuildZoneMapJson(int sampleStep, int offsetX, int offsetZ, int width, int height)
        {
            ZoneManager zm = ZoneManager.instance;
            if (zm == null) return CommandResult.Fail("ZoneManager is not available.");
            if (sampleStep < 1) sampleStep = 8;
            if (width < 1 || width > 200) width = 100;
            if (height < 1 || height > 200) height = 100;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"cells\":[");
            bool first = true;

            for (int z = offsetZ; z < offsetZ + height && z < ZoneManager.ZONEGRID_RESOLUTION; z += sampleStep)
            {
                for (int x = offsetX; x < offsetX + width && x < ZoneManager.ZONEGRID_RESOLUTION; x += sampleStep)
                {
                    int zoneVal = (int)zm.m_zoneGrid[z * ZoneManager.ZONEGRID_RESOLUTION + x];
                    if (zoneVal == 0) continue;
                    if (!first) json.Append(",");
                    first = false;
                    json.Append("{\"x\":").Append(x);
                    json.Append(",\"z\":").Append(z);
                    json.Append(",\"zone\":").Append(zoneVal);
                    json.Append("}");
                }
            }

            json.Append("]");
            json.Append(",\"sampleStep\":" + sampleStep);
            json.Append(",\"gridSize\":" + ZoneManager.ZONEGRID_RESOLUTION);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }
    }
}
