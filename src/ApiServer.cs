using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SkylinesAgentBridge
{
    public sealed class ApiServer
    {
        private readonly AgentBridge bridge;
        private readonly int port;
        private TcpListener listener;
        private Thread thread;
        private volatile bool running;

        public ApiServer(AgentBridge bridge, int port)
        {
            this.bridge = bridge;
            this.port = port;
        }

        public bool IsRunning
        {
            get { return running; }
        }

        public void Start()
        {
            if (running)
            {
                return;
            }

            listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            running = true;

            thread = new Thread(AcceptLoop);
            thread.IsBackground = true;
            thread.Name = "Skylines Agent Bridge API";
            thread.Start();

            Debug.Log("[SkylinesAgentBridge] API server listening on http://127.0.0.1:" + port);
        }

        private void AcceptLoop()
        {
            while (running)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (Exception ex)
                {
                    Debug.Log("[SkylinesAgentBridge] API accept failed: " + ex.Message);
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = (TcpClient)state;

            try
            {
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                NetworkStream stream = client.GetStream();
                HttpRequest request = HttpRequest.Read(stream);
                HttpResponse response = Route(request);
                response.Write(stream);
            }
            catch (Exception ex)
            {
                try
                {
                    HttpResponse.Json(500, "{\"ok\":false,\"error\":\"" + JsonUtil.Escape(ex.Message) + "\"}").Write(client.GetStream());
                }
                catch
                {
                }
            }
            finally
            {
                client.Close();
            }
        }

        private HttpResponse Route(HttpRequest request)
        {
            if (request.Method == "OPTIONS")
            {
                return HttpResponse.Json(200, "{\"ok\":true}");
            }

            if (request.Method == "GET" && request.Path == "/health")
            {
                return HttpResponse.Json(200, "{\"ok\":true,\"mod\":\"Skylines Agent Bridge\",\"levelLoaded\":" + JsonUtil.Bool(bridge.LevelLoaded) + ",\"port\":" + port + "}");
            }

            if (request.Method == "GET" && request.Path == "/state/summary")
            {
                return RunOnGameThread(request, GameState.BuildSummaryJson);
            }

            if (request.Method == "GET" && request.Path == "/state/problems")
            {
                int limit = request.GetQueryInt("limit", 200);
                return RunOnGameThread(request, delegate { return GameState.BuildProblemsJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/demand")
            {
                return RunOnGameThread(request, GameState.BuildDemandJson);
            }

            if (request.Method == "GET" && request.Path == "/state/chirps")
            {
                int limit = request.GetQueryInt("limit", 50);
                return RunOnGameThread(request, delegate { return GameState.BuildChirpsJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/zones")
            {
                return RunOnGameThread(request, GameState.BuildZonesJson);
            }

            if (request.Method == "GET" && request.Path == "/state/economy")
            {
                return RunOnGameThread(request, GameState.BuildEconomyJson);
            }

            if (request.Method == "GET" && request.Path == "/state/facilities")
            {
                int limit = request.GetQueryInt("limit", 500);
                string service = request.GetQueryString("service", "");
                bool includeMapObjects = request.GetQueryString("includeMapObjects", "false") == "true";
                return RunOnGameThread(request, delegate { return GameState.BuildFacilitiesJson(limit, service, includeMapObjects); });
            }

            if (request.Method == "GET" && request.Path == "/state/growables")
            {
                int limit = request.GetQueryInt("limit", 500);
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildGrowablesJson(limit, service); });
            }

            if (request.Method == "GET" && request.Path == "/state/networks")
            {
                int limit = request.GetQueryInt("limit", 500);
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildNetworksJson(limit, service); });
            }

            if (request.Method == "GET" && request.Path == "/state/road-anomalies")
            {
                int limit = request.GetQueryInt("limit", 200);
                float nearMissDistance = request.GetQueryFloat("nearMissDistance", 16f);
                float shortSegmentLength = request.GetQueryFloat("shortSegmentLength", 28f);
                bool includeDeadEnds = request.GetQueryString("includeDeadEnds", "true") == "true";
                return RunOnGameThread(request, delegate { return GameState.BuildRoadAnomaliesJson(limit, nearMissDistance, shortSegmentLength, includeDeadEnds); });
            }

            if (request.Method == "GET" && request.Path == "/state/external-connections")
            {
                int limit = request.GetQueryInt("limit", 50);
                return RunOnGameThread(request, delegate { return GameState.BuildExternalConnectionsJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/building-anomalies")
            {
                int limit = request.GetQueryInt("limit", 200);
                return RunOnGameThread(request, delegate { return GameState.BuildBuildingAnomaliesJson(limit); });
            }

            if (request.Method == "GET" && request.Path == "/state/zone-anomalies")
            {
                int limit = request.GetQueryInt("limit", 200);
                int minMinorityCells = request.GetQueryInt("minMinorityCells", 3);
                int minUnzonedCells = request.GetQueryInt("minUnzonedCells", 6);
                bool includeUnzonedHoles = request.GetQueryString("includeUnzonedHoles", "true") == "true";
                return RunOnGameThread(request, delegate { return GameState.BuildZoneAnomaliesJson(limit, minMinorityCells, minUnzonedCells, includeUnzonedHoles); });
            }

            if (request.Method == "GET" && request.Path == "/state/saves")
            {
                return RunOnGameThread(request, SaveCommands.ListSaves);
            }

            if (request.Method == "GET" && request.Path == "/prefabs/roads")
            {
                return RunOnGameThread(request, GameState.BuildRoadPrefabsJson);
            }

            if (request.Method == "GET" && request.Path == "/prefabs/networks")
            {
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildNetworkPrefabsJson(service); });
            }

            if (request.Method == "GET" && request.Path == "/prefabs/buildings")
            {
                string service = request.GetQueryString("service", "");
                return RunOnGameThread(request, delegate { return GameState.BuildBuildingPrefabsJson(service); });
            }

            if (request.Method == "POST" && request.Path == "/commands/build-road")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return RoadCommands.BuildRoad(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/build-network")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return RoadCommands.BuildRoad(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-zone")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return ZoneCommands.SetZone(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/repair-zones-to-growables")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return ZoneCommands.RepairZonesToGrowables(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/repair-zone-clusters")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return ZoneCommands.RepairZoneClusters(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/place-building")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BuildingCommands.PlaceBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/move-building")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BuildingCommands.MoveBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-building-active")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BuildingCommands.SetBuildingActive(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/disable-blocked-assets")
            {
                return RunOnGameThread(request, AssetCommands.DisableBlockedAssets);
            }

            if (request.Method == "POST" && request.Path == "/commands/set-simulation-speed")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return SimulationCommands.SetSimulationSpeed(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-tax-rate")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return EconomyCommands.SetTaxRate(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/bulldoze")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BulldozeCommands.Bulldoze(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/save")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return SaveCommands.Save(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/batch")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BatchCommands.Execute(body); });
            }

            // === NEW: Traffic ===
            if (request.Method == "GET" && request.Path == "/state/traffic")
            {
                int limit = request.GetQueryInt("limit", 200);
                return RunOnGameThread(request, delegate { return TrafficCommands.BuildTrafficJson(limit); });
            }

            // === NEW: Budget ===
            if (request.Method == "GET" && request.Path == "/state/budget")
            {
                return RunOnGameThread(request, BudgetCommands.BuildBudgetJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/set-budget")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return BudgetCommands.SetBudget(body); });
            }

            // === NEW: Coverage ===
            if (request.Method == "GET" && request.Path == "/state/coverage")
            {
                return RunOnGameThread(request, CoverageCommands.BuildCoverageJson);
            }

            // === NEW: Districts ===
            if (request.Method == "GET" && request.Path == "/state/districts")
            {
                return RunOnGameThread(request, DistrictCommands.BuildDistrictsJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/set-policy")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return DistrictCommands.SetPolicy(body); });
            }

            // === NEW: Environment ===
            if (request.Method == "GET" && request.Path == "/state/environment")
            {
                return RunOnGameThread(request, EnvironmentCommands.BuildEnvironmentJson);
            }

            // === NEW: Milestones ===
            if (request.Method == "GET" && request.Path == "/state/milestones")
            {
                return RunOnGameThread(request, MilestoneCommands.BuildMilestoneJson);
            }

            // === NEW: Camera ===
            if (request.Method == "GET" && request.Path == "/state/camera")
            {
                return RunOnGameThread(request, CameraCommands.GetCameraState);
            }

            if (request.Method == "POST" && request.Path == "/commands/move-camera")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return CameraCommands.MoveCamera(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/focus-building")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return CameraCommands.FocusOnBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/clear-camera-target")
            {
                return RunOnGameThread(request, CameraCommands.ClearCameraTarget);
            }

            // === NEW: Milestones ===
            if (request.Method == "GET" && request.Path == "/state/milestones")
            {
                return RunOnGameThread(request, MilestoneCommands.BuildMilestoneJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/unlock-milestone")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return MilestoneCommands.UnlockMilestone(body); });
            }

            // === NEW: Game ===
            if (request.Method == "GET" && request.Path == "/state/maps")
            {
                return RunOnGameThread(request, GameCommands.ListMaps);
            }

            if (request.Method == "POST" && request.Path == "/commands/new-game")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return GameCommands.NewGame(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/load-game")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return GameCommands.LoadGame(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/quit-to-menu")
            {
                return RunOnGameThread(request, GameCommands.QuitToMenu);
            }

            // === NEW: Immaterial Resources ===
            if (request.Method == "GET" && request.Path == "/state/immaterial-resources")
            {
                return RunOnGameThread(request, ImmaterialResourceCommands.BuildResourceJson);
            }

            // === NEW: Electricity ===
            if (request.Method == "GET" && request.Path == "/state/electricity")
            {
                return RunOnGameThread(request, ElectricityCommands.BuildElectricityJson);
            }

            // === NEW: Water ===
            if (request.Method == "GET" && request.Path == "/state/water")
            {
                return RunOnGameThread(request, WaterCommands.BuildWaterJson);
            }

            // === NEW: Disasters ===
            if (request.Method == "GET" && request.Path == "/state/disasters")
            {
                return RunOnGameThread(request, DisasterCommands.BuildDisastersJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/create-disaster")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return DisasterCommands.CreateDisaster(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/start-random-disaster")
            {
                return RunOnGameThread(request, DisasterCommands.StartRandomDisaster);
            }

            if (request.Method == "POST" && request.Path == "/commands/evacuate")
            {
                return RunOnGameThread(request, DisasterCommands.EvacuateAll);
            }

            // === NEW: Citizens ===
            if (request.Method == "GET" && request.Path == "/state/citizens")
            {
                return RunOnGameThread(request, CitizenCommands.BuildCitizensJson);
            }

            // === NEW: Loans ===
            if (request.Method == "GET" && request.Path == "/state/loans")
            {
                return RunOnGameThread(request, LoanCommands.BuildLoansJson);
            }

            // === NEW: Events ===
            if (request.Method == "GET" && request.Path == "/state/events")
            {
                return RunOnGameThread(request, EventCommands.BuildEventsJson);
            }

            // === NEW: Transfers ===
            if (request.Method == "GET" && request.Path == "/state/transfers")
            {
                return RunOnGameThread(request, TransferCommands.BuildTransfersJson);
            }

            // === NEW: Statistics ===
            if (request.Method == "GET" && request.Path == "/state/statistics")
            {
                return RunOnGameThread(request, StatisticsCommands.BuildStatisticsJson);
            }

            // === NEW: Screenshot ===
            if (request.Method == "POST" && request.Path == "/commands/screenshot")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return ScreenshotCommands.CaptureScreenshot(body); });
            }

            // === NEW: GameArea ===
            if (request.Method == "GET" && request.Path == "/state/areas")
            {
                return RunOnGameThread(request, GameAreaCommands.BuildAreasJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/unlock-area")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return GameAreaCommands.UnlockArea(body); });
            }

            // === NEW: Trees ===
            if (request.Method == "GET" && request.Path == "/state/trees")
            {
                return RunOnGameThread(request, TreeCommands.BuildTreesJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/plant-tree")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TreeCommands.PlantTree(body); });
            }

            // === NEW: Props ===
            if (request.Method == "GET" && request.Path == "/state/props")
            {
                return RunOnGameThread(request, PropCommands.BuildPropsJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/place-prop")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return PropCommands.PlaceProp(body); });
            }

            // === NEW: Weather ===
            if (request.Method == "GET" && request.Path == "/state/weather")
            {
                return RunOnGameThread(request, WeatherCommands.BuildWeatherJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/lightning-strike")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return WeatherCommands.LightningStrike(body); });
            }

            // === NEW: Chirps ===
            if (request.Method == "GET" && request.Path == "/state/chirp-count")
            {
                return RunOnGameThread(request, ChirpCommands.GetChirpCount);
            }

            // === NEW: Natural Resources ===
            if (request.Method == "GET" && request.Path == "/state/natural-resources")
            {
                return RunOnGameThread(request, NaturalResourceCommands.BuildResourcesJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/set-natural-resource")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return NaturalResourceCommands.SetResource(body); });
            }

            // === NEW: Terrain ===
            if (request.Method == "GET" && request.Path == "/state/terrain")
            {
                return RunOnGameThread(request, TerrainCommands.BuildTerrainJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/modify-terrain")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TerrainCommands.ModifyTerrain(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-water-level")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TerrainCommands.SetWaterLevel(body); });
            }

            // === NEW: Transport Lines ===
            if (request.Method == "GET" && request.Path == "/state/transport-lines")
            {
                return RunOnGameThread(request, TransportLineCommands.BuildTransportLinesJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/create-transport-line")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TransportLineCommands.CreateTransportLine(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/delete-transport-line")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TransportLineCommands.DeleteTransportLine(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/add-stop")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TransportLineCommands.AddStop(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/remove-stop")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return TransportLineCommands.RemoveStop(body); });
            }

            // === NEW: Notifications ===
            if (request.Method == "GET" && request.Path == "/state/notifications")
            {
                return RunOnGameThread(request, NotificationCommands.BuildNotificationsJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/dismiss-notifications")
            {
                return RunOnGameThread(request, NotificationCommands.DismissAllNotifications);
            }

            if (request.Method == "POST" && request.Path == "/commands/dismiss-notification")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return NotificationCommands.DismissNotification(body); });
            }

            // === NEW: Pathfinding ===
            if (request.Method == "GET" && request.Path == "/state/pathfinding")
            {
                return RunOnGameThread(request, PathCommands.BuildPathfindingJson);
            }

            // === NEW: Building Levels ===
            if (request.Method == "GET" && request.Path == "/state/levels")
            {
                return RunOnGameThread(request, LevelCommands.BuildLevelsJson);
            }

            if (request.Method == "GET" && request.Path == "/state/building-level")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return LevelCommands.GetBuildingLevelInfo(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/level-up")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return LevelCommands.LevelUpBuilding(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/level-down")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return LevelCommands.LevelDownBuilding(body); });
            }

            // === NEW: Industries / Parks / Campus / Styles ===
            if (request.Method == "GET" && request.Path == "/state/industry-areas")
            {
                return RunOnGameThread(request, IndustriesCommands.BuildIndustryAreasJson);
            }

            if (request.Method == "GET" && request.Path == "/state/park-areas")
            {
                return RunOnGameThread(request, IndustriesCommands.BuildParkAreasJson);
            }

            if (request.Method == "GET" && request.Path == "/state/campus-areas")
            {
                return RunOnGameThread(request, IndustriesCommands.BuildCampusAreasJson);
            }

            if (request.Method == "GET" && request.Path == "/state/district-styles")
            {
                return RunOnGameThread(request, IndustriesCommands.BuildDistrictStylesJson);
            }

            if (request.Method == "POST" && request.Path == "/commands/set-industry-type")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return IndustriesCommands.SetIndustryType(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-park-budget")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return IndustriesCommands.SetParkBudget(body); });
            }

            if (request.Method == "POST" && request.Path == "/commands/set-district-style")
            {
                string body = request.Body;
                return RunOnGameThread(request, delegate { return IndustriesCommands.SetDistrictStyle(body); });
            }

            return HttpResponse.Json(404, "{\"ok\":false,\"error\":\"Not found\"}");
        }

        private HttpResponse RunOnGameThread(HttpRequest request, Func<CommandResult> action)
        {
            if (!bridge.LevelLoaded)
            {
                return HttpResponse.Json(409, "{\"ok\":false,\"error\":\"No city is loaded.\"}");
            }

            CommandResult result = bridge.Queue.RunSync(action, 10000);
            bridge.Queue.RunSync(delegate
            {
                AgentBridgeNotifier.Notify((result.Ok ? "API OK: " : "API FAIL: ") + DescribeRequest(request));
                return CommandResult.FromJson("{\"ok\":true}");
            }, 10000);
            return HttpResponse.Json(result.Ok ? 200 : 500, result.Json);
        }

        private static string DescribeRequest(HttpRequest request)
        {
            string body = request.Body == null ? "" : request.Body;

            if (request.Method == "GET")
            {
                if (request.Path == "/state/summary") return "Read city summary";
                if (request.Path == "/state/problems") return "Read city problems";
                if (request.Path == "/state/demand") return "Read zone demand";
                if (request.Path == "/state/chirps") return "Read citizen chirps";
                if (request.Path == "/state/zones") return "Read zoning summary";
                if (request.Path == "/state/economy") return "Read economy state";
                if (request.Path == "/state/facilities") return "Read facilities";
                if (request.Path == "/state/growables") return "Read growable buildings";
                if (request.Path == "/state/networks") return "Read networks";
                if (request.Path == "/state/road-anomalies") return "Inspect road anomalies";
                if (request.Path == "/state/external-connections") return "Inspect external connections";
                if (request.Path == "/state/building-anomalies") return "Inspect building placement";
                if (request.Path == "/state/zone-anomalies") return "Inspect zoning anomalies";
                if (request.Path == "/state/saves") return "List saves";
                if (request.Path == "/prefabs/roads") return "List road prefabs";
                if (request.Path == "/prefabs/networks") return "List network prefabs";
                if (request.Path == "/prefabs/buildings") return "List building prefabs";
                return "GET " + request.Path;
            }

            if (request.Path == "/commands/build-network" || request.Path == "/commands/build-road")
            {
                string prefab = JsonUtil.GetString(body, "roadPrefab", "network");
                return "Build network " + prefab;
            }

            if (request.Path == "/commands/set-zone")
            {
                return "Set zone " + JsonUtil.GetString(body, "zone", "");
            }

            if (request.Path == "/commands/repair-zones-to-growables")
            {
                return "Repair zones to growables";
            }

            if (request.Path == "/commands/repair-zone-clusters")
            {
                return "Repair zone clusters";
            }

            if (request.Path == "/commands/place-building")
            {
                return "Place building " + JsonUtil.GetString(body, "buildingPrefab", "");
            }

            if (request.Path == "/commands/move-building")
            {
                return "Move building #" + ((int)JsonUtil.GetNumber(body, "id", 0f)).ToString();
            }

            if (request.Path == "/commands/set-building-active")
            {
                return "Set building active #" + ((int)JsonUtil.GetNumber(body, "id", 0f)).ToString();
            }

            if (request.Path == "/commands/disable-blocked-assets")
            {
                return "Disable blocked assets";
            }

            if (request.Path == "/commands/bulldoze")
            {
                string entityType = JsonUtil.GetString(body, "entityType", "entity");
                return "Bulldoze " + entityType + " #" + ((int)JsonUtil.GetNumber(body, "id", 0f)).ToString();
            }

            if (request.Path == "/commands/save")
            {
                return "Save city " + JsonUtil.GetString(body, "name", "AgentAutoSave");
            }

            if (request.Path == "/commands/set-simulation-speed")
            {
                if (JsonUtil.GetBool(body, "paused", false))
                {
                    return "Pause simulation";
                }
                return "Set simulation speed " + ((int)JsonUtil.GetNumber(body, "speed", 0f)).ToString();
            }

            if (request.Path == "/commands/set-tax-rate")
            {
                return "Set tax rate " + ((int)JsonUtil.GetNumber(body, "rate", 0f)).ToString();
            }

            if (request.Path == "/commands/batch")
            {
                return "Run batch commands";
            }

            if (request.Method == "GET")
            {
                if (request.Path == "/state/traffic") return "Read traffic data";
                if (request.Path == "/state/budget") return "Read budget";
                if (request.Path == "/state/coverage") return "Read coverage";
                if (request.Path == "/state/districts") return "Read districts";
                if (request.Path == "/state/environment") return "Read environment";
            }

            if (request.Path == "/commands/set-budget") return "Set budget";
            if (request.Path == "/commands/set-policy") return "Set policy";
            if (request.Path == "/commands/move-camera") return "Move camera";
            if (request.Path == "/commands/focus-building") return "Focus building";
            if (request.Path == "/commands/clear-camera-target") return "Clear camera target";
            if (request.Path == "/commands/unlock-milestone") return "Unlock milestone";
            if (request.Path == "/commands/new-game") return "New game";
            if (request.Path == "/commands/load-game") return "Load game";
            if (request.Path == "/commands/quit-to-menu") return "Quit to menu";
            if (request.Path == "/commands/create-disaster") return "Create disaster";
            if (request.Path == "/commands/start-random-disaster") return "Start random disaster";
            if (request.Path == "/commands/evacuate") return "Evacuate city";
            if (request.Path == "/commands/screenshot") return "Take screenshot";
            if (request.Path == "/commands/unlock-area") return "Unlock area";
            if (request.Path == "/commands/plant-tree") return "Plant tree";
            if (request.Path == "/commands/place-prop") return "Place prop";
            if (request.Path == "/commands/lightning-strike") return "Lightning strike";

            if (request.Method == "GET")
            {
                if (request.Path == "/state/immaterial-resources") return "Read immaterial resources";
                if (request.Path == "/state/electricity") return "Read electricity";
                if (request.Path == "/state/water") return "Read water";
                if (request.Path == "/state/disasters") return "Read disasters";
                if (request.Path == "/state/citizens") return "Read citizens";
                if (request.Path == "/state/loans") return "Read loans";
                if (request.Path == "/state/events") return "Read events";
                if (request.Path == "/state/transfers") return "Read transfers";
                if (request.Path == "/state/statistics") return "Read statistics";
                if (request.Path == "/state/areas") return "Read areas";
                if (request.Path == "/state/trees") return "Read trees";
                if (request.Path == "/state/props") return "Read props";
                if (request.Path == "/state/weather") return "Read weather";
                if (request.Path == "/state/natural-resources") return "Read natural resources";
                if (request.Path == "/state/terrain") return "Read terrain data";
                if (request.Path == "/state/transport-lines") return "Read transport lines";
                if (request.Path == "/state/notifications") return "Read notifications";
                if (request.Path == "/state/pathfinding") return "Read pathfinding data";
                if (request.Path == "/state/levels") return "Read building levels";
                if (request.Path == "/state/building-level") return "Read building level info";
                if (request.Path == "/state/industry-areas") return "Read industry areas";
                if (request.Path == "/state/park-areas") return "Read park areas";
                if (request.Path == "/state/campus-areas") return "Read campus areas";
                if (request.Path == "/state/district-styles") return "Read district styles";
            }

            if (request.Path == "/commands/set-natural-resource") return "Set natural resource";
            if (request.Path == "/commands/modify-terrain") return "Modify terrain";
            if (request.Path == "/commands/set-water-level") return "Set water level";
            if (request.Path == "/commands/create-transport-line") return "Create transport line";
            if (request.Path == "/commands/delete-transport-line") return "Delete transport line";
            if (request.Path == "/commands/add-stop") return "Add transport stop";
            if (request.Path == "/commands/remove-stop") return "Remove transport stop";
            if (request.Path == "/commands/dismiss-notifications") return "Dismiss all notifications";
            if (request.Path == "/commands/dismiss-notification") return "Dismiss notification";
            if (request.Path == "/commands/level-up") return "Level up building";
            if (request.Path == "/commands/level-down") return "Level down building";
            if (request.Path == "/commands/set-industry-type") return "Set industry type";
            if (request.Path == "/commands/set-park-budget") return "Set park budget";
            if (request.Path == "/commands/set-district-style") return "Set district style";

            return request.Method + " " + request.Path;
        }

        private sealed class HttpRequest
        {
            public string Method;
            public string Path;
            public string Query;
            public string Body;

            public int GetQueryInt(string name, int defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        int value;
                        if (int.TryParse(parts[1], out value))
                        {
                            return value;
                        }
                    }
                }

                return defaultValue;
            }

            public float GetQueryFloat(string name, float defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        float value;
                        if (float.TryParse(parts[1], out value))
                        {
                            return value;
                        }
                    }
                }

                return defaultValue;
            }

            public string GetQueryString(string name, string defaultValue)
            {
                if (Query == null || Query.Length == 0)
                {
                    return defaultValue;
                }

                string[] pairs = Query.Split('&');
                for (int i = 0; i < pairs.Length; i++)
                {
                    string[] parts = pairs[i].Split('=');
                    if (parts.Length == 2 && parts[0] == name)
                    {
                        return Uri.UnescapeDataString(parts[1].Replace("+", " "));
                    }
                }

                return defaultValue;
            }

            public static HttpRequest Read(NetworkStream stream)
            {
                MemoryStream headerBytes = new MemoryStream();
                int matched = 0;

                while (true)
                {
                    int b = stream.ReadByte();
                    if (b < 0)
                    {
                        break;
                    }

                    headerBytes.WriteByte((byte)b);

                    if ((matched == 0 && b == '\r') ||
                        (matched == 1 && b == '\n') ||
                        (matched == 2 && b == '\r') ||
                        (matched == 3 && b == '\n'))
                    {
                        matched++;
                        if (matched == 4)
                        {
                            break;
                        }
                    }
                    else
                    {
                        matched = b == '\r' ? 1 : 0;
                    }

                    if (headerBytes.Length > 65536)
                    {
                        throw new InvalidOperationException("Request headers are too large.");
                    }
                }

                string headers = Encoding.UTF8.GetString(headerBytes.ToArray());
                string[] lines = headers.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                string[] first = lines[0].Split(' ');
                int contentLength = 0;

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int colon = line.IndexOf(':');
                    if (colon > 0 && string.Compare(line.Substring(0, colon), "Content-Length", true) == 0)
                    {
                        int.TryParse(line.Substring(colon + 1).Trim(), out contentLength);
                    }
                }

                byte[] bodyBytes = new byte[contentLength];
                int offset = 0;
                while (offset < contentLength)
                {
                    int read = stream.Read(bodyBytes, offset, contentLength - offset);
                    if (read <= 0)
                    {
                        break;
                    }
                    offset += read;
                }

                HttpRequest request = new HttpRequest();
                request.Method = first.Length > 0 ? first[0].ToUpperInvariant() : "";
                string target = first.Length > 1 ? first[1] : "/";
                int queryIndex = target.IndexOf('?');
                if (queryIndex >= 0)
                {
                    request.Path = target.Substring(0, queryIndex);
                    request.Query = target.Substring(queryIndex + 1);
                }
                else
                {
                    request.Path = target;
                    request.Query = "";
                }
                request.Body = Encoding.UTF8.GetString(bodyBytes, 0, offset);
                return request;
            }
        }

        private sealed class HttpResponse
        {
            private readonly int status;
            private readonly string body;

            private HttpResponse(int status, string body)
            {
                this.status = status;
                this.body = body;
            }

            public static HttpResponse Json(int status, string body)
            {
                return new HttpResponse(status, body);
            }

            public void Write(NetworkStream stream)
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                string header = "HTTP/1.1 " + status + " " + Reason(status) + "\r\n" +
                    "Content-Type: application/json; charset=utf-8\r\n" +
                    "Content-Length: " + bodyBytes.Length + "\r\n" +
                    "Access-Control-Allow-Origin: *\r\n" +
                    "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                    "Access-Control-Allow-Headers: Content-Type\r\n" +
                    "Connection: close\r\n\r\n";

                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                stream.Write(headerBytes, 0, headerBytes.Length);
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            private static string Reason(int status)
            {
                if (status == 200) return "OK";
                if (status == 404) return "Not Found";
                if (status == 409) return "Conflict";
                return "Internal Server Error";
            }
        }
    }
}
