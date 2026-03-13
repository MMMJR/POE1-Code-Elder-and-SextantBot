# POE1 Elder & Sextant Bot

A Path of Exile 1 bot plugin for the [DreamPoeBot](https://github.com/DreamPoeBot) framework.
Automates two endgame farming workflows: **Elder Fragment runs** and **Sextant crafting**.

## Features

### FarmingBot / ElderBot
Automates full Elder fragment map runs:
- Fetches maps/fragments from stash
- Opens the Map Device using the new AtlasUi API
- Right-clicks one fragment — game auto-places all 4 Elder fragments
- Activates device, enters map portal
- Explores the map, kills bosses, loots items
- Handles league mechanics: Breaches, Abysses
- Returns to hideout and stores items in stash
- Supports gem leveling and defensive skills/flasks

### SextantBot
Automates sextant (compass) crafting on the Atlas:
- Runs configurable rows of Atlas nodes
- Applies selected sextant mods
- Stores completed compasses back to stash

## Requirements

- Path of Exile 1 (Steam or standalone)
- [DreamPoeBot](https://dreambot.org) v0.3.28.05 BETA or later
- Visual Studio 2019+ with .NET Framework 4.8
- `DreamPoeBot.exe` placed in `MmmjrBot/bin/Debug/`

## Build

```bash
msbuild MmmjrBot/MmmjrBot.csproj /p:Configuration=Debug
```

The output `MmmjrBot.dll` goes into `MmmjrBot/bin/Debug/`. Copy it to the DreamPoeBot plugins folder.

## Setup

1. Build the project (see above)
2. Place `MmmjrBot.dll` in the DreamPoeBot `Plugins/` folder
3. Launch DreamPoeBot and load the plugin
4. Configure settings in the **Farming** or **Sextant Bot** tabs

### FarmingBot Configuration
| Setting | Description |
|---|---|
| `EnableFarmingBot` | Enable/disable the FarmingBot |
| `SingleUseElderFragments` | Elder fragment run mode (Purification, Constriction, Enslavement, Eradication) |
| `SingleUseShaperFragments` | Shaper fragment run mode (Hydra, Chimera, Phoenix, Minotaur) |
| `SingleUseShaperGuardianMap` | Shaper guardian map sequence mode |
| `AtlasExplorationEnabled` | Atlas exploration mode with regular maps |
| `ExplorationPercent` | Map exploration % before leaving |
| `MaxDeaths` | Max deaths before stopping |
| `EnabledAbyss` | Handle Abyss encounters |
| `EnabledBreachs` | Handle Breach encounters |
| `TrackMob` | Track and kill specific mobs |

### SextantBot Configuration
| Setting | Description |
|---|---|
| `EnableSextantBot` | Enable/disable the SextantBot |
| `SelectedSextant` | Which sextant type to apply |

## Bot Modes

| ID | Bot | Description |
|---|---|---|
| 1 | SextantBot | Atlas sextant crafting automation |
| 2 | FarmingBot | Map farming / Elder fragment runs |

## Project Structure

See [CLAUDE.md](CLAUDE.md) for the full developer reference including architecture, API notes, and the complete file tree.

## Notes

- **DreamPoeBot v0.3.28.05+**: `MasterDeviceUi` and `MapDeviceUi` are obsolete. All map device interaction now goes through `LokiPoe.InGameState.AtlasUi.MapDevice`.
- The bot runs from a **hideout** with a Map Device placed.
- Stash tabs must be configured in DreamPoeBot's stash settings.
