using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Traffic and transport state APIs for Cities: Skylines 1.
    /// Reads vehicle counts, transport line data, and congestion metrics.
    /// </summary>
    public static class TrafficCommands
    {
        public static CommandResult BuildTrafficJson(int limit)
        {
            if (limit < 0) limit = 0;
            if (limit > 5000) limit = 5000;

            VehicleManager vehicleManager = VehicleManager.instance;
            NetManager netManager = NetManager.instance;
            TransportManager transportManager = Singleton<TransportManager>.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Vehicle counts by type ---
            int totalVehicles = 0;
            int parkedVehicles = 0;
            int movingVehicles = 0;
            System.Collections.Generic.Dictionary<string, int> vehicleTypes = new System.Collections.Generic.Dictionary<string, int>();

            for (ushort i = 1; i < vehicleManager.m_vehicles.m_buffer.Length; i++)
            {
                Vehicle vehicle = vehicleManager.m_vehicles.m_buffer[i];
                if ((vehicle.m_flags & Vehicle.Flags.Created) == Vehicle.Flags.None) continue;
                totalVehicles++;

                if ((vehicle.m_flags & Vehicle.Flags.Parking) != Vehicle.Flags.None) parkedVehicles++;
                else movingVehicles++;

                VehicleInfo info = vehicle.Info;
                string typeName = info != null ? info.m_vehicleType.ToString() : "Unknown";
                if (vehicleTypes.ContainsKey(typeName)) vehicleTypes[typeName]++;
                else vehicleTypes[typeName] = 1;
            }

            json.Append(",\"vehicles\":{");
            json.Append("\"total\":").Append(totalVehicles);
            json.Append(",\"parked\":").Append(parkedVehicles);
            json.Append(",\"moving\":").Append(movingVehicles);
            json.Append(",\"byType\":{");
            bool firstType = true;
            foreach (var kv in vehicleTypes)
            {
                if (!firstType) json.Append(",");
                json.Append("\"").Append(JsonUtil.Escape(kv.Key)).Append("\":").Append(kv.Value);
                firstType = false;
            }
            json.Append("}}");

            // --- Transport lines ---
            json.Append(",\"transportLines\":[");
            int lineCount = 0;
            for (ushort i = 1; i < transportManager.m_lines.m_buffer.Length && lineCount < 500; i++)
            {
                TransportLine line = transportManager.m_lines.m_buffer[i];
                if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None) continue;

                if (lineCount > 0) json.Append(",");
                json.Append("{\"id\":").Append(i);
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(line.m_name ?? "")).Append("\"");
                json.Append(",\"active\":").Append(JsonUtil.Bool((line.m_flags & TransportLine.Flags.Active) != TransportLine.Flags.None));
                json.Append(",\"vehicleCount\":").Append(line.m_vehicleCount);
                json.Append(",\"totalLength\":").Append(JsonUtil.Number(line.m_totalLength));
                json.Append(",\"averageVehicleSpeed\":").Append(JsonUtil.Number(line.m_averageVehicleSpeed));
                json.Append(",\"passengers\":").Append(JsonUtil.Number(line.m_passengers));
                json.Append(",\"accumulatedCash\":").Append(line.m_accumulatedCash);
                json.Append(",\"problems\":\"").Append(JsonUtil.Escape(line.m_problems.ToString())).Append("\"");
                json.Append(",\"lineType\":\"").Append(JsonUtil.Escape(line.Info != null ? line.Info.m_transportType.ToString() : "Unknown")).Append("\"}");
                lineCount++;
            }
            json.Append("]");

            // --- Road segment congestion sample ---
            json.Append(",\"roadCongestion\":[");
            int segCount = 0;
            for (ushort i = 1; i < netManager.m_segments.m_buffer.Length && segCount < Math.Min(limit, 200); i++)
            {
                NetSegment seg = netManager.m_segments.m_buffer[i];
                if ((seg.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.None) continue;
                NetInfo segInfo = seg.Info;
                if (segInfo == null || segInfo.m_class == null || segInfo.m_class.m_service != ItemClass.Service.Road) continue;

                // Traffic density = number of vehicles on segment lanes vs capacity
                float trafficDensity = GetSegmentTrafficDensity(seg);
                if (trafficDensity < 0.1f) continue; // skip empty segments

                if (segCount > 0) json.Append(",");
                json.Append("{\"segmentId\":").Append(i);
                json.Append(",\"name\":\"").Append(JsonUtil.Escape(segInfo.GetUncheckedLocalizedTitle())).Append("\"");
                json.Append(",\"trafficDensity\":").Append(JsonUtil.Number(trafficDensity));
                json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(seg.m_middlePosition.x));
                json.Append(",\"z\":").Append(JsonUtil.Number(seg.m_middlePosition.z)).Append("}}");
                segCount++;
            }
            json.Append("]");

            // --- Transport stop counts ---
            json.Append(",\"transportStops\":{");
            int busStops = 0, metroStops = 0, trainStops = 0, tramStops = 0, shipStops = 0, planeStops = 0, otherStops = 0;
            for (ushort i = 1; i < netManager.m_nodes.m_buffer.Length; i++)
            {
                NetNode node = netManager.m_nodes.m_buffer[i];
                if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.None) continue;
                if ((node.m_flags & NetNode.Flags.Stop) != NetNode.Flags.None)
                {
                    TransportInfo tInfo = node.Info?.m_netAI as TransportInfo;
                    if (tInfo != null)
                    {
                        switch (tInfo.m_transportType)
                        {
                            case TransportInfo.TransportType.Bus: busStops++; break;
                            case TransportInfo.TransportType.Metro: metroStops++; break;
                            case TransportInfo.TransportType.Train: trainStops++; break;
                            case TransportInfo.TransportType.Tram: tramStops++; break;
                            case TransportInfo.TransportType.Ship: shipStops++; break;
                            case TransportInfo.TransportType.Airplane: planeStops++; break;
                            default: otherStops++; break;
                        }
                    }
                }
            }
            json.Append("\"bus\":").Append(busStops);
            json.Append(",\"metro\":").Append(metroStops);
            json.Append(",\"train\":").Append(trainStops);
            json.Append(",\"tram\":").Append(tramStops);
            json.Append(",\"ship\":").Append(shipStops);
            json.Append(",\"plane\":").Append(planeStops);
            json.Append(",\"other\":").Append(otherStops).Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static float GetSegmentTrafficDensity(NetSegment segment)
        {
            uint firstLane = segment.m_lanes;
            if (firstLane == 0) return 0f;

            NetManager net = NetManager.instance;
            int vehicleCount = 0;
            int laneCount = 0;

            uint laneId = firstLane;
            while (laneId != 0 && laneCount < 32)
            {
                NetLane lane = net.m_lanes.m_buffer[laneId];
                if ((lane.m_flags & NetLane.Flags.Created) != NetLane.Flags.None)
                {
                    ushort vehicleId = lane.m_firstVehicle;
                    while (vehicleId != 0 && vehicleCount < 100)
                    {
                        vehicleCount++;
                        Vehicle v = VehicleManager.instance.m_vehicles.m_buffer[vehicleId];
                        vehicleId = v.m_nextVehicle;
                    }
                    laneCount++;
                }
                laneId = lane.m_nextLane;
            }

            if (laneCount == 0) return 0f;
            return Math.Min(1f, (float)vehicleCount / (laneCount * 4f));
        }
    }
}
