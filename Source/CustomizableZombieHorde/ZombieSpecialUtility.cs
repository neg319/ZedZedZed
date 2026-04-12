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
        private const int SickSpitCooldownTicks = 468;
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

        public static Pawn FindClosestLivingPrey(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            Pawn bileTarget = FindBestLivingPrey(pawn, Mathf.Max(radius, 22f), true);
            if (bileTarget != null)
            {
                return bileTarget;
            }

            return FindBestLivingPrey(pawn, radius, false);
        }

        private static Pawn FindBestLivingPrey(Pawn pawn, float radius, bool requirePukedOn)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Pawn best = null;
            float bestDistance = float.MaxValue;
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

                if (requirePukedOn && !hasPukedOn)
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(other.PositionHeld);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, other.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                best = other;
                bestDistance = distance;
            }

            return best;
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

            IntVec3 target;
            int x = Mathf.Clamp(pawn.PositionHeld.x, 6, map.Size.x - 7);
            int z = Mathf.Clamp(pawn.PositionHeld.z, 6, map.Size.z - 7);
            switch (direction)
            {
                case ZombieHerdDirection.NorthToSouth:
                    target = new IntVec3(x, 0, 6);
                    break;
                case ZombieHerdDirection.SouthToNorth:
                    target = new IntVec3(x, 0, map.Size.z - 7);
                    break;
                case ZombieHerdDirection.WestToEast:
                    target = new IntVec3(map.Size.x - 7, 0, z);
                    break;
                default:
                    target = new IntVec3(6, 0, z);
                    break;
            }

            if (target.InBounds(map) && target.Standable(map))
            {
                return target;
            }

            for (int radius = 1; radius <= 8; radius++)
            {
                IntVec3 nearby = CellFinder.RandomClosewalkCellNear(target, map, radius);
                if (nearby.IsValid && nearby.Standable(map))
                {
                    return nearby;
                }
            }

            return FindAssaultCell(pawn);
        }

        public static IntVec3 FindAssaultCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 targetCenter = GetPlayerBaseCenter(map);
            if (targetCenter.IsValid && targetCenter.InBounds(map) && targetCenter.Standable(map))
            {
                return targetCenter;
            }

            List<IntVec3> innerOptions = GenRadial.RadialCellsAround(targetCenter, 10f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 4)
                .OrderBy(cell => cell.DistanceToSquared(targetCenter))
                .ToList();
            if (innerOptions.Count > 0)
            {
                return innerOptions.First();
            }

            List<IntVec3> fallbackOptions = GenRadial.RadialCellsAround(targetCenter, 18f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 4)
                .OrderBy(cell => cell.DistanceToSquared(targetCenter))
                .ToList();
            if (fallbackOptions.Count > 0)
            {
                return fallbackOptions.First();
            }

            return targetCenter;
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

            return PickRollingCellForPawn(pawn, options, 1800);
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
                return PickRollingCellForPawn(pawn, baseOptions, 2400);
            }

            List<IntVec3> interiorOptions = GenRadial.RadialCellsAround(map.Center, 28f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 12)
                .OrderBy(cell => cell.x)
                .ThenBy(cell => cell.z)
                .ToList();
            if (interiorOptions.Count > 0)
            {
                return PickRollingCellForPawn(pawn, interiorOptions, 2400);
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

            List<IntVec3> options = GenRadial.RadialCellsAround(pawn.PositionHeld, 14f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 16)
                .ToList();

            if (options.Count > 0)
            {
                int step = ((pawn.thingIDNumber / 11) + ((Find.TickManager?.TicksGame ?? 0) / 3200)) % options.Count;
                return options[step];
            }

            return FindInteriorNearEdgeCell(map, pawn.PositionHeld);
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
                return options.RandomElement();
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

        public static IntVec3 GetPlayerBaseCenter(Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

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

                return new IntVec3(sumX / homeCells.Count, 0, sumZ / homeCells.Count);
            }

            if (map.mapPawns?.FreeColonistsSpawned != null && map.mapPawns.FreeColonistsSpawned.Count > 0)
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
                    return new IntVec3(sumX / count, 0, sumZ / count);
                }
            }

            return map.Center;
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

            List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
            foreach (Corpse corpse in corpses)
            {
                if (corpse.Destroyed || ZombieUtility.IsZombie(corpse.InnerPawn))
                {
                    continue;
                }

                int nearbyZombies = map.mapPawns.AllPawnsSpawned.Count(pawn => ZombieUtility.IsZombie(pawn) && !pawn.Dead && !pawn.Destroyed && pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld) <= 2.9f * 2.9f);
                if (nearbyZombies <= 0)
                {
                    continue;
                }

                for (int i = 0; i < nearbyZombies; i++)
                {
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                }

                if (Rand.Chance(0.10f * nearbyZombies))
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

            foreach (IntVec3 cell in map.AllCells)
            {
                if (ZombieUtility.IsWaterCell(cell, map))
                {
                    return true;
                }
            }

            return false;
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
            if (pawn == null || pawn.Dead || pawn.health == null || severityGain <= 0f)
            {
                return;
            }

            AddOrRefreshDamageOverTimeHediff(pawn, ZombieDefOf.CZH_ZombieBloodSepsis, severityGain, 0.35f);
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
            if (map == null)
            {
                return;
            }

            if (ZombieDefOf.CZH_Filth_ZombieAcid != null)
            {
                List<Thing> acidFilth = map.listerThings.ThingsOfDef(ZombieDefOf.CZH_Filth_ZombieAcid);
                for (int i = 0; i < acidFilth.Count; i++)
                {
                    IntVec3 cell = acidFilth[i].Position;
                    List<Thing> things = cell.GetThingList(map);
                    for (int j = 0; j < things.Count; j++)
                    {
                        if (things[j] is Pawn pawn && !pawn.Dead && !ZombieUtility.ShouldZombiesIgnore(pawn))
                        {
                            ApplyAcidCorrosion(pawn, 0.12f);
                        }
                    }
                }
            }

            if (ZombieDefOf.CZH_Filth_ZombieBlood != null)
            {
                List<Thing> bloodFilth = map.listerThings.ThingsOfDef(ZombieDefOf.CZH_Filth_ZombieBlood);
                for (int i = 0; i < bloodFilth.Count; i++)
                {
                    IntVec3 cell = bloodFilth[i].Position;
                    List<Thing> things = cell.GetThingList(map);
                    for (int j = 0; j < things.Count; j++)
                    {
                        if (things[j] is Pawn pawn && !pawn.Dead && !ZombieUtility.ShouldZombiesIgnore(pawn))
                        {
                            ApplyZombieBloodExposure(pawn, 0.04f);
                        }
                    }
                }
            }

            if (ZombieDefOf.CZH_Filth_SickZombieBlood == null)
            {
                return;
            }

            List<Thing> filth = map.listerThings.ThingsOfDef(ZombieDefOf.CZH_Filth_SickZombieBlood);
            for (int i = 0; i < filth.Count; i++)
            {
                IntVec3 cell = filth[i].Position;
                List<Thing> things = cell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Pawn pawn && !pawn.Dead && !ZombieUtility.ShouldZombiesIgnore(pawn))
                    {
                        ZombieTraitUtility.TryApplyZombieSickness(pawn, 0.035f);
                        ApplyZombieBloodExposure(pawn, 0.05f);
                    }
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

            Pawn prey = FindClosestLivingPrey(pawn, 18f);
            return prey == null;
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
            Pawn prey = FindClosestLivingPrey(pawn, 18f);
            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;

            if (currentTarget != null)
            {
                bool validTarget = !currentTarget.Dead && !currentTarget.Destroyed && !ZombieUtility.ShouldZombieIgnoreTarget(pawn, currentTarget);
                if (!validTarget || pawn.PositionHeld.DistanceToSquared(currentTarget.PositionHeld) > 24f * 24f)
                {
                    currentTarget = null;
                }
            }

            if (prey == null && currentTarget == null)
            {
                if (inWater)
                {
                    if (pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.AttackMelee || pawn.CurJob.def == JobDefOf.Goto))
                    {
                        pawn.jobs.StopAll();
                    }

                    return;
                }

                IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 35f);
                if (waterCell.IsValid && (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Goto || pawn.CurJob.targetA.Cell != waterCell))
                {
                    Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    returnToWater.expiryInterval = 900;
                    returnToWater.checkOverrideOnExpire = true;
                    returnToWater.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(returnToWater, JobTag.Misc);
                }

                return;
            }

            if (!inWater && prey != null && pawn.PositionHeld.DistanceToSquared(prey.PositionHeld) > 24f * 24f)
            {
                IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 35f);
                if (waterCell.IsValid)
                {
                    Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    returnToWater.expiryInterval = 900;
                    returnToWater.checkOverrideOnExpire = true;
                    returnToWater.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(returnToWater, JobTag.Misc);
                }
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
