using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public class ZombieVerb_MeleeAttack : Verb_MeleeAttack
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            Thing thing = target.Thing;
            if (thing == null)
            {
                return new DamageWorker.DamageResult();
            }

            Pawn casterPawn = CasterPawn;
            if (casterPawn == null)
            {
                return new DamageWorker.DamageResult();
            }

            if (thing is Pawn victimPawn
                && ZombieUtility.IsZombie(casterPawn)
                && !ZombieUtility.IsZombie(victimPawn)
                && Rand.Value < ZombieUtility.GetZombieMeleeMissChance(casterPawn, victimPawn))
            {
                return new DamageWorker.DamageResult();
            }

            float armorPenetration = verbProps.AdjustedArmorPenetration(this, casterPawn);
            float angle = (thing.Position - casterPawn.Position).AngleFlat;

            DamageInfo dinfo = new DamageInfo(
                verbProps.meleeDamageDef,
                verbProps.AdjustedMeleeDamageAmount(this, casterPawn),
                armorPenetration,
                angle,
                casterPawn,
                null,
                null,
                DamageInfo.SourceCategory.ThingOrUnknown,
                target.Thing);

            return thing.TakeDamage(dinfo);
        }
    }
}
