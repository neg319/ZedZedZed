using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieInfectionUtility
    {
        public const float InitialInfectionSeverity = 0.20f;
        private const int InfectionTickInterval = 180;

        public static bool HasZombieInfection(Pawn pawn)
        {
            return GetZombieInfection(pawn) != null;
        }

        public static Hediff GetZombieInfection(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieSickness);
        }

        public static bool IsZombieInfectionBlocked(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return true;
            }

            if (!pawn.RaceProps.Humanlike)
            {
                return true;
            }

            if (!pawn.health.hediffSet.HasHead)
            {
                return true;
            }

            if (IsSkullMissing(pawn))
            {
                return true;
            }

            return component?.IsInfectionHeadFatal(pawn) == true;
        }

        public static void ProgressLivingInfection(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || ZombieUtility.IsZombie(pawn))
            {
                return;
            }

            Hediff infection = GetZombieInfection(pawn);
            if (infection == null)
            {
                return;
            }

            if (!pawn.RaceProps.Humanlike)
            {
                RemoveZombieInfection(pawn, component);
                return;
            }

            infection.Severity = Math.Min(1f, infection.Severity + GetInfectionSeverityPerTickStep());
            if (infection.Severity < 0.999f)
            {
                return;
            }

            if (ShouldBecomeLurkerAfterInfection(pawn, component))
            {
                component?.MarkInfectionShouldBecomeLurker(pawn);
            }
            else
            {
                component?.ClearInfectionShouldBecomeLurker(pawn);
            }

            try
            {
                pawn.Kill(null);
            }
            catch
            {
            }
        }

        public static void ProgressDeadInfection(Pawn pawn, ZombieGameComponent component, bool advanceSeverity = true)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed)
            {
                return;
            }

            Hediff infection = GetZombieInfection(pawn);
            if (infection == null)
            {
                component?.ClearInfectionHeadFatal(pawn);
                component?.ClearInfectionShouldBecomeLurker(pawn);
                return;
            }

            if (!pawn.RaceProps.Humanlike || IsZombieInfectionBlocked(pawn, component))
            {
                RemoveZombieInfection(pawn, component);
                return;
            }

            if (advanceSeverity)
            {
                infection.Severity = Math.Min(1f, infection.Severity + GetInfectionSeverityPerTickStep());
            }

            if (infection.Severity < 0.999f)
            {
                return;
            }

            TryTurnDeadInfectedPawn(pawn, component);
        }

        public static bool TryTurnDeadInfectedPawn(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (pawn.Corpse == null)
            {
                return false;
            }

            if (IsZombieInfectionBlocked(pawn, component))
            {
                RemoveZombieInfection(pawn, component);
                return false;
            }

            bool wasColonist = ShouldBecomeLurkerAfterInfection(pawn, component);
            Name preservedName = wasColonist ? pawn.Name : null;

            if (!ZombieUtility.TryResurrectZombie(pawn))
            {
                return false;
            }

            RemoveZombieInfection(pawn, component);

            if (wasColonist)
            {
                PawnKindDef lurkerKind = ZombieKindSelector.GetKindForVariant(ZombieVariant.Lurker, pawn.MapHeld);
                ZombiePawnFactory.ConvertExistingPawnToZombie(pawn, lurkerKind, Faction.OfPlayer, preserveName: true, preserveSkills: true, preserveRelations: true, initialSpawn: false);
                if (preservedName != null)
                {
                    ZombiePawnFactory.TrySetPawnName(pawn, preservedName);
                }

                ZombieLurkerUtility.EnsureColonyLurkerState(pawn, emergencyStabilize: true, stopCurrentJobs: true);
                ZombieFeedbackUtility.SendZombieTurnMessage(pawn, becameLurker: true);
                return true;
            }

            PawnKindDef randomKind = ZombieKindSelector.GetRandomKind(pawn.MapHeld);
            ZombiePawnFactory.ConvertExistingPawnToZombie(pawn, randomKind, ZombieFactionUtility.GetOrCreateZombieFaction(), preserveName: false, preserveSkills: false, preserveRelations: false, initialSpawn: false);
            ZombieUtility.EnsureZombieAggression(pawn);
            ZombieFeedbackUtility.SendZombieTurnMessage(pawn, becameLurker: false);
            return true;
        }

        private static float GetInfectionSeverityPerTickStep()
        {
            int configuredDays = CustomizableZombieHordeMod.Settings?.infectionDaysToTurn ?? 7;
            configuredDays = Mathf.Clamp(configuredDays, 1, 30);

            float severityToGain = Mathf.Max(0.01f, 1f - InitialInfectionSeverity);
            float totalSteps = (configuredDays * GenDate.TicksPerDay) / (float)InfectionTickInterval;
            return severityToGain / Mathf.Max(1f, totalSteps);
        }

        public static void RemoveZombieInfection(Pawn pawn, ZombieGameComponent component = null)
        {
            Hediff infection = GetZombieInfection(pawn);
            if (infection != null)
            {
                try
                {
                    pawn.health.RemoveHediff(infection);
                }
                catch
                {
                }
            }

            component?.ClearInfectionHeadFatal(pawn);
            component?.ClearInfectionShouldBecomeLurker(pawn);
        }

        public static float GetInfectionCompletion(Hediff infection)
        {
            if (infection == null)
            {
                return 0f;
            }

            float severityToGain = Mathf.Max(0.01f, 1f - InitialInfectionSeverity);
            return Mathf.Clamp01((infection.Severity - InitialInfectionSeverity) / severityToGain);
        }

        public static float GetInfectionCompletion(Pawn pawn)
        {
            return GetInfectionCompletion(GetZombieInfection(pawn));
        }

        public static string GetInfectionCompletionLabel(Pawn pawn)
        {
            return GetInfectionCompletion(pawn).ToStringPercent();
        }

        public static bool ShouldBecomeLurkerAfterInfection(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn == null)
            {
                return false;
            }

            if (component?.ShouldBecomeLurkerAfterInfection(pawn) == true)
            {
                return true;
            }

            if (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer)
            {
                return true;
            }

            return pawn.IsColonist || pawn.IsPrisonerOfColony;
        }

        public static bool IsSkullMissing(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return true;
            }

            return pawn.health.hediffSet.hediffs
                .OfType<Hediff_MissingPart>()
                .Any(hediff => IsSkullOrHeadPart(hediff.Part));
        }

        public static bool IsHeadOrChildPart(BodyPartRecord part, Pawn pawn)
        {
            BodyPartRecord head = ZombieUtility.GetHeadPart(pawn);
            if (head == null)
            {
                return false;
            }

            for (BodyPartRecord current = part; current != null; current = current.parent)
            {
                if (current == head)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSkullOrHeadPart(BodyPartRecord part)
        {
            if (part?.def == BodyPartDefOf.Head)
            {
                return true;
            }

            string defName = part?.def?.defName ?? string.Empty;
            return string.Equals(defName, "Skull", StringComparison.OrdinalIgnoreCase);
        }
    }
}
