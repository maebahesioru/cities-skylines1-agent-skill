# API Reference — Skylines Agent Bridge

[Japanese version](ja/api.md)

**Base URL:** `http://127.0.0.1:32123`  
**Current version:** 66 source files · 223KB · ~150 endpoints · CS1 v1.17+

While a city is loaded, every API request that touches game state appears in the CS1 UI as a short overlay notification (e.g. `API OK: Read city problems`). `/health` is excluded — it works without a level loaded.

---

## Agent Workflow

1. **Read** state from the API
2. **Decide** which small change to make
3. **Call** one command API
4. **Let** the simulation settle
5. **Re-read** state
6. **Save** and verify

---

## Endpoint Index

### Health & Meta
| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Bridge status (no city required) |
| GET | `/state/mods` | List installed mods |

### City State (Read)
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/summary` | — | Game time, network counts, citizens, demand |
| GET | `/state/demand` | — | Residential/commercial/workplace demand (0–100) |
| GET | `/state/chirps` | `limit` (50) | Recent citizen chirps/messages |
| GET | `/state/chirp-count` | — | Total chirp count |
| GET | `/state/zones` | — | Zoning cell counts per type |
| GET | `/state/zones/map` | `sampleStep`, `offsetX`, `offsetZ`, `width`, `height` | Zone cell grid map |
| GET | `/state/problems` | `limit` (200) | City problems (abandoned, burned, taxes, etc.) |
| GET | `/state/economy` | — | Tax rates by service/sub-service/level |
| GET | `/state/budget` | — | Cash balance + population |
| GET | `/state/budget/detail` | — | Per-service budget with day/night split |
| GET | `/state/loans` | — | Active loans |
| GET | `/state/statistics` | — | Population/employment/education/health stats |
| GET | `/state/statistics/detail` | `category`, `limit` | Time-series stats by category |

### Buildings
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/facilities` | `limit` (500), `service`, `includeMapObjects` | Service buildings grouped by service |
| GET | `/state/growables` | `limit` (500), `service` | Zoned growable buildings |
| GET | `/state/building` | `id` | **Full detail** for one building (all state, problems, citizens, AI type) |
| GET | `/state/building/upgrades` | `id` | Building level, upgrade progress, upgrade path |
| GET | `/state/building-anomalies` | `limit` (200) | Buildings colliding with roads |
| GET | `/state/levels` | — | Level distribution across all growables |
| GET | `/state/building-level` | Body: `{buildingId}` | Detailed level info for one building |
| GET | `/prefabs/buildings` | `service` | Available building prefabs |

### Networks (Roads, Water, Power)
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/networks` | `limit` (500), `service` | Network segments (roads/pipes/power) |
| GET | `/state/network-segment` | `id` | **Full detail** for one segment (nodes, bounds, lanes, traffic lights) |
| GET | `/state/network-node` | `id` | **Full detail** for one node (position, connected segments) |
| GET | `/state/road-anomalies` | `limit`, `nearMissDistance`, `shortSegmentLength`, `includeDeadEnds` | Road geometry issues |
| GET | `/state/traffic-lights` | `limit` (100) | Intersections with traffic light state |
| GET | `/state/junctions` | `limit` (100) | All junctions (≥3 connected segments) |
| GET | `/state/road-names` | `limit` (100) | Named road segments |
| GET | `/state/speed-limits` | `limit` (100) | Per-segment speed limits |
| GET | `/state/external-connections` | `limit` (50) | Outside road connections |
| GET | `/prefabs/roads` | — | Road-like NetInfo prefabs |
| GET | `/prefabs/networks` | `service` | All network prefabs |

### Traffic & Transport
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/traffic` | `limit` (200) | Vehicle counts by type |
| GET | `/state/vehicles` | `limit` (200), `service`, `includePosition` | **All vehicles** with position/type/passengers/cargo |
| GET | `/state/transport-lines` | — | All transport lines (bus/metro/train etc.) |
| GET | `/state/transfers` | — | Transfer match offers |

### Citizens
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/citizens` | — | Citizen count + age distribution |
| GET | `/state/citizen` | `id` | **Full detail** for one citizen (job, home, education, health, wealth) |
| GET | `/state/citizens/search` | `limit` (100), `employed`, `age`, `education` | Filter/search citizens |

### Districts
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/districts` | — | All districts with population/policies |
| GET | `/state/district` | `id` | **Full detail** for one district (all policies, population, land value, consumption) |
| GET | `/state/areas` | — | Unlocked map tiles |
| GET | `/state/policies` | `districtId` (0=city) | Full policy enumeration (Services/Taxation/CityPlanning/Specialization) |
| GET | `/state/district-styles` | — | District building styles |
| GET | `/state/coverage` | — | Service coverage overview |
| GET | `/state/coverage/detail` | — | Detailed coverage data |

### Industry & DLC
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/industry-areas` | — | Industry specialization areas (Oil/Ore/Forest/Farming) |
| GET | `/state/supply-chain` | — | Industries supply chain (extractors → warehouses → processors → factories) |
| GET | `/state/park-areas` | — | Parklife park areas |
| GET | `/state/parks/detail` | — | Detailed park stats (visitors, entertainment, land value) |
| GET | `/state/campus-areas` | — | Campus areas |
| GET | `/state/campuses/detail` | — | Detailed campus stats (students, workers, happiness) |
| GET | `/state/airports` | — | Airport areas (passengers, cargo, airplane fullness) |

### Environment & Resources
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/environment` | — | Pollution, land value, terrain, natural resources summary |
| GET | `/state/natural-resources` | — | Oil/Ore/Forest/Fertility distribution + extractor counts |
| GET | `/state/terrain` | — | Terrain height range + water level |
| GET | `/state/immaterial-resources` | — | Land value, attractiveness, entertainment |
| GET | `/state/electricity` | — | Power production/consumption |
| GET | `/state/water` | — | Water/sewage production/consumption |
| GET | `/state/weather` | — | Current weather state |
| GET | `/state/disasters` | — | Disaster overview |
| GET | `/state/disasters/active` | — | **Active disasters** with position, intensity |
| GET | `/state/events` | — | City events |
| GET | `/state/pathfinding` | — | Path unit stats (active/free counts) |
| GET | `/state/notifications` | — | Problem building count + simulation state |
| GET | `/state/trees` | — | Tree count summary |
| GET | `/state/props` | — | Prop count summary |

### Game Management
| Method | Path | Query Params | Description |
|--------|------|-------------|-------------|
| GET | `/state/camera` | — | Current camera position/target |
| GET | `/state/saves` | — | Local .crp saves |
| GET | `/state/maps` | — | Available maps for new games |
| GET | `/state/milestones` | — | Milestone unlock status |
| GET | `/state/radio` | — | Audio/radio state |
| GET | `/state/info-view` | — | Current info overlay mode |
| GET | `/state/active-tool` | — | Currently active tool |
| GET | `/state/effects` | — | Active visual effects |
| GET | `/state/guides` | — | Tutorial guide state |

---

## POST Endpoints (Commands)

### Network Construction
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/build-network` | `{roadPrefab, start, end, name, dryRun}` | Create road/pipe/power segment |
| POST | `/commands/build-road` | (same) | Alias for build-network |
| POST | `/commands/bulldoze` | `{entityType, id, keepNodes}` | Delete building/segment/node |

### Building Operations
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/place-building` | `{buildingPrefab, position, angleDegrees, dryRun}` | Place new building |
| POST | `/commands/move-building` | `{id, position, angleDegrees}` | Relocate building |
| POST | `/commands/set-building-active` | `{id, active}` | Turn building on/off |
| POST | `/commands/disable-blocked-assets` | — | Disable known broken assets |
| POST | `/commands/building-level-info` | `{buildingId}` | Get detailed level info |
| POST | `/commands/level-up` | `{buildingId, targetLevel}` | Force level up (Residential/Commercial/Office) |
| POST | `/commands/level-down` | `{buildingId}` | Decrease building level |
| POST | `/commands/upgrade-building` | `{buildingId, targetLevel}` | Force building level change |

### Zoning
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-zone` | `{zone, center, radius, preserveOccupied, dryRun}` | Paint zones |
| POST | `/commands/repair-zones-to-growables` | `{dryRun}` | Align zones with existing buildings |
| POST | `/commands/repair-zone-clusters` | `{gridSize, includePatchy, fillUnzoned, dryRun}` | Repair mottled zoning clusters |

### Economy & Budget
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-tax-rate` | `{service, rate, subService, level, dryRun}` | Set tax rate (0–29) |
| POST | `/commands/set-budget` | `{service, subService, amount, night}` | Set service budget % (50–150) |

### Simulation
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-simulation-speed` | `{paused, speed}` | Pause/unpause, set speed 1–3 |
| POST | `/commands/save` | `{name}` | Save game |
| POST | `/commands/screenshot` | `{filename, width, height}` | Take screenshot |

### Game Management
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/new-game` | `{mapName, theme}` | Start new city |
| POST | `/commands/load-game` | `{saveName}` | Load save |
| POST | `/commands/quit-to-menu` | — | Return to main menu |
| POST | `/commands/console` | `{command}` | Execute console commands (save/pause/speed) |

### Camera
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/move-camera` | `{position, target, instant}` | Move camera |
| POST | `/commands/focus-building` | `{buildingId, zoomDistance}` | Focus on building |
| POST | `/commands/clear-camera-target` | — | Release camera target |

### Policies & Districts
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-policy` | `{policyType, policy, active, scope, districtId}` | Set city/district policy |
| POST | `/commands/set-policy-full` | `{policyType, policy, active, districtId, scope}` | Full policy control (all sub-types) |
| POST | `/commands/unlock-area` | `{x, z}` | Purchase map tile |
| POST | `/commands/set-industry-type` | `{districtId, type}` | Set industry specialization |
| POST | `/commands/set-park-budget` | `{parkId, budget}` | Set park ticket price |
| POST | `/commands/set-district-style` | `{districtId, style, variation}` | Set district building style |

### Environment & Resources
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/create-disaster` | `{disasterType, position, intensity}` | Trigger disaster |
| POST | `/commands/start-random-disaster` | — | Random disaster |
| POST | `/commands/evacuate` | — | Evacuate entire city |
| POST | `/commands/evacuate-building` | `{buildingId}` | Evacuate specific building |
| POST | `/commands/lightning-strike` | `{position}` | Trigger lightning strike |
| POST | `/commands/modify-terrain` | `{x, z, height, radius, mode}` | Modify terrain height |
| POST | `/commands/set-water-level` | `{x, z, level}` | Set water level |
| POST | `/commands/set-natural-resource` | `{type, x, z, value, radius}` | Set oil/ore/forest/fertility |
| POST | `/commands/plant-tree` | `{treePrefab, position}` | Plant tree |
| POST | `/commands/place-prop` | `{propPrefab, position, angleDegrees}` | Place prop |

### Traffic & Transport
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-traffic-light` | `{segmentId, mode}` | Toggle traffic light |
| POST | `/commands/rename-road` | `{segmentId, name}` | Rename road |
| POST | `/commands/create-transport-line` | `{prefab}` | Create bus/metro/train line |
| POST | `/commands/delete-transport-line` | `{lineId}` | Remove transport line |
| POST | `/commands/add-stop` | `{lineId, buildingId}` | Add stop to line |
| POST | `/commands/remove-stop` | `{lineId, stopIndex}` | Remove stop from line |

### Misc
| Method | Path | Body | Description |
|--------|------|------|-------------|
| POST | `/commands/set-radio-channel` | `{channelIndex}` | Change radio |
| POST | `/commands/set-volume` | `{type, volume}` | Set audio volume |
| POST | `/commands/set-info-view` | `{mode, subMode}` | Switch info overlay |
| POST | `/commands/dismiss-notifications` | — | Dismiss all notifications |
| POST | `/commands/dismiss-notification` | `{id}` | Dismiss specific notification |
| POST | `/commands/unlock-milestone` | `{milestoneName}` | Force unlock milestone |
| POST | `/commands/batch` | `{commands[], stopOnError, dryRun}` | Run up to 32 commands at once |

---

## Batch Command Types

All POST commands can be batched. Use the `type` field matching the endpoint name:

```
build-road, set-zone, repair-zones-to-growables, repair-zone-clusters,
place-building, move-building, bulldoze, set-simulation-speed, set-tax-rate,
save, set-budget, set-policy, set-policy-full, create-disaster, evacuate,
start-random-disaster, screenshot, move-camera, focus-building, new-game,
load-game, unlock-area, plant-tree, place-prop, lightning-strike,
set-natural-resource, modify-terrain, set-water-level, create-transport-line,
delete-transport-line, add-stop, remove-stop, dismiss-notification,
level-up, level-down, set-traffic-light, rename-road, evacuate-building,
upgrade-building, set-radio-channel, set-volume, set-info-view,
set-industry-type, set-park-budget, set-district-style, console
```

---

## Response Format

All responses are JSON with `Content-Type: application/json` and CORS headers.

**Success:**
```json
{"ok": true, ...data fields...}
```

**Error:**
```json
{"ok": false, "error": "human-readable message"}
```

**Not loaded:**
```json
{"ok": false, "error": "No city is loaded."}
```
HTTP 409
