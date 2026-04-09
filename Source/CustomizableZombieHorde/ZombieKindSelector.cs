using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieKindSelector
    {
        public static PawnKindDef GetKindForVariant(ZombieVariant variant, Map map = null)
        {
            if (variant == ZombieVariant.Lurker)
            {
                return DefDatabase<PawnKindDef>.GetNamedSilentFail(ZombieVariantUtility.GetKindDefName(variant));
            }

            string defName = ZombieVariantUtility.GetKindDefName(variant);
            bool enabled = IsVariantAllowed(variant, map);
            PawnKindDef def = GetNamedIfAllowed(defName, enabled);
            if (def != null)
            {
                return def;
            }

            return variant == ZombieVariant.Biter ? DefDatabase<PawnKindDef>.GetNamedSilentFail(ZombieVariantUtility.GetKindDefName(ZombieVariant.Biter)) : null;
        }

        public static bool IsVariantEnabled(ZombieVariant variant, Map map = null)
        {
            return GetKindForVariant(variant, map) != null;
        }

        private static bool IsVariantAllowed(ZombieVariant variant, Map map)
        {
            switch (variant)
            {
                case ZombieVariant.Runt:
                    return CustomizableZombieHordeMod.Settings.allowRunts;
                case ZombieVariant.Boomer:
                    return CustomizableZombieHordeMod.Settings.allowBoomers;
                case ZombieVariant.Sick:
                    return CustomizableZombieHordeMod.Settings.allowSick;
                case ZombieVariant.Drowned:
                    return CanSpawnDrowned(map);
                case ZombieVariant.Brute:
                    return CustomizableZombieHordeMod.Settings.allowBrutes;
                case ZombieVariant.Grabber:
                    return CustomizableZombieHordeMod.Settings.allowGrabbers;
                case ZombieVariant.Lurker:
                    return true;
                default:
                    return CustomizableZombieHordeMod.Settings.allowBiters;
            }
        }

        public static PawnKindDef GetRandomKind(Map map = null)
        {
            List<PawnKindDef> kinds = new List<PawnKindDef>();
            List<float> weights = new List<float>();

            AddIfAllowed(kinds, weights, "CZH_Zombie_Biter", CustomizableZombieHordeMod.Settings.allowBiters, 100f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Runt", CustomizableZombieHordeMod.Settings.allowRunts, 8f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Boomer", CustomizableZombieHordeMod.Settings.allowBoomers, 6f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Sick", CustomizableZombieHordeMod.Settings.allowSick, 5f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Drowned", CanSpawnDrowned(map), GetDrownedWeight(map));
            AddIfAllowed(kinds, weights, "CZH_Zombie_Brute", CustomizableZombieHordeMod.Settings.allowBrutes, 2f);
            AddIfAllowed(kinds, weights, "CZH_Zombie_Grabber", CustomizableZombieHordeMod.Settings.allowGrabbers, 3f);

            if (kinds.Count == 0)
            {
                return DefDatabase<PawnKindDef>.GetNamed("CZH_Zombie_Biter");
            }

            return kinds.RandomElementByWeight(kind => weights[kinds.IndexOf(kind)]);
        }


        private static bool CanSpawnDrowned(Map map)
        {
            return CustomizableZombieHordeMod.Settings.allowDrowned
                && map != null
                && (ZombieSpecialUtility.MapHasWater(map) || ZombieSpecialUtility.IsRainActive(map));
        }

        private static float GetDrownedWeight(Map map)
        {
            if (map != null && ZombieSpecialUtility.IsRainActive(map))
            {
                return ZombieSpecialUtility.MapHasWater(map) ? 20f : 16f;
            }

            return 4f;
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
