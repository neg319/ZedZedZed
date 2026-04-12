using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class HediffCompProperties_ZombieFeignDeath : HediffCompProperties
    {
        public int minWakeTicks = 15000;
        public int maxWakeTicks = 30000;
        public int healIntervalTicks = 180;
        public float healAmount = 1.35f;

        public HediffCompProperties_ZombieFeignDeath()
        {
            compClass = typeof(HediffComp_ZombieFeignDeath);
        }
    }

    public sealed class HediffComp_ZombieFeignDeath : HediffComp
    {
        private int wakeTick = -1;

        public HediffCompProperties_ZombieFeignDeath Props => (HediffCompProperties_ZombieFeignDeath)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            RefreshWakeDelay();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref wakeTick, "wakeTick", -1);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null || Pawn.Destroyed)
            {
                return;
            }

            if (wakeTick < 0)
            {
                RefreshWakeDelay();
            }

            if (Pawn.Dead)
            {
                return;
            }

            if (!ZombieUtility.IsZombie(Pawn))
            {
                try
                {
                    Pawn.health?.RemoveHediff(parent);
                }
                catch
                {
                }

                return;
            }

            int interval = Mathf.Max(60, Props?.healIntervalTicks ?? 180);
            if (Pawn.IsHashIntervalTick(interval))
            {
                ZombieFeignDeathUtility.RegenerateDuringFeignDeath(Pawn, Mathf.Max(0.25f, Props?.healAmount ?? 1.35f));
            }

            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (currentTick >= wakeTick)
            {
                if (!ZombieFeignDeathUtility.TryRiseFromFeignDeath(Pawn, parent))
                {
                    wakeTick = currentTick + 1800;
                }
            }
        }

        public void RefreshWakeDelay()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int minTicks = Mathf.Max(2400, Props?.minWakeTicks ?? 15000);
            int maxTicks = Mathf.Max(minTicks, Props?.maxWakeTicks ?? 30000);
            wakeTick = currentTick + Rand.RangeInclusive(minTicks, maxTicks);
        }

        public int WakeTick => wakeTick;
    }
}
