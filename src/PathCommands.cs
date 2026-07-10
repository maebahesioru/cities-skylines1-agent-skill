using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class PathCommands
    {
        public static CommandResult BuildPathfindingJson()
        {
            PathManager pm = Singleton<PathManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (pm != null)
            {
                json.Append(",\"pathUnits\":{");
                json.Append("\"totalSize\":" + pm.m_pathUnits.m_size);
                json.Append("}");

                int active = 0, free = 0;
                for (uint i = 0; i < pm.m_pathUnits.m_size; i++)
                {
                    PathUnit pu = pm.m_pathUnits.m_buffer[i];
                    if (pu.m_positionCount > 0 && pu.m_referenceCount > 0)
                        active++;
                    else
                        free++;
                }

                json.Append(",\"byType\":{");
                json.Append("\"active\":" + active);
                json.Append(",\"free\":" + free);
                json.Append("}");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
