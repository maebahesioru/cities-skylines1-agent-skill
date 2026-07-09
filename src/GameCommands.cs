using System;
using System.IO;
using System.Text;
using ColossalFramework;
using ColossalFramework.Packaging;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game-level operations: list saves, list maps, new game, load game.
    /// Verified against Assembly-CSharp.dll (monodis):
    ///   LoadingManager.LoadLevel(string mapName, string uiScene, string assetEditorScene, SimulationMetaData meta)
    ///   LoadingManager.LoadLevel(Package.Asset, string, string, SimulationMetaData, bool)
    ///   LoadingManager.SaveLevel(string name)
    ///   SimulationMetaData: m_MapName, m_environment, m_CityName, m_currentDateTime, etc.
    /// </summary>
    public static class GameCommands
    {
        /// <summary>
        /// List available save files.
        /// GET /state/maps
        /// </summary>
        public static CommandResult ListMaps()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // --- Save files ---
            string savesPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Saves");

            json.Append(",\"saves\":[");
            int count = 0;
            if (System.IO.Directory.Exists(savesPath))
            {
                string[] files = System.IO.Directory.GetFiles(savesPath, "*.crp");
                for (int i = 0; i < System.Math.Min(files.Length, 200); i++)
                {
                    string file = files[i];
                    if (count > 0) json.Append(",");
                    FileInfo fi = new FileInfo(file);
                    json.Append("{\"name\":\"" + JsonUtil.Escape(System.IO.Path.GetFileNameWithoutExtension(file)) + "\"");
                    json.Append(",\"file\":\"" + JsonUtil.Escape(System.IO.Path.GetFileName(file)) + "\"");
                    json.Append(",\"size\":" + fi.Length);
                    json.Append(",\"modified\":\"" + JsonUtil.Escape(fi.LastWriteTime.ToString("s")) + "\"}");
                    count++;
                }
            }
            json.Append("],\"totalSaves\":" + count);

            // --- Available maps (from map editor packages) ---
            json.Append(",\"maps\":[");
            int mapCount = 0;
            try
            {
                // Maps are stored as Package.Asset with type Map
                foreach (Package.Asset asset in PackageManager.GetAssetsOfType(UserAssetType.MapMetaData))
                {
                    if (asset == null || !asset.isEnabled) continue;
                    if (mapCount > 0) json.Append(",");
                    json.Append("{\"name\":\"" + JsonUtil.Escape(asset.name) + "\"");
                    json.Append(",\"packageName\":\"" + JsonUtil.Escape(asset.package?.packageName ?? "?") + "\"}");
                    mapCount++;
                    if (mapCount >= 100) break;
                }
            }
            catch { /* PackageManager may not be available in all contexts */ }
            json.Append("],\"totalMaps\":" + mapCount);

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Start a new game with a map.
        /// POST /commands/new-game
        /// Body: { "mapName": "Green Plains", "cityName": "My City" }
        /// </summary>
        public static CommandResult NewGame(string body)
        {
            LoadingManager lm = LoadingManager.instance;
            if (lm == null) return CommandResult.Fail("LoadingManager not found.");

            string mapName = JsonUtil.GetString(body, "mapName", "");
            if (string.IsNullOrEmpty(mapName))
                return CommandResult.Fail("mapName required. Available maps can be listed via GET /state/maps.");

            string cityName = JsonUtil.GetString(body, "cityName", mapName);

            // Find the map asset
            Package.Asset mapAsset = null;
            foreach (Package.Asset asset in PackageManager.GetAssetsOfType(UserAssetType.MapMetaData))
            {
                if (asset != null && asset.isEnabled && asset.name == mapName)
                {
                    mapAsset = asset;
                    break;
                }
            }
            if (mapAsset == null)
                return CommandResult.Fail("Map not found: " + mapName + ". Check available maps via GET /state/maps.");

            // Build SimulationMetaData
            SimulationMetaData meta = new SimulationMetaData();
            meta.m_MapName = mapName;
            meta.m_CityName = cityName;
            meta.m_environment = "Sunny"; // Default
            meta.m_currentDateTime = System.DateTime.Now;
            meta.m_startingDateTime = System.DateTime.Now;
            meta.m_updateMode = SimulationManager.UpdateMode.LoadSimulation;

            try
            {
                lm.LoadLevel(mapAsset, "InGame", "InAssetEditor", meta, false);
                return CommandResult.FromJson("{\"ok\":true,\"mapName\":\"" + JsonUtil.Escape(mapName) +
                    "\",\"cityName\":\"" + JsonUtil.Escape(cityName) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to start new game: " + ex.Message);
            }
        }

        /// <summary>
        /// Load a saved game.
        /// POST /commands/load-game
        /// Body: { "name": "MyCity", "file": "MyCity.crp" }
        /// Provide either 'name' (without .crp) or 'file' (with extension).
        /// </summary>
        public static CommandResult LoadGame(string body)
        {
            LoadingManager lm = LoadingManager.instance;
            if (lm == null) return CommandResult.Fail("LoadingManager not found.");

            string name = JsonUtil.GetString(body, "name", "");
            string file = JsonUtil.GetString(body, "file", "");
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(file))
                return CommandResult.Fail("Either 'name' or 'file' required.");

            if (!string.IsNullOrEmpty(name) && string.IsNullOrEmpty(file))
                file = name + ".crp";

            string savesPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Saves");

            string fullPath = System.IO.Path.Combine(savesPath, file);
            if (!System.IO.File.Exists(fullPath))
                return CommandResult.Fail("Save file not found: " + file);

            try
            {
                // Use LoadLevel with save file path
                SimulationMetaData meta = new SimulationMetaData();
                meta.m_updateMode = SimulationManager.UpdateMode.LoadSimulation;
                lm.LoadLevel(fullPath, "InGame", "InAssetEditor", meta);
                return CommandResult.FromJson("{\"ok\":true,\"loaded\":\"" + JsonUtil.Escape(file) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to load game: " + ex.Message);
            }
        }

        /// <summary>
        /// Quit to main menu.
        /// POST /commands/quit-to-menu
        /// </summary>
        public static CommandResult QuitToMenu()
        {
            LoadingManager lm = LoadingManager.instance;
            if (lm == null) return CommandResult.Fail("LoadingManager not found.");

            try
            {
                lm.QuitApplication();
                return CommandResult.FromJson("{\"ok\":true,\"message\":\"Quitting to main menu.\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to quit: " + ex.Message);
            }
        }
    }
}
