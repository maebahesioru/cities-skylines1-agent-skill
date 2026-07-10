using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Transport line management APIs.
    /// Uses verified CS1 TransportManager API signatures.
    /// CreateLine(out ushort, out Randomizer, TransportInfo, bool)
    /// </summary>
    public static class TransportLineCommands
    {
        public static CommandResult BuildTransportLinesJson()
        {
            TransportManager tm = TransportManager.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (tm != null)
            {
                json.Append(",\"lines\":[");
                bool first = true;
                int totalLines = 0;

                for (ushort i = 0; i < tm.m_lines.m_size; i++)
                {
                    TransportLine line = tm.m_lines.m_buffer[i];
                    if (line.m_flags == 0) continue;

                    if (!first) json.Append(",");
                    first = false;

                    bool active, visible;
                    line.GetActive(out active, out visible);

                    json.Append("{");
                    json.Append("\"id\":" + i);
                    json.Append(",\"active\":" + JsonUtil.Bool(active));
                    json.Append(",\"visible\":" + JsonUtil.Bool(visible));
                    json.Append(",\"complete\":" + JsonUtil.Bool(line.Complete));
                    json.Append(",\"stops\":" + line.CountStops(0));
                    json.Append(",\"flags\":" + (int)line.m_flags);
                    json.Append("}");

                    totalLines++;
                }

                json.Append("]");
                json.Append(",\"totalLines\":" + totalLines);
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Create a new transport line.
        /// Body: { "prefab": "Bus" }
        /// CreateLine(out ushort lineId, out Randomizer rand, TransportInfo info, bool unused)
        /// </summary>
        public static CommandResult CreateTransportLine(string body)
        {
            TransportManager tm = TransportManager.instance;
            if (tm == null) return CommandResult.Fail("TransportManager is not available.");

            string prefabName = JsonUtil.GetString(body, "prefab", "Bus");
            TransportInfo info = FindTransportInfo(prefabName);
            if (info == null)
                return CommandResult.Fail("Transport prefab not found: " + prefabName);

            ushort lineId;
            var rand = Singleton<SimulationManager>.instance.m_randomizer;
            if (tm.CreateLine(out lineId, ref rand, info, false))
            {
                return CommandResult.FromJson("{\"ok\":true,\"lineId\":" + lineId +
                    ",\"prefab\":\"" + JsonUtil.Escape(prefabName) + "\"}");
            }

            return CommandResult.Fail("Failed to create transport line.");
        }

        /// <summary>
        /// Delete a transport line.
        /// Body: { "lineId": ushort }
        /// </summary>
        public static CommandResult DeleteTransportLine(string body)
        {
            TransportManager tm = TransportManager.instance;
            if (tm == null) return CommandResult.Fail("TransportManager is not available.");

            ushort lineId = (ushort)(int)JsonUtil.GetNumber(body, "lineId", 0f);
            if (lineId >= tm.m_lines.m_size)
                return CommandResult.Fail("Invalid line ID: " + lineId);

            TransportLine line = tm.m_lines.m_buffer[lineId];
            if (line.m_flags == 0)
                return CommandResult.Fail("Transport line does not exist: " + lineId);

            int stopCount = line.CountStops(0);
            tm.ReleaseLine(lineId);

            return CommandResult.FromJson("{\"ok\":true,\"deletedLineId\":" + lineId +
                ",\"stopsRemoved\":" + stopCount + "}");
        }

        /// <summary>
        /// Add a stop to a transport line.
        /// Body: { "lineId": ushort, "buildingId": ushort }
        /// </summary>
        public static CommandResult AddStop(string body)
        {
            TransportManager tm = TransportManager.instance;
            if (tm == null) return CommandResult.Fail("TransportManager is not available.");

            ushort lineId = (ushort)(int)JsonUtil.GetNumber(body, "lineId", 0f);
            ushort buildingId = (ushort)(int)JsonUtil.GetNumber(body, "buildingId", 0f);

            if (lineId >= tm.m_lines.m_size)
                return CommandResult.Fail("Invalid line ID: " + lineId);

            TransportLine line = tm.m_lines.m_buffer[lineId];
            if (line.m_flags == 0)
                return CommandResult.Fail("Transport line does not exist: " + lineId);

            // Verify building exists
            BuildingManager bm = BuildingManager.instance;
            if (bm != null && buildingId < bm.m_buildings.m_size)
            {
                Building b = bm.m_buildings.m_buffer[buildingId];
                if (b.m_flags == Building.Flags.None)
                    return CommandResult.Fail("Building does not exist: " + buildingId);
            }

            // Get building position for stop
            Vector3 pos = Vector3.zero;
            if (bm != null && buildingId < bm.m_buildings.m_size)
            {
                pos = bm.m_buildings.m_buffer[buildingId].m_position;
            }

            int stopCount = line.CountStops(0);
            line.MoveStop(buildingId, stopCount, pos, true);

            return CommandResult.FromJson("{\"ok\":true,\"lineId\":" + lineId +
                ",\"buildingId\":" + buildingId + ",\"addedStop\":true}");
        }

        /// <summary>
        /// Remove a stop from a transport line.
        /// Body: { "lineId": ushort, "stopIndex": int }
        /// </summary>
        public static CommandResult RemoveStop(string body)
        {
            TransportManager tm = TransportManager.instance;
            if (tm == null) return CommandResult.Fail("TransportManager is not available.");

            ushort lineId = (ushort)(int)JsonUtil.GetNumber(body, "lineId", 0f);
            int stopIndex = (int)JsonUtil.GetNumber(body, "stopIndex", 0f);

            if (lineId >= tm.m_lines.m_size)
                return CommandResult.Fail("Invalid line ID: " + lineId);

            TransportLine line = tm.m_lines.m_buffer[lineId];
            if (line.m_flags == 0)
                return CommandResult.Fail("Transport line does not exist: " + lineId);

            if (stopIndex < 0 || stopIndex >= line.CountStops(0))
                return CommandResult.Fail("Invalid stop index: " + stopIndex);

            // Move the stop to an invalid position to effectively remove it
            line.MoveStop(0, stopIndex, Vector3.zero, false);

            return CommandResult.FromJson("{\"ok\":true,\"lineId\":" + lineId +
                ",\"removedStopIndex\":" + stopIndex + "}");
        }

        private static TransportInfo FindTransportInfo(string prefabName)
        {
            int count = PrefabCollection<TransportInfo>.PrefabCount();
            for (uint i = 0; i < count; i++)
            {
                TransportInfo info = PrefabCollection<TransportInfo>.GetPrefab(i);
                if (info != null && info.name != null &&
                    info.name.ToLowerInvariant().Contains(prefabName.ToLowerInvariant()))
                {
                    return info;
                }
            }
            return null;
        }
    }
}
