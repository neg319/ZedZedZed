using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public class JobDriver_TameLurker : JobDriver
    {
        private const TargetIndex LurkerInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(LurkerInd);
            this.FailOn(() => !ZombieLurkerUtility.IsPassiveLurker(TargetThingA as Pawn));
            this.FailOn(() => ZombieLurkerUtility.FindCarriedTameFood(pawn) == null);

            yield return Toils_Goto.GotoThing(LurkerInd, PathEndMode.Touch);

            Toil wait = Toils_General.Wait(240);
            wait.WithProgressBarToilDelay(LurkerInd);
            wait.FailOnCannotTouch(LurkerInd, PathEndMode.Touch);
            yield return wait;

            Toil tame = Toils_General.Do(delegate
            {
                Pawn lurker = TargetThingA as Pawn;
                Thing food = ZombieLurkerUtility.FindCarriedTameFood(pawn);
                if (lurker == null || food == null)
                {
                    return;
                }

                float chance = ZombieLurkerUtility.GetTameChance(pawn, food);
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
                    ZombieFeedbackUtility.SendLurkerTameFailedMessage(lurker);
                    ZombieLurkerUtility.EnsurePassiveLurkerBehavior(lurker);
                }
            });
            tame.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return tame;
        }
    }
}
