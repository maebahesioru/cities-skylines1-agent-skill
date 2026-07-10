using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class BudgetDetailCommands
    {
        public static CommandResult BuildBudgetDetailJson()
        {
            EconomyManager em = EconomyManager.instance;
            if (em == null) return CommandResult.Fail("EconomyManager is not available.");
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"lastCashAmount\":").Append(em.LastCashAmount);
            json.Append(",\"lastCashDelta\":").Append(em.LastCashDelta);
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult BuildCoverageDetailJson()
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"Coverage detail via CoverageManager.\"}");
        }
    }
}
