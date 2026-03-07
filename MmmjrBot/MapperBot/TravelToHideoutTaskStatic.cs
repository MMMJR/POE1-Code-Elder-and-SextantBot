using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;

namespace MmmjrBot.MapperBot
{
    public static class TravelToHideoutTaskStatic
    {
        public static async Task<bool> Run()
        {

            var area = World.CurrentArea;
            if (area.IsHideoutArea || area.IsMap)
                return false;

            //Zana daily room is handled by DeviceAreaTask
            if (area.Id.Contains("Daily3_1"))
                return false;

            GlobalLog.Debug("[TravelToHideoutTask] Now traveling to player's hideout.");

            if (area.IsTown || AnyWpNearby)
            {
                if (!await PlayerAction.GoToHideout())
                {
                    ErrorManager.ReportError();
                    return false;
                }
                    
            }
            else
            {
                if (!await PlayerAction.TpToTown())
                {
                    ErrorManager.ReportError();
                    return false;
                }
 
                    
            }
            return true;
        }

        private static bool AnyWpNearby => LokiPoe.ObjectManager.Objects
            .Any<Waypoint>(w => w.Distance <= 70 && w.PathDistance() <= 73);

    }
}