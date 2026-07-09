using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Budget and finance APIs for Cities: Skylines 1.
    /// Reads income/expense breakdown and budget allocations.
    /// </summary>
    public static class BudgetCommands
    {
        public static CommandResult BuildBudgetJson()
        {
            EconomyManager economy = Singleton<EconomyManager>.instance;
            SimulationManager simulation = Singleton<SimulationManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Money
            json.Append(",\"money\":").Append((long)economy.LastCashAmount);
            json.Append(",\"moneyChange\":").Append(economy.LastCashAmount - economy.LastExpenseAmount + economy.LastIncomeAmount);

            // Income breakdown
            json.Append(",\"income\":{");
            json.Append("\"taxIncome\":").Append(economy.m_totalTaxIncome);
            json.Append(",\"residentialTaxIncome\":").Append(economy.m_totalResidentialTaxIncome);
            json.Append(",\"commercialTaxIncome\":").Append(economy.m_totalCommercialTaxIncome);
            json.Append(",\"industrialTaxIncome\":").Append(economy.m_totalIndustrialTaxIncome);
            json.Append(",\"totalIncome\":").Append(economy.LastIncomeAmount);
            json.Append("}");

            // Expense breakdown
            json.Append(",\"expenses\":{");
            json.Append("\"roadExpense\":").Append(economy.m_totalRoadExpenses);
            json.Append(",\"electricityExpense\":").Append(economy.m_totalElectricityExpenses);
            json.Append(",\"waterExpense\":").Append(economy.m_totalWaterExpenses);
            json.Append(",\"healthcareExpense\":").Append(economy.m_totalHealthcareExpenses);
            json.Append(",\"policeExpense\":").Append(economy.m_totalPoliceExpenses);
            json.Append(",\"fireExpense\":").Append(economy.m_totalFireExpenses);
            json.Append(",\"educationExpense\":").Append(economy.m_totalEducationExpenses);
            json.Append(",\"garbageExpense\":").Append(economy.m_totalGarbageExpenses);
            json.Append(",\"totalExpense\":").Append(economy.LastExpenseAmount);
            json.Append("}");

            // Loan info
            json.Append(",\"loans\":[");
            int loanCount = 0;
            for (int i = 0; i < economy.m_loanCount; i++)
            {
                if (loanCount > 0) json.Append(",");
                LoanInfo loan = economy.m_loans[i];
                json.Append("{\"amount\":").Append(loan.m_amount);
                json.Append(",\"interestRate\":").Append(JsonUtil.Number(loan.m_interestRate));
                json.Append(",\"weeklyPayment\":").Append(loan.m_weeklyPayment);
                json.Append("}");
                loanCount++;
            }
            json.Append("]");
            json.Append(",\"loanCount\":").Append(loanCount);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set budget for a specific service.
        /// POST /commands/set-budget
        /// Body: { "service": "PoliceDepartment", "amount": 100 }
        /// amount is 50-150 (percentage)
        /// </summary>
        public static CommandResult SetBudget(string body)
        {
            string serviceName = JsonUtil.GetString(body, "service", "");
            int amount = (int)JsonUtil.GetNumber(body, "amount", 100f);
            if (amount < 50) amount = 50;
            if (amount > 150) amount = 150;

            if (serviceName.Length == 0) return CommandResult.Fail("service name is required.");

            EconomyManager economy = Singleton<EconomyManager>.instance;
            ItemClass.Service service = ParseService(serviceName);
            if (service == ItemClass.Service.None)
                return CommandResult.Fail("Unknown service: " + serviceName);

            // EconomyManager uses internal budget array — set via reflection
            try
            {
                var budgetField = typeof(EconomyManager).GetField("m_budgetArray",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (budgetField != null)
                {
                    int[] budgets = (int[])budgetField.GetValue(economy);
                    int index = (int)service;
                    if (index >= 0 && index < budgets.Length)
                    {
                        budgets[index] = amount;
                        budgetField.SetValue(economy, budgets);
                    }
                }
            }
            catch (System.Exception ex)
            {
                return CommandResult.Fail("Failed to set budget: " + ex.Message);
            }

            return CommandResult.FromJson(
                "{\"ok\":true,\"service\":\"" + JsonUtil.Escape(serviceName) +
                "\",\"amount\":" + amount + "}");
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
                default:
                    System.Array values = System.Enum.GetValues(typeof(ItemClass.Service));
                    foreach (ItemClass.Service s in values)
                    {
                        if (s.ToString() == name) return s;
                    }
                    return ItemClass.Service.None;
            }
        }
    }
}
