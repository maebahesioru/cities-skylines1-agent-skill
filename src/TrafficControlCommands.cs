using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Traffic control: speed limits, road naming, junction toggles.
    /// </summary>
    public static class TrafficControlCommands
    {
        /// <summary>
        /// Get all named roads and their current names.
        /// </summary>
        public static CommandResult BuildRoadNamesJson(int limit)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (limit < 0) limit = 0;
            if (limit > 200) limit = 200;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"roads\":[");
            bool first = true;
            int emitted = 0;

            for (ushort i = 0; i < nm.m_segments.m_size; i++)
            {
                NetSegment seg = nm.m_segments.m_buffer[i];
                if (seg.m_flags == NetSegment.Flags.None) continue;

                string segName = nm.GetSegmentName(i);
                if (string.IsNullOrEmpty(segName)) continue;
                if (emitted >= limit) break;

                if (!first) json.Append(",");
                first = false;

                json.Append("{");
                json.Append("\"segmentId\":").Append(i);
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(segName)).Append("\"");
                json.Append(",\"startNode\":").Append(seg.m_startNode);
                json.Append(",\"endNode\":").Append(seg.m_endNode);
                json.Append("}");

                emitted++;
            }

            json.Append("]");
            json.Append(",\"total\":" + emitted);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Rename a road segment.
        /// Body: { "segmentId": ushort, "name": "New Name" }
        /// </summary>
        public static CommandResult RenameRoad(string body)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");

            ushort segId = (ushort)(int)JsonUtil.GetNumber(body, "segmentId", 0f);
            string newName = JsonUtil.GetString(body, "name", "");

            if (segId >= nm.m_segments.m_size)
                return CommandResult.Fail("Invalid segment ID: " + segId);

            NetSegment seg = nm.m_segments.m_buffer[segId];
            if (seg.m_flags == NetSegment.Flags.None)
                return CommandResult.Fail("Segment does not exist: " + segId);

            string oldName = nm.GetSegmentName(segId) ?? "";
            nm.SetSegmentName(segId, newName);

            return CommandResult.FromJson("{\"ok\":true,\"segmentId\":" + segId +
                ",\"oldName\":\"" + JsonUtil.Escape(oldName) +
                "\",\"newName\":\"" + JsonUtil.Escape(newName) + "\"}");
        }

        /// <summary>
        /// Get speed limits for roads.
        /// Returns per-segment info about speed limits.
        /// </summary>
        public static CommandResult BuildSpeedLimitsJson(int limit)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (limit < 0) limit = 0;
            if (limit > 200) limit = 200;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"segments\":[");
            bool first = true;
            int emitted = 0;

            for (ushort i = 0; i < nm.m_segments.m_size; i++)
            {
                NetSegment seg = nm.m_segments.m_buffer[i];
                if (seg.m_flags == NetSegment.Flags.None) continue;

                NetInfo info = seg.Info;
                if (info == null || info.m_netAI == null) continue;

                // Only include roads
                if (!(info.m_netAI is RoadBaseAI)) continue;

                if (emitted >= limit) break;
                if (!first) json.Append(",");
                first = false;

                json.Append("{");
                json.Append("\"segmentId\":").Append(i);
                json.Append(",\"speedLimit\":").Append(JsonUtil.Number(info.m_halfWidth));
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(nm.GetSegmentName(i) ?? "")).Append("\"");
                json.Append(",\"startNode\":").Append(seg.m_startNode);
                json.Append(",\"endNode\":").Append(seg.m_endNode);
                json.Append(",\"averageLength\":").Append(JsonUtil.Number(seg.m_averageLength));
                json.Append("}");

                emitted++;
            }

            json.Append("]");
            json.Append(",\"total\":" + emitted);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Get all junctions and their type (traffic lights, stop signs, yield).
        /// </summary>
        public static CommandResult BuildJunctionsJson(int limit)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (limit < 0) limit = 0;
            if (limit > 200) limit = 200;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"junctions\":[");
            bool first = true;
            int emitted = 0;

            for (ushort i = 0; i < nm.m_nodes.m_size; i++)
            {
                NetNode node = nm.m_nodes.m_buffer[i];
                if (node.m_flags == NetNode.Flags.None) continue;

                // Count connected segments - if >= 3 it's a junction
                int segCount = 0;
                for (int s = 0; s < 8; s++)
                {
                    if (node.GetSegment(s) != 0) segCount++;
                }

                if (segCount < 3) continue;
                if (emitted >= limit) break;

                if (!first) json.Append(",");
                first = false;

                // Check for traffic lights on connected segments
                bool hasTrafficLights = false;
                for (int s = 0; s < 8; s++)
                {
                    ushort segId = node.GetSegment(s);
                    if (segId != 0)
                    {
                        NetSegment seg = nm.m_segments.m_buffer[segId];
                        if ((seg.m_flags & (NetSegment.Flags)0x80) != NetSegment.Flags.None)
                        {
                            hasTrafficLights = true;
                            break;
                        }
                    }
                }

                json.Append("{");
                json.Append("\"nodeId\":").Append(i);
                json.Append(",\"connectedSegments\":" + segCount);
                json.Append(",\"hasTrafficLights\":").Append(JsonUtil.Bool(hasTrafficLights));
                json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(node.m_position.x));
                json.Append(",\"z\":").Append(JsonUtil.Number(node.m_position.z)).Append("}");
                json.Append("}");

                emitted++;
            }

            json.Append("]");
            json.Append(",\"total\":" + emitted);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }
    }
}
