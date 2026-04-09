using Verse;

namespace CustomizableZombieHorde
{
    public sealed class HediffCompProperties_TemporaryStatus : HediffCompProperties
    {
        public int durationTicks = 1200;

        public HediffCompProperties_TemporaryStatus()
        {
            compClass = typeof(HediffComp_TemporaryStatus);
        }
    }

    public sealed class HediffComp_TemporaryStatus : HediffComp
    {
        private int ticksRemaining = -1;

        public HediffCompProperties_TemporaryStatus Props => (HediffCompProperties_TemporaryStatus)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            if (ticksRemaining < 0)
            {
                ticksRemaining = Props?.durationTicks > 0 ? Props.durationTicks : 1200;
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null)
            {
                return;
            }

            if (ticksRemaining < 0)
            {
                ticksRemaining = Props?.durationTicks > 0 ? Props.durationTicks : 1200;
            }

            ticksRemaining--;
            if (ticksRemaining > 0)
            {
                return;
            }

            try
            {
                Pawn.health?.RemoveHediff(parent);
            }
            catch
            {
            }
        }

        public void RefreshDuration(int ticks)
        {
            int duration = ticks > 0 ? ticks : (Props?.durationTicks > 0 ? Props.durationTicks : 1200);
            if (duration > ticksRemaining)
            {
                ticksRemaining = duration;
            }
        }

        public int TicksRemaining => ticksRemaining;
    }
}
