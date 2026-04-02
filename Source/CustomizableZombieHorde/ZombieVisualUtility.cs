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

        private static readonly Color[] CrawlerSkinPalette =
        {
            new Color(0.53f, 0.48f, 0.40f),
            new Color(0.47f, 0.42f, 0.34f),
            new Color(0.43f, 0.48f, 0.39f),
            new Color(0.41f, 0.39f, 0.34f)
        };

        private static readonly Color[] BoomerSkinPalette =
        {
            new Color(0.67f, 0.73f, 0.50f),
            new Color(0.62f, 0.69f, 0.46f),
            new Color(0.58f, 0.63f, 0.41f),
            new Color(0.52f, 0.56f, 0.39f)
        };

        private static readonly Color[] SickSkinPalette =
        {
            new Color(0.54f, 0.52f, 0.51f),
            new Color(0.49f, 0.47f, 0.48f),
            new Color(0.45f, 0.42f, 0.44f),
            new Color(0.40f, 0.37f, 0.39f)
        };

        private static readonly Color[] DrownedSkinPalette =
        {
            new Color(0.53f, 0.62f, 0.64f),
            new Color(0.47f, 0.57f, 0.60f),
            new Color(0.42f, 0.50f, 0.56f),
            new Color(0.40f, 0.46f, 0.49f)
        };

        private static readonly Color[] TankSkinPalette =
        {
            new Color(0.51f, 0.47f, 0.44f),
            new Color(0.47f, 0.43f, 0.40f),
            new Color(0.43f, 0.40f, 0.37f),
            new Color(0.38f, 0.36f, 0.34f)
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
            new Color(0.86f, 0.82f, 0.72f),
            new Color(0.80f, 0.76f, 0.67f),
            new Color(0.75f, 0.71f, 0.63f),
            new Color(0.69f, 0.66f, 0.59f)
        };

        public static bool ShouldLookSkeletal(Pawn pawn)
        {
            if (pawn == null || ZombieUtility.GetVariant(pawn) != ZombieVariant.Biter)
            {
                return false;
            }

            return Mathf.Abs(pawn.thingIDNumber) % 9 == 0;
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
            if (variant == ZombieVariant.Biter)
            {
                if (ShouldLookSkeletal(pawn))
                {
                    return SkeletalBiterSkinPalette.RandomElement();
                }

                if (ShouldLookDesiccated(pawn))
                {
                    return DesiccatedBiterSkinPalette.RandomElement();
                }
            }

            switch (variant)
            {
                case ZombieVariant.Crawler:
                    return CrawlerSkinPalette.RandomElement();
                case ZombieVariant.Boomer:
                    return BoomerSkinPalette.RandomElement();
                case ZombieVariant.Sick:
                    return SickSkinPalette.RandomElement();
                case ZombieVariant.Drowned:
                    return DrownedSkinPalette.RandomElement();
                case ZombieVariant.Tank:
                    return TankSkinPalette.RandomElement();
                case ZombieVariant.Grabber:
                    return GrabberSkinPalette.RandomElement();
                default:
                    return BiterSkinPalette.RandomElement();
            }
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
                case ZombieVariant.Tank:
                    tint = new Color(0.22f, 0.22f, 0.20f);
                    break;
                case ZombieVariant.Grabber:
                    tint = new Color(0.40f, 0.33f, 0.35f);
                    break;
                default:
                    tint = Color.gray;
                    break;
            }

            return Color.Lerp(existingColor, tint, 0.82f);
        }

        public static BodyTypeDef GetBodyType(ZombieVariant variant, Pawn pawn, BodyTypeDef fallback)
        {
            switch (variant)
            {
                case ZombieVariant.Biter:
                case ZombieVariant.Crawler:
                    return BodyTypeDefOf.Thin;
                case ZombieVariant.Boomer:
                    return BodyTypeDefOf.Fat;
                case ZombieVariant.Drowned:
                case ZombieVariant.Grabber:
                    return GetNormalBodyType(pawn);
                case ZombieVariant.Tank:
                    return BodyTypeDefOf.Hulk;
                default:
                    return fallback ?? GetNormalBodyType(pawn);
            }
        }

        private static BodyTypeDef GetNormalBodyType(Pawn pawn)
        {
            return pawn != null && pawn.gender == Gender.Female ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
        }

        public static Color GetOverlayColor(Pawn pawn)
        {
            if (ShouldLookSkeletal(pawn))
            {
                return new Color(0.93f, 0.90f, 0.83f, 1f);
            }

            if (ShouldLookDesiccated(pawn))
            {
                return new Color(0.74f, 0.67f, 0.52f, 1f);
            }

            return Color.white;
        }

        public static string GetVariantOverlayPath(Pawn pawn, ZombieVariant variant)
        {
            if (variant == ZombieVariant.Biter && ShouldLookSkeletal(pawn))
            {
                return "PawnOverlays/ZombieSkeletonBody";
            }

            switch (variant)
            {
                case ZombieVariant.Crawler:
                    return "PawnOverlays/ZombieCrawlerBody";
                case ZombieVariant.Boomer:
                    return "PawnOverlays/ZombieBoomerBody";
                case ZombieVariant.Sick:
                    return "PawnOverlays/ZombieSickBody";
                case ZombieVariant.Drowned:
                    return "PawnOverlays/ZombieDrownedBody";
                case ZombieVariant.Tank:
                    return "PawnOverlays/ZombieTankBody";
                case ZombieVariant.Grabber:
                    return "PawnOverlays/ZombieGrabberBody";
                default:
                    return "PawnOverlays/ZombieBiterBody";
            }
        }
    }
}
