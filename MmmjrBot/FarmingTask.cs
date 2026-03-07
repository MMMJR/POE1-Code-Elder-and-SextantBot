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
using DreamPoeBot.Loki.Elements.InventoryElements;
using MmmjrBot.MapperBot;
using System.Diagnostics;
using MmmjrBot.Leagues.Breaches;
using MmmjrBot.Leagues.Abyss;
using DreamPoeBot.Loki.Elements;
using DreamPoeBot.Loki.Bot.Pathfinding;

namespace MmmjrBot
{
    public class SearchTargetResult
    {
        public int Id;
        public WalkablePosition Position;
        public Monster _monsterObject;

        public SearchTargetResult()
        {
            Id = 0;
            Position = null;
            _monsterObject = null;
        }

        public SearchTargetResult(int id, WalkablePosition position, Monster monsterObject)
        {
            Id = id;
            Position = position;
            _monsterObject = monsterObject;
        }
    }
	class FarmingTask : ITask
	{
        private static readonly Stopwatch CombatInterval = Stopwatch.StartNew();
        private static bool AnyMobsNearby => LokiPoe.ObjectManager.Objects.Any<Monster>(m => m.IsActive && m.Distance <= (MmmjrBotSettings.Instance.CombatRange + 10));
        public static bool IsCombatActive;
        public static bool FinalDeath;

        private static CachedTransition FrontTransition
        {
            get
            { 
                return CombatAreaCache.Current.AreaTransitions.ClosestValid(t => t.Type == TransitionType.Local);
            }
        }

        private async Task<bool> EnterTransition(CachedTransition _transition)
        {
            var pos = _transition.Position;
            if (pos.IsFar)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Debug($"[ComplexExplorer] Fail to move to {pos}. Marking this transition as unwalkable.");
                    _transition.Unwalkable = true;
                }
                await Wait.SleepSafe(1200);
            }
            var transitionObj = _transition.Object;
            if (transitionObj == null)
            {
                GlobalLog.Error("[ComplexExplorer] Unknown error. There is no transition near cached position.");
                _transition.Ignored = true;
                return false;
            }

            if (!transitionObj.IsTargetable)
            {
                return false;
            }

            if (!await PlayerAction.TakeTransition(transitionObj))
            {
                return false;
            }
            else
            {
                if (MmmjrBotSettings.Instance.EnableMapperRoutineTriggerCWDT)
                {
                    MmmjrBot.CWDTTrigger = false;
                }
            }
            return true;
        }


        public async Task<bool> Run()
		{
            if(!MmmjrBotSettings.Instance.EnableFarmingBot)
            {
                return true;
            }

            /*GlobalLog.Info("===============MapInfo:");
            GlobalLog.Info("Map Name: " + World.CurrentArea.Name);
            GlobalLog.Info("Map Id: " + World.CurrentArea.Id);
            GlobalLog.Info("Map Index: " + World.CurrentArea.Index);
            GlobalLog.Info("Map PTr: " + World.CurrentArea.IntPtr_0);
            GlobalLog.Info("===============:");
            GlobalLog.Info("===============LocalData:");
            GlobalLog.Info("MyId: " + LokiPoe.LocalData.MyId);
            GlobalLog.Info("MyPosition: " + LokiPoe.LocalData.MyPosition);
            GlobalLog.Info("MyWorldPosition: " + LokiPoe.LocalData.MyWorldPosition);
            GlobalLog.Info("MePtr: " + LokiPoe.LocalData.MePtr);
            GlobalLog.Info("MyReaction: " + LokiPoe.LocalData.MyReaction);
            GlobalLog.Info("AreaHash: " + LokiPoe.LocalData.AreaHash);
            GlobalLog.Info("===============:");
            GlobalLog.Info("===============Terrain Data:");
            GlobalLog.Info("Terrain Data Size: " + LokiPoe.TerrainData.Size);
            GlobalLog.Info("Terrain Data Size Nav: " + LokiPoe.TerrainData.SizeInNavCells);
            GlobalLog.Info("Cache AreaId: " + LokiPoe.TerrainData.Cache.AreaId);
            GlobalLog.Info("Cache BPR: " + LokiPoe.TerrainData.Cache.BPR);
            GlobalLog.Info("Cache Value: " + LokiPoe.TerrainData.Cache.Value);*/


            if(MmmjrBot.mapbot_state == MapperBotState.GetItems)
            {
                GlobalLog.Info("Geting Itens");
                IsCombatActive = false;
                if (!World.CurrentArea.IsHideoutArea)
                {
                    if(World.CurrentArea.IsMap)
                    {
                        IsCombatActive = false;
                        MmmjrBot.mapbot_state = MapperBotState.Mapping;
                        await Wait.ArtificialDelay();
                        await Wait.Sleep(70);
                        return true;
                    }
                    await TravelToHideoutTaskStatic.Run();
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }

                if (!await TakeMap.Run())
                {
                    MmmjrBotSettings.Instance.EnableFarmingBot = false;
                    return true;
                }

                MmmjrBot.mapbot_state = MapperBotState.OpeningMap;
                GlobalLog.Info("Geting Itens complete");
                await Wait.ArtificialDelay();
                await Wait.Sleep(70);
                return true;
            }
            else if(MmmjrBot.mapbot_state == MapperBotState.OpeningMap)
            {
                GlobalLog.Info("Opening Map");
                IsCombatActive = false;
                if (!World.CurrentArea.IsHideoutArea)
                {
                    await TravelToHideoutTaskStatic.Run();
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }

                if(!await OpenMapTask.Run())
                {
                    MmmjrBot.mapbot_state = MapperBotState.GetItems;
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }
                GlobalLog.Info("Opening Map complete");
                IsCombatActive = false;
                MmmjrBot.mapbot_state = MapperBotState.Mapping;
            }
            else if(MmmjrBot.mapbot_state == MapperBotState.Mapping)
            {
                GlobalLog.Info("Mapping");
                if (LokiPoe.Me.IsDead) return true;

                //Tick();

                if (MapExplorationTask.MapCompleted && !LokiPoe.Me.IsDead)
                {
                    if (MmmjrBotSettings.Instance.SingleUseElderFragments)
                    {
                        if (World.CurrentArea.IsHideoutArea && FinalDeath && World.CurrentArea.IsHideoutArea)
                        {
                            bool ret = false;
                            FinalDeath = false;
                            for (int x = 0; x < 6; x++)
                            {
                                ret = await DeviceAreaTask.EnterMapPortal();
                                if (ret)
                                {
                                    break;
                                }
                                if (World.CurrentArea.IsMap)
                                {
                                    ret = true;
                                    break;
                                }
                                await Wait.ArtificialDelay();
                                await Wait.Sleep(500);
                            }
                            if (!ret)
                            {
                                MmmjrBot.mapbot_state = MapperBotState.StoringItems;
                                DeviceAreaTask._toMap = false;
                                return true;
                            }
                            CachedTransition _transition2 = FrontTransition;
                            GlobalLog.Info("transiction : " + FrontTransition);
                            if (_transition2 != null)
                            {
                                await EnterTransition(_transition2);
                                await Wait.SleepSafe(1000);


                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(1000);
                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(500);
                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(200);
                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(200);
                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.SleepSafe(250);
                                await Wait.ArtificialDelay();

                                CachedTransition _transition3 = FrontTransition;
                                GlobalLog.Info("transiction : " + FrontTransition);
                                if (_transition3 != null)
                                {
                                    await EnterTransition(_transition3);
                                    Portal portal = null;

                                    if (await Wait.For(() => (portal = PlayerAction.PortalInRangeOf2(100)) != null, "Searching for portals to hideout"))
                                    {
                                        if (await PlayerAction.TakePortal(portal))
                                        {
                                            if (await Wait.For(() => (World.CurrentArea.IsHideoutArea), "Entering HO"))
                                            {
                                                GlobalLog.Info("Map Complete? Leaving the area...");
                                                MmmjrBot.mapbot_state = MapperBotState.StoringItems;
                                                DeviceAreaTask._toMap = false;
                                                return true;
                                            }
                                        }
                                    }
                                }

                            }
                        }
                        else
                        {
                            Vector2i escapePos = new Vector2i(253, 203);
                            if (ExilePather.PathExistsBetween(LokiPoe.MyPosition, escapePos))
                            {
                                GlobalLog.Warn("My Position: " + LokiPoe.MyPosition + "Escape Pos: " + escapePos);


                                WalkablePosition escapeWalkAblePosition = new WalkablePosition("Zana Escape", escapePos, 20, 100);

                                await Wait.SleepSafe(1050);

                                escapeWalkAblePosition.TryCome();
                                await Wait.SleepSafe(250);
                                escapeWalkAblePosition.TryCome();
                                await Wait.SleepSafe(250);
                                escapeWalkAblePosition.TryCome();
                                await Wait.SleepSafe(250);

                                await Wait.SleepSafe(150);
                                await Wait.SleepSafe(12500);


                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(2000);
                                while (await LootItemTaskStatic.Run())
                                {
                                    await Wait.SleepSafe(250);
                                    await Wait.ArtificialDelay();
                                }
                                await Wait.Sleep(1000);


                                CachedTransition _transition = FrontTransition;
                                GlobalLog.Info("transiction : " + FrontTransition);
                                if (_transition != null)
                                {
                                    await EnterTransition(_transition);
                                    Portal portal = null;

                                    if (await Wait.For(() => (portal = PlayerAction.PortalInRangeOf2(100)) != null, "Searching for portals to hideout"))
                                    {
                                        if (await PlayerAction.TakePortal(portal))
                                        {
                                            if (await Wait.For(() => (World.CurrentArea.IsHideoutArea), "Entering HO"))
                                            {
                                                GlobalLog.Info("Map Complete? Leaving the area...");
                                                MmmjrBot.mapbot_state = MapperBotState.StoringItems;
                                                DeviceAreaTask._toMap = false;
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //253 // 203
                        
                    }

                    if (LokiPoe.Me.IsDead) return true;
                    GlobalLog.Info("Map Complete? Leaving the area...");
                    MmmjrBot.mapbot_state = MapperBotState.LeavingArea;
                    DeviceAreaTask._toMap = false;
                    return true;
                }

                if (MmmjrBotSettings.Instance.EnableMapperRoutineTriggerCWDT)
                {
                    if (!MmmjrBot.CWDTTrigger)
                    {
                            for (int x = 1; x < 5; x++)
                            {
                                var thisflask = LokiPoe.InGameState.QuickFlaskHud.InventoryControl.Inventory.Items.FirstOrDefault(f => f.LocationTopLeft.X == x - 1);
                                DefenseAndFlaskStatic.UseFlask(thisflask, x);
                                await Wait.SleepSafe(40);
                            }

                        LokiPoe.Input.SimulateKeyEvent(Keys.X, true, false, true, Keys.X);
                        await Wait.Sleep(30);
                        LokiPoe.Input.SimulateKeyEvent(Keys.X, true, false, true, Keys.X);


                        MmmjrBot.CWDTTrigger = true;
                    }
                }

                /*if (!MmmjrBotSettings.Instance.EnableMapperCustomRoutine)
                {
                    if (IsCombatActive)
                    {
                        await KillBossTask.Run();
                        GlobalLog.Info("Mapping2");
                        return true;
                    }
                    if (AnyMobsNearby && !MmmjrBotSettings.Instance.SingleUseElderFragments && !MmmjrBotSettings.Instance.SingleUseShaperFragments)
                    {
                        GlobalLog.Info("Mapping2");
                        return true;
                    }
                }*/
                GlobalLog.Info("Mapping33");
                

                GlobalLog.Info("Mapping444");
                if (!await DeviceAreaTask.Run())
                {
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(50);
                    return true;
                }
                GlobalLog.Info("Mapping555");
                await LevelGemsTaskStatic.Run();
                //await CastAuraTaskStatic.Run();
                await LootItemTaskStatic.Run();

                await HandleBlockingChestsTaskStatic.Run();
                await HandleBlockingObjectTaskStatic.Run();

                
                await SpecialObjectTaskStatic.Run();
                if (World.CurrentArea.IsMap)
                {

                    await KillBossTask.Run();
                    if(await CombatSystem())
                    {
                        return true;
                    }
                }

                GlobalLog.Info("Mapping666");

                if (MmmjrBotSettings.Instance.EnabledBreachs)
                {
                    await HandleBreachesTask.Run();
                }
                if(MmmjrBotSettings.Instance.EnabledAbyss)
                {
                    await Abyss.Run();
                }
                GlobalLog.Info("Mapping777");
                if (!await OpenChestTaskStatic.Run())
                {
                    GlobalLog.Info("Mapping888");
                    if (!await LootItemTaskStatic.Run())
                    {
                        if (!MmmjrBotSettings.Instance.SingleUseElderFragments)
                        {
                            await TransitionTriggerTaskStatic.Run();
                        }
                            GlobalLog.Info("Mapping999");
                            if (!await MapExplorationTask.Run())
                            {
                                await TravelToHideoutTaskStatic.Run();
                                await Wait.ArtificialDelay();
                                await Wait.Sleep(70);
                                return true;
                            }
                    }
                }
                GlobalLog.Info("Mapping");
                if (!MmmjrBotSettings.Instance.SingleUseElderFragments)
                    await TrackMob.Execute(80);
                await DefenseAndFlaskStatic.Run();
                await LootItemTaskStatic.Run();

                return true;
            }
            else if(MmmjrBot.mapbot_state == MapperBotState.LeavingArea)
            {
                if(await PlayerAction.TryTo(() => TravelToHideoutTaskStatic.Run(),"Leaving Area", 6))
                {
                    MmmjrBot.mapbot_state = MapperBotState.StoringItems;
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }
                if(await LeaveAreaTaskStatic.Run())
                {
                    MmmjrBot.mapbot_state = MapperBotState.StoringItems;
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }
                return true;
            }
            else if(MmmjrBot.mapbot_state == MapperBotState.StoringItems)
            {
                if (!World.CurrentArea.IsHideoutArea)
                {
                    await TravelToHideoutTaskStatic.Run();
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(70);
                    return true;
                }
                await StashTaskStatic.Run();
                MmmjrBot.mapbot_state = MapperBotState.OpeningMap;
                return true;
            }

            await Wait.ArtificialDelay();
            await Wait.Sleep(70);
            return true;
        }

		public async void Tick()
		{
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
                return;

            SpecialObjectTaskStatic.Tick();
            OpenChestTaskStatic.Tick();
            KillBossTask.Tick();

            if (MmmjrBotSettings.Instance.EnableExileRoutine)
                ExileRoutine.Tick();

            if (MmmjrBotSettings.Instance.EnabledBreachs)
                HandleBreachesTask.Tick();

            if (MmmjrBotSettings.Instance.EnabledAbyss)
                Abyss.Tick();

            MapExplorationTask.Tick();

            if(!MmmjrBotSettings.Instance.EnableSingleMap)
            {
                await StuckDetection.Tick();
            }
            
            
            return;
        }

        public static async Task<bool> CombatSystem()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
                return false;

            await DefenseAndFlaskStatic.Run();
            if (MmmjrBotSettings.Instance.EnableMapperRoutine)
            {
                var res = await MmmjrRoutine.RunCombat();
                return res == LogicResult.Provided; 
            }
            else if (MmmjrBotSettings.Instance.EnableMapperCustomRoutine)
            {
                await CombatTaskStatic.Execute();
                //loot
            }
            else if (MmmjrBotSettings.Instance.EnableExileRoutine)
            {
                await ExileRoutine.RunCombat();
            }

            return false;
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

        public void Start()
		{
            MmmjrBot.mapbot_state = MapperBotState.OpeningMap;
            FinalDeath = false;
            StashTaskStatic.Start();
            HandleBreachesTask.Start();
		}

		public void Stop()
		{

		}

		public string Name => "Farming Bot";
		public string Description => "Farming Bot";
		public string Author => "MMMJR";
		public string Version => "1.0";

		#endregion
	}
}
