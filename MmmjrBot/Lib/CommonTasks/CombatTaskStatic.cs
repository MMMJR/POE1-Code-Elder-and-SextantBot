using System.Threading.Tasks;
using System.Windows.Input;
using DreamPoeBot.Loki.Bot;

namespace MmmjrBot.Lib.CommonTasks
{
    public static class CombatTaskStatic
    {
        public static async Task<bool> Run(int leashRange)
        {
            if (!MmmjrBotSettings.Instance.ShouldKill) return false;
            if (!World.CurrentArea.IsCombatArea)
                return false;

            var routine = RoutineManager.Current;

            routine.Message(new Message("SetLeash", null, leashRange));

            var res = await routine.Logic(new Logic("hook_combat"));
            return res == LogicResult.Provided;
        }

        public static async Task<bool> Execute()
        {
            await CombatTaskStatic.Run(50);
            await CombatTaskStatic.Run(-1);
            await Wait.ArtificialDelay();
            await Wait.Sleep(20);
            return true;
        }
    }
}
