using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieTraitUtility
    {
        public static bool HasTrait(Pawn pawn, TraitDef traitDef)
        {
            return pawn?.story?.traits?.HasTrait(traitDef) == true;
        }

        public static bool HasSickImmunity(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_ZombieSickImmune);
        }

        public static bool HasHeadHunter(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_HeadHunter);
        }

        public static bool IsIgnoredByZombies(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_DeadScent);
        }

        public static bool CanCatchZombieSickness(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.health != null
                && !ZombieUtility.IsZombie(pawn)
                && !HasSickImmunity(pawn)
                && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSickness);
        }

        public static Pawn ResolveDamageInstigatorPawn(Thing instigator)
        {
            if (instigator is Pawn pawn)
            {
                return pawn;
            }

            if (instigator is Projectile projectile)
            {
                return projectile.Launcher as Pawn;
            }

            return null;
        }
    }
}
