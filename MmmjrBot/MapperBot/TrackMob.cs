using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState.GuildStashUi.EssenceTab;

namespace MmmjrBot.MapperBot
{
    public static class TrackMob
    {
        private const int MaxKillAttempts = 20;
        private static readonly Interval LogInterval = new Interval(1000);

        public static CachedObject CurrentTarget;

        static TrackMob()
        {
            Events.AreaChanged += (sender, args) => CurrentTarget = null;
        }

        internal static void RestrictRange()
        {
            TrackMobLogic.CurrentTarget = null;
        }

        public static SearchTargetResult GetTarget()
        {
            if (CurrentTarget == null)
                return null;

            var m = CurrentTarget.Object as Monster;

            if (m == null) return null;

            SearchTargetResult result = new SearchTargetResult(CurrentTarget.Id, CurrentTarget.Position, m);

            return result;
        }

        public static async Task<bool> Execute(int range)
        {
            var cachedMonsters = CombatAreaCache.Current.Monsters;

            if (CurrentTarget == null)
            {
                CurrentTarget = range == -1
                    ? cachedMonsters.ClosestValid()
                    : cachedMonsters.ClosestValid(m => m.Position.Distance <= range);

                if (CurrentTarget == null)
                    return false;
            }

            if (Blacklist.Contains(CurrentTarget.Id))
            {
                CurrentTarget.Ignored = true;
                CurrentTarget = null;
                return false;
            }

            var pos = CurrentTarget.Position;
            Vector2i newpos = pos.AsVector;

            if(!ExilePather.IsWalkable(newpos))
            {
                newpos = pos;
            }
            if (pos.IsFar || pos.IsFarByPath)
            {
                if (!PlayerMoverManager.MoveTowards(newpos))
                {
                    CurrentTarget.Unwalkable = true;
                    CurrentTarget = null;
                    return false;
                }
                return true;
            }

            var monsterObj = CurrentTarget.Object as Monster;

            // Untested fix to not wait on a captured beast. Will be changed once confirmed issue is solved.
            //if (monsterObj == null || monsterObj.IsDead || (Loki.Game.LokiPoe.InstanceInfo.Bestiary.IsActive && (monsterObj.HasBestiaryCapturedAura || monsterObj.HasBestiaryDisappearingAura)))

            if (monsterObj == null || monsterObj.IsDead || (LokiPoe.InstanceInfo.Bestiary.IsActive && (monsterObj.HasBestiaryCapturedAura || monsterObj.HasBestiaryDisappearingAura)))
            {
                cachedMonsters.Remove(CurrentTarget);
                CurrentTarget = null;
                return false;
            }
            else
            {

                var attempts = ++CurrentTarget.InteractionAttempts;
                if (attempts > MaxKillAttempts)
                {
                    GlobalLog.Error("[TrackMob] All attempts to kill current monster have been spent. Now ignoring it.");
                    CurrentTarget.Ignored = true;
                    CurrentTarget = null;
                    return false;
                }
                GlobalLog.Debug($"[TrackMob] Alive monster is nearby, this is our {attempts}/{MaxKillAttempts} attempt to kill it.");
                await DreamPoeBot.Loki.Coroutine.Coroutine.Sleep(200);
            }
            return true;
        }
    }
}
