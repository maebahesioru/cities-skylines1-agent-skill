using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Chirp message management.
    /// Reading is already handled by GameState.BuildChirpsJson at /state/chirps.
    /// </summary>
    public static class ChirpCommands
    {
        public static CommandResult GetChirpCount()
        {
            MessageManager mm = MessageManager.instance;
            if (mm == null) return CommandResult.Fail("MessageManager not found.");
            try
            {
                var messages = mm.GetRecentMessages();
                int count = messages != null ? messages.Length : 0;
                return CommandResult.FromJson("{\"ok\":true,\"recentChirps\":" + count + "}");
            }
            catch { return CommandResult.FromJson("{\"ok\":true,\"recentChirps\":-1}"); }
        }
    }
}
