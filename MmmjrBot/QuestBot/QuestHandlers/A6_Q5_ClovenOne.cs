using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using System.Threading.Tasks;

namespace MmmjrBot.QuestBot.QuestHandlers
{
    public static class A6_Q5_ClovenOne
    {
        private const int FinishedStateMinimum = 2;
        private static bool _finished;

        private static Monster Abberath => LokiPoe.ObjectManager.GetObjects(LokiPoe.ObjectManager.PoeObjectEnum.Abberath_the_Cloven_One)
            .FirstOrDefault<Monster>(m => m.Rarity == Rarity.Unique);

        public static void Tick()
        {
            _finished = QuestManager.GetStateInaccurate(Quests.ClovenOne) <= FinishedStateMinimum;
        }

        public static async Task<bool> KillAbberath()
        {
            if (_finished)
                return false;

            if (World.Act6.PrisonerGate.IsCurrentArea)
            {
                var abberath = Abberath;
                if (abberath != null && abberath.PathExists())
                {
                    if (await Helpers.StopBeforeBoss(MmmjrBotSettings.BossNames.Abberath))
                        return true;

                    await Helpers.MoveAndWait(abberath);
                    return true;
                }
                await Helpers.Explore();
                return true;
            }
            await Travel.To(World.Act6.PrisonerGate);
            return true;
        }

        public static async Task<bool> TakeReward()
        {
            return await Helpers.TakeQuestReward(
                World.Act6.LioneyeWatch,
                TownNpcs.Bestel_A6,
                "rath Reward", // Abberath or Aberrath? GGG fix your typos
                book: QuestItemMetadata.BookAbberath);
        }
    }
}