using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public class JobDriver_AdministerBileTreatment : JobDriver
    {
        private const TargetIndex PatientInd = TargetIndex.A;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedNullOrForbidden(PatientInd);
            this.FailOn(() => !ZombieBileUtility.NeedsBileTreatment(TargetThingA as Pawn));
            this.FailOn(() => ZombieBileUtility.FindCarriedBileTreatmentKit(pawn) == null);

            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);

            Toil wait = Toils_General.Wait(180);
            wait.WithProgressBarToilDelay(PatientInd);
            wait.FailOnCannotTouch(PatientInd, PathEndMode.Touch);
            yield return wait;

            Toil treat = Toils_General.Do(delegate
            {
                Pawn patient = TargetThingA as Pawn;
                Thing kit = ZombieBileUtility.FindCarriedBileTreatmentKit(pawn);
                if (patient == null || kit == null)
                {
                    return;
                }

                if (!ZombieBileUtility.ConsumeOneBileTreatmentKit(pawn, kit))
                {
                    return;
                }

                ZombieBileUtility.CureZombieSickness(patient, pawn);
            });
            treat.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return treat;
        }
    }
}
