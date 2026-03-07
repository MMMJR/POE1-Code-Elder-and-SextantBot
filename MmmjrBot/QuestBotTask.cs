using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using log4net;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Coroutine;
using System.Collections.Generic;
using Default.QuestBot;
using MmmjrBot.Lib.Global;
using MmmjrBot.QuestBot;

namespace MmmjrBot
{
    class QuestBotTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private readonly Interval _scanInterval = new Interval(200);
        private QuestHandler _handler;

        public string Name => "QuestBot";
        public string Description => "Bot for doing quests.";
        public string Author => "ExVault";
        public string Version => "2.0.8";

        private Vector2i _lastSeenMasterPosition;

        public void Start()
        {
            _lastSeenMasterPosition = Vector2i.Zero;
            MmmjrBotSettings.Instance.CheckGrindingFirst = false;
        }
        public void Stop()
        {

        }
        public async void Tick()
        {
            _handler?.Tick?.Invoke();
            await StuckDetection.Tick();

            if (MmmjrBotSettings.Instance.TalkToQuestgivers && World.CurrentArea.IsTown)
                TownQuestgiversLogic.Tick();
        }

        public async Task<bool> Run()
        {
            if (!MmmjrBotSettings.Instance.ShouldFollow) return false;
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInHideout)
            {
                return false;
            }
            
            if (FollowBot.Leader == null)
            {
                return false;
            }

            var leader = FollowBot.Leader;

            var distance = leader.Position.Distance(LokiPoe.Me.Position);
            if (ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(leader.Position)))
                _lastSeenMasterPosition = leader.Position;

            if (distance > MmmjrBotSettings.Instance.MaxFollowDistance || (leader.HasCurrentAction && leader.CurrentAction.Skill.InternalId == "Move")  )
            {
                var pos = ExilePather.FastWalkablePositionFor(LokiPoe.Me.Position.GetPointAtDistanceBeforeEnd(
                    leader.Position,
                    LokiPoe.Random.Next(MmmjrBotSettings.Instance.FollowDistance,
                        MmmjrBotSettings.Instance.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(LokiPoe.Me.Position, pos))
                {
                    //First check for Delve portals:
                    var delveportal = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().FirstOrDefault(x => x.Name == "Azurite Mine" && x.Metadata == "Metadata/MiscellaneousObject/PortalTransition");
                    if (delveportal != null)
                    {
                        Log.DebugFormat("[{0}] Found walkable delve portal.", Name);
                        if (LokiPoe.Me.Position.Distance(delveportal.Position) > 20)
                        {
                            var walkablePosition = ExilePather.FastWalkablePositionFor(delveportal, 20);

                            // Cast Phase run if we have it.
                            FollowBot.PhaseRun();

                            Move.Towards(walkablePosition, "moving to delve portal");
                            return true;
                        }

                        var tele = await Coroutines.InteractWith(delveportal);

                        if (!tele)
                        {
                            Log.DebugFormat("[{0}] delve portal error.", Name);
                        }

                        FollowBot.Leader = null;
                        return true;
                    }
                    AreaTransition areatransition = null;
                    if (_lastSeenMasterPosition != Vector2i.Zero)
                        areatransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                    if (areatransition == null)
                    {
                        var teleport = LokiPoe.ObjectManager.GetObjectsByName("Portal").OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                        if (teleport == null)
                            return false;
                        Log.DebugFormat("[{0}] Found walkable Teleport.", Name);
                        if (LokiPoe.Me.Position.Distance(teleport.Position) > 20)
                        {
                            var walkablePosition = ExilePather.FastWalkablePositionFor(teleport, 20);

                            // Cast Phase run if we have it.
                            FollowBot.PhaseRun();

                            Move.Towards(walkablePosition, "moving to Teleport");
                            return true;
                        }

                        var tele = await Coroutines.InteractWith(teleport);

                        if (!tele)
                        {
                            Log.DebugFormat("[{0}] Teleport error.", Name);
                        }

                        FollowBot.Leader = null;
                        return true;
                    }

                    Log.DebugFormat("[{0}] Found walkable Area Transition [{1}].", Name, areatransition.Name);
                    if (LokiPoe.Me.Position.Distance(areatransition.Position) > 20)
                    {
                        var walkablePosition = ExilePather.FastWalkablePositionFor(areatransition, 20);

                        // Cast Phase run if we have it.
                        FollowBot.PhaseRun();

                        Move.Towards(walkablePosition, "moving to area transition");
                        return true;
                    }

                    var trans = await PlayerAction.TakeTransition(areatransition);

                    if (!trans)
                    {
                        Log.DebugFormat("[{0}] Areatransition error.", Name);
                    }

                    FollowBot.Leader = null;
                    return true;
                }

                // Cast Phase run if we have it.
                FollowBot.PhaseRun();

                if (LokiPoe.Me.Position.Distance(pos) < 50)
                {
                    LokiPoe.InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos);
                }
                else
                    Move.Towards(pos, $"{leader.Name}");
                return true;
            }
            KeyManager.ClearAllKeyStates();
            return false;
        }

        private static ExplorationSettings QuestBotExploration()
        {
            var area = World.CurrentArea;

            if (!area.IsOverworldArea)
                return null;

            var areaId = area.Id;

            if (areaId == World.Act4.GrandArena.Id)
                return new ExplorationSettings(false, true, true, false, tileSeenRadius: 3);

            if (areaId == World.Act7.MaligaroSanctum.Id)
                return new ExplorationSettings(false, true);

            if (MultilevelAreas.Contains(areaId))
                return new ExplorationSettings(false, true, openPortals: false);

            return null;
        }

        private static readonly HashSet<string> MultilevelAreas = new HashSet<string>
        {
            World.Act2.AncientPyramid.Id,
            World.Act3.SceptreOfGod.Id,
            World.Act3.UpperSceptreOfGod.Id,
            World.Act6.PrisonerGate.Id,
            World.Act7.Crypt.Id,
            World.Act7.TempleOfDecay1.Id,
            World.Act7.TempleOfDecay2.Id,
            World.Act9.Descent.Id,
            World.Act9.Oasis.Id,
            World.Act9.RottingCore.Id,
            World.Act10.Ossuary.Id
        };

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            var id = message.Id;

            if (id == BotStructure.GetTaskManagerMessage)
            {
                message.AddOutput(this, MmmjrBot._taskManager);
                handled = true;
            }
            else if (id == Events.Messages.IngameBotStart)
            {
                QuestManager.CompletedQuests.Instance.Verify();
                handled = true;
            }
            else if (message.Id == Events.Messages.PlayerDied)
            {
                int deathCount = message.GetInput<int>();
                GrindingHandler.OnPlayerDied(deathCount);
                handled = true;
            }
            else if (message.Id == "QB_get_current_quest")
            {
                var s = MmmjrBotSettings.Instance;
                message.AddOutputs(this, s.CurrentQuestName, s.CurrentQuestState);
                handled = true;
            }
            else if (message.Id == "QB_finish_grinding")
            {
                GlobalLog.Debug("[QuestBot] Grinding force finish: true");
                GrindingHandler.ForceFinish = true;
                handled = true;
            }

            Events.FireEventsFromMessage(message);

            var res = MmmjrBot._taskManager.SendMessage(TaskGroup.Enabled, message);
            if (res == MessageResult.Processed)
                handled = true;

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }
    }
}