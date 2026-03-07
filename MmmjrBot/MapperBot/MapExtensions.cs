using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using MmmjrBot.Lib;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState.ExpeditionDealerUi.Deal;

namespace MmmjrBot.MapperBot
{
    public static class MapExtensions
    {
        public static bool IsMap(this Item item)
        {
            return item.Class == ItemClasses.Map;
        }

        public static bool IsMapFragment(this Item item)
        {
            return item.Class == ItemClasses.MapFragment;
        }

        public static string CleanName(this Item map)
        {
            return map.RarityLite() == Rarity.Unique ? map.FullName : map.MapArea.Name;
        }

        public static bool BelowTierLimit(this Item map)
        {
            return map.MapTier <= MmmjrBotSettings.Instance.MaxMapTier;
        }

        public static bool CanAugment(this Item map)
        {
            return map.ExplicitAffixes.Count() < 2;
        }

        public static bool ShouldUpgrade(this Item map, Upgrade upgrade)
        {
            var tier = map.MapTier;

            if (!upgrade.TierEnabled) return false;

            if (MmmjrBotSettings.Instance.AtlasExplorationEnabled)
            {
                var cleanName = map.CleanName();
                if (!AtlasData.IsCompleted(cleanName))
                {
                    if (tier >= 6 && (upgrade == MmmjrBotSettings.Instance.MagicRareUpgrade))
                        return true;

                    if (tier >= 11 && upgrade == MmmjrBotSettings.Instance.VaalUpgrade)
                        return true;
                }

                if (upgrade.TierEnabled && tier >= upgrade.Tier)
                    return true;
            }

            return false;
        }

        /*public static bool ShouldSell(this Item map)
        {
            if (map.RarityLite() == Rarity.Unique)
                return false;

            if (GeneralSettings.SellIgnoredMaps && map.Ignored())
                return true;

            if (map.MapTier > GeneralSettings.MaxSellTier)
                return false;

            if (map.Priority() > GeneralSettings.MaxSellPriority)
                return false;

            return true;
        }*/

        public static bool IsSacrificeFragment(this Item item)
        {
            return item.Metadata.Contains("CurrencyVaalFragment1");
        }

        internal class AtlasData
        {
            private static readonly HashSet<string> BonusCompletedAreas = new HashSet<string>();

            internal static bool IsCompleted(string name)
            {
                return BonusCompletedAreas.Contains(name);
            }

            internal static void Update()
            {
                BonusCompletedAreas.Clear();

                foreach (var area in LokiPoe.InstanceInfo.Atlas.BonusCompletedAreas)
                {
                    BonusCompletedAreas.Add(area.Name);
                }
            }
        }
    }
}