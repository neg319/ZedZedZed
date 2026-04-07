using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class ZombieButcherProfile
    {
        public ZombieButcherProfile(int fleshCount, int leatherCount, float bileChance, int bileMinCount, int bileMaxCount)
        {
            FleshCount = fleshCount;
            LeatherCount = leatherCount;
            BileChance = bileChance;
            BileMinCount = bileMinCount;
            BileMaxCount = bileMaxCount;
        }

        public int FleshCount { get; }
        public int LeatherCount { get; }
        public float BileChance { get; }
        public int BileMinCount { get; }
        public int BileMaxCount { get; }
        public bool CanDropBile => BileChance > 0f && BileMaxCount > 0;
    }

    public static class ZombieVariantUtility
    {
        private static readonly Dictionary<string, ZombieVariant> KindDefNameToVariant = new Dictionary<string, ZombieVariant>
        {
            { "CZH_Zombie_Biter", ZombieVariant.Biter },
            { "CZH_Zombie_Crawler", ZombieVariant.Crawler },
            { "CZH_Zombie_Boomer", ZombieVariant.Boomer },
            { "CZH_Zombie_Sick", ZombieVariant.Sick },
            { "CZH_Zombie_Drowned", ZombieVariant.Drowned },
            { "CZH_Zombie_Tank", ZombieVariant.Tank },
            { "CZH_Zombie_Grabber", ZombieVariant.Grabber },
            { "CZH_Zombie_Lurker", ZombieVariant.Lurker }
        };

        private static readonly Dictionary<ZombieVariant, string> VariantToKindDefName = new Dictionary<ZombieVariant, string>
        {
            { ZombieVariant.Biter, "CZH_Zombie_Biter" },
            { ZombieVariant.Crawler, "CZH_Zombie_Crawler" },
            { ZombieVariant.Boomer, "CZH_Zombie_Boomer" },
            { ZombieVariant.Sick, "CZH_Zombie_Sick" },
            { ZombieVariant.Drowned, "CZH_Zombie_Drowned" },
            { ZombieVariant.Tank, "CZH_Zombie_Tank" },
            { ZombieVariant.Grabber, "CZH_Zombie_Grabber" },
            { ZombieVariant.Lurker, "CZH_Zombie_Lurker" }
        };

        private static readonly Dictionary<ZombieVariant, string> DefaultVariantLabels = new Dictionary<ZombieVariant, string>
        {
            { ZombieVariant.Biter, "Biter" },
            { ZombieVariant.Crawler, "Crawler" },
            { ZombieVariant.Boomer, "Boomer" },
            { ZombieVariant.Sick, "Sick" },
            { ZombieVariant.Drowned, "Drowned" },
            { ZombieVariant.Tank, "Heavy" },
            { ZombieVariant.Grabber, "Grabber" },
            { ZombieVariant.Lurker, "Lurker" }
        };

        private static readonly Dictionary<ZombieVariant, ZombieButcherProfile> ButcherProfiles = new Dictionary<ZombieVariant, ZombieButcherProfile>
        {
            { ZombieVariant.Biter, new ZombieButcherProfile(9, 7, 0f, 0, 0) },
            { ZombieVariant.Crawler, new ZombieButcherProfile(5, 4, 0.22f, 1, 1) },
            { ZombieVariant.Boomer, new ZombieButcherProfile(9, 7, 1f, 1, 2) },
            { ZombieVariant.Sick, new ZombieButcherProfile(9, 7, 1f, 1, 2) },
            { ZombieVariant.Drowned, new ZombieButcherProfile(9, 7, 0.34f, 1, 1) },
            { ZombieVariant.Tank, new ZombieButcherProfile(18, 14, 0.40f, 1, 2) },
            { ZombieVariant.Grabber, new ZombieButcherProfile(9, 7, 0.44f, 1, 1) },
            { ZombieVariant.Lurker, new ZombieButcherProfile(9, 7, 0.28f, 1, 1) }
        };

        public static ZombieVariant GetVariantFromKindDefName(string defName)
        {
            if (!defName.NullOrEmpty() && KindDefNameToVariant.TryGetValue(defName, out ZombieVariant variant))
            {
                return variant;
            }

            return ZombieVariant.Biter;
        }

        public static ZombieVariant GetVariant(Pawn pawn)
        {
            return GetVariantFromKindDefName(pawn?.kindDef?.defName);
        }

        public static string GetKindDefName(ZombieVariant variant)
        {
            if (VariantToKindDefName.TryGetValue(variant, out string defName))
            {
                return defName;
            }

            return VariantToKindDefName[ZombieVariant.Biter];
        }

        public static string GetVariantLabel(ZombieVariant variant)
        {
            return ZombieDefUtility.GetVariantLabel(variant);
        }

        public static string GetDefaultVariantLabel(ZombieVariant variant)
        {
            if (DefaultVariantLabels.TryGetValue(variant, out string label))
            {
                return label;
            }

            return DefaultVariantLabels[ZombieVariant.Biter];
        }

        public static bool IsSpecialVariant(ZombieVariant variant)
        {
            return variant != ZombieVariant.Biter;
        }

        public static bool IsSpecialVariant(Pawn pawn)
        {
            return pawn != null && IsSpecialVariant(GetVariant(pawn));
        }

        public static ZombieButcherProfile GetButcherProfile(ZombieVariant variant)
        {
            if (ButcherProfiles.TryGetValue(variant, out ZombieButcherProfile profile))
            {
                return profile;
            }

            return ButcherProfiles[ZombieVariant.Biter];
        }

        public static ThingDef GetGraveThingDef(ZombieVariant variant)
        {
            switch (variant)
            {
                case ZombieVariant.Crawler:
                    return ZombieDefOf.CZH_Grave_Crawler;
                case ZombieVariant.Boomer:
                    return ZombieDefOf.CZH_Grave_Boomer;
                case ZombieVariant.Sick:
                    return ZombieDefOf.CZH_Grave_Sick;
                case ZombieVariant.Drowned:
                    return ZombieDefOf.CZH_Grave_Drowned;
                case ZombieVariant.Tank:
                    return ZombieDefOf.CZH_Grave_Tank;
                case ZombieVariant.Grabber:
                    return ZombieDefOf.CZH_Grave_Grabber;
                default:
                    return ZombieDefOf.CZH_Grave_Biter;
            }
        }
    }
}
