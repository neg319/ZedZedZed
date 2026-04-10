using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class HediffCompProperties_DamageOverTime : HediffCompProperties
    {
        public int tickInterval = 300;
        public float damageAmount = 1f;
        public DamageDef damageDef;
        public float severityLossPerTick = 0.05f;
        public bool onlyWhenAlive = true;

        public HediffCompProperties_DamageOverTime()
        {
            compClass = typeof(HediffComp_DamageOverTime);
        }
    }

    public sealed class HediffComp_DamageOverTime : HediffComp
    {
        public HediffCompProperties_DamageOverTime Props => (HediffCompProperties_DamageOverTime)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null)
            {
                return;
            }

            if (Props.onlyWhenAlive && Pawn.Dead)
            {
                return;
            }

            int interval = Props.tickInterval <= 0 ? 300 : Props.tickInterval;
            if (!Pawn.IsHashIntervalTick(interval))
            {
                return;
            }

            DamageDef damageDef = Props.damageDef ?? DamageDefOf.Burn;
            float damageAmount = Props.damageAmount <= 0f ? 1f : Props.damageAmount;
            BodyPartRecord hitPart = parent.Part;

            try
            {
                Pawn.TakeDamage(new DamageInfo(damageDef, damageAmount, 0f, -1f, null, hitPart));
            }
            catch
            {
            }

            if (Props.severityLossPerTick > 0f)
            {
                parent.Severity = Mathf.Max(0f, parent.Severity - Props.severityLossPerTick);
            }

            if (parent.Severity <= 0.0001f)
            {
                Pawn.health?.RemoveHediff(parent);
            }
        }
    }
}
