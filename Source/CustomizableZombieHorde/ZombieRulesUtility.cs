using System.Linq;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieRulesUtility
    {
        public static bool IsZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieRot) == true)
            {
                return true;
            }

            return pawn.kindDef?.defName?.StartsWith("CZH_Zombie_") == true;
        }

        public static bool IsPassiveUndead(Pawn pawn)
        {
            return pawn != null && ZombieLurkerUtility.IsPassiveLurker(pawn);
        }

        public static bool IsIgnoredByZombies(Pawn pawn)
        {
            return pawn != null && (IsZombie(pawn) || IsPassiveUndead(pawn) || ZombieTraitUtility.IsIgnoredByZombies(pawn));
        }

        public static bool CanReanimate(Pawn pawn)
        {
            return IsZombie(pawn) && !HasHeadDamageOrDestruction(pawn);
        }

        public static bool HasHeadDamageOrDestruction(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return true;
            }

            if (!pawn.health.hediffSet.HasHead)
            {
                return true;
            }

            BodyPartRecord head = pawn.RaceProps?.body?.AllParts?.FirstOrDefault(part => part.def == BodyPartDefOf.Head);
            if (head == null)
            {
                return true;
            }

            return pawn.health.hediffSet.hediffs.Any(hediff => IsHeadPartOrChild(hediff.Part, head) && (hediff is Hediff_Injury || hediff is Hediff_MissingPart));
        }

        private static bool IsHeadPartOrChild(BodyPartRecord part, BodyPartRecord head)
        {
            for (BodyPartRecord current = part; current != null; current = current.parent)
            {
                if (current == head)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
