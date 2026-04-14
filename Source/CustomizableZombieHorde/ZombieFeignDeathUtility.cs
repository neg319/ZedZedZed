using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieFeignDeathUtility
    {
        public static bool IsFeigningDeath(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieFeignDeath) == true;
        }

        public static float GetReanimationProgress(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return 0f;
            }

            HediffWithComps hediff = pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieFeignDeath) as HediffWithComps;
            return GetReanimationProgress(hediff);
        }

        public static float GetReanimationProgress(Hediff hediff)
        {
            if (hediff is HediffWithComps withComps)
            {
                HediffComp_ZombieFeignDeath comp = withComps.TryGetComp<HediffComp_ZombieFeignDeath>();
                if (comp != null)
                {
                    return comp.GetProgress();
                }
            }

            return 0f;
        }

        public static bool ShouldPreventZombieDeath(Pawn pawn)
        {
            return false;
        }

        public static bool HasIntactHeadForFakeDeath(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }

            BodyPartRecord head = ZombieUtility.GetHeadPart(pawn);
            if (head == null || pawn.health.hediffSet.PartIsMissing(head))
            {
                return false;
            }

            if (ZombieInfectionUtility.IsSkullMissing(pawn))
            {
                return false;
            }

            BodyPartRecord brain = FindBodyPart(pawn, "Brain");
            if (brain != null && pawn.health.hediffSet.PartIsMissing(brain))
            {
                return false;
            }

            return !ZombieRulesUtility.HasHeadDamageOrDestruction(pawn);
        }

        public static bool EnterFeignDeath(Pawn pawn)
        {
            return false;
        }

        public static void RegenerateDuringFeignDeath(Pawn pawn, float healAmount)
        {
            if (!IsFeigningDeath(pawn) || pawn?.health?.hediffSet == null || pawn.Dead)
            {
                return;
            }

            RemoveRecoverableMissingParts(pawn);
            RemoveBloodLoss(pawn);

            float actualHeal = Math.Max(0.20f, healAmount);
            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Brute))
            {
                actualHeal *= 1.25f;
            }
            else if (ZombieUtility.IsVariant(pawn, ZombieVariant.Runt))
            {
                actualHeal *= 0.75f;
            }

            Hediff_Injury injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => CanRecoverInjury(pawn, hediff))
                .OrderByDescending(hediff => GetRecoveryPriority(pawn, hediff))
                .FirstOrDefault();

            if (injury != null)
            {
                try
                {
                    injury.Heal(actualHeal);
                }
                catch
                {
                }
            }

            ForceZombieIntoDownedState(pawn);
        }

        public static bool TryRiseFromFeignDeath(Pawn pawn, Hediff feignDeathHediff)
        {
            if (!IsFeigningDeath(pawn) || pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (!HasIntactHeadForFakeDeath(pawn))
            {
                return false;
            }

            FullyHealForRise(pawn);

            try
            {
                if (feignDeathHediff != null)
                {
                    pawn.health.RemoveHediff(feignDeathHediff);
                }
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceIncap").SetValue(false);
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceDowned").SetValue(false);
            }
            catch
            {
            }

            ZombieUtility.PrepareSpawnedZombie(pawn);

            try
            {
                AccessTools.Method(pawn.health.GetType(), "Notify_HediffChanged")?.Invoke(pawn.health, new object[] { null });
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceIncap").SetValue(false);
                Traverse.Create(pawn.health).Field("forceDowned").SetValue(false);
            }
            catch
            {
            }

            ZombieUtility.MarkPawnGraphicsDirty(pawn);

            if (pawn.Downed)
            {
                return false;
            }

            if (ZombieUtility.IsPlayerAlignedZombie(pawn))
            {
                ZombieUtility.EnsureFriendlyZombieState(pawn, stopCurrentJobs: true);
            }
            else
            {
                ZombieUtility.AssignInitialShambleJob(pawn);
            }

            return true;
        }

        public static void ProcessFeignDeathState(Pawn pawn, int currentTick)
        {
            if (pawn?.health?.hediffSet == null || pawn.Dead || pawn.Destroyed)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieFeignDeath);
            if (hediff == null)
            {
                return;
            }

            try
            {
                pawn.health.RemoveHediff(hediff);
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceIncap").SetValue(false);
                Traverse.Create(pawn.health).Field("forceDowned").SetValue(false);
            }
            catch
            {
            }

            ZombieUtility.MarkPawnGraphicsDirty(pawn);
        }

        public static string GetFeignDeathInspectString(Pawn pawn)
        {
            return null;
        }

        public static void ForceZombieIntoDownedState(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return;
            }

            try
            {
                if (pawn.drafter != null && pawn.Drafted)
                {
                    pawn.drafter.Drafted = false;
                }
            }
            catch
            {
            }

            try
            {
                pawn.jobs?.StopAll();
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceIncap").SetValue(true);
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn.health).Field("forceDowned").SetValue(true);
            }
            catch
            {
            }
        }

        public static bool CanAutoDoubleTapPawn(Pawn pawn)
        {
            return false;
        }

        public static bool ShouldCollapseIntoReanimationComa(Pawn pawn, DamageInfo dinfo, float totalDamageDealt)
        {
            return false;
        }

        private static void StabilizeZombieForReanimationComa(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            // Keep combat damage on the zombie so gunfire still matters.
            // The coma should preserve visible trauma instead of erasing it.
            RemoveBloodLoss(pawn);
        }

        private static float GetRecoveryPriority(Pawn pawn, Hediff_Injury injury)
        {
            if (injury == null)
            {
                return 0f;
            }

            float priority = injury.Severity;
            if (!IsHeadRegionInjury(pawn, injury))
            {
                priority += 6f;
            }

            if (injury.Part?.def == BodyPartDefOf.Torso)
            {
                priority += 3f;
            }

            return priority;
        }

        private static bool CanRecoverInjury(Pawn pawn, Hediff_Injury injury)
        {
            return injury != null
                && !injury.IsPermanent()
                && injury.Part != null
                && pawn != null
                && !pawn.health.hediffSet.PartIsMissing(injury.Part);
        }

        private static bool IsHeadRegionInjury(Pawn pawn, Hediff_Injury injury)
        {
            return injury?.Part != null && ZombieInfectionUtility.IsHeadOrChildPart(injury.Part, pawn);
        }

        private static void RemoveRecoverableMissingParts(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            List<Hediff_MissingPart> missingParts = pawn.health.hediffSet.hediffs
                .OfType<Hediff_MissingPart>()
                .Where(hediff => hediff?.Part != null && !ZombieInfectionUtility.IsHeadOrChildPart(hediff.Part, pawn))
                .ToList();

            foreach (Hediff_MissingPart missing in missingParts)
            {
                try
                {
                    pawn.health.RemoveHediff(missing);
                }
                catch
                {
                }
            }
        }

        private static void RemoveBloodLoss(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            List<Hediff> removable = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff != null && hediff.def == HediffDefOf.BloodLoss)
                .ToList();

            foreach (Hediff hediff in removable)
            {
                try
                {
                    pawn.health.RemoveHediff(hediff);
                }
                catch
                {
                }
            }
        }

        private static void FullyHealForRise(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            RemoveRecoverableMissingParts(pawn);
            RemoveBloodLoss(pawn);

            List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => CanRecoverInjury(pawn, hediff))
                .ToList();

            foreach (Hediff_Injury injury in injuries)
            {
                try
                {
                    injury.Heal(999f);
                }
                catch
                {
                }
            }
        }

        private static BodyPartRecord FindBodyPart(Pawn pawn, string defName)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || defName.NullOrEmpty())
            {
                return null;
            }

            return pawn.RaceProps.body.AllParts.FirstOrDefault(part => part?.def?.defName == defName);
        }
    }
}
