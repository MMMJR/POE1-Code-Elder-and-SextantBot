# POE1-Code-Elder-and-SextantBot

Path of Exile 1 bot plugin for the **DreamPoeBot** framework. Supports two active bot modes:
- **SextantBot** (ID=1) – automates sextant crafting on the Atlas
- **FarmingBot / ElderBot** (ID=2) – automates map farming (open map, explore, kill boss, loot, stash)

## Build
- Target: .NET Framework 4.8, output type `Library` (DLL plugin)
- IDE: Visual Studio (old-style `.csproj` with explicit `<Compile>` entries)
- Requires `DreamPoeBot.exe` in `MmmjrBot/bin/Debug/` as a reference

```
msbuild MmmjrBot/MmmjrBot.csproj /p:Configuration=Debug
```

## DreamPoeBot Framework Version
Currently targeting **v0.3.28.05 BETA**. Key API changes in this version:
- `MasterDeviceUi` is **obsolete** — all map device functionality moved to `LokiPoe.InGameState.AtlasUi.MapDevice`
- `MapDeviceUi` is **obsolete** — same migration to `AtlasUi.MapDevice`
- Interacting with the physical Map Device now opens `LokiPoe.InGameState.AtlasUi` (the Atlas panel), not a standalone UI
- `ActivateResult` lives at `LokiPoe.InGameState.ActivateResult` (unchanged)

### Key AtlasUi API
```csharp
LokiPoe.InGameState.AtlasUi.IsOpened               // Atlas panel visible
LokiPoe.InGameState.AtlasUi.MapDevice.IsOpened      // Map Device panel visible within Atlas
LokiPoe.InGameState.AtlasUi.MapDevice.InventoryControl  // InventoryControlWrapper for device slots
LokiPoe.InGameState.AtlasUi.MapDevice.Activate()    // returns LokiPoe.InGameState.ActivateResult
LokiPoe.InGameState.AtlasUi.MapDevice.IsFiveSlotDevice
LokiPoe.InGameState.AtlasUi.MapDevice.IsSixSlotDevice
```

In `OpenMapTask.cs` a type alias is used to keep call sites clean:
```csharp
using AtlasMapDevice = DreamPoeBot.Loki.Game.LokiPoe.InGameState.AtlasUi.MapDevice;
```

## Project Structure

```
MmmjrBot/
├── MmmjrBot.cs              # IBot entry point; OnChangeEnabledBots dispatches tasks
├── MmmjrBotSettings.cs      # JsonSettings singleton; INotifyPropertyChanged for WPF bindings
├── MmmjrGUI.xaml            # WPF UserControl (bot settings UI, DataContext = MmmjrBotSettings.Instance)
├── MmmjrGUI.xaml.cs         # Code-behind for XAML event handlers
├── SextantTask.cs           # SextantBot logic (ITask) + SextantBotState enum
├── FarmingTask.cs           # FarmingBot/ElderBot logic (ITask) + SearchTargetResult
├── SextantModSelectorClass.cs  # Sextant mod selection helper
│
├── AutoLogin/
│   └── AutoLogin.cs
│
├── Class/
│   ├── ChatParser.cs
│   ├── DefensiveSkillsClass.cs
│   ├── Flasks.cs
│   ├── FlasksClass.cs
│   ├── MapperRoutineSkill.cs
│   └── OverlayWindow.cs
│
├── Lib/
│   ├── AreaInfo.cs
│   ├── BotStructure.cs
│   ├── ClassExtensions.cs
│   ├── CurrencyNames.cs
│   ├── Dnd.cs
│   ├── Enums.cs
│   ├── ErrorManager.cs / ErrorReporter.cs
│   ├── EXtensions.cs
│   ├── GlobalLog.cs
│   ├── InputDelayOverride.cs
│   ├── Interval.cs
│   ├── Inventories.cs          # OpenInventory2(), FastMoveFromInventory(), InventoryItems, etc.
│   ├── ITaskManagerHolder.cs
│   ├── MapNames.cs
│   ├── MessageBoxes.cs
│   ├── Move.cs
│   ├── PlayerAction.cs
│   ├── RarityColors.cs
│   ├── Settings.cs
│   ├── Tgt.cs
│   ├── TownNpcs.cs
│   ├── Wait.cs
│   ├── World.cs
│   │
│   ├── CachedObjects/
│   │   ├── CachedItem.cs, CachedObject.cs, CachedStrongbox.cs
│   │   ├── CachedTransition.cs, CachedWorldItem.cs
│   │
│   ├── CommonTasks/               # Static task implementations used by FarmingBot
│   │   ├── CombatTaskStatic.cs
│   │   ├── LeaveAreaTaskStatic.cs
│   │   ├── LootItemTaskStatic.cs
│   │   ├── OpenChestTaskStatic.cs
│   │   ├── SpecialObjectTaskStatic.cs
│   │   ├── StashTaskStatic.cs
│   │   ├── TransitionTriggerTaskStatic.cs
│   │   ├── HandleBlockingChestsTaskStatic.cs
│   │   ├── HandleBlockingObjectTaskStatic.cs
│   │   └── ClearCursorTask.cs
│   │
│   ├── Global/
│   │   ├── CombatAreaCache.cs
│   │   ├── ComplexExplorer.cs
│   │   ├── ResurrectionLogic.cs
│   │   ├── StuckDetection.cs
│   │   ├── TrackMobLogic.cs
│   │   └── Travel.cs
│   │
│   └── Positions/
│       ├── Position.cs, StaticPositions.cs, TgtPosition.cs
│       ├── WalkablePosition.cs, WorldPosition.cs
│
├── MapperBot/
│   ├── AffixData.cs
│   ├── DefenseAndFlaskStatic.cs
│   ├── DeviceAreaTask.cs
│   ├── KillBossTask.cs
│   ├── LevelGemsTaskStatic.cs
│   ├── MapData.cs
│   ├── MapExplorationTask.cs
│   ├── MapExtensions.cs         # IsMap(), IsMapFragment(), IsSacrificeFragment(), AtlasData
│   ├── OpenMapTask.cs           # Map device open/fill/activate logic (see below)
│   ├── TakeMap.cs
│   ├── TrackMob.cs
│   ├── TravelToHideoutTaskStatic.cs
│   └── Leagues/
│       ├── Abyss/  (Abyss.cs, FollowAbyssTask.cs, OpenAbyssChestTask.cs, StartAbyssTask.cs)
│       └── Breaches/  (BreachCache.cs, BreachData.cs, HandleBreachesTask.cs)
│
├── MmmjrRoutine/
│   └── MmmjrRoutine.cs          # Combat routine (skills, flasks)
│
└── ExileRoutine/
    └── ExileRoutine.cs          # Alternative combat routine
```

## Core Patterns

### Bot Entry Point
`MmmjrBot.cs` implements `IBot`. The static `_taskManager` (type `TaskManager`) holds the active `ITask` list.

```csharp
public static void OnChangeEnabledBots(int botId, bool enabled)
{
    _taskManager.Reset();
    if (botId == 1 && enabled)       // SextantBot
        _taskManager.Add(new SextantTask());
    else if (botId == 2 && enabled)  // FarmingBot
    {
        mapbot_state = MapperBotState.OpeningMap;
        _taskManager.Add(new FarmingTask());
    }
}
```

### ITask Pattern
Each task implements `ITask` from DreamPoeBot framework:
```csharp
public async Task<bool> Run()  // return false = keep running, true = task complete / yield
```

### Settings
`MmmjrBotSettings` extends `JsonSettings` (auto-serialized to JSON). It is a singleton accessed via `MmmjrBotSettings.Instance`. All properties use `INotifyPropertyChanged` so they bind directly to XAML controls.

Key setting groups:
- **Mapper settings**: `EnableFarmingBot`, `MapperDefensiveSkills`, `MapperFlasks`, `MapperBloorOrSand`, `MaxDeaths`, `ExplorationPercent`, `EnabledAbyss`, `EnabledBreachs`, `TrackMob`
- **Sextant settings**: `EnableSextantBot`, `SelectedSextant`, sextant row/count settings
- **Routine settings**: `MmmjrRoutineName`, `CombatRange`, aspect skill toggles
- **AutoLogin settings**: `EnableAutoLogin`, `Character`, login credentials
- **Overlay settings**: `EnableOverlay`

### MapperBotState
```csharp
public enum MapperBotState { GetItems, OpeningMap, Mapping, LeavingArea, StoringItems }
```
`MmmjrBot.mapbot_state` is used by `FarmingTask` to track the current farming phase.

### SextantBotState
```csharp
public enum SextantBotState { GetItems = 1, RunningRow = 2, StoringRow = 3, StoringCompassInStash = 4 }
```
`SextantTask.sextantBotState` tracks the current sextant automation phase.

## OpenMapTask — Map Device Flow

`OpenMapTask.Run()` handles all map-opening modes. The active mode for ElderBot is `SingleUseElderFragments`.

### Elder Fragment Flow (`SingleUseElderFragments`)
```
1. OpenDevice()
   → Walk to physical Map Device object
   → Interact → wait for AtlasUi.IsOpened
   → Retry if AtlasUi.MapDevice.IsOpened is still false

2. If AtlasUi.MapDevice is not yet open:
   → Open inventory (Inventories.OpenInventory2())
   → Right-click any one elder fragment from inventory
     (UseItem via InventoryUi.InventoryControl_Main.UseItem)
   → Game auto-opens device panel AND auto-places all 4 fragments
   → Verify AtlasUi.MapDevice.IsOpened

3. CheckDeviceHaveFragments()
   → If all 4 already inside → goto opmap (skip re-fill)

4. ClearDevice() — if device has stale items, FastMove them back to inventory

5. Open inventory, verify all 4 fragments present

6. RightClickElderFragment(frag.LocalId)
   → InventoryUi.InventoryControl_Main.UseItem(localId)
   → Wait 2 seconds for game to auto-place all 4

7. Verify AtlasMapDevice.InventoryControl.Inventory.Items.Count >= 4

opmap:
8. ActivateDevice()
   → AtlasMapDevice.Activate()
   → Check LokiPoe.InGameState.ActivateResult.None
   → Wait for AtlasMapDevice.IsOpened == false

9. TakeMapPortal() with retry logic (up to 10 attempts)

10. MapData.ResetCurrent(), KillBossTask.SetNew(), MapExplorationTask.Reset()
```

### Other Modes
- **AtlasExplorationEnabled**: Places a regular map + optional Vaal fragment via `FastMove`
- **SingleUseShaperGuardianMap**: Places one of the 4 Shaper guardian maps
- **SingleUseShaperFragments**: Places all 4 Shaper fragments individually via `FastMove`

## GUI / XAML

`MmmjrGUI.xaml` is a WPF `UserControl` with a `TabControl`. All settings bind to `MmmjrBotSettings.Instance`.

Active tabs:
- **Farming > Mapper Bot v2.1**: General, Combat, Leagues, Flasks, Defensives, Auras, Level Gems
- **Misc Features > Sextant Bot**: General, Mod Selector
- **Trade Bot**
- **Auto Login**
- **Overlay**

**Important**: When removing a settings property from `MmmjrBotSettings.cs`, always check `MmmjrGUI.xaml` for bindings and remove the corresponding XAML elements too.

## Key Dependencies

| Assembly | Purpose |
|---|---|
| `DreamPoeBot.exe` | Core bot framework (IBot, ITask, LokiPoe, TaskManager, etc.) |
| `Newtonsoft.Json` | Settings serialization |
| `SharpDX` / `SharpDX.Direct2D1` / `SharpDX.DXGI` | Overlay rendering |
| `GameOverlay.Net` | Game overlay window |
| `log4net` | Logging (`GlobalLog`, `Logger.GetLoggerInstanceForType()`) |
| `MahApps.Metro` | WPF UI styling |
| `websocket-sharp` | WebSocket communication |

## Logging
Use `GlobalLog.Info(...)` / `GlobalLog.Error(...)` for bot-level logging, or `Logger.GetLoggerInstanceForType()` for per-class log4net loggers.
