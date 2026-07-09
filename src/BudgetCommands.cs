using System;
using System.Text;
using ColossalFramework;
using static EconomyManager;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Budget and finance APIs for Cities: Skylines 1.
    /// Reads income/expense breakdown and loan information.
    /// Uses EconomyManager.SetBudget for budget changes.
    /// </summary>
    public static class BudgetCommands
    {
        public static CommandResult BuildBudgetJson()
        {
            EconomyManager economy = Singleton<EconomyManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Money
            long cash = (long)economy.LastCashAmount;
            json.Append(",\"money\":").Append(cash);

            // Income breakdown — only expose what EconomyManager provides
            json.Append(",\"income\":{");
            json.Append("\"totalIncome\":").Append(economy.LastIncomeAmount);
            json.Append("}");

            // Expense
            json.Append(",\"expenses\":{");
            json.Append("\"totalExpense\":").Append(economy.LastExpenseAmount);
            json.Append("}");

            // Loans — economy.m_loans is Loan[] (field, verified from CSM)
            json.Append(",\"loans\":[");
            Loan[] loans = null;
            try
            {
                var loansField = typeof(EconomyManager).GetField("m_loans",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (loansField != null)
                    loans = (Loan[])loansField.GetValue(economy);
            }
            catch { }

            int loanCount = 0;
            if (loans != null)
            {
                for (int i = 0; i < loans.Length; i++)
                {
                    if (loans[i].m_amount == 0) continue;
                    if (loanCount > 0) json.Append(",");
                    json.Append("{\"index\":").Append(i);
                    json.Append(",\"amount\":").Append(loans[i].m_amount);
                    json.Append(",\"amountLeft\":").Append(loans[i].m_amountLeft);
                    json.Append(",\"interestRate\":").Append(loans[i].m_interestRate);
                    json.Append(",\"length\":").Append(loans[i].m_length);
                    json.Append("}");
                    loanCount++;
                }
            }
            json.Append("]");
            json.Append(",\"loanCount\":").Append(loanCount);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set budget for a specific service using EconomyManager.SetBudget.
        /// POST /commands/set-budget
        /// Body: { "service": "PoliceDepartment", "subService": "PoliceDepartment", "amount": 100, "night": false }
        /// amount is 50-150 (percentage)
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
                default: return ItemClass.Service.None;
            }
        }

        private static ItemClass.SubService ParseSubService(string name)
        {
            switch (name)
            {
                case "PoliceDepartment": return ItemClass.SubService.PoliceDepartment;
                case "FireDepartment": return ItemClass.SubService.FireDepartment;
                case "HealthCare": return ItemClass.SubService.HealthCare;
                case "EducationElementary": return ItemClass.SubService.EducationElementary;
                case "EducationHighSchool": return ItemClass.SubService.EducationHighSchool;
                case "EducationUniversity": return ItemClass.SubService.EducationUniversity;
                case "Garbage": return ItemClass.SubService.Garbage;
                case "Road": return ItemClass.SubService.PublicTransportBus; // closest match
                case "Electricity": return ItemClass.SubService.Electricity;
                case "Water": return ItemClass.SubService.Water;
                case "PublicTransport": return ItemClass.SubService.PublicTransportBus;
                default: return ItemClass.SubService.None;
            }
        }
    }
}
