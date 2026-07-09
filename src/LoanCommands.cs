using System;
using System.Text;
using System.Reflection;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Loan/finance state — cash, income, expenses, active loans.
    /// Uses EconomyManager.instance fields verified via monodis.
    /// </summary>
    public static class LoanCommands
    {
        private static readonly FieldInfo fCashAmount = typeof(EconomyManager).GetField("m_cashAmount",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo fLoans = typeof(EconomyManager).GetField("m_loans",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo fTotalIncome = typeof(EconomyManager).GetField("m_totalIncome",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo fTotalExpenses = typeof(EconomyManager).GetField("m_totalExpenses",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public static CommandResult BuildLoansJson()
        {
            EconomyManager em = EconomyManager.instance;
            if (em == null) return CommandResult.Fail("EconomyManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Cash
            if (fCashAmount != null)
                json.Append(",\"cash\":" + ((long)fCashAmount.GetValue(em)).ToString());

            // Income/Expense totals (sum all)
            try
            {
                if (fTotalIncome != null)
                {
                    long[] income = (long[])fTotalIncome.GetValue(em);
                    long totalIn = 0;
                    if (income != null) foreach (long v in income) totalIn += v;
                    json.Append(",\"totalIncome\":" + totalIn);
                }
                if (fTotalExpenses != null)
                {
                    long[] expenses = (long[])fTotalExpenses.GetValue(em);
                    long totalEx = 0;
                    if (expenses != null) foreach (long v in expenses) totalEx += v;
                    json.Append(",\"totalExpenses\":" + totalEx);
                }
            }
            catch { }

            // Loans
            json.Append(",\"loans\":[");
            try
            {
                if (fLoans != null)
                {
                    Array loans = (Array)fLoans.GetValue(em);
                    if (loans != null)
                    {
                        int loanCount = 0;
                        for (int i = 0; i < loans.Length; i++)
                        {
                            object loan = loans.GetValue(i);
                            if (loan == null) continue;

                            // Loan struct: m_amount (int), m_interest (int), m_paidBack (int)?
                            var fAmount = loan.GetType().GetField("m_amount");
                            var fInterest = loan.GetType().GetField("m_interest");
                            var fWeekLength = loan.GetType().GetField("m_weekLength");

                            int amount = fAmount != null ? (int)fAmount.GetValue(loan) : 0;
                            if (amount == 0) continue;

                            if (loanCount > 0) json.Append(",");
                            json.Append("{");
                            json.Append("\"id\":" + i);
                            json.Append(",\"amount\":" + amount);
                            if (fInterest != null)
                                json.Append(",\"interestRate\":" + JsonUtil.Number((float)((int)fInterest.GetValue(loan) / 100.0)));
                            if (fWeekLength != null)
                                json.Append(",\"weeksLeft\":" + (int)fWeekLength.GetValue(loan));
                            json.Append("}");
                            loanCount++;
                        }
                        json.Append("],\"loanCount\":" + loanCount);
                    }
                }
            }
            catch { json.Append("],\"loanCount\":0"); }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
