using System;
using System.IO;
using System.Text;
using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.IO;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game-level operations: load saves, start new maps, get map info.
    /// </summary>
    public static class GameCommands
    {
        /// <summary>
        /// Load a saved game by name.
        /// POST /commands/load-save
        /// Body: { "name": "MyCity" }
        /// </summary>
        public static CommandResult LoadSave(string body)
        {
            string name = JsonUtil.GetString(body, "name", "");
            if (name.Length == 0) return CommandResult.Fail("Save name is required.");

            string savesPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Saves");

            string filePath = Path.Combine(savesPath, name + ".crp");
            if (!File.Exists(filePath))
            {
                // Try without extension
                filePath = Path.Combine(savesPath, name);
                if (!File.Exists(filePath))
                    return CommandResult.Fail("Save file not found: " + name + ".crp");
            }

            // Loading is async — we queue it and notify
            SimulationManager simulation = Singleton<SimulationManager>.instance;
            LoadingManager loadingManager = Singleton<LoadingManager>.instance;

            try
            {
                // Use the LoadingManager to load the save
                SaveGameMetaData metaData = new SaveGameMetaData();
                loadingManager.LoadLevel(filePath, "Game", "InGame", metaData);

                return CommandResult.FromJson(
                    "{\"ok\":true,\"message\":\"Loading save: " + JsonUtil.Escape(name) +
                    "\",\"path\":\"" + JsonUtil.Escape(filePath) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to load save: " + ex.Message);
            }
        }

        /// <summary>
        /// Start a new game on a specific map.
        /// POST /commands/new-game
        /// Body: { "mapName": "Green Plains" }
        /// </summary>
        public static CommandResult NewGame(string body)
        {
            string mapName = JsonUtil.GetString(body, "mapName", "");
            if (mapName.Length == 0) return CommandResult.Fail("Map name is required.");

            // Find the map in loaded maps
            LoadedMapMetaData[] maps = UnityEngine.Object.FindObjectsOfType<LoadedMapMetaData>();
            string mapPath = null;

            // Fall back to looking in Maps directory
            string mapsPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Maps");

            string[] mapFiles = Directory.GetFiles(mapsPath, "*.crp", SearchOption.AllDirectories);
            foreach (string file in mapFiles)
            {
                if (Path.GetFileNameWithoutExtension(file).IndexOf(mapName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    mapPath = file;
                    break;
                }
            }

            if (mapPath == null)
                return CommandResult.Fail("Map not found: " + mapName);

            LoadingManager loadingManager = Singleton<LoadingManager>.instance;
            try
            {
                SaveGameMetaData metaData = new SaveGameMetaData();
                loadingManager.LoadLevel(mapPath, "Game", "InGame", metaData);

                return CommandResult.FromJson(
                    "{\"ok\":true,\"message\":\"Starting new game on: " + JsonUtil.Escape(mapName) +
                    "\",\"path\":\"" + JsonUtil.Escape(mapPath) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to start new game: " + ex.Message);
            }
        }

        /// <summary>
        /// List available maps.
        /// GET /state/maps
        /// </summary>
        public static CommandResult ListMaps()
        {
            string mapsPath = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Maps");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"maps\":[");
            int count = 0;

            if (Directory.Exists(mapsPath))
            {
                string[] files = Directory.GetFiles(mapsPath, "*.crp", SearchOption.AllDirectories);
                for (int i = 0; i < Math.Min(files.Length, 200); i++)
                {
                    string file = files[i];
                    if (count > 0) json.Append(",");
                    FileInfo fi = new FileInfo(file);
                    json.Append("{\"name\":\"").Append(JsonUtil.Escape(Path.GetFileNameWithoutExtension(file))).Append("\"");
                    json.Append(",\"size\":" + fi.Length);
                    json.Append(",\"path\":\"").Append(JsonUtil.Escape(file)).Append("\"}");
                    count++;
                }
            }

            json.Append("],\"total\":" + count + "}");
            return CommandResult.FromJson(json.ToString());
        }
    }
}
