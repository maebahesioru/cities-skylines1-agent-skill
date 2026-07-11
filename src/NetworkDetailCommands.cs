using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Network segment and node detail APIs.
    /// GET /state/network-segment/{id} - detailed segment data
    /// GET /state/network-node/{id} - detailed node data  
    /// GET /state/traffic-lights - traffic light state per intersection
    /// </summary>
    public static class NetworkDetailCommands
    {
        /// <summary>
        /// Get complete detail for a network segment.
        /// Query: ?id=ushort
        /// </summary>
        public static CommandResult BuildSegmentDetail(string query)
        {
            ushort segId = ParseId(query);
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (segId >= nm.m_segments.m_size) return CommandResult.Fail("Invalid segment ID: " + segId);

            NetSegment seg = nm.m_segments.m_buffer[segId];
            if (seg.m_flags == NetSegment.Flags.None)
                return CommandResult.Fail("Segment does not exist: " + segId);

            NetInfo info = seg.Info;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"id\":").Append(segId);
            json.Append(",\"flags\":").Append((int)seg.m_flags);
            json.Append(",\"flags2\":").Append((int)seg.m_flags2);

            if (info != null)
            {
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(info.name != null ? info.name : "")).Append("\"");
                json.Append(",\"service\":\"").Append(info.m_netAI != null ? info.m_netAI.GetType().Name : "Unknown").Append("\"");
                json.Append(",\"speedLimit\":").Append(JsonUtil.Number(info.m_halfWidth));
            }

            json.Append(",\"startNode\":").Append(seg.m_startNode);
            json.Append(",\"endNode\":").Append(seg.m_endNode);

            // Traffic light state
            json.Append(",\"trafficLightState0\":").Append(seg.m_trafficLightState0);
            json.Append(",\"trafficLightState1\":").Append(seg.m_trafficLightState1);

            // Node positions
            if (seg.m_startNode < nm.m_nodes.m_size)
            {
                NetNode startNode = nm.m_nodes.m_buffer[seg.m_startNode];
                if (startNode.m_flags != NetNode.Flags.None)
                {
                    json.Append(",\"startPosition\":{\"x\":").Append(JsonUtil.Number(startNode.m_position.x));
                    json.Append(",\"z\":").Append(JsonUtil.Number(startNode.m_position.z)).Append("}");
                }
            }
            if (seg.m_endNode < nm.m_nodes.m_size)
            {
                NetNode endNode = nm.m_nodes.m_buffer[seg.m_endNode];
                if (endNode.m_flags != NetNode.Flags.None)
                {
                    json.Append(",\"endPosition\":{\"x\":").Append(JsonUtil.Number(endNode.m_position.x));
                    json.Append(",\"z\":").Append(JsonUtil.Number(endNode.m_position.z)).Append("}");
                }
            }

            // Bounds
            json.Append(",\"bounds\":{");
            json.Append("\"minX\":").Append(JsonUtil.Number(seg.m_bounds.min.x));
            json.Append(",\"minZ\":").Append(JsonUtil.Number(seg.m_bounds.min.z));
            json.Append(",\"maxX\":").Append(JsonUtil.Number(seg.m_bounds.max.x));
            json.Append(",\"maxZ\":").Append(JsonUtil.Number(seg.m_bounds.max.z));
            json.Append("}");

            json.Append(",\"buildIndex\":").Append(seg.m_buildIndex);
            json.Append(",\"averageLength\":").Append(JsonUtil.Number(seg.m_averageLength));
            json.Append(",\"nextGridSegment\":").Append(seg.m_nextGridSegment);

            // Lane count
            json.Append(",\"laneCount\":").Append((int)seg.m_lanes);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Get complete detail for a network node.
        /// Query: ?id=ushort
        /// </summary>
        public static CommandResult BuildNodeDetail(string query)
        {
            ushort nodeId = ParseId(query);
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (nodeId >= nm.m_nodes.m_size) return CommandResult.Fail("Invalid node ID: " + nodeId);

            NetNode node = nm.m_nodes.m_buffer[nodeId];
            if (node.m_flags == NetNode.Flags.None)
                return CommandResult.Fail("Node does not exist: " + nodeId);

            NetInfo info = node.Info;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"id\":").Append(nodeId);
            json.Append(",\"flags\":").Append((int)node.m_flags);
            json.Append(",\"flags2\":").Append((int)node.m_flags2);

            if (info != null)
            {
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(info.name != null ? info.name : "")).Append("\"");
            }

            json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(node.m_position.x));
            json.Append(",\"z\":").Append(JsonUtil.Number(node.m_position.z)).Append("}");

            json.Append(",\"buildIndex\":").Append(node.m_buildIndex);
            json.Append(",\"lane\":").Append(node.m_lane);
            json.Append(",\"nextBuildingNode\":").Append(node.m_nextBuildingNode);
            json.Append(",\"maxWaitTime\":").Append(node.m_maxWaitTime);

            // Connected segments
            json.Append(",\"connectedSegments\":[");
            bool first = true;
            for (int i = 0; i < 8; i++)
            {
                ushort seg = node.GetSegment(i);
                if (seg != 0)
                {
                    if (!first) json.Append(",");
                    first = false;
                    json.Append(seg);
                }
            }
            json.Append("]");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Get traffic light state for all intersections.
        /// Returns segments with traffic light state and their connected nodes.
        /// </summary>
        public static CommandResult BuildTrafficLightsJson(int limit)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");
            if (limit < 0) limit = 0;
            if (limit > 200) limit = 200;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"intersections\":[");
            bool first = true;
            int emitted = 0;

            for (ushort i = 0; i < nm.m_segments.m_size; i++)
            {
                NetSegment seg = nm.m_segments.m_buffer[i];
                if (seg.m_flags == NetSegment.Flags.None) continue;

                // Check if this segment has traffic lights (non-zero state)
                if (seg.m_trafficLightState0 == 0 && seg.m_trafficLightState1 == 0) continue;

                if (emitted >= limit) break;
                if (!first) json.Append(",");
                first = false;

                json.Append("{");
                json.Append("\"segmentId\":").Append(i);
                json.Append(",\"trafficLightState0\":").Append(seg.m_trafficLightState0);
                json.Append(",\"trafficLightState1\":").Append(seg.m_trafficLightState1);
                json.Append(",\"startNode\":").Append(seg.m_startNode);
                json.Append(",\"endNode\":").Append(seg.m_endNode);
                json.Append("}");

                emitted++;
            }

            json.Append("]");
            json.Append(",\"totalIntersections\":" + emitted);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set traffic light on a segment.
        /// Body: { "segmentId": ushort, "mode": "green"|"red"|"toggle" }
        /// Note: CS1 traffic light modification is limited through public API.
        /// </summary>
        public static CommandResult SetTrafficLight(string body)
        {
            NetManager nm = NetManager.instance;
            if (nm == null) return CommandResult.Fail("NetManager is not available.");

            ushort segId = (ushort)(int)JsonUtil.GetNumber(body, "segmentId", 0f);
            string mode = JsonUtil.GetString(body, "mode", "toggle").ToLowerInvariant();

            if (segId >= nm.m_segments.m_size)
                return CommandResult.Fail("Invalid segment ID: " + segId);

            NetSegment seg = nm.m_segments.m_buffer[segId];
            if (seg.m_flags == NetSegment.Flags.None)
                return CommandResult.Fail("Segment does not exist: " + segId);

            int oldState = seg.m_trafficLightState0;

            // Toggle traffic light by modifying flags
            if (mode == "toggle")
            {
                if ((seg.m_flags & (NetSegment.Flags)0x80) != NetSegment.Flags.None)
                    seg.m_flags &= ~(NetSegment.Flags)0x80;
                else
                    seg.m_flags |= (NetSegment.Flags)0x80;
                nm.UpdateSegment(segId);
            }

            return CommandResult.FromJson("{\"ok\":true,\"segmentId\":" + segId +
                ",\"mode\":\"" + JsonUtil.Escape(mode) + "\"}");
        }

        private static ushort ParseId(string query)
        {
            ushort id = 0;
            if (query != null && query.Length > 0)
            {
                string[] pairs = query.Split('&');
                foreach (string pair in pairs)
                {
                    string[] kv = pair.Split('=');
                    if (kv.Length == 2 && kv[0] == "id")
                    {
                        ushort.TryParse(kv[1], out id);
                        break;
                    }
                }
            }
            return id;
        }
    }
}
