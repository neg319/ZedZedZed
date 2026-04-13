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
        private int startTick = -1;
        private int lastHealTick = -1;

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
            Scribe_Values.Look(ref startTick, "startTick", -1);
            Scribe_Values.Look(ref lastHealTick, "lastHealTick", -1);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null || Pawn.Destroyed)
            {
                return;
            }

            if (wakeTick < 0 || startTick < 0 || wakeTick <= startTick)
            {
                RefreshWakeDelay();
            }

            if (Pawn.Dead)
            {
                return;
            }

            parent.Severity = GetProgress();
        }

        public void RefreshWakeDelay()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int minTicks = Mathf.Max(2400, Props?.minWakeTicks ?? 15000);
            int maxTicks = Mathf.Max(minTicks, Props?.maxWakeTicks ?? 30000);
            startTick = currentTick;
            lastHealTick = currentTick;
            wakeTick = currentTick + Rand.RangeInclusive(minTicks, maxTicks);
            if (parent != null)
            {
                parent.Severity = 0f;
            }
        }

        public float GetProgress()
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            if (wakeTick < 0 || startTick < 0 || wakeTick <= startTick)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)(currentTick - startTick) / (wakeTick - startTick));
        }

        public int WakeTick => wakeTick;
        public int StartTick => startTick;
        public bool ShouldHealNow(int currentTick)
        {
            int interval = Mathf.Max(60, Props?.healIntervalTicks ?? 180);
            if (lastHealTick < 0)
            {
                lastHealTick = startTick >= 0 ? startTick : currentTick;
            }

            if (currentTick - lastHealTick < interval)
            {
                return false;
            }

            lastHealTick = currentTick;
            return true;
        }

        public void DelayWake(int ticks)
        {
            int currentTick = Find.TickManager?.TicksGame ?? 0;
            int delay = Mathf.Max(180, ticks);
            startTick = currentTick;
            lastHealTick = currentTick;
            wakeTick = currentTick + delay;
            if (parent != null)
            {
                parent.Severity = 0f;
            }
        }

    }
}
