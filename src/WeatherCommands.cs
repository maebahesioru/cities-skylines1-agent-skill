using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class WeatherCommands
    {
        public static CommandResult BuildWeatherJson()
        {
            WeatherManager wm = WeatherManager.instance;
            if (wm == null) return CommandResult.Fail("WeatherManager not found.");

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");

            try
            {
                float windSpeed = wm.GetWindSpeed(Vector2.zero);
                json.Append(",\"windSpeedFactor\":" + JsonUtil.Number(windSpeed));
                float rainbow = wm.GetRainbowVisibility();
                json.Append(",\"rainbowVisibility\":" + JsonUtil.Number(rainbow));
                float aurora = wm.GetNorthernLightsVisibility();
                json.Append(",\"northernLightsVisibility\":" + JsonUtil.Number(aurora));
            }
            catch { }

            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        public static CommandResult LightningStrike(string body)
        {
            float x = JsonUtil.GetPointNumber(body, "position", "x", float.NaN);
            float z = JsonUtil.GetPointNumber(body, "position", "z", float.NaN);

            WeatherManager wm = WeatherManager.instance;
            if (wm == null) return CommandResult.Fail("WeatherManager not found.");

            try
            {
                if (float.IsNaN(x) || float.IsNaN(z))
                    wm.QueueLightningStrike((uint)System.DateTime.Now.Ticks);
                else
                    wm.QueueLightningStrike((uint)System.DateTime.Now.Ticks);

                return CommandResult.FromJson("{\"ok\":true}");
            }
            catch (Exception ex) { return CommandResult.Fail("Error: " + ex.Message); }
        }
    }
}
