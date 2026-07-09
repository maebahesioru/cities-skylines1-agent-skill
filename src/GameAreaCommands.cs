using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Game area (tile) management — buy, check, list city tiles.
    /// </summary>
    public static class GameAreaCommands
    {
        public static CommandResult BuildAreasJson()
        {
            GameAreaManager gam = GameAreaManager.instance;
            if (gam == null) return CommandResult.Fail("GameAreaManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"areaCount\":" + gam.m_areaCount);
            json.Append(",\"maxAreaCount\":" + gam.m_maxAreaCount);

            json.Append(",\"tiles\":[");
            int count = 0;
            int gridSize = 5;
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    int idx = z * gridSize + x;
                    bool unlocked = gam.IsUnlocked(x, z);
                    bool canUnlock = !unlocked && gam.CanUnlock(x, z);

                    if (count > 0) json.Append(",");
                    json.Append("{\"x\":" + x + ",\"z\":" + z + ",\"index\":" + idx);
                    json.Append(",\"unlocked\":" + JsonUtil.Bool(unlocked));
                    json.Append(",\"canUnlock\":" + JsonUtil.Bool(canUnlock) + "}");
                    count++;
                }
            }
            json.Append("],\"total\":" + count);

            int sx = 0, sz = 0;
            gam.GetStartTile(out sx, out sz);
            json.Append(",\"startTile\":{\"x\":" + sx + ",\"z\":" + sz + "}");

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult UnlockArea(string body)
        {
            int x = (int)JsonUtil.GetNumber(body, "x", -1f);
            int z = (int)JsonUtil.GetNumber(body, "z", -1f);
            int index = (int)JsonUtil.GetNumber(body, "index", -1f);

            if (index < 0 && (x < 0 || z < 0))
                return CommandResult.Fail("x/z or index required.");
            if (index < 0) index = z * 5 + x;

            GameAreaManager gam = GameAreaManager.instance;
            if (gam == null) return CommandResult.Fail("GameAreaManager not found.");

            try
            {
                bool unlocked = gam.UnlockArea(index);
                return CommandResult.FromJson("{\"ok\":true,\"unlocked\":" + JsonUtil.Bool(unlocked) + ",\"index\":" + index + "}");
            }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
