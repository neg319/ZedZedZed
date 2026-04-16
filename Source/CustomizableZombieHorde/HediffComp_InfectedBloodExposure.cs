using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class HediffCompProperties_InfectedBloodExposure : HediffCompProperties
    {
        public int durationTicks = GenDate.TicksPerDay;
        public int infectionCheckIntervalTicks = 3600;
        public float infectionChancePerCheck = 0.05f;

        public HediffCompProperties_InfectedBloodExposure()
        {
            compClass = typeof(HediffComp_InfectedBloodExposure);
        }
    }

    public sealed class HediffComp_InfectedBloodExposure : HediffComp
    {
        private int ticksRemaining = -1;
        private int ticksUntilInfectionCheck = -1;

        public HediffCompProperties_InfectedBloodExposure Props => (HediffCompProperties_InfectedBloodExposure)props;

        public override void CompPostMake()
        {
            base.CompPostMake();
            InitializeTimersIfNeeded();
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksRemaining, "ticksRemaining", -1);
            Scribe_Values.Look(ref ticksUntilInfectionCheck, "ticksUntilInfectionCheck", -1);
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null || parent == null)
            {
                return;
            }

            InitializeTimersIfNeeded();

            if (ShouldRinseOff(Pawn))
            {
                RemoveParent();
                return;
            }

            ticksRemaining--;
            if (ticksRemaining <= 0)
            {
                RemoveParent();
                return;
            }

            if (!ZombieTraitUtility.CanCatchZombieSickness(Pawn))
            {
                RemoveParent();
                return;
            }

            ticksUntilInfectionCheck--;
            if (ticksUntilInfectionCheck > 0)
            {
                return;
            }

            ticksUntilInfectionCheck = GetCheckInterval();

            List<Hediff_Injury> cutInjuries = GetCurrentCutInjuries(Pawn);
            if (cutInjuries.Count == 0)
            {
                return;
            }

            if (!Rand.Chance(Props.infectionChancePerCheck))
            {
                return;
            }

            List<BodyPartRecord> validParts = cutInjuries
                .Where(injury => injury?.Part != null)
                .Select(injury => injury.Part)
                .Distinct()
                .ToList();
            BodyPartRecord infectionPart = validParts.Count > 0 ? validParts.RandomElement() : null;

            if (ZombieTraitUtility.ApplyZombieSicknessDirect(Pawn, infectionPart))
            {
                RemoveParent();
            }
        }

        public void RefreshDuration(int durationTicks)
        {
            int duration = durationTicks > 0 ? durationTicks : GenDate.TicksPerDay;
            if (duration > ticksRemaining)
            {
                ticksRemaining = duration;
            }

            if (ticksUntilInfectionCheck < 0)
            {
                ticksUntilInfectionCheck = GetCheckInterval();
            }
        }

        private void InitializeTimersIfNeeded()
        {
            if (ticksRemaining < 0)
            {
                ticksRemaining = Props?.durationTicks > 0 ? Props.durationTicks : GenDate.TicksPerDay;
            }

            if (ticksUntilInfectionCheck < 0)
            {
                ticksUntilInfectionCheck = GetCheckInterval();
            }
        }

        private int GetCheckInterval()
        {
            return Props?.infectionCheckIntervalTicks > 0 ? Props.infectionCheckIntervalTicks : 3600;
        }

        private static List<Hediff_Injury> GetCurrentCutInjuries(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return new List<Hediff_Injury>();
            }

            return pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury != null
                    && !injury.IsPermanent()
                    && injury.def == HediffDefOf.Cut
                    && injury.Severity > 0.01f)
                .ToList();
        }

        private static bool ShouldRinseOff(Pawn pawn)
        {
            if (pawn?.MapHeld == null || !pawn.Spawned)
            {
                return false;
            }

            if (ZombieUtility.IsWaterCell(pawn.PositionHeld, pawn.MapHeld))
            {
                return true;
            }

            return ZombieSpecialUtility.IsRainActive(pawn.MapHeld) && !pawn.PositionHeld.Roofed(pawn.MapHeld);
        }

        private void RemoveParent()
        {
            try
            {
                Pawn.health?.RemoveHediff(parent);
            }
            catch
            {
            }
        }
    }
}
