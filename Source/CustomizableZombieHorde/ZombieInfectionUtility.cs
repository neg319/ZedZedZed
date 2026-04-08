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
        public const float TerminalSeverityThreshold = 0.90f;
        private const int InfectionTickInterval = 180;

        public static bool HasZombieInfection(Pawn pawn)
        {
            return GetZombieInfection(pawn) != null;
        }

        public static Hediff GetZombieInfection(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieSickness);
        }

        public static bool HasReanimatedState(Pawn pawn)
        {
            return GetReanimatedState(pawn) != null;
        }

        public static Hediff GetReanimatedState(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieReanimated);
        }

        public static Hediff GetVisibleInfectionState(Pawn pawn)
        {
            return GetReanimatedState(pawn) ?? GetZombieInfection(pawn);
        }

        public static bool IsTerminal(Pawn pawn)
        {
            return IsTerminal(GetZombieInfection(pawn));
        }

        public static bool IsTerminal(Hediff infection)
        {
            return infection != null
                && infection.def == ZombieDefOf.CZH_ZombieSickness
                && infection.Severity >= TerminalSeverityThreshold;
        }

        public static bool CanCureZombieInfection(Pawn pawn)
        {
            Hediff infection = GetZombieInfection(pawn);
            return infection != null && !IsTerminal(infection) && !HasReanimatedState(pawn);
        }

        public static bool CanReanimateFromReanimatedState(Pawn pawn)
        {
            return pawn != null
                && HasReanimatedState(pawn)
                && !ZombieRulesUtility.HasHeadDamageOrDestruction(pawn)
                && !IsSkullMissing(pawn);
        }

        public static bool IsFullyReanimated(Pawn pawn)
        {
            return HasReanimatedState(pawn);
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
            if (infection.Severity < TerminalSeverityThreshold)
            {
                return;
            }

            infection.Severity = Mathf.Clamp(infection.Severity, TerminalSeverityThreshold, 0.999f);

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
                if (!pawn.Dead)
                {
                    pawn.Kill(null);
                }
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
                component?.ClearInfectionReanimation(ResolveCorpse(pawn));
                return;
            }

            if (!pawn.RaceProps.Humanlike)
            {
                return;
            }

            Corpse corpse = ResolveCorpse(pawn);
            if (corpse == null)
            {
                return;
            }

            if (HasReanimatedState(pawn))
            {
                component?.ScheduleInfectionReanimation(corpse, fixedDelayTicks: GenDate.TicksPerHour);
                infection.Severity = 1f;
                return;
            }

            component?.ScheduleInfectionReanimation(corpse);

            if (advanceSeverity)
            {
                if (component != null
                    && component.TryGetInfectionReanimationWindow(corpse, out int startTick, out int wakeTick)
                    && Find.TickManager != null
                    && wakeTick > startTick)
                {
                    float startSeverity = Mathf.Clamp01(infection.Severity);
                    if (component.TryGetInfectionReanimationStartSeverity(corpse, out float storedStartSeverity))
                    {
                        startSeverity = Mathf.Clamp01(storedStartSeverity);
                    }

                    float progress = Mathf.InverseLerp(startTick, wakeTick, Find.TickManager.TicksGame);
                    infection.Severity = Mathf.Max(infection.Severity, Mathf.Lerp(startSeverity, 1f, progress));
                }
                else
                {
                    infection.Severity = Math.Min(1f, infection.Severity + GetDeadInfectionSeverityPerTickStep(infection.Severity));
                }
            }

            infection.Severity = Mathf.Clamp(infection.Severity, InitialInfectionSeverity, 1f);
        }

        public static bool PromoteDeadInfectionToReanimated(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            Hediff infection = GetZombieInfection(pawn);
            if (infection == null || infection.Severity < 0.999f)
            {
                return false;
            }

            Corpse corpse = ResolveCorpse(pawn);
            if (corpse == null || corpse.Destroyed)
            {
                return false;
            }

            infection.Severity = 1f;
            ApplyReanimatedState(pawn);
            component?.ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
            return true;
        }

        public static bool TryTurnDeadInfectedPawn(Pawn pawn, ZombieGameComponent component)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            Corpse corpse = ResolveCorpse(pawn);
            if (corpse == null || !HasReanimatedState(pawn))
            {
                return false;
            }

            bool wasColonist = ShouldBecomeLurkerAfterInfection(pawn, component);
            Name preservedName = wasColonist ? pawn.Name : null;
            component?.ClearInfectionReanimation(corpse);

            if (!ZombieUtility.TryResurrectZombie(pawn))
            {
                component?.ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
                return false;
            }

            Hediff lingeringInfection = GetZombieInfection(pawn);
            if (lingeringInfection != null)
            {
                lingeringInfection.Severity = 1f;
            }

            if (wasColonist)
            {
                PawnKindDef lurkerKind = ZombieKindSelector.GetKindForVariant(ZombieVariant.Lurker, pawn.MapHeld);
                ZombiePawnFactory.ConvertExistingPawnToZombie(pawn, lurkerKind, Faction.OfPlayer, preserveName: true, preserveSkills: true, preserveRelations: true, initialSpawn: false);
                if (preservedName != null)
                {
                    ZombiePawnFactory.TrySetPawnName(pawn, preservedName);
                }

                ApplyReanimatedState(pawn);
                ZombieLurkerUtility.EnsureColonyLurkerState(pawn, emergencyStabilize: true, stopCurrentJobs: true);
                ZombieFeedbackUtility.SendZombieTurnMessage(pawn, becameLurker: true);
                return true;
            }

            PawnKindDef randomKind = ZombieKindSelector.GetRandomKind(pawn.MapHeld);
            ZombiePawnFactory.ConvertExistingPawnToZombie(pawn, randomKind, ZombieFactionUtility.GetOrCreateZombieFaction(), preserveName: false, preserveSkills: false, preserveRelations: false, initialSpawn: false);
            ApplyReanimatedState(pawn);
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

        private static float GetDeadInfectionSeverityPerTickStep(float currentSeverity)
        {
            float clampedSeverity = Mathf.Clamp01(currentSeverity);
            float severityToGain = Mathf.Max(0.001f, 1f - clampedSeverity);
            float totalSteps = (3f * GenDate.TicksPerHour) / InfectionTickInterval;
            return severityToGain / Mathf.Max(1f, totalSteps);
        }

        public static void ApplyReanimatedState(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            Hediff existing = GetReanimatedState(pawn);
            if (existing != null)
            {
                existing.Severity = 1f;
                return;
            }

            try
            {
                Hediff hediff = HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieReanimated, pawn);
                hediff.Severity = 1f;
                pawn.health.AddHediff(hediff);
            }
            catch
            {
            }
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

            if (infection.def == ZombieDefOf.CZH_ZombieReanimated)
            {
                return 1f;
            }

            float severityToGain = Mathf.Max(0.01f, 1f - InitialInfectionSeverity);
            return Mathf.Clamp01((infection.Severity - InitialInfectionSeverity) / severityToGain);
        }

        public static float GetInfectionCompletion(Pawn pawn)
        {
            return GetInfectionCompletion(GetVisibleInfectionState(pawn));
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

        private static Corpse ResolveCorpse(Pawn pawn)
        {
            if (pawn?.Corpse != null && !pawn.Corpse.Destroyed)
            {
                return pawn.Corpse;
            }

            Map map = pawn?.MapHeld;
            if (map != null && pawn.PositionHeld.IsValid && pawn.PositionHeld.InBounds(map))
            {
                Corpse localCorpse = map.thingGrid?.ThingsAt(pawn.PositionHeld)?.OfType<Corpse>()?.FirstOrDefault(c => c.InnerPawn == pawn);
                if (localCorpse != null && !localCorpse.Destroyed)
                {
                    return localCorpse;
                }
            }

            foreach (Map searchMap in Find.Maps)
            {
                Corpse found = searchMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn);
                if (found != null && !found.Destroyed)
                {
                    return found;
                }
            }

            return null;
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
