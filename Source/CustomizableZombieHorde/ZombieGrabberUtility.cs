
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
        private sealed class PullState
        {
            public int targetId;
            public int expireTick;
            public int nextPullTick;
            public int nextRepathTick;
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
                        TryStartNewPull(pawn, ticksGame);
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
            if (grabber == null || prey == null || grabber.Dead || prey.Dead || grabber.MapHeld == null || prey.MapHeld != grabber.MapHeld)
            {
                return false;
            }

            if (!ZombieUtility.IsVariant(grabber, ZombieVariant.Grabber) || ZombieUtility.ShouldZombiesIgnore(prey) || prey.Downed)
            {
                return false;
            }

            float distanceSquared = prey.PositionHeld.DistanceToSquared(grabber.PositionHeld);
            if (distanceSquared < 2.2f * 2.2f || distanceSquared > 26f * 26f)
            {
                return false;
            }

            if (!GenSight.LineOfSight(grabber.PositionHeld, prey.PositionHeld, grabber.MapHeld) || IsTargetAlreadyGrabbed(prey))
            {
                return false;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            ActivePulls[grabber.thingIDNumber] = new PullState
            {
                targetId = prey.thingIDNumber,
                expireTick = ticksGame + Rand.RangeInclusive(520, 900),
                nextPullTick = ticksGame,
                nextRepathTick = ticksGame
            };
            ForceFaceTarget(grabber, prey);
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

        private static void TryStartNewPull(Pawn grabber, int ticksGame)
        {
            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(grabber, 26f);
            if (prey == null)
            {
                return;
            }

            TryForceTongueStart(grabber, prey);
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
            if (distanceSquared <= 1.6f * 1.6f)
            {
                ActivePulls.Remove(grabber.thingIDNumber);
                TryQueueAttack(grabber, prey);
                return;
            }

            ForceFaceTarget(grabber, prey);

            if (state.nextPullTick <= ticksGame)
            {
                IntVec3 pullCell = FindPullCellTowardGrabber(grabber, prey);
                if (pullCell.IsValid && pullCell != prey.PositionHeld)
                {
                    if (TryPullTarget(prey, pullCell))
                    {
                        state.expireTick = ticksGame + 150;
                        state.nextPullTick = ticksGame + 4;
                    }
                    else
                    {
                        state.nextPullTick = ticksGame + 8;
                    }
                }
                else
                {
                    state.nextPullTick = ticksGame + 8;
                }
            }

            if (state.nextRepathTick <= ticksGame)
            {
                TryQueueApproach(grabber, prey);
                state.nextRepathTick = ticksGame + 15;
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
            if (prey == null || prey.Dead || prey.Destroyed || !prey.Spawned || ZombieUtility.ShouldZombiesIgnore(prey) || prey.Map != grabber.Map)
            {
                return null;
            }

            if (!GenSight.LineOfSight(grabber.PositionHeld, prey.PositionHeld, grabber.MapHeld))
            {
                return null;
            }

            if (grabber.PositionHeld.DistanceToSquared(prey.PositionHeld) > 26f * 26f)
            {
                return null;
            }

            return prey;
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

        private static IntVec3 FindPullCellTowardGrabber(Pawn grabber, Pawn prey)
        {
            Map map = prey.MapHeld;
            IntVec3 best = prey.PositionHeld;
            float bestDistance = prey.PositionHeld.DistanceToSquared(grabber.PositionHeld);
            IntVec3[] offsets =
            {
                IntVec3.Zero,
                new IntVec3(1, 0, 0),
                new IntVec3(-1, 0, 0),
                new IntVec3(0, 0, 1),
                new IntVec3(0, 0, -1),
                new IntVec3(1, 0, 1),
                new IntVec3(1, 0, -1),
                new IntVec3(-1, 0, 1),
                new IntVec3(-1, 0, -1)
            };

            foreach (IntVec3 cell in offsets)
            {
                IntVec3 candidate = prey.PositionHeld + cell;
                if (!candidate.InBounds(map) || !candidate.Standable(map) || candidate.GetEdifice(map) != null)
                {
                    continue;
                }

                float distance = candidate.DistanceToSquared(grabber.PositionHeld);
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool TryPullTarget(Pawn prey, IntVec3 pullCell)
        {
            if (prey == null || prey.Dead || !pullCell.IsValid || prey.PositionHeld == pullCell)
            {
                return false;
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

            try
            {
                if (pullCell.InBounds(prey.MapHeld) && pullCell.Standable(prey.MapHeld) && pullCell.GetEdifice(prey.MapHeld) == null)
                {
                    prey.Position = pullCell;
                    if (prey.jobs != null)
                    {
                        prey.jobs.StopAll();
                    }
                    return true;
                }
            }
            catch
            {
            }

            if (prey.jobs == null)
            {
                return false;
            }

            try
            {
                Job pullJob = JobMaker.MakeJob(JobDefOf.Goto, pullCell);
                pullJob.expiryInterval = 20;
                pullJob.checkOverrideOnExpire = true;
                pullJob.locomotionUrgency = LocomotionUrgency.Walk;
                prey.jobs.TryTakeOrderedJob(pullJob, JobTag.Misc);
                return true;
            }
            catch
            {
            }

            return false;
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
