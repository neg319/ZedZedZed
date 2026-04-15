using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public class JobDriver_RecruitLurker : JobDriver
    {
        private const TargetIndex LurkerInd = TargetIndex.A;
        private const TargetIndex FoodInd = TargetIndex.B;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            bool reservedLurker = pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
            if (!reservedLurker)
            {
                return false;
            }

            if (job.targetB.IsValid && job.targetB.HasThing)
            {
                pawn.Reserve(job.targetB.Thing, job, 1, -1, null, errorOnFailed);
            }

            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(LurkerInd);
            this.FailOn(() => !ZombieLurkerUtility.IsPassiveLurker(TargetThingA as Pawn));
            this.FailOn(() => ZombieLurkerUtility.FindCarriedTameFood(pawn) == null && ZombieLurkerUtility.FindAvailableTameFood(pawn, pawn.Map) == null);

            Toil findFood = Toils_General.Do(delegate
            {
                if (ZombieLurkerUtility.FindCarriedTameFood(pawn) != null)
                {
                    return;
                }

                Thing food = job.targetB.IsValid && job.targetB.HasThing ? job.targetB.Thing : ZombieLurkerUtility.FindAvailableTameFood(pawn, pawn.Map);
                if (food != null)
                {
                    job.targetB = food;
                }
            });
            findFood.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return findFood;

            if (ZombieLurkerUtility.FindCarriedTameFood(pawn) == null)
            {
                yield return Toils_Goto.GotoThing(FoodInd, PathEndMode.Touch);
                yield return Toils_Haul.StartCarryThing(FoodInd, putRemainderInQueue: false);
            }

            yield return Toils_Goto.GotoThing(LurkerInd, PathEndMode.Touch);

            Toil wait = Toils_General.Wait(360);
            wait.WithProgressBarToilDelay(LurkerInd);
            wait.FailOnCannotTouch(LurkerInd, PathEndMode.Touch);
            yield return wait;

            Toil recruit = Toils_General.Do(delegate
            {
                Pawn lurker = TargetThingA as Pawn;
                Thing food = ZombieLurkerUtility.FindCarriedTameFood(pawn);
                if (lurker == null || food == null)
                {
                    return;
                }

                float chance = ZombieLurkerUtility.GetRecruitChance(pawn, food, lurker);
                if (!ZombieLurkerUtility.ConsumeOneUnit(pawn, food))
                {
                    return;
                }

                if (Rand.Chance(chance))
                {
                    ZombieLurkerUtility.JoinColony(lurker, pawn);
                }
                else
                {
                    Messages.Message(ZZZLocalize.T("ZZZ.LurkerRecruitFail"), lurker, MessageTypeDefOf.RejectInput, false);
                    ZombieLurkerUtility.EnsurePassiveLurkerBehavior(lurker);
                }
            });
            recruit.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return recruit;
        }
    }
}
