using System.Security.Policy;
using System.Threading.Tasks;
using DreamPoeBot.Framework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;

namespace MmmjrBot.MapperBot
{
    public static class DeviceAreaTask
    {
        public static bool _toMap;

        public static async Task<bool> Run()
        {
            var area = World.CurrentArea;
            if (area.IsHideoutArea)
            {
                bool ret = false;
                for(int x = 0; x < 6; x++)
                {
                    ret = await EnterMapPortal();
                    if(ret)
                    {
                        break;
                    }
                    if(World.CurrentArea.IsMap)
                    {
                        ret = true;
                        break;
                    }
                    await Wait.ArtificialDelay();
                    await Wait.Sleep(500);
                }
                if(!ret)
                {
                    MmmjrBot.mapbot_state = MapperBotState.OpeningMap;
                    return true;
                }
                return true;
            }
            if (area.IsMapRoom)
            {
                if (_toMap)
                {
                    if (await HandleStairs(true))
                        return true;

                    return await EnterMapPortal();
                }

                var portal = ClosestTownPortal;
                if (portal != null && portal.PathExists())
                {
                    if (!await PlayerAction.TakePortal(portal))
                    {
                        ErrorManager.ReportError();
                        return false;
                    }
                        
                    return true;
                }

                if (await HandleStairs(false))
                    return true;

                if (!await PlayerAction.TakeWaypoint(World.Act11.Oriath))
                {
                    ErrorManager.ReportError();
                    return false;
                }
                    
                return true;
            }
            if(area.IsTown)
            {
                await TravelToHideoutTaskStatic.Run();
                return true;
            }
            if(area.IsMap)
            {
                return true;
            }
            return false;
        }

        public static async Task<bool> HandleStairs(bool down)
        {
            var wp = LokiPoe.ObjectManager.Objects.Find(o => o is Waypoint);
            if (down)
            {
                if (wp != null && wp.PathExists())
                {
                    if (!await PlayerAction.TakeTransition(ClosestLocalTransition))
                        ErrorManager.ReportError();

                    MmmjrBot.CWDTTrigger = false;
                    return true;
                }
            }
            else
            {
                if (wp == null || !wp.PathExists())
                {
                    if (!await PlayerAction.TakeTransition(ClosestLocalTransition))
                        ErrorManager.ReportError();

                    MmmjrBot.CWDTTrigger = false;
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> EnterMapPortal()
        {
            await Coroutines.CloseBlockingWindows();
            var portal = ClosestActiveMapPortal;
            if (portal == null)
            {
                GlobalLog.Error("[DeviceAreaTask] Fail to find any active map portal.");
                MmmjrBot.IsOnRun = false;
                _toMap = false;
                return false;
            }
            if (ExilePather.PathDistance(LokiPoe.MyPosition, portal.Position) > 10)
            {
                if (ExilePather.IsWalkable(portal.Position))
                {
                    bool ret = false;
                    for(int x = 0; x < 4; x++)
                    {
                        if(PlayerMoverManager.MoveTowards(portal.Position))
                        {
                            ret = true;
                            break;
                        }
                    }
                    if (!ret)
                        return false;
                }
            }
            if (!await PlayerAction.TakePortal(portal))
            {
                if (MmmjrBotSettings.Instance.EnableFarmingBot)
                {
                    FarmingTask.IsCombatActive = false;
                }
                ErrorManager.ReportError();
                return false;
            }
            MmmjrBot.CWDTTrigger = false;
            
            return true;
        }

        private static Portal ClosestActiveMapPortal
        {
            get
            {
                var mapPortal = LokiPoe.ObjectManager.Objects.Closest<Portal>(p => p.IsTargetable && p.LeadsTo(a => a.IsMap));

                if (mapPortal != null)
                    return mapPortal;

                // Zana daily quest
                return LokiPoe.ObjectManager.Objects.Closest<Portal>(p => p.IsTargetable && p.LeadsTo(a => a.IsMapRoom));
            }
        }

        private static Portal ClosestTownPortal => LokiPoe.ObjectManager.Objects
            .Closest<Portal>(p => p.IsTargetable && p.LeadsTo(a => a.IsTown || a.IsHideoutArea));

        private static AreaTransition ClosestLocalTransition => LokiPoe.ObjectManager.Objects
            .Closest<AreaTransition>(a => a.IsTargetable && a.TransitionType == TransitionTypes.Local);
    }
}