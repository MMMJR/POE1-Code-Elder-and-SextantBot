using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Controllers;
using MmmjrBot.Lib.CachedObjects;
using MmmjrBot.Lib.Global;
using MmmjrBot.Lib.Positions;
using System.Text;
using MmmjrBot.MapperBot;

namespace MmmjrBot.Lib.CommonTasks
{
	public static class LootItemTaskStatic
	{
		private const int MaxItemPickupAttempts = 200;
		private static readonly Interval LogInterval = new Interval(1000);

        private static CachedWorldItem _item;

        public static async Task<bool> Run()
        {
            GlobalLog.Info("KKKKK");
            List<CachedWorldItem> items = CombatAreaCache.Current.Items;
            List<CachedWorldItem> validItems = new List<CachedWorldItem>();
            GlobalLog.Info("KKKKK2");
            if (LokiPoe.Me.IsDead) return false;
            GlobalLog.Info("KKKKK3");
            var allItems = items;
            if (allItems.Count == 0)
            {
                GlobalLog.Info("KKKKK4");
                return false;
            }
            GlobalLog.Info("KKKKK5");
            if (_item == null)
			{
                GlobalLog.Info("KKKKK6");
                _item = allItems.OrderBy(i => i.Position.DistanceSqr).First();
			}
            GlobalLog.Info("KKKKK7");
            if (!CanFit(_item.Size, Inventories.AvailableInventorySquares))
            {
                GlobalLog.Info("KKKKK8");
                GlobalLog.Warn($"[LootItemTask] No room in inventory for {_item.Position.Name}");
                _item.Ignored = true;
                _item = null;
                return false;
            }
            GlobalLog.Info("KKKKK9");
            await Coroutines.CloseBlockingWindows();
            GlobalLog.Info("KKKKK10");
            WalkablePosition pos = _item.Position;
            GlobalLog.Info("KKKKK11");
            if (pos.Distance > 30 || pos.PathDistance > 34)
			{
                GlobalLog.Info("KKKKK12");
                if (LogInterval.Elapsed)
				{
					GlobalLog.Debug($"[LootItemTask] Items to pick up: {validItems.Count}");
					GlobalLog.Debug($"[LootItemTask] Moving to {pos}");
				}

                // Cast Phase run if we have it.
                //FollowBot.PhaseRun();
                GlobalLog.Info("KKKKK13");
                if (!PlayerMoverManager.MoveTowards(pos))
				{
					if (_item.Object != null)
					{
						GlobalLog.Error($"[LootItemTask] Fail to move to {pos}. Marking this item as unwalkable.");
						_item.Unwalkable = true;
						_item = null;
					}
					else
					{
						GlobalLog.Error($"[LootItemTask] Fail to move to {pos}. item Object is null, removing item from the cache and reevaluating it.");
						CombatAreaCache.Current.RemoveItemFromCache(_item);
						_item = null;
					}
				}
                return true;
			}
			WorldItem itemObj = _item.Object;
			if (itemObj == null)
			{
				items.Remove(_item);
				_item = null;
				return true;
			}
            GlobalLog.Info("KKKKK14");
            await PlayerAction.EnableAlwaysHighlight();
            GlobalLog.Info("KKKKK15");
            if (!itemObj.HasVisibleHighlightLabel)
            {
                return true;
            }
            GlobalLog.Info("KKKKK16");
            GlobalLog.Debug($"[LootItemTask] Now picking up {pos}");
            GlobalLog.Info("KKKKK17");
            CachedItem cached = new CachedItem(itemObj.Item);

			int minTimeout = 400;
			int timeout = Math.Max(LatencyTracker.Average * 2, minTimeout);

			LokiPoe.ProcessHookManager.ClearAllKeyStates();
            GlobalLog.Info("KKKKK18");
            if (await FastInteraction(itemObj))
            {
                //await Coroutines.LatencyWait();
                if (await Wait.For(() => _item.Object == null, "item pick up", 5, timeout))
                {
                    items.Remove(_item);
                    _item = null;
                    GlobalLog.Info($"[Events] Item looted ({cached.Name})");
                }
            }
            GlobalLog.Info("KKKKK19");
            return true;
        }

        private static async Task<bool> FastInteraction(WorldItem item)
        {
            if (item == null) return false;
            var label = item.WorldItemLabel;
            //if (label.Coordinate.X < LokiPoe.ClientWindowInfo.Client.Left ||
            //    label.Coordinate.Y < LokiPoe.ClientWindowInfo.Client.Top) return false;

            //if (label.Coordinate.X + label.Size.X > LokiPoe.ClientWindowInfo.Client.Right ||
            //    label.Coordinate.Y + label.Size.Y > LokiPoe.ClientWindowInfo.Client.Bottom * 0.85) return false;
            var found = false;
            var point = Vector2i.Zero;
            bool useHighlight = false;
            bool useBound = false;
			if (LokiPoe.Input.Binding.KeyPickup == LokiPoe.ConfigManager.KeyPickupType.UseHighlightKey)
            {
                //GlobalLog.Info($"[FastInteraction] pressing UseHighlightKey Key [{LokiPoe.Input.Binding.highlight_combo.Modifier} + {LokiPoe.Input.Binding.highlight_combo.Key}]");
				LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.highlight_combo.Key, -32768, LokiPoe.Input.Binding.highlight_combo.Modifier);
                useHighlight = true;
                await Wait.SleepSafe(15);
            }
            if (LokiPoe.Input.Binding.KeyPickup == LokiPoe.ConfigManager.KeyPickupType.UseBoundKey)
            {
                //GlobalLog.Info($"[FastInteraction] pressing UseBoundKey [{LokiPoe.Input.Binding.enable_key_pickup_combo.Modifier} + {LokiPoe.Input.Binding.enable_key_pickup_combo.Key}]");
				LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.enable_key_pickup_combo.Key, -32768, LokiPoe.Input.Binding.enable_key_pickup_combo.Modifier);
                useBound = true;
				await Wait.SleepSafe(15);
			}

			for (int i = 1; i < 6; i++)
            {
                point = new Vector2i((int)(label.Coordinate.X + (label.Size.X / 7) * i), (int)(label.Coordinate.Y + (label.Size.Y / 2)));
                MouseManager.SetMousePosition(point, false);
                await Wait.SleepSafe(15);
				if (GameController.Instance.Game.IngameState.FrameUnderCursor == item.Entity.Address)
                {
					found = true;
                    break;
                }
            }

            if (!found)
            {
				if (useHighlight)
                {
                    LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.highlight_combo.Key, 0, LokiPoe.Input.Binding.highlight_combo.Modifier);
                    await Wait.SleepSafe(15);
				}

                if (useBound)
                {
                    LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.enable_key_pickup_combo.Key, 0, LokiPoe.Input.Binding.enable_key_pickup_combo.Modifier);
                    await Wait.SleepSafe(15);
				}
				return false;
            }
            MouseManager.ClickLMB(point.X, point.Y);
			await Wait.SleepSafe(15, 25);
			if (useHighlight)
            {
                LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.highlight_combo.Key, 0, LokiPoe.Input.Binding.highlight_combo.Modifier);
                await Wait.SleepSafe(15);
			}

            if (useBound)
            {
                LokiPoe.ProcessHookManager.SetKeyState(LokiPoe.Input.Binding.enable_key_pickup_combo.Key, 0, LokiPoe.Input.Binding.enable_key_pickup_combo.Modifier);
                await Wait.SleepSafe(15);
			}
            return true;
		}
		private static async Task<bool> MoveAway(int min, int max)
		{
			WorldPosition pos = WorldPosition.FindPathablePositionAtDistance(min, max, 5);
			if (pos == null)
			{
				GlobalLog.Debug("[LootItemTask] Fail to find any pathable position at distance.");
				return false;
			}
			await Move.AtOnce(pos, "distant position", 10);
			return true;
		}

		private static bool CanFit(Vector2i size, int availableSquares)
		{
            return LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Main).CanFitItem(size);
		}
	}
}
