using System;
using System.IO;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game-level operations: list saves, new game, load game.
    /// </summary>
    public static class GameCommands
    {
        public static CommandResult ListMaps()
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            // Save files
            string savesPath = System.IO.Path.Combine(
                System.IO.Path.Combine(
                    System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                        "Colossal Order"),
                    "Cities_Skylines"),
                "Saves");

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
            json.Append(",\"maps\":[],\"totalMaps\":0}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult NewGame(string body)
        {
            LoadingManager lm = LoadingManager.instance;
            if (lm == null) return CommandResult.Fail("LoadingManager not found.");

            string mapName = JsonUtil.GetString(body, "mapName", "");
            if (string.IsNullOrEmpty(mapName))
                return CommandResult.Fail("mapName required.");

            string cityName = JsonUtil.GetString(body, "cityName", mapName);

            SimulationMetaData meta = new SimulationMetaData();
            meta.m_MapName = mapName;
            meta.m_CityName = cityName;
            meta.m_environment = "Sunny";
            meta.m_currentDateTime = System.DateTime.Now;
            meta.m_startingDateTime = System.DateTime.Now;
            meta.m_updateMode = SimulationManager.UpdateMode.NewGameFromMap;

            try
            {
                lm.LoadLevel(mapName, "InGame", "InAssetEditor", meta);
                return CommandResult.FromJson("{\"ok\":true,\"mapName\":\"" + JsonUtil.Escape(mapName) +
                    "\",\"cityName\":\"" + JsonUtil.Escape(cityName) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to start new game: " + ex.Message);
            }
        }

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
                System.IO.Path.Combine(
                    System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                        "Colossal Order"),
                    "Cities_Skylines"),
                "Saves");
            string fullPath = System.IO.Path.Combine(savesPath, file);

            if (!System.IO.File.Exists(fullPath))
                return CommandResult.Fail("Save file not found: " + file);

            try
            {
                SimulationMetaData meta = new SimulationMetaData();
                meta.m_updateMode = SimulationManager.UpdateMode.LoadGame;
                lm.LoadLevel(fullPath, "InGame", "InAssetEditor", meta);
                return CommandResult.FromJson("{\"ok\":true,\"loaded\":\"" + JsonUtil.Escape(file) + "\"}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Failed to load game: " + ex.Message);
            }
        }

        public static CommandResult QuitToMenu()
        {
            LoadingManager lm = LoadingManager.instance;
            if (lm == null) return CommandResult.Fail("LoadingManager not found.");
            try { lm.QuitApplication(); return CommandResult.FromJson("{\"ok\":true}"); }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
