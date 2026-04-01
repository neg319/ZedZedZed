using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieKindSelector
    {
        public static PawnKindDef GetKindForVariant(ZombieVariant variant, Map map = null)
        {
            switch (variant)
            {
                case ZombieVariant.Crawler:
                    return GetNamedIfAllowed("CZH_Zombie_Crawler", CustomizableZombieHordeMod.Settings.allowCrawlers);
                case ZombieVariant.Boomer:
                    return GetNamedIfAllowed("CZH_Zombie_Boomer", CustomizableZombieHordeMod.Settings.allowBoomers);
                case ZombieVariant.Sick:
                    return GetNamedIfAllowed("CZH_Zombie_Sick", CustomizableZombieHordeMod.Settings.allowSick);
                case ZombieVariant.Drowned:
                    return GetNamedIfAllowed("CZH_Zombie_Drowned", CustomizableZombieHordeMod.Settings.allowDrowned);
                case ZombieVariant.Tank:
                    return GetNamedIfAllowed("CZH_Zombie_Tank", CustomizableZombieHordeMod.Settings.allowHeavies);
                case ZombieVariant.Grabber:
                    return GetNamedIfAllowed("CZH_Zombie_Grabber", CustomizableZombieHordeMod.Settings.allowGrabbers);
                default:
                    return GetNamedIfAllowed("CZH_Zombie_Biter", CustomizableZombieHordeMod.Settings.allowBiters)
                        ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Biter");
            }
        }

        public static bool IsVariantEnabled(ZombieVariant variant, Map map = null)
        {
            return GetKindForVariant(variant, map) != null;
        }

        public static PawnKindDef GetRandomKind(Map map = null)
        {
            List<PawnKindDef> kinds = new List<PawnKindDef>();
            List<float> weights = new List<float>();

            AddIfAllowed(kinds, weights, "CZH_Zombie_Biter", CustomizableZombieHordeMod.Settings.allowBiters, 100f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Crawler", CustomizableZombieHordeMod.Settings.allowCrawlers, 8f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Boomer", CustomizableZombieHordeMod.Settings.allowBoomers, 6f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Sick", CustomizableZombieHordeMod.Settings.allowSick, 5f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Drowned", CustomizableZombieHordeMod.Settings.allowDrowned && ZombieSpecialUtility.MapHasWater(map), 4f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Tank", CustomizableZombieHordeMod.Settings.allowHeavies, 2f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Grabber", CustomizableZombieHordeMod.Settings.allowGrabbers, 3f);

            if (kinds.Count == 0)
            {
                return DefDatabase<PawnKindDef>.GetNamed("CZH_Zombie_Biter");
            }

            return kinds.RandomElementByWeight(kind => weights[kinds.IndexOf(kind)]);
        }

        private static PawnKindDef GetNamedIfAllowed(string defName, bool enabled)
        {
            if (!enabled)
            {
                return null;
            }

            return DefDatabase<PawnKindDef>.GetNamedSilentFail(defName);
        }

        private static void AddIfAllowed(List<PawnKindDef> kinds, List<float> weights, string defName, bool enabled, float weight)
        {
            if (!enabled)
            {
                return;
            }

            PawnKindDef def = DefDatabase<PawnKindDef>.GetNamedSilentFail(defName);
            if (def == null)
            {
                return;
            }

            kinds.Add(def);
            weights.Add(weight);
        }
    }
}
