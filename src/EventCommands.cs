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
            json.Append(",\"eventCount\":" + evm.m_eventCount);
            json.Append(",\"eventRouteCount\":" + evm.m_eventRouteCount);
            json.Append(",\"scheduleCount\":" + evm.m_eventScheduleCount);
            json.Append(",\"bandPopularityBonus\":" + evm.m_bandPopularityBonus);

            // List active events
            json.Append(",\"events\":[");
            int count = 0;
            if (evm.m_events != null)
            {
                var bufferField = typeof(FastList<>).GetField("m_buffer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var sizeField = typeof(FastList<>).GetField("m_size",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Array buffer = bufferField.GetValue(evm.m_events) as Array;
                int size = sizeField != null ? (int)sizeField.GetValue(evm.m_events) : 0;

                if (buffer != null)
                {
                    for (int i = 0; i < size && i < 50; i++)
                    {
                        object ed = buffer.GetValue(i);
                        if (ed == null) continue;

                        var flagsField = ed.GetType().GetField("m_flags");
                        int flags = flagsField != null ? (int)flagsField.GetValue(ed) : 0;
                        if (flags == 0) continue;

                        var infoField = ed.GetType().GetField("m_infoIndex");
                        int infoIdx = infoField != null ? (int)infoField.GetValue(ed) : -1;
                        EventInfo info = null;
                        if (infoIdx >= 0)
                            info = PrefabCollection<EventInfo>.GetLoaded((uint)infoIdx);

                        var posField = ed.GetType().GetField("m_position");
                        var attendeesField = ed.GetType().GetField("m_visitorCount");

                        if (count > 0) json.Append(",");
                        json.Append("{");
                        json.Append("\"id\":" + i);
                        json.Append(",\"name\":\"" + JsonUtil.Escape(info != null ? info.name : "Unknown") + "\"");
                        if (posField != null)
                        {
                            var pos = (UnityEngine.Vector3)posField.GetValue(ed);
                            json.Append(",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}");
                        }
                        if (attendeesField != null)
                            json.Append(",\"attendees\":" + (int)attendeesField.GetValue(ed));
                        json.Append("}");
                        count++;
                    }
                }
            }
            json.Append("],\"activeCount\":" + count);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }
    }
}
