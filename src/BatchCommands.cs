using System.Collections.Generic;
using System.Text;

namespace SkylinesAgentBridge
{
    public static class BatchCommands
    {
        public static CommandResult Execute(string body)
        {
            List<string> commands = JsonUtil.GetObjectArray(body, "commands");
            bool stopOnError = JsonUtil.GetBool(body, "stopOnError", true);
            bool defaultDryRun = JsonUtil.GetBool(body, "dryRun", true);

            if (commands.Count == 0)
            {
                return CommandResult.Fail("Batch request must include a commands array.");
            }

            if (commands.Count > 32)
            {
                return CommandResult.Fail("Batch command limit is 32.");
            }

            StringBuilder items = new StringBuilder();
            bool allOk = true;
            int executed = 0;

            for (int i = 0; i < commands.Count; i++)
            {
                string command = EnsureDryRunDefault(commands[i], defaultDryRun);
                string type = JsonUtil.GetString(command, "type", "");
                CommandResult result;

                if (type == "build-road")
                {
                    result = RoadCommands.BuildRoad(command);
                }
                else if (type == "set-zone")
                {
                    result = ZoneCommands.SetZone(command);
                }
                else if (type == "repair-zones-to-growables")
                {
                    result = ZoneCommands.RepairZonesToGrowables(command);
                }
                else if (type == "repair-zone-clusters")
                {
                    result = ZoneCommands.RepairZoneClusters(command);
                }
                else if (type == "place-building")
                {
                    result = BuildingCommands.PlaceBuilding(command);
                }
                else if (type == "move-building")
                {
                    result = BuildingCommands.MoveBuilding(command);
                }
                else if (type == "bulldoze")
                {
                    result = BulldozeCommands.Bulldoze(command);
                }
                else if (type == "set-simulation-speed")
                {
                    result = SimulationCommands.SetSimulationSpeed(command);
                }
                else if (type == "set-tax-rate")
                {
                    result = EconomyCommands.SetTaxRate(command);
                }
                else if (type == "save")
                {
                    result = SaveCommands.Save(command);
                }
                else if (type == "set-budget")
                {
                    result = BudgetCommands.SetBudget(command);
                }
                else if (type == "set-policy")
                {
                    result = DistrictCommands.SetPolicy(command);
                }
                else if (type == "create-disaster")
                {
                    result = DisasterCommands.CreateDisaster(command);
                }
                else if (type == "evacuate")
                {
                    result = DisasterCommands.EvacuateAll();
                }
                else if (type == "start-random-disaster")
                {
                    result = DisasterCommands.StartRandomDisaster();
                }
                else if (type == "screenshot")
                {
                    result = ScreenshotCommands.CaptureScreenshot(command);
                }
                else if (type == "move-camera")
                {
                    result = CameraCommands.MoveCamera(command);
                }
                else if (type == "focus-building")
                {
                    result = CameraCommands.FocusOnBuilding(command);
                }
                else if (type == "new-game")
                {
                    result = GameCommands.NewGame(command);
                }
                else if (type == "load-game")
                {
                    result = GameCommands.LoadGame(command);
                }
                else if (type == "unlock-area")
                {
                    result = GameAreaCommands.UnlockArea(command);
                }
                else if (type == "plant-tree")
                {
                    result = TreeCommands.PlantTree(command);
                }
                else if (type == "place-prop")
                {
                    result = PropCommands.PlaceProp(command);
                }
                else if (type == "lightning-strike")
                {
                    result = WeatherCommands.LightningStrike(command);
                }
                else if (type == "set-natural-resource")
                {
                    result = NaturalResourceCommands.SetResource(command);
                }
                else if (type == "modify-terrain")
                {
                    result = TerrainCommands.ModifyTerrain(command);
                }
                else if (type == "set-water-level")
                {
                    result = TerrainCommands.SetWaterLevel(command);
                }
                else if (type == "create-transport-line")
                {
                    result = TransportLineCommands.CreateTransportLine(command);
                }
                else if (type == "delete-transport-line")
                {
                    result = TransportLineCommands.DeleteTransportLine(command);
                }
                else if (type == "add-stop")
                {
                    result = TransportLineCommands.AddStop(command);
                }
                else if (type == "remove-stop")
                {
                    result = TransportLineCommands.RemoveStop(command);
                }
                else if (type == "dismiss-notification")
                {
                    result = NotificationCommands.DismissNotification(command);
                }
                else if (type == "level-up")
                {
                    result = LevelCommands.LevelUpBuilding(command);
                }
                else if (type == "level-down")
                {
                    result = LevelCommands.LevelDownBuilding(command);
                }
                else if (type == "set-industry-type")
                {
                    result = IndustriesCommands.SetIndustryType(command);
                }
                else if (type == "set-park-budget")
                {
                    result = IndustriesCommands.SetParkBudget(command);
                }
                else if (type == "set-district-style")
                {
                    result = IndustriesCommands.SetDistrictStyle(command);
                }
                else if (type == "set-traffic-light")
                {
                    result = NetworkDetailCommands.SetTrafficLight(command);
                }
                else if (type == "set-policy-full")
                {
                    result = PolicyDetailCommands.SetPolicyFull(command);
                }
                else if (type == "rename-road")
                {
                    result = TrafficControlCommands.RenameRoad(command);
                }
                else if (type == "evacuate-building")
                {
                    result = EnhancedCommands.EvacuateBuilding(command);
                }
                else if (type == "upgrade-building")
                {
                    result = EnhancedCommands.UpgradeBuilding(command);
                }
                else if (type == "set-radio-channel")
                {
                    result = AudioCommands.SetRadioChannel(command);
                }
                else if (type == "set-volume")
                {
                    result = AudioCommands.SetVolume(command);
                }
                else if (type == "set-info-view")
                {
                    result = InfoViewCommands.SetInfoView(command);
                }
                else if (type == "console")
                {
                    result = ConsoleCommands.ExecuteConsole(command);
                }
                else
                {
                    result = CommandResult.Fail("Unsupported command type: " + type);
                }

                if (i > 0)
                {
                    items.Append(",");
                }

                items.Append("{\"index\":").Append(i);
                items.Append(",\"type\":\"").Append(JsonUtil.Escape(type)).Append("\"");
                items.Append(",\"result\":").Append(result.Json).Append("}");

                if (result.Ok)
                {
                    executed++;
                }
                else
                {
                    allOk = false;
                    if (stopOnError)
                    {
                        AppendSkipped(items, commands, i + 1);
                        break;
                    }
                }
            }

            StringBuilder results = new StringBuilder();
            results.Append("{\"ok\":").Append(JsonUtil.Bool(allOk));
            results.Append(",\"results\":[").Append(items.ToString());
            results.Append("],\"executed\":").Append(executed);
            results.Append(",\"allOk\":").Append(JsonUtil.Bool(allOk));
            results.Append("}");

            return CommandResult.FromJson(results.ToString());
        }

        private static string EnsureDryRunDefault(string command, bool defaultDryRun)
        {
            if (command.IndexOf("\"dryRun\"") >= 0)
            {
                return command;
            }

            int insert = command.LastIndexOf('}');
            if (insert < 0)
            {
                return command;
            }

            string suffix = command.Length > 2 ? "," : "";
            return command.Substring(0, insert) + suffix + "\"dryRun\":" + JsonUtil.Bool(defaultDryRun) + command.Substring(insert);
        }

        private static void AppendSkipped(StringBuilder results, List<string> commands, int start)
        {
            for (int i = start; i < commands.Count; i++)
            {
                string type = JsonUtil.GetString(commands[i], "type", "");
                results.Append(",{\"index\":").Append(i);
                results.Append(",\"type\":\"").Append(JsonUtil.Escape(type)).Append("\"");
                results.Append(",\"skipped\":true}");
            }
        }
    }
}
