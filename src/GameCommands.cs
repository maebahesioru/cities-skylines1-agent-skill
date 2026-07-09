using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game-level operations: list saves, maps.
    /// Simplified version with verified field names.
    /// Load/new-game use LoadingManager which needs in-game verification.
    /// </summary>
    public static class GameCommands
    {
        /// <summary>
        /// List available maps for new games.
        /// GET /state/maps
        /// </summary>
        public static CommandResult ListMaps()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"maps\":[");

            // Maps are loaded scenes accessible via LoadingManager
            // For now, list save files which are more reliably accessible
            string savesPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Saves");

            int count = 0;
            if (System.IO.Directory.Exists(savesPath))
            {
                string[] files = System.IO.Directory.GetFiles(savesPath, "*.crp");
                for (int i = 0; i < System.Math.Min(files.Length, 200); i++)
                {
                    string file = files[i];
                    if (count > 0) json.Append(",");
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    json.Append("{\"name\":\"").Append(JsonUtil.Escape(System.IO.Path.GetFileNameWithoutExtension(file))).Append("\"");
                    json.Append(",\"size\":" + fi.Length);
                    json.Append(",\"modified\":\"").Append(JsonUtil.Escape(fi.LastWriteTime.ToString("s"))).Append("\"}");
                    count++;
                }
            }
            json.Append("],\"total\":" + count + "}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
