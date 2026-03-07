using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using MmmjrBot.Lib;
using log4net;

namespace MmmjrBot
{
    class PreCombatFollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int FollowFailCounter = 0;
        
        public string Name { get { return "PreCombatFollowTask"; } }
        public string Description { get { return "This task will keep the bot under a specific distance from the leader, in combat situation."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }


        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            if (!MmmjrBotSettings.Instance.ShouldKill) return false;
            if (!MmmjrBotSettings.Instance.ShouldFollow) return false;
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInHideout)
            {
                return false;
            }

            if (FollowBot.Leader == null)
            {
                return false;
            }

            var distance = FollowBot.Leader.Position.Distance(LokiPoe.Me.Position);

            if (distance > MmmjrBotSettings.Instance.MaxCombatDistance)
            {
                var pos = ExilePather.FastWalkablePositionFor(LokiPoe.Me.Position.GetPointAtDistanceBeforeEnd(
                    FollowBot.Leader.Position,
                    LokiPoe.Random.Next(MmmjrBotSettings.Instance.FollowDistance,
                        MmmjrBotSettings.Instance.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(LokiPoe.Me.Position, pos))
                {

                    return false;
                }

                // Cast Phase run if we have it.
                FollowBot.PhaseRun();

                if (LokiPoe.Me.Position.Distance(pos) < 50)
                    LokiPoe.InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false,
                        pos);
                else
                    Move.Towards(pos, $"{FollowBot.Leader.Name}");
                return true;
            }
            KeyManager.ClearAllKeyStates();
            return false;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}
