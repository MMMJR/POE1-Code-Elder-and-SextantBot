using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using System.Threading.Tasks;

namespace MmmjrBot.Lib.CommonTasks
{
    public static class LeaveAreaTaskStatic
    {
        private static bool _isActive;

        public static bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                GlobalLog.Debug(value ? "[LeaveAreaTask] Activated." : "[LeaveAreaTask] Deactivated.");
            }
        }

        public static async Task<bool> Run()
        {
            if (!World.CurrentArea.IsCombatArea)
                return false;

            if (AnyMobsNearby && MmmjrBotSettings.Instance.EnableAutoLogin)
            {
                GlobalLog.Warn("[LeaveAreaTask] Now logging out because there are monsters nearby.");
                if (!await PlayerAction.Logout())
                {
                    ErrorManager.ReportError();
                    return true;
                }
            }
            else
            {
                GlobalLog.Debug("[LeaveAreaTask] Now leaving current area.");
                if (!await PlayerAction.TpToTown(true))
                {
                    ErrorManager.ReportError();
                    return true;
                }
            }

            return true;
        }

        private static bool AnyMobsNearby => LokiPoe.ObjectManager.Objects.Any<Monster>(m => m.IsActive && m.Distance <= 20);

    }
}