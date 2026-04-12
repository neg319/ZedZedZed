using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public class JobDriver_DoubleTapZombieCorpse : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(CorpseInd);
            this.FailOn(() => !ZombieDoubleTapUtility.CanDoubleTapThing(TargetA.Thing));

            yield return Toils_Goto.GotoThing(CorpseInd, PathEndMode.Touch);

            Toil wait = Toils_General.Wait(150);
            wait.WithProgressBarToilDelay(CorpseInd);
            wait.FailOnCannotTouch(CorpseInd, PathEndMode.Touch);
            wait.FailOn(() => !ZombieDoubleTapUtility.CanDoubleTapThing(TargetA.Thing));
            yield return wait;

            Toil finish = Toils_General.Do(delegate
            {
                ZombieDoubleTapUtility.PerformDoubleTap(pawn, TargetA.Thing);
            });
            finish.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return finish;
        }
    }
}
