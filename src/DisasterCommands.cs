using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Disaster state and control.
    /// Uses DisasterManager.instance fields verified via monodis.
    ///   CreateDisaster(out ushort, DisasterInfo) → bool
    ///   StartRandomDisaster() → void
    ///   EvacuateAll() → void
    ///   IsEvacuating() → bool
    ///   m_disasters: FastList<DisasterData>, m_disasterCount: int
    /// </summary>
    public static class DisasterCommands
    {
        public static CommandResult BuildDisastersJson()
        {
            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"disasterCount\":" + dm.m_disasterCount);
            json.Append(",\"randomProbability\":" + JsonUtil.Number(dm.m_randomDisastersProbability));
            json.Append(",\"randomCooldown\":" + dm.m_randomDisasterCooldown);
            json.Append(",\"isEvacuating\":" + JsonUtil.Bool(dm.IsEvacuating()));

            // List active disasters
            json.Append(",\"disasters\":[");
            int count = 0;
            if (dm.m_disasters != null)
            {
                var disasters = dm.m_disasters;
                int size = (int)typeof(FastList<>).GetField("m_size",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(disasters) ?? 0;

                var buffer = typeof(FastList<>).GetField("m_buffer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(disasters) as Array;

                if (buffer != null)
                {
                    for (int i = 0; i < size && i < 50; i++)
                    {
                        object dd = buffer.GetValue(i);
                        if (dd == null) continue;

                        if (count > 0) json.Append(",");
                        // Read DisasterData fields via reflection
                        var flagsField = dd.GetType().GetField("m_flags");
                        var infoField = dd.GetType().GetField("m_infoIndex");
                        var posField = dd.GetType().GetField("m_position");
                        var intensityField = dd.GetType().GetField("m_intensity");

                        int flags = flagsField != null ? (int)flagsField.GetValue(dd) : 0;
                        if (flags == 0) continue; // Not created

                        int infoIdx = infoField != null ? (int)infoField.GetValue(dd) : -1;
                        DisasterInfo info = null;
                        if (infoIdx >= 0)
                            info = PrefabCollection<DisasterInfo>.GetLoaded((uint)infoIdx);

                        json.Append("{");
                        json.Append("\"id\":" + i);
                        json.Append(",\"flags\":" + flags);
                        json.Append(",\"type\":\"" + JsonUtil.Escape(info != null ? info.name : "Unknown") + "\"");

                        if (posField != null)
                        {
                            var pos = (UnityEngine.Vector3)posField.GetValue(dd);
                            json.Append(",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}");
                        }

                        if (intensityField != null)
                            json.Append(",\"intensity\":" + JsonUtil.Number((double)(float)intensityField.GetValue(dd)));

                        json.Append("}");
                        count++;
                    }
                }
            }
            json.Append("],\"activeCount\":" + count);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Create a disaster by name.
        /// POST /commands/create-disaster
        /// Body: { "name": "Earthquake", "x": 500, "z": 300 }
        /// </summary>
        public static CommandResult CreateDisaster(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (string.IsNullOrEmpty(name)) return CommandResult.Fail("Disaster name required.");

            float x = JsonUtil.GetPointNumber(body, "position", "x", float.NaN);
            float z = JsonUtil.GetPointNumber(body, "position", "z", float.NaN);

            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");

            // Find disaster info
            DisasterInfo info = dm.FindDisasterInfo(name);
            if (info == null)
            {
                // Try prefab collection
                int prefabCount = PrefabCollection<DisasterInfo>.LoadedCount();
                for (int i = 0; i < prefabCount; i++)
                {
                    DisasterInfo di = PrefabCollection<DisasterInfo>.GetLoaded((uint)i);
                    if (di != null && di.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        info = di;
                        break;
                    }
                }
            }
            if (info == null) return CommandResult.Fail("Disaster not found: " + name);

            try
            {
                ushort disasterId;
                bool created = dm.CreateDisaster(out disasterId, info);
                if (created)
                    return CommandResult.FromJson("{\"ok\":true,\"disasterId\":" + disasterId + ",\"name\":\"" + JsonUtil.Escape(name) + "\"}");
                return CommandResult.Fail("Failed to create disaster: " + name);
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Error creating disaster: " + ex.Message);
            }
        }

        /// <summary>
        /// Start a random disaster.
        /// POST /commands/start-random-disaster
        /// </summary>
        public static CommandResult StartRandomDisaster()
        {
            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");

            try
            {
                dm.StartRandomDisaster();
                return CommandResult.FromJson("{\"ok\":true,\"message\":\"Random disaster started.\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Evacuate all citizens.
        /// POST /commands/evacuate
        /// </summary>
        public static CommandResult EvacuateAll()
        {
            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");

            try
            {
                dm.EvacuateAll();
                return CommandResult.FromJson("{\"ok\":true,\"message\":\"Evacuation started.\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Error: " + ex.Message);
            }
        }
    }
}
