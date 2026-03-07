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
│   ├── Inventories.cs
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
│   ├── MapExtensions.cs
│   ├── MapperBot.cs (implicit via FarmingTask.cs)
│   ├── OpenMapTask.cs
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
