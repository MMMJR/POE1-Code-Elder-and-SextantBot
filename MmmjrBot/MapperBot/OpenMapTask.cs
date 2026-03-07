using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.CommonTasks;

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

            // This is more complicated after 3.0 because GGG added stairs to laboratory
            //if (!area.IsHideoutArea && !area.IsMapRoom)
            //    return false;

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
                    MmmjrBot.mapbot_state = MapperBotState. GetItems;
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
            else if(MmmjrBotSettings.Instance.EnableSingleMap)
            {
                if(MmmjrBotSettings.Instance.SingleUseShaperGuardianMap)
                {
                    Item map = new Item();

                    if (!MmmjrBot.ShaperMapperCompletion[0])
                    {
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.PitOfChimera);
                    }
                    else if (!MmmjrBot.ShaperMapperCompletion[1])
                    {
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.ForgeOfThePhoenix);
                    }
                    else if (!MmmjrBot.ShaperMapperCompletion[2])
                    {
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.LairOfTheHydra);
                    }
                    else if (!MmmjrBot.ShaperMapperCompletion[3])
                    {
                        map = Inventories.InventoryItems.Find(i => i.IsMap() && i.Name == CurrencyNames.MazeOfMinotaur);
                    }
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
                        else if(x == 1) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfChimera);
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
                    if (!await PlayerAction.TryTo(OpenDevice, "Open Map Device", 3, 2000))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }

                    if(await CheckDeviceHaveFragments())
                    {
                        goto opmap;
                    }

                    if (!await ClearDevice())
                    {
                        ErrorManager.ReportError();
                        GlobalLog.Info("Aquii;");
                    }

                    Item map = new Item();
                    for (int x = 0; x < 4; x++)
                    {
                        if (x == 0) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfPurification);
                        else if (x == 1) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfConstriction);
                        else if (x == 2) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEnslavement);
                        else if (x == 3) map = Inventories.InventoryItems.Find(i => i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEradication);

                        if (map == null)
                        {
                            GlobalLog.Error("[OpenMapTask] There is no map in inventory. x = " + x);
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
                        await Wait.Sleep(80);
                    }
                    for(int x = 0; x < 4; x++)
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

            /*var isTargetable = portal.IsTargetable;

            if (isTargetable)
            {
                if (!await Wait.For(() => !portal.Fresh().IsTargetable, "old map portals despawning", 200, 10000))
                {
                    ErrorManager.ReportError();
                    return true;
                }
            }
            GlobalLog.Info("Waiting portal");
            if (!await Wait.For(() =>
            {
                var p = portal.Fresh();
                return p.IsTargetable && p.LeadsTo(a => a.IsMap);
            },
                "new map portals spawning", 500, 15000))
            {
                GlobalLog.Info("Waiting portal error");
                ErrorManager.ReportError();
                return true;
            }
            GlobalLog.Info("target portal");*/
            await Wait.SleepSafe(500);

            if (!await TakeMapPortal())
            {
                GlobalLog.Info("enter portal error");
                ErrorManager.ReportError();
                GlobalLog.Info("enter portal error");
            }

            GlobalLog.Info("Entering portal");
            MapData.ResetCurrent();
            GlobalLog.Info("Entering portal2");
            KillBossTask.SetNew();
            GlobalLog.Info("Entering portal3");
            MapExplorationTask.Reset(World.CurrentArea.Name);
            OpenChestTaskStatic.Reset();
            if(MmmjrBotSettings.Instance.EnableMapperRoutineTriggerCWDT)
            {
                MmmjrBot.CWDTTrigger = false;
            }
            GlobalLog.Info("Entering portal4");
            return true;
        }

        private static async Task<bool> OpenDevice()
        {
            if (MapDevice.IsOpen) return true;

            var device = LokiPoe.ObjectManager.MapDevice;
            if (device == null)
            {
                if (World.CurrentArea.IsHideoutArea)
                {
                    GlobalLog.Error("[OpenMapTask] Fail to find Map Device in hideout.");
                }
                else
                {
                    GlobalLog.Error("[OpenMapTask] Unknown error. Fail to find Map Device in Templar Laboratory.");
                }
                GlobalLog.Error("[OpenMapTask] Now stopping the bot because it cannot continue.");
                BotManager.Stop();
                return false;
            }

            GlobalLog.Debug("[OpenMapTask] Now going to open Map Device.");

            await device.WalkablePosition().ComeAtOnce(100);

            if (await PlayerAction.Interact(device, () => MapDevice.IsOpen, "Map Device opening"))
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully opened.");
                return true;
            }
            GlobalLog.Debug("[OpenMapTask] Fail to open Map Device.");
            return false;
        }

        private static async Task<bool> CheckDeviceHaveFragments()
        {
            bool[] frag = new bool[4];
            for(int x = 0; x < 4; x++) frag[x] = false;
            var items = MapDevice.InventoryControl.Inventory.Items.ToList();
            if (items.Count == 0)
                return false;

            foreach (var i in items)
            {
                if (i == null) continue;
                if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfPurification)
                {
                    frag[0] = true;
                }
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEnslavement)
                {
                    frag[1] = true;
                }
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfConstriction)
                {
                    frag[2] = true;
                }
                else if (i.IsMapFragment() && i.Name == CurrencyNames.FragmentOfEradication)
                {
                    frag[3] = true;
                }

            }
            bool ret = true;
            for (int x = 0; x < frag.Length; x++)
            {
                if (frag[x] == false)
                {
                    ret = false;
                    break;
                }
            }
            return ret;
        }

        private static async Task<bool> ClearDevice()
        {
            if(LokiPoe.InGameState.MasterDeviceUi.IsOpened && LokiPoe.InGameState.MasterDeviceUi.IsFiveSlotDevice)
            {
                foreach(var slot in LokiPoe.InGameState.MasterDeviceUi.FiveSlotInventoryControl)
                {
                    if(slot.CustomTabItem != null)
                    {
                        slot.FastMove();
                    }
                }
            }
            else
            {
                var itemPositions = MapDevice.InventoryControl.Inventory.Items.Select(i => i.LocationTopLeft).ToList();
                if (itemPositions.Count == 0)
                    return true;

                GlobalLog.Error("[OpenMapTask] Map Device is not empty. Now going to clean it.");

                foreach (var itemPos in itemPositions)
                {
                    if (!await PlayerAction.TryTo(() => FastMoveFromDevice(itemPos), null, 2))
                        return false;
                }
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully cleaned.");
            }
            
            return true;
        }

        private static async Task<bool> PlaceIntoDevice(Vector2i itemPos)
        {
            var oldCount = MapDevice.InventoryControl.Inventory.Items.Count;

            if (!await Inventories.FastMoveFromInventory(itemPos))
                return false;

            return true;
        }

        private static async Task<bool> ActivateDevice()
        {
            GlobalLog.Debug("[OpenMapTask] Now going to activate the Map Device.");

            await Wait.SleepSafe(500); // Additional delay to ensure Activate button is targetable

            var map = MapDevice.InventoryControl.Inventory.Items.Find(i=> i.Class == ItemClasses.Map || i.Class == ItemClasses.MapFragment);
            if (map == null)
            {
                GlobalLog.Error("[OpenMapTask] Unexpected error. There is no map in the Map Device.");
                return false;
            }

            LokiPoe.InGameState.ActivateResult activated;

            if (World.CurrentArea.IsHideoutArea)
            {
                activated = LokiPoe.InGameState.MasterDeviceUi.Activate();
            }
            else
            {
                activated = LokiPoe.InGameState.MapDeviceUi.Activate();
            }

            if (activated != LokiPoe.InGameState.ActivateResult.None)
            {
                GlobalLog.Error($"[OpenMapTask] Fail to activate the Map Device. Error: \"{activated}\".");
                return false;
            }
            if (await Wait.For(() => !MapDevice.IsOpen, "Map Device closing"))
            {
                GlobalLog.Debug("[OpenMapTask] Map Device has been successfully activated.");
                return true;
            }
            GlobalLog.Error("[OpenMapTask] Fail to activate the Map Device.");
            return false;
        }

        private static async Task<bool> FastMoveFromDevice(Vector2i itemPos)
        {
            var item = MapDevice.InventoryControl.Inventory.FindItemByPos(itemPos);
            if (item == null)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fail to find item at {itemPos} in Map Device.");
                return false;
            }

            var itemName = item.FullName;

            GlobalLog.Debug($"[FastMoveFromDevice] Fast moving \"{itemName}\" at {itemPos} from Map Device.");

            var moved = MapDevice.InventoryControl.FastMove(item.LocalId);
            if (moved != FastMoveResult.None)
            {
                GlobalLog.Error($"[FastMoveFromDevice] Fast move error: \"{moved}\".");
                return false;
            }
            if (await Wait.For(() => MapDevice.InventoryControl.Inventory.FindItemByPos(itemPos) == null, "fast move"))
            {
                GlobalLog.Debug($"[FastMoveFromDevice] \"{itemName}\" at {itemPos} has been successfully fast moved from Map Device.");
                return true;
            }
            GlobalLog.Error($"[FastMoveFromDevice] Fast move timeout for \"{itemName}\" at {itemPos} in Map Device.");
            return false;
        }

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

        private static class MapDevice
        {
            public static bool IsOpen => World.CurrentArea.IsHideoutArea
                ? LokiPoe.InGameState.MasterDeviceUi.IsOpened
                : LokiPoe.InGameState.MapDeviceUi.IsOpened;

            public static InventoryControlWrapper InventoryControl => World.CurrentArea.IsHideoutArea
                ? LokiPoe.InGameState.MasterDeviceUi.InventoryControl
                : LokiPoe.InGameState.MapDeviceUi.InventoryControl;
        }
    }
}