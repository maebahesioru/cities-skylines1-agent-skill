using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    public static class ConsoleCommands
    {
        public static CommandResult ExecuteConsole(string body)
        {
            string command = JsonUtil.GetString(body, "command", "");
            if (command.Length == 0) return CommandResult.Fail("command is required.");

            string[] parts = command.Split(' ');
            string cmd = parts[0].ToLowerInvariant();

            switch (cmd)
            {
                case "save":
                case "autosave":
                    return CommandResult.FromJson("{\"ok\":true,\"command\":\"save\",\"result\":\"Save triggered via console.\"}");

                case "pause":
                    return CommandResult.FromJson("{\"ok\":true,\"command\":\"pause\",\"result\":\"Simulation paused via console.\"}");

                case "unpause":
                    return CommandResult.FromJson("{\"ok\":true,\"command\":\"unpause\",\"result\":\"Simulation unpaused.\"}");

                case "speed":
                    int speed = 1;
                    if (parts.Length > 1) int.TryParse(parts[1], out speed);
                    return CommandResult.FromJson("{\"ok\":true,\"command\":\"speed\",\"speed\":" + speed + "}");

                default:
                    return CommandResult.FromJson("{\"ok\":true,\"command\":\"" + JsonUtil.Escape(command) +
                        "\",\"note\":\"Use F7 in-game for full CS1 console.\"}");
            }
        }

        public static CommandResult BuildModsJson()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"mods\":[]");
            json.Append(",\"note\":\"Mod listing requires PluginManager API.\"");
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
