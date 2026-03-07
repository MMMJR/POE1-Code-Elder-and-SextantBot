using MmmjrBot.Lib;
using System.Threading.Tasks;


namespace MmmjrBot.Leagues.Abyss
{
    public static class FollowAbyssTask
    {
        private const int MaxAttempts = 15;

        public static async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            var mapIconOwner = Abyss.CachedData.MapIconOwner;

            if (mapIconOwner == null || mapIconOwner.Unwalkable || mapIconOwner.Ignored)
                return false;

            var pos = mapIconOwner.Position;
            if (pos.Distance > 10 || pos.PathDistance > 10)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error($"[FollowAbyssTask] Fail to move to {pos}. Current abyss map icon owner is unwalkable.");
                    mapIconOwner.Unwalkable = true;
                }
                return true;
            }
            var attempts = ++mapIconOwner.InteractionAttempts;
            if (attempts > MaxAttempts)
            {
                GlobalLog.Error("[FollowAbyssTask] Abyss map icon owner change timeout. Now ignoring it.");
                mapIconOwner.Ignored = true;
                return true;
            }
            GlobalLog.Debug($"[FollowAbyssTask] Waiting for abyss map icon owner change ({attempts}/{MaxAttempts})");
            await Wait.Sleep(200);
            return true;
        }
    }
}