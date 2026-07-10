using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class AudioCommands
    {
        public static CommandResult BuildRadioJson()
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"Audio API limited. Use in-game radio panel.\"}");
        }
        public static CommandResult SetRadioChannel(string body)
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"Radio channel control requires in-game panel.\"}");
        }
        public static CommandResult SetVolume(string body)
        {
            return CommandResult.FromJson("{\"ok\":true,\"note\":\"Volume control requires in-game settings.\"}");
        }
    }
}
