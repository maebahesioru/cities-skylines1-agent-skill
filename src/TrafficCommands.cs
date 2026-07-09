using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Traffic and transport state APIs for Cities: Skylines 1.
    /// Verified against Assembly-CSharp.dll decompiled fields.
    /// </summary>
    public static class TrafficCommands
    {
        public static CommandResult BuildTrafficJson(int limit)
        {
            if (limit < 0) limit = 0;
            if (limit > 5000) limit = 5000;

            VehicleManager vm = VehicleManager.instance;
            NetManager nm = NetManager.instance;
            TransportManager tm = Singleton<TransportManager>.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Vehicle counts by info ---
            json.Append(",\"vehicles\":{");
            json.Append("\"total\":" + vm.m_vehicleCount);
            json.Append(",\"parked\":" + vm.m_parkedCount);
            json.Append(",\"active\":" + (vm.m_vehicleCount - vm.m_parkedCount));
            json.Append("}");

            // --- Traffic flow ---
            json.Append(",\"trafficFlow\":{");
            json.Append("\"total\":" + vm.m_totalTrafficFlow);
            json.Append(",\"max\":" + vm.m_maxTrafficFlow);
            json.Append(",\"last\":" + vm.m_lastTrafficFlow);
            json.Append("}");

            // --- Transport lines ---
            json.Append(",\"transportLines\":[");
            int lineCount = 0;
            for (ushort i = 1; i < tm.m_lines.m_size && lineCount < Math.Min(limit, 200); i++)
            {
                TransportLine line = tm.m_lines.m_buffer[i];
                if ((line.m_flags & TransportLine.Flags.Created) == TransportLine.Flags.None) continue;

                if (lineCount > 0) json.Append(",");
                TransportInfo info = PrefabCollection<TransportInfo>.GetLoaded(line.m_infoIndex);
                string transportType = info != null ? info.m_transportType.ToString() : "Unknown";

                json.Append("{\"id\":").Append(i);
                json.Append(",\"active\":" + JsonUtil.Bool((line.m_flags & TransportLine.Flags.Complete) != TransportLine.Flags.None));
                json.Append(",\"stops\":").Append(line.m_stops);
                json.Append(",\"vehicles\":").Append(line.m_vehicles);
                json.Append(",\"totalLength\":" + JsonUtil.Number(line.m_totalLength));
                json.Append(",\"budget\":").Append(line.m_budget);
                json.Append(",\"ticketPrice\":").Append(line.m_ticketPrice);
                json.Append(",\"averageInterval\":").Append(line.m_averageInterval);
                json.Append(",\"type\":\"").Append(JsonUtil.Escape(transportType)).Append("\"");
                json.Append(",\"passengers\":{");
                json.Append("\"resident\":" + line.m_passengers.m_residentPassengers.m_finalCount);
                json.Append(",\"tourist\":" + line.m_passengers.m_touristPassengers.m_finalCount);
                json.Append("}");
                json.Append("}");
                lineCount++;
            }
            json.Append("]");

            // --- Vehicle sample by type ---
            json.Append(",\"vehicleTypes\":{");
            System.Collections.Generic.Dictionary<string, int> types = new System.Collections.Generic.Dictionary<string, int>();
            int sampled = 0;
            for (ushort i = 1; i < vm.m_vehicles.m_size && sampled < 2000; i++)
            {
                Vehicle v = vm.m_vehicles.m_buffer[i];
                if ((v.m_flags & Vehicle.Flags.Created) == (Vehicle.Flags)0) continue;
                sampled++;

                VehicleInfo info = PrefabCollection<VehicleInfo>.GetLoaded(v.m_infoIndex);
                string typeName = info != null ? info.m_vehicleType.ToString() : "Unknown";
                if (types.ContainsKey(typeName)) types[typeName]++;
                else types[typeName] = 1;
            }
            bool firstType = true;
            foreach (var kv in types)
            {
                if (!firstType) json.Append(",");
                json.Append("\"").Append(JsonUtil.Escape(kv.Key)).Append("\":").Append(kv.Value);
                firstType = false;
            }
            json.Append("}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
