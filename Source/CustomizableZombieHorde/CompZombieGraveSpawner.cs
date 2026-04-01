using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class CompProperties_ZombieGraveSpawner : CompProperties
    {
        public string variant = "Biter";
        public int initialBurstCount = 4;
        public int minBurstCount = 1;
        public int maxBurstCount = 2;
        public int minRespawnTicks = 1800;
        public int maxRespawnTicks = 3600;

        public CompProperties_ZombieGraveSpawner()
        {
            compClass = typeof(CompZombieGraveSpawner);
        }
    }

    public sealed class CompZombieGraveSpawner : ThingComp
    {
        private int nextBurstTick = -1;

        public CompProperties_ZombieGraveSpawner Props => (CompProperties_ZombieGraveSpawner)props;

        private ZombieVariant Variant
        {
            get
            {
                try
                {
                    return (ZombieVariant)System.Enum.Parse(typeof(ZombieVariant), Props.variant);
                }
                catch
                {
                    return ZombieVariant.Biter;
                }
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (nextBurstTick < 0)
            {
                ScheduleNextBurst();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (parent?.Map == null || parent.Destroyed)
            {
                return;
            }

            if (nextBurstTick < 0)
            {
                ScheduleNextBurst();
                return;
            }

            if ((Find.TickManager?.TicksGame ?? 0) < nextBurstTick)
            {
                return;
            }

            TrySpawnBurst();
            ScheduleNextBurst();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref nextBurstTick, "nextBurstTick", -1);
        }

        public void TriggerInitialBurst()
        {
            if (parent?.Map == null || parent.Destroyed)
            {
                return;
            }

            int count = Props.initialBurstCount < 1 ? 1 : Props.initialBurstCount;
            ZombieSpawnHelper.SpawnVariantPackAround(parent.Map, parent.Position, Variant, count, ZombieSpawnEventType.HuddledPack, ignoreCap: true);
            ScheduleNextBurst();
        }

        public override string CompInspectStringExtra()
        {
            int ticksLeft = nextBurstTick - (Find.TickManager?.TicksGame ?? 0);
            if (ticksLeft <= 0)
            {
                return "The grave is stirring.";
            }

            return "Next corpse burst in about " + ticksLeft.ToStringTicksToPeriod().Colorize(ColoredText.SubtleGrayColor);
        }

        private void TrySpawnBurst()
        {
            if (parent?.Map == null || parent.Destroyed)
            {
                return;
            }

            if (!ZombieKindSelector.IsVariantEnabled(Variant, parent.Map))
            {
                return;
            }

            int count = Rand.RangeInclusive(Props.minBurstCount, Props.maxBurstCount < Props.minBurstCount ? Props.minBurstCount : Props.maxBurstCount);
            ZombieSpawnHelper.SpawnVariantPackAround(parent.Map, parent.Position, Variant, count, ZombieSpawnEventType.HuddledPack, ignoreCap: true);
        }

        private void ScheduleNextBurst()
        {
            int minTicks = Props.minRespawnTicks < 250 ? 250 : Props.minRespawnTicks;
            int maxTicks = Props.maxRespawnTicks < minTicks ? minTicks : Props.maxRespawnTicks;
            nextBurstTick = (Find.TickManager?.TicksGame ?? 0) + Rand.RangeInclusive(minTicks, maxTicks);
        }
    }
}
