using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Disaster state and control.
    /// Verified via monodis:
    ///   IsEvacuating(Vector3) → bool
    ///   EvacuateAll(bool) → void
    ///   FindDisasterInfo<T>() → DisasterInfo (generic)
    ///   m_disasters: FastList<DisasterData>
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
            json.Append(",\"isEvacuating\":" + JsonUtil.Bool(dm.IsEvacuating(Vector3.zero)));

            json.Append(",\"disasters\":[");
            int count = 0;
            if (dm.m_disasters != null)
            {
                var bufferField = typeof(FastList<>).GetField("m_buffer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var sizeField = typeof(FastList<>).GetField("m_size",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (sizeField != null)
                {
                    int size = 0;
                    try { size = (int)sizeField.GetValue(dm.m_disasters); } catch { }
                    Array buffer = bufferField.GetValue(dm.m_disasters) as Array;

                    if (buffer != null)
                    {
                        for (int i = 0; i < size && i < 50; i++)
                        {
                            object dd = buffer.GetValue(i);
                            if (dd == null) continue;

                            var flagsField = dd.GetType().GetField("m_flags");
                            var infoField = dd.GetType().GetField("m_infoIndex");
                            var posField = dd.GetType().GetField("m_position");
                            var intensityField = dd.GetType().GetField("m_intensity");

                            int flags = 0;
                            if (flagsField != null)
                            {
                                try { flags = (int)flagsField.GetValue(dd); } catch { }
                            }
                            if (flags == 0) continue;

                            int infoIdx = -1;
                            if (infoField != null)
                            {
                                try { infoIdx = (int)infoField.GetValue(dd); } catch { }
                            }
                            DisasterInfo info = null;
                            if (infoIdx >= 0)
                                info = PrefabCollection<DisasterInfo>.GetLoaded((uint)infoIdx);

                            if (count > 0) json.Append(",");
                            json.Append("{");
                            json.Append("\"id\":" + i);
                            json.Append(",\"flags\":" + flags);
                            json.Append(",\"type\":\"" + JsonUtil.Escape(info != null ? info.name : "Unknown") + "\"");

                            if (posField != null)
                            {
                                try
                                {
                                    Vector3 pos = (Vector3)posField.GetValue(dd);
                                    json.Append(",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}");
                                }
                                catch { }
                            }

                            if (intensityField != null)
                            {
                                try { json.Append(",\"intensity\":" + JsonUtil.Number((float)intensityField.GetValue(dd))); } catch { }
                            }

                            json.Append("}");
                            count++;
                        }
                    }
                }
            }
            json.Append("],\"activeCount\":" + count);
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Create a disaster by prefab name.
        /// </summary>
        public static CommandResult CreateDisaster(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (string.IsNullOrEmpty(name)) return CommandResult.Fail("Disaster name required.");

            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");

            // Find disaster info by iterating prefab collection
            DisasterInfo info = null;
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

        public static CommandResult StartRandomDisaster()
        {
            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");
            try { dm.StartRandomDisaster(); return CommandResult.FromJson("{\"ok\":true,\"message\":\"Random disaster started.\"}"); }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }

        public static CommandResult EvacuateAll()
        {
            DisasterManager dm = DisasterManager.instance;
            if (dm == null) return CommandResult.Fail("DisasterManager not found.");
            try { dm.EvacuateAll(false); return CommandResult.FromJson("{\"ok\":true,\"message\":\"Evacuation started.\"}"); }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
