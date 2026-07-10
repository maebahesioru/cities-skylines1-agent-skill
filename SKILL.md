---
name: cities-skylines1-agent-skill
description: "Operate Cities: Skylines 1 through the Skylines Agent Bridge mod and localhost API. Use when Codex needs to build, inspect, repair, resume, save, or continue a CS1 city using focused API calls rather than screenshot recognition, including road connectivity, service infrastructure, zoning, facilities, problem icons, and save verification. 66 command files, ~150 endpoints, full CS1 manager coverage."
---

# Cities: Skylines 1 Agent Skill

Use this skill to control a running Cities: Skylines 1 city through the local Skylines Agent Bridge API. Full API reference: [docs/api.md](docs/api.md)

## Core Rules

- Prefer API state over image recognition.
- Resume and repair existing saves by default. Start fresh only when explicitly requested.
- Use small, separated commands: inspect, bulldoze, build, place, move, zone, simulate, save.
- Save after meaningful city changes and verify the `.crp` file exists.
- Commit repository changes after each coherent code/docs task when working inside this repository.

## Local Setup

The bridge listens on:

```text
http://127.0.0.1:32123
```

Build and install the mod:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build.ps1
```

Resume the latest save:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-resume.ps1
```

Start a new map only when the user asks:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-new-map.ps1
```

## Inspection Loop (Basic)

Always run these before acting:

```powershell
# Core health & overview
Invoke-RestMethod http://127.0.0.1:32123/health
Invoke-RestMethod http://127.0.0.1:32123/state/summary

# Economy & problems
Invoke-RestMethod "http://127.0.0.1:32123/state/problems?limit=200"
Invoke-RestMethod http://127.0.0.1:32123/state/economy
Invoke-RestMethod http://127.0.0.1:32123/state/budget

# Buildings & zoning
Invoke-RestMethod "http://127.0.0.1:32123/state/facilities?limit=500"
Invoke-RestMethod "http://127.0.0.1:32123/state/growables?limit=500"
Invoke-RestMethod http://127.0.0.1:32123/state/zones

# Networks & roads
Invoke-RestMethod "http://127.0.0.1:32123/state/networks?limit=1000&service=Road"
Invoke-RestMethod "http://127.0.0.1:32123/state/road-anomalies?nearMissDistance=18&shortSegmentLength=32&includeDeadEnds=false"
Invoke-RestMethod "http://127.0.0.1:32123/state/building-anomalies?limit=200"

# Citizen feedback
Invoke-RestMethod "http://127.0.0.1:32123/state/chirps?limit=50"
```

## Deep Inspection (use when diagnosing specific issues)

```powershell
# Detailed building state
Invoke-RestMethod "http://127.0.0.1:32123/state/building?id=12345"

# Traffic & transport
Invoke-RestMethod "http://127.0.0.1:32123/state/vehicles?limit=200&includePosition=true"
Invoke-RestMethod "http://127.0.0.1:32123/state/transport-lines"
Invoke-RestMethod "http://127.0.0.1:32123/state/traffic-lights?limit=100"
Invoke-RestMethod "http://127.0.0.1:32123/state/junctions?limit=100"

# Districts & policies
Invoke-RestMethod "http://127.0.0.1:32123/state/district?id=0"
Invoke-RestMethod "http://127.0.0.1:32123/state/policies?districtId=0"
Invoke-RestMethod "http://127.0.0.1:32123/state/coverage"

# Environment & resources
Invoke-RestMethod http://127.0.0.1:32123/state/electricity
Invoke-RestMethod http://127.0.0.1:32123/state/water
Invoke-RestMethod http://127.0.0.1:32123/state/natural-resources
Invoke-RestMethod http://127.0.0.1:32123/state/terrain

# Disasters & weather
Invoke-RestMethod "http://127.0.0.1:32123/state/disasters/active"
Invoke-RestMethod http://127.0.0.1:32123/state/weather

# DLC areas
Invoke-RestMethod http://127.0.0.1:32123/state/industry-areas
Invoke-RestMethod http://127.0.0.1:32123/state/park-areas
Invoke-RestMethod http://127.0.0.1:32123/state/campus-areas
Invoke-RestMethod http://127.0.0.1:32123/state/airports
Invoke-RestMethod http://127.0.0.1:32123/state/supply-chain

# Statistics
Invoke-RestMethod http://127.0.0.1:32123/state/statistics
Invoke-RestMethod "http://127.0.0.1:32123/state/statistics/detail?category=population&limit=50"
```

## Command Patterns

### Network Construction

**Bulldoze a bad segment:**
```powershell
$body = @{ entityType = "netSegment"; id = 19023; keepNodes = $false } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/bulldoze -Body $body -ContentType "application/json"
```

**Build a road/pipe:**
```powershell
$body = @{
  roadPrefab = "Basic Road"
  start = @{ x = 400; z = 300 }
  end = @{ x = 423.614; z = 554.945 }
  name = "Agent Highway Link"
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/build-network -Body $body -ContentType "application/json"
```

### Buildings

**Place or move a building:**
```powershell
$body = @{ buildingPrefab = "Water Tower"; position = @{ x = 120; z = -220 }; angleDegrees = 0 } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/place-building -Body $body -ContentType "application/json"
```

**Force level up a building:**
```powershell
$body = @{ buildingId = 12345; targetLevel = 3 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/level-up -Body $body -ContentType "application/json"
```

### Zoning

**Paint zones:**
```powershell
$body = @{ zone = "ResidentialLow"; preserveOccupied = $true; center = @{ x = 240; z = -40 }; radius = 70 } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-zone -Body $body -ContentType "application/json"
```

### Economy & Simulation

**Run simulation:**
```powershell
$body = @{ paused = $false; speed = 3 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-simulation-speed -Body $body -ContentType "application/json"
```

**Lower taxes when `TaxesTooHigh`:**
```powershell
$body = @{ service = "Commercial"; rate = 9 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-tax-rate -Body $body -ContentType "application/json"
```

**Set budget:**
```powershell
$body = @{ service = "PoliceDepartment"; subService = "PoliceDepartment"; amount = 120; night = $false } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-budget -Body $body -ContentType "application/json"
```

### Policies

**Set city-wide policy:**
```powershell
$body = @{ policyType = "Services"; policy = "SmokeDetector"; active = $true; scope = "city" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-policy -Body $body -ContentType "application/json"
```

**Set district policy:**
```powershell
$body = @{ policyType = "CityPlanning"; policy = "HighRiseBan"; active = $true; scope = "district"; districtId = 1 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-policy-full -Body $body -ContentType "application/json"
```

### Traffic Management

**Toggle traffic light:**
```powershell
$body = @{ segmentId = 500; mode = "toggle" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-traffic-light -Body $body -ContentType "application/json"
```

**Rename road:**
```powershell
$body = @{ segmentId = 500; name = "Main Street" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/rename-road -Body $body -ContentType "application/json"
```

### Transport Lines

**Create bus line:**
```powershell
$body = @{ prefab = "Bus" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/create-transport-line -Body $body -ContentType "application/json"
```

**Add/remove stops:**
```powershell
$body = @{ lineId = 3; buildingId = 500 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/add-stop -Body $body -ContentType "application/json"
```

### Environment

**Set natural resources:**
```powershell
$body = @{ type = "oil"; x = 400; z = 300; value = 200; radius = 5 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/set-natural-resource -Body $body -ContentType "application/json"
```

**Plant trees / place props:**
```powershell
$body = @{ treePrefab = "Oak"; position = @{ x = 200; z = 100 } } | ConvertTo-Json -Depth 3
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/plant-tree -Body $body -ContentType "application/json"
```

### Disasters

**Create disaster / evacuate building:**
```powershell
$body = @{ disasterType = "Tsunami"; position = @{ x = 0; z = 0 }; intensity = 10 } | ConvertTo-Json -Depth 3
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/create-disaster -Body $body -ContentType "application/json"

$body = @{ buildingId = 12345 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/evacuate-building -Body $body -ContentType "application/json"
```

### Game Management

**Save, screenshot, console:**
```powershell
$body = @{ name = "AgentAutoSave-clean" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/save -Body $body -ContentType "application/json"

$body = @{ filename = "screenshot.png"; width = 1920; height = 1080 } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/screenshot -Body $body -ContentType "application/json"

$body = @{ command = "pause" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/console -Body $body -ContentType "application/json"
```

**Save and verify:**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\save-city.ps1 -Name AgentAutoSave-clean
Invoke-RestMethod http://127.0.0.1:32123/state/saves
```

## Batch Operations

Run up to 32 commands in one request. All command types are batchable:

```powershell
$body = @{
  dryRun = $false
  stopOnError = $true
  commands = @(
    @{ type = "build-road"; roadPrefab = "Basic Road"; start = @{ x = 100; z = 0 }; end = @{ x = 200; z = 0 } }
    @{ type = "set-zone"; zone = "ResidentialLow"; center = @{ x = 150; z = 0 }; radius = 48 }
    @{ type = "place-building"; buildingPrefab = "Water Tower"; position = @{ x = 160; z = -50 }; angleDegrees = 0 }
  )
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:32123/commands/batch -Body $body -ContentType "application/json"
```

## Known Gotchas

- CS1 network crossings are not intersections unless a real node is created. If a pipe or road visually crosses another segment but does not connect, bulldoze and rebuild split segments with a shared endpoint.
- Heating service buildings may create an actual connection helper offset from the building center. Query `/state/facilities?service=Water&includeMapObjects=true` when diagnosing heating pipe issues.
- Roads that look connected to highways can still have separate nodes. Use `/state/road-anomalies` and `/state/external-connections` and rebuild with endpoints close enough to reuse the existing road nodes.
- Do not treat all dead ends as errors. Use bounded checks or `includeDeadEnds=false` unless the user asks to remove cul-de-sacs/stubs.
- Use `/state/zone-anomalies` when zone colors look mottled or circular paint left residential/commercial/industrial/office cells mixed in the same block.
- `/commands/set-zone` defaults to `preserveOccupied=true`; check `/state/growables` first and keep that flag enabled unless the user explicitly wants to repaint developed blocks.
- For detailed building diagnostics, use `/state/building?id=X` to get full state including citizen counts, service buffers, problem flags, and AI type.
- For traffic analysis, `/state/vehicles?includePosition=true` gives live vehicle positions. `/state/junctions` shows all intersections with traffic light state.
- District policies use enum names from `DistrictPolicies.Services`, `DistrictPolicies.Taxation`, `DistrictPolicies.CityPlanning`, `DistrictPolicies.Specialization`. Check `/state/policies` for current values.
- Natural resource manipulation uses grid coordinates derived from the resource array size. Use `/state/natural-resources` to see current distribution before setting values.
- Terrain and water level modification via public API is limited in CS1. Use in-game terrain tools for precise control.
