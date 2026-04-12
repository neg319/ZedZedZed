using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class CorpseEligibilityUtility
    {
        public static bool CanRise(Corpse corpse, ZombieRaiseMode mode)
        {
            Pawn pawn = corpse?.InnerPawn;
            if (corpse == null || corpse.Destroyed || pawn == null || pawn.Destroyed)
            {
                return false;
            }

            if (!pawn.Dead)
            {
                return true;
            }

            if (pawn.RaceProps?.IsFlesh != true || pawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (ZombieRulesUtility.HasHeadDamageOrDestruction(pawn) || ZombieInfectionUtility.IsSkullMissing(pawn))
            {
                return false;
            }

            switch (mode)
            {
                case ZombieRaiseMode.ZombieCorpse:
                    return ZombieUtility.IsZombie(pawn) && !ZombieUtility.IsVariant(pawn, ZombieVariant.Boomer);
                case ZombieRaiseMode.ColonyLurker:
                    return ZombieInfectionUtility.HasReanimatedState(pawn) && ZombieInfectionUtility.GetZombieInfection(pawn) != null;
                case ZombieRaiseMode.InfectedZombie:
                    return ZombieInfectionUtility.HasReanimatedState(pawn) && ZombieInfectionUtility.GetZombieInfection(pawn) != null;
                default:
                    return false;
            }
        }
    }
}
