using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game statistics / historical data.
    /// Uses StatisticsManager.instance.
    /// </summary>
    public static class StatisticsCommands
    {
        public static CommandResult BuildStatisticsJson()
        {
            StatisticsManager sm = StatisticsManager.instance;
            if (sm == null) return CommandResult.Fail("StatisticsManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // StatisticsManager.m_statistics is a StatisticBase with named sub-stats
            try
            {
                var fStats = typeof(StatisticsManager).GetField("m_statistics",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (fStats != null)
                {
                    object stats = fStats.GetValue(sm);
                    if (stats != null)
                    {
                        // Try reading known stat groups
                        json.Append(",\"stats\":{");
                        json.Append(ReadStatString(stats, "population"));
                        json.Append(",");
                        json.Append(ReadStatString(stats, "income"));
                        json.Append(",");
                        json.Append(ReadStatString(stats, "expenses"));
                        json.Append(",");
                        json.Append(ReadStatString(stats, "crimeRate"));
                        json.Append(",");
                        json.Append(ReadStatString(stats, "health"));
                        json.Append(",");
                        json.Append(ReadStatString(stats, "happiness"));
                        json.Append("}");
                    }
                }
            }
            catch { json.Append(",\"stats\":{}"); }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        private static string ReadStatString(object statsObj, string name)
        {
            try
            {
                // StatisticBase has Get(string name) or we find by field
                var method = statsObj.GetType().GetMethod("Get");
                if (method != null)
                {
                    object val = method.Invoke(statsObj, new object[] { name });
                    if (val != null && val is int)
                        return "\"" + name + "\":" + ((int)val).ToString();
                }

                // Try field lookup
                var field = statsObj.GetType().GetField(name,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    object val = field.GetValue(statsObj);
                    if (val != null)
                        return "\"" + name + "\":" + val.ToString();
                }
            }
            catch { }
            return "\"" + name + "\":0";
        }
    }
}
