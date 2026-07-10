using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Vehicle listing and detail APIs.
    /// GET /state/vehicles returns all vehicles with position/type/passengers/cargo.
    /// </summary>
    public static class VehicleCommands
    {
        /// <summary>
        /// List vehicles with optional type/service filter.
        /// Query params: limit (default 200), service (optional), includePosition (default true)
        /// </summary>
        public static CommandResult BuildVehiclesJson(int limit, string service, bool includePosition)
        {
            VehicleManager vm = VehicleManager.instance;
            if (vm == null) return CommandResult.Fail("VehicleManager is not available.");

            if (limit < 0) limit = 0;
            if (limit > 500) limit = 500;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"vehicles\":[");
            bool first = true;
            int emitted = 0;
            int totalActive = 0;

            for (ushort i = 0; i < vm.m_vehicles.m_size; i++)
            {
                Vehicle v = vm.m_vehicles.m_buffer[i];
                if (v.m_flags == 0) continue;

                // Skip spawned/parked if we want active only (spawned/parked = 0 flags variation)
                if ((v.m_flags & (Vehicle.Flags)0x20) != 0) continue;

                totalActive++;
                if (emitted >= limit) continue;

                // Service filter
                VehicleInfo info = v.Info;
                if (service != null && service.Length > 0 && info != null)
                {
                    if (!info.name.ToLowerInvariant().Contains(service.ToLowerInvariant()))
                        continue;
                }

                if (!first) json.Append(",");
                first = false;

                json.Append("{");
                json.Append("\"id\":").Append(i);
                json.Append(",\"flags\":").Append((int)v.m_flags);
                json.Append(",\"flags2\":").Append((int)v.m_flags2);

                if (info != null)
                {
                    json.Append(",\"type\":\"").Append(JsonUtil.Escape(info.name ?? "")).Append("\"");
                    json.Append(",\"vehicleType\":").Append((int)info.m_vehicleType);
                }

                if (includePosition)
                {
                    Vector3 pos = v.GetLastFramePosition();
                    json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(pos.x));
                    json.Append(",\"y\":").Append(JsonUtil.Number(pos.y));
                    json.Append(",\"z\":").Append(JsonUtil.Number(pos.z)).Append("}");
                }

                json.Append(",\"sourceBuilding\":").Append(v.m_sourceBuilding);
                json.Append(",\"targetBuilding\":").Append(v.m_targetBuilding);
                json.Append(",\"transportLine\":").Append(v.m_transportLine);
                json.Append(",\"transferSize\":").Append(v.m_transferSize);

                // Citizen units (passenger count proxy)
                int passengers = CountPassengerUnits(v);
                json.Append(",\"passengers\":").Append(passengers);

                json.Append(",\"leadingVehicle\":").Append(v.m_leadingVehicle);
                json.Append(",\"trailingVehicle\":").Append(v.m_trailingVehicle);
                json.Append(",\"waitCounter\":").Append((int)v.m_waitCounter);
                json.Append(",\"transferType\":").Append((int)v.m_transferType);

                json.Append("}");
                emitted++;
            }

            json.Append("]");
            json.Append(",\"totalActive\":" + totalActive);
            json.Append(",\"returned\":" + emitted);
            json.Append(",\"limit\":" + limit);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        private static int CountPassengerUnits(Vehicle v)
        {
            if (v.m_citizenUnits == 0) return 0;

            CitizenManager cm = CitizenManager.instance;
            if (cm == null) return 0;

            int count = 0;
            uint unit = v.m_citizenUnits;
            while (unit != 0)
            {
                CitizenUnit cu = cm.m_units.m_buffer[unit];
                if (cu.m_citizen0 != 0) count++;
                if (cu.m_citizen1 != 0) count++;
                if (cu.m_citizen2 != 0) count++;
                if (cu.m_citizen3 != 0) count++;
                if (cu.m_citizen4 != 0) count++;
                unit = cu.m_nextUnit;
                // Safety limit
                if (count > 1000) break;
            }
            return count;
        }
    }
}
