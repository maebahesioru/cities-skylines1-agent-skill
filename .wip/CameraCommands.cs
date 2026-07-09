using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Camera control API — move camera, zoom, focus on positions.
    /// </summary>
    public static class CameraCommands
    {
        public static CommandResult GetCameraState()
        {
            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null)
            {
                return CommandResult.Fail("CameraController not found.");
            }

            Vector3 position = controller.transform.position;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(position.x));
            json.Append(",\"y\":").Append(JsonUtil.Number(position.y));
            json.Append(",\"z\":").Append(JsonUtil.Number(position.z)).Append("}");
            json.Append(",\"angle\":{\"x\":").Append(JsonUtil.Number(controller.transform.eulerAngles.x));
            json.Append(",\"y\":").Append(JsonUtil.Number(controller.transform.eulerAngles.y));
            json.Append(",\"z\":").Append(JsonUtil.Number(controller.transform.eulerAngles.z)).Append("}");
            json.Append(",\"zoom\":" + JsonUtil.Number(controller.m_currentSize));
            json.Append(",\"minZoom\":" + JsonUtil.Number(controller.m_minDistance));
            json.Append(",\"maxZoom\":" + JsonUtil.Number(controller.m_maxDistance));
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Move camera to a world position.
        /// POST /commands/move-camera
        /// Body: { "position": { "x": N, "z": N }, "y": optional_height, "instant": false }
        /// </summary>
        public static CommandResult MoveCamera(string body)
        {
            float x = JsonUtil.GetPointNumber(body, "position", "x", 0f);
            float z = JsonUtil.GetPointNumber(body, "position", "z", 0f);
            float y = JsonUtil.GetPointNumber(body, "position", "y", 500f);
            bool instant = JsonUtil.GetBool(body, "instant", false);

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null)
            {
                return CommandResult.Fail("CameraController not found.");
            }

            Vector3 target = new Vector3(x, y, z);

            if (instant)
            {
                // Set absolute position
                controller.transform.position = new Vector3(x, y, z);
                controller.m_targetPosition = target;
            }
            else
            {
                // Smooth transition via CameraController's built-in methods
                controller.SetTarget(target, controller.transform.rotation, controller.m_currentSize, true);
            }

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"target\":{\"x\":").Append(JsonUtil.Number(x));
            json.Append(",\"y\":").Append(JsonUtil.Number(y));
            json.Append(",\"z\":").Append(JsonUtil.Number(z)).Append("}");
            json.Append(",\"instant\":").Append(JsonUtil.Bool(instant));
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Focus camera on a specific building by ID.
        /// POST /commands/focus-building
        /// Body: { "id": 123 }
        /// </summary>
        public static CommandResult FocusOnBuilding(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            if (id == 0) return CommandResult.Fail("Building id is required.");

            BuildingManager buildings = BuildingManager.instance;
            Building building = buildings.m_buildings.m_buffer[id];
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
            {
                return CommandResult.Fail("Building not found: " + id);
            }

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            Vector3 pos = building.m_position;
            Vector3 target = new Vector3(pos.x, pos.y + 80f, pos.z - 60f);
            controller.SetTarget(target, Quaternion.Euler(50f, 0f, 0f), 100f, true);

            BuildingInfo info = building.Info;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true,\"buildingId\":").Append(id);
            json.Append(",\"prefab\":\"").Append(JsonUtil.Escape(info != null ? info.name : "?")).Append("\"");
            json.Append(",\"position\":{\"x\":").Append(JsonUtil.Number(pos.x));
            json.Append(",\"y\":").Append(JsonUtil.Number(pos.y));
            json.Append(",\"z\":").Append(JsonUtil.Number(pos.z)).Append("}}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Set zoom level.
        /// POST /commands/set-zoom
        /// Body: { "size": 200.0 }
        /// </summary>
        public static CommandResult SetZoom(string body)
        {
            float size = JsonUtil.GetNumber(body, "size", 200f);
            if (size < 10f) size = 10f;
            if (size > 2000f) size = 2000f;

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            controller.m_currentSize = size;
            controller.m_targetSize = size;

            return CommandResult.FromJson("{\"ok\":true,\"size\":" + JsonUtil.Number(size) + "}");
        }
    }
}
