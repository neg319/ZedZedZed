using RimWorld;
using Verse;
using Verse.AI;

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

        public static bool HasZombiePassive(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_ZombiePassive);
        }

        public static bool IsIgnoredByZombies(Pawn pawn)
        {
            return HasZombiePassive(pawn) || HasTrait(pawn, ZombieDefOf.CZH_Trait_DeadScent);
        }

        public static void EnsureTrait(Pawn pawn, TraitDef traitDef)
        {
            if (pawn?.story?.traits == null || traitDef == null || pawn.story.traits.HasTrait(traitDef))
            {
                return;
            }

            try
            {
                pawn.story.traits.GainTrait(new Trait(traitDef));
            }
            catch
            {
            }
        }

        public static bool HasHardToKill(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_HardToKill);
        }

        public static bool HasSteadyHands(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_SteadyHands);
        }

        public static bool HasQuickEscape(Pawn pawn)
        {
            return HasTrait(pawn, ZombieDefOf.CZH_Trait_QuickEscape);
        }

        public static bool CanCatchZombieSickness(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.health != null
                && pawn.RaceProps?.Humanlike == true
                && !ZombieUtility.IsZombie(pawn)
                && !HasSickImmunity(pawn)
                && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSickness);
        }



        public static bool TryApplyZombieSickness(Pawn pawn, float chance)
        {
            if (HasHardToKill(pawn))
            {
                chance *= 0.35f;
            }

            if (!CanCatchZombieSickness(pawn) || Rand.Value > chance)
            {
                return false;
            }

            try
            {
                Hediff hediff = HediffMaker.MakeHediff(ZombieDefOf.CZH_ZombieSickness, pawn);
                hediff.Severity = Mathf.Max(hediff.Severity, 0.20f);
                pawn.health.AddHediff(hediff);
                ZombieFeedbackUtility.SendZombieSicknessMessage(pawn);
                return true;
            }
            catch
            {
                return false;
            }
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

        public static bool IsRangedAttack(Pawn attacker, DamageInfo dinfo)
        {
            if (dinfo.Instigator is Projectile)
            {
                return true;
            }

            if (attacker?.equipment?.Primary?.def?.IsRangedWeapon == true)
            {
                return true;
            }

            return attacker?.CurJob?.verbToUse?.verbProps?.range > 1.45f;
        }

        public static bool TryTriggerQuickEscape(Pawn victim, Pawn attacker)
        {
            if (victim == null || attacker == null || victim.Dead || victim.Downed || victim.MapHeld == null || victim.jobs == null)
            {
                return false;
            }

            Map map = victim.MapHeld;
            IntVec3 bestCell = IntVec3.Invalid;
            float bestScore = float.MinValue;

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(victim.PositionHeld, 3.9f, true))
            {
                if (!cell.IsValid || cell == victim.PositionHeld || !cell.InBounds(map) || !cell.Standable(map))
                {
                    continue;
                }

                if (cell.GetEdifice(map) != null)
                {
                    continue;
                }

                float currentDistance = victim.PositionHeld.DistanceToSquared(attacker.PositionHeld);
                float candidateDistance = cell.DistanceToSquared(attacker.PositionHeld);
                if (candidateDistance <= currentDistance + 0.1f)
                {
                    continue;
                }

                float score = candidateDistance - victim.PositionHeld.DistanceToSquared(cell) * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCell = cell;
                }
            }

            if (!bestCell.IsValid)
            {
                return false;
            }

            try
            {
                victim.Position = bestCell;
                return true;
            }
            catch
            {
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, bestCell);
                moveJob.expiryInterval = 90;
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = LocomotionUrgency.Sprint;
                victim.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
                return true;
            }
            catch
            {
            }

            return false;
        }

    }
}
