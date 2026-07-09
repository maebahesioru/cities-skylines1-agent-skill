using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Screenshot capture — saves current camera view as PNG.
    /// Uses CameraController + RenderManager.
    /// </summary>
    public static class ScreenshotCommands
    {
        public static CommandResult CaptureScreenshot(string body)
        {
            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            string filename = JsonUtil.GetString(body, "filename", "");
            if (string.IsNullOrEmpty(filename))
                filename = "agent_screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            int width = (int)JsonUtil.GetNumber(body, "width", 0f);
            int height = (int)JsonUtil.GetNumber(body, "height", 0f);
            if (width <= 0) width = Screen.width;
            if (height <= 0) height = Screen.height;

            // Ensure extension
            if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                filename += ".png";

            // Save to the game's screenshots folder
            string screenshotsPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "Colossal Order", "Cities_Skylines", "Screenshots");
            string fullPath = System.IO.Path.Combine(screenshotsPath, filename);

            try
            {
                System.IO.Directory.CreateDirectory(screenshotsPath);

                // Render and capture
                Camera camera = controller.m_camera;
                if (camera == null) return CommandResult.Fail("Main camera not found.");

                RenderTexture rt = new RenderTexture(width, height, 24);
                camera.targetTexture = rt;
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = rt;
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();

                byte[] bytes = screenshot.EncodeToPNG();
                System.IO.File.WriteAllBytes(fullPath, bytes);

                // Cleanup
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.Destroy(rt);
                UnityEngine.Object.Destroy(screenshot);

                System.IO.FileInfo fi = new System.IO.FileInfo(fullPath);
                return CommandResult.FromJson("{\"ok\":true,\"file\":\"" + JsonUtil.Escape(filename) +
                    "\",\"path\":\"" + JsonUtil.Escape(fullPath) + "\",\"size\":" + fi.Length +
                    ",\"width\":" + width + ",\"height\":" + height + "}");
            }
            catch (Exception ex)
            {
                return CommandResult.Fail("Screenshot failed: " + ex.Message);
            }
        }
    }
}
