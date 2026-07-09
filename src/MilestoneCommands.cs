using System;
using System.Text;
using System.Collections.Generic;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Milestone API — read milestone state via UnlockManager.
    /// Verified against Assembly-CSharp.dll (monodis):
    ///   UnlockManager.instance.m_allMilestones: Dictionary<string, MilestoneInfo>
    ///   UnlockManager.Unlocked(MilestoneInfo) → bool
    ///   UnlockManager.CheckMilestone(MilestoneInfo, bool, bool) → int
    ///   MilestoneInfo.m_name, m_rewardCash, m_hidden, m_canRelock
    ///   MilestoneInfo.GetLocalizedName() → string
    ///   MilestonesWrapper.UnlockMilestone(string)
    ///   MilestonesWrapper.EnumerateMilestones() → string[]
    /// </summary>
    public static class MilestoneCommands
    {
        public static CommandResult BuildMilestoneJson()
        {
            UnlockManager um = UnlockManager.instance;
            if (um == null) return CommandResult.Fail("UnlockManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Unlock counts ---
            int unlockedCount = 0;
            int totalCount = 0;

            Dictionary<string, MilestoneInfo> milestones = um.m_allMilestones;
            if (milestones != null)
            {
                json.Append(",\"milestones\":[");

                // Get globally unlocked milestone names from MilestonesWrapper
                HashSet<string> unlockedNames = new HashSet<string>();
                try
                {
                    MilestonesWrapper mw = um.m_MilestonesWrapper;
                    if (mw != null)
                    {
                        string[] unlocked = mw.EnumerateMilestones();
                        if (unlocked != null)
                            foreach (string s in unlocked)
                                unlockedNames.Add(s);
                    }
                }
                catch { /* fallback: use Unlocked() below */ }

                bool first = true;
                foreach (KeyValuePair<string, MilestoneInfo> kv in milestones)
                {
                    MilestoneInfo mi = kv.Value;
                    if (mi == null || mi.m_hidden) continue;
                    totalCount++;

                    bool isUnlocked = unlockedNames.Count > 0
                        ? unlockedNames.Contains(kv.Key)
                        : um.Unlocked(mi);
                    if (isUnlocked) unlockedCount++;

                    if (!first) json.Append(",");
                    json.Append("{");
                    json.Append("\"name\":\"" + JsonUtil.Escape(kv.Key) + "\"");
                    json.Append(",\"displayName\":\"" + JsonUtil.Escape(mi.GetLocalizedName()) + "\"");
                    json.Append(",\"unlocked\":" + JsonUtil.Bool(isUnlocked));
                    json.Append(",\"rewardCash\":" + mi.m_rewardCash);
                    json.Append(",\"canRelock\":" + JsonUtil.Bool(mi.m_canRelock));
                    if (mi.m_prevMilestone != null)
                        json.Append(",\"prevMilestone\":\"" + JsonUtil.Escape(mi.m_prevMilestone.name) + "\"");
                    if (mi.m_nextMilestone != null)
                        json.Append(",\"nextMilestone\":\"" + JsonUtil.Escape(mi.m_nextMilestone.name) + "\"");
                    json.Append("}");
                    first = false;
                }
                json.Append("]");
            }

            json.Append(",\"summary\":{");
            json.Append("\"unlocked\":" + unlockedCount);
            json.Append(",\"total\":" + totalCount);
            json.Append("}");

            // --- City population context ---
            DistrictManager dm = DistrictManager.instance;
            if (dm != null && dm.m_districts.m_size > 0)
            {
                District d = dm.m_districts.m_buffer[0];
                json.Append(",\"city\":{");
                json.Append("\"population\":" + ((int)d.m_populationData.m_finalCount));
                json.Append(",\"happiness\":" + d.m_finalHappiness);
                json.Append(",\"crimeRate\":" + d.m_finalCrimeRate);
                json.Append("}");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Force-unlock a milestone by name.
        /// POST /commands/unlock-milestone
        /// Body: { "name": "Milestone 1" }
        /// </summary>
        public static CommandResult UnlockMilestone(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (string.IsNullOrEmpty(name))
                return CommandResult.Fail("Milestone name required.");

            UnlockManager um2 = UnlockManager.instance;
            if (um2 == null) return CommandResult.Fail("UnlockManager not found.");
            MilestonesWrapper mw = um2.m_MilestonesWrapper;
            if (mw == null) return CommandResult.Fail("MilestonesWrapper not found.");

            try
            {
                mw.UnlockMilestone(name);
                return CommandResult.FromJson("{\"ok\":true,\"unlocked\":\"" + JsonUtil.Escape(name) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to unlock milestone: " + ex.Message);
            }
        }
    }
}
