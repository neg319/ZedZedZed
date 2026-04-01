using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombiePawnFactory
    {
        public static Pawn GenerateZombie(PawnKindDef kind, Faction faction)
        {
            Pawn pawn = null;

            try
            {
                pawn = faction != null ? PawnGenerator.GeneratePawn(kind, faction) : PawnGenerator.GeneratePawn(kind);
            }
            catch
            {
                try
                {
                    pawn = PawnGenerator.GeneratePawn(kind);
                }
                catch
                {
                    return null;
                }
            }

            FinalizeZombie(pawn, initialSpawn: true);
            return pawn;
        }

        public static void FinalizeZombie(Pawn pawn, bool initialSpawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRot))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRot);
            }

            if (pawn.story != null)
            {
                ZombieVariant variant = ZombieUtility.GetVariant(pawn);
                pawn.story.skinColorOverride = ZombieVisualUtility.GetSkinColor(variant);
                TrySetHairColor(pawn, ZombieVisualUtility.GetHairColor(Color.gray, variant));
                pawn.story.bodyType = ZombieVisualUtility.GetBodyType(variant, pawn.story.bodyType);
                if (pawn.style != null)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }
            }

            ZombieUtility.SetZombieDisplayName(pawn);
            ZombieUtility.StripAllUsableItems(pawn);
            ZombieUtility.TrimZombieApparel(pawn);
            ZombieUtility.MarkZombieApparelTainted(pawn, degradeApparel: initialSpawn);
            ZombieUtility.ApplyLimbDecay(pawn);
            ZombieUtility.ApplyVariantHediffs(pawn);

            if (initialSpawn)
            {
                ZombieUtility.ApplyVisibleDecayInjuries(pawn);
                ZombieUtility.GiveRunnerSpeedIfRolled(pawn);
            }

            ZombieUtility.RefreshDrownedState(pawn);
            if (pawn.needs?.mood != null)
            {
                pawn.needs.mood.CurLevel = 0.05f;
            }

            ZombieUtility.MarkPawnGraphicsDirty(pawn);
        }

        private static void TrySetHairColor(Pawn pawn, Color hairColor)
        {
            if (pawn?.story == null)
            {
                return;
            }

            try
            {
                var hairColorProperty = AccessTools.Property(pawn.story.GetType(), "hairColor");
                if (hairColorProperty != null && hairColorProperty.CanWrite)
                {
                    hairColorProperty.SetValue(pawn.story, hairColor, null);
                }
            }
            catch
            {
            }
        }
    }
}
