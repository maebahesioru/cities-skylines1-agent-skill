using UnityEngine;

namespace SkylinesAgentBridge
{
    public static class ZoneCommands
    {
        public static CommandResult SetZone(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            bool preserveOccupied = JsonUtil.GetBool(body, "preserveOccupied", true);
            string zoneName = JsonUtil.GetString(body, "zone", "ResidentialLow");
            float radius = JsonUtil.GetNumber(body, "radius", 32f);
            Vector3 center = ReadPoint(body, "center");

            if (radius <= 0f || radius > 256f)
            {
                return CommandResult.Fail("Radius must be between 0 and 256.");
            }

            ItemClass.Zone zone;
            if (!TryParseZone(zoneName, out zone))
            {
                return CommandResult.Fail("Unsupported zone: " + zoneName);
            }

            ZoneManager manager = ZoneManager.instance;
            float radiusSq = radius * radius;
            int touchedBlocks = 0;
            int skippedOccupiedBlocks = 0;
            int changedCells = 0;

            for (int i = 1; i < manager.m_blocks.m_buffer.Length; i++)
            {
                ZoneBlock block = manager.m_blocks.m_buffer[i];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) == 0)
                {
                    continue;
                }

                Vector3 delta = block.m_position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSq)
                {
                    continue;
                }

                touchedBlocks++;
                if (preserveOccupied && ZoneBlockHasProtectedBuilding(block))
                {
                    skippedOccupiedBlocks++;
                    continue;
                }

                if (!dryRun)
                {
                    int rows = block.RowCount;
                    if (rows <= 0)
                    {
                        rows = 4;
                    }
                    if (rows > 8)
                    {
                        rows = 8;
                    }

                    for (int z = 0; z < rows; z++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            try
                            {
                                if (manager.m_blocks.m_buffer[i].SetZone(x, z, zone))
                                {
                                    changedCells++;
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.Log("[SkylinesAgentBridge] Skipped zone cell " + i + "/" + x + "/" + z + ": " + ex.Message);
                            }
                        }
                    }

                    try
                    {
                        manager.m_blocks.m_buffer[i].RefreshZoning((ushort)i);
                        manager.UpdateBlock((ushort)i);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.Log("[SkylinesAgentBridge] Failed to refresh zone block " + i + ": " + ex.Message);
                    }
                }
            }

            if (dryRun)
            {
                return CommandResult.FromJson("{\"ok\":true,\"dryRun\":true,\"zone\":\"" + JsonUtil.Escape(zoneName) + "\",\"preserveOccupied\":" + JsonUtil.Bool(preserveOccupied) + ",\"matchingBlocks\":" + touchedBlocks + ",\"skippedOccupiedBlocks\":" + skippedOccupiedBlocks + "}");
            }

            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":false,\"zone\":\"" + JsonUtil.Escape(zoneName) + "\",\"preserveOccupied\":" + JsonUtil.Bool(preserveOccupied) + ",\"touchedBlocks\":" + touchedBlocks + ",\"skippedOccupiedBlocks\":" + skippedOccupiedBlocks + ",\"changedCells\":" + changedCells + "}");
        }

        public static CommandResult RepairZonesToGrowables(string body)
        {
            bool dryRun = JsonUtil.GetBool(body, "dryRun", false);
            ZoneManager manager = ZoneManager.instance;
            int inspectedBlocks = 0;
            int repairableBlocks = 0;
            int skippedMixedUseBlocks = 0;
            int changedCells = 0;

            for (int i = 1; i < manager.m_blocks.m_buffer.Length; i++)
            {
                ZoneBlock block = manager.m_blocks.m_buffer[i];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) == 0)
                {
                    continue;
                }

                inspectedBlocks++;
                ItemClass.Zone expectedZone;
                bool hasGrowable;
                bool mixedUse;
                GetExpectedZoneFromNearbyGrowables(block, out expectedZone, out hasGrowable, out mixedUse);
                if (!hasGrowable)
                {
                    continue;
                }
                if (mixedUse)
                {
                    skippedMixedUseBlocks++;
                    continue;
                }

                int rows = GetRowCount(block);
                bool needsRepair = false;
                for (int z = 0; z < rows; z++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        ItemClass.Zone current = block.GetZone(x, z);
                        if (current != ItemClass.Zone.Unzoned && current != expectedZone)
                        {
                            needsRepair = true;
                        }
                    }
                }

                if (!needsRepair)
                {
                    continue;
                }

                repairableBlocks++;
                if (dryRun)
                {
                    continue;
                }

                for (int z = 0; z < rows; z++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        try
                        {
                            ItemClass.Zone current = manager.m_blocks.m_buffer[i].GetZone(x, z);
                            if (current != ItemClass.Zone.Unzoned && manager.m_blocks.m_buffer[i].SetZone(x, z, expectedZone))
                            {
                                changedCells++;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.Log("[SkylinesAgentBridge] Skipped growable zone repair cell " + i + "/" + x + "/" + z + ": " + ex.Message);
                        }
                    }
                }

                try
                {
                    manager.m_blocks.m_buffer[i].RefreshZoning((ushort)i);
                    manager.UpdateBlock((ushort)i);
                }
                catch (System.Exception ex)
                {
                    Debug.Log("[SkylinesAgentBridge] Failed to refresh repaired zone block " + i + ": " + ex.Message);
                }
            }

            return CommandResult.FromJson("{\"ok\":true,\"dryRun\":" + JsonUtil.Bool(dryRun) +
                ",\"inspectedBlocks\":" + inspectedBlocks +
                ",\"repairableBlocks\":" + repairableBlocks +
                ",\"skippedMixedUseBlocks\":" + skippedMixedUseBlocks +
                ",\"changedCells\":" + changedCells + "}");
        }

        private static bool TryParseZone(string name, out ItemClass.Zone zone)
        {
            if (name == "Unzoned") { zone = ItemClass.Zone.Unzoned; return true; }
            if (name == "ResidentialLow") { zone = ItemClass.Zone.ResidentialLow; return true; }
            if (name == "ResidentialHigh") { zone = ItemClass.Zone.ResidentialHigh; return true; }
            if (name == "CommercialLow") { zone = ItemClass.Zone.CommercialLow; return true; }
            if (name == "CommercialHigh") { zone = ItemClass.Zone.CommercialHigh; return true; }
            if (name == "Industrial") { zone = ItemClass.Zone.Industrial; return true; }
            if (name == "Office") { zone = ItemClass.Zone.Office; return true; }
            zone = ItemClass.Zone.Unzoned;
            return false;
        }

        private static bool ZoneBlockHasProtectedBuilding(ZoneBlock block)
        {
            BuildingManager manager = BuildingManager.instance;
            for (ushort i = 1; i < manager.m_buildings.m_buffer.Length; i++)
            {
                Building building = manager.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                BuildingInfo info = building.Info;
                if (info == null || info.m_class == null || IsInternalNetworkHelperBuilding(info))
                {
                    continue;
                }

                if (!ShouldProtectBuilding(info))
                {
                    continue;
                }

                float protectionRadius = Mathf.Max(info.GetWidth(), info.GetLength()) * 4f + 38f;
                Vector3 delta = building.m_position - block.m_position;
                delta.y = 0f;
                if (delta.sqrMagnitude <= protectionRadius * protectionRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static void GetExpectedZoneFromNearbyGrowables(ZoneBlock block, out ItemClass.Zone expectedZone, out bool hasGrowable, out bool mixedUse)
        {
            expectedZone = ItemClass.Zone.Unzoned;
            hasGrowable = false;
            mixedUse = false;
            BuildingManager manager = BuildingManager.instance;
            float closestSq = float.MaxValue;

            for (ushort i = 1; i < manager.m_buildings.m_buffer.Length; i++)
            {
                Building building = manager.m_buildings.m_buffer[i];
                if ((building.m_flags & Building.Flags.Created) == Building.Flags.None)
                {
                    continue;
                }

                BuildingInfo info = building.Info;
                if (info == null || info.m_class == null)
                {
                    continue;
                }

                ItemClass.Zone buildingZone;
                if (!TryGetGrowableZone(info, out buildingZone))
                {
                    continue;
                }

                float protectionRadius = Mathf.Max(info.GetWidth(), info.GetLength()) * 4f + 38f;
                Vector3 delta = building.m_position - block.m_position;
                delta.y = 0f;
                if (delta.sqrMagnitude > protectionRadius * protectionRadius)
                {
                    continue;
                }

                if (!hasGrowable || delta.sqrMagnitude < closestSq)
                {
                    expectedZone = buildingZone;
                    hasGrowable = true;
                    closestSq = delta.sqrMagnitude;
                }
                else if (expectedZone != buildingZone && Mathf.Abs(delta.sqrMagnitude - closestSq) < 100f)
                {
                    mixedUse = true;
                    return;
                }
            }
        }

        private static bool TryGetGrowableZone(BuildingInfo info, out ItemClass.Zone zone)
        {
            string service = info.m_class.m_service.ToString();
            string subService = info.m_class.m_subService.ToString();
            if (service == "Residential")
            {
                zone = subService == "ResidentialHigh" ? ItemClass.Zone.ResidentialHigh : ItemClass.Zone.ResidentialLow;
                return true;
            }
            if (service == "Commercial")
            {
                zone = subService == "CommercialHigh" ? ItemClass.Zone.CommercialHigh : ItemClass.Zone.CommercialLow;
                return true;
            }
            if (service == "Industrial")
            {
                zone = ItemClass.Zone.Industrial;
                return true;
            }
            if (service == "Office")
            {
                zone = ItemClass.Zone.Office;
                return true;
            }

            zone = ItemClass.Zone.Unzoned;
            return false;
        }

        private static int GetRowCount(ZoneBlock block)
        {
            int rows = block.RowCount;
            if (rows <= 0)
            {
                rows = 4;
            }
            if (rows > 8)
            {
                rows = 8;
            }
            return rows;
        }

        private static bool ShouldProtectBuilding(BuildingInfo info)
        {
            string service = info.m_class.m_service.ToString();
            return service == "Residential" ||
                service == "Commercial" ||
                service == "Industrial" ||
                service == "Office" ||
                service == "Water" ||
                service == "Electricity" ||
                service == "Garbage" ||
                service == "HealthCare" ||
                service == "PoliceDepartment" ||
                service == "FireDepartment" ||
                service == "Education" ||
                service == "Disaster" ||
                service == "Beautification" ||
                service == "Monument";
        }

        private static bool IsInternalNetworkHelperBuilding(BuildingInfo info)
        {
            return info.name == "Water Pipe Junction" || info.name == "Heating Pipe Junction";
        }

        private static Vector3 ReadPoint(string body, string name)
        {
            float x = JsonUtil.GetPointNumber(body, name, "x", 0f);
            float z = JsonUtil.GetPointNumber(body, name, "z", 0f);
            float y = JsonUtil.GetPointNumber(body, name, "y", 0f);
            return new Vector3(x, y, z);
        }
    }
}
