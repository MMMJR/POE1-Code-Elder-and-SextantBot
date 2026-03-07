using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using System.Threading.Tasks;

namespace MmmjrBot.Leagues.Abyss
{
    public static class OpenAbyssChestTask
    {
        private const int MaxAttempts = 4;

        internal static CachedObject AbyssChest;

        public static async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            if (AbyssChest == null)
            {
                if ((AbyssChest = Abyss.CachedData.Chests.ClosestValid()) == null)
                    return false;
            }

            var pos = AbyssChest.Position;
            if (pos.IsFar || pos.IsFarByPath)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error($"[OpenAbyssChest] Fail to move to {pos}. Abyssal Trove position is unwalkable.");
                    AbyssChest.Unwalkable = true;
                    AbyssChest = null;
                }
                return true;
            }
            var chestObj = AbyssChest.Object as Chest;
            if (chestObj == null)
            {
                GlobalLog.Error("[OpenAbyssChest] Unexpected error. Chest object is null.");
                AbyssChest.Ignored = true;
                AbyssChest = null;
                return true;
            }
            if (!chestObj.IsTargetable)
            {
                GlobalLog.Debug("[OpenAbyssChest] Waiting for Abyssal Trove activation.");
                await Wait.Sleep(500);
                return true;
            }
            var attempts = ++AbyssChest.InteractionAttempts;
            if (attempts > MaxAttempts)
            {
                GlobalLog.Error($"[OpenAbyssChest] All attempts to interact with {pos.Name} have been spent. Now ignoring it.");
                AbyssChest.Ignored = true;
                AbyssChest = null;
                return true;
            }
            if (await PlayerAction.Interact(chestObj, () => chestObj.Fresh().IsOpened, "Abyssal Trove opening", 1500))
            {
                Abyss.CachedData.Chests.Remove(AbyssChest);
                AbyssChest = null;
                await Wait.Sleep(1500);
                BotStructure.TaskManager.GetTaskByName("LootItemTask")?.Message(new Message("ResetCurrentItem"));
            }
            else
            {
                await Wait.StuckDetectionSleep(500);
            }
            return true;
        }
    }
}