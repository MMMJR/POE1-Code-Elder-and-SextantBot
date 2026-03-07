using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using MmmjrBot.Lib.Positions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cursor = DreamPoeBot.Loki.Game.LokiPoe.InGameState.CursorItemOverlay;
using InventoryUi = DreamPoeBot.Loki.Game.LokiPoe.InGameState.InventoryUi;
using AtlasUI = DreamPoeBot.Loki.Game.LokiPoe.InGameState.AtlasUi;
using StashUI = DreamPoeBot.Loki.Game.LokiPoe.InGameState.StashUi;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using DreamPoeBot.Loki.Controllers;
using System;
using System.Collections.Generic;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using MmmjrBot.Lib.CommonTasks;
using SharpDX;
using System.Security.Cryptography;
using DreamPoeBot.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using Newtonsoft.Json.Linq;

namespace MmmjrBot
{
    public enum SextantBotState
    {
        GetItems = 1,
        RunningRow = 2,
        StoringRow = 3,
        StoringCompassInStash = 4
    }
	class SextantTask : ITask
	{
		private static readonly Interval ScanInterval = new Interval(500);

        public static SextantBotState sextantBotState = SextantBotState.GetItems;
        public static string selectedSextantMetadata = "";

        public async Task<bool> Run()
		{
            if (!MmmjrBotSettings.Instance.EnableSextantBot)
            {
                return true;
            }

            if (!World.CurrentArea.IsTown && !World.CurrentArea.IsHideoutArea)
            {
                return true;
            }


            DateTime time = DateTime.Now;

            selectedSextantMetadata = GetSextantMetadataBySettings(MmmjrBotSettings.Instance.SelectedSextant);

            if(Inventories.AvailableInventorySquares < 2)
            {
                GlobalLog.Warn("No have space in inventory");
                MmmjrBotSettings.Instance.EnableSextantBot = false;
                return true;
            }

            if(sextantBotState == SextantBotState.GetItems)
            {
                await new ClearCursorTask().Run();

                if (!InventoryUi.IsOpened)
                {
                    if (!await Inventories.OpenInventory())
                    {
                        return true;
                    }
                    if (!InventoryUi.IsOpened)
                    {
                        return true;
                    }
                }
                bool result = await GetItemsReadyInInventory();
                if(result == false)
                {
                    MmmjrBotSettings.Instance.EnableSextantBot = false;
                    return true;
                }
                sextantBotState = SextantBotState.RunningRow;
                await Coroutines.CloseBlockingWindows();
                await Wait.ArtificialDelay();
                await Wait.Sleep(100);
                return true;
            }
            
            if(sextantBotState == SextantBotState.RunningRow)
            {
                if(!AtlasUI.IsOpened || !InventoryUi.IsOpened)
                {
                    if (!await Inventories.OpenAtlasUIAndInventory())
                    {
                        return true;
                    }
                    if (!InventoryUi.IsOpened || !AtlasUI.IsOpened)
                    {
                        return true;
                    }
                }

                //check have uncharged compass and sextants
                int sextantCount = await Inventories.CountAllCurrencysByMetadataInInventoryMain(selectedSextantMetadata);
                int compassCount = await Inventories.CountAllCurrencysByMetadataInInventoryMain(CurrencyNames.SurveyorsCompass);

                if(sextantCount == 0 || compassCount == 0)
                {
                    sextantBotState = SextantBotState.GetItems;
                    await Coroutines.CloseBlockingWindows();
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(100);
                    return true;
                }

                //check voidstones
                if (MmmjrBotSettings.Instance.EnableCerimonialVoidstone)
                {
                    if(AtlasUI.CerimonialVoidstone.Item == null)
                    {
                        MmmjrBotSettings.Instance.EnableSextantBot = false;
                        return true;
                    }

                    bool checkResult = await CheckVoidstoneRollAndStore(AtlasUI.CerimonialVoidstone);

                    if(checkResult)
                    {
                        return true;
                    }

                    checkResult = await UseSextantOnVoidStone(selectedSextantMetadata, AtlasUI.CerimonialVoidstone);

                    if (!checkResult)
                    {
                        return false;
                    }
                    

                    //Check Roll
                }
                else if (MmmjrBotSettings.Instance.EnableOmniscientVoidstone)
                {
                    if (AtlasUI.OmniscientVoidstone.Item == null)
                    {
                        MmmjrBotSettings.Instance.EnableSextantBot = false;
                        return true;
                    }
                    bool checkResult = await CheckVoidstoneRollAndStore(AtlasUI.OmniscientVoidstone);

                    if (checkResult)
                    {
                        return true;
                    }

                    checkResult = await UseSextantOnVoidStone(selectedSextantMetadata, AtlasUI.OmniscientVoidstone);

                    if (!checkResult)
                    {
                        return false;
                    }
                }
                else if (MmmjrBotSettings.Instance.EnableGraspingVoidstone)
                {
                    if (AtlasUI.GraspingVoidstone.Item == null)
                    {
                        MmmjrBotSettings.Instance.EnableSextantBot = false;
                        return true;
                    }

                    bool checkResult = await CheckVoidstoneRollAndStore(AtlasUI.GraspingVoidstone);

                    if (checkResult)
                    {
                        return true;
                    }

                    checkResult = await UseSextantOnVoidStone(selectedSextantMetadata, AtlasUI.GraspingVoidstone);

                    if (!checkResult)
                    {
                        return false;
                    }
                }
                else if (MmmjrBotSettings.Instance.EnableDecayedVoidstone)
                {
                    if (AtlasUI.DecayedVoidstone.Item == null)
                    {
                        MmmjrBotSettings.Instance.EnableSextantBot = false;
                        return true;
                    }

                    bool checkResult = await CheckVoidstoneRollAndStore(AtlasUI.DecayedVoidstone);

                    if (checkResult)
                    {
                        return true;
                    }

                    checkResult = await UseSextantOnVoidStone(selectedSextantMetadata, AtlasUI.DecayedVoidstone);

                    if (!checkResult)
                    {
                        return false;
                    }
                }
            }

            //Estamos com os items prontos no inventario.
            await Wait.ArtificialDelay();
            await Wait.Sleep(70);
            return true;
        }

		public void Tick()
		{
            if (!MmmjrBotSettings.Instance.EnableSextantBot)
            {
                return;
            }

            if (!ScanInterval.Elapsed)
            {
                return;
            }
        }

        public async Task<bool> UseSextantOnVoidStone(string _sextantMeta, AtlasUI.Voidstone voidstone)
        {
            int voidx = 0, voidy = 0;
            AtlasUI.Voidstone.ViewItemMoveResult vresult = voidstone.ViewItem();
            if (vresult != AtlasUI.Voidstone.ViewItemMoveResult.None)
            {
                return false;
            }
            LokiPoe.ProcessHookManager.ReadCursorPos(out voidx, out voidy, out var _);

            var sextantItem = Inventories.InventoryItems.Where(i => i.Name == _sextantMeta).OrderBy(i => i.StackCount).FirstOrDefault();

            if (sextantItem != null)
            {
                UseItemResult uresult = InventoryUi.InventoryControl_Main.UseItem(sextantItem.LocalId);
                if (uresult != UseItemResult.None)
                {
                    return false;
                }

                vresult = voidstone.ViewItem();
                if (vresult != AtlasUI.Voidstone.ViewItemMoveResult.None)
                {
                    return false;
                }

                LokiPoe.ProcessHookManager.ReadCursorPos(out voidx, out voidy, out var _);

                LokiPoe.Input.ClickLMB(voidx, voidy);
                await Wait.ArtificialDelay();
                await CheckVoidstoneRollAndStore(voidstone);
                
                return true;
            }

            await Wait.ArtificialDelay();
            return false;
        }

        public async Task<bool> CheckVoidstoneRollAndStore(AtlasUI.Voidstone _voidstone)
        {
            int voidx = 0, voidy = 0;
            AtlasUI.Voidstone.ViewItemMoveResult vresult = _voidstone.ViewItem();
            if (vresult != AtlasUI.Voidstone.ViewItemMoveResult.None)
            {
                return true;
            }

            LokiPoe.ProcessHookManager.ReadCursorPos(out voidx, out voidy, out var _);
            if (_voidstone.Item.LocalStats.ContainsKey(StatTypeGGG.SextantUsesRemaining))
            {
                var selectedRowsByUser = MmmjrBotSettings.Instance.GetAllEnabledMods();
                bool storeRow = true;
                if (selectedRowsByUser.Count == 0) { storeRow = false; return true; }
                foreach (SextantModSelectorClass i in selectedRowsByUser)
                {
                    storeRow = true;

                    if (i.ModStatGGG1 != StatTypeGGG.None)
                    {
                        if (!_voidstone.Item.Stats.ContainsKey(i.ModStatGGG1))
                        {
                            storeRow = false;
                        }
                        else
                        {
                            if (i.Value1 > 0)
                            {
                                int v = 0;
                                if (_voidstone.Item.Stats.TryGetValue(i.ModStatGGG1, out v))
                                {
                                    if (v != i.Value1)
                                    {
                                        storeRow = false;
                                    }
                                }
                            }
                        }
                    }
                    if (i.ModStatGGG2 != StatTypeGGG.None)
                    {
                        if (!_voidstone.Item.Stats.ContainsKey(i.ModStatGGG2))
                        {
                            storeRow = false;
                        }
                        else
                        {
                            if (i.Value2 > 0)
                            {
                                int v = 0;
                                if (_voidstone.Item.Stats.TryGetValue(i.ModStatGGG2, out v))
                                {
                                    if (v != i.Value2)
                                    {
                                        storeRow = false;
                                    }
                                }
                            }
                        }
                    }
                    if (i.ModStatGGG3 != StatTypeGGG.None)
                    {
                        if (!_voidstone.Item.Stats.ContainsKey(i.ModStatGGG3))
                        {
                            storeRow = false;
                        }
                        else
                        {
                            if (i.Value3 > 0)
                            {
                                int v = 0;
                                if (_voidstone.Item.Stats.TryGetValue(i.ModStatGGG3, out v))
                                {
                                    if (v != i.Value3)
                                    {
                                        storeRow = false;
                                    }
                                }
                            }
                        }
                    }
                    if (i.ModStatGGG4 != StatTypeGGG.None)
                    {
                        if (!_voidstone.Item.Stats.ContainsKey(i.ModStatGGG4))
                        {
                            storeRow = false;
                        }
                        else
                        {
                            if (i.Value4 > 0)
                            {
                                int v = 0;
                                if (_voidstone.Item.Stats.TryGetValue(i.ModStatGGG4, out v))
                                {
                                    if (v != i.Value4)
                                    {
                                        storeRow = false;
                                    }
                                }
                            }
                        }
                    }
                    if (i.ModStatGGG5 != StatTypeGGG.None)
                    {
                        if (!_voidstone.Item.Stats.ContainsKey(i.ModStatGGG5))
                        {
                            storeRow = false;
                        }
                        else
                        {
                            if (i.Value5 > 0)
                            {
                                int v = 0;
                                if (_voidstone.Item.Stats.TryGetValue(i.ModStatGGG5, out v))
                                {
                                    if (v != i.Value5)
                                    {
                                        storeRow = false;
                                    }
                                }
                            }
                        }
                    }

                    if (storeRow)
                    {
                        var compassItem = Inventories.InventoryItems.Where(it => it.Name == CurrencyNames.SurveyorsCompass).OrderBy(it => it.StackCount).FirstOrDefault();
                        if (compassItem == null)
                        {
                            return true;
                        }

                        UseItemResult uresult = InventoryUi.InventoryControl_Main.UseItem(compassItem.LocalId);
                        if (uresult != UseItemResult.None)
                        {
                            return true;
                        }

                        if (Cursor.Mode == LokiPoe.InGameState.CursorItemModes.VirtualUse || Cursor.Mode == LokiPoe.InGameState.CursorItemModes.VirtualMove)
                        {
                            vresult = _voidstone.ViewItem();
                            if (vresult != AtlasUI.Voidstone.ViewItemMoveResult.None)
                            {
                                return false;
                            }

                            LokiPoe.ProcessHookManager.ReadCursorPos(out voidx, out voidy, out var _);

                            LokiPoe.Input.ClickLMB(voidx, voidy);
                            await Wait.ArtificialDelay();

                            int col, row;
                            if (!LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.CanFitItem(LokiPoe.InGameState.CursorItemOverlay.ItemSize, out col, out row))
                            {
                                GlobalLog.Error("[ClearCursorTask] There is no space in main inventory. Now stopping the bot because it cannot continue.");
                                BotManager.Stop();
                                return true;
                            }

                            if (!await LokiPoe.InGameState.InventoryUi.InventoryControl_Main.PlaceItemFromCursor(new Vector2i(col, row)))
                                ErrorManager.ReportError();

                        }
                        await Wait.ArtificialDelay();
                        return false;
                    }
                }
                
            }
            await Wait.ArtificialDelay();
            return false;
        }

        public async Task<bool> GetItemsReadyInInventory()
        {
            FastMoveResult fresult = FastMoveResult.Failed;
            int cursor_x, cursor_y;
            int sextantItemId = 0;
            InventoryControlWrapper sextantInventoryControl = null;
            Item sextantItem = Inventories.InventoryItems.Where(i => i.Name == selectedSextantMetadata).OrderBy(i => i.StackCount).FirstOrDefault();
            int sextantCount = await Inventories.CountAllCurrencysByMetadataInInventoryMain(selectedSextantMetadata);
            int minSextants = int.Parse(MmmjrBotSettings.Instance.MinSextantsInInventory);
            if (minSextants < 1 || minSextants > 5000)
            {
                minSextants = 1;
            }
            int minCompass = int.Parse(MmmjrBotSettings.Instance.MinCompassInInventory);
            if (minCompass < 1 || minSextants > 5000)
            {
                minCompass = 1;
            }

            if (sextantItem == null || sextantCount <= minSextants)
            {
                if (!StashUI.IsOpened)
                {
                    if (await Inventories.OpenStash())
                    {
                        while (sextantCount < minSextants)
                        {
                            SearchItemResult searchTabResult = await Inventories.SearchForCurrencyInAllTabs(selectedSextantMetadata);
                            if (searchTabResult.InventoryControl == null)
                            {
                                return false;
                            }
                            sextantItemId = searchTabResult.ResultItem.LocalId;
                            sextantInventoryControl = searchTabResult.InventoryControl;
                            fresult = sextantInventoryControl.FastMove(sextantItemId);

                            if (fresult != FastMoveResult.None)
                            {
                                return false;
                            }

                            LokiPoe.ProcessHookManager.ReadCursorPos(out cursor_x, out cursor_y, out var _);
                            LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, true, false, false);
                            MouseManager.ClickLMB(cursor_x, cursor_y);
                            LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, false, true, false);

                            sextantCount += searchTabResult.ResultItem.StackCount;
                            await Wait.ArtificialDelay();
                            await Wait.Sleep(200);
                        }
                    }
                }
                else
                {
                    while (sextantCount < minSextants)
                    {
                        SearchItemResult searchTabResult = await Inventories.SearchForCurrencyInAllTabs(selectedSextantMetadata);
                        if (searchTabResult.InventoryControl == null)
                        {
                            return false;
                        }
                        sextantItemId = searchTabResult.ResultItem.LocalId;
                        sextantInventoryControl = searchTabResult.InventoryControl;
                        fresult = sextantInventoryControl.FastMove(sextantItemId);

                        if (fresult != FastMoveResult.None)
                        {
                            return false;
                        }

                        LokiPoe.ProcessHookManager.ReadCursorPos(out cursor_x, out cursor_y, out var _);
                        LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, true, false, false);
                        MouseManager.ClickLMB(cursor_x, cursor_y);
                        LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, false, true, false);

                        sextantCount += searchTabResult.ResultItem.StackCount;
                        await Wait.ArtificialDelay();
                        await Wait.Sleep(200);
                    }
                }
            }

            int compassItemId = 0;
            InventoryControlWrapper compassInventoryControl = null;
            Item compassItem = Inventories.InventoryItems.Where(i => i.Name == CurrencyNames.SurveyorsCompass).OrderBy(i => i.StackCount).FirstOrDefault();
            int compassCount = await Inventories.CountAllCurrencysByMetadataInInventoryMain(CurrencyNames.SurveyorsCompass);

            if (compassItem == null || compassCount < minCompass)
            {
                if (!StashUI.IsOpened)
                {
                    if (await Inventories.OpenStash())
                    {
                        while (compassCount < minCompass)
                        {
                            //implementar while pra pegar varias sextants pro inventario
                            SearchItemResult searchTabResult = await Inventories.SearchForCurrencyInAllTabs(CurrencyNames.SurveyorsCompass);
                            if (searchTabResult.InventoryControl == null)
                            {
                                return false;
                            }
                            compassItemId = searchTabResult.ResultItem.LocalId;
                            compassInventoryControl = searchTabResult.InventoryControl;
                            fresult = compassInventoryControl.FastMove(compassItemId);

                            if (fresult != FastMoveResult.None)
                            {
                                return false;
                            }

                            LokiPoe.ProcessHookManager.ReadCursorPos(out cursor_x, out cursor_y, out var _);
                            LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, true, false, false);
                            MouseManager.ClickLMB(cursor_x, cursor_y);
                            LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, false, true, false);
                            compassCount += searchTabResult.ResultItem.StackCount;
                            await Wait.ArtificialDelay();
                            await Wait.Sleep(200);
                        }
                    }
                }
                else
                {
                    while (compassCount < minCompass)
                    {
                        //implementar while pra pegar varias sextants pro inventario
                        SearchItemResult searchTabResult = await Inventories.SearchForCurrencyInAllTabs(CurrencyNames.SurveyorsCompass);
                        if (searchTabResult.InventoryControl == null)
                        {
                            return false;
                        }
                        compassItemId = searchTabResult.ResultItem.LocalId;
                        compassInventoryControl = searchTabResult.InventoryControl;
                        fresult = compassInventoryControl.FastMove(compassItemId);

                        if (fresult != FastMoveResult.None)
                        {
                            return false;
                        }

                        LokiPoe.ProcessHookManager.ReadCursorPos(out cursor_x, out cursor_y, out var _);
                        LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, true, false, false);
                        MouseManager.ClickLMB(cursor_x, cursor_y);
                        LokiPoe.Input.SimulateKeyEvent(Keys.LControlKey, false, true, false);
                        compassCount += searchTabResult.ResultItem.StackCount;
                        await Wait.ArtificialDelay();
                        await Wait.Sleep(200);
                    }
                }
            }
            await Wait.ArtificialDelay();
            await Wait.Sleep(200);
            return true;
        }

        #region Unused interface methods

        public async Task<LogicResult> Logic(Logic logic)
		{
			return LogicResult.Unprovided;
		}

        public MessageResult Message(DreamPoeBot.Loki.Bot.Message message)
        {
            return MessageResult.Unprocessed;
        }

        public string GetSextantMetadataBySettings(int sextantId)
        {
            string selectedSextant = "";
            if (sextantId == 0) selectedSextant = CurrencyNames.SextantMaster;
            else if(sextantId == 1) selectedSextant = CurrencyNames.ElevatedSextant;
            return selectedSextant;
        }

        public void Start()
		{
		}

		public void Stop()
		{
		}

		public string Name => "Sextant Bot";
		public string Description => "Sextant Bot";
		public string Author => "MMMJR";
		public string Version => "1.0";

		#endregion
	}
}
