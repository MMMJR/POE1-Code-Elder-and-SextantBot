using MmmjrBot.Lib;
using System.Threading.Tasks;

namespace MmmjrBot.Leagues.Abyss
{
    public static class StartAbyssTask
    {
        private const int MaxAttempts = 15;

        internal static CachedObject StartNode;

        public static async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            if (StartNode == null)
            {
                if ((StartNode = Abyss.CachedData.StartNodes.ClosestValid()) == null)
                    return false;
            }

            var pos = StartNode.Position;
            if (pos.Distance > 10 || pos.PathDistance > 10)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error($"[StartAbyssTask] Fail to move to {pos}. Abyss start node is unwalkable.");
                    StartNode.Unwalkable = true;
                    StartNode = null;
                }
                return true;
            }

            var attempts = ++StartNode.InteractionAttempts;
            if (attempts > MaxAttempts)
            {
                GlobalLog.Error("[StartAbyssTask] Abyss start node activation timeout. Now ignoring it.");
                StartNode.Ignored = true;
                StartNode = null;
                return true;
            }
            GlobalLog.Debug($"[StartAbyssTask] Waiting for Abyss start node activation ({attempts}/{MaxAttempts})");
            await Wait.Sleep(200);
            return true;
        }
    }
}