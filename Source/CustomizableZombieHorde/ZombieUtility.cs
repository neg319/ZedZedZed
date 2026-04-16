using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieUtility
    {
        private static readonly HashSet<BodyPartDef> LimbPartDefs = new HashSet<BodyPartDef>
        {
            BodyPartDefOf.Arm,
            BodyPartDefOf.Leg
        };

        public static bool IsZombie(Pawn pawn)
        {
            return ZombieRulesUtility.IsZombie(pawn);
        }

        public static ZombieVariant GetVariant(Pawn pawn)
        {
            return ZombieVariantUtility.GetVariant(pawn);
        }

        public static bool IsVariant(Pawn pawn, ZombieVariant variant)
        {
            return IsZombie(pawn) && GetVariant(pawn) == variant;
        }

        public static bool ShouldSpawnAsSkeletonBiter(Pawn pawn)
        {
            return pawn != null && IsVariant(pawn, ZombieVariant.Biter) && Mathf.Abs(pawn.thingIDNumber) % 20 == 0;
        }

        public static bool IsSkeletonBiter(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null
                && IsVariant(pawn, ZombieVariant.Biter)
                && pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSkeletonBiter);
        }

        public static void EnsureZombieInfectionState(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn?.health == null)
            {
                return;
            }

            ZombieInfectionUtility.ApplyReanimatedState(pawn);
            ZombieInfectionUtility.EnsureZombieInfection(pawn, ZombieInfectionUtility.InitialInfectionSeverity);
            ZombieInfectionUtility.StabilizeZombieInfection(pawn);
            ClearLegacyZombieFeignDeath(pawn);
            NormalizeCoreZombieState(pawn);
        }

        public static void ClearLegacyZombieFeignDeath(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            Hediff staleFeignDeath = pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieFeignDeath);
            if (staleFeignDeath == null)
            {
                return;
            }

            try
            {
                pawn.health.RemoveHediff(staleFeignDeath);
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
        }



        private static void RemoveZombieInfectionHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || ZombieDefOf.CZH_ZombieSickness == null)
            {
                return;
            }

            Hediff infection = pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieSickness);
            if (infection == null)
            {
                return;
            }

            try
            {
                pawn.health.RemoveHediff(infection);
            }
            catch
            {
            }
        }

        public static void NormalizeCoreZombieState(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn == null)
            {
                return;
            }

            ForceZombieFaction(pawn);
            ClearZombieGuestState(pawn);
            EnsureEmotionlessZombie(pawn);
            ClearZombieIdeoligion(pawn);
            EnsureZombieCannibalTrait(pawn);
        }

        private static void ForceZombieFaction(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (ZombieLurkerUtility.IsLurker(pawn) || IsPlayerAlignedZombie(pawn))
            {
                return;
            }

            Faction zombieFaction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (zombieFaction == null)
            {
                return;
            }

            if (pawn.Faction != zombieFaction)
            {
                try
                {
                    pawn.SetFaction(zombieFaction);
                }
                catch
                {
                    try
                    {
                        AccessTools.Method(typeof(Pawn), "SetFaction", new[] { typeof(Faction), typeof(Pawn) })?.Invoke(pawn, new object[] { zombieFaction, null });
                    }
                    catch
                    {
                        try
                        {
                            pawn.SetFactionDirect(zombieFaction);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        public static void EnsureEmotionlessZombie(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn?.needs == null || pawn.needs.AllNeeds == null || ZombieLurkerUtility.IsLurker(pawn) || IsPlayerAlignedZombie(pawn))
            {
                return;
            }

            NeedDef moodDef = DefDatabase<NeedDef>.GetNamedSilentFail("Mood");
            NeedDef joyDef = DefDatabase<NeedDef>.GetNamedSilentFail("Joy");

            for (int i = pawn.needs.AllNeeds.Count - 1; i >= 0; i--)
            {
                Need need = pawn.needs.AllNeeds[i];
                if (need?.def == moodDef || need?.def == joyDef)
                {
                    pawn.needs.AllNeeds.RemoveAt(i);
                }
            }
        }

        private static void ClearZombieGuestState(Pawn pawn)
        {
            if (pawn == null || ZombieLurkerUtility.IsLurker(pawn) || IsPlayerAlignedZombie(pawn))
            {
                return;
            }

            try
            {
                FieldInfo guestField = AccessTools.Field(typeof(Pawn), "guest") ?? AccessTools.Field(typeof(Pawn), "guestInt");
                guestField?.SetValue(pawn, null);
            }
            catch
            {
            }

            try
            {
                object guest = pawn.guest;
                if (guest != null)
                {
                    AccessTools.Field(guest.GetType(), "hostFactionInt")?.SetValue(guest, null);
                    AccessTools.Field(guest.GetType(), "hostFaction")?.SetValue(guest, null);
                }
            }
            catch
            {
            }
        }

        private static void EnsureZombieCannibalTrait(Pawn pawn)
        {
            TraitDef cannibal = DefDatabase<TraitDef>.GetNamedSilentFail("Cannibal");
            if (cannibal == null)
            {
                return;
            }

            ZombieTraitUtility.EnsureTrait(pawn, cannibal);
        }

        private static void ClearZombieIdeoligion(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                object tracker = Traverse.Create(pawn).Field("ideo").GetValue();
                if (tracker == null)
                {
                    tracker = Traverse.Create(pawn).Field("ideoTracker").GetValue();
                }
                if (tracker == null)
                {
                    tracker = Traverse.Create(pawn).Property("ideo").GetValue();
                }
                if (tracker == null)
                {
                    tracker = Traverse.Create(pawn).Property("IdeoTracker").GetValue();
                }

                if (tracker == null)
                {
                    return;
                }

                MethodInfo setIdeoOneArg = AccessTools.Method(tracker.GetType(), "SetIdeo", new[] { typeof(Ideo) });
                if (setIdeoOneArg != null)
                {
                    setIdeoOneArg.Invoke(tracker, new object[] { null });
                }
                else
                {
                    MethodInfo setIdeoTwoArgs = AccessTools.Method(tracker.GetType(), "SetIdeo", new[] { typeof(Ideo), typeof(bool) });
                    if (setIdeoTwoArgs != null)
                    {
                        setIdeoTwoArgs.Invoke(tracker, new object[] { null, false });
                    }
                }

                AccessTools.Field(tracker.GetType(), "ideo")?.SetValue(tracker, null);
                AccessTools.Field(tracker.GetType(), "ideoInt")?.SetValue(tracker, null);
                AccessTools.Field(tracker.GetType(), "certainty")?.SetValue(tracker, 0f);
                AccessTools.Field(tracker.GetType(), "certaintyInt")?.SetValue(tracker, 0f);
            }
            catch
            {
            }
        }
        public static bool StabilizeFreshZombieForSpawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            EnsureZombieInfectionState(pawn);
            RemoveSpawnCriticalHeadDamage(pawn);
            TryRecoverFromSpawnIncap(pawn);
            return !pawn.Dead;
        }

        private static void RemoveSpawnCriticalHeadDamage(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                if (hediff == null)
                {
                    continue;
                }

                BodyPartRecord part = hediff.Part;
                if (!ZombieInfectionUtility.IsHeadOrChildPart(part, pawn))
                {
                    continue;
                }

                try
                {
                    if (hediff is Hediff_MissingPart || hediff is Hediff_Injury)
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                }
                catch
                {
                }
            }
        }

        public static float GetZombieIncomingDamageMultiplier(Pawn pawn)
        {
            if (!IsZombie(pawn))
            {
                return 1f;
            }

            if (IsSkeletonBiter(pawn))
            {
                return 5.5f;
            }

            if (IsVariant(pawn, ZombieVariant.Biter))
            {
                return 14f;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 6f;
            }

            if (IsVariant(pawn, ZombieVariant.Boomer))
            {
                return 10f;
            }

            if (IsVariant(pawn, ZombieVariant.Sick))
            {
                return 3f;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return 5.5f;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return 1.5f;
            }

            if (IsVariant(pawn, ZombieVariant.Grabber))
            {
                return 4f;
            }

            return 2.2f;
        }

        public static float GetZombieOutgoingDamageMultiplier(Pawn attacker, Pawn victim)
        {
            if (!IsZombie(attacker) || victim == null || IsZombie(victim))
            {
                return 1f;
            }

            if (IsVariant(attacker, ZombieVariant.Runt))
            {
                return 0.12f;
            }

            if (IsSkeletonBiter(attacker))
            {
                return 0.20f;
            }

            if (IsVariant(attacker, ZombieVariant.Biter))
            {
                return 0.16f;
            }

            if (IsVariant(attacker, ZombieVariant.Boomer))
            {
                return 0.12f;
            }

            if (IsVariant(attacker, ZombieVariant.Sick))
            {
                return 0.16f;
            }

            if (IsVariant(attacker, ZombieVariant.Drowned))
            {
                return 0.18f;
            }

            if (IsVariant(attacker, ZombieVariant.Grabber))
            {
                return 0.14f;
            }

            if (IsVariant(attacker, ZombieVariant.Brute))
            {
                return 0.28f;
            }

            return 0.20f;
        }

        public static float GetZombieMeleeMissChance(Pawn attacker, Pawn victim)
        {
            if (!IsZombie(attacker) || victim == null || victim.Dead || victim.Downed || IsZombie(victim))
            {
                return 0f;
            }

            float missChance = 0.16f;
            float dodgeChance = 0f;

            try
            {
                dodgeChance = Mathf.Max(0f, victim.GetStatValue(StatDefOf.MeleeDodgeChance, true));
            }
            catch
            {
            }

            missChance += dodgeChance * 0.60f;

            if (victim.stances?.stunner?.Stunned == true)
            {
                missChance -= 0.10f;
            }

            if (IsVariant(attacker, ZombieVariant.Runt))
            {
                missChance += 0.12f;
            }
            else if (IsSkeletonBiter(attacker))
            {
                missChance += 0.06f;
            }
            else if (IsVariant(attacker, ZombieVariant.Biter))
            {
                missChance += 0.10f;
            }
            else if (IsVariant(attacker, ZombieVariant.Grabber))
            {
                missChance += 0.04f;
            }
            else if (IsVariant(attacker, ZombieVariant.Brute))
            {
                missChance -= 0.08f;
            }

            return Mathf.Clamp(missChance, 0f, 0.60f);
        }

        public static BodyPartRecord GetHeadPart(Pawn pawn)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || pawn.health?.hediffSet == null)
            {
                return null;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part?.def == BodyPartDefOf.Head && !pawn.health.hediffSet.PartIsMissing(part))
                {
                    return part;
                }
            }

            return null;
        }

        public static BodyPartRecord FindBodyPart(Pawn pawn, string defName)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || string.IsNullOrWhiteSpace(defName))
            {
                return null;
            }

            return pawn.RaceProps.body.AllParts.FirstOrDefault(part => string.Equals(part?.def?.defName, defName, StringComparison.OrdinalIgnoreCase));
        }

        public static BodyPartRecord GetBrainPart(Pawn pawn)
        {
            return FindBodyPart(pawn, "Brain");
        }


        public static bool HasDestroyedBrain(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }

            if (pawn.health.hediffSet.hediffs.Any(hediff => hediff?.def == ZombieDefOf.CZH_ZombieBrainDestroyed))
            {
                return true;
            }

            BodyPartRecord brain = GetBrainPart(pawn);
            if (brain != null)
            {
                try
                {
                    if (pawn.health.hediffSet.PartIsMissing(brain))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool IsSafeMarkerPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null)
            {
                return false;
            }

            try
            {
                return !pawn.health.hediffSet.PartIsMissing(part);
            }
            catch
            {
                return false;
            }
        }

        private static BodyPartRecord ResolveBrainMarkerPart(Pawn victim, DamageInfo sourceDamage = default(DamageInfo))
        {
            if (victim?.health?.hediffSet == null)
            {
                return null;
            }

            BodyPartRecord brain = GetBrainPart(victim);
            if (IsSafeMarkerPart(victim, brain))
            {
                return brain;
            }

            BodyPartRecord head = GetHeadPart(victim);
            if (IsSafeMarkerPart(victim, head))
            {
                return head;
            }

            if (IsSafeMarkerPart(victim, sourceDamage.HitPart))
            {
                return sourceDamage.HitPart;
            }

            return null;
        }

        public static void EnsureZombieBrainDestroyed(Pawn victim, Pawn attacker, DamageInfo sourceDamage = default(DamageInfo))
        {
            if (!IsZombie(victim) || victim?.health?.hediffSet == null || victim.Destroyed)
            {
                return;
            }

            BodyPartRecord markerPart = ResolveBrainMarkerPart(victim, sourceDamage);
            Current.Game?.GetComponent<ZombieGameComponent>()?.MarkInfectionHeadFatal(victim);

            try
            {
                bool hasMarker = victim.health.hediffSet.hediffs.Any(hediff => hediff?.def == ZombieDefOf.CZH_ZombieBrainDestroyed && (markerPart == null || hediff.Part == markerPart));
                if (!hasMarker)
                {
                    Hediff brainDestroyed = HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieBrainDestroyed, victim, markerPart);
                    victim.health.AddHediff(brainDestroyed, markerPart);
                }
            }
            catch
            {
            }
        }

        public static float GetBrainDestroyChance(Pawn victim, Pawn attacker, DamageInfo dinfo, float totalDamageDealt)
        {
            if (!IsZombie(victim) || victim.Dead)
            {
                return 0f;
            }

            float chance = 0.14f;

            if (attacker != null && ZombieTraitUtility.IsRangedAttack(attacker, dinfo))
            {
                chance += 0.08f;
            }

            if (dinfo.Weapon?.IsRangedWeapon == true)
            {
                chance += 0.03f;
            }

            chance += Mathf.Clamp(totalDamageDealt * 0.0125f, 0f, 0.15f);

            if (IsVariant(victim, ZombieVariant.Brute))
            {
                chance *= 0.65f;
            }
            else if (IsVariant(victim, ZombieVariant.Grabber))
            {
                chance *= 0.90f;
            }
            else if (IsVariant(victim, ZombieVariant.Runt))
            {
                chance *= 1.10f;
            }
            else if (IsSkeletonBiter(victim))
            {
                chance *= 1.05f;
            }

            return Mathf.Clamp(chance, 0f, 0.45f);
        }

        public static void DestroyZombieBrain(Pawn victim, Pawn attacker, DamageInfo sourceDamage = default(DamageInfo), bool forceEvenIfAlreadyDead = false)
        {
            if (!IsZombie(victim) || victim == null || victim.Destroyed)
            {
                return;
            }

            BodyPartRecord fatalPart = ResolveBrainMarkerPart(victim, sourceDamage) ?? GetHeadPart(victim) ?? sourceDamage.HitPart;

            if (victim.Dead)
            {
                EnsureZombieBrainDestroyed(victim, attacker, sourceDamage);
                return;
            }

            EnsureZombieBrainDestroyed(victim, attacker, sourceDamage);

            try
            {
                victim.Kill(new DamageInfo(sourceDamage.Def ?? DamageDefOf.Bullet, 999f, 999f, -1f, attacker, fatalPart));
                return;
            }
            catch
            {
            }

            try
            {
                victim.Kill(new DamageInfo(DamageDefOf.Cut, 999f, 999f, -1f, attacker, fatalPart));
            }
            catch
            {
            }
        }

        private static void TryAddBrainTrauma(Pawn victim, Pawn attacker, BodyPartRecord fatalPart, DamageInfo sourceDamage)
        {
            if (victim?.health == null || victim.Destroyed)
            {
                return;
            }

            EnsureZombieBrainDestroyed(victim, attacker, sourceDamage);
        }

        public static bool ShouldZombiesIgnore(Pawn pawn)
        {
            return ZombieRulesUtility.IsIgnoredByZombies(pawn);
        }

        public static bool IsPlayerAlignedZombie(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn == null)
            {
                return false;
            }

            if (ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                return true;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                return true;
            }

            try
            {
                return pawn.IsSlaveOfColony;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsColonyAlly(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer)
            {
                return true;
            }

            try
            {
                if (pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool IsEnemyOfColony(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (IsColonyAlly(pawn) || IsPlayerAlignedZombie(pawn) || ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                return false;
            }

            if (IsZombie(pawn))
            {
                return true;
            }

            Faction faction = pawn.Faction;
            return faction != null && !faction.IsPlayer && faction.HostileTo(Faction.OfPlayer);
        }

        public static bool ShouldZombieIgnoreTarget(Pawn zombie, Pawn target)
        {
            if (target == null)
            {
                return true;
            }

            if (IsPlayerAlignedZombie(zombie))
            {
                if (IsColonyAlly(target) || IsPlayerAlignedZombie(target) || ZombieLurkerUtility.IsPassiveLurker(target))
                {
                    return true;
                }

                return !IsEnemyOfColony(target);
            }

            return ShouldZombiesIgnore(target);
        }


        public static bool ShouldSuppressZombieThought(ThoughtDef thoughtDef)
        {
            if (thoughtDef == null)
            {
                return false;
            }

            string text = ((thoughtDef.defName ?? string.Empty) + " " + (thoughtDef.label ?? string.Empty) + " " + (thoughtDef.workerClass?.Name ?? string.Empty)).ToLowerInvariant();
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.Contains("pain")
                || text.Contains("naked")
                || text.Contains("nudity")
                || text.Contains("nude")
                || text.Contains("filth")
                || text.Contains("dirty");
        }

        public static bool IsUnderColonyRestraint(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            try
            {
                if (pawn.IsPrisonerOfColony)
                {
                    return true;
                }
            }
            catch
            {
            }

            return pawn.Downed || pawn.CurJob?.def == JobDefOf.LayDown;
        }

        public static bool HasHeadDamageOrDestruction(Pawn pawn)
        {
            return ZombieRulesUtility.HasHeadDamageOrDestruction(pawn);
        }

        public static bool CanReanimate(Pawn pawn)
        {
            return ZombieRulesUtility.CanReanimate(pawn);
        }

        public static IEnumerable<BodyPartRecord> GetZombieLimbParts(Pawn pawn)
        {
            if (pawn?.RaceProps?.body?.AllParts == null)
            {
                yield break;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part != null && LimbPartDefs.Contains(part.def))
                {
                    yield return part;
                }
            }
        }

        public static void ApplyLimbDecay(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            List<BodyPartRecord> candidateParts = GetZombieLimbParts(pawn)
                .Where(part => part != null && !pawn.health.hediffSet.PartIsMissing(part))
                .ToList();
            if (candidateParts.Count == 0)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                candidateParts = candidateParts.Where(part => part.def == BodyPartDefOf.Leg).ToList();
            }

            int decayCount = GetInitialDecayPartCount(pawn, candidateParts.Count);
            foreach (BodyPartRecord part in candidateParts.InRandomOrder().Take(decayCount))
            {
                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieLimbDecay, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieLimbDecay, part);
                }
            }
        }

        public static void ApplyRuntLegDamage(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Runt) || pawn?.health?.hediffSet == null || pawn.RaceProps?.body?.AllParts == null)
            {
                return;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part?.def != BodyPartDefOf.Leg || pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }

                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieOpenWound, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieOpenWound, part);
                }

                Hediff_Injury existingInjury = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .FirstOrDefault(injury => injury.Part == part && !injury.IsPermanent() && injury.Severity >= 12f);
                if (existingInjury != null)
                {
                    continue;
                }

                try
                {
                    Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, part);
                    if (injury != null)
                    {
                        injury.Severity = 12f;
                        pawn.health.AddHediff(injury, part);
                    }
                }
                catch
                {
                }
            }
        }

        public static void ApplyVisibleDecayInjuries(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            List<BodyPartRecord> parts = GetZombieLimbParts(pawn)
                .Where(part => part != null && !pawn.health.hediffSet.PartIsMissing(part))
                .InRandomOrder()
                .ToList();

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                parts = parts.Where(part => part.def == BodyPartDefOf.Leg).ToList();
            }

            int woundCount = Math.Min(parts.Count, GetInitialVisibleWoundCount(pawn));
            for (int i = 0; i < woundCount; i++)
            {
                BodyPartRecord part = parts[i];
                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieOpenWound, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieOpenWound, part);
                }

                TryAddStartingDecayDamage(pawn, part, torsoSeverity: false);
            }

            BodyPartRecord torso = pawn.RaceProps?.body?.corePart;
            if (torso != null && !pawn.health.hediffSet.PartIsMissing(torso) && Rand.Chance(GetInitialTorsoWoundChance(pawn)))
            {
                TryAddStartingDecayDamage(pawn, torso, torsoSeverity: true);
            }
        }

        private static void TryAddStartingDecayDamage(Pawn pawn, BodyPartRecord part, bool torsoSeverity)
        {
            if (pawn?.health == null || part == null)
            {
                return;
            }

            try
            {
                Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, part);
                if (injury == null)
                {
                    return;
                }

                float severity = torsoSeverity ? Rand.Range(0.8f, 1.8f) : Rand.Range(0.35f, 1.10f);
                if (IsVariant(pawn, ZombieVariant.Brute))
                {
                    severity *= 0.30f;
                }
                else if (IsVariant(pawn, ZombieVariant.Runt))
                {
                    severity *= 0.85f;
                }

                injury.Severity = severity;
                pawn.health.AddHediff(injury, part);
            }
            catch
            {
            }
        }

        private static int GetInitialDecayPartCount(Pawn pawn, int availableCount)
        {
            if (availableCount <= 0)
            {
                return 0;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return Math.Min(availableCount, 2);
            }

            if (IsVariant(pawn, ZombieVariant.Brute) || IsVariant(pawn, ZombieVariant.Grabber))
            {
                return Math.Min(availableCount, 2);
            }

            return 1;
        }

        private static int GetInitialVisibleWoundCount(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 1;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return Rand.Chance(0.50f) ? 1 : 0;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return Rand.Chance(0.35f) ? 1 : 0;
            }

            return Rand.Chance(0.60f) ? 1 : 0;
        }

        private static float GetInitialTorsoWoundChance(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 0.15f;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return 0.20f;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return 0.30f;
            }

            return 0.45f;
        }

        public static void NormalizeZombieCosmeticDamage(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            // Cosmetic wound normalization is for spawn generation only.
            // Living zombies should keep the wounds they earn in combat.
        }

        private static void TrimExcessLimbDecay(Pawn pawn)
        {
            List<Hediff> limbDecay = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieLimbDecay && hediff.Part != null)
                .ToList();
            if (limbDecay.Count == 0)
            {
                return;
            }

            IEnumerable<Hediff> allowed = limbDecay.Where(hediff => LimbPartDefs.Contains(hediff.Part.def));
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                allowed = allowed.Where(hediff => hediff.Part.def == BodyPartDefOf.Leg);
            }

            int keepCount = GetInitialDecayPartCount(pawn, allowed.Count());
            HashSet<BodyPartRecord> keepParts = new HashSet<BodyPartRecord>(allowed
                .OrderByDescending(hediff => GetCosmeticPartPriority(pawn, hediff.Part))
                .Take(keepCount)
                .Select(hediff => hediff.Part));

            foreach (Hediff hediff in limbDecay)
            {
                if (!keepParts.Contains(hediff.Part))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        private static void TrimExcessOpenWounds(Pawn pawn)
        {
            List<Hediff> openWounds = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieOpenWound && hediff.Part != null)
                .ToList();
            if (openWounds.Count == 0)
            {
                return;
            }

            IEnumerable<Hediff> allowed = openWounds.Where(hediff => LimbPartDefs.Contains(hediff.Part.def));
            int keepCount = IsVariant(pawn, ZombieVariant.Runt) ? 2 : 1;
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                allowed = allowed.Where(hediff => hediff.Part.def == BodyPartDefOf.Leg);
            }

            HashSet<BodyPartRecord> keepParts = new HashSet<BodyPartRecord>(allowed
                .OrderByDescending(hediff => GetCosmeticPartPriority(pawn, hediff.Part))
                .Take(keepCount)
                .Select(hediff => hediff.Part));

            foreach (Hediff hediff in openWounds)
            {
                if (keepParts.Contains(hediff.Part))
                {
                    continue;
                }

                BodyPartRecord part = hediff.Part;
                pawn.health.RemoveHediff(hediff);
                RemoveCosmeticStartingInjuries(pawn, part);
            }
        }

        private static float GetCosmeticPartPriority(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null)
            {
                return 0f;
            }

            float injurySeverity = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.Part == part && !injury.IsPermanent())
                .Sum(injury => injury.Severity);
            float priority = injurySeverity;
            if (part.def == BodyPartDefOf.Leg)
            {
                priority += 0.25f;
            }

            return priority;
        }

        private static void RemoveCosmeticStartingInjuries(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null)
            {
                return;
            }

            List<Hediff_Injury> removable = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.Part == part && !injury.IsPermanent() && injury.Severity <= 1.20f)
                .ToList();
            foreach (Hediff_Injury injury in removable)
            {
                pawn.health.RemoveHediff(injury);
            }
        }

        public static void ApplyVariantHediffs(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Biter) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieBiter))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieBiter);
            }

            if (ShouldSpawnAsSkeletonBiter(pawn) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSkeletonBiter))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieSkeletonBiter);
            }

            if (IsVariant(pawn, ZombieVariant.Runt) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRunt))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRunt);
            }

            if (IsVariant(pawn, ZombieVariant.Brute) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieBrute))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieBrute);
            }
        }

        public static void SetZombieDisplayName(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            NameSingle name = new NameSingle(ZombieDefUtility.GetDisplayLabelForPawn(pawn));
            try
            {
                Traverse.Create(pawn).Property("Name").SetValue(name);
                return;
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn).Field("nameInt").SetValue(name);
            }
            catch
            {
            }
        }

        public static void StripAllUsableItems(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.equipment != null)
            {
                try
                {
                    pawn.equipment.DestroyAllEquipment();
                }
                catch
                {
                }
            }

            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    try
                    {
                        thing?.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }

            if (pawn.carryTracker?.innerContainer != null)
            {
                for (int i = pawn.carryTracker.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing thing = pawn.carryTracker.innerContainer[i];
                    try
                    {
                        thing?.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void TrimZombieApparel(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return;
            }

            bool isBrute = IsVariant(pawn, ZombieVariant.Brute);
            List<Apparel> worn = pawn.apparel.WornApparel.ToList();
            List<Apparel> keep = new List<Apparel>();

            int basicTarget = isBrute ? Rand.RangeInclusive(0, 1) : Rand.RangeInclusive(0, 2);
            bool allowRareArmor = !isBrute && Rand.Chance(0.025f);
            bool allowRareHeadwear = Rand.Chance(isBrute ? 0.008f : 0.018f);
            int basicKept = 0;

            foreach (Apparel apparel in worn.InRandomOrder())
            {
                if (apparel == null)
                {
                    continue;
                }

                if (IsZombieArmor(apparel))
                {
                    if (allowRareArmor && keep.All(existing => !IsZombieArmor(existing)))
                    {
                        keep.Add(apparel);
                    }
                    continue;
                }

                if (IsZombieHeadwear(apparel))
                {
                    if (allowRareHeadwear && keep.All(existing => !IsZombieHeadwear(existing)))
                    {
                        keep.Add(apparel);
                    }
                    continue;
                }

                if (basicKept < basicTarget && IsBasicZombieClothing(apparel))
                {
                    keep.Add(apparel);
                    basicKept++;
                }
            }

            foreach (Apparel apparel in worn)
            {
                if (apparel == null || keep.Contains(apparel))
                {
                    continue;
                }

                try
                {
                    pawn.apparel.Remove(apparel);
                }
                catch
                {
                }

                try
                {
                    apparel.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }
        }

        private static bool IsBasicZombieClothing(Apparel apparel)
        {
            if (apparel?.def?.apparel == null)
            {
                return false;
            }

            if (IsZombieArmor(apparel) || IsZombieHeadwear(apparel))
            {
                return false;
            }

            foreach (BodyPartGroupDef group in apparel.def.apparel.bodyPartGroups ?? Enumerable.Empty<BodyPartGroupDef>())
            {
                string name = ((group?.defName ?? string.Empty) + ' ' + (group?.label ?? string.Empty)).ToLowerInvariant();
                if (name.Contains("torso") || name.Contains("legs") || name.Contains("shoulder") || name.Contains("waist"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsZombieHeadwear(Apparel apparel)
        {
            if (apparel?.def?.apparel == null)
            {
                return false;
            }

            foreach (BodyPartGroupDef group in apparel.def.apparel.bodyPartGroups ?? Enumerable.Empty<BodyPartGroupDef>())
            {
                string name = ((group?.defName ?? string.Empty) + ' ' + (group?.label ?? string.Empty)).ToLowerInvariant();
                if (name.Contains("head") || name.Contains("eye") || name.Contains("face") || name.Contains("jaw") || name.Contains("nose"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsZombieArmor(Apparel apparel)
        {
            if (apparel?.def == null)
            {
                return false;
            }

            float armorValue = 0f;
            foreach (StatModifier modifier in apparel.def.statBases ?? Enumerable.Empty<StatModifier>())
            {
                string statName = modifier?.stat?.defName ?? string.Empty;
                if (statName == "ArmorRating_Sharp" || statName == "ArmorRating_Blunt" || statName == "ArmorRating_Heat")
                {
                    armorValue += modifier.value;
                }
            }

            if (armorValue >= 0.55f)
            {
                return true;
            }

            string labelText = ((apparel.def.defName ?? string.Empty) + ' ' + (apparel.def.label ?? string.Empty)).ToLowerInvariant();
            return labelText.Contains("armor") || labelText.Contains("helmet") || labelText.Contains("flak") || labelText.Contains("shield belt");
        }

        public static void NormalizeRareBiterClothing(Pawn pawn, bool initialSpawn)
        {
            if (!initialSpawn || !IsVariant(pawn, ZombieVariant.Biter) || ZombieSpecialUtility.IsBoneBiter(pawn) || pawn?.apparel == null)
            {
                return;
            }

            List<Apparel> worn = pawn.apparel.WornApparel?.ToList() ?? new List<Apparel>();
            foreach (Apparel apparel in worn)
            {
                try
                {
                    pawn.apparel.Remove(apparel);
                }
                catch
                {
                }

                try
                {
                    apparel.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }

            if (!Rand.Chance(0.03f))
            {
                return;
            }

            Apparel apparelToWear = TryCreateRandomBiterClothing(pawn);
            if (apparelToWear == null)
            {
                return;
            }

            try
            {
                pawn.apparel.Wear(apparelToWear, false);
            }
            catch
            {
                try
                {
                    apparelToWear.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }
        }

        private static Apparel TryCreateRandomBiterClothing(Pawn pawn)
        {
            List<ThingDef> candidates = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def?.IsApparel == true
                    && def.apparel != null
                    && !def.apparel.bodyPartGroups.NullOrEmpty()
                    && IsHumanBasicClothingDef(def)
                    && CanZombieWearApparel(pawn, def))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                ThingDef def = candidates.RandomElement();
                ThingDef stuff = null;
                if (def.MadeFromStuff)
                {
                    List<ThingDef> stuffs = DefDatabase<ThingDef>.AllDefsListForReading
                        .Where(thingDef => thingDef != null && thingDef.IsStuff && GenStuff.AllowedStuffsFor(def).Contains(thingDef))
                        .ToList();
                    if (stuffs.Count == 0)
                    {
                        continue;
                    }

                    stuff = stuffs.RandomElement();
                }

                Apparel apparel = null;
                try
                {
                    apparel = ThingMaker.MakeThing(def, stuff) as Apparel;
                }
                catch
                {
                }

                if (apparel == null)
                {
                    continue;
                }

                SetRandomBiterQuality(apparel);
                SetApparelTainted(apparel);
                apparel.HitPoints = Math.Max(1, Mathf.RoundToInt(apparel.MaxHitPoints * Rand.Range(0.25f, 0.85f)));
                return apparel;
            }

            return null;
        }

        private static bool IsHumanBasicClothingDef(ThingDef def)
        {
            if (def?.apparel == null)
            {
                return false;
            }

            float armorValue = 0f;
            foreach (StatModifier modifier in def.statBases ?? Enumerable.Empty<StatModifier>())
            {
                string statName = modifier?.stat?.defName ?? string.Empty;
                if (statName == "ArmorRating_Sharp" || statName == "ArmorRating_Blunt" || statName == "ArmorRating_Heat")
                {
                    armorValue += modifier.value;
                }
            }

            if (armorValue >= 0.35f)
            {
                return false;
            }

            string text = ((def.defName ?? string.Empty) + " " + (def.label ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("armor") || text.Contains("helmet") || text.Contains("flak") || text.Contains("shield belt") || text.Contains("power"))
            {
                return false;
            }

            return def.apparel.bodyPartGroups.Any(group =>
            {
                string name = ((group?.defName ?? string.Empty) + " " + (group?.label ?? string.Empty)).ToLowerInvariant();
                return name.Contains("torso") || name.Contains("legs") || name.Contains("waist") || name.Contains("shoulder");
            });
        }

        private static bool CanZombieWearApparel(Pawn pawn, ThingDef def)
        {
            if (pawn == null || def?.apparel == null)
            {
                return false;
            }

            string text = ((def.defName ?? string.Empty) + " " + (def.label ?? string.Empty)).ToLowerInvariant();
            if (text.Contains("baby") || text.Contains("child") || text.Contains("kid") || text.Contains("romper") || text.Contains("onesie"))
            {
                return false;
            }

            return true;
        }

        private static void SetRandomBiterQuality(Apparel apparel)
        {
            CompQuality qualityComp = apparel?.TryGetComp<CompQuality>();
            if (qualityComp == null)
            {
                return;
            }

            QualityCategory quality;
            float roll = Rand.Value;
            if (roll < 0.28f)
            {
                quality = QualityCategory.Awful;
            }
            else if (roll < 0.60f)
            {
                quality = QualityCategory.Poor;
            }
            else if (roll < 0.86f)
            {
                quality = QualityCategory.Normal;
            }
            else if (roll < 0.96f)
            {
                quality = QualityCategory.Good;
            }
            else
            {
                quality = QualityCategory.Excellent;
            }

            try
            {
                qualityComp.SetQuality(quality, ArtGenerationContext.Outsider);
            }
            catch
            {
            }
        }

        public static void MarkZombieApparelTainted(Pawn pawn, bool degradeApparel)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return;
            }

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel == null)
                {
                    continue;
                }

                SetApparelTainted(apparel);
                if (degradeApparel)
                {
                    int minHitPoints = Math.Max(1, Mathf.RoundToInt(apparel.MaxHitPoints * 0.20f));
                    int maxHitPoints = Math.Max(minHitPoints, Mathf.RoundToInt(apparel.MaxHitPoints * 0.70f));
                    apparel.HitPoints = Math.Min(apparel.HitPoints, Rand.RangeInclusive(minHitPoints, maxHitPoints));
                }
            }
        }

        public static void SetApparelTainted(Apparel apparel)
        {
            if (apparel == null)
            {
                return;
            }

            try
            {
                AccessTools.PropertySetter(typeof(Apparel), "WornByCorpse")?.Invoke(apparel, new object[] { true });
            }
            catch
            {
            }

            try
            {
                Traverse.Create(apparel).Field("wornByCorpseInt").SetValue(true);
            }
            catch
            {
            }
        }

        public static void GiveRunnerSpeedIfRolled(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            float chance = CustomizableZombieHordeMod.Settings?.fastZombieChance ?? 0.02f;
            if (chance > 0f && Rand.Chance(chance) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRunner))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRunner);
            }
        }

        public static void PrepareZombieForReanimation(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Faction == null && !ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                pawn.SetFactionDirect(ZombieFactionUtility.GetOrCreateZombieFaction());
            }

            StripAllUsableItems(pawn);
            TrimZombieApparel(pawn);
            MarkZombieApparelTainted(pawn, degradeApparel: false);
            if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRot))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRot);
            }

            EnsureZombieInfectionState(pawn);
            ApplyVariantHediffs(pawn);
            RefreshDrownedState(pawn);
            MarkPawnGraphicsDirty(pawn);
        }

        public static bool IsWaterCell(IntVec3 cell, Map map)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null)
            {
                return false;
            }

            string terrainName = (terrain.defName ?? string.Empty) + " " + (terrain.label ?? string.Empty);
            return terrainName.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("ocean", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("river", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("marsh", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static bool IsExposedToRain(Pawn pawn)
        {
            return pawn?.Spawned == true
                && pawn.MapHeld != null
                && ZombieSpecialUtility.IsRainActive(pawn.MapHeld)
                && !pawn.PositionHeld.Roofed(pawn.MapHeld);
        }

        public static void RefreshDrownedState(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Drowned) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            bool inWater = pawn.Spawned && IsWaterCell(pawn.Position, pawn.Map);
            bool rainFed = IsExposedToRain(pawn);
            bool hasWater = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedWater);
            bool hasLand = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedLand);
            bool hasRain = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedRain);

            if (inWater || rainFed)
            {
                if (hasLand)
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieDrownedLand));
                }

                if (!hasWater)
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieDrownedWater);
                }
            }
            else
            {
                if (hasWater)
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieDrownedWater));
                }

                if (!hasLand)
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieDrownedLand);
                }
            }

            if (rainFed)
            {
                if (!hasRain)
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieDrownedRain);
                }
            }
            else if (hasRain)
            {
                pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieDrownedRain));
            }

            ApplyDrownedMoodBoost(pawn, inWater, rainFed);
        }

        public static void RefreshNightSpeedBoost(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            Hediff existing = ZombieDefOf.CZH_ZombieNightRush == null
                ? null
                : pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieNightRush);
            bool shouldHaveNightRush = pawn.Spawned && pawn.MapHeld != null && ZombieSpawnHelper.IsNight(pawn.MapHeld);

            if (shouldHaveNightRush)
            {
                if (existing == null && ZombieDefOf.CZH_ZombieNightRush != null)
                {
                    try
                    {
                        pawn.health.AddHediff(ZombieDefOf.CZH_ZombieNightRush);
                    }
                    catch
                    {
                    }
                }
            }
            else if (existing != null)
            {
                try
                {
                    pawn.health.RemoveHediff(existing);
                }
                catch
                {
                }
            }
        }

        public static void HandleDrownedRegeneration(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Drowned) || pawn?.health?.hediffSet == null || !pawn.Spawned)
            {
                return;
            }

            bool inWater = IsWaterCell(pawn.PositionHeld, pawn.MapHeld);
            bool inRain = IsExposedToRain(pawn);
            if ((!inWater && !inRain) || !pawn.IsHashIntervalTick(300))
            {
                return;
            }

            Hediff_Injury injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => !hediff.IsPermanent() && hediff.Part != null && !pawn.health.hediffSet.PartIsMissing(hediff.Part))
                .OrderByDescending(hediff => hediff.Severity)
                .FirstOrDefault();
            if (injury == null)
            {
                return;
            }

            injury.Heal(inWater ? 0.65f : 0.35f);
        }

        private static void ApplyDrownedMoodBoost(Pawn pawn, bool inWater, bool rainFed)
        {
            Need_Mood mood = pawn?.needs?.mood;
            if (!IsVariant(pawn, ZombieVariant.Drowned) || mood == null)
            {
                return;
            }

            bool wet = inWater || rainFed || HasWetStatus(pawn);
            if (!wet)
            {
                return;
            }

            float minimumMood = inWater ? 0.90f : 0.72f;
            if (mood.CurLevel < minimumMood)
            {
                mood.CurLevel = minimumMood;
            }
        }

        private static bool HasWetStatus(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return false;
            }

            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                string text = ((hediff?.def?.defName ?? string.Empty) + " " + (hediff?.def?.label ?? string.Empty)).ToLowerInvariant();
                if (text.Contains("wet") || text.Contains("soak") || text.Contains("waterlogged"))
                {
                    return true;
                }
            }

            return false;
        }

        public static void EnsureZombieAggression(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.jobs == null)
            {
                return;
            }

            NormalizeCoreZombieState(pawn);

            if (ZombieFeignDeathUtility.IsFeigningDeath(pawn))
            {
                ZombieFeignDeathUtility.ForceZombieIntoDownedState(pawn);
                return;
            }

            bool moonRush = Current.Game?.GetComponent<ZombieGameComponent>()?.IsMoonRushActive(pawn.MapHeld) == true;

            if (ZombieLurkerUtility.IsPassiveLurker(pawn) && !moonRush)
            {
                ZombieLurkerUtility.EnsurePassiveLurkerBehavior(pawn);
                return;
            }

            if (IsPlayerAlignedZombie(pawn))
            {
                EnsureFriendlyZombieState(pawn);
                return;
            }

            if (ZombieLurkerUtility.IsColonyLurker(pawn) || IsUnderColonyRestraint(pawn))
            {
                return;
            }

            if (!moonRush && IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.ShouldDrownedHoldWater(pawn))
            {
                PrepareSpawnedZombie(pawn);

                if (IsWaterCell(pawn.PositionHeld, pawn.MapHeld))
                {
                    Pawn waterTarget = pawn.CurJob?.targetA.Thing as Pawn;
                    if (pawn.CurJob?.def == JobDefOf.AttackMelee && (waterTarget == null || waterTarget.Dead || ShouldZombieIgnoreTarget(pawn, waterTarget)))
                    {
                        pawn.jobs.StopAll();
                    }

                    return;
                }

                if (!ZombieSpecialUtility.HasValidDrownedWaterReturnJob(pawn))
                {
                    ZombieSpecialUtility.TryStartDrownedReturnToWater(pawn);
                }

                return;
            }

            PrepareSpawnedZombie(pawn);
            if (pawn.Downed)
            {
                return;
            }

            if (!moonRush && ZombieSpecialUtility.HandleBoneBiterBehavior(pawn))
            {
                return;
            }

            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;
            if (currentTarget != null && IsZombie(currentTarget))
            {
                pawn.jobs.StopAll();
                currentTarget = null;
            }

            if (currentTarget != null && !currentTarget.Dead && !currentTarget.Destroyed && !ShouldZombieIgnoreTarget(pawn, currentTarget))
            {
                return;
            }

            ZombieSpawnEventType currentBehavior = Current.Game?.GetComponent<ZombieGameComponent>()?.GetAssignedBehavior(pawn) ?? ZombieRulesUtility.GetNaturalBehavior(pawn);
            if (currentBehavior != ZombieSpawnEventType.Herd && ZombieSpecialUtility.DistanceToNearestEdge(pawn.PositionHeld, pawn.MapHeld) < 4)
            {
                bool needsEdgeRescue = pawn.CurJob == null
                    || pawn.CurJob.def != JobDefOf.Goto
                    || !pawn.CurJob.targetA.IsValid
                    || !pawn.CurJob.targetA.Cell.InBounds(pawn.MapHeld)
                    || ZombieSpecialUtility.DistanceToNearestEdge(pawn.CurJob.targetA.Cell, pawn.MapHeld) <= ZombieSpecialUtility.DistanceToNearestEdge(pawn.PositionHeld, pawn.MapHeld);
                if (needsEdgeRescue)
                {
                    try
                    {
                        pawn.jobs.StopAll();
                    }
                    catch
                    {
                    }
                }
            }

            if (IsBadZombieJob(pawn, pawn.CurJob, pawn.MapHeld))
            {
                pawn.jobs.StopAll();
            }

            AssignBehaviorJob(pawn, moonRush);
        }

        public static void EnsureFriendlyZombieState(Pawn pawn, bool stopCurrentJobs = false)
        {
            if (!IsZombie(pawn) || pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return;
            }

            bool colonyAligned = false;
            try
            {
                colonyAligned = IsPlayerAlignedZombie(pawn) || pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer;
            }
            catch
            {
                colonyAligned = pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer;
            }

            if (!colonyAligned)
            {
                return;
            }

            TryEndZombieMentalState(pawn);
            TryNormalizeFriendlyZombieFaction(pawn);

            if (pawn.jobs == null)
            {
                return;
            }

            Pawn primaryTarget = pawn.CurJob?.targetA.Thing as Pawn;
            Pawn secondaryTarget = pawn.CurJob?.targetB.Thing as Pawn;
            bool hasFriendlyTarget = IsColonyAlly(primaryTarget) || IsPlayerAlignedZombie(primaryTarget) || IsColonyAlly(secondaryTarget) || IsPlayerAlignedZombie(secondaryTarget);
            string currentJobName = pawn.CurJobDef?.defName ?? string.Empty;
            bool hostileJob = pawn.CurJobDef == JobDefOf.AttackMelee || currentJobName.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0;
            if (stopCurrentJobs || hostileJob || hasFriendlyTarget)
            {
                try
                {
                    pawn.jobs.StopAll();
                }
                catch
                {
                }
            }
        }

        private static void TryNormalizeFriendlyZombieFaction(Pawn pawn)
        {
            if (pawn == null || pawn.Faction == Faction.OfPlayer)
            {
                return;
            }

            bool shouldJoinPlayerFaction = pawn.HostFaction == Faction.OfPlayer;
            if (!shouldJoinPlayerFaction)
            {
                try
                {
                    shouldJoinPlayerFaction = pawn.IsSlaveOfColony;
                }
                catch
                {
                }
            }

            if (!shouldJoinPlayerFaction)
            {
                return;
            }

            try
            {
                pawn.SetFaction(Faction.OfPlayer);
                return;
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(typeof(Pawn), "SetFaction", new[] { typeof(Faction), typeof(Pawn) })?.Invoke(pawn, new object[] { Faction.OfPlayer, null });
                return;
            }
            catch
            {
            }

            try
            {
                pawn.SetFactionDirect(Faction.OfPlayer);
            }
            catch
            {
            }
        }

        private static void TryEndZombieMentalState(Pawn pawn)
        {
            object handler = pawn?.mindState?.mentalStateHandler;
            if (handler == null)
            {
                return;
            }

            try
            {
                if ((bool)(AccessTools.Property(handler.GetType(), "InMentalState")?.GetValue(handler, null) ?? false))
                {
                    AccessTools.Method(handler.GetType(), "Reset")?.Invoke(handler, null);
                }
            }
            catch
            {
            }
        }

        public static void PrepareSpawnedZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            NormalizeCoreZombieState(pawn);
            TryEndZombieMentalState(pawn);
            StripAllUsableItems(pawn);
            TrimZombieApparel(pawn);
            MarkZombieApparelTainted(pawn, degradeApparel: false);
            SetZombieDisplayName(pawn);
            EnsureZombieInfectionState(pawn);
            TryRecoverFromSpawnIncap(pawn);
        }

        public static LocomotionUrgency GetZombieUrgency(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return LocomotionUrgency.Amble;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned) && IsExposedToRain(pawn))
            {
                return LocomotionUrgency.Jog;
            }

            return IsVariant(pawn, ZombieVariant.Grabber) ? LocomotionUrgency.Walk : LocomotionUrgency.Walk;
        }

        public static void AssignInitialShambleJob(Pawn pawn)
        {
            ZombieSpawnEventType behavior = Current.Game?.GetComponent<ZombieGameComponent>()?.GetAssignedBehavior(pawn) ?? ZombieSpawnEventType.AssaultBase;
            AssignInitialShambleJob(pawn, behavior);
        }

        public static void AssignInitialShambleJob(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null || pawn.Downed)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.IsRainActive(pawn.MapHeld) && behavior != ZombieSpawnEventType.Herd)
            {
                behavior = ZombieSpawnEventType.AssaultBase;
            }

            IntVec3 targetCell = ZombieSpecialUtility.FindInitialBehaviorCell(pawn, behavior);
            if (!targetCell.IsValid || targetCell == pawn.PositionHeld)
            {
                return;
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
                moveJob.expiryInterval = GetBehaviorMoveJobDurationTicks(behavior);
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = GetZombieUrgency(pawn);
                pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        public static bool IsBadZombieJob(Pawn pawn, Job job, Map map)
        {
            if (job == null)
            {
                return true;
            }

            ZombieSpawnEventType behavior = Current.Game?.GetComponent<ZombieGameComponent>()?.GetAssignedBehavior(pawn) ?? ZombieRulesUtility.GetNaturalBehavior(pawn);
            bool allowEdgeTravel = false;
            bool allowCorpseEdgeTarget = ZombieRulesUtility.ShouldPrioritizeCorpseFeeding(pawn)
                && job.targetA.IsValid
                && job.targetA.HasThing
                && job.targetA.Thing is Corpse;
            bool allowDrownedWaterTarget = IsVariant(pawn, ZombieVariant.Drowned)
                && job.def == JobDefOf.Goto
                && job.targetA.IsValid
                && map != null
                && ZombieUtility.IsWaterCell(job.targetA.Cell, map);

            if (IsMapExitStyleJob(job))
            {
                return true;
            }

            if (map != null && job.def == JobDefOf.Goto && job.targetA.IsValid)
            {
                IntVec3 cell = job.targetA.Cell;
                if (!cell.IsValid || !cell.InBounds(map))
                {
                    return true;
                }

                if (behavior == ZombieSpawnEventType.Herd)
                {
                    return !ZombieSpecialUtility.IsValidHerdTravelTarget(pawn, cell);
                }

                if (!allowEdgeTravel && !allowCorpseEdgeTarget && !allowDrownedWaterTarget)
                {
                    int targetEdgeDistance = ZombieSpecialUtility.DistanceToNearestEdge(cell, map);
                    int currentEdgeDistance = pawn != null && pawn.PositionHeld.IsValid && pawn.PositionHeld.InBounds(map)
                        ? ZombieSpecialUtility.DistanceToNearestEdge(pawn.PositionHeld, map)
                        : 0;

                    if (targetEdgeDistance < 6)
                    {
                        return true;
                    }

                    if (behavior == ZombieSpawnEventType.EdgeWander || behavior == ZombieSpawnEventType.HuddledPack)
                    {
                        int minimumInteriorGain = currentEdgeDistance < 12 ? 3 : 1;
                        if (targetEdgeDistance < Mathf.Max(12, currentEdgeDistance + minimumInteriorGain))
                        {
                            return true;
                        }
                    }
                    else if ((behavior == ZombieSpawnEventType.AssaultBase || behavior == ZombieSpawnEventType.GroundBurst)
                        && targetEdgeDistance < Mathf.Max(6, currentEdgeDistance + 1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMapExitStyleJob(Job job)
        {
            string defName = job?.def?.defName ?? string.Empty;
            if (defName.IndexOf("ExitMap", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("LeaveMap", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("GotoDestMap", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.IndexOf("Travel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string report = job?.def?.reportString ?? string.Empty;
            return report.IndexOf("leave the area", StringComparison.OrdinalIgnoreCase) >= 0
                || report.IndexOf("exit map", StringComparison.OrdinalIgnoreCase) >= 0
                || report.IndexOf("travel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Job CreateReplacementZombieMoveJob(Pawn pawn)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null || pawn.Downed)
            {
                return null;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.ShouldDrownedHoldWater(pawn))
            {
                IntVec3 waterCell = pawn.PositionHeld;
                if (!ZombieUtility.IsWaterCell(waterCell, pawn.MapHeld))
                {
                    List<IntVec3> waterCells = pawn.MapHeld.AllCells
                        .Where(cell => ZombieUtility.IsWaterCell(cell, pawn.MapHeld) && cell.Walkable(pawn.MapHeld))
                        .OrderBy(cell => cell.DistanceToSquared(pawn.PositionHeld))
                        .ToList();
                    waterCell = waterCells.Count > 0 ? waterCells[0] : IntVec3.Invalid;
                }

                if (waterCell.IsValid && waterCell.InBounds(pawn.MapHeld) && waterCell != pawn.PositionHeld)
                {
                    Job waterJob = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    waterJob.expiryInterval = 900;
                    waterJob.checkOverrideOnExpire = true;
                    waterJob.locomotionUrgency = GetZombieUrgency(pawn);
                    return waterJob;
                }
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            bool moonRush = component?.IsFullMoonActive(pawn.MapHeld) == true || component?.IsBloodMoonVisualActive(pawn.MapHeld) == true;
            ZombieSpawnEventType behavior = component?.GetAssignedBehavior(pawn) ?? ZombieRulesUtility.GetNaturalBehavior(pawn);
            bool rainRushingDrowned = IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.IsRainActive(pawn.MapHeld);
            if ((moonRush || rainRushingDrowned) && behavior != ZombieSpawnEventType.Herd)
            {
                behavior = ZombieSpawnEventType.AssaultBase;
            }

            IntVec3 targetCell = ZombieSpecialUtility.FindBehaviorCell(pawn, behavior);
            if (!targetCell.IsValid || !targetCell.InBounds(pawn.MapHeld) || targetCell == pawn.PositionHeld)
            {
                return null;
            }

            Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
            moveJob.expiryInterval = GetBehaviorMoveJobDurationTicks(behavior);
            moveJob.checkOverrideOnExpire = true;
            moveJob.locomotionUrgency = GetZombieUrgency(pawn);
            return moveJob;
        }

        private static void TryRecoverFromSpawnIncap(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || !pawn.Downed)
            {
                return;
            }

            if (ZombieFeignDeathUtility.IsFeigningDeath(pawn))
            {
                return;
            }

            List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().OrderByDescending(injury => injury.Severity).ToList();
            for (int pass = 0; pass < 3 && pawn.Downed; pass++)
            {
                for (int i = 0; i < injuries.Count && pawn.Downed; i++)
                {
                    Hediff_Injury injury = injuries[i];
                    if (injury == null)
                    {
                        continue;
                    }

                    injury.Severity *= 0.45f;
                    if (injury.Severity < 0.20f)
                    {
                        try
                        {
                            pawn.health.RemoveHediff(injury);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (pawn.Downed)
            {
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
            }
        }

        private static void AssignBehaviorJob(Pawn pawn, bool moonRush)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null)
            {
                return;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            ZombieSpawnEventType behavior = component?.GetAssignedBehavior(pawn) ?? ZombieRulesUtility.GetNaturalBehavior(pawn);
            bool rainRushingDrowned = IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.IsRainActive(pawn.MapHeld);
            if ((moonRush || rainRushingDrowned) && behavior != ZombieSpawnEventType.Herd)
            {
                behavior = ZombieSpawnEventType.AssaultBase;
            }

            float preyRadius = behavior == ZombieSpawnEventType.Herd ? (ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) ? 9f : 7f) : (ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) ? 26f : 12f);
            if (behavior == ZombieSpawnEventType.AssaultBase || behavior == ZombieSpawnEventType.GroundBurst)
            {
                preyRadius = Mathf.Max(preyRadius, 28f);
            }
            else if (moonRush && behavior != ZombieSpawnEventType.Herd)
            {
                preyRadius = Mathf.Max(preyRadius, 34f);
            }

            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(pawn, preyRadius);
            if (prey != null)
            {
                if (IsVariant(pawn, ZombieVariant.Grabber))
                {
                    if (Current.Game != null)
                    {
                        var gameComponent = Current.Game.GetComponent<ZombieGameComponent>();
                        if (gameComponent != null && gameComponent.HasActiveGrabberTongue(pawn))
                        {
                            return;
                        }
                    }

                    float distanceSquared = pawn.PositionHeld.DistanceToSquared(prey.PositionHeld);
                    if (distanceSquared <= ZombieGrabberUtility.HoldStartRange * ZombieGrabberUtility.HoldStartRange && ZombieGrabberUtility.TryForceTongueStart(pawn, prey))
                    {
                        return;
                    }
                }

                try
                {
                    Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, prey);
                    attackJob.expiryInterval = 700;
                    attackJob.checkOverrideOnExpire = true;
                    attackJob.locomotionUrgency = GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
                    return;
                }
                catch
                {
                }
            }

            if (!moonRush && ZombieRulesUtility.ShouldPrioritizeCorpseFeeding(pawn))
            {
                Corpse corpse = ZombieSpecialUtility.FindNearbyFreshCorpse(pawn, 8f);
                if (corpse != null)
                {
                    try
                    {
                        Job feedJob = JobMaker.MakeJob(JobDefOf.Goto, corpse.Position);
                        feedJob.expiryInterval = 450;
                        feedJob.checkOverrideOnExpire = true;
                        feedJob.locomotionUrgency = GetZombieUrgency(pawn);
                        pawn.jobs.TryTakeOrderedJob(feedJob, JobTag.Misc);
                        return;
                    }
                    catch
                    {
                    }
                }
            }

            if (HasValidBehaviorMoveJob(pawn, behavior))
            {
                return;
            }

            IntVec3 shambleCell = ZombieSpecialUtility.FindBehaviorCell(pawn, behavior);
            if (!shambleCell.IsValid || shambleCell == pawn.PositionHeld)
            {
                return;
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, shambleCell);
                moveJob.expiryInterval = GetBehaviorMoveJobDurationTicks(behavior);
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = GetZombieUrgency(pawn);
                pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        private static bool HasValidBehaviorMoveJob(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null || pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Goto || !pawn.CurJob.targetA.IsValid)
            {
                return false;
            }

            IntVec3 targetCell = pawn.CurJob.targetA.Cell;
            if (!targetCell.IsValid || !targetCell.InBounds(pawn.MapHeld) || !targetCell.Standable(pawn.MapHeld))
            {
                return false;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned) && ZombieUtility.IsWaterCell(targetCell, pawn.MapHeld))
            {
                return pawn.CurJob.expiryInterval > 0;
            }

            if (behavior == ZombieSpawnEventType.Herd)
            {
                if (!ZombieSpecialUtility.IsValidHerdTravelTarget(pawn, targetCell))
                {
                    return false;
                }
            }
            else
            {
                int targetEdgeDistance = ZombieSpecialUtility.DistanceToNearestEdge(targetCell, pawn.MapHeld);
                int currentEdgeDistance = ZombieSpecialUtility.DistanceToNearestEdge(pawn.PositionHeld, pawn.MapHeld);
                if (targetEdgeDistance < 5)
                {
                    return false;
                }

                if ((behavior == ZombieSpawnEventType.EdgeWander || behavior == ZombieSpawnEventType.HuddledPack)
                    && targetEdgeDistance < Mathf.Max(10, currentEdgeDistance + 1))
                {
                    return false;
                }
            }

            if (behavior == ZombieSpawnEventType.AssaultBase || behavior == ZombieSpawnEventType.GroundBurst)
            {
                IntVec3 baseCenter = ZombieSpecialUtility.GetPlayerBaseCenter(pawn.MapHeld);
                if (baseCenter.IsValid && targetCell.DistanceToSquared(baseCenter) > 40f * 40f)
                {
                    return false;
                }
            }

            return pawn.CurJob.expiryInterval > 0;
        }

        private static int GetBehaviorMoveJobDurationTicks(ZombieSpawnEventType behavior)
        {
            switch (behavior)
            {
                case ZombieSpawnEventType.EdgeWander:
                    return Rand.RangeInclusive(600, 1800);
                case ZombieSpawnEventType.HuddledPack:
                    return Rand.RangeInclusive(900, 1800);
                case ZombieSpawnEventType.Herd:
                    return Rand.RangeInclusive(600, 900);
                case ZombieSpawnEventType.GroundBurst:
                    return Rand.RangeInclusive(450, 900);
                default:
                    return Rand.RangeInclusive(600, 1200);
            }
        }

        public static void MarkPawnGraphicsDirty(Pawn pawn)
        {
            object renderer = pawn?.Drawer?.renderer;
            if (renderer == null)
            {
                return;
            }

            try
            {
                var directMethod = AccessTools.Method(renderer.GetType(), "SetAllGraphicsDirty");
                if (directMethod != null)
                {
                    directMethod.Invoke(renderer, null);
                    return;
                }

                object graphics = Traverse.Create(renderer).Property("graphics").GetValue();
                if (graphics == null)
                {
                    return;
                }

                var graphicsMethod = AccessTools.Method(graphics.GetType(), "SetAllGraphicsDirty");
                graphicsMethod?.Invoke(graphics, null);
            }
            catch
            {
            }
        }

        public static bool TryResurrectZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (!pawn.Dead)
            {
                return true;
            }

            Corpse corpse = ResolveCorpseForResurrection(pawn);
            Type[] candidateTypes =
            {
                typeof(ResurrectionUtility),
                AccessTools.TypeByName("RimWorld.DebugToolsSpawning"),
                AccessTools.TypeByName("Verse.DebugToolsSpawning")
            };

            foreach (Type type in candidateTypes)
            {
                if (type == null)
                {
                    continue;
                }

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    if (method == null)
                    {
                        continue;
                    }

                    if (!string.Equals(method.Name, "Resurrect", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(method.Name, "TryResurrect", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!TryBuildResurrectionArgs(method, pawn, corpse, out object[] args))
                    {
                        continue;
                    }

                    try
                    {
                        object result = method.Invoke(null, args);
                        if (!pawn.Dead)
                        {
                            return true;
                        }

                        if (result is bool success && success && !pawn.Dead)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return !pawn.Dead;
        }

        private static Corpse ResolveCorpseForResurrection(Pawn pawn)
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
                Corpse found = searchMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn && !c.Destroyed);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool TryBuildResurrectionArgs(MethodInfo method, Pawn pawn, Corpse corpse, out object[] args)
        {
            args = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters == null || parameters.Length == 0 || parameters.Length > 5)
            {
                return false;
            }

            bool assignedPawnOrCorpse = false;
            object[] built = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;
                Type concreteType = parameterType.IsByRef ? parameterType.GetElementType() : parameterType;

                if (concreteType == null)
                {
                    return false;
                }

                if (concreteType.IsAssignableFrom(typeof(Pawn)))
                {
                    built[i] = pawn;
                    assignedPawnOrCorpse = true;
                    continue;
                }

                if (corpse != null && concreteType.IsAssignableFrom(typeof(Corpse)))
                {
                    built[i] = corpse;
                    assignedPawnOrCorpse = true;
                    continue;
                }

                if (parameter.IsOptional)
                {
                    built[i] = parameter.DefaultValue is DBNull ? Type.Missing : parameter.DefaultValue;
                    continue;
                }

                if (string.Equals(concreteType.Name, "ResurrectionParams", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(concreteType.FullName, "RimWorld.ResurrectionParams", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        built[i] = Activator.CreateInstance(concreteType);
                        continue;
                    }
                    catch
                    {
                    }
                }

                if (concreteType.IsValueType)
                {
                    built[i] = Activator.CreateInstance(concreteType);
                    continue;
                }

                try
                {
                    built[i] = Activator.CreateInstance(concreteType);
                    continue;
                }
                catch
                {
                }

                built[i] = null;
            }

            if (!assignedPawnOrCorpse)
            {
                return false;
            }

            args = built;
            return true;
        }
    }
}
