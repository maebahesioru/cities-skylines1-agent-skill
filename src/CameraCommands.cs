using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Camera control API — move camera, focus on buildings.
    /// Experimental: some method signatures may need in-game verification.
    /// </summary>
    public static class CameraCommands
    {
        public static CommandResult GetCameraState()
        {
            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            Vector3 pos = controller.transform.position;
            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}");
            json.Append(",\"angle\":{\"x\":" + JsonUtil.Number(controller.transform.eulerAngles.x) + ",\"y\":" + JsonUtil.Number(controller.transform.eulerAngles.y) + ",\"z\":" + JsonUtil.Number(controller.transform.eulerAngles.z) + "}");
            json.Append(",\"currentSize\":" + JsonUtil.Number(controller.m_currentSize));
            json.Append("}");
            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Move camera to a position.
        /// POST /commands/move-camera
        /// Body: { "position": { "x": 500, "z": 300 }, "y": 200 }
        /// </summary>
        public static CommandResult MoveCamera(string body)
        {
            float x = JsonUtil.GetPointNumber(body, "position", "x", 0f);
            float z = JsonUtil.GetPointNumber(body, "position", "z", 0f);
            float y = JsonUtil.GetPointNumber(body, "position", "y", 500f);

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            Vector3 target = new Vector3(x, y, z);
            controller.transform.position = target;
            controller.m_targetPosition = target;

            return CommandResult.FromJson("{\"ok\":true,\"target\":{\"x\":" + JsonUtil.Number(x) + ",\"y\":" + JsonUtil.Number(y) + ",\"z\":" + JsonUtil.Number(z) + "}}");
        }

        /// <summary>
        /// Focus on a building by ID.
        /// POST /commands/focus-building
        /// Body: { "id": 123 }
        /// </summary>
        public static CommandResult FocusOnBuilding(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            if (id == 0) return CommandResult.Fail("Building id required.");

            BuildingManager bm = BuildingManager.instance;
            Building building = bm.m_buildings.m_buffer[id];
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                return CommandResult.Fail("Building not found: " + id);

            Vector3 pos = building.m_position;
            BuildingInfo info = building.Info;

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            // Move camera to look at building from above
            Vector3 target = new Vector3(pos.x, pos.y + 100f, pos.z - 80f);
            controller.transform.position = target;
            controller.m_targetPosition = target;

            return CommandResult.FromJson("{\"ok\":true,\"buildingId\":" + id +
                ",\"prefab\":\"" + JsonUtil.Escape(info != null ? info.name : "?") + "\"" +
                ",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}}");
        }
    }
}
