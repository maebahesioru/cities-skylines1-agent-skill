using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Budget APIs for Cities: Skylines 1.
    /// Uses EconomyManager.SetBudget for budget changes.
    /// Verified against CS1 modding API.
    /// </summary>
    public static class BudgetCommands
    {
        /// <summary>
        /// Read basic economy state.
        /// GET /state/budget
        /// </summary>
        public static CommandResult BuildBudgetJson()
        {
            EconomyManager economy = Singleton<EconomyManager>.instance;
            DistrictManager districtManager = DistrictManager.instance;

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Cash
            json.Append(",\"cash\":" + ((long)Singleton<EconomyManager>.instance.LastCashAmount));

            // Population
            if (districtManager != null)
            {
                District district = districtManager.m_districts.m_buffer[0];
                if ((district.m_flags & District.Flags.Created) != District.Flags.None)
                {
                    json.Append(",\"population\":" + ((int)district.m_populationData.m_finalCount));
                }
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set budget for a specific service using EconomyManager.SetBudget.
        /// POST /commands/set-budget
        /// Body: { "service": "PoliceDepartment", "subService": "PoliceDepartment", "amount": 100, "night": false }
        /// amount is 50-150 (percentage)
        /// service/subService use ItemClass.Service/SubService enum names
        /// </summary>
        public static CommandResult SetBudget(string body)
        {
            string serviceName = JsonUtil.GetString(body, "service", "");
            string subServiceName = JsonUtil.GetString(body, "subService", serviceName);
            int amount = (int)JsonUtil.GetNumber(body, "amount", 100f);
            bool night = JsonUtil.GetBool(body, "night", false);

            if (amount < 50) amount = 50;
            if (amount > 150) amount = 150;
            if (serviceName.Length == 0) return CommandResult.Fail("service name is required.");

            ItemClass.Service service = ParseService(serviceName);
            ItemClass.SubService subService = ParseSubService(subServiceName);
            if (service == ItemClass.Service.None)
                return CommandResult.Fail("Unknown service: " + serviceName);

            EconomyManager economy = Singleton<EconomyManager>.instance;
            economy.SetBudget(service, subService, amount, night);

            return CommandResult.FromJson(
                "{\"ok\":true,\"service\":\"" + JsonUtil.Escape(serviceName) +
                "\",\"subService\":\"" + JsonUtil.Escape(subServiceName) +
                "\",\"amount\":" + amount +
                ",\"night\":" + JsonUtil.Bool(night) + "}");
        }

        private static ItemClass.Service ParseService(string name)
        {
            switch (name)
            {
                case "Road": return ItemClass.Service.Road;
                case "Electricity": return ItemClass.Service.Electricity;
                case "Water": return ItemClass.Service.Water;
                case "HealthCare": return ItemClass.Service.HealthCare;
                case "PoliceDepartment": return ItemClass.Service.PoliceDepartment;
                case "FireDepartment": return ItemClass.Service.FireDepartment;
                case "Education": return ItemClass.Service.Education;
                case "Garbage": return ItemClass.Service.Garbage;
                case "PublicTransport": return ItemClass.Service.PublicTransport;
                default:
                    try { return (ItemClass.Service)System.Enum.Parse(typeof(ItemClass.Service), name); }
                    catch { return ItemClass.Service.None; }
            }
        }

        private static ItemClass.SubService ParseSubService(string name)
        {
            try { return (ItemClass.SubService)System.Enum.Parse(typeof(ItemClass.SubService), name); }
            catch { return ItemClass.SubService.None; }
        }
    }
}
