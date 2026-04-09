using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class ZombieVerb_GrabberTongue : Verb_Shoot
    {
        protected override bool TryCastShot()
        {
            Pawn grabber = CasterPawn;
            Pawn prey = currentTarget.Thing as Pawn;
            if (grabber == null || prey == null)
            {
                return false;
            }

            if (!ZombieUtility.IsVariant(grabber, ZombieVariant.Grabber) || ZombieGrabberUtility.HasActiveTongue(grabber))
            {
                return false;
            }

            bool fired = base.TryCastShot();
            if (!fired)
            {
                return false;
            }

            ZombieGrabberUtility.TryBeginPull(grabber, prey);
            return true;
        }
    }
}
