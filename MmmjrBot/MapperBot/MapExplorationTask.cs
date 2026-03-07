using DreamPoeBot.Loki.Game;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using System.Linq;
using System.Threading.Tasks;

namespace MmmjrBot.MapperBot
{
    public static class MapExplorationTask
    {
        private static readonly Interval TickInterval = new Interval(100);

        private static bool _mapCompletionPointReached;
        private static bool _mapCompleted;
        private static bool _bossInTheEnd;
        private static bool triggeredCompletion;

        public static bool MapCompleted
        {
            get => _mapCompleted;
            private set
            {
                _mapCompleted = value;
                if (value) TrackMob.RestrictRange();
            }
        }

        public async static Task<bool> Run()
        {
            if (MapCompleted || !World.CurrentArea.IsMap)
                return false;

            GlobalLog.Info("explore start1");

            

            if (MmmjrBotSettings.Instance.SingleUseElderFragments)
            {
                if ((LokiPoe.MyPosition.X < 165 || LokiPoe.MyPosition.X > 340) || (LokiPoe.MyPosition.Y < 165 || LokiPoe.MyPosition.Y > 340))
                {
                    GlobalLog.Warn("Exploring...");
                    return await CombatAreaCache.Current.Explorer.Execute();
                }
            }
            return true;
        }

        public static void Tick()
        {
            if (MapCompleted || !TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            GlobalLog.Info("Info Null1");

            var mapData = MapData.Current;
            if (mapData == null)
            {
                MapData.ResetCurrent();
                return;
            }
            GlobalLog.Info("Info Null1");
            var type = mapData.Type;

            if(MapCompleted && !triggeredCompletion)
            {
                if(MmmjrBotSettings.Instance.EnableSingleMap)
                {
                    if (MmmjrBotSettings.Instance.SingleUseShaperGuardianMap)
                    {
                        if (MmmjrBot.ShaperMapperCompletion[0] == false)
                        {
                            MmmjrBot.ShaperMapperCompletion[0] = true;
                        }
                        else if (MmmjrBot.ShaperMapperCompletion[1] == false)
                        {
                            MmmjrBot.ShaperMapperCompletion[1] = true;
                        }
                        else if (MmmjrBot.ShaperMapperCompletion[2] == false)
                        {
                            MmmjrBot.ShaperMapperCompletion[2] = true;
                        }
                        else if (MmmjrBot.ShaperMapperCompletion[3] == false)
                        {
                            MmmjrBot.ShaperMapperCompletion[3] = true;
                        }

                    }
                }
                triggeredCompletion = true;
            }
            GlobalLog.Info("Info Null1");
            if (KillBossTask.BossKilled)
            {
                if (_mapCompletionPointReached)
                {
                    GlobalLog.Debug("[MapExplorationTask] Boss is killed and map completion point is reached. Map is complete.");
                    MapCompleted = true;
                    return;
                }
                if (type == MapType.Bossroom)
                {
                    GlobalLog.Debug("[MapExplorationTask] Boss is killed and map type is bossroom. Map is complete.");
                    MapCompleted = true;
                    return;
                }
                if (type == MapType.Multilevel)
                {
                    GlobalLog.Debug("[MapExplorationTask] Boss is killed and map type is multilevel. Map is complete.");
                    MapCompleted = true;
                    return;
                }
                if (_bossInTheEnd)
                {
                    GlobalLog.Debug("[MapExplorationTask] Boss is killed and BossInTheEnd flag is true. Map is complete.");
                    MapCompleted = true;
                    return;
                }
            }
            GlobalLog.Info("Info Null1");
            if (!_mapCompletionPointReached)
            {
                if (LokiPoe.InstanceInfo.MonstersRemaining <= mapData.MobRemaining)
                {
                    if (type == MapType.Bossroom)
                    {
                        if (mapData.IgnoredBossroom)
                        {
                            GlobalLog.Debug("[MapExplorationTask] Bossroom is ignored. Map is complete.");
                            MapCompleted = true;
                            return;
                        }
                        TrackMob.RestrictRange();
                        CombatAreaCache.Current.Explorer.Settings.FastTransition = true;
                        return;
                    }
                    _mapCompletionPointReached = true;
                    return;
                }
                if (type == MapType.Regular || type == MapType.Bossroom)
                {
                    if (CombatAreaCache.Current.Explorer.BasicExplorer.PercentComplete >= mapData.ExplorationPercent)
                    {
                        if (type == MapType.Bossroom)
                        {
                            if (mapData.IgnoredBossroom)
                            {
                                GlobalLog.Debug("[MapExplorationTask] Bossroom is ignored. Map is complete.");
                                MapCompleted = true;
                                return;
                            }
                            TrackMob.RestrictRange();
                            CombatAreaCache.Current.Explorer.Settings.FastTransition = true;
                        }
                        _mapCompletionPointReached = true;
                    }
                }
            }
        }

        public static void Reset(string areaName)
        {
            MapCompleted = false;
            _mapCompletionPointReached = false;
            _bossInTheEnd = false;
            triggeredCompletion = false;

            if (areaName == MapNames.Excavation || areaName == MapNames.Arena)
            {
                _bossInTheEnd = true;
                GlobalLog.Info("[MapExplorationTask] BossInTheEnd is set to true.");
                return;
            }
            if (areaName == MapNames.VaultsOfAtziri)
            {
                MapData.Current.MobRemaining = -1;
                GlobalLog.Info("[MapExplorationTask] Monster remaining is set to -1.");
            }
        }

        private static void SpecificTweaksOnLocalTransition()
        {
            var areaName = World.CurrentArea.Name;
            if (areaName == MapNames.JungleValley || areaName == MapNames.ArachnidNest || areaName == MapNames.DarkForest)
            {
                GlobalLog.Info("[MapExplorationTask] Setting TileSeenRadius to 1 for this bossroom.");
                CombatAreaCache.Current.Explorer.BasicExplorer.TileSeenRadius = 1;
                return;
            }
            if (areaName == MapNames.Ramparts)
            {
                var backTransition = CombatAreaCache.Current.AreaTransitions
                    .Where(t => t.Type == TransitionType.Local && !t.LeadsBack && !t.Visited)
                    .OrderByDescending(t => t.Position.DistanceSqr)
                    .FirstOrDefault();

                if (backTransition != null)
                {
                    GlobalLog.Info($"[MapExplorationTask] Marking {backTransition.Position} as back transition.");
                    backTransition.LeadsBack = true;
                }
            }
        }
    }
}