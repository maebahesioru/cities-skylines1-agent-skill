using System;
using System.Text;
using ColossalFramework;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Info view overlay and DLC-specific (Airport/Campus/Parklife/Industries supply chain) APIs.
    /// </summary>
    public static class InfoViewCommands
    {
        /// <summary>
        /// Get current info view mode.
        /// </summary>
        public static CommandResult BuildInfoViewJson()
        {
            InfoManager im = Singleton<InfoManager>.instance;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            if (im != null)
            {
                json.Append(",\"currentMode\":" + (int)im.CurrentMode);
                json.Append(",\"currentSubMode\":" + (int)im.CurrentSubMode);
                json.Append(",\"modeName\":\"" + JsonUtil.Escape(im.CurrentMode.ToString()) + "\"");
            }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set the info view overlay mode.
        /// Body: { "mode": int|string, "subMode": int }
        /// mode can be: Traffic, Pollution, LandValue, Happiness, Population, Education, Health, Fire, Crime, Water, Electricity, NaturalResources, Transport, etc.
        /// </summary>
        public static CommandResult SetInfoView(string body)
        {
            InfoManager im = Singleton<InfoManager>.instance;
            if (im == null) return CommandResult.Fail("InfoManager is not available.");

            string modeName = JsonUtil.GetString(body, "mode", "");
            int subMode = (int)JsonUtil.GetNumber(body, "subMode", 0f);

            InfoManager.InfoMode oldMode = im.CurrentMode;

            // Try to parse mode name to enum
            if (modeName.Length > 0)
            {
                try
                {
                    InfoManager.InfoMode mode = (InfoManager.InfoMode)Enum.Parse(typeof(InfoManager.InfoMode), modeName);
                    im.SetCurrentMode(mode, (InfoManager.SubInfoMode)subMode);
                }
                catch
                {
                    return CommandResult.Fail("Unknown info mode: " + modeName + ". Try: Traffic, Pollution, LandValue, Happiness, etc.");
                }
            }

            return CommandResult.FromJson("{\"ok\":true,\"newMode\":\"" + JsonUtil.Escape(im.CurrentMode.ToString()) +
                "\",\"newSubMode\":" + (int)im.CurrentSubMode + "}");
        }
    }
}
