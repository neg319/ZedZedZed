using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombiePawnFactory
    {
        public static Pawn GenerateZombie(PawnKindDef kind, Faction faction)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind,
                faction,
                PawnGenerationContext.NonPlayer,
                tile: -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: true,
                colonistRelationChanceFactor: 0f,
                forceAddFreeWarmLayerIfNeeded: false,
                allowGay: true,
                allowPregnant: false,
                allowFood: false,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeApparelChance: 0f,
                biocodeWeaponChance: 0f,
                relationWithExtraPawnChanceFactor: 0f,
                validatorPreGear: null,
                validatorPostGear: null,
                forcedTraits: null,
                prohibitedTraits: null,
                minChanceToRedressWorldPawn: null,
                fixedBiologicalAge: null,
                fixedChronologicalAge: null,
                fixedGender: null,
                fixedBirthName: null,
                fixedLastName: null,
                allowedDevelopmentalStages: DevelopmentalStage.Adult);

            return PawnGenerator.GeneratePawn(request);
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
                pawn.story.hairColor = ZombieVisualUtility.GetHairColor(pawn.story.hairColor, variant);
                pawn.story.bodyType = ZombieVisualUtility.GetBodyType(variant, pawn.story.bodyType);
                if (pawn.style != null)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }
            }

            ZombieUtility.StripWeaponsAndWeaponInventory(pawn);
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

            pawn.Drawer?.renderer?.graphics?.SetAllGraphicsDirty();
        }
    }
}
