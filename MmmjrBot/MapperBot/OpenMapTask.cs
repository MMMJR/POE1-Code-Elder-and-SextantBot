using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using MmmjrBot.Lib;
using MmmjrBot.Lib.CommonTasks;
using System.Linq;
using System.Threading.Tasks;
using AtlasMapDevice = DreamPoeBot.Loki.Game.LokiPoe.InGameState.AtlasUi.MapDevice;
using InventoryUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.InventoryUi;

namespace MmmjrBot.MapperBot
{
    public static class OpenMapTask
    {
        internal static bool Enabled;

        static bool[] fragments = new bool[4];

        public static async Task<bool> Run()
        {
            for (int x = 0; x < 4; x++) fragments[x] = false;

            var area = World.CurrentArea;

            if (area.IsHideoutArea)
                goto inProperArea;

            if (area.IsMapRoom)
            {
                if (await DeviceAreaTask.HandleStairs(true))
                    return true;

                goto inProperArea;
            }

            return false;

            inProperArea:

            if (MmmjrBotSettings.Instance.AtlasExplorationEnabled)
            {
                var map = Inventories.InventoryItems.Find(i => i.IsMap());
                if (map == null)
                {
                    GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                    MmmjrBot.mapbot_state = MapperBotState.GetItems;
                    return false;
                }

                var mapPos = map.LocationTopLeft;

                await Coroutines.CloseBlockingWindows();

                if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                {
                    ErrorManager.ReportError();
                    return false;
                }

                if (!await ClearDevice())
                {
                    ErrorManager.ReportError();
                    return false;
                }

                if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                {
                    ErrorManager.ReportError();
                    return false;
                }

                var fragment = Inventories.InventoryItems.Find(i => i.IsSacrificeFragment());
                if (fragment != null)
                {
                    await PlayerAction.TryTo(() => PlaceIntoDevice(fragment.LocationTopLeft), "Place vaal fragment into device", 3);
                }
            }
            else if (MmmjrBotSettings.Instance.EnableSingleMap)
            {
                if (MmmjrBotSettings.Instance.SingleUseShaperGuardianMap)
                {
                    Item map = new Item();

                    if (!MmmjrBot.ShaperMapperCompletion[0])
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.PitOfChimera);
                    else if (!MmmjrBot.ShaperMapperCompletion[1])
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.ForgeOfThePhoenix);
                    else if (!MmmjrBot.ShaperMapperCompletion[2])
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.LairOfTheHydra);
                    else if (!MmmjrBot.ShaperMapperCompletion[3])
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.MazeOfMinotaur);
                    else
                        return false;

                    if (map == null)
                    {
                        GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                        MmmjrBot.mapbot_state = MapperBotState.GetItems;
                        return false;
                    }

                    var mapPos = map.LocationTopLeft;

                    if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }
                    if (!await ClearDevice())
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    await Wait.ArtificialDelay();
                    await Wait.Sleep(30);
                }
                else if (MmmjrBotSettings.Instance.SingleUseShaperFragments)
                {
                    if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    if (!await ClearDevice())
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    for (int x = 0; x < 4; x++)
                    {
                        Item map = new Item();
                        if (x == 0) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfHydra);
                        else if (x == 1) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfChimera);
                        else if (x == 2) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfPhoenix);
                        else if (x == 3) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfMinotaur);

                        if (map == null)
                        {
                            GlobalLog.Error("[OpenMapTask] There is no map in inventory.");
                            MmmjrBot.mapbot_state = MapperBotState.GetItems;
                            return false;
                        }

                        var mapPos = map.LocationTopLeft;

                        if (!await PlayerAction.TryTo(() => PlaceIntoDevice(mapPos), "Place map into device", 3))
                        {
                            ErrorManager.ReportError();
                            return false;
                        }

                        fragments[x] = true;
                        await Wait.ArtificialDelay();
                        await Wait.Sleep(40);
                    }
                    for (int x = 0; x < fragments.Length; x++)
                    {
                        if (!fragments[x])
                        {
                            if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                            {
                                ErrorManager.ReportError();
                                return false;
                            }

                            if (!await ClearDevice())
                            {
                                ErrorManager.ReportError();
                                return false;
                            }
                            return false;
                        }
                    }
                }
                else if (MmmjrBotSettings.Instance.SingleUseElderFragments)
                {
                    // 1. Open the map device
                    if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    if(!LokiPoe.InGameState.AtlasUi.IsOpened)
                    {
                        if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                        {
                            ErrorManager.ReportError();
                            return false;
                        }
                    }

                    await Wait.Sleep(1000);

                    if (!LokiPoe.InGameState.AtlasUi.MapDevice.IsOpened)
                    {
                        if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                        {
                            if (!await Inventories.OpenInventory2())
                            {
                                return false;
                            }
                            if (!await Wait.For(() => InventoryUi.IsOpened, "inventory opening", 100, 3000))
                            {
                                GlobalLog.Error("[OpenMapTask] Failed to open inventory.");
                                //ErrorManager.ReportError();
                                return false;
                            }
                        }
                        
                        var l = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.Items.Where(n => n.Name == CurrencyNames.FragmentOfPurification || n.Name == CurrencyNames.FragmentOfEnslavement || n.Name == CurrencyNames.FragmentOfConstriction || n.Name == CurrencyNames.FragmentOfEradication).ToList();

                        if (l.Count > 0)
                        {
                            var frag = l.First();
                            if (frag != null)
                            {
                                if (!await PlayerAction.TryTo(() => RightClickElderFragment(frag.LocalId), "Right-click elder fragment", 3))
                                {
                                    GlobalLog.Warn("[OpenMapTask] Failed to right-click elder fragment. Attempting to continue anyway, but this may cause the bot to fail.");
                                    //ErrorManager.ReportError();
                                    return false;
                                }
                                if (!LokiPoe.InGameState.AtlasUi.MapDevice.IsOpened)
                                {
                                    GlobalLog.Error("[OpenMapTask] Failed to open Map Device UI after right-clicking fragment.");
                                    //ErrorManager.ReportError();
                                    return false;
                                }
                            }
                        }
                    }

                    // 2. If device already has the 4 fragments, go straight to activate
                    if (await CheckDeviceHaveFragments())
                        goto opmap;

                    // 3. Clear device if it has stale items
                    if (!await ClearDevice())
                    {
                        ErrorManager.ReportError();
                        GlobalLog.Error("[OpenMapTask] Failed to clear device.");
                    }

                    // 4. Open inventory
                    if (!InventoryUi.IsOpened)
                    {
                        LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                        if (!await Wait.For(() => InventoryUi.IsOpened, "inventory opening", 100, 3000))
                        {
                            GlobalLog.Error("[OpenMapTask] Failed to open inventory.");
                            ErrorManager.ReportError();
                            return false;
                        }
                        await Wait.ArtificialDelay();
                    }

                    // 5. Verify all 4 elder fragments are present in inventory
                    var frag0 = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfPurification);
                    var frag1 = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfConstriction);
                    var frag2 = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEnslavement);
                    var frag3 = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEradication);

                    if (frag0 == null || frag1 == null || frag2 == null || frag3 == null)
                    {
                        GlobalLog.Error("[OpenMapTask] Not all 4 elder fragments found in inventory. Going to GetItems.");
                        MmmjrBot.mapbot_state = MapperBotState.GetItems;
                        return false;
                    }

                    // 6. Right-click one fragment — game auto-places all 4 into the device
                    if (!await PlayerAction.TryTo(
                        () => RightClickElderFragment(frag0.LocalId),
                        "Right-click elder fragment", 3))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    // 7. Wait for the game to place all fragments (2 seconds)
                    await Wait.SleepSafe(2000);

                    // 8. Verify fragments are inside the device
                    var deviceItems = AtlasMapDevice.InventoryControl.Inventory.Items;
                    if (deviceItems == null || deviceItems.Count < 4)
                    {
                        GlobalLog.Error($"[OpenMapTask] Device does not have 4 fragments after right-click. Found: {deviceItems?.Count ?? 0}");
                        ErrorManager.ReportError();
                        return false;
                    }

                    GlobalLog.Debug($"[OpenMapTask] {deviceItems.Count} fragments confirmed in device. Proceeding to activate.");
                }
            }

            opmap:
            if (!await PlayerAction.TryTo(ActivateDevice, "Activate Map Device", 3))
            {
                ErrorManager.ReportError();
                return true;
            }

            await Wait.SleepSafe(4500);

            var portal = LokiPoe.ObjectManager.Portals.FirstOrDefault();
            if (portal == null)
            {
                GlobalLog.Error("[OpenMapTask] Unknown error. Fail to find any portal near map device.");
            }

            await Wait.SleepSafe(500);

            if (!await TakeMapPortal())
            {
                GlobalLog.Info("enter portal error");
                ErrorManager.ReportError();
                GlobalLog.Info("enter portal error");
            }

            await Wait.SleepSafe(500);

            portal = LokiPoe.ObjectManager.Portals.FirstOrDefault();
            if (portal != null)
            {
                await Wait.SleepSafe(500);

                if (!await TakeMapPortal())
                {
                    GlobalLog.Info("enter portal error");
                    ErrorManager.ReportError();
                    GlobalLog.Info("enter portal error");
                    return false;
                }
            }

            GlobalLog.Info("Entering portal");
            MapData.ResetCurrent();
            GlobalLog.Info("Entering portal2");
            KillBossTask.SetNew();
            GlobalLog.Info("Entering portal3");
            MapExplorationTask.Reset(World.CurrentArea.Name);
            OpenChestTaskStatic.Reset();
            if (MmmjrBotSettings.Instance.EnableMapperRoutineTriggerCWDT)
            {
                MmmjrBot.CWDTTrigger = false;
            }
            GlobalLog.Info("Entering portal4");
            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Helper: right-click a fragment by LocalId so the game auto-fills
        // all 4 elder fragments into the open device panel.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> RightClickElderFragment(int localId)
        {
            GlobalLog.Debug("[OpenMapTask] Right-clicking elder fragment to auto-fill device.");

            var err = InventoryUi.InventoryControl_Main.UseItem(localId);
            if (err != UseItemResult.None)
            {
                GlobalLog.Error($"[OpenMapTask] UseItem (right-click) failed. Error: \"{err}\".");
                return false;
            }

            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Opens the physical Map Device object and waits for the UI.
        // ──────────────────────────────────────────────────────────────
        public static async Task<bool> OpenDevice()
        {
            if (AtlasMapDevice.IsOpened) return true;

            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                if (World.CurrentArea.IsHideoutArea)
                    GlobalLog.Error("[OpenMapTask] Fail to find Map Device in hideout.");
                else
                    GlobalLog.Error("[OpenMapTask] Fail to find Map Device in Templar Laboratory.");

                GlobalLog.Error("[OpenMapTask] Now stopping the bot because it cannot continue.");
                BotManager.Stop();
                return false;
            }

            GlobalLog.Debug("[OpenMapTask] Now going to open Map Device.");

            await device.WalkablePosition().ComeAtOnce(100);

            if (await PlayerAction.Interact(device, () => LokiPoe.InGameState.AtlasUi.IsOpened, "Map Device opening"))
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully opened.");
                return true;
            }
            GlobalLog.Debug("[OpenMapTask] Fail to open Map Device.");
            return false;
        }

        // ──────────────────────────────────────────────────────────────
        // Returns true if all 4 elder fragments are already in the device.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> CheckDeviceHaveFragments()
        {
            bool[] frag = new bool[4];
            for (int x = 0; x < 4; x++) frag[x] = false;

            var items = AtlasMapDevice.InventoryControl.Inventory.Items.ToList();
            if (items.Count == 0)
                return false;

            foreach (var i in items)
            {
                if (i == null) continue;
                if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfPurification)  frag[0] = true;
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEnslavement) frag[1] = true;
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfConstriction) frag[2] = true;
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEradication)  frag[3] = true;
            }

            for (int x = 0; x < frag.Length; x++)
                if (!frag[x]) return false;

            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Clears all items from the device back to inventory.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> ClearDevice()
        {
            var itemPositions = AtlasMapDevice.InventoryControl.Inventory.Items
                .Select(i => i.LocationTopLeft).ToList();

            if (itemPositions.Count == 0)
                return true;

            GlobalLog.Error("[OpenMapTask] Map Device is not empty. Now going to clean it.");

            foreach (var itemPos in itemPositions)
            {
                if (!await PlayerAction.TryTo(() => FastMoveFromDevice(itemPos), null, 2))
                    return false;
            }

            GlobalLog.Debug("[OpenMapTask] Map Device has been successfully cleaned.");
            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Fast-moves (Ctrl+Click) an item from inventory into the device.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> PlaceIntoDevice(Vector2i itemPos)
        {
            if (!await Inventories.FastMoveFromInventory(itemPos))
                return false;

            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // Activates the device (clicks the Activate button).
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> ActivateDevice()
        {
            GlobalLog.Debug("[OpenMapTask] Now going to activate the Map Device.");

            await Wait.SleepSafe(500);

            var map = AtlasMapDevice.InventoryControl.Inventory.Items
                .Find(i => i.Class == ItemClasses.Map || i.Class == ItemClasses.MapFragment);
            if (map == null)
            {
                GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                return false;
            }

            var activated = AtlasMapDevice.Activate();

            if (activated != LokiPoe.InGameState.ActivateResult.None)
            {
                GlobalLog.Error($"[OpenMapTask] Fail to activate the Map Device. Error: \"{activated}\".");
                return false;
            }

            if (await Wait.For(() => !AtlasMapDevice.IsOpened, "Map Device closing"))
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully activated.");
                return true;
            }

            GlobalLog.Error("[OpenMapTask] Fail to activate the Map Device.");
            return false;
        }

        // ──────────────────────────────────────────────────────────────
        // Fast-moves an item from the device back to inventory.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> FastMoveFromDevice(Vector2i itemPos)
        {
            var item = AtlasMapDevice.InventoryControl.Inventory.FindItemByPos(itemPos);
            if (item == null)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fail to find item at {itemPos} in Map Device.");
                return false;
            }

            var itemName = item.FullName;
            GlobalLog.Debug($"[FastMoveFromDevice] Fast moving \"{itemName}\" at {itemPos} from Map Device.");

            var moved = AtlasMapDevice.InventoryControl.FastMove(item.LocalId);
            if (moved != FastMoveResult.None)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fast move error: \"{moved}\".");
                return false;
            }

            if (await Wait.For(
                () => AtlasMapDevice.InventoryControl.Inventory.FindItemByPos(itemPos) == null, "fast move"))
            {
                GlobalLog.Debug($"[FastMoveFromDevice] \"{itemName}\" at {itemPos} has been successfully fast moved from Map Device.");
                return true;
            }

            GlobalLog.Error($"[FastMoveFromDevice] Fast move timeout for \"{itemName}\" at {itemPos} in Map Device.");
            return false;
        }

        // ──────────────────────────────────────────────────────────────
        // Enters the map portal after activation.
        // ──────────────────────────────────────────────────────────────
        private static async Task<bool> TakeMapPortal(int attempts = 10)
        {
            for (int i = 1; i <= attempts; ++i)
            {
                if (!LokiPoe.IsInGame || World.CurrentArea.IsMap)
                    return true;

                var portal = LokiPoe.ObjectManager.Portals.FirstOrDefault();
                GlobalLog.Debug($"[OpenMapTask] Take portal to map attempt: {i}/{attempts}");

                if (await PlayerAction.TakePortal(portal))
                    return true;

                await Wait.SleepSafe(1000);
            }
            return false;
        }
    }
}
