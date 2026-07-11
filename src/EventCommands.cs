using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Event state — concerts, sports events, festivals.
    /// Uses EventManager.instance fields verified via monodis.
    /// </summary>
    public static class EventCommands
    {
        public static CommandResult BuildEventsJson()
        {
            EventManager evm = EventManager.instance;
            if (evm == null) return CommandResult.Fail("EventManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"eventCount\":").Append(evm.m_eventCount);
            json.Append(",\"eventRouteCount\":").Append(evm.m_eventRouteCount);
            json.Append(",\"scheduleCount\":").Append(evm.m_eventScheduleCount);
            json.Append(",\"bandPopularityBonus\":").Append(evm.m_bandPopularityBonus);
            json.Append(",\"activeCount\":").Append(evm.m_eventCount);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }
    }
}
