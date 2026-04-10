using System;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieRuntUtility
    {
        private const long TicksPerYear = 3600000L;

        public static void ApplyRuntAgeProfile(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            int displayedMonths = Rand.RangeInclusive(7, 9);
            SetVisualChildAge(pawn, displayedMonths);
            RemoveExistingAgeMarkers(pawn);

            HediffDef marker = GetAgeMarker(displayedMonths);
            if (marker != null && !pawn.health.hediffSet.HasHediff(marker))
            {
                try
                {
                    pawn.health.AddHediff(marker);
                }
                catch
                {
                }
            }
        }


        public static void NormalizeRuntCorpseForButchering(Pawn pawn)
        {
            if (pawn?.ageTracker == null || !ZombieUtility.IsVariant(pawn, ZombieVariant.Runt))
            {
                return;
            }

            RemoveExistingAgeMarkers(pawn);
            SetAdultCorpseAge(pawn);
        }

        public static int GetDisplayedMonths(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return 0;
            }

            if (ZombieDefOf.CZH_RuntAgeNineMonths != null && pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_RuntAgeNineMonths))
            {
                return 9;
            }

            if (ZombieDefOf.CZH_RuntAgeEightMonths != null && pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_RuntAgeEightMonths))
            {
                return 8;
            }

            if (ZombieDefOf.CZH_RuntAgeSevenMonths != null && pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_RuntAgeSevenMonths))
            {
                return 7;
            }

            return 0;
        }

        public static string GetDisplayedAgeLabel(Pawn pawn)
        {
            int months = GetDisplayedMonths(pawn);
            return months > 0 ? months + " Months" : null;
        }


        private static void SetAdultCorpseAge(Pawn pawn)
        {
            if (pawn?.ageTracker == null)
            {
                return;
            }

            const int adultYears = 20;
            long biologicalTicks = adultYears * TicksPerYear;
            long chronologicalTicks = biologicalTicks;
            long currentTicks = Find.TickManager?.TicksGame ?? 0;
            long birthAbsTicks = currentTicks - chronologicalTicks;

            SetAgeTrackerValue(pawn.ageTracker, "AgeBiologicalTicks", biologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "AgeChronologicalTicks", chronologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "ageBiologicalTicksInt", biologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "ageChronologicalTicksInt", chronologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "BirthAbsTicks", birthAbsTicks);
            SetAgeTrackerValue(pawn.ageTracker, "birthAbsTicksInt", birthAbsTicks);

            try
            {
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "RecalculateLifeStageIndex")?.Invoke(pawn.ageTracker, null);
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "PostResolveLifeStageChange")?.Invoke(pawn.ageTracker, null);
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "CalculateInitialGrowth")?.Invoke(pawn.ageTracker, null);
            }
            catch
            {
            }
        }

        private static void SetVisualChildAge(Pawn pawn, int displayedMonths)
        {
            if (pawn?.ageTracker == null)
            {
                return;
            }

            int visualYears = Math.Max(7, Math.Min(9, displayedMonths));
            long biologicalTicks = visualYears * TicksPerYear;
            long chronologicalTicks = biologicalTicks;
            long currentTicks = Find.TickManager?.TicksGame ?? 0;
            long birthAbsTicks = currentTicks - chronologicalTicks;

            SetAgeTrackerValue(pawn.ageTracker, "AgeBiologicalTicks", biologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "AgeChronologicalTicks", chronologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "ageBiologicalTicksInt", biologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "ageChronologicalTicksInt", chronologicalTicks);
            SetAgeTrackerValue(pawn.ageTracker, "BirthAbsTicks", birthAbsTicks);
            SetAgeTrackerValue(pawn.ageTracker, "birthAbsTicksInt", birthAbsTicks);

            try
            {
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "RecalculateLifeStageIndex")?.Invoke(pawn.ageTracker, null);
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "PostResolveLifeStageChange")?.Invoke(pawn.ageTracker, null);
                HarmonyLib.AccessTools.Method(pawn.ageTracker.GetType(), "CalculateInitialGrowth")?.Invoke(pawn.ageTracker, null);
            }
            catch
            {
            }
        }

        private static void RemoveExistingAgeMarkers(Pawn pawn)
        {
            foreach (HediffDef marker in new[]
            {
                ZombieDefOf.CZH_RuntAgeSevenMonths,
                ZombieDefOf.CZH_RuntAgeEightMonths,
                ZombieDefOf.CZH_RuntAgeNineMonths
            }.Where(def => def != null))
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(marker);
                if (hediff != null)
                {
                    try
                    {
                        pawn.health.RemoveHediff(hediff);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static HediffDef GetAgeMarker(int displayedMonths)
        {
            switch (displayedMonths)
            {
                case 7:
                    return ZombieDefOf.CZH_RuntAgeSevenMonths;
                case 8:
                    return ZombieDefOf.CZH_RuntAgeEightMonths;
                case 9:
                    return ZombieDefOf.CZH_RuntAgeNineMonths;
                default:
                    return ZombieDefOf.CZH_RuntAgeEightMonths;
            }
        }

        private static void SetAgeTrackerValue(object tracker, string memberName, long value)
        {
            if (tracker == null || string.IsNullOrEmpty(memberName))
            {
                return;
            }

            try
            {
                var property = HarmonyLib.AccessTools.Property(tracker.GetType(), memberName);
                if (property != null && property.CanWrite)
                {
                    if (property.PropertyType == typeof(long))
                    {
                        property.SetValue(tracker, value, null);
                        return;
                    }

                    if (property.PropertyType == typeof(int))
                    {
                        property.SetValue(tracker, (int)value, null);
                        return;
                    }
                }
            }
            catch
            {
            }

            try
            {
                var field = HarmonyLib.AccessTools.Field(tracker.GetType(), memberName);
                if (field == null)
                {
                    return;
                }

                if (field.FieldType == typeof(long))
                {
                    field.SetValue(tracker, value);
                }
                else if (field.FieldType == typeof(int))
                {
                    field.SetValue(tracker, (int)value);
                }
            }
            catch
            {
            }
        }
    }
}
