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
                else if (type == "set-policy")
                {
                    result = DistrictCommands.SetPolicy(command);
                }
                else if (type == "set-budget")
                {
                    result = BudgetCommands.SetBudget(command);
                }
                else if (type == "move-camera")
                {
                    result = CameraCommands.MoveCamera(command);
                }
                else if (type == "focus-building")
                {
                    result = CameraCommands.FocusOnBuilding(command);
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
