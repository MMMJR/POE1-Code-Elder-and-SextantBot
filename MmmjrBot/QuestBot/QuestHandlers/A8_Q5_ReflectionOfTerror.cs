using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using MmmjrBot.Lib.Global;
using MmmjrBot.Lib.Positions;
using System.Threading.Tasks;

namespace MmmjrBot.QuestBot.QuestHandlers
{
    public static class A8_Q5_ReflectionOfTerror
    {
        private static readonly TgtPosition YugulRoomTgt = new TgtPosition("Yugul room", "garden_wall_entrance_v01_01_c1r2.tgt");

        private const int FinishedStateMinimum = 2;
        private static bool _finished;

        private static NetworkObject YugulRoomObj => LokiPoe.ObjectManager.Objects
            .Find(o => o.Metadata == "Metadata/Monsters/Frog/FrogGod/SilverStatueRoots");

        private static Monster Yugul => LokiPoe.ObjectManager.GetObjects(LokiPoe.ObjectManager.PoeObjectEnum.Yugul_Reflection_of_Terror)
            .FirstOrDefault<Monster>(m => m.Rarity == Rarity.Unique);

        public static void Tick()
        {
            _finished = QuestManager.GetStateInaccurate(Quests.ReflectionOfTerror) <= FinishedStateMinimum;
        }

        public static async Task<bool> KillYugul()
        {
            if (_finished)
                return false;

            if (World.Act8.HighGardens.IsCurrentArea)
            {
                var roomobj = YugulRoomObj;
                if (roomobj != null)
                {
                    if (await Helpers.StopBeforeBoss(MmmjrBotSettings.BossNames.Yugul))
                        return true;

                    var yugul = Yugul;
                    if (yugul != null)
                    {
                        await Helpers.MoveToBossOrAnyMob(yugul);
                        return true;
                    }
                    await Helpers.MoveAndWait(roomobj.WalkablePosition(), "Waiting for any Yugul fight object");
                    return true;
                }
                await Helpers.MoveAndTakeLocalTransition(YugulRoomTgt);
                return true;
            }
            await Travel.To(World.Act8.HighGardens);
            return true;
        }

        public static async Task<bool> TakeReward()
        {
            return await Helpers.TakeQuestReward(
                World.Act8.SarnEncampment,
                TownNpcs.Hargan_A8,
                "Yugul Reward",
                book: QuestItemMetadata.BookYugul);
        }
    }
}