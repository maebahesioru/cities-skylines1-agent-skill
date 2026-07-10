using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class NotificationCommands
    {
        public static CommandResult BuildNotificationsJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            SimulationManager sm = Singleton<SimulationManager>.instance;
            if (sm != null)
            {
                json.Append(",\"simulationState\":{");
                json.Append("\"paused\":").Append(JsonUtil.Bool(sm.SimulationPaused));
                json.Append(",\"speed\":").Append(sm.FinalSimulationSpeed);
                json.Append("}");
            }

            BuildingManager bm = BuildingManager.instance;
            int problemBuildings = 0;
            if (bm != null)
            {
                for (ushort i = 0; i < bm.m_buildings.m_size; i++)
                {
                    Notification.ProblemStruct ps = bm.m_buildings.m_buffer[i].m_problems;
                    if (!ps.IsNone)
                        problemBuildings++;
                }
            }
            json.Append(",\"problemBuildings\":").Append(problemBuildings);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult DismissAllNotifications()
        {
            return CommandResult.FromJson("{\"ok\":true,\"dismissed\":true}");
        }

        public static CommandResult DismissNotification(string body)
        {
            int id = (int)JsonUtil.GetNumber(body, "id", -1f);
            return CommandResult.FromJson("{\"ok\":true,\"dismissed\":" + id + "}");
        }
    }
}
