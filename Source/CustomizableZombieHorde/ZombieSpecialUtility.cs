using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace CustomizableZombieHorde
{
    public static class ZombieSpecialUtility
    {
        private static readonly HashSet<int> TriggeredBoomerBursts = new HashSet<int>();
        private static readonly Dictionary<int, int> LastSickSpitTickByPawn = new Dictionary<int, int>();
        private static readonly Dictionary<int, PendingSickSpewState> PendingSickSpewsByPawn = new Dictionary<int, PendingSickSpewState>();
        private static readonly Dictionary<int, int> BoneBiterMealTargetByPawn = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> BoneBiterDisturbedUntilTickByPawn = new Dictionary<int, int>();
        private static readonly Dictionary<int, CombatNoiseFocus> CombatNoiseFocusByZombie = new Dictionary<int, CombatNoiseFocus>();
        private static readonly Dictionary<int, int> LastForcedNoiseRetargetTickByZombie = new Dictionary<int, int>();
        private static readonly Dictionary<int, IntVec3> CachedBaseCenterByMapId = new Dictionary<int, IntVec3>();
        private static readonly Dictionary<int, int> CachedBaseCenterTickByMapId = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> CachedMapHasWaterByMapId = new Dictionary<int, bool>();
        private static readonly Dictionary<int, int> CachedMapHasWaterTickByMapId = new Dictionary<int, int>();
        private const int SickSpitCooldownTicks = 936;
        private const int BoneBiterDisturbedTicks = 900;
        private const float BoneBiterMealSearchRadius = 28f;
        private const float BoneBiterMealTouchRadius = 2.9f;
        private const int SickSpewWarmupTicks = 60;
        private const int SickSpewPreviewIntervalTicks = 12;
        private const float SickSpitMinRange = 1.9f;
        private const float SickSpitMaxRange = 7.9f;
        private const float SickSpitChancePerCheck = 0.85f;
        private const float SickSpewEndWidth = 3.0f;
        private const float SickSpewMinCellRadius = 0.42f;
        private const float SickSpewBaseSeverity = 0.10f;
        private const float SickSpewTargetSeverity = 0.22f;
        private static bool suppressBoomerKillBurst;

        private sealed class PendingSickSpewState
        {
            public int SpewerPawnId;
            public int TargetPawnId;
            public int ExecuteTick;
            public int NextPreviewTick;
            public IntVec3 LastTargetCell;
        }

        private sealed class CombatNoiseFocus
        {
            public int TargetPawnId;
            public int ExpireTick;
            public bool Loud;
        }


        public static IEnumerable<Thing> BuildZombieButcherProducts(Pawn pawn)
        {
            List<Thing> result = new List<Thing>();
            if (pawn == null)
            {
                return result;
            }

            ZombieButcherProfile profile = ZombieVariantUtility.GetButcherProfile(ZombieVariantUtility.GetVariant(pawn));
            int fleshCount = profile?.FleshCount ?? 0;
            int leatherCount = profile?.LeatherCount ?? 0;

            TryAddStackedThing(result, ZombieDefOf.CZH_RottenFlesh, fleshCount, "rotten flesh");
            TryAddStackedThing(result, ZombieDefOf.CZH_RottenLeather, leatherCount, "rotten leather");

            int bileCount = ZombieBileUtility.GetButcheredBileCount(pawn);
            TryAddStackedThing(result, ZombieDefOf.CZH_ZombieBile, bileCount, "zombie bile");

            int goldToothCount = GetGoldToothCount(pawn);
            TryAddStackedThing(result, ZombieDefOf.CZH_GoldTooth, goldToothCount, "gold tooth");

            return result;
        }

        public static int GetGoldToothCount(Pawn pawn)
        {
            if (pawn == null || !ZombieRulesUtility.IsZombie(pawn))
            {
                return 0;
            }

            int seed = Gen.HashCombineInt(pawn.thingIDNumber, 19031);
            Rand.PushState(seed);
            try
            {
                return Rand.Chance(0.03f) ? 1 : 0;
            }
            finally
            {
                Rand.PopState();
            }
        }


        private static void TryAddStackedThing(List<Thing> result, ThingDef def, int count, string label)
        {
            if (result == null || def == null || count <= 0)
            {
                return;
            }

            try
            {
                Thing thing = ThingMaker.MakeThing(def);
                if (thing == null)
                {
                    return;
                }

                int stackLimit = thing.def?.stackLimit ?? count;
                thing.stackCount = count < 1 ? 1 : (count > stackLimit ? stackLimit : count);
                result.Add(thing);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to create {label} butcher product from def {def.defName}: {ex}");
            }
        }

        public static void NotifyWeaponFired(Pawn attacker, Verb verb, LocalTargetInfo targetInfo)
        {
            if (attacker == null || attacker.Dead || attacker.Destroyed || !attacker.Spawned || attacker.MapHeld == null)
            {
                return;
            }

            if (ZombieUtility.IsZombie(attacker) || !IsLoudWeaponFire(attacker, verb))
            {
                return;
            }

            IntVec3 focusCell = targetInfo.IsValid ? targetInfo.Cell : attacker.PositionHeld;
            if (!focusCell.IsValid || !focusCell.InBounds(attacker.MapHeld))
            {
                focusCell = attacker.PositionHeld;
            }

            AttractNearbyZombiesToTarget(attacker, focusCell, loud: true, damagedZombie: null);
        }

        public static void NotifyCombatAttraction(Pawn attacker, Pawn victim, bool loud)
        {
            if (attacker == null || victim == null || attacker.Dead || victim.Dead || attacker.Destroyed || victim.Destroyed)
            {
                return;
            }

            if (ZombieUtility.IsZombie(attacker) || !ZombieUtility.IsZombie(victim) || attacker.MapHeld == null || attacker.MapHeld != victim.MapHeld)
            {
                return;
            }

            IntVec3 focusCell = victim.Spawned ? victim.PositionHeld : attacker.PositionHeld;
            AttractNearbyZombiesToTarget(attacker, focusCell, loud, victim);
        }

        public static Pawn GetFocusedPrey(Pawn zombie, float fallbackRadius)
        {
            if (zombie?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            int zombieId = zombie.thingIDNumber;
            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            if (!CombatNoiseFocusByZombie.TryGetValue(zombieId, out CombatNoiseFocus focus))
            {
                return null;
            }

            if (focus == null || ticksGame > focus.ExpireTick)
            {
                CombatNoiseFocusByZombie.Remove(zombieId);
                return null;
            }

            Pawn target = zombie.MapHeld.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn != null && pawn.thingIDNumber == focus.TargetPawnId);
            if (target == null || target.Dead || target.Destroyed || target.MapHeld != zombie.MapHeld || ZombieUtility.ShouldZombieIgnoreTarget(zombie, target))
            {
                CombatNoiseFocusByZombie.Remove(zombieId);
                return null;
            }

            float allowedRadius = Mathf.Max(fallbackRadius, focus.Loud ? 42f : 8f);
            if (zombie.PositionHeld.DistanceToSquared(target.PositionHeld) > allowedRadius * allowedRadius)
            {
                return null;
            }

            return target;
        }

        public static bool IsLoudWeaponFire(Pawn attacker, Verb verb)
        {
            if (attacker == null)
            {
                return false;
            }

            if (attacker.equipment?.Primary?.def?.IsRangedWeapon == true)
            {
                return true;
            }

            if (verb?.verbProps?.range > 1.45f)
            {
                return true;
            }

            return false;
        }

        private static void AttractNearbyZombiesToTarget(Pawn attacker, IntVec3 focusCell, bool loud, Pawn damagedZombie)
        {
            Map map = attacker?.MapHeld;
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            float radius = loud ? 34f : 4.5f;
            float radiusSquared = radius * radius;
            int expireTick = (Find.TickManager?.TicksGame ?? 0) + (loud ? 1500 : 300);

            foreach (Pawn zombie in map.mapPawns.AllPawnsSpawned)
            {
                if (!ZombieUtility.IsZombie(zombie) || zombie.Dead || zombie.Destroyed || !zombie.Spawned || zombie.Downed || zombie.jobs == null)
                {
                    continue;
                }


                if (!loud && zombie != damagedZombie)
                {
                    continue;
                }

                float distance = zombie.PositionHeld.DistanceToSquared(focusCell);
                if (distance > radiusSquared)
                {
                    continue;
                }

                if (!loud && !GenSight.LineOfSight(zombie.PositionHeld, attacker.PositionHeld, map))
                {
                    continue;
                }

                CombatNoiseFocusByZombie[zombie.thingIDNumber] = new CombatNoiseFocus
                {
                    TargetPawnId = attacker.thingIDNumber,
                    ExpireTick = expireTick,
                    Loud = loud
                };

                if (ShouldForceNoiseReaction(zombie, attacker, loud, damagedZombie))
                {
                    TryForceNoiseReaction(zombie, attacker, loud);
                }
            }
        }

        private static bool ShouldForceNoiseReaction(Pawn zombie, Pawn attacker, bool loud, Pawn damagedZombie)
        {
            if (zombie == null || attacker == null || zombie.jobs == null)
            {
                return false;
            }

            if (zombie == damagedZombie)
            {
                Pawn directTarget = zombie.CurJob?.targetA.Thing as Pawn;
                return zombie.CurJob?.def != JobDefOf.AttackMelee || directTarget != attacker;
            }

            Pawn currentTarget = zombie.CurJob?.targetA.Thing as Pawn;
            if (currentTarget == attacker && zombie.CurJob?.def == JobDefOf.AttackMelee)
            {
                return false;
            }

            if (currentTarget != null && !currentTarget.Dead && !currentTarget.Destroyed && !ZombieUtility.ShouldZombieIgnoreTarget(zombie, currentTarget))
            {
                return false;
            }

            JobDef jobDef = zombie.CurJob?.def;
            if (jobDef == null || jobDef == JobDefOf.Wait)
            {
                return true;
            }

            if (jobDef == JobDefOf.Goto)
            {
                if (!loud)
                {
                    return true;
                }

                if (zombie.PositionHeld.DistanceToSquared(attacker.PositionHeld) <= 14f * 14f)
                {
                    int closeRangeTick = Find.TickManager?.TicksGame ?? 0;
                    if (LastForcedNoiseRetargetTickByZombie.TryGetValue(zombie.thingIDNumber, out int closeRangeLastTick) && closeRangeTick - closeRangeLastTick < 300)
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }

            if (!loud)
            {
                return true;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (LastForcedNoiseRetargetTickByZombie.TryGetValue(zombie.thingIDNumber, out int lastTick) && currentTick - lastTick < 300)
            {
                return false;
            }

            return currentTarget == null;
        }

        private static void TryForceNoiseReaction(Pawn zombie, Pawn attacker, bool loud)
        {
            if (zombie == null || attacker == null || zombie.MapHeld != attacker.MapHeld || zombie.jobs == null)
            {
                return;
            }

            if (ZombieUtility.ShouldZombieIgnoreTarget(zombie, attacker))
            {
                return;
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            LastForcedNoiseRetargetTickByZombie[zombie.thingIDNumber] = currentTick;

            try
            {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, attacker);
                attackJob.expiryInterval = loud ? 900 : 360;
                attackJob.checkOverrideOnExpire = true;
                attackJob.locomotionUrgency = ZombieUtility.GetZombieUrgency(zombie);
                zombie.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        public static Pawn FindClosestLivingPrey(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            Pawn focusedTarget = GetFocusedPrey(pawn, radius);
            if (focusedTarget != null)
            {
                return focusedTarget;
            }

            return FindBestLivingPrey(pawn, Mathf.Max(radius, 22f));
        }

        private static Pawn FindBestLivingPrey(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Pawn best = null;
            float bestScore = float.MaxValue;
            bool preferAnimals = ShouldPreferAnimalPrey(pawn);
            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (other == pawn || other.Dead || other.Destroyed || other.RaceProps?.IsFlesh != true)
                {
                    continue;
                }

                bool hasPukedOn = HasPukedOn(other);
                if (ZombieUtility.IsPlayerAlignedZombie(pawn))
                {
                    if (ZombieUtility.ShouldZombieIgnoreTarget(pawn, other))
                    {
                        continue;
                    }
                }
                else if (ZombieUtility.ShouldZombiesIgnore(other) && !hasPukedOn)
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(other.PositionHeld);
                if (distance > radiusSquared)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, other.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                float score = ScorePreyTarget(pawn, other, distance, preferAnimals);
                if (score >= bestScore)
                {
                    continue;
                }

                best = other;
                bestScore = score;
            }

            return best;
        }

        private static bool ShouldPreferAnimalPrey(Pawn pawn)
        {
            if (pawn == null || ZombieLurkerUtility.IsLurker(pawn))
            {
                return false;
            }

            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
            {
                return true;
            }

            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Biter) || ZombieUtility.IsVariant(pawn, ZombieVariant.Runt))
            {
                return pawn.thingIDNumber % 3 == 0;
            }

            return pawn.thingIDNumber % 5 == 0;
        }

        private static float ScorePreyTarget(Pawn hunter, Pawn prey, float distanceSquared, bool preferAnimals)
        {
            float distance = Mathf.Sqrt(distanceSquared);
            float score = distance;
            bool preyAnimal = prey?.RaceProps?.Animal == true;
            bool preyHumanlike = prey?.RaceProps?.Humanlike == true;

            if (preyAnimal)
            {
                score += preferAnimals ? -4.0f : 2.5f;

                if (prey.Downed)
                {
                    score -= 2.0f;
                }

                if (hunter != null && ZombieUtility.IsVariant(hunter, ZombieVariant.Drowned) && IsNearWater(prey.PositionHeld, prey.MapHeld, 4))
                {
                    score -= 2.5f;
                }
            }
            else if (preyHumanlike)
            {
                score -= 0.5f;
            }

            if (HasPukedOn(prey))
            {
                score -= 5.5f;
            }

            if (prey.Downed)
            {
                score -= preyAnimal ? 0.5f : 1.0f;
            }

            return score;
        }

        private static bool IsNearWater(IntVec3 center, Map map, int radius)
        {
            if (map == null || !center.IsValid)
            {
                return false;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                if (ZombieUtility.IsWaterCell(cell, map))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasPukedOn(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_PukedOn) == true;
        }

        public static bool IsBoneBiter(Pawn pawn)
        {
            return pawn != null
                && ZombieUtility.IsVariant(pawn, ZombieVariant.Biter)
                && (ZombieUtility.IsSkeletonBiter(pawn) || ZombieUtility.ShouldSpawnAsSkeletonBiter(pawn));
        }

        public static void NotifyBoneBiterDisturbed(Pawn pawn)
        {
            if (!IsBoneBiter(pawn))
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            BoneBiterDisturbedUntilTickByPawn[pawn.thingIDNumber] = ticksGame + BoneBiterDisturbedTicks;
        }

        public static bool IsBoneBiterDisturbed(Pawn pawn)
        {
            if (!IsBoneBiter(pawn))
            {
                return false;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            return BoneBiterDisturbedUntilTickByPawn.TryGetValue(pawn.thingIDNumber, out int untilTick) && untilTick > ticksGame;
        }

        public static void ClearBoneBiterMealTarget(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            BoneBiterMealTargetByPawn.Remove(pawn.thingIDNumber);
        }

        public static Thing GetBoneBiterMealTarget(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return null;
            }

            if (!BoneBiterMealTargetByPawn.TryGetValue(pawn.thingIDNumber, out int thingId))
            {
                return null;
            }

            Thing target = FindThingById(pawn.MapHeld, thingId);
            if (!IsValidBoneBiterMealTarget(pawn, target))
            {
                if (target is Pawn targetPawn && targetPawn.Dead && targetPawn.Corpse != null && IsValidBoneBiterMealTarget(pawn, targetPawn.Corpse))
                {
                    SetBoneBiterMealTarget(pawn, targetPawn.Corpse);
                    return targetPawn.Corpse;
                }

                BoneBiterMealTargetByPawn.Remove(pawn.thingIDNumber);
                return null;
            }

            return target;
        }

        public static bool HandleBoneBiterBehavior(Pawn pawn)
        {
            if (!IsBoneBiter(pawn) || pawn?.MapHeld == null || pawn.jobs == null || pawn.Dead || pawn.Destroyed || pawn.Downed)
            {
                return false;
            }

            Thing mealTarget = GetBoneBiterMealTarget(pawn);
            Thing preferredMeal = FindPreferredBoneBiterMealTarget(pawn, BoneBiterMealSearchRadius);
            if (preferredMeal != null)
            {
                mealTarget = preferredMeal;
                SetBoneBiterMealTarget(pawn, preferredMeal);
            }
            else if (mealTarget == null)
            {
                return false;
            }

            if (IsBoneBiterDisturbed(pawn))
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee)
            {
                pawn.jobs.StopAll();
            }

            if (IsBoneBiterCloseEnoughToFeed(pawn, mealTarget))
            {
                EnsureBoneBiterFeedingJob(pawn, mealTarget);
            }
            else
            {
                EnsureBoneBiterApproachJob(pawn, mealTarget);
            }

            return true;
        }

        private static void SetBoneBiterMealTarget(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null)
            {
                return;
            }

            BoneBiterMealTargetByPawn[pawn.thingIDNumber] = target.thingIDNumber;
        }

        private static Thing FindThingById(Map map, int thingId)
        {
            if (map?.listerThings?.AllThings == null)
            {
                return null;
            }

            return map.listerThings.AllThings.FirstOrDefault(thing => thing != null && thing.thingIDNumber == thingId);
        }

        private static Thing FindPreferredBoneBiterMealTarget(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Thing bestCorpse = null;
            float bestCorpseDistance = float.MaxValue;
            foreach (Corpse corpse in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
            {
                if (!IsValidBoneBiterMealTarget(pawn, corpse))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld);
                if (distance > radiusSquared || distance >= bestCorpseDistance)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, corpse.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                bestCorpse = corpse;
                bestCorpseDistance = distance;
            }

            if (bestCorpse != null)
            {
                return bestCorpse;
            }

            Pawn bestDownedPawn = null;
            float bestDownedDistance = float.MaxValue;
            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (!IsValidBoneBiterMealTarget(pawn, other))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(other.PositionHeld);
                if (distance > radiusSquared || distance >= bestDownedDistance)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, other.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                bestDownedPawn = other;
                bestDownedDistance = distance;
            }

            return bestDownedPawn;
        }

        private static bool IsValidBoneBiterMealTarget(Pawn pawn, Thing target)
        {
            if (pawn?.MapHeld == null || target == null || target.Destroyed || target.MapHeld != pawn.MapHeld)
            {
                return false;
            }

            if (target is Corpse corpse)
            {
                Pawn innerPawn = corpse.InnerPawn;
                return innerPawn != null
                    && innerPawn.RaceProps?.IsFlesh == true
                    && !ZombieUtility.IsZombie(innerPawn);
            }

            if (target is Pawn targetPawn)
            {
                return targetPawn != pawn
                    && !targetPawn.Dead
                    && !targetPawn.Destroyed
                    && targetPawn.Downed
                    && targetPawn.RaceProps?.IsFlesh == true
                    && !ZombieUtility.IsZombie(targetPawn)
                    && !ZombieUtility.ShouldZombiesIgnore(targetPawn);
            }

            return false;
        }

        private static bool IsBoneBiterCloseEnoughToFeed(Pawn pawn, Thing target)
        {
            return pawn != null
                && target != null
                && pawn.PositionHeld.DistanceToSquared(target.PositionHeld) <= BoneBiterMealTouchRadius * BoneBiterMealTouchRadius;
        }

        private static void EnsureBoneBiterApproachJob(Pawn pawn, Thing target)
        {
            if (pawn?.jobs == null || target == null)
            {
                return;
            }

            if (pawn.CurJob != null
                && pawn.CurJob.def == JobDefOf.Goto
                && pawn.CurJob.targetA.IsValid
                && pawn.CurJob.targetA.Cell == target.PositionHeld)
            {
                return;
            }

            Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, target.PositionHeld);
            moveJob.expiryInterval = 750;
            moveJob.checkOverrideOnExpire = true;
            moveJob.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
            pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
        }

        private static void EnsureBoneBiterFeedingJob(Pawn pawn, Thing target)
        {
            if (pawn?.jobs == null)
            {
                return;
            }

            pawn.rotationTracker?.FaceTarget(new LocalTargetInfo(target));
            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Wait_Combat)
            {
                return;
            }

            Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            waitJob.expiryInterval = 240;
            waitJob.checkOverrideOnExpire = true;
            pawn.jobs.TryTakeOrderedJob(waitJob, JobTag.Misc);
        }

        public static void HandleBoneBiterFeeding(Map map)
        {
            if (map == null)
            {
                return;
            }

            PruneBoneBiterState(map);
            List<Pawn> spawnedPawns = map.mapPawns?.AllPawnsSpawned?.ToList() ?? new List<Pawn>();
            foreach (Pawn pawn in spawnedPawns)
            {
                if (!IsBoneBiter(pawn) || pawn.Dead || pawn.Destroyed || pawn.Downed)
                {
                    continue;
                }

                Thing target = GetBoneBiterMealTarget(pawn);
                if (target == null)
                {
                    continue;
                }

                if (IsBoneBiterDisturbed(pawn) || !IsBoneBiterCloseEnoughToFeed(pawn, target))
                {
                    continue;
                }

                EnsureBoneBiterFeedingJob(pawn, target);
                if (target is Corpse corpse)
                {
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                    if (Rand.Chance(0.22f))
                    {
                        corpse.Destroy(DestroyMode.Vanish);
                        ClearBoneBiterMealTarget(pawn);
                    }

                    continue;
                }

                if (target is Pawn prey && prey.Downed && !prey.Dead)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, Rand.Range(4f, 8f), 999f, -1f, pawn);
                    prey.TakeDamage(dinfo);
                    FilthMaker.TryMakeFilth(prey.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                    if (prey.Dead && prey.Corpse != null)
                    {
                        SetBoneBiterMealTarget(pawn, prey.Corpse);
                    }
                }
            }
        }

        private static void PruneBoneBiterState(Map map)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            HashSet<int> liveBoneBiterIds = new HashSet<int>(map.mapPawns.AllPawnsSpawned
                .Where(IsBoneBiter)
                .Select(pawn => pawn.thingIDNumber));

            foreach (int pawnId in BoneBiterMealTargetByPawn.Keys.Where(id => !liveBoneBiterIds.Contains(id)).ToList())
            {
                BoneBiterMealTargetByPawn.Remove(pawnId);
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            foreach (int pawnId in BoneBiterDisturbedUntilTickByPawn.Keys.Where(id => !liveBoneBiterIds.Contains(id) || BoneBiterDisturbedUntilTickByPawn[id] <= ticksGame).ToList())
            {
                BoneBiterDisturbedUntilTickByPawn.Remove(pawnId);
            }
        }

        public static Corpse FindNearbyFreshCorpse(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Corpse best = null;
            float bestDistance = float.MaxValue;
            foreach (Corpse corpse in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
            {
                if (corpse.Destroyed || ZombieUtility.IsZombie(corpse.InnerPawn))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                best = corpse;
                bestDistance = distance;
            }

            return best;
        }

        public static IntVec3 FindInitialBehaviorCell(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            switch (behavior)
            {
                case ZombieSpawnEventType.EdgeWander:
                    return FindEdgePatrolCell(pawn);
                case ZombieSpawnEventType.HuddledPack:
                    return FindHuddleCell(pawn);
                case ZombieSpawnEventType.GroundBurst:
                    return FindAssaultCell(pawn);
                case ZombieSpawnEventType.Herd:
                    return FindHerdCrossingCell(pawn);
                default:
                    return FindAssaultCell(pawn);
            }
        }

        public static IntVec3 FindBehaviorCell(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            switch (behavior)
            {
                case ZombieSpawnEventType.EdgeWander:
                    return FindEdgePatrolCell(pawn);
                case ZombieSpawnEventType.HuddledPack:
                    return FindHuddleCell(pawn);
                case ZombieSpawnEventType.GroundBurst:
                    return FindAssaultCell(pawn);
                case ZombieSpawnEventType.Herd:
                    return FindHerdCrossingCell(pawn);
                default:
                    return FindAssaultCell(pawn);
            }
        }

        public static IntVec3 FindHerdCrossingCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            if (component == null || !component.TryGetAssignedHerdDirection(pawn, out ZombieHerdDirection direction))
            {
                return FindAssaultCell(pawn);
            }

            int minMargin = 6;
            int maxX = map.Size.x - 7;
            int maxZ = map.Size.z - 7;
            int seed = Mathf.Abs(pawn.thingIDNumber * 31);
            int laneOffset = (seed % 7) - 3;
            int waveOffset = Mathf.RoundToInt(Mathf.Sin(((Find.TickManager?.TicksGame ?? 0) * 0.01f) + (seed * 0.17f)) * 2f);
            IntVec3 target;

            switch (direction)
            {
                case ZombieHerdDirection.NorthToSouth:
                {
                    int x = Mathf.Clamp(pawn.PositionHeld.x + laneOffset + waveOffset, minMargin, maxX);
                    target = new IntVec3(x, 0, minMargin);
                    break;
                }
                case ZombieHerdDirection.SouthToNorth:
                {
                    int x = Mathf.Clamp(pawn.PositionHeld.x + laneOffset + waveOffset, minMargin, maxX);
                    target = new IntVec3(x, 0, maxZ);
                    break;
                }
                case ZombieHerdDirection.WestToEast:
                {
                    int z = Mathf.Clamp(pawn.PositionHeld.z + laneOffset + waveOffset, minMargin, maxZ);
                    target = new IntVec3(maxX, 0, z);
                    break;
                }
                default:
                {
                    int z = Mathf.Clamp(pawn.PositionHeld.z + laneOffset + waveOffset, minMargin, maxZ);
                    target = new IntVec3(minMargin, 0, z);
                    break;
                }
            }

            if (target.InBounds(map) && target.Standable(map))
            {
                return target;
            }

            for (int radius = 1; radius <= 10; radius++)
            {
                IntVec3 nearby = CellFinder.RandomClosewalkCellNear(target, map, radius);
                if (nearby.IsValid && nearby.Standable(map))
                {
                    return nearby;
                }
            }

            return FindAssaultCell(pawn);
        }

        public static bool IsValidHerdTravelTarget(Pawn pawn, IntVec3 target)
        {
            Map map = pawn?.MapHeld;
            if (map == null || !target.IsValid || !target.InBounds(map) || !target.Standable(map))
            {
                return false;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            if (component == null || !component.TryGetAssignedHerdDirection(pawn, out ZombieHerdDirection direction))
            {
                return false;
            }

            const int minimumProgress = 2;
            switch (direction)
            {
                case ZombieHerdDirection.NorthToSouth:
                    return target.z <= pawn.PositionHeld.z - minimumProgress || target.z <= 8;
                case ZombieHerdDirection.SouthToNorth:
                    return target.z >= pawn.PositionHeld.z + minimumProgress || target.z >= map.Size.z - 9;
                case ZombieHerdDirection.WestToEast:
                    return target.x >= pawn.PositionHeld.x + minimumProgress || target.x >= map.Size.x - 9;
                default:
                    return target.x <= pawn.PositionHeld.x - minimumProgress || target.x <= 8;
            }
        }

        public static bool IsHerdReadyToLeaveMap(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return false;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            if (component == null || !component.TryGetAssignedHerdDirection(pawn, out ZombieHerdDirection direction))
            {
                return false;
            }

            switch (direction)
            {
                case ZombieHerdDirection.NorthToSouth:
                    return pawn.PositionHeld.z <= 4;
                case ZombieHerdDirection.SouthToNorth:
                    return pawn.PositionHeld.z >= map.Size.z - 5;
                case ZombieHerdDirection.WestToEast:
                    return pawn.PositionHeld.x >= map.Size.x - 5;
                default:
                    return pawn.PositionHeld.x <= 4;
            }
        }

        public static IntVec3 FindAssaultCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 targetCenter = GetPlayerBaseCenter(map);
            List<IntVec3> assaultOptions = GenRadial.RadialCellsAround(targetCenter, 22f, true)
                .Where(cell => cell.InBounds(map)
                    && cell.Standable(map)
                    && DistanceToNearestEdge(cell, map) >= 10
                    && cell.DistanceToSquared(targetCenter) >= 9f)
                .ToList();

            IntVec3 visitorStyleCell = SelectVisitorStyleDestination(pawn, assaultOptions, targetCenter, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 9f, maxAnchorDistanceSquared: 22f * 22f, ticksPerStep: 600);
            if (visitorStyleCell.IsValid)
            {
                return visitorStyleCell;
            }

            if (targetCenter.IsValid && targetCenter.InBounds(map) && targetCenter.Standable(map))
            {
                return targetCenter;
            }

            return map.Center;
        }

        public static IntVec3 FindHuddleCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<Pawn> nearbyZombies = map.mapPawns.AllPawnsSpawned
                .Where(other => other != pawn && ZombieUtility.IsZombie(other) && !other.Dead && !other.Destroyed && other.PositionHeld.DistanceToSquared(pawn.PositionHeld) <= 225f)
                .ToList();

            IntVec3 packCenter = pawn.PositionHeld;
            if (nearbyZombies.Count > 0)
            {
                int sumX = pawn.PositionHeld.x;
                int sumZ = pawn.PositionHeld.z;
                int count = 1;
                foreach (Pawn zombie in nearbyZombies)
                {
                    count++;
                    sumX += zombie.PositionHeld.x;
                    sumZ += zombie.PositionHeld.z;
                }

                packCenter = new IntVec3(sumX / count, 0, sumZ / count);
            }
            else if (DistanceToNearestEdge(packCenter, map) < 7)
            {
                packCenter = FindInteriorNearEdgeCell(map, pawn.PositionHeld);
            }

            int packEdgeDistance = DistanceToNearestEdge(packCenter, map);
            IntVec3 deeperCell = IntVec3.Invalid;
            if (packEdgeDistance < 10)
            {
                deeperCell = FindDeeperHuddleCell(pawn, packCenter, minExtraEdgeDistance: 5, minEdgeDistance: 12, maxEdgeDistance: 26, searchRadius: 20f);
            }
            else if (packEdgeDistance < 18)
            {
                deeperCell = FindDeeperHuddleCell(pawn, packCenter, minExtraEdgeDistance: 4, minEdgeDistance: 18, maxEdgeDistance: 34, searchRadius: 26f);
            }
            else if (packEdgeDistance < 26)
            {
                deeperCell = FindDeeperHuddleCell(pawn, packCenter, minExtraEdgeDistance: 3, minEdgeDistance: 24, maxEdgeDistance: 42, searchRadius: 30f);
            }

            if (deeperCell.IsValid)
            {
                return deeperCell;
            }

            IntVec3 threatCell = FindDeepHuddleThreatCell(pawn, packCenter);
            if (threatCell.IsValid)
            {
                return threatCell;
            }

            for (int i = 0; i < 12; i++)
            {
                IntVec3 candidate = CellFinder.RandomClosewalkCellNear(packCenter, map, 4);
                if (candidate.IsValid && candidate.Standable(map) && DistanceToNearestEdge(candidate, map) >= 6)
                {
                    return candidate;
                }
            }

            return FindInteriorNearEdgeCell(map, packCenter);
        }

        private static IntVec3 FindDeeperHuddleCell(Pawn pawn, IntVec3 anchor, int minExtraEdgeDistance, int minEdgeDistance, int maxEdgeDistance, float searchRadius)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            int currentEdgeDistance = DistanceToNearestEdge(anchor, map);
            int requiredEdgeDistance = Mathf.Max(minEdgeDistance, currentEdgeDistance + minExtraEdgeDistance);
            List<IntVec3> options = GenRadial.RadialCellsAround(anchor, searchRadius, true)
                .Where(cell => cell.InBounds(map)
                    && cell.Standable(map)
                    && cell != pawn.PositionHeld
                    && cell.DistanceToSquared(anchor) >= 36f)
                .Where(cell =>
                {
                    int edgeDistance = DistanceToNearestEdge(cell, map);
                    return edgeDistance >= requiredEdgeDistance && edgeDistance <= maxEdgeDistance;
                })
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.z)
                .ToList();

            if (options.Count == 0)
            {
                return IntVec3.Invalid;
            }

            IntVec3 chosen = SelectVisitorStyleDestination(pawn, options, anchor, preferRoads: false, preferOpenGround: true, minAnchorDistanceSquared: 36f, maxAnchorDistanceSquared: searchRadius * searchRadius, ticksPerStep: 1800);
            return chosen.IsValid ? chosen : PickRollingCellForPawn(pawn, options, 1800);
        }

        private static IntVec3 FindDeepHuddleThreatCell(Pawn pawn, IntVec3 packCenter)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 baseCenter = GetPlayerBaseCenter(map);
            List<IntVec3> baseOptions = new List<IntVec3>();
            if (baseCenter.IsValid)
            {
                baseOptions = GenRadial.RadialCellsAround(baseCenter, 18f, true)
                    .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 12)
                    .OrderBy(cell => cell.x)
                    .ThenBy(cell => cell.z)
                    .ToList();
            }

            if (baseOptions.Count > 0)
            {
                IntVec3 chosen = SelectVisitorStyleDestination(pawn, baseOptions, baseCenter, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 0f, maxAnchorDistanceSquared: 18f * 18f, ticksPerStep: 2400);
                return chosen.IsValid ? chosen : PickRollingCellForPawn(pawn, baseOptions, 2400);
            }

            List<IntVec3> interiorOptions = GenRadial.RadialCellsAround(map.Center, 28f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 12)
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.z)
                .ToList();
            if (interiorOptions.Count > 0)
            {
                IntVec3 chosen = SelectVisitorStyleDestination(pawn, interiorOptions, map.Center, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 0f, maxAnchorDistanceSquared: 28f * 28f, ticksPerStep: 2400);
                return chosen.IsValid ? chosen : PickRollingCellForPawn(pawn, interiorOptions, 2400);
            }

            return FindAssaultCell(pawn);
        }

        private static IntVec3 PickRollingCellForPawn(Pawn pawn, List<IntVec3> options, int ticksPerStep)
        {
            if (pawn == null || options == null || options.Count == 0)
            {
                return IntVec3.Invalid;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int step = ticksPerStep <= 0 ? 0 : ticksGame / ticksPerStep;
            int index = Mathf.Abs((pawn.thingIDNumber / 7) + step) % options.Count;
            return options[index];
        }

        public static IntVec3 FindEdgePatrolCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            bool preferBase = ShouldFavorBaseWander(pawn);
            IntVec3 baseCell = FindBaseWanderCell(map, pawn);
            IntVec3 cornerCell = FindCornerWanderCell(map, pawn);
            if (preferBase && baseCell.IsValid)
            {
                return baseCell;
            }

            if (cornerCell.IsValid)
            {
                return cornerCell;
            }

            if (baseCell.IsValid)
            {
                return baseCell;
            }

            List<IntVec3> interiorOptions = GenRadial.RadialCellsAround(map.Center, 28f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 10)
                .ToList();
            IntVec3 interiorCell = SelectVisitorStyleDestination(pawn, interiorOptions, map.Center, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 16f, maxAnchorDistanceSquared: 28f * 28f, ticksPerStep: 900);
            if (interiorCell.IsValid)
            {
                return interiorCell;
            }

            return FindAssaultCell(pawn);
        }

        public static IntVec3 FindInteriorNearEdgeCell(Map map, IntVec3 anchor)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<IntVec3> options = GenRadial.RadialCellsAround(anchor, 16f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 18)
                .ToList();
            if (options.Count > 0)
            {
                IntVec3 chosen = SelectVisitorStyleDestination(null, options, anchor, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 0f, maxAnchorDistanceSquared: 16f * 16f, ticksPerStep: 900);
                return chosen.IsValid ? chosen : options.RandomElement();
            }

            foreach (IntVec3 cell in map.AllCells.InRandomOrder())
            {
                if (cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 18)
                {
                    return cell;
                }
            }

            return map.Center;
        }

        private static IntVec3 FindBaseWanderCell(Map map, Pawn pawn)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 baseCenter = GetPlayerBaseCenter(map);
            if (baseCenter.IsValid)
            {
                List<IntVec3> options = GenRadial.RadialCellsAround(baseCenter, 30f, true)
                    .Where(cell => cell.InBounds(map)
                        && cell.Standable(map)
                        && DistanceToNearestEdge(cell, map) >= 10
                        && cell.DistanceToSquared(baseCenter) >= 25f
                        && cell.DistanceToSquared(baseCenter) <= 30f * 30f)
                    .ToList();
                IntVec3 chosen = SelectVisitorStyleDestination(pawn, options, baseCenter, preferRoads: true, preferOpenGround: true, minAnchorDistanceSquared: 25f, maxAnchorDistanceSquared: 30f * 30f, ticksPerStep: 900);
                if (chosen.IsValid)
                {
                    return chosen;
                }
            }

            IntVec3 fallbackAnchor = pawn != null && pawn.PositionHeld.IsValid ? pawn.PositionHeld : map.Center;
            return FindInteriorNearEdgeCell(map, fallbackAnchor);
        }

        private static IntVec3 FindCornerWanderCell(Map map, Pawn pawn)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            int margin = 16;
            List<IntVec3> anchors = new List<IntVec3>
            {
                new IntVec3(margin, 0, margin),
                new IntVec3(map.Size.x - 1 - margin, 0, margin),
                new IntVec3(margin, 0, map.Size.z - 1 - margin),
                new IntVec3(map.Size.x - 1 - margin, 0, map.Size.z - 1 - margin)
            }
                .Where(cell => cell.InBounds(map))
                .ToList();

            if (anchors.Count == 0)
            {
                return IntVec3.Invalid;
            }

            int pawnOffset = pawn == null ? 0 : Mathf.Abs(pawn.thingIDNumber);
            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            IntVec3 anchor = anchors[(pawnOffset / 11 + ticksGame / 900) % anchors.Count];
            List<IntVec3> options = GenRadial.RadialCellsAround(anchor, 16f, true)
                .Where(cell => cell.InBounds(map)
                    && cell.Standable(map)
                    && DistanceToNearestEdge(cell, map) >= 10
                    && cell.DistanceToSquared(anchor) >= 9f)
                .ToList();

            IntVec3 chosen = SelectVisitorStyleDestination(pawn, options, anchor, preferRoads: false, preferOpenGround: true, minAnchorDistanceSquared: 9f, maxAnchorDistanceSquared: 16f * 16f, ticksPerStep: 900);
            if (chosen.IsValid)
            {
                return chosen;
            }

            return anchor.Standable(map) ? anchor : IntVec3.Invalid;
        }

        private static bool ShouldFavorBaseWander(Pawn pawn)
        {
            if (pawn == null)
            {
                return true;
            }

            if (ZombieLurkerUtility.IsLurker(pawn))
            {
                return true;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int roll = Mathf.Abs((pawn.thingIDNumber / 5) + (ticksGame / 900)) % 10;
            return roll < 7;
        }

        private static IntVec3 SelectVisitorStyleDestination(Pawn pawn, IEnumerable<IntVec3> candidates, IntVec3 anchor, bool preferRoads, bool preferOpenGround, float minAnchorDistanceSquared, float maxAnchorDistanceSquared, int ticksPerStep)
        {
            Map map = pawn?.MapHeld;
            if (map == null || candidates == null)
            {
                return IntVec3.Invalid;
            }

            List<(IntVec3 Cell, float Score)> ordered = candidates
                .Where(cell => cell.IsValid && cell.InBounds(map) && cell.Standable(map) && cell != pawn.PositionHeld)
                .Select(cell => new { Cell = cell, AnchorDistanceSquared = anchor.IsValid ? cell.DistanceToSquared(anchor) : 0f })
                .Where(entry => !anchor.IsValid || (entry.AnchorDistanceSquared >= minAnchorDistanceSquared && (maxAnchorDistanceSquared <= 0f || entry.AnchorDistanceSquared <= maxAnchorDistanceSquared)))
                .Select(entry => (entry.Cell, Score: ScoreVisitorStyleCell(map, entry.Cell, anchor, preferRoads, preferOpenGround)))
                .Where(entry => entry.Score > float.MinValue / 4f)
                .OrderByDescending(entry => entry.Score)
                .Take(8)
                .ToList();

            if (ordered.Count == 0)
            {
                return IntVec3.Invalid;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int step = ticksPerStep <= 0 ? 0 : ticksGame / ticksPerStep;
            int pawnOffset = pawn == null ? 0 : Mathf.Abs(pawn.thingIDNumber / 3);

            for (int offset = 0; offset < ordered.Count; offset++)
            {
                IntVec3 candidate = ordered[(pawnOffset + step + offset) % ordered.Count].Cell;
                bool reachable;
                try
                {
                    reachable = pawn.CanReach(candidate, PathEndMode.OnCell, Danger.Deadly);
                }
                catch
                {
                    reachable = candidate.Walkable(map);
                }

                if (reachable)
                {
                    return candidate;
                }
            }

            return ordered[0].Cell;
        }

        private static float ScoreVisitorStyleCell(Map map, IntVec3 cell, IntVec3 anchor, bool preferRoads, bool preferOpenGround)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map) || !cell.Standable(map))
            {
                return float.MinValue;
            }

            int edgeDistance = DistanceToNearestEdge(cell, map);
            if (edgeDistance < 8)
            {
                return float.MinValue;
            }

            float score = edgeDistance * 10f;
            if (anchor.IsValid)
            {
                float anchorDistance = Mathf.Sqrt(cell.DistanceToSquared(anchor));
                score -= anchorDistance * 0.55f;
            }

            int openNeighbors = CountWalkableCardinalNeighbors(cell, map);
            score += openNeighbors * (preferOpenGround ? 9f : 4f);

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain != null)
            {
                string terrainName = ((terrain.defName ?? string.Empty) + " " + (terrain.label ?? string.Empty)).ToLowerInvariant();
                if (preferRoads && (terrainName.Contains("road") || terrainName.Contains("path") || terrainName.Contains("paved") || terrainName.Contains("bridge")))
                {
                    score += 22f;
                }

                score -= Mathf.Max(0f, terrain.pathCost) * 0.35f;
            }

            Building edifice = cell.GetEdifice(map);
            if (edifice != null)
            {
                score -= 8f;
            }

            return score;
        }

        private static int CountWalkableCardinalNeighbors(IntVec3 cell, Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int count = 0;
            IntVec3 north = new IntVec3(cell.x, 0, cell.z + 1);
            IntVec3 south = new IntVec3(cell.x, 0, cell.z - 1);
            IntVec3 east = new IntVec3(cell.x + 1, 0, cell.z);
            IntVec3 west = new IntVec3(cell.x - 1, 0, cell.z);
            if (north.InBounds(map) && north.Walkable(map)) count++;
            if (south.InBounds(map) && south.Walkable(map)) count++;
            if (east.InBounds(map) && east.Walkable(map)) count++;
            if (west.InBounds(map) && west.Walkable(map)) count++;
            return count;
        }

        public static IntVec3 GetPlayerBaseCenter(Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int mapId = map.uniqueID;
            if (CachedBaseCenterTickByMapId.TryGetValue(mapId, out int cachedTick)
                && ticksGame - cachedTick < 600
                && CachedBaseCenterByMapId.TryGetValue(mapId, out IntVec3 cachedCenter)
                && cachedCenter.IsValid)
            {
                return cachedCenter;
            }

            IntVec3 result = IntVec3.Invalid;

            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => cell.Standable(map)).ToList();
            if (homeCells != null && homeCells.Count > 0)
            {
                int sumX = 0;
                int sumZ = 0;
                foreach (IntVec3 cell in homeCells)
                {
                    sumX += cell.x;
                    sumZ += cell.z;
                }

                result = new IntVec3(sumX / homeCells.Count, 0, sumZ / homeCells.Count);
            }
            else if (map.mapPawns?.FreeColonistsSpawned != null && map.mapPawns.FreeColonistsSpawned.Count > 0)
            {
                int sumX = 0;
                int sumZ = 0;
                int count = 0;
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!colonist.Spawned)
                    {
                        continue;
                    }

                    count++;
                    sumX += colonist.Position.x;
                    sumZ += colonist.Position.z;
                }

                if (count > 0)
                {
                    result = new IntVec3(sumX / count, 0, sumZ / count);
                }
            }

            if (!result.IsValid)
            {
                result = map.Center;
            }

            CachedBaseCenterByMapId[mapId] = result;
            CachedBaseCenterTickByMapId[mapId] = ticksGame;
            return result;
        }

        public static int DistanceToNearestEdge(IntVec3 cell, Map map)
        {
            int xDistance = cell.x < map.Size.x - 1 - cell.x ? cell.x : map.Size.x - 1 - cell.x;
            int zDistance = cell.z < map.Size.z - 1 - cell.z ? cell.z : map.Size.z - 1 - cell.z;
            return xDistance < zDistance ? xDistance : zDistance;
        }

        public static void HandleCorpseFeeding(Map map)
        {
            if (map == null)
            {
                return;
            }

            List<Pawn> boneBiters = map.mapPawns?.AllPawnsSpawned?
                .Where(pawn => IsBoneBiter(pawn) && !pawn.Dead && !pawn.Destroyed)
                .ToList() ?? new List<Pawn>();
            if (boneBiters.Count == 0)
            {
                return;
            }

            List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
            foreach (Corpse corpse in corpses)
            {
                if (corpse.Destroyed || ZombieUtility.IsZombie(corpse.InnerPawn))
                {
                    continue;
                }

                int nearbyBoneBiters = 0;
                for (int i = 0; i < boneBiters.Count; i++)
                {
                    if (boneBiters[i].PositionHeld.DistanceToSquared(corpse.PositionHeld) <= 9f)
                    {
                        nearbyBoneBiters++;
                    }
                }

                if (nearbyBoneBiters <= 0)
                {
                    continue;
                }

                for (int i = 0; i < nearbyBoneBiters; i++)
                {
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                }

                if (Rand.Chance(0.10f * nearbyBoneBiters))
                {
                    corpse.Destroy(DestroyMode.Vanish);
                }
            }
        }

        public static bool MapHasWater(Map map)
        {
            if (map == null)
            {
                return false;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int mapId = map.uniqueID;
            if (CachedMapHasWaterTickByMapId.TryGetValue(mapId, out int cachedTick)
                && ticksGame - cachedTick < 2500
                && CachedMapHasWaterByMapId.TryGetValue(mapId, out bool cachedValue))
            {
                return cachedValue;
            }

            bool hasWater = false;

            foreach (IntVec3 cell in map.AllCells)
            {
                if (ZombieUtility.IsWaterCell(cell, map))
                {
                    hasWater = true;
                    break;
                }
            }

            CachedMapHasWaterByMapId[mapId] = hasWater;
            CachedMapHasWaterTickByMapId[mapId] = ticksGame;
            return hasWater;
        }

        public static void DropZombieBlood(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            ThingDef filth = ZombieDefOf.CZH_Filth_ZombieBlood;
            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Sick))
            {
                filth = ZombieDefOf.CZH_Filth_SickZombieBlood;
            }

            if (filth == null || !TryGetEffectLocation(pawn, out Map map, out IntVec3 pos))
            {
                return;
            }

            FilthMaker.TryMakeFilth(pos, map, filth);
        }

        public static void HandleZombieDeathEffects(Pawn pawn)
        {
            if (!ZombieUtility.IsZombie(pawn))
            {
                return;
            }

            switch (ZombieUtility.GetVariant(pawn))
            {
                case ZombieVariant.Boomer:
                    if (TriggeredBoomerBursts.Contains(pawn.thingIDNumber))
                    {
                        FinalizeBoomerBurst(pawn);
                    }
                    else
                    {
                        TriggerBoomerBurst(pawn, consumePawn: false, force: !suppressBoomerKillBurst);
                    }
                    break;
                case ZombieVariant.Sick:
                    DropZombieBlood(pawn);
                    DoSicknessBurst(pawn);
                    break;
                default:
                    DropZombieBlood(pawn);
                    break;
            }
        }

        public static bool TriggerBoomerBurst(Pawn pawn, bool consumePawn, bool force = false)
        {
            if (!TriggerBoomerBurstOnly(pawn, force))
            {
                return false;
            }

            Map map = pawn.MapHeld;
            IntVec3 pos = pawn.PositionHeld;
            if (consumePawn && !pawn.Dead && !pawn.Destroyed)
            {
                try
                {
                    suppressBoomerKillBurst = true;
                    pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 999f, 0f, -1f, null));
                }
                finally
                {
                    suppressBoomerKillBurst = false;
                }
            }

            ReplaceBoomerCorpseWithRottenFlesh(pawn, map, pos);
            return true;
        }

        public static bool TriggerBoomerBurstOnly(Pawn pawn, bool force = false)
        {
            if (pawn == null || pawn.DestroyedOrNull() || !ZombieUtility.IsVariant(pawn, ZombieVariant.Boomer))
            {
                return false;
            }

            int id = pawn.thingIDNumber;
            if (!force && TriggeredBoomerBursts.Contains(id))
            {
                return false;
            }

            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 pos))
            {
                return false;
            }

            TriggeredBoomerBursts.Add(id);
            DropZombieBlood(pawn);
            DoAcidBurst(map, pos, pawn);
            return true;
        }

        public static void FinalizeBoomerBurst(Pawn pawn)
        {
            if (pawn == null || !TriggeredBoomerBursts.Contains(pawn.thingIDNumber))
            {
                return;
            }

            ReplaceBoomerCorpseWithRottenFlesh(pawn, pawn.MapHeld, pawn.PositionHeld);
        }

        private static void ReplaceBoomerCorpseWithRottenFlesh(Pawn pawn, Map map, IntVec3 pos)
        {
            Corpse corpse = pawn?.Corpse;
            if (corpse != null)
            {
                map = corpse.MapHeld ?? map;
                if (corpse.PositionHeld.IsValid)
                {
                    pos = corpse.PositionHeld;
                }
            }
            else if ((map == null || !pos.IsValid || !pos.InBounds(map)) && pawn != null)
            {
                corpse = pawn.MapHeld?.thingGrid?.ThingsAt(pawn.PositionHeld).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn);
                if (corpse != null)
                {
                    map = corpse.MapHeld;
                    pos = corpse.PositionHeld;
                }
            }

            if (map == null || !pos.IsValid || !pos.InBounds(map))
            {
                return;
            }

            if (corpse != null && !corpse.Destroyed)
            {
                corpse.Destroy(DestroyMode.Vanish);
            }
            else if (pawn != null && !pawn.Destroyed && !pawn.Dead)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }

            ThingDef rottenFlesh = ZombieDefOf.CZH_RottenFlesh;
            if (rottenFlesh != null)
            {
                Thing flesh = ThingMaker.MakeThing(rottenFlesh);
                flesh.stackCount = Rand.RangeInclusive(1, 4);
                GenPlace.TryPlaceThing(flesh, pos, map, ThingPlaceMode.Near);
            }

            ThingDef zombieBile = ZombieDefOf.CZH_ZombieBile;
            if (zombieBile != null)
            {
                Thing bile = ThingMaker.MakeThing(zombieBile);
                bile.stackCount = Rand.RangeInclusive(1, 2);
                GenPlace.TryPlaceThing(bile, pos, map, ThingPlaceMode.Near);
            }

            TrySpawnBoomerRunt(pawn, map, pos);
        }

        private static void TrySpawnBoomerRunt(Pawn sourcePawn, Map map, IntVec3 pos)
        {
            bool isPregnantBoomer = sourcePawn?.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_PregnantBoomer) ?? false;
            PawnKindDef runtKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Runt");
            if (sourcePawn == null || map == null || !pos.IsValid || !pos.InBounds(map) || runtKind == null || !isPregnantBoomer)
            {
                return;
            }

            try
            {
                Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
                if (faction == null)
                {
                    return;
                }

                Pawn runt = ZombiePawnFactory.GenerateZombie(runtKind, faction);
                if (runt == null)
                {
                    return;
                }

                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(pos, map, 2);
                if (!spawnCell.IsValid || !spawnCell.InBounds(map))
                {
                    spawnCell = pos;
                }

                GenSpawn.Spawn(runt, spawnCell, map);
                runt.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, forceWake: true);
            }
            catch (Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to spawn runt from pregnant boomer burst: {ex}");
            }
        }

        public static void DoAcidBurst(Pawn pawn)
        {
            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 center))
            {
                return;
            }

            DoAcidBurst(map, center, pawn);
        }

        public static void ApplyAcidCorrosion(Pawn pawn, float severityGain)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || severityGain <= 0f)
            {
                return;
            }

            AddOrRefreshDamageOverTimeHediff(pawn, ZombieDefOf.CZH_ZombieAcidCorrosion, severityGain, 0.75f);
        }

        public static void ApplyZombieBloodExposure(Pawn pawn, float severityGain)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || severityGain <= 0f || ZombieDefOf.CZH_ZombieBloodSepsis == null)
            {
                return;
            }

            if (!ZombieTraitUtility.CanCatchZombieSickness(pawn))
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet?.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieBloodSepsis);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieBloodSepsis, pawn);
                existing.Severity = 1f;
                pawn.health.AddHediff(existing);
            }
            else
            {
                existing.Severity = 1f;
            }

            if (existing is HediffWithComps withComps)
            {
                HediffComp_InfectedBloodExposure exposureComp = withComps.TryGetComp<HediffComp_InfectedBloodExposure>();
                exposureComp?.RefreshDuration(GenDate.TicksPerDay);
            }
        }

        public static void ApplyPukedOn(Pawn pawn, int durationTicks = 1200)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || ZombieDefOf.CZH_PukedOn == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet?.GetFirstHediffOfDef(ZombieDefOf.CZH_PukedOn);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(ZombieDefOf.CZH_PukedOn, pawn);
                existing.Severity = 1f;
                pawn.health.AddHediff(existing);
            }

            if (existing is HediffWithComps withComps)
            {
                HediffComp_TemporaryStatus timed = withComps.TryGetComp<HediffComp_TemporaryStatus>();
                if (timed != null)
                {
                    timed.RefreshDuration(durationTicks);
                }
            }
        }

        public static void TryApplyAcidPukedOn(Pawn pawn, int durationTicks = 1200)
        {
            if (pawn == null || pawn.Dead || ZombieUtility.ShouldZombiesIgnore(pawn))
            {
                return;
            }

            ApplyPukedOn(pawn, durationTicks);
        }

        private static void AddOrRefreshDamageOverTimeHediff(Pawn pawn, HediffDef hediffDef, float severityGain, float maxSeverity)
        {
            if (pawn == null || hediffDef == null || pawn.health == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet?.GetFirstHediffOfDef(hediffDef);
            if (existing == null)
            {
                existing = HediffMaker.MakeHediff(hediffDef, pawn);
                existing.Severity = Mathf.Clamp(severityGain, 0.01f, maxSeverity);
                pawn.health.AddHediff(existing);
                return;
            }

            existing.Severity = Mathf.Clamp(existing.Severity + severityGain, 0.01f, maxSeverity);
        }

        private static void DoAcidBurst(Map map, IntVec3 center, Pawn instigator)
        {
            if (map == null || !center.IsValid || !center.InBounds(map))
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2.9f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, map, ZombieDefOf.CZH_Filth_ZombieAcid);
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn target && target != instigator && !ZombieUtility.IsZombie(target))
                    {
                        float damage = Rand.Range(7f, 12f);
                        target.TakeDamage(new DamageInfo(ZombieDefOf.CZH_ZombieAcidBurn ?? DamageDefOf.Burn, damage, 0f, -1f, instigator));
                        ApplyAcidCorrosion(target, 0.42f);
                        TryApplyAcidPukedOn(target, 1200);
                    }
                }
            }
        }

        private static bool TryGetEffectLocation(Pawn pawn, out Map map, out IntVec3 pos)
        {
            map = pawn?.MapHeld;
            if (map != null)
            {
                pos = pawn.PositionHeld;
                return pos.IsValid && pos.InBounds(map);
            }

            Corpse corpse = pawn?.Corpse;
            map = corpse?.MapHeld;
            if (map != null)
            {
                pos = corpse.PositionHeld;
                return pos.IsValid && pos.InBounds(map);
            }

            pos = IntVec3.Invalid;
            return false;
        }

        public static void DoSicknessBurst(Pawn pawn)
        {
            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 center))
            {
                return;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2.4f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, map, ZombieDefOf.CZH_Filth_SickZombieBlood);
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Pawn target && target != pawn && !target.Dead && !ZombieUtility.ShouldZombiesIgnore(target))
                    {
                        ZombieTraitUtility.TryApplyZombieSickness(target, 0.22f);
                    }
                }
            }
        }

        public static void HandleSickSpitAttack(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Sick) || pawn?.MapHeld == null || pawn.Dead || pawn.Downed || pawn.stances?.stunner?.Stunned == true)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int pawnId = pawn.thingIDNumber;
            if (PendingSickSpewsByPawn.ContainsKey(pawnId))
            {
                return;
            }

            if (LastSickSpitTickByPawn.TryGetValue(pawnId, out int lastSpitTick) && ticksGame - lastSpitTick < SickSpitCooldownTicks)
            {
                return;
            }

            Pawn prey = FindClosestLivingPrey(pawn, SickSpitMaxRange);
            if (prey == null || prey.Dead || prey.Destroyed || prey.MapHeld != pawn.MapHeld)
            {
                return;
            }

            float distanceSquared = pawn.PositionHeld.DistanceToSquared(prey.PositionHeld);
            if (distanceSquared < SickSpitMinRange * SickSpitMinRange || distanceSquared > SickSpitMaxRange * SickSpitMaxRange)
            {
                return;
            }

            if (!GenSight.LineOfSight(pawn.PositionHeld, prey.PositionHeld, pawn.MapHeld))
            {
                return;
            }

            if (pawn.CurJobDef == JobDefOf.AttackMelee && distanceSquared <= 5.8f)
            {
                return;
            }

            if (!Rand.Chance(SickSpitChancePerCheck))
            {
                return;
            }

            BeginSickSpewWarmup(pawn, prey, ticksGame);
        }

        public static void TickPendingSickSpewWarmups()
        {
            if (PendingSickSpewsByPawn.Count == 0)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            List<int> pawnIds = PendingSickSpewsByPawn.Keys.ToList();
            foreach (int pawnId in pawnIds)
            {
                if (!PendingSickSpewsByPawn.TryGetValue(pawnId, out PendingSickSpewState state))
                {
                    continue;
                }

                Pawn spewer = FindSpewPawnById(state.SpewerPawnId);
                if (spewer == null || spewer.Dead || spewer.Destroyed || spewer.MapHeld == null || !spewer.Spawned || spewer.Downed || spewer.stances?.stunner?.Stunned == true)
                {
                    PendingSickSpewsByPawn.Remove(pawnId);
                    continue;
                }

                Pawn target = FindSpewTargetById(spewer.MapHeld, state.TargetPawnId);
                if (target != null && !target.Dead && !target.Destroyed)
                {
                    state.LastTargetCell = target.PositionHeld;
                }

                if (!state.LastTargetCell.IsValid || !state.LastTargetCell.InBounds(spewer.MapHeld))
                {
                    PendingSickSpewsByPawn.Remove(pawnId);
                    continue;
                }

                spewer.pather?.StopDead();
                spewer.rotationTracker?.FaceTarget(new LocalTargetInfo(state.LastTargetCell));

                if (ticksGame >= state.NextPreviewTick)
                {
                    ShowSickSpewPreview(spewer, state.LastTargetCell);
                }

                if (ticksGame >= state.ExecuteTick)
                {
                    if (TryDoSickSpew(spewer, target, state.LastTargetCell))
                    {
                        LastSickSpitTickByPawn[pawnId] = ticksGame;
                    }

                    PendingSickSpewsByPawn.Remove(pawnId);
                    continue;
                }

                state.NextPreviewTick = ticksGame + SickSpewPreviewIntervalTicks;
                PendingSickSpewsByPawn[pawnId] = state;
            }
        }

        private static void BeginSickSpewWarmup(Pawn spewer, Pawn target, int ticksGame)
        {
            if (spewer == null || target == null)
            {
                return;
            }

            PendingSickSpewsByPawn[spewer.thingIDNumber] = new PendingSickSpewState
            {
                SpewerPawnId = spewer.thingIDNumber,
                TargetPawnId = target.thingIDNumber,
                ExecuteTick = ticksGame + SickSpewWarmupTicks,
                NextPreviewTick = ticksGame,
                LastTargetCell = target.PositionHeld
            };

            spewer.pather?.StopDead();
            spewer.rotationTracker?.FaceTarget(new LocalTargetInfo(target));
            ShowSickSpewPreview(spewer, target.PositionHeld, playSound: true);
        }

        private static bool TryDoSickSpew(Pawn spewer, Pawn target, IntVec3 fallbackTargetCell)
        {
            if (spewer == null || spewer.MapHeld == null || !spewer.Spawned)
            {
                return false;
            }

            try
            {
                Map map = spewer.MapHeld;
                IntVec3 targetCell = target != null && !target.Dead && !target.Destroyed ? target.PositionHeld : fallbackTargetCell;
                if (!targetCell.IsValid || !targetCell.InBounds(map))
                {
                    return false;
                }

                ThingDef sickFilth = ZombieDefOf.CZH_Filth_SickZombieBlood ?? ThingDefOf.Filth_Blood;
                HashSet<IntVec3> affectedCells = GetSickSpewCells(spewer, targetCell, map);
                if (affectedCells.Count == 0)
                {
                    return false;
                }

                ShowSickSpewPreview(spewer, targetCell, playSound: true);

                Dictionary<int, float> severityByPawnId = new Dictionary<int, float>();

                foreach (IntVec3 cell in affectedCells)
                {
                    FilthMaker.TryMakeFilth(cell, map, sickFilth);

                    float severity = cell.DistanceToSquared(targetCell) <= 2.25f
                        ? SickSpewTargetSeverity
                        : SickSpewBaseSeverity;

                    List<Thing> things = cell.GetThingList(map);
                    for (int i = 0; i < things.Count; i++)
                    {
                        if (!(things[i] is Pawn victim) || victim == spewer || victim.Dead || ZombieUtility.ShouldZombiesIgnore(victim))
                        {
                            continue;
                        }

                        int victimId = victim.thingIDNumber;
                        if (severityByPawnId.TryGetValue(victimId, out float existingSeverity))
                        {
                            if (severity > existingSeverity)
                            {
                                severityByPawnId[victimId] = severity;
                            }
                        }
                        else
                        {
                            severityByPawnId[victimId] = severity;
                        }
                    }
                }

                foreach (KeyValuePair<int, float> entry in severityByPawnId)
                {
                    Pawn victim = map.mapPawns?.AllPawnsSpawned?.FirstOrDefault(p => p.thingIDNumber == entry.Key);
                    if (victim != null && !victim.Dead)
                    {
                        ZombieTraitUtility.TryApplyZombieSickness(victim, entry.Value);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to apply sick spew: {ex}");
                return false;
            }
        }

        private static HashSet<IntVec3> GetSickSpewCells(Pawn spewer, IntVec3 targetCell, Map map)
        {
            HashSet<IntVec3> cells = new HashSet<IntVec3>();
            if (spewer == null || !targetCell.IsValid || map == null)
            {
                return cells;
            }

            Vector3 start = spewer.DrawPos;
            Vector3 end = targetCell.ToVector3Shifted();
            float distance = Mathf.Min(SickSpitMaxRange, Mathf.Max(1f, Vector3.Distance(start, end)));
            int steps = Mathf.Max(4, Mathf.CeilToInt(distance * 1.75f));

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 point = Vector3.Lerp(start, end, t);
                IntVec3 centerCell = new IntVec3(Mathf.RoundToInt(point.x), 0, Mathf.RoundToInt(point.z));
                float radius = Mathf.Max(SickSpewMinCellRadius, Mathf.Lerp(0.18f, SickSpewEndWidth * 0.5f, t));

                foreach (IntVec3 cell in GenRadial.RadialCellsAround(centerCell, radius, true))
                {
                    if (cell.InBounds(map) && cell != spewer.PositionHeld)
                    {
                        cells.Add(cell);
                    }
                }
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, SickSpewEndWidth * 0.5f, true))
            {
                if (cell.InBounds(map) && cell != spewer.PositionHeld)
                {
                    cells.Add(cell);
                }
            }

            return cells;
        }

        private static Pawn FindSpewPawnById(int pawnId)
        {
            foreach (Map map in Find.Maps)
            {
                Pawn pawn = map.mapPawns?.AllPawnsSpawned?.FirstOrDefault(p => p.thingIDNumber == pawnId);
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static Pawn FindSpewTargetById(Map map, int pawnId)
        {
            return map?.mapPawns?.AllPawnsSpawned?.FirstOrDefault(p => p.thingIDNumber == pawnId);
        }

        private static void ShowSickSpewPreview(Pawn spewer, IntVec3 targetCell, bool playSound = false)
        {
            if (spewer?.MapHeld == null || !targetCell.IsValid || !targetCell.InBounds(spewer.MapHeld))
            {
                return;
            }

            try
            {
                EffecterDef fireSpewEffecter = DefDatabase<EffecterDef>.GetNamedSilentFail("Fire_Spew");
                if (fireSpewEffecter != null)
                {
                    Effecter effecter = fireSpewEffecter.Spawn();
                    effecter?.Trigger(new TargetInfo(spewer.PositionHeld, spewer.MapHeld), new TargetInfo(targetCell, spewer.MapHeld));
                    effecter?.Cleanup();
                }
            }
            catch
            {
            }

            if (!playSound)
            {
                return;
            }

            try
            {
                SoundDef spewSound = DefDatabase<SoundDef>.GetNamedSilentFail("Pawn_Impid_FireSpew");
                if (spewSound != null)
                {
                    SoundStarter.PlayOneShot(spewSound, SoundInfo.InMap(new TargetInfo(spewer.PositionHeld, spewer.MapHeld), MaintenanceType.None));
                }
            }
            catch
            {
            }
        }

        public static void HandleSickBloodContact(Map map)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            ThingDef acidDef = ZombieDefOf.CZH_Filth_ZombieAcid;
            ThingDef bloodDef = ZombieDefOf.CZH_Filth_ZombieBlood;
            ThingDef sickBloodDef = ZombieDefOf.CZH_Filth_SickZombieBlood;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn == null || pawn.Dead || ZombieUtility.ShouldZombiesIgnore(pawn))
                {
                    continue;
                }

                List<Thing> things = pawn.PositionHeld.GetThingList(map);
                bool hasAcid = false;
                bool hasBlood = false;
                bool hasSickBlood = false;
                for (int i = 0; i < things.Count; i++)
                {
                    ThingDef def = things[i]?.def;
                    if (acidDef != null && def == acidDef)
                    {
                        hasAcid = true;
                    }
                    else if (bloodDef != null && def == bloodDef)
                    {
                        hasBlood = true;
                    }
                    else if (sickBloodDef != null && def == sickBloodDef)
                    {
                        hasSickBlood = true;
                    }

                    if (hasAcid && hasBlood && hasSickBlood)
                    {
                        break;
                    }
                }

                if (hasAcid)
                {
                    ApplyAcidCorrosion(pawn, 0.12f);
                }

                if (hasBlood)
                {
                    ApplyZombieBloodExposure(pawn, 0.04f);
                }

                if (hasSickBlood)
                {
                    ApplyZombieBloodExposure(pawn, 0.05f);
                }
            }
        }

        public static bool IsRainActive(Map map)
        {
            if (map?.weatherManager == null)
            {
                return false;
            }

            try
            {
                if (map.weatherManager.RainRate > 0.02f)
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                WeatherDef weather = map.weatherManager.curWeather;
                return weather != null && weather.rainRate > 0.02f;
            }
            catch
            {
                return false;
            }
        }

        public static bool ShouldDrownedRoamFreely(Pawn pawn)
        {
            return pawn?.MapHeld != null && IsRainActive(pawn.MapHeld);
        }

        public static bool ShouldDrownedHoldWater(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || ShouldDrownedRoamFreely(pawn) || ZombieUtility.IsUnderColonyRestraint(pawn))
            {
                return false;
            }

            Pawn prey = FindClosestLivingPrey(pawn, 10f);
            return prey == null;
        }

        public static bool HasValidDrownedWaterReturnJob(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || pawn.jobs == null)
            {
                return false;
            }

            Job currentJob = pawn.CurJob;
            if (currentJob == null || currentJob.def != JobDefOf.Goto || !currentJob.targetA.IsValid)
            {
                return false;
            }

            IntVec3 targetCell = currentJob.targetA.Cell;
            return targetCell.IsValid && targetCell.InBounds(pawn.MapHeld) && ZombieUtility.IsWaterCell(targetCell, pawn.MapHeld);
        }

        public static bool TryStartDrownedReturnToWater(Pawn pawn, float radius = 45f)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || pawn.jobs == null)
            {
                return false;
            }

            if (HasValidDrownedWaterReturnJob(pawn))
            {
                return true;
            }

            IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, radius);
            if (!waterCell.IsValid)
            {
                waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, Mathf.Max(pawn.MapHeld.Size.x, pawn.MapHeld.Size.z));
            }

            if (!waterCell.IsValid || !waterCell.InBounds(pawn.MapHeld) || waterCell == pawn.PositionHeld)
            {
                return false;
            }

            Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
            returnToWater.expiryInterval = 1800;
            returnToWater.checkOverrideOnExpire = true;
            returnToWater.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
            pawn.jobs.TryTakeOrderedJob(returnToWater, JobTag.Misc);
            return true;
        }

        public static bool TryStartDrownedWaterWander(Pawn pawn, float radius = 8f)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || pawn.jobs == null)
            {
                return false;
            }

            if (HasValidDrownedWaterReturnJob(pawn))
            {
                return true;
            }

            List<IntVec3> cells = GenRadial.RadialCellsAround(pawn.PositionHeld, radius, true)
                .Where(cell => cell.IsValid
                    && cell.InBounds(pawn.MapHeld)
                    && cell != pawn.PositionHeld
                    && ZombieUtility.IsWaterCell(cell, pawn.MapHeld)
                    && cell.Walkable(pawn.MapHeld)
                    && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                .ToList();

            if (cells.Count == 0)
            {
                return false;
            }

            IntVec3 wanderCell = cells.RandomElementByWeight(cell =>
            {
                float distance = Mathf.Sqrt(pawn.PositionHeld.DistanceToSquared(cell));
                return 1f / Mathf.Max(1f, distance);
            });

            Job waterWander = JobMaker.MakeJob(JobDefOf.Goto, wanderCell);
            waterWander.expiryInterval = 900;
            waterWander.checkOverrideOnExpire = true;
            waterWander.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
            pawn.jobs.TryTakeOrderedJob(waterWander, JobTag.Misc);
            return true;
        }

        public static void HandleDrownedBehavior(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || pawn.jobs == null || ZombieUtility.IsUnderColonyRestraint(pawn))
            {
                return;
            }

            if (pawn.Downed || ZombieFeignDeathUtility.IsFeigningDeath(pawn))
            {
                return;
            }

            if (ShouldDrownedRoamFreely(pawn))
            {
                return;
            }

            bool inWater = ZombieUtility.IsWaterCell(pawn.PositionHeld, pawn.MapHeld);
            Pawn prey = FindClosestLivingPrey(pawn, 10f);
            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;

            if (currentTarget != null)
            {
                bool validTarget = !currentTarget.Dead && !currentTarget.Destroyed && !ZombieUtility.ShouldZombieIgnoreTarget(pawn, currentTarget);
                if (!validTarget || pawn.PositionHeld.DistanceToSquared(currentTarget.PositionHeld) > 12f * 12f)
                {
                    currentTarget = null;
                }
            }

            if (prey == null && currentTarget == null)
            {
                if (inWater)
                {
                    if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee)
                    {
                        pawn.jobs.StopAll();
                    }

                    TryStartDrownedWaterWander(pawn);
                    return;
                }

                TryStartDrownedReturnToWater(pawn, 60f);
                return;
            }

            if (!inWater && prey != null && pawn.PositionHeld.DistanceToSquared(prey.PositionHeld) > 12f * 12f)
            {
                TryStartDrownedReturnToWater(pawn, 60f);
                return;
            }

            if (!inWater && currentTarget != null && pawn.PositionHeld.DistanceToSquared(currentTarget.PositionHeld) > 12f * 12f)
            {
                TryStartDrownedReturnToWater(pawn, 60f);
                return;
            }

            if (!inWater && prey == null && currentTarget == null)
            {
                TryStartDrownedReturnToWater(pawn, 60f);
            }
        }

        private static IntVec3 FindNearestWaterCell(IntVec3 origin, Map map, float radius)
        {
            float radiusSquared = radius * radius;
            IntVec3 best = IntVec3.Invalid;
            float bestDistance = float.MaxValue;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!ZombieUtility.IsWaterCell(cell, map) || !cell.Walkable(map))
                {
                    continue;
                }

                float distance = origin.DistanceToSquared(cell);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                best = cell;
                bestDistance = distance;
            }

            return best;
        }
    }
}
