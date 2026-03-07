using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MmmjrBot.Lib;
using MmmjrBot.Lib.CachedObjects;
using InventoryUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.InventoryUi;
using StashUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.StashUi;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using ExSettings = MmmjrBot.Lib.Settings;
using SharpDX;
using System;
using log4net.Util;

namespace MmmjrBot.MapperBot
{
    public static class TakeMap
    {
        private static readonly MmmjrBotSettings Settings = MmmjrBotSettings.Instance;

        private static readonly Dictionary<string, bool> AvailableCurrency = new Dictionary<string, bool>
        {
            [CurrencyNames.Transmutation] = true,
            [CurrencyNames.Augmentation] = true,
            [CurrencyNames.Alteration] = true,
            [CurrencyNames.Alchemy] = true,
            [CurrencyNames.Chaos] = true,
            [CurrencyNames.Scouring] = true,
            [CurrencyNames.Chisel] = true,
            [CurrencyNames.Vaal] = true
        };

        private static readonly Dictionary<string, int> AmountForAvailable = new Dictionary<string, int>
        {
            [CurrencyNames.Transmutation] = 1,
            [CurrencyNames.Augmentation] = 5,
            [CurrencyNames.Alteration] = 5,
            [CurrencyNames.Alchemy] = 5,
            [CurrencyNames.Chaos] = 5,
            [CurrencyNames.Scouring] = 5,
            [CurrencyNames.Chisel] = 4,
            [CurrencyNames.Vaal] = 1
        };

        private static bool _hasFragments = true;

        public static async Task<bool> Run()
        {
            foreach (var key in AvailableCurrency.Keys.ToList())
            {
                AvailableCurrency[key] = true;
            }
            _hasFragments = true;

            var area = World.CurrentArea;

            if (!area.IsTown && !area.IsHideoutArea)
                return false;

            if (Settings.StopRequested)
            {
                GlobalLog.Warn("Stopping the bot by a user's request (stop after current map)");
                Settings.StopRequested = false;
                BotManager.Stop();
                return false;
            }
            if (!await Inventories.OpenStash())
            {
                ErrorManager.ReportError();
                return false;
            }

            if (await FindProperMap())
                goto hasProperMap;

            GlobalLog.Error("[TakeMapTask] Fail to find a proper map in all map tabs. Now stopping the bot because it cannot continue.");
            BotManager.Stop();
            return false;

            hasProperMap:
            return true;
        }

        public static async Task<bool> HandleMap(Item map)
        {
            var mapInv = Inventories.InventoryItems.Where(i => i.Name == map.Name && i.IsMap() && i.Id == map.Id).FirstOrDefault();
            if (mapInv == null) return true;
            var mapPos = mapInv.LocationTopLeft;
            var mapRarity = map.RarityLite();

            if (mapRarity == Rarity.Unique || !map.IsIdentified || map.IsMirrored || map.IsCorrupted)
            {
                ChooseMap(mapPos);
                return true;
            }

            switch (mapRarity)
            {
                case Rarity.Normal:
                    if (!await HandleNormalMap(mapPos)) return true;
                    break;

                case Rarity.Magic:
                    if (!await HandleMagicMap(mapPos)) return true;
                    break;

                case Rarity.Rare:
                    if (!await HandleRareMap(mapPos)) return true;
                    break;

                default:
                    GlobalLog.Error($"[TakeMapTask] Unknown map rarity: \"{mapRarity}\".");
                    ErrorManager.ReportCriticalError();
                    return true;
            }

            var newref = UpdateMapReference(mapPos);

            if (newref.ShouldUpgrade(Settings.VaalUpgrade) && HasCurrency(CurrencyNames.Vaal))
            {
                if (!await CorruptMap(mapPos))
                    return true;

                newref = UpdateMapReference(mapPos);
            }
            if (newref.ShouldUpgrade(Settings.FragmentUpgrade) && _hasFragments)
            {
                await GetFragment();
            }
            ChooseMap(mapPos);
            return true;
        }

        private static async Task<bool> FindProperMap()
        {
            var maps = new List<Item>();
            int mapCount = 0;
            bool[] fragment = new bool[4];
            for (int x = 0; x < 4; x++) fragment[x] = false;

            if (mapCount == 0)
            {
                if (!StashUi.TabControl.IsOnFirstTab)
                {
                    while (StashUi.TabControl.CurrentTabIndex > 0)
                    {
                        var switchTabResult = StashUi.TabControl.PreviousTabKeyboard();
                        if (switchTabResult == SwitchToTabResult.None)
                        {
                            if (StashUi.TabControl.IsValid && StashUi.TabControl.IsOnFirstTab)
                            {
                                break;
                            }
                        }
                    }
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(300);
                }
                if (!StashUi.TabControl.IsOnFirstTab)
                {
                    return false;
                }
                mapCount = 0;
                for (int x = 0; x < 4; x++) fragment[x] = false;
                for (int tabIndex = StashUi.TabControl.CurrentTabIndex; tabIndex < StashUi.TabControl.LastTabIndex; tabIndex++)
                {
                    if (StashUi.StashTabInfo.IsPremiumQuad || StashUi.StashTabInfo.IsPremiumSpecial || StashUi.StashTabInfo.IsNormalTab)
                    {
                        List<Item> tabItems = StashUi.InventoryControl.Inventory.Items;
                        foreach (var map in tabItems.Where(i => (i.IsMap() || i.IsMapFragment())))
                        {
                            if (mapCount > 3) break;
                            if (MmmjrBotSettings.Instance.EnableSingleMap)
                            {
                                if (MmmjrBotSettings.Instance.SingleMapMetadata != "" && map.Metadata == MmmjrBotSettings.Instance.SingleMapMetadata)
                                {
                                    maps.Add(map);
                                    mapCount++;
                                }
                                else if (MmmjrBotSettings.Instance.SingleUseShaperGuardianMap)
                                {
                                    if (map.Name == CurrencyNames.LairOfTheHydra && fragment[0] == false)
                                    {
                                        maps.Add(map);
                                        fragment[0] = true;
                                        mapCount++;
                                        MmmjrBot.ShaperMapperCompletion[0] = false;
                                    }
                                    else if (map.Name == CurrencyNames.ForgeOfThePhoenix && fragment[1] == false)
                                    {
                                        maps.Add(map);
                                        fragment[1] = true;
                                        mapCount++;
                                        MmmjrBot.ShaperMapperCompletion[1] = false;
                                    }
                                    else if (map.Name == CurrencyNames.PitOfChimera && fragment[2] == false)
                                    {
                                        maps.Add(map);
                                        fragment[2] = true;
                                        mapCount++;
                                        MmmjrBot.ShaperMapperCompletion[2] = false;
                                    }
                                    else if (map.Name == CurrencyNames.MazeOfMinotaur && fragment[3] == false)
                                    {
                                        maps.Add(map);
                                        fragment[3] = true;
                                        mapCount++;
                                        MmmjrBot.ShaperMapperCompletion[3] = false;
                                    }
                                }
                                else if (MmmjrBotSettings.Instance.SingleUseShaperFragments)
                                {
                                    if (map.Name == CurrencyNames.FragmentOfChimera && fragment[0] == false)
                                    {
                                        maps.Add(map);
                                        fragment[0] = true;
                                        mapCount++;
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfPhoenix && fragment[1] == false)
                                    {
                                        maps.Add(map);
                                        fragment[1] = true;
                                        mapCount++;
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfChimera && fragment[2] == false)
                                    {
                                        maps.Add(map);
                                        fragment[2] = true;
                                        mapCount++;
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfMinotaur && fragment[3] == false)
                                    {
                                        maps.Add(map);
                                        fragment[3] = true;
                                        mapCount++;
                                    }
                                }
                                else if (MmmjrBotSettings.Instance.SingleUseElderFragments)
                                {
                                    if (map.Name == CurrencyNames.FragmentOfConstriction && fragment[0] == false)
                                    {
                                        fragment[0] = true;
                                        mapCount++;
                                        if (!await Inventories.FastMoveFromStashTab(map.LocationTopLeft))
                                        {
                                            ErrorManager.ReportError();
                                        }

                                        await Wait.ArtificialDelay();
                                        await Wait.Sleep(100);
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfEnslavement && fragment[1] == false)
                                    {
                                        if (!await Inventories.FastMoveFromStashTab(map.LocationTopLeft))
                                        {
                                            ErrorManager.ReportError();
                                        }

                                        await Wait.ArtificialDelay();
                                        await Wait.Sleep(100);
                                        fragment[1] = true;
                                        mapCount++;
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfPurification && fragment[2] == false)
                                    {
                                        if (!await Inventories.FastMoveFromStashTab(map.LocationTopLeft))
                                        {
                                            ErrorManager.ReportError();
                                        }

                                        await Wait.ArtificialDelay();
                                        await Wait.Sleep(100);
                                        fragment[2] = true;
                                        mapCount++;
                                    }
                                    else if (map.Name == CurrencyNames.FragmentOfEradication && fragment[3] == false)
                                    {
                                        if (!await Inventories.FastMoveFromStashTab(map.LocationTopLeft))
                                        {
                                            ErrorManager.ReportError();
                                        }

                                        await Wait.ArtificialDelay();
                                        await Wait.Sleep(100);
                                        fragment[3] = true;
                                        mapCount++;
                                    }
                                }
                            }
                            else if (MmmjrBotSettings.Instance.AtlasExplorationEnabled)
                            {
                                GlobalLog.Info("MM_2");
                                if (map.IsMapFragment()) continue;
                                var rarity = map.RarityLite();
                                if (rarity == Rarity.Unique)
                                {
                                    GlobalLog.Info("MM_22");
                                    maps.Add(map);
                                    continue;
                                }

                                if (!map.BelowTierLimit())
                                {
                                    GlobalLog.Info("MM_23");
                                    continue;
                                }

                                GlobalLog.Info("MM_3");
                                maps.Add(map);
                                GlobalLog.Info("Maps: " + maps.Count);
                                mapCount++;
                                break;
                            }
                        }
                    }
                    if (mapCount == 4)
                        break;

                    var switchTabResult = StashUi.TabControl.NextTabKeyboard();
                    if (switchTabResult != SwitchToTabResult.None)
                    {
                        await Wait.ArtificialDelay();
                        await Wait.Sleep(150);
                        return false;
                    }
                }
            }
            if (mapCount < 4)
                return false;

            await Wait.ArtificialDelay();
            return true;
            
        }

        private static async Task<bool> HandleNormalMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleNormalMap] Fail to find a map at {mapPos}.");
                return false;
            }
            if (!map.IsIdentified) return true;

            if (map.ShouldUpgrade(Settings.ChiselUpgrade) && HasCurrency(CurrencyNames.Chisel))
            {
                if (!await ApplyChisels(mapPos))
                    return false;

                map = UpdateMapReference(mapPos);
            }
            if (map.ShouldUpgrade(Settings.RareUpgrade) && HasRareOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                    return false;

                await Wait.ArtificialDelay();
                return true;
            }
            if (map.ShouldUpgrade(Settings.MagicUpgrade) && HasMagicOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Transmutation))
                    return false;

                await Wait.ArtificialDelay();
                return true;
            }
            return true;
        }

        private static async Task<bool> HandleMagicMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleMagicMap] Fail to find map at {mapPos}.");
                return false;
            }
            if (map.ShouldUpgrade(Settings.MagicRareUpgrade) && HasMagicToRareOrbs)
            {
                if (!await ApplyOrb(mapPos, CurrencyNames.Scouring))
                    return false;

                if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                    return false;

                await Wait.ArtificialDelay();
                return true;
            }
            await Wait.ArtificialDelay();
            return true;
        }

        private static async Task<bool> HandleRareMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (map == null)
            {
                GlobalLog.Error($"[HandleRareMap] Fail to find map at {mapPos}.");
                return false;
            }
            await Wait.ArtificialDelay();
            return true;
        }

        public static async Task<bool> RerollMagic(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[RerollMagic] Fail to find a map at {mapPos}.");
                    return false;
                }
                var rarity = map.RarityLite();
                if (rarity != Rarity.Magic)
                {
                    GlobalLog.Error($"[TakeMapTask] RerollMagic is called on {rarity} map.");
                    return false;
                }

                if (map.CanAugment() && HasCurrency(CurrencyNames.Augmentation))
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Augmentation))
                        return false;

                    continue;
                }
                return true;
            }
        }

        /*public static async Task<bool> RerollRare(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[RerollRare] Fail to find a map at {mapPos}.");
                    return false;
                }
                var rarity = map.RarityLite();
                if (rarity != Rarity.Rare)
                {
                    GlobalLog.Error($"[TakeMapTask] RerollRare is called on {rarity} map.");
                    return false;
                }

                var affix = map.GetBannedAffix();

                if (affix == null)
                    return true;

                GlobalLog.Info($"[RerollRare] Rerolling banned \"{affix}\" affix.");

                if (Settings.RerollMethod == RareReroll.Chaos)
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Chaos))
                        return false;
                }
                else
                {
                    if (!await ApplyOrb(mapPos, CurrencyNames.Scouring))
                        return false;

                    if (!await ApplyOrb(mapPos, CurrencyNames.Alchemy))
                        return false;
                }
            }
        }*/

        public static async Task<bool> ApplyChisels(Vector2i mapPos)
        {
            while (true)
            {
                var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
                if (map == null)
                {
                    GlobalLog.Error($"[ApplyChisels] Fail to find a map at {mapPos}.");
                    return false;
                }

                if (map.Quality >= 18)
                    return true;

                if (!await ApplyOrb(mapPos, CurrencyNames.Chisel))
                    return false;
            }
        }

        private static async Task<bool> CorruptMap(Vector2i mapPos)
        {
            if (!await ApplyOrb(mapPos, CurrencyNames.Vaal))
                return false;

            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            if (!map.IsIdentified)
            {
                GlobalLog.Warn("[CorruptMap] Unidentified corrupted map retains it's original affixes. We are good to go.");
                return true;
            }

            if (!map.BelowTierLimit())
            {
                GlobalLog.Warn("[CorruptMap] Map tier has been increased beyond tier limit in settings.");
                return false;
            }

            GlobalLog.Warn("[CorruptMap] Resulting corrupted map fits all requirements. We are good to go.");
            return true;
        }

        private static async Task<bool> ApplyOrb(Vector2i targetPos, string orbName)
        {
            if (!await Inventories.FindTabWithCurrency(orbName))
            {
                GlobalLog.Warn($"[TakeMapTask] There are no \"{orbName}\" in all tabs assigned to them. Now marking this currency as unavailable.");
                AvailableCurrency[orbName] = false;
                return false;
            }

            if (StashUi.StashTabInfo.IsPremiumCurrency)
            {
                var control = Inventories.GetControlWithCurrency(orbName);
                if (!await control.PickItemToCursor(true))
                {
                    ErrorManager.ReportError();
                    return false;
                }
            }
            else
            {
                var orb = Inventories.StashTabItems.Find(i => i.Name == orbName);
                if (!await StashUi.InventoryControl.PickItemToCursor(orb.LocationTopLeft, true))
                {
                    ErrorManager.ReportError();
                    return false;
                }
            }
            if (!await InventoryUi.InventoryControl_Main.PlaceItemFromCursor(targetPos))
            {
                ErrorManager.ReportError();
                return false;
            }
            return true;
        }

        private static async Task GetFragment()
        {
            var tabs = new List<string>(ExSettings.Instance.GetTabsForCategory(ExSettings.StashingCategory.Fragment));

            if (tabs.Count > 1 && StashUi.IsOpened)
            {
                var currentTab = StashUi.StashTabInfo.DisplayName;
                var index = tabs.IndexOf(currentTab);
                if (index > 0)
                {
                    var tab = tabs[index];
                    tabs.RemoveAt(index);
                    tabs.Insert(0, tab);
                }
            }

            foreach (var tab in tabs)
            {
                GlobalLog.Debug($"[TakeMapTask] Looking for Sacrifice Fragment in \"{tab}\" tab.");

                if (!await Inventories.OpenStashTab(tab))
                    return;

                if (StashUi.StashTabInfo.IsPremiumSpecial)
                {
                    var tabType = StashUi.StashTabInfo.TabType;
                    if (tabType == InventoryTabType.Fragment)
                    {
                        foreach (var control in SacrificeControls)
                        {
                            var fragment = control.CustomTabItem;
                            if (fragment != null)
                            {
                                GlobalLog.Debug($"[TakeMapTask] Found \"{fragment.Name}\" in \"{tab}\" tab.");
                                await Inventories.FastMoveFromPremiumStashTab(control);
                                return;
                            }
                            GlobalLog.Debug($"[TakeMapTask] There are no Sacrifice Fragments in \"{tab}\" tab.");
                        }
                    }
                    else
                    {
                        GlobalLog.Error($"[TakeMapTask] Incorrect tab type ({tabType}) for sacrifice fragments.");
                    }
                }
                else
                {
                    var fragment = Inventories.StashTabItems
                        .Where(i => i.IsSacrificeFragment())
                        .OrderBy(i => i.Name == "Sacrifice at Midnight") // move midnights to the end of the list
                        .FirstOrDefault();

                    if (fragment != null)
                    {
                        GlobalLog.Debug($"[TakeMapTask] Found \"{fragment.Name}\" in \"{tab}\" tab.");
                        await Inventories.FastMoveFromStashTab(fragment.LocationTopLeft);
                        return;
                    }
                    GlobalLog.Debug($"[TakeMapTask] There are no Sacrifice Fragments in \"{tab}\" tab.");
                }
            }
            GlobalLog.Info("[TakeMapTask] There are no Sacrifice Fragments in all tabs assigned to them. Now marking them as unavailable.");
            _hasFragments = false;
        }

        private static void ChooseMap(Vector2i mapPos)
        {
            var map = InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
            //To Open Map
            //OpenMapTask.Enabled = true;
        }

        // ReSharper disable once RedundantAssignment
        private static Item UpdateMapReference(Vector2i mapPos)
        {
            return InventoryUi.InventoryControl_Main.Inventory.FindItemByPos(mapPos);
        }

        private static bool NoRareUpgrade(Item map)
        {
            return !map.ShouldUpgrade(Settings.RareUpgrade) && !map.ShouldUpgrade(Settings.MagicRareUpgrade);
        }

        private static bool HasCurrency(string name)
        {
            if (AvailableCurrency[name])
                return true;

            GlobalLog.Debug($"[TakeMapTask] HasCurrency is false for {name}.");
            return false;
        }

        private static bool HasMagicOrbs
        {
            get
            {
                return HasCurrency(CurrencyNames.Alteration) &&
                       HasCurrency(CurrencyNames.Augmentation) &&
                       HasCurrency(CurrencyNames.Transmutation);
            }
        }

        private static bool HasRareOrbs
        {
            get
            {
                return HasCurrency(CurrencyNames.Alchemy);
            }
        }

        private static bool HasMagicToRareOrbs
        {
            get
            {
                return HasScourAlchemy;
            }
        }

        private static bool HasScourAlchemy
        {
            get
            {
                return HasCurrency(CurrencyNames.Scouring) &&
                       HasCurrency(CurrencyNames.Alchemy);
            }
        }

        private static bool HasScourTransmute
        {
            get
            {
                return HasCurrency(CurrencyNames.Scouring) &&
                       HasCurrency(CurrencyNames.Transmutation);
            }
        }

        private static IEnumerable<InventoryControlWrapper> SacrificeControls => new[]
        {
            StashUi.FragmentTab.General.SacrificeatDusk,
            StashUi.FragmentTab.General.SacrificeatDawn,
            StashUi.FragmentTab.General.SacrificeatNoon,
            StashUi.FragmentTab.General.SacrificeatMidnight
        };

        private static void UpdateAvailableCurrency(string currencyName)
        {
            if (!AvailableCurrency.TryGetValue(currencyName, out bool available))
                return;

            if (available)
                return;

            var amount = Inventories.GetCurrencyAmountInStashTab(currencyName);
            if (amount >= AmountForAvailable[currencyName])
            {
                GlobalLog.Info($"[TakeMapTask] There are {amount} \"{currencyName}\" in current stash tab. Now marking this currency as available.");
                AvailableCurrency[currencyName] = true;
            }
        }

        private static void UpdateAvailableFragments()
        {
            if (StashUi.StashTabInfo.IsPremiumFragment)
            {
                if (SacrificeControls.Any(c => c.CustomTabItem != null))
                {
                    GlobalLog.Info("[TakeMapTask] Sacrifice Fragment has been stashed. Now marking it as available.");
                    _hasFragments = true;
                }
            }
            else
            {
                if (Inventories.StashTabItems.Any(i => i.IsSacrificeFragment()))
                {
                    GlobalLog.Info("[TakeMapTask] Sacrifice Fragment has been stashed. Now marking it as available.");
                    _hasFragments = true;
                }
            }
        }
    }
}