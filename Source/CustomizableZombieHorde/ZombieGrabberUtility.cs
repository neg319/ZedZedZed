using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CustomizableZombieHorde
{
    public static class ZombieGrabberUtility
    {
        public const float TongueMinRange = 2.2f;
        public const float TongueMaxRange = 12f;
        public const float HoldStartRange = 1.6f;
        private const float HoldPersistRange = 2.6f;
        private const int EscapeRollInterval = 60;
        private const int HoldMaintenanceInterval = 15;

        private sealed class PullState
        {
            public int targetId;
            public int expireTick;
            public int nextEscapeTick;
            public int nextMaintenanceTick;
            public IntVec3 holdCell;
        }

        private static readonly Dictionary<int, PullState> ActivePulls = new Dictionary<int, PullState>();

        public static void TickGrabbers()
        {
            if (Current.Game == null || Find.TickManager == null)
            {
                ActivePulls.Clear();
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            CleanupInvalidPulls(ticksGame);

            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) || pawn.Dead || pawn.Destroyed || pawn.Downed || !pawn.Spawned)
                    {
                        continue;
                    }

                    if (HasActiveTongue(pawn))
                    {
                        UpdateActivePull(pawn, ticksGame);
                    }
                    else
                    {
                        TryStartNewPull(pawn);
                    }
                }
            }
        }

        public static bool HasActiveTongue(Pawn pawn)
        {
            return pawn != null && ActivePulls.TryGetValue(pawn.thingIDNumber, out PullState state) && ResolveTarget(pawn, state) != null;
        }

        public static bool TryForceTongueStart(Pawn grabber, Pawn prey)
        {
            return TryBeginPull(grabber, prey);
        }

        public static bool TryBeginPull(Pawn grabber, Pawn prey)
        {
            if (!CanTongueTarget(grabber, prey, HoldPersistRange, requireOpenPullSlot: true))
            {
                return false;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            ActivePulls[grabber.thingIDNumber] = new PullState
            {
                targetId = prey.thingIDNumber,
                expireTick = ticksGame + 3600,
                nextEscapeTick = ticksGame + EscapeRollInterval,
                nextMaintenanceTick = ticksGame,
                holdCell = prey.PositionHeld
            };

            InterruptPrey(prey);
            ForceFaceTarget(grabber, prey);
            HoldTargetInPlace(prey, prey.PositionHeld);
            TryQueueAttack(grabber, prey);
            ZombieFeedbackUtility.TrySendGrabberPullWarning(prey, grabber);
            return true;
        }

        public static Pawn GetTongueTarget(Pawn pawn)
        {
            if (pawn == null || !ActivePulls.TryGetValue(pawn.thingIDNumber, out PullState state))
            {
                return null;
            }

            return ResolveTarget(pawn, state);
        }

        private static void TryStartNewPull(Pawn grabber)
        {
            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(grabber, TongueMaxRange);
            if (prey == null)
            {
                return;
            }

            float distanceSquared = prey.PositionHeld.DistanceToSquared(grabber.PositionHeld);
            if (distanceSquared <= HoldStartRange * HoldStartRange)
            {
                TryBeginPull(grabber, prey);
                return;
            }

            TryQueueApproach(grabber, prey);
        }

        private static void UpdateActivePull(Pawn grabber, int ticksGame)
        {
            if (!ActivePulls.TryGetValue(grabber.thingIDNumber, out PullState state))
            {
                return;
            }

            Pawn prey = ResolveTarget(grabber, state);
            if (prey == null)
            {
                ActivePulls.Remove(grabber.thingIDNumber);
                return;
            }

            float distanceSquared = grabber.PositionHeld.DistanceToSquared(prey.PositionHeld);
            if (distanceSquared > HoldPersistRange * HoldPersistRange)
            {
                ActivePulls.Remove(grabber.thingIDNumber);
                ZombieFeedbackUtility.TrySendGrabberEscapeMessage(prey, grabber);
                return;
            }

            ForceFaceTarget(grabber, prey);
            if (state.nextMaintenanceTick <= ticksGame)
            {
                HoldTargetInPlace(prey, state.holdCell);
                InterruptPrey(prey);
                TryQueueAttack(grabber, prey);
                state.nextMaintenanceTick = ticksGame + HoldMaintenanceInterval;
            }

            if (state.nextEscapeTick <= ticksGame)
            {
                if (TryResolveEscape(prey, grabber))
                {
                    ActivePulls.Remove(grabber.thingIDNumber);
                    ZombieFeedbackUtility.TrySendGrabberEscapeMessage(prey, grabber);
                    return;
                }

                state.nextEscapeTick = ticksGame + EscapeRollInterval;
                state.expireTick = ticksGame + 3600;
            }
        }

        private static void CleanupInvalidPulls(int ticksGame)
        {
            List<int> removeIds = new List<int>();
            foreach (KeyValuePair<int, PullState> pair in ActivePulls)
            {
                if (pair.Value == null || pair.Value.expireTick <= ticksGame)
                {
                    removeIds.Add(pair.Key);
                    continue;
                }

                Pawn grabber = FindThing(pair.Key) as Pawn;
                if (grabber == null || grabber.Dead || grabber.Destroyed || !grabber.Spawned)
                {
                    removeIds.Add(pair.Key);
                    continue;
                }

                Pawn prey = ResolveTarget(grabber, pair.Value);
                if (prey == null)
                {
                    removeIds.Add(pair.Key);
                    continue;
                }
            }

            foreach (int id in removeIds)
            {
                ActivePulls.Remove(id);
            }
        }

        private static Pawn ResolveTarget(Pawn grabber, PullState state)
        {
            Pawn prey = FindThing(state.targetId) as Pawn;
            if (prey == null || prey.Dead || prey.Destroyed || !prey.Spawned || ZombieUtility.ShouldZombieIgnoreTarget(grabber, prey) || prey.Map != grabber.Map)
            {
                return null;
            }

            if (!GenSight.LineOfSight(grabber.PositionHeld, prey.PositionHeld, grabber.MapHeld))
            {
                return null;
            }

            if (grabber.PositionHeld.DistanceToSquared(prey.PositionHeld) > HoldPersistRange * HoldPersistRange)
            {
                return null;
            }

            return prey;
        }

        private static bool CanTongueTarget(Pawn grabber, Pawn prey, float maxRange, bool requireOpenPullSlot)
        {
            if (grabber == null || prey == null || grabber.Dead || prey.Dead || grabber.MapHeld == null || prey.MapHeld != grabber.MapHeld)
            {
                return false;
            }

            if (!ZombieUtility.IsVariant(grabber, ZombieVariant.Grabber) || ZombieUtility.ShouldZombieIgnoreTarget(grabber, prey) || prey.Downed)
            {
                return false;
            }

            float distanceSquared = prey.PositionHeld.DistanceToSquared(grabber.PositionHeld);
            if (distanceSquared > maxRange * maxRange)
            {
                return false;
            }

            if (!GenSight.LineOfSight(grabber.PositionHeld, prey.PositionHeld, grabber.MapHeld))
            {
                return false;
            }

            if (requireOpenPullSlot && (HasActiveTongue(grabber) || IsTargetAlreadyGrabbed(prey)))
            {
                return false;
            }

            return true;
        }

        private static Thing FindThing(int thingId)
        {
            foreach (Map map in Find.Maps)
            {
                Thing thing = map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == thingId);
                if (thing != null)
                {
                    return thing;
                }
            }

            return null;
        }

        private static bool IsTargetAlreadyGrabbed(Pawn prey)
        {
            return ActivePulls.Values.Any(state => state != null && state.targetId == prey.thingIDNumber);
        }

        private static void InterruptPrey(Pawn prey)
        {
            if (prey == null || prey.Dead)
            {
                return;
            }

            try
            {
                prey.pather?.StopDead();
            }
            catch
            {
            }

            try
            {
                prey.jobs?.StopAll();
            }
            catch
            {
            }
        }

        private static void HoldTargetInPlace(Pawn prey, IntVec3 holdCell)
        {
            if (prey == null || prey.Dead || prey.MapHeld == null)
            {
                return;
            }

            InterruptPrey(prey);

            try
            {
                if (holdCell.IsValid && holdCell.InBounds(prey.MapHeld) && holdCell.Standable(prey.MapHeld) && holdCell.GetEdifice(prey.MapHeld) == null && prey.PositionHeld != holdCell)
                {
                    prey.Position = holdCell;
                }
            }
            catch
            {
            }
        }

        private static bool TryResolveEscape(Pawn prey, Pawn grabber)
        {
            int escapeRoll = Rand.RangeInclusive(1, 20)
                + Mathf.RoundToInt(GetCapacityLevel(prey, PawnCapacityDefOf.Moving, 1f) * 8f)
                + Mathf.RoundToInt(GetCapacityLevel(prey, PawnCapacityDefOf.Manipulation, 1f) * 4f)
                + Mathf.RoundToInt(GetStatValue(prey, StatDefOf.MeleeDodgeChance, 0.08f) * 30f);

            if (prey.Downed)
            {
                escapeRoll -= 8;
            }

            int holdRoll = Rand.RangeInclusive(1, 20)
                + 9
                + Mathf.RoundToInt(GetCapacityLevel(grabber, PawnCapacityDefOf.Moving, 0.6f) * 4f)
                + Mathf.RoundToInt(GetCapacityLevel(grabber, PawnCapacityDefOf.Manipulation, 0.5f) * 5f);

            return escapeRoll >= holdRoll;
        }

        private static float GetCapacityLevel(Pawn pawn, PawnCapacityDef capacity, float fallback)
        {
            if (pawn?.health?.capacities == null || capacity == null)
            {
                return fallback;
            }

            try
            {
                return pawn.health.capacities.GetLevel(capacity);
            }
            catch
            {
                return fallback;
            }
        }

        private static float GetStatValue(Pawn pawn, StatDef stat, float fallback)
        {
            if (pawn == null || stat == null)
            {
                return fallback;
            }

            try
            {
                return pawn.GetStatValue(stat);
            }
            catch
            {
                return fallback;
            }
        }

        private static void TryQueueApproach(Pawn grabber, Pawn prey)
        {
            if (grabber.jobs == null)
            {
                return;
            }

            try
            {
                Job approachJob = JobMaker.MakeJob(JobDefOf.Goto, prey.PositionHeld);
                approachJob.expiryInterval = 45;
                approachJob.checkOverrideOnExpire = true;
                approachJob.locomotionUrgency = LocomotionUrgency.Jog;
                grabber.jobs.TryTakeOrderedJob(approachJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        private static void TryQueueAttack(Pawn grabber, Pawn prey)
        {
            if (grabber.jobs == null)
            {
                return;
            }

            try
            {
                Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, prey);
                attackJob.expiryInterval = 240;
                attackJob.checkOverrideOnExpire = true;
                attackJob.locomotionUrgency = ZombieUtility.GetZombieUrgency(grabber);
                grabber.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        private static void ForceFaceTarget(Pawn grabber, Pawn prey)
        {
            if (grabber == null || prey == null)
            {
                return;
            }

            IntVec3 delta = prey.PositionHeld - grabber.PositionHeld;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.z))
            {
                grabber.Rotation = delta.x >= 0 ? Rot4.East : Rot4.West;
            }
            else if (delta.z != 0)
            {
                grabber.Rotation = delta.z >= 0 ? Rot4.North : Rot4.South;
            }
        }
    }
}
