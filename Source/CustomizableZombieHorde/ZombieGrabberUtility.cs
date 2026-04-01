
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieGrabberUtility
    {
        private sealed class PullState
        {
            public int targetId;
            public int expireTick;
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
            if (Rand.Value > 0.18f)
            {
                return;
            }

            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(grabber, 12f);
            if (prey == null || prey.Downed || IsTargetAlreadyGrabbed(prey))
            {
                return;
            }

            if (prey.PositionHeld.DistanceToSquared(grabber.PositionHeld) < 9f)
            {
                return;
            }

            ActivePulls[grabber.thingIDNumber] = new PullState
            {
                targetId = prey.thingIDNumber,
                expireTick = ticksGame + Rand.RangeInclusive(240, 420)
            };
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
            if (distanceSquared <= 2.1f * 2.1f)
            {
                ActivePulls.Remove(grabber.thingIDNumber);
                TryQueueAttack(grabber, prey);
                return;
            }

            IntVec3 pullCell = FindPullCellTowardGrabber(grabber, prey);
            if (pullCell.IsValid && pullCell != prey.PositionHeld && ticksGame % 20 == 0)
            {
                TryPullTarget(prey, pullCell);
            }

            if (ticksGame % 45 == 0)
            {
                TryQueueApproach(grabber, prey);
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

            if (grabber.PositionHeld.DistanceToSquared(prey.PositionHeld) > 14f * 14f)
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
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8WayAndInside)
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

        private static void TryPullTarget(Pawn prey, IntVec3 pullCell)
        {
            if (prey.jobs == null || prey.Downed)
            {
                return;
            }

            try
            {
                Job pullJob = JobMaker.MakeJob(JobDefOf.Goto, pullCell);
                pullJob.expiryInterval = 40;
                pullJob.checkOverrideOnExpire = true;
                pullJob.locomotionUrgency = LocomotionUrgency.Walk;
                prey.jobs.TryTakeOrderedJob(pullJob, JobTag.Misc);
            }
            catch
            {
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
                approachJob.expiryInterval = 75;
                approachJob.checkOverrideOnExpire = true;
                approachJob.locomotionUrgency = ZombieUtility.GetZombieUrgency(grabber);
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
    }
}
