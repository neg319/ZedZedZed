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
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return null;
            }

            var infections = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieSickness)
                .ToList();

            if (infections.Count == 0)
            {
                return null;
            }

            Hediff primary = infections
                .OrderByDescending(hediff => hediff.Severity)
                .ThenByDescending(hediff => hediff.Part != null ? 1 : 0)
                .First();

            if (infections.Count > 1)
            {
                float strongestSeverity = infections.Max(hediff => hediff.Severity);
                primary.Severity = Mathf.Clamp(Mathf.Max(primary.Severity, strongestSeverity), InitialInfectionSeverity, 1f);

                foreach (Hediff extra in infections)
                {
                    if (extra == null || extra == primary)
                    {
                        continue;
                    }

                    try
                    {
                        pawn.health.RemoveHediff(extra);
                    }
                    catch
                    {
                    }
                }
            }

            return primary;
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
            PawnKindDef newKind = wasColonist
                ? ZombieKindSelector.GetKindForVariant(ZombieVariant.Lurker, corpse.MapHeld)
                : ZombieKindSelector.GetRandomKind(corpse.MapHeld);
            Faction desiredFaction = wasColonist ? Faction.OfPlayer : ZombieFactionUtility.GetOrCreateZombieFaction();

            if (!ZombiePawnFactory.TrySpawnReanimatedPawnFromCorpse(corpse, newKind, desiredFaction, preserveName: wasColonist, preserveSkills: wasColonist, preserveRelations: wasColonist, out Pawn risenPawn))
            {
                component?.ScheduleInfectionReanimation(corpse, forceReschedule: true);
                return false;
            }

            component?.ClearInfectionHeadFatal(pawn);
            component?.ClearInfectionShouldBecomeLurker(pawn);
            component?.ClearInfectionReanimation(corpse);
            component?.ClearDeadInfectedCorpse(corpse);
            ApplyReanimatedState(risenPawn);

            if (wasColonist)
            {
                ZombieLurkerUtility.EnsureColonyLurkerState(risenPawn, emergencyStabilize: true, stopCurrentJobs: true);
                ZombieFeedbackUtility.SendZombieTurnMessage(risenPawn, becameLurker: true);
                return true;
            }

            ZombieUtility.EnsureZombieAggression(risenPawn);
            ZombieFeedbackUtility.SendZombieTurnMessage(risenPawn, becameLurker: false);
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
            if (pawn?.health?.hediffSet?.hediffs != null)
            {
                foreach (Hediff infection in pawn.health.hediffSet.hediffs
                    .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieSickness)
                    .ToList())
                {
                    try
                    {
                        pawn.health.RemoveHediff(infection);
                    }
                    catch
                    {
                    }
                }
            }

            component?.ClearInfectionHeadFatal(pawn);
            component?.ClearInfectionShouldBecomeLurker(pawn);
        }

        public static Hediff EnsureZombieInfection(Pawn pawn, float severity)
        {
            return EnsureZombieInfection(pawn, severity, null);
        }

        public static Hediff EnsureZombieInfection(Pawn pawn, float severity, BodyPartRecord part)
        {
            if (pawn?.health == null)
            {
                return null;
            }

            Hediff infection = GetZombieInfection(pawn);
            if (infection == null)
            {
                try
                {
                    infection = part != null
                        ? HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieSickness, pawn, part)
                        : HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieSickness, pawn);
                    pawn.health.AddHediff(infection);
                }
                catch
                {
                    return null;
                }
            }

            infection.Severity = Mathf.Clamp(severity, InitialInfectionSeverity, 1f);
            return infection;
        }


        public static bool IsZombieBiteDamage(DamageInfo dinfo)
        {
            DamageDef damageDef = dinfo.Def;
            if (damageDef == null)
            {
                return false;
            }

            if (damageDef == DamageDefOf.Bite)
            {
                return true;
            }

            string defName = damageDef.defName ?? string.Empty;
            return string.Equals(defName, "Bite", StringComparison.OrdinalIgnoreCase);
        }

        public static float GetZombieBiteInfectionChance(Pawn attacker)
        {
            if (attacker == null || !ZombieUtility.IsZombie(attacker))
            {
                return 0f;
            }

            if (ZombieUtility.IsVariant(attacker, ZombieVariant.Sick))
            {
                return 0.05f;
            }

            if (ZombieUtility.IsVariant(attacker, ZombieVariant.Brute) || ZombieUtility.IsVariant(attacker, ZombieVariant.Drowned))
            {
                return 0.035f;
            }

            if (ZombieUtility.IsVariant(attacker, ZombieVariant.Biter) || ZombieUtility.IsVariant(attacker, ZombieVariant.Boomer))
            {
                return 0.02f;
            }

            if (ZombieUtility.IsVariant(attacker, ZombieVariant.Runt) || ZombieUtility.IsVariant(attacker, ZombieVariant.Grabber))
            {
                return 0.015f;
            }

            return 0.02f;
        }

        public static BodyPartRecord ResolveAmputationFriendlyInfectionPart(Pawn pawn, BodyPartRecord hitPart)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            BodyPartRecord bestPart = null;
            int bestScore = 0;

            for (BodyPartRecord current = hitPart; current != null; current = current.parent)
            {
                int score = GetAmputationFriendlyPartScore(current);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPart = current;
                }
            }

            return bestScore > 0 ? bestPart : null;
        }

        private static int GetAmputationFriendlyPartScore(BodyPartRecord part)
        {
            if (part?.def == null)
            {
                return 0;
            }

            string name = ((part.def.defName ?? string.Empty) + " " + (part.Label ?? string.Empty)).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name))
            {
                return 0;
            }

            if (name.Contains("arm") || name.Contains("leg") || name.Contains("hand") || name.Contains("foot"))
            {
                return 5;
            }

            if (name.Contains("shoulder") || name.Contains("clavicle") || name.Contains("femur") || name.Contains("tibia") || name.Contains("fibula") || name.Contains("humerus") || name.Contains("radius") || name.Contains("ulna"))
            {
                return 4;
            }

            if (name.Contains("finger") || name.Contains("thumb") || name.Contains("toe"))
            {
                return 2;
            }

            return 0;
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
