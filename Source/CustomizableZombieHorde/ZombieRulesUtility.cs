using System;
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

        public static bool IsZombieAlignedCritter(Pawn pawn)
        {
            return false;
        }

        public static bool IsIgnoredByZombies(Pawn pawn)
        {
            return pawn != null && (IsZombie(pawn) || IsPassiveUndead(pawn) || IsZombieAlignedCritter(pawn) || ZombieTraitUtility.IsIgnoredByZombies(pawn));
        }

        public static bool CanReanimate(Pawn pawn)
        {
            return false;
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
            if (head == null || pawn.health.hediffSet.PartIsMissing(head))
            {
                return true;
            }

            if (pawn.health.hediffSet.hediffs.OfType<Hediff_MissingPart>().Any(hediff => IsHeadOrSkullPart(hediff.Part)))
            {
                return true;
            }

            var headRegionInjuries = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury?.Part != null && ZombieInfectionUtility.IsHeadOrChildPart(injury.Part, pawn))
                .ToList();

            if (headRegionInjuries.Count == 0)
            {
                return false;
            }

            if (headRegionInjuries.Any(injury => string.Equals(injury.Part?.def?.defName, "Brain", StringComparison.OrdinalIgnoreCase) && injury.Severity > 0.01f))
            {
                return true;
            }

            float directHeadTrauma = headRegionInjuries
                .Where(injury => injury.Part == head)
                .Sum(injury => injury.Severity);
            if (directHeadTrauma >= 8f)
            {
                return true;
            }

            float totalHeadRegionTrauma = headRegionInjuries.Sum(injury => injury.Severity);
            return totalHeadRegionTrauma >= 12f;
        }

        private static bool IsHeadOrSkullPart(BodyPartRecord part)
        {
            if (part?.def == BodyPartDefOf.Head)
            {
                return true;
            }

            string defName = part?.def?.defName ?? string.Empty;
            return string.Equals(defName, "Skull", StringComparison.OrdinalIgnoreCase);
        }
    }
}
