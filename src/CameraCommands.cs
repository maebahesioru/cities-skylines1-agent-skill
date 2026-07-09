using System;
using System.Text;
using ColossalFramework;
using UnityEngine;

namespace SkylinesAgentBridge
{
    /// <summary>
    /// Camera control API — smooth movement, focus on buildings, view modes.
    /// Verified against Assembly-CSharp.dll (monodis).
    ///   CameraController.SetTarget(InstanceID, Vector3, bool)
    ///   CameraController.ClearTarget()
    ///   CameraController.SetViewMode(ViewMode)
    ///   CameraController.m_targetPosition / m_targetAngle / m_targetSize / m_targetHeight
    /// </summary>
    public static class CameraCommands
    {
        public static CommandResult GetCameraState()
        {
            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            Vector3 pos = controller.m_currentPosition;
            Vector2 angle = controller.m_currentAngle;
            InstanceID target = controller.GetTarget();

            StringBuilder json = new StringBuilder();
            json.Append("{\"ok\":true");
            json.Append(",\"position\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}");
            json.Append(",\"angle\":{\"x\":" + JsonUtil.Number(angle.x) + ",\"y\":" + JsonUtil.Number(angle.y) + "}");
            json.Append(",\"size\":" + JsonUtil.Number(controller.m_currentSize));
            json.Append(",\"height\":" + JsonUtil.Number(controller.m_currentHeight));
            json.Append(",\"freeCamera\":" + JsonUtil.Bool(controller.m_freeCamera));
            json.Append(",\"hasTarget\":" + JsonUtil.Bool(controller.HasTarget()));
            if (controller.HasTarget())
            {
                json.Append(",\"targetInstance\":{\"type\":\"" + JsonUtil.Escape(target.Type.ToString()) + "\",\"index\":" + target.Index + "}");
            }
            json.Append("}");

            return CommandResult.FromJson(json.ToString());
        }

        /// <summary>
        /// Smoothly move camera to a position.
        /// POST /commands/move-camera
        /// Body: { "position": { "x": 500, "z": 300 }, "y": 200, "angle": { "x": -30, "y": 20 }, "size": 500 }
        /// Sets m_targetPosition/m_targetAngle/m_targetSize for smooth interpolation.
        /// </summary>
        public static CommandResult MoveCamera(string body)
        {
            float x = JsonUtil.GetPointNumber(body, "position", "x", float.NaN);
            float z = JsonUtil.GetPointNumber(body, "position", "z", float.NaN);
            float y = JsonUtil.GetPointNumber(body, "position", "y", float.NaN);

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            bool changed = false;

            // Position
            if (!float.IsNaN(x) && !float.IsNaN(z))
            {
                float useY = float.IsNaN(y) ? controller.m_currentPosition.y : y;
                Vector3 target = new Vector3(x, useY, z);
                controller.m_targetPosition = target;
                changed = true;
            }

            // Angle (pitch=x, yaw=y)
            float angleX = JsonUtil.GetPointNumber(body, "angle", "x", float.NaN);
            float angleY = JsonUtil.GetPointNumber(body, "angle", "y", float.NaN);
            if (!float.IsNaN(angleX) || !float.IsNaN(angleY))
            {
                Vector2 targetAngle = controller.m_targetAngle;
                if (!float.IsNaN(angleX)) targetAngle.x = angleX;
                if (!float.IsNaN(angleY)) targetAngle.y = angleY;
                controller.m_targetAngle = targetAngle;
                changed = true;
            }

            // Size (zoom)
            float size = JsonUtil.GetNumber(body, "size", float.NaN);
            if (!float.IsNaN(size) && size > 0)
            {
                controller.m_targetSize = size;
                changed = true;
            }

            if (!changed) return CommandResult.Fail("No valid camera parameters provided. Use position, angle, and/or size.");

            Vector3 pos = controller.m_targetPosition;
            return CommandResult.FromJson("{\"ok\":true,\"target\":{\"x\":" + JsonUtil.Number(pos.x) +
                ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) +
                "},\"angle\":{\"x\":" + JsonUtil.Number(controller.m_targetAngle.x) +
                ",\"y\":" + JsonUtil.Number(controller.m_targetAngle.y) +
                "},\"size\":" + JsonUtil.Number(controller.m_targetSize) + "}");
        }

        /// <summary>
        /// Focus on a building/entity by ID using the game's smooth camera system.
        /// POST /commands/focus-building
        /// Body: { "id": 123, "type": "building" }
        /// type: "building" (default), "vehicle", "citizen", "prop", "tree", "disaster", "transportline"
        /// Uses CameraController.SetTarget(InstanceID, offset, follow).
        /// </summary>
        public static CommandResult FocusOnBuilding(string body)
        {
            ushort id = (ushort)JsonUtil.GetNumber(body, "id", 0f);
            if (id == 0) return CommandResult.Fail("Entity id required.");

            string typeStr = JsonUtil.GetString(body, "type", "building");
            InstanceType instanceType;
            try { instanceType = (InstanceType)Enum.Parse(typeof(InstanceType), typeStr, true); }
            catch { instanceType = InstanceType.Building; }

            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            InstanceID instanceId = new InstanceID { Type = instanceType, Index = id };

            // Verify entity exists (basic check for buildings)
            if (instanceType == InstanceType.Building)
            {
                BuildingManager bm = BuildingManager.instance;
                Building building = bm.m_buildings.m_buffer[id];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                    return CommandResult.Fail("Building not found: " + id);
            }

            // Smooth target with follow
            bool follow = JsonUtil.GetBool(body, "follow", false);
            Vector3 offset = new Vector3(0, 30f, -20f);
            controller.SetTarget(instanceId, offset, follow);

            // Get position for response
            Vector3 pos = Vector3.zero;
            if (instanceType == InstanceType.Building)
            {
                pos = BuildingManager.instance.m_buildings.m_buffer[id].m_position;
            }

            return CommandResult.FromJson("{\"ok\":true,\"targetType\":\"" + JsonUtil.Escape(typeStr) + "\",\"targetId\":" + id +
                (instanceType == InstanceType.Building ? (",\"buildingPosition\":{\"x\":" + JsonUtil.Number(pos.x) + ",\"y\":" + JsonUtil.Number(pos.y) + ",\"z\":" + JsonUtil.Number(pos.z) + "}") : "") +
                ",\"follow\":" + JsonUtil.Bool(follow) + "}");
        }

        /// <summary>
        /// Clear the camera target (stop following).
        /// POST /commands/clear-camera-target
        /// </summary>
        public static CommandResult ClearCameraTarget()
        {
            CameraController controller = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (controller == null) return CommandResult.Fail("CameraController not found.");

            if (controller.HasTarget())
            {
                controller.ClearTarget();
                return CommandResult.FromJson("{\"ok\":true,\"message\":\"Camera target cleared.\"}");
            }
            return CommandResult.FromJson("{\"ok\":true,\"message\":\"No target was set.\"}");
        }
    }
}
