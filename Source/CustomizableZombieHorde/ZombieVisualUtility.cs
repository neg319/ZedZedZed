using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieVisualUtility
    {
        private static readonly Color[] BiterSkinPalette =
        {
            new Color(0.63f, 0.71f, 0.59f),
            new Color(0.58f, 0.66f, 0.56f),
            new Color(0.53f, 0.59f, 0.50f),
            new Color(0.47f, 0.48f, 0.46f)
        };

        private static readonly Color[] RuntSkinPalette =
        {
            new Color(0.53f, 0.48f, 0.40f),
            new Color(0.47f, 0.42f, 0.34f),
            new Color(0.43f, 0.48f, 0.39f),
            new Color(0.41f, 0.39f, 0.34f)
        };

        private static readonly Color[] BoomerSkinPalette =
        {
            new Color(0.66f, 0.69f, 0.44f),
            new Color(0.61f, 0.64f, 0.39f),
            new Color(0.56f, 0.60f, 0.32f),
            new Color(0.62f, 0.58f, 0.34f),
            new Color(0.57f, 0.54f, 0.29f)
        };

        private static readonly Color[] SickSkinPalette =
        {
            new Color(0.34f, 0.31f, 0.29f),
            new Color(0.30f, 0.27f, 0.25f),
            new Color(0.27f, 0.24f, 0.21f),
            new Color(0.23f, 0.20f, 0.18f),
            new Color(0.20f, 0.18f, 0.16f)
        };

        private static readonly Color[] DrownedSkinPalette =
        {
            new Color(0.47f, 0.58f, 0.62f),
            new Color(0.41f, 0.53f, 0.58f),
            new Color(0.37f, 0.48f, 0.53f),
            new Color(0.33f, 0.44f, 0.49f),
            new Color(0.30f, 0.41f, 0.45f)
        };

        private static readonly Color[] BruteSkinPalette =
        {
            new Color(0.78f, 0.68f, 0.66f),
            new Color(0.73f, 0.64f, 0.62f),
            new Color(0.69f, 0.61f, 0.59f),
            new Color(0.65f, 0.58f, 0.56f),
            new Color(0.61f, 0.55f, 0.53f)
        };

        private static readonly Color[] GrabberSkinPalette =
        {
            new Color(0.56f, 0.61f, 0.55f),
            new Color(0.50f, 0.56f, 0.51f),
            new Color(0.46f, 0.52f, 0.47f),
            new Color(0.42f, 0.47f, 0.43f)
        };

        private static readonly Color[] DesiccatedBiterSkinPalette =
        {
            new Color(0.63f, 0.58f, 0.43f),
            new Color(0.56f, 0.50f, 0.37f),
            new Color(0.49f, 0.44f, 0.33f),
            new Color(0.42f, 0.38f, 0.28f)
        };

        private static readonly Color[] SkeletalBiterSkinPalette =
        {
            new Color(0.95f, 0.94f, 0.91f),
            new Color(0.90f, 0.89f, 0.86f),
            new Color(0.86f, 0.85f, 0.82f),
            new Color(0.82f, 0.81f, 0.77f),
            new Color(0.78f, 0.76f, 0.71f)
        };

        public static bool ShouldLookSkeletal(Pawn pawn)
        {
            return ZombieUtility.IsSkeletonBiter(pawn) || ZombieUtility.ShouldSpawnAsSkeletonBiter(pawn);
        }

        public static bool ShouldLookDesiccated(Pawn pawn)
        {
            if (pawn == null || ZombieUtility.GetVariant(pawn) != ZombieVariant.Biter || ShouldLookSkeletal(pawn))
            {
                return false;
            }

            return Mathf.Abs(pawn.thingIDNumber) % 5 == 0;
        }

        public static Color GetSkinColor(Pawn pawn, ZombieVariant variant)
        {
            variant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, variant);

            if (variant == ZombieVariant.Biter)
            {
                if (ShouldLookSkeletal(pawn))
                {
                    return GetStablePaletteColor(pawn, SkeletalBiterSkinPalette, 31);
                }

                if (ShouldLookDesiccated(pawn))
                {
                    return GetStablePaletteColor(pawn, DesiccatedBiterSkinPalette, 37);
                }
            }

            switch (variant)
            {
                case ZombieVariant.Runt:
                    return GetStablePaletteColor(pawn, RuntSkinPalette, 5);
                case ZombieVariant.Boomer:
                    return GetStablePaletteColor(pawn, BoomerSkinPalette, 11);
                case ZombieVariant.Sick:
                    return GetStablePaletteColor(pawn, SickSkinPalette, 13);
                case ZombieVariant.Drowned:
                    return GetStablePaletteColor(pawn, DrownedSkinPalette, 17);
                case ZombieVariant.Brute:
                    return GetStablePaletteColor(pawn, BruteSkinPalette, 19);
                case ZombieVariant.Grabber:
                    return GetStablePaletteColor(pawn, GrabberSkinPalette, 23);
                default:
                    return GetStablePaletteColor(pawn, BiterSkinPalette, 29);
            }
        }

        private static Color GetStablePaletteColor(Pawn pawn, Color[] palette, int salt)
        {
            if (palette == null || palette.Length == 0)
            {
                return Color.white;
            }

            int seed = pawn != null ? pawn.thingIDNumber : 0;
            int index = Mathf.Abs(seed + salt) % palette.Length;
            return palette[index];
        }

        public static Color GetHairColor(Color existingColor, ZombieVariant variant)
        {
            Color tint;
            switch (variant)
            {
                case ZombieVariant.Boomer:
                    tint = new Color(0.48f, 0.51f, 0.30f);
                    break;
                case ZombieVariant.Sick:
                    tint = new Color(0.25f, 0.21f, 0.22f);
                    break;
                case ZombieVariant.Drowned:
                    tint = new Color(0.28f, 0.39f, 0.37f);
                    break;
                case ZombieVariant.Brute:
                    tint = new Color(0.22f, 0.22f, 0.20f);
                    break;
                case ZombieVariant.Grabber:
                    tint = new Color(0.40f, 0.33f, 0.35f);
                    break;
                case ZombieVariant.Lurker:
                    tint = new Color(0.36f, 0.35f, 0.31f);
                    break;
                default:
                    tint = Color.gray;
                    break;
            }

            return Color.Lerp(existingColor, tint, 0.82f);
        }

        public static BodyTypeDef GetBodyType(ZombieVariant variant, Pawn pawn, BodyTypeDef fallback)
        {
            variant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, variant);
            switch (variant)
            {
                case ZombieVariant.Biter:
                    return BodyTypeDefOf.Thin;
                case ZombieVariant.Runt:
                    return GetChildBodyType() ?? BodyTypeDefOf.Thin;
                case ZombieVariant.Boomer:
                    return BodyTypeDefOf.Fat;
                case ZombieVariant.Drowned:
                case ZombieVariant.Grabber:
                    return GetNormalBodyType(pawn);
                case ZombieVariant.Brute:
                    return BodyTypeDefOf.Hulk;
                default:
                    return fallback ?? GetNormalBodyType(pawn);
            }
        }

        private static BodyTypeDef GetChildBodyType()
        {
            return DefDatabase<BodyTypeDef>.GetNamedSilentFail("Child")
                ?? DefDatabase<BodyTypeDef>.GetNamedSilentFail("Baby");
        }

        private static BodyTypeDef GetNormalBodyType(Pawn pawn)
        {
            return pawn != null && pawn.gender == Gender.Female ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
        }

        public static Color GetOverlayColor(Pawn pawn)
        {
            if (ShouldLookSkeletal(pawn))
            {
                return new Color(0.95f, 0.96f, 0.93f, 1f);
            }

            if (ShouldLookDesiccated(pawn))
            {
                return new Color(0.74f, 0.67f, 0.52f, 1f);
            }

            return Color.white;
        }

        public static string GetVariantOverlayPath(Pawn pawn, ZombieVariant variant)
        {
            variant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, variant);

            if (variant == ZombieVariant.Biter && ShouldLookSkeletal(pawn))
            {
                return "PawnOverlays/ZombieBoneBiterBody";
            }

            switch (variant)
            {
                case ZombieVariant.Runt:
                    return "PawnOverlays/ZombieRuntBody";
                case ZombieVariant.Boomer:
                    return "PawnOverlays/ZombieBoomerBody";
                case ZombieVariant.Sick:
                    return "PawnOverlays/ZombieSickBody";
                case ZombieVariant.Drowned:
                    return "PawnOverlays/ZombieDrownedBody";
                case ZombieVariant.Brute:
                    return "PawnOverlays/ZombieBruteBody";
                case ZombieVariant.Grabber:
                    return "PawnOverlays/ZombieGrabberBody";
                default:
                    return "PawnOverlays/ZombieBiterBody";
            }
        }
    }
}
