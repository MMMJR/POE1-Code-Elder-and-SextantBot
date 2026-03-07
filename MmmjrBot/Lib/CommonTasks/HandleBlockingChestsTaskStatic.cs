using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib.Global;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MmmjrBot.Lib.CommonTasks
{
    public static class HandleBlockingChestsTaskStatic
    {
        public static readonly HashSet<int> Processed = new HashSet<int>();

        public static bool Enabled;

        public static async Task<bool> Run()
        {
            if (!Enabled || !World.CurrentArea.IsCombatArea)
                return false;

            var chests = LokiPoe.ObjectManager.Objects
                .Where<Chest>(c => c.Distance <= 10 && !c.IsOpened && c.IsStompable && !Processed.Contains(c.Id))
                .OrderBy(c => c.DistanceSqr)
                .ToList();

            if (chests.Count == 0)
            {
                //run this task only once, by StuckDetection demand
                Enabled = false;
                return false;
            }

            LokiPoe.ProcessHookManager.Reset();
            await Coroutines.CloseBlockingWindows();

            var positions1 = new List<Vector2i> {LokiPoe.MyPosition};
            var positions2 = new List<Vector2> {LokiPoe.MyWorldPosition};

            foreach (var chest in chests)
            {
                Processed.Add(chest.Id);
                positions1.Add(chest.Position);
                positions2.Add(chest.WorldPosition);
            }

            foreach (var position in positions1)
            {
                MouseManager.SetMousePos("EXtensions.CommonTasks.HandleBlockingChestsTask", position);
                await Click();
            }

            foreach (var position in positions2)
            {
                MouseManager.SetMousePos("EXtensions.CommonTasks.HandleBlockingChestsTask", position);
                await Click();
            }
            return true;
        }

        private static async Task Click()
        {
            StuckDetection.Reset();
            await Wait.LatencySleep();
            var target = LokiPoe.InGameState.CurrentTarget;
            if (target != null)
            {
                GlobalLog.Info($"[HandleBlockingChestsTask] \"{target.Name}\" ({target.Id}) is under the cursor. Now clicking on it.");
                LokiPoe.Input.ClickLMB();
                await Coroutines.FinishCurrentAction(false);
            }
        }
    }
}