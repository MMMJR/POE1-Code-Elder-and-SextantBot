using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.CommonTasks;
using MmmjrBot.Lib.Positions;
using static DreamPoeBot.Loki.Elements.GrandHeistContractElement;

namespace MmmjrBot.MapperBot
{
    public static class KillBossTask
    {
        private const int MaxKillAttempts = 1000;

        private static readonly Interval LogInterval = new Interval(1000);
        private static readonly Interval TickInterval = new Interval(80);
        private static readonly Stopwatch _elderTime = Stopwatch.StartNew();


        public static bool BossKilled { get; private set; }

        public static List<CachedBoss> CachedBosses = new List<CachedBoss>();

        public static CachedBoss _currentTarget;
        private static int _bossesKilled;

        private static bool _multiPhaseBoss;
        private static bool _teleportingBoss;
        private static string _priorityBossName;
        private static int _bossRange;

        private static Func<Monster, bool> _isMapBoss = DefaultBossSelector;

        public static SearchTargetResult GetTarget()
        {
            if (_currentTarget == null)
                return null;

            var m = _currentTarget.Object as Monster;

            if (m == null) return null;

            SearchTargetResult result = new SearchTargetResult(_currentTarget.Id, _currentTarget.Position, m);

            return result;
        }

        public async static Task<bool> Run()
        {
            if (BossKilled || MapExplorationTask.MapCompleted)
                return false;

            var area = World.CurrentArea;

            if (!area.IsMap)
                return true;

            if (_currentTarget == null)
            {
                if ((_currentTarget = CachedBosses.ClosestValid(b => !b.IsDead)) == null)
                    return false;
            }
            // min 164
            // max 341
            if(MmmjrBotSettings.Instance.SingleUseElderFragments)
            {
                if ((LokiPoe.MyPosition.X < 165 || LokiPoe.MyPosition.X > 340) || (LokiPoe.MyPosition.Y < 165 || LokiPoe.MyPosition.Y > 340))
                {
                    GlobalLog.Warn("Out of Boss Area");
                    return false;
                } 
            }

            if (!MmmjrBotSettings.Instance.SingleUseElderFragments && !MmmjrBotSettings.Instance.SingleUseShaperFragments)
            {
                if (Blacklist.Contains(_currentTarget.Id))
                {
                    GlobalLog.Warn("[KillBossTask] Boss is in global blacklist. Now marking it as killed.");
                    string name = _currentTarget.Name;
                    _currentTarget.IsDead = true;
                    _currentTarget = null;
                    RegisterDeath(name);
                    return false;
                }
            }
            

            if (_priorityBossName != null && _currentTarget.Position.Name != _priorityBossName)
            {
                var priorityBoss = CachedBosses.ClosestValid(b => !b.IsDead && b.Position.Name == _priorityBossName);
                if (priorityBoss != null)
                {
                    GlobalLog.Debug($"[KillBossTask] Switching current target to \"{priorityBoss}\".");
                    _currentTarget = priorityBoss;
                    return false;
                }
            }

            if (_currentTarget.IsDead)
            {
                _currentTarget = null;
                return true;
            }

            var pos = _currentTarget.Position;
            if (pos.Distance <= 70 && pos.PathDistance <= 75)
            {
                var bossObj = _currentTarget.Object as Monster;
                if (bossObj == null)
                {
                    if (_teleportingBoss)
                    {
                        CachedBosses.Remove(_currentTarget);
                        _currentTarget = null;
                        return false;
                    }
                    if(_currentTarget.Name == "The Elder")
                    {
                            return true;
                    }
                    GlobalLog.Debug("[KillBossTask] We are close to last know position of map boss, but boss object does not exist anymore.");
                    GlobalLog.Debug("[KillBossTask] Most likely this boss does not spawn a corpse or was shattered/exploded.");
                    string mobName = _currentTarget.Name;
                    _currentTarget.IsDead = true;
                    _currentTarget = null;
                    RegisterDeath(mobName);
                    return false;
                }
            }

            if (pos.Distance > MmmjrBotSettings.Instance.CombatRange)
            {
                if (!pos.TryCome())
                {
                    GlobalLog.Error(MapData.Current.Type == MapType.Regular
                        ? $"[KillBossTask] Unexpected error. Fail to move to map boss ({pos.Name}) in a regular map."
                        : $"[KillBossTask] Fail to move to the map boss \"{pos.Name}\". Will try again after area transition.");
                    return true;
                }
                return true;
            }

            return true;
        }

        public static async void Tick()
        {
            if (BossKilled)
                return;

            if (!TickInterval.Elapsed)
                return;

            if (!LokiPoe.IsInGame || !World.CurrentArea.IsMap)
                return;

            foreach (var obj in LokiPoe.ObjectManager.Objects)
            {
                var mob = obj as Monster;

                if (mob == null)
                    continue;

                var id = mob.Id;
                var cached = CachedBosses.Find(b => b.Id == id);

                if (!mob.IsDead)
                {
                    var pos = mob.WalkablePosition(5, 20);
                    if (cached != null)
                    {
                        cached.Position = pos;
                    }
                    else
                    {
                        if (mob.Metadata.Contains("TentaclePortal") || mob.Name.Contains("Portal") || mob.Name.Contains("Formless") || mob.Name.Contains("Witness"))
                        {
                            continue;
                        }

                        if (mob.Metadata == "Metadata/Monsters/AtlasBosses/ZanaElder" || mob.Metadata == "Metadata/Monsters/AtlasBosses/TheShaperBossElderEncounter" || mob.Metadata == "Metadata/Monsters/ArcticBreath/ArcticBreathSkull" || mob.Metadata == "Metadata/Monsters/RaisedSkeletons/RaisedSkeletonStandard")
                        {
                            continue;
                        }

                        if (mob.Name == "The Elder")
                        {
                            GlobalLog.Warn($"[KillBossTask] Registering {pos}");
                            CachedBosses.Add(new CachedBoss(id, pos, false, mob.Name));
                            _elderTime.Restart();
                        }
                    }
                }
                else
                {
                    if (cached == null)
                    {
                        GlobalLog.Warn($"[KillBossTask] Registering dead map boss \"{mob.Name}\".");
                        CachedBosses.Add(new CachedBoss(id, mob.WalkablePosition(), true, mob.Name));
                        RegisterDeath(mob.Name);
                    }
                    else if (!cached.IsDead)
                    {
                        GlobalLog.Warn($"[KillBossTask] Registering death of \"{mob.Name}\".");
                        cached.IsDead = true;
                        if (!_multiPhaseBoss) _currentTarget = null;
                        RegisterDeath(mob.Name);
                    }
                }
            }
            return;
        }

        private static void RegisterDeath(string name)
        {
            ++_bossesKilled;
            var total = BossAmountForMap;
            GlobalLog.Warn($"[KillBossTask] Bosses killed: {_bossesKilled} out of {total}.");
            if(MmmjrBotSettings.Instance.SingleUseElderFragments)
            {
                if (name == "The Elder")
                {
                    BossKilled = true;
                }
                return;
            }
            if (_bossesKilled >= BossAmountForMap)
            {
                BossKilled = true;
            }
            return;
        }

        private static int BossAmountForMap
        {
            get
            {
                int bosses;
                if (!BossesPerMap.TryGetValue(World.CurrentArea.Name, out bosses))
                {
                    bosses = 1;
                }

                LokiPoe.LocalData.MapMods.TryGetValue(StatTypeGGG.MapSpawnTwoBosses, out int twoBossesFlag);
                if (twoBossesFlag == 1)
                {
                    return bosses * 2;
                }
                return Math.Max(bosses, CachedBosses.Count);
            }
        }

        public static readonly Dictionary<string, int> BossesPerMap = new Dictionary<string, int>
        {
            [MapNames.Arcade] = 2,
            [MapNames.CrystalOre] = 3,
            [MapNames.VaalPyramid] = 3,
            [MapNames.Canyon] = 2,
            [MapNames.Racecourse] = 3,
            [MapNames.Strand] = 2,
            [MapNames.Arcade] = 2,
            [MapNames.Arena] = 3,
            [MapNames.TropicalIsland] = 3,
            [MapNames.Coves] = 2,
            [MapNames.Promenade] = 2,
            [MapNames.Courtyard] = 3,
            [MapNames.Port] = 5,
            [MapNames.Excavation] = 2,
            [MapNames.Plateau] = 2,
            [MapNames.OvergrownRuin] = 2,
            [MapNames.MineralPools] = 2,
            [MapNames.Palace] = 2,
            [MapNames.Core] = 4,
            [MapNames.CitySquare] = 3,
            [MapNames.Courthouse] = 3,
            [MapNames.Graveyard] = 3,
            [MapNames.Basilica] = 2,

            [MapNames.MaelstromOfChaos] = 2,
            [MapNames.WhakawairuaTuahu] = 2,
            [MapNames.OlmecSanctum] = 5,
        };

        public static void SetNew()
        {
            _bossesKilled = 0;
            _currentTarget = null;
            BossKilled = false;
            _multiPhaseBoss = false;
            _teleportingBoss = false;
            CachedBosses.Clear();
            SetBossSelector(World.CurrentArea.Name);
            SetPriorityBossName(World.CurrentArea.Name);
            SetBossRange(World.CurrentArea.Name);
            string areaName = World.CurrentArea.Name;
            if (areaName == MapNames.VaultsOfAtziri)
            {
                BossKilled = true;
            }

            if (areaName == MapNames.MineralPools ||
                areaName == MapNames.Palace ||
                areaName == MapNames.Basilica ||
                areaName == MapNames.MaelstromOfChaos)
            {
                _multiPhaseBoss = true;
            }

            if (areaName == MapNames.Pen ||
                areaName == MapNames.Pier ||
                areaName == MapNames.Shrine ||
                areaName == MapNames.DesertSpring ||
                areaName == MapNames.Summit ||
                areaName == MapNames.DarkForest ||
                areaName == MapNames.PutridCloister)
            {
                _teleportingBoss = true;
            }
        }

        private static void SetPriorityBossName(string areaName)
        {
            if (areaName == MapNames.Racecourse)
            {
                _priorityBossName = "Bringer of Blood";
                GlobalLog.Info("[KillBossTask] Priority boss name is set to \"Bringer of Blood\".");
            }
            else if (areaName == MapNames.InfestedValley)
            {
                _priorityBossName = "Gorulis' Nest";
                GlobalLog.Info("[KillBossTask] Priority boss name is set to \"Gorulis' Nest\".");
            }
            else if (areaName == MapNames.Siege)
            {
                _priorityBossName = "Tukohama's Protection";
                GlobalLog.Info("[KillBossTask] Priority boss name is set to \"Tukohama's Protection\".");
            }
            else
            {
                _priorityBossName = null;
            }
        }

        private static void SetBossRange(string areaName)
        {
            if (areaName == MapNames.Tower)
            {
                _bossRange = 35;
            }
            else if (areaName == MapNames.Fields)
            {
                _bossRange = 30;
            }
            else if (areaName == MapNames.UndergroundRiver)
            {
                _bossRange = 7;
            }
            else
            {
                _bossRange = 15;
            }
            GlobalLog.Info($"[KillBossTask] Boss range: {_bossRange}");
        }

        private static void SetBossSelector(string areaName)
        {
            if (areaName == MapNames.Precinct)
            {
                GlobalLog.Info("[KillBossTask] This map has a group of Rogue Exiles as map bosses.");
                _isMapBoss = m => m.Rarity == Rarity.Unique && m.Metadata.Contains("MapBoss");
                return;
            }
            if (areaName == MapNames.Courthouse)
            {
                GlobalLog.Info("[KillBossTask] This map has a group of dark Rogue Exiles as map bosses.");
                _isMapBoss = m => m.Rarity == Rarity.Unique && m.Metadata.EndsWith("Kitava") && m.Metadata.Contains("/Exiles/");
                return;
            }
            if (areaName == MapNames.InfestedValley)
            {
                GlobalLog.Info("[KillBossTask] Nests have to be killed to activate boss on this map.");
                _isMapBoss = m => m.IsMapBoss || m.Name == "Gorulis' Nest";
                return;
            }
            if (areaName == MapNames.Siege)
            {
                GlobalLog.Info("[KillBossTask] Totems must be killed to remove boss immunity on this map.");
                _isMapBoss = m => m.IsMapBoss || m.Name == "Tukohama's Protection";
                return;
            }
            if (areaName == MapNames.WhakawairuaTuahu)
            {
                GlobalLog.Info("[KillBossTask] This map has Shade of a player as one of the map bosses.");
                _isMapBoss = m => m.IsMapBoss || (m.Rarity == Rarity.Unique && m.Metadata.Contains("DarkExile"));
                return;
            }

            if (areaName == MapNames.Shipyard ||
                areaName == MapNames.Lighthouse ||
                areaName == MapNames.Iceberg)
            {
                GlobalLog.Info("[KillBossTask] This map has a Warband leader as a map boss.");
                _isMapBoss = m => m.Rarity == Rarity.Unique && m.ExplicitAffixes.Any(a => a.InternalName == "MonsterWbLeader");
                return;
            }

            _isMapBoss = DefaultBossSelector;
        }

        private static bool DefaultBossSelector(Monster m)
        {
            return m.IsMapBoss;
        }

        public class CachedBoss : CachedObject
        {
            public bool IsDead { get; set; }

            public string Name { get; set; }

            public CachedBoss(int id, WalkablePosition position, bool isDead, string name) : base(id, position)
            {
                IsDead = isDead;
                Name = name;
            }
        }
    }
}