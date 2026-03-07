using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using MmmjrBot.Lib.Positions;
using System.Threading.Tasks;

namespace MmmjrBot.QuestBot.QuestHandlers
{
    public static class A3_Q5_PietyPets
    {
        private static readonly TgtPosition PietyPortalTgt = new TgtPosition("Piety room", "templeclean_prepiety_roundtop_center_01_c1r3.tgt");

        private static Monster Piety => LokiPoe.ObjectManager.GetObjects(LokiPoe.ObjectManager.PoeObjectEnum.Piety)
            .FirstOrDefault<Monster>(m => m.Rarity == Rarity.Unique);

        public static void Tick()
        {
        }

        public static async Task<bool> KillPiety()
        {
            if (Helpers.PlayerHasQuestItem(QuestItemMetadata.TowerKey))
                return false;

            if (World.Act3.LunarisTemple2.IsCurrentArea)
            {
                var piety = Piety;
                if (piety != null)
                {
                    if (await Helpers.StopBeforeBoss(MmmjrBotSettings.BossNames.Piety))
                        return true;

                    await Helpers.MoveAndWait(piety.WalkablePosition());
                    return true;
                }
                await Helpers.MoveAndTakeLocalTransition(PietyPortalTgt);
                return true;
            }
            await Travel.To(World.Act3.LunarisTemple2);
            return true;
        }

        public static async Task<bool> TakeReward()
        {
            return await Helpers.TakeQuestReward(
                World.Act3.SarnEncampment,
                TownNpcs.Grigor,
                "Piety Reward",
                book: QuestItemMetadata.BookPiety);
        }
    }
}