using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace CustomizableZombieHorde
{
    public sealed class CustomizableZombieHordeSettings : ModSettings
    {
        private enum SettingsTab
        {
            Overview,
            Events,
            Variants,
            Names,
            Advanced,
            Debug
        }

        private SettingsTab selectedTab = SettingsTab.Overview;

        private Vector2 overviewScrollPosition;
        private Vector2 eventsScrollPosition;
        private Vector2 variantsScrollPosition;
        private Vector2 namesScrollPosition;
        private Vector2 advancedScrollPosition;
        private Vector2 debugScrollPosition;

        private float overviewViewHeight = 1200f;
        private float eventsViewHeight = 2400f;
        private float variantsViewHeight = 1500f;
        private float namesViewHeight = 1750f;
        private float advancedViewHeight = 1650f;
        private float debugViewHeight = 2200f;

        public string zombiePrefix = "Zombie";
        public int minGroupSize = 3;
        public int maxGroupSize = 7;
        public float fastZombieChance = 0.02f;
        public float resurrectionDelayHours = 5f;
        public int infectionDaysToTurn = 7;

        public bool enableEdgeTrickle = true;
        public float trickleIntervalHours = 2.75f;
        public int trickleMinGroupSize = 1;
        public int trickleMaxGroupSize = 2;

        public float outbreakIntensity = 4f;
        public int difficultyLevel = -999;

        public bool enableGroundBursts = true;
        public float groundBurstMinDays = 5f;
        public float groundBurstMaxDays = 10f;
        public int groundBurstMinGroupSize = 2;
        public int groundBurstMaxGroupSize = 4;

        public bool enableHerdEvents = true;
        public float herdEventMinDays = 10f;
        public float herdEventMaxDays = 18f;

        public bool enableGraveEvents = true;
        public float graveEventMinDays = 8f;
        public float graveEventMaxDays = 16f;

        public bool enableMoonEvents = true;
        public float bloodMoonChance = 0.10f;
        public int fullMoonBaseCount = 12;
        public int bloodMoonBaseCount = 24;

        public bool useColonistScaledPopulation = true;
        public int dayColonistMultiplierMin = 2;
        public int dayColonistMultiplierMax = 4;
        public int nightColonistMultiplierMin = 3;
        public int nightColonistMultiplierMax = 6;
        public int fullMoonColonistMultiplierMin = 5;
        public int fullMoonColonistMultiplierMax = 8;
        public int bloodMoonColonistMultiplierMin = 7;
        public int bloodMoonColonistMultiplierMax = 10;

        public bool showZombieCounter = true;
        public bool enableDebugControls = false;
        public bool enablePrioritizedDoubleTap = false;
        public bool autoAllowZombieCorpses = true;
        public List<string> prioritizedDoubleTapWorkTypeDefs = new List<string>();

        public bool allowBiters = true;
        public bool allowRunts = true;
        public bool allowBoomers = true;
        public bool allowSick = true;
        public bool allowDrowned = true;
        public bool allowBrutes = true;
        public bool allowGrabbers = true;

        public string biterName = "Biter";
        public string runtName = "Runt";
        public string boomerName = "Boomer";
        public string sickName = "Sick";
        public string drownedName = "Drowned";
        public string bruteName = "Brute";
        public string grabberName = "Grabber";
        public string lurkerName = "Lurker";
        public string boneBiterName = "Bone Biter";
        public string pregnantBoomerName = "Pregnant Boomer";

        private static string T(string key)
        {
            return ("ZZZ." + key).Translate().ToString();
        }


        public override void ExposeData()
        {
            Scribe_Values.Look(ref zombiePrefix, "zombiePrefix", "Zombie");
            Scribe_Values.Look(ref minGroupSize, "minGroupSize", 3);
            Scribe_Values.Look(ref maxGroupSize, "maxGroupSize", 7);
            Scribe_Values.Look(ref fastZombieChance, "fastZombieChance", 0.02f);
            Scribe_Values.Look(ref resurrectionDelayHours, "resurrectionDelayHours", 5f);
            Scribe_Values.Look(ref infectionDaysToTurn, "infectionDaysToTurn", 7);

            Scribe_Values.Look(ref enableEdgeTrickle, "enableEdgeTrickle", true);
            Scribe_Values.Look(ref trickleIntervalHours, "trickleIntervalHours", 2.75f);
            Scribe_Values.Look(ref trickleMinGroupSize, "trickleMinGroupSize", 1);
            Scribe_Values.Look(ref trickleMaxGroupSize, "trickleMaxGroupSize", 2);

            Scribe_Values.Look(ref outbreakIntensity, "outbreakIntensity", 4f);
            Scribe_Values.Look(ref difficultyLevel, "difficultyLevel", -999);

            Scribe_Values.Look(ref enableGroundBursts, "enableGroundBursts", true);
            Scribe_Values.Look(ref groundBurstMinDays, "groundBurstMinDays", 5f);
            Scribe_Values.Look(ref groundBurstMaxDays, "groundBurstMaxDays", 10f);
            Scribe_Values.Look(ref groundBurstMinGroupSize, "groundBurstMinGroupSize", 2);
            Scribe_Values.Look(ref groundBurstMaxGroupSize, "groundBurstMaxGroupSize", 4);

            Scribe_Values.Look(ref enableHerdEvents, "enableHerdEvents", true);
            Scribe_Values.Look(ref herdEventMinDays, "herdEventMinDays", 10f);
            Scribe_Values.Look(ref herdEventMaxDays, "herdEventMaxDays", 18f);

            Scribe_Values.Look(ref enableGraveEvents, "enableGraveEvents", true);
            Scribe_Values.Look(ref graveEventMinDays, "graveEventMinDays", 8f);
            Scribe_Values.Look(ref graveEventMaxDays, "graveEventMaxDays", 16f);

            Scribe_Values.Look(ref enableMoonEvents, "enableMoonEvents", true);
            Scribe_Values.Look(ref bloodMoonChance, "bloodMoonChance", 0.10f);
            Scribe_Values.Look(ref fullMoonBaseCount, "fullMoonBaseCount", 12);
            Scribe_Values.Look(ref bloodMoonBaseCount, "bloodMoonBaseCount", 24);

            Scribe_Values.Look(ref useColonistScaledPopulation, "useColonistScaledPopulation", true);
            Scribe_Values.Look(ref dayColonistMultiplierMin, "dayColonistMultiplierMin", 2);
            Scribe_Values.Look(ref dayColonistMultiplierMax, "dayColonistMultiplierMax", 4);
            Scribe_Values.Look(ref nightColonistMultiplierMin, "nightColonistMultiplierMin", 3);
            Scribe_Values.Look(ref nightColonistMultiplierMax, "nightColonistMultiplierMax", 6);
            Scribe_Values.Look(ref fullMoonColonistMultiplierMin, "fullMoonColonistMultiplierMin", 5);
            Scribe_Values.Look(ref fullMoonColonistMultiplierMax, "fullMoonColonistMultiplierMax", 8);
            Scribe_Values.Look(ref bloodMoonColonistMultiplierMin, "bloodMoonColonistMultiplierMin", 7);
            Scribe_Values.Look(ref bloodMoonColonistMultiplierMax, "bloodMoonColonistMultiplierMax", 10);

            Scribe_Values.Look(ref showZombieCounter, "showZombieCounter", true);
            Scribe_Values.Look(ref enableDebugControls, "enableDebugControls", false);
            Scribe_Values.Look(ref enablePrioritizedDoubleTap, "enablePrioritizedDoubleTap", false);
            Scribe_Values.Look(ref autoAllowZombieCorpses, "autoAllowZombieCorpses", true);
            Scribe_Collections.Look(ref prioritizedDoubleTapWorkTypeDefs, "prioritizedDoubleTapWorkTypeDefs", LookMode.Value);

            Scribe_Values.Look(ref allowBiters, "allowBiters", true);
            Scribe_Values.Look(ref allowRunts, "allowRunts", true);
            Scribe_Values.Look(ref allowBoomers, "allowBoomers", true);
            Scribe_Values.Look(ref allowSick, "allowSick", true);
            Scribe_Values.Look(ref allowDrowned, "allowDrowned", true);
            Scribe_Values.Look(ref allowBrutes, "allowBrutes", true);
            Scribe_Values.Look(ref allowGrabbers, "allowGrabbers", true);

            Scribe_Values.Look(ref biterName, "biterName", "Biter");
            Scribe_Values.Look(ref runtName, "runtName", "Runt");
            Scribe_Values.Look(ref boomerName, "boomerName", "Boomer");
            Scribe_Values.Look(ref sickName, "sickName", "Sick");
            Scribe_Values.Look(ref drownedName, "drownedName", "Drowned");
            Scribe_Values.Look(ref bruteName, "bruteName", "Brute");
            Scribe_Values.Look(ref grabberName, "grabberName", "Grabber");
            Scribe_Values.Look(ref lurkerName, "lurkerName", "Lurker");
            Scribe_Values.Look(ref boneBiterName, "boneBiterName", "Bone Biter");
            Scribe_Values.Look(ref pregnantBoomerName, "pregnantBoomerName", "Pregnant Boomer");

            base.ExposeData();
            ClampAndRepair();
        }

        public float DaytimeTargetMultiplier => Mathf.Max(0.5f, outbreakIntensity);

        public float NighttimeTargetMultiplier => DaytimeTargetMultiplier * 1.5f;

        public float OutbreakIntensityScale => Mathf.Max(0.25f, outbreakIntensity / 4f);

        public float InverseOutbreakIntensityScale => 4f / Mathf.Max(0.5f, outbreakIntensity);

        public int ScaleSpawnCountByOutbreak(int baseCount, int minimum = 1)
        {
            return Mathf.Max(minimum, Mathf.RoundToInt(baseCount * OutbreakIntensityScale));
        }

        public float ScaleHoursByOutbreak(float baseHours, float minimumHours = 0.25f, float maximumHours = 24f)
        {
            return Mathf.Clamp(baseHours * InverseOutbreakIntensityScale, minimumHours, maximumHours);
        }

        public float ScaleDaysByOutbreak(float baseDays, float minimumDays = 1f, float maximumDays = 30f)
        {
            return Mathf.Clamp(baseDays * InverseOutbreakIntensityScale, minimumDays, maximumDays);
        }

        public int GetEffectiveTrickleMinGroupSize()
        {
            return ScaleSpawnCountByOutbreak(trickleMinGroupSize, 1);
        }

        public int GetEffectiveTrickleMaxGroupSize()
        {
            return Mathf.Max(GetEffectiveTrickleMinGroupSize(), ScaleSpawnCountByOutbreak(trickleMaxGroupSize, GetEffectiveTrickleMinGroupSize()));
        }

        public float GetEffectiveTrickleIntervalHours()
        {
            return ScaleHoursByOutbreak(trickleIntervalHours, 0.25f, 24f);
        }

        public float GetEffectiveGroundBurstMinDays()
        {
            return ScaleDaysByOutbreak(groundBurstMinDays, 1f, 20f);
        }

        public float GetEffectiveGroundBurstMaxDays()
        {
            return Mathf.Max(GetEffectiveGroundBurstMinDays(), ScaleDaysByOutbreak(groundBurstMaxDays, GetEffectiveGroundBurstMinDays(), 30f));
        }

        public float GetEffectiveHerdEventMinDays()
        {
            return ScaleDaysByOutbreak(herdEventMinDays, 4f, 30f);
        }

        public float GetEffectiveHerdEventMaxDays()
        {
            return Mathf.Max(GetEffectiveHerdEventMinDays(), ScaleDaysByOutbreak(herdEventMaxDays, GetEffectiveHerdEventMinDays(), 40f));
        }

        public int GetEffectiveGroundBurstMinGroupSize()
        {
            return ScaleSpawnCountByOutbreak(groundBurstMinGroupSize, 1);
        }

        public int GetEffectiveGroundBurstMaxGroupSize()
        {
            return Mathf.Max(GetEffectiveGroundBurstMinGroupSize(), ScaleSpawnCountByOutbreak(groundBurstMaxGroupSize, GetEffectiveGroundBurstMinGroupSize()));
        }

        public float GetEffectiveGraveEventMinDays()
        {
            return ScaleDaysByOutbreak(graveEventMinDays, 3f, 30f);
        }

        public float GetEffectiveGraveEventMaxDays()
        {
            return Mathf.Max(GetEffectiveGraveEventMinDays(), ScaleDaysByOutbreak(graveEventMaxDays, GetEffectiveGraveEventMinDays(), 40f));
        }

        public float GetPopulationTopUpIntervalHours(float deficitFraction)
        {
            float urgency = Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(deficitFraction));
            return Mathf.Clamp(GetEffectiveTrickleIntervalHours() * urgency, 0.35f, 6f);
        }

        public int GetPopulationTopUpCount(int deficit)
        {
            if (deficit <= 0)
            {
                return 0;
            }

            float fillFraction = Mathf.Lerp(0.45f, 0.70f, Mathf.Clamp01((outbreakIntensity - 0.5f) / 11.5f));
            int scaled = Mathf.Max(1, Mathf.CeilToInt(deficit * fillFraction));
            int maxChunk = Mathf.Max(4, ScaleSpawnCountByOutbreak(10, 4));
            return Mathf.Clamp(scaled, 1, Mathf.Max(1, maxChunk));
        }

        public void DoWindowContents(Rect inRect)
        {
            try
            {
                ClampAndRepair();

                Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 90f);
                Rect tabsRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, 34f);
                Rect bodyRect = new Rect(inRect.x, tabsRect.yMax + 8f, inRect.width, inRect.height - (tabsRect.yMax - inRect.y) - 8f);

                DrawHeader(headerRect);
                DrawTabButtons(tabsRect);

                SettingsTheme.DrawBody(bodyRect);
                Rect innerRect = bodyRect.ContractedBy(12f);

                switch (selectedTab)
                {
                    case SettingsTab.Overview:
                        DrawOverviewTab(innerRect);
                        break;
                    case SettingsTab.Events:
                        DrawEventsTab(innerRect);
                        break;
                    case SettingsTab.Variants:
                        DrawVariantsTab(innerRect);
                        break;
                    case SettingsTab.Names:
                        DrawNamesTab(innerRect);
                        break;
                    case SettingsTab.Advanced:
                        DrawAdvancedTab(innerRect);
                        break;
                    case SettingsTab.Debug:
                        DrawDebugTab(innerRect);
                        break;
                }

                ClampAndRepair();
            }
            catch (System.Exception ex)
            {
                Log.Error("[Zed Zed Zed] Settings UI fell back to simple mode. " + ex);
                DrawFallbackWindow(inRect);
            }
        }

        public void ResetToRecommendedDefaults()
        {
            zombiePrefix = "Zombie";
            minGroupSize = 3;
            maxGroupSize = 7;
            fastZombieChance = 0.02f;
            resurrectionDelayHours = 5f;
            infectionDaysToTurn = 7;

            enableEdgeTrickle = true;
            trickleIntervalHours = 2.75f;
            trickleMinGroupSize = 1;
            trickleMaxGroupSize = 2;

            outbreakIntensity = 4f;
            difficultyLevel = -999;

            enableGroundBursts = true;
            groundBurstMinDays = 5f;
            groundBurstMaxDays = 10f;
            groundBurstMinGroupSize = 2;
            groundBurstMaxGroupSize = 4;

            enableHerdEvents = true;
            herdEventMinDays = 10f;
            herdEventMaxDays = 18f;

            enableGraveEvents = true;
            graveEventMinDays = 8f;
            graveEventMaxDays = 16f;

            enableMoonEvents = true;
            bloodMoonChance = 0.10f;
            fullMoonBaseCount = 12;
            bloodMoonBaseCount = 24;

            useColonistScaledPopulation = true;
            dayColonistMultiplierMin = 2;
            dayColonistMultiplierMax = 4;
            nightColonistMultiplierMin = 3;
            nightColonistMultiplierMax = 6;
            fullMoonColonistMultiplierMin = 5;
            fullMoonColonistMultiplierMax = 8;
            bloodMoonColonistMultiplierMin = 7;
            bloodMoonColonistMultiplierMax = 10;

            showZombieCounter = true;
            enableDebugControls = false;
            enablePrioritizedDoubleTap = false;
            autoAllowZombieCorpses = true;
            prioritizedDoubleTapWorkTypeDefs = GetDefaultDoubleTapWorkTypes();

            allowBiters = true;
            allowRunts = true;
            allowBoomers = true;
            allowSick = true;
            allowDrowned = true;
            allowBrutes = true;
            allowGrabbers = true;

            biterName = "Biter";
            runtName = "Runt";
            boomerName = "Boomer";
            sickName = "Sick";
            drownedName = "Drowned";
            bruteName = "Brute";
            grabberName = "Grabber";
            lurkerName = "Lurker";
            boneBiterName = "Bone Biter";
            pregnantBoomerName = "Pregnant Boomer";
        }

        private void ApplyCasualPreset()
        {
            ResetToRecommendedDefaults();
            outbreakIntensity = 2.0f;
            difficultyLevel = -999;
            trickleIntervalHours = 4f;
            trickleMinGroupSize = 1;
            trickleMaxGroupSize = 1;
            fullMoonBaseCount = 8;
            bloodMoonBaseCount = 16;
            bloodMoonChance = 0.05f;
            groundBurstMinDays = 8f;
            groundBurstMaxDays = 14f;
            herdEventMinDays = 14f;
            herdEventMaxDays = 24f;
            graveEventMinDays = 12f;
            graveEventMaxDays = 20f;
            fastZombieChance = 0.01f;
        }

        private void ApplyApocalypsePreset()
        {
            ResetToRecommendedDefaults();
            outbreakIntensity = 6.0f;
            difficultyLevel = -999;
            trickleIntervalHours = 1.5f;
            trickleMinGroupSize = 2;
            trickleMaxGroupSize = 4;
            fullMoonBaseCount = 20;
            bloodMoonBaseCount = 40;
            bloodMoonChance = 0.20f;
            groundBurstMinDays = 3f;
            groundBurstMaxDays = 7f;
            groundBurstMinGroupSize = 3;
            groundBurstMaxGroupSize = 6;
            herdEventMinDays = 7f;
            herdEventMaxDays = 12f;
            graveEventMinDays = 5f;
            graveEventMaxDays = 12f;
            fastZombieChance = 0.05f;
            resurrectionDelayHours = 3f;
        }

        private void ClampAndRepair()
        {
            if (string.IsNullOrWhiteSpace(zombiePrefix))
            {
                zombiePrefix = "Zombie";
            }

            zombiePrefix = zombiePrefix.Trim();
            biterName = NormalizeVariantName(biterName, "Biter");
            runtName = MigrateLegacyVariantName(NormalizeVariantName(runtName, "Runt"), "Crawler", "Runt");
            boomerName = NormalizeVariantName(boomerName, "Boomer");
            sickName = NormalizeVariantName(sickName, "Sick");
            drownedName = NormalizeVariantName(drownedName, "Drowned");
            bruteName = MigrateLegacyVariantName(MigrateLegacyVariantName(NormalizeVariantName(bruteName, "Brute"), "Heavy", "Brute"), "Tank", "Brute");
            grabberName = NormalizeVariantName(grabberName, "Grabber");
            lurkerName = NormalizeVariantName(lurkerName, "Lurker");
            boneBiterName = NormalizeVariantName(boneBiterName, "Bone Biter");
            pregnantBoomerName = NormalizeVariantName(pregnantBoomerName, "Pregnant Boomer");
            if (outbreakIntensity < 0f)
            {
                outbreakIntensity = difficultyLevel >= 0 ? (1f + (Mathf.Clamp(difficultyLevel, 0, 8) * 0.25f)) : 4f;
            }

            outbreakIntensity = Mathf.Clamp(outbreakIntensity, 0.5f, 12f);
            difficultyLevel = -999;

            minGroupSize = Mathf.Clamp(minGroupSize, 1, 60);
            maxGroupSize = Mathf.Clamp(maxGroupSize, minGroupSize, 120);

            trickleIntervalHours = Mathf.Clamp(trickleIntervalHours, 0.5f, 24f);
            trickleMinGroupSize = Mathf.Clamp(trickleMinGroupSize, 1, 12);
            trickleMaxGroupSize = Mathf.Clamp(trickleMaxGroupSize, trickleMinGroupSize, 24);

            fullMoonBaseCount = Mathf.Clamp(fullMoonBaseCount, 6, 80);
            bloodMoonBaseCount = Mathf.Clamp(bloodMoonBaseCount, 12, 140);
            if (bloodMoonBaseCount < fullMoonBaseCount)
            {
                bloodMoonBaseCount = fullMoonBaseCount;
            }

            dayColonistMultiplierMin = Mathf.Clamp(dayColonistMultiplierMin, 1, 20);
            dayColonistMultiplierMax = Mathf.Clamp(dayColonistMultiplierMax, dayColonistMultiplierMin, 20);
            nightColonistMultiplierMin = Mathf.Clamp(nightColonistMultiplierMin, 1, 24);
            nightColonistMultiplierMax = Mathf.Clamp(nightColonistMultiplierMax, nightColonistMultiplierMin, 24);
            fullMoonColonistMultiplierMin = Mathf.Clamp(fullMoonColonistMultiplierMin, 1, 30);
            fullMoonColonistMultiplierMax = Mathf.Clamp(fullMoonColonistMultiplierMax, fullMoonColonistMultiplierMin, 30);
            bloodMoonColonistMultiplierMin = Mathf.Clamp(bloodMoonColonistMultiplierMin, 1, 40);
            bloodMoonColonistMultiplierMax = Mathf.Clamp(bloodMoonColonistMultiplierMax, bloodMoonColonistMultiplierMin, 40);

            bloodMoonChance = Mathf.Clamp(bloodMoonChance, 0.01f, 0.50f);
            fastZombieChance = Mathf.Clamp(fastZombieChance, 0f, 0.20f);
            resurrectionDelayHours = Mathf.Clamp(resurrectionDelayHours, 0.5f, 24f);
            infectionDaysToTurn = Mathf.Clamp(infectionDaysToTurn, 1, 30);

            groundBurstMinDays = Mathf.Clamp(groundBurstMinDays, 1f, 20f);
            groundBurstMaxDays = Mathf.Clamp(groundBurstMaxDays, groundBurstMinDays, 30f);
            groundBurstMinGroupSize = Mathf.Clamp(groundBurstMinGroupSize, 1, 12);
            groundBurstMaxGroupSize = Mathf.Clamp(groundBurstMaxGroupSize, groundBurstMinGroupSize, 18);

            herdEventMinDays = Mathf.Clamp(herdEventMinDays, 4f, 30f);
            herdEventMaxDays = Mathf.Clamp(herdEventMaxDays, herdEventMinDays, 40f);

            graveEventMinDays = Mathf.Clamp(graveEventMinDays, 3f, 30f);
            graveEventMaxDays = Mathf.Clamp(graveEventMaxDays, graveEventMinDays, 40f);

            prioritizedDoubleTapWorkTypeDefs ??= GetDefaultDoubleTapWorkTypes();
            prioritizedDoubleTapWorkTypeDefs = prioritizedDoubleTapWorkTypeDefs
                .Where(defName => !defName.NullOrEmpty() && DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName) != null)
                .Distinct()
                .ToList();
        }

        public bool IsDoubleTapWorkTypeSelected(WorkTypeDef workType)
        {
            return workType != null && prioritizedDoubleTapWorkTypeDefs != null && prioritizedDoubleTapWorkTypeDefs.Contains(workType.defName);
        }

        private void SetDoubleTapWorkTypeSelected(WorkTypeDef workType, bool selected)
        {
            if (workType == null)
            {
                return;
            }

            prioritizedDoubleTapWorkTypeDefs ??= GetDefaultDoubleTapWorkTypes();
            if (selected)
            {
                if (!prioritizedDoubleTapWorkTypeDefs.Contains(workType.defName))
                {
                    prioritizedDoubleTapWorkTypeDefs.Add(workType.defName);
                }
            }
            else
            {
                prioritizedDoubleTapWorkTypeDefs.Remove(workType.defName);
            }
        }

        private static List<WorkTypeDef> GetAvailableDoubleTapWorkTypes()
        {
            return ZombieDoubleTapUtility.GetAvailableWorkTypes().ToList();
        }

        private static List<string> GetDefaultDoubleTapWorkTypes()
        {
            return ZombieDoubleTapUtility.GetDefaultWorkTypeDefNames();
        }

        private string NormalizeVariantName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string trimmed = value.Trim();
            if (trimmed.Length > 24)
            {
                trimmed = trimmed.Substring(0, 24).Trim();
            }

            return trimmed.NullOrEmpty() ? fallback : trimmed;
        }

        private string MigrateLegacyVariantName(string value, string legacyDefault, string newDefault)
        {
            return string.Equals(value, legacyDefault, System.StringComparison.Ordinal) ? newDefault : value;
        }

        private void DrawHeader(Rect rect)
        {
            SettingsTheme.DrawHeader(rect);
            SettingsTheme.DrawHeaderWatermark(rect);

            Rect titleRect = new Rect(rect.x + 86f, rect.y + 10f, rect.width - 188f, 32f);
            Rect subtitleRect = new Rect(rect.x + 86f, rect.y + 40f, rect.width - 188f, 22f);
            Rect hintRect = new Rect(rect.x + 86f, rect.y + 62f, rect.width - 188f, 20f);
            Rect badgeRect = new Rect(rect.xMax - 122f, rect.y + 20f, 96f, 28f);

            Text.Font = GameFont.Medium;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(titleRect, T("SettingsTitle"));
            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(subtitleRect, T("SettingsSubtitle"));
            Widgets.Label(hintRect, T("SettingsHint"));
            GUI.color = Color.white;

            SettingsTheme.DrawStatusPill(badgeRect, true);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(badgeRect, T("BadgeActive"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawTabButtons(Rect rect)
        {
            string[] labels = { T("TabOverview"), T("TabEvents"), T("TabVariants"), T("TabNames"), T("TabColony"), T("TabDebug") };
            SettingsTab[] tabs =
            {
                SettingsTab.Overview,
                SettingsTab.Events,
                SettingsTab.Variants,
                SettingsTab.Names,
                SettingsTab.Advanced,
                SettingsTab.Debug
            };

            float gap = 6f;
            float width = (rect.width - (gap * (labels.Length - 1))) / labels.Length;

            for (int i = 0; i < labels.Length; i++)
            {
                Rect buttonRect = new Rect(rect.x + (i * (width + gap)), rect.y, width, rect.height);
                bool active = selectedTab == tabs[i];
                if (SettingsTheme.DrawButton(buttonRect, labels[i], active))
                {
                    selectedTab = tabs[i];
                }
            }
        }

        private void DrawOverviewTab(Rect rect)
        {
            BeginScrollableListing(rect, ref overviewScrollPosition, ref overviewViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "Start here. This is the fast pass for shaping how the outbreak feels without digging through every last toggle.");
            DrawSectionLabel(listing, "Quick setup", "If you only touch a handful of settings, these are the ones that matter most.");
            DrawPresetButtons(listing);
            DrawDifficultyCard(listing);
            DrawToggleCard(listing, "Show zombie HUD", "Shows the live zombie count and current Danger on your home map.", ref showZombieCounter);
            DrawInfoCard(listing, "How Danger is calculated", "Danger compares active zombies to your current target population. Daytime target = colonists × outbreak intensity. Night target = daytime target × 1.5. Example: 4 colonists at 2.0 intensity gives a daytime target of 8 zombies. If 6 are active, Danger is 75 percent.");

            DrawSectionLabel(listing, "Core outbreak feel", "These are the big knobs for how hard the undead lean on the colony from day to night.");
            DrawInfoCard(listing, "Zombie combat rules", "Hostile zombies belong to the zombie faction. Zombies have no ideoligion, count as cannibals, ignore pain, filth, and nudity, move very slowly unless they roll the rare runner strain, gain about 50 percent more movement speed at night, and swarm loud gunfire much harder than quiet melee. Lurkers stay as the friendly exception.");
            DrawPercentStepperCard(listing, "Runner strain chance", "Chance for a fresh spawn to roll the faster runner strain.", ref fastZombieChance, 0f, 0.20f, 0.01f);

            listing.End();
            overviewViewHeight = Mathf.Max(1200f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawEventsTab(Rect rect)
        {
            BeginScrollableListing(rect, ref eventsScrollPosition, ref eventsViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "This is where the outbreak gets its rhythm, from background pressure all the way up to moon attacks and full herd crossings.");
            DrawSectionLabel(listing, "Constant edge trickle", "Small groups should keep shambling in from the map edges so the colony never feels fully safe. Higher outbreak intensity should refill the map faster and keep more pressure on it.");
            DrawToggleCard(listing, "Enable edge trickle", "Lets small zombie groups keep coming in from the map edges. Higher outbreak intensity shortens the gap between trickles and helps the mod keep the map closer to its target zombie count.", ref enableEdgeTrickle);
            if (enableEdgeTrickle)
            {
                DrawFloatStepperCard(listing, "Time between trickles", "Lower values make edge groups show up more often.", ref trickleIntervalHours, 0.5f, 24f, 0.25f, "hours");
                DrawIntStepperCard(listing, "Trickle minimum group size", "Smallest group size the edge trickle can send.", ref trickleMinGroupSize, 1, 12, 1);
                DrawIntStepperCard(listing, "Trickle maximum group size", "Largest group size the edge trickle can send.", ref trickleMaxGroupSize, trickleMinGroupSize, 24, 1);
            }
            else
            {
                DrawInfoCard(listing, "Edge trickle is off", "Only larger events and manual spawns will add new pressure from off map while this is disabled.");
            }

            DrawSectionLabel(listing, "Colony scaled population", "This sets the soft target for how many zombies the mod tries to keep on your map during ordinary play.");
            DrawToggleCard(listing, "Scale zombie population to colonists", "Uses your free colonist count to set the normal day and night zombie target instead of leaning on older fixed scaling rules.", ref useColonistScaledPopulation);
            if (useColonistScaledPopulation)
            {
                DrawInfoCard(listing, "How normal population works", $"Daytime target = colonists × outbreak intensity. Right now that means {DaytimeTargetMultiplier:0.0} zombies per colonist during the day. At night, that target rises by 50 percent to {NighttimeTargetMultiplier:0.0} zombies per colonist, and zombies also gain about 50 percent more movement speed at night.");
                DrawInfoCard(listing, "What still pushes past that", "Moon events, debug spawns, leftover bodies, and other special surges can still push the active count above the usual target. When that happens, Danger can go over 100%.");
            }
            else
            {
                DrawInfoCard(listing, "Colonist scaling is off", "Normal population pressure falls back to the older fixed rules instead of using outbreak intensity against your colonist count.");
            }

            DrawSectionLabel(listing, "Moon events", "Big night spikes tied to the moon cycle. Blood moons are rarer, nastier, and push the map harder.");
            DrawToggleCard(listing, "Enable moon events", "Roughly every 30 days, a larger horde can show up. Blood moons send an even bigger one, and moon pressure can shove the whole map into a harder base push.", ref enableMoonEvents);
            if (enableMoonEvents)
            {
                if (!useColonistScaledPopulation)
                {
                    DrawIntStepperCard(listing, "Full moon base horde size", "Base horde size for a full moon when colonist scaling is off.", ref fullMoonBaseCount, 6, 80, 2);
                    DrawIntStepperCard(listing, "Blood moon base horde size", "Base horde size for a blood moon when colonist scaling is off.", ref bloodMoonBaseCount, fullMoonBaseCount, 140, 2);
                }
                DrawPercentStepperCard(listing, "Blood moon chance", "Chance for a full moon to turn into a blood moon.", ref bloodMoonChance, 0.01f, 0.50f, 0.01f);
            }
            else
            {
                DrawInfoCard(listing, "Moon events are off", "Full moon and blood moon attacks will stay out of the normal event rotation until you turn them back on.");
            }

            DrawSectionLabel(listing, "Herd crossings", "These are the big migrating herds that enter from one side, cross the whole map, and leave on the far side. They are meant to feel like a moving wall, not a normal raid.");
            DrawToggleCard(listing, "Enable herd events", "Lets massive zombie herds cross the map in a broad lane instead of showing up as a normal attack wave.", ref enableHerdEvents);
            if (enableHerdEvents)
            {
                DrawFloatStepperCard(listing, "Minimum days between herds", "Shortest possible gap between herd events.", ref herdEventMinDays, 4f, 30f, 0.5f, "days");
                DrawFloatStepperCard(listing, "Maximum days between herds", "Longest possible gap between herd events.", ref herdEventMaxDays, herdEventMinDays, 40f, 0.5f, "days");
                DrawInfoCard(listing, "Herd size", "Herd size is automatic. The mod aims for 10 times your colonists, clamps that to 50 to 75, and gives the herd a temporary speed push so it keeps moving.");
            }
            else
            {
                DrawInfoCard(listing, "Herd events are off", "No map crossing herds will fire from the event system while this is disabled.");
            }

            DrawSectionLabel(listing, "Ground bursts", "These are the surprise eruptions that can pop up inside your base. Outbreak intensity also squeezes their timing and group size upward.");
            DrawToggleCard(listing, "Enable ground bursts", "Lets small zombie groups erupt from the ground inside your colony.", ref enableGroundBursts);
            if (enableGroundBursts)
            {
                DrawFloatStepperCard(listing, "Minimum days between bursts", "Shortest possible gap between ground burst events.", ref groundBurstMinDays, 1f, 20f, 0.5f, "days");
                DrawFloatStepperCard(listing, "Maximum days between bursts", "Longest possible gap between ground burst events.", ref groundBurstMaxDays, groundBurstMinDays, 30f, 0.5f, "days");
                DrawIntStepperCard(listing, "Ground burst minimum group size", "Smallest group a ground burst can send.", ref groundBurstMinGroupSize, 1, 12, 1);
                DrawIntStepperCard(listing, "Ground burst maximum group size", "Largest group a ground burst can send.", ref groundBurstMaxGroupSize, groundBurstMinGroupSize, 18, 1);
            }
            else
            {
                DrawInfoCard(listing, "Ground bursts are off", "No buried eruptions will fire until you turn this back on.");
            }

            DrawSectionLabel(listing, "Grave events", "These rare events create a spawning grave that keeps causing trouble until you destroy it. Higher outbreak intensity also makes them roll around sooner.");
            DrawToggleCard(listing, "Enable grave events", "Lets rare grave events appear and keep spawning more bodies.", ref enableGraveEvents);
            if (enableGraveEvents)
            {
                DrawFloatStepperCard(listing, "Minimum days between grave events", "Shortest possible gap between grave events.", ref graveEventMinDays, 3f, 30f, 0.5f, "days");
                DrawFloatStepperCard(listing, "Maximum days between grave events", "Longest possible gap between grave events.", ref graveEventMaxDays, graveEventMinDays, 40f, 0.5f, "days");
            }
            else
            {
                DrawInfoCard(listing, "Grave events are off", "No spawning graves will appear from the event system while this is disabled.");
            }

            listing.End();
            eventsViewHeight = Mathf.Max(2400f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawVariantsTab(Rect rect)
        {
            BeginScrollableListing(rect, ref variantsScrollPosition, ref variantsViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "Use this tab to decide which strains are part of the normal outbreak pool.");
            DrawSectionLabel(listing, "Main strain roster", "Turn each core strain on or off. Disabled strains stay out of ordinary waves, debug free play balance, and grave events that use the regular pool.");
            DrawVariantCard(listing, "Standard biters", "Likes: Prey nearby, loud gunfire, moving toward the colony, fresh flesh. Dislikes: Empty ground, quiet areas, losing track of prey, being far from the fight.", ref allowBiters);
            DrawVariantCard(listing, "Boomers", "Likes: Crowded prey, close range fights, tight formations, bursting in the middle of enemies. Dislikes: Dying alone, spread out targets, long range fights, empty approach lanes.", ref allowBoomers);
            DrawVariantCard(listing, "Sick", "Likes: Infected prey, spreading filth, sick spew hitting targets, contaminated battlefields. Dislikes: Clean rooms, prey staying out of range, fire clearing filth, long stretches without spreading infection.", ref allowSick);
            DrawVariantCard(listing, "Drowned", "Likes: Rain, water, wet ground, shoreline hunting, returning to water. Dislikes: Dry land, hot clear weather, being far from water, getting stranded inland.", ref allowDrowned);
            DrawVariantCard(listing, "Brutes", "Likes: Close melee, breaking through defenses, smashing prey, dense fighting. Dislikes: Being kited, long range fire, prey escaping, getting stalled before reaching the target.", ref allowBrutes);
            DrawVariantCard(listing, "Grabbers", "Likes: Prey in tongue range, holding a victim in place, panic at close range, packs closing in on trapped prey. Dislikes: Prey breaking free, broken line of sight, fast targets, fighting alone.", ref allowGrabbers);
            DrawVariantCard(listing, "Runts", "Likes: Packs nearby, tight paths, crawling toward prey, downed victims. Dislikes: Open ground, being left behind, fast moving prey, empty quiet spaces.", ref allowRunts);

            if (!allowBiters && !allowRunts && !allowBoomers && !allowSick && !allowDrowned && !allowBrutes && !allowGrabbers)
            {
                DrawWarningCard(listing, "No strains are enabled. Standard biters will be used as a safe fallback.");
            }

            DrawSectionLabel(listing, "Special states and exceptions", "These are not part of the main hostile roster toggle list, but they still matter to how the undead feel in play.");
            DrawInfoCard(listing, "Bone Biter", "Likes: Fresh corpses nearby, feeding on corpses, battlefields full of bodies, being left alone while feeding. Dislikes: Feeding interrupted, corpses taken away, clean empty ground, being pulled away from a body.");
            DrawInfoCard(listing, "Lurker", "Likes: Wandering near the colony, familiar ground, being left alone, quiet base life. Dislikes: Forced aggression, being disturbed, being driven away, open hostility near its home area.");
            DrawInfoCard(listing, "Runner strain", "Likes: Open lanes, fleeing prey, fast pursuit, sudden pressure. Dislikes: Tight stalls, being slowed, losing momentum, blocked paths.");
            DrawInfoCard(listing, "Pregnant Boomer", "Likes: Crowded prey, close quarters, bursting near enemies, leaving something behind. Dislikes: Long range kills, empty lanes, isolated death, targets keeping their distance.");

            DrawSectionLabel(listing, "Recommended setup", "Most players will want every strain enabled. Turn one off only if you do not like what it adds.");
            if (DrawActionCard(listing, "Enable every strain", "Turns the full strain roster back on with one click."))
            {
                allowBiters = true;
                allowRunts = true;
                allowBoomers = true;
                allowSick = true;
                allowDrowned = true;
                allowBrutes = true;
                allowGrabbers = true;
            }

            listing.End();
            variantsViewHeight = Mathf.Max(1500f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawNamesTab(Rect rect)
        {
            BeginScrollableListing(rect, ref namesScrollPosition, ref namesViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "Every naming option lives here now, from the overall zombie family name down to each strain label, so you do not have to hunt for them across multiple tabs.");
            DrawSectionLabel(listing, "Shared naming", "These labels show up all across the mod and are the ones players usually notice first.");
            DrawTextEntryCard(listing, "Zombie family name", "Sets the shared family name used in zombie labels, letters, HUD text, and other outbreak messaging.", ref zombiePrefix, "Zombie");
            DrawInfoCard(listing, "Example names", ZombieDefUtility.ExampleNames(zombiePrefix));

            DrawSectionLabel(listing, "Main strain names", "These are the core outbreak names used in labels, letters, graves, and debug messages.");
            DrawTextEntryCard(listing, "Biter name", "The in game name used for your basic zombie strain.", ref biterName, "Biter");
            DrawTextEntryCard(listing, "Boomer name", "The in game name used for the acid bursting strain.", ref boomerName, "Boomer");
            DrawTextEntryCard(listing, "Sick name", "The in game name used for the infection spreading strain.", ref sickName, "Sick");
            DrawTextEntryCard(listing, "Drowned name", "The in game name used for the waterlogged strain.", ref drownedName, "Drowned");
            DrawTextEntryCard(listing, "Brute name", "The in game name used for the heavy strain.", ref bruteName, "Brute");
            DrawTextEntryCard(listing, "Grabber name", "The in game name used for the grabbing strain.", ref grabberName, "Grabber");
            DrawTextEntryCard(listing, "Runt name", "The in game name used for the dragging runt strain.", ref runtName, "Runt");

            DrawSectionLabel(listing, "Special case names", "These rename special zombies that show up through debug tools or special events.");
            DrawTextEntryCard(listing, "Lurker name", "The in game name used for the passive capturable strain.", ref lurkerName, "Lurker");
            DrawTextEntryCard(listing, "Bone biter name", "The in game name used for the skeletal biter variant.", ref boneBiterName, "Bone Biter");
            DrawTextEntryCard(listing, "Pregnant boomer name", "The in game name used for the boomer variant that can burst into runts.", ref pregnantBoomerName, "Pregnant Boomer");

            listing.End();
            namesViewHeight = Mathf.Max(1750f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawAdvancedTab(Rect rect)
        {
            BeginScrollableListing(rect, ref advancedScrollPosition, ref advancedViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "Use this tab for colony side cleanup, infection pacing, and the practical rules that shape how your pawns deal with the aftermath.");

            DrawSectionLabel(listing, "Prioritized double tapping", "Optional cleanup behavior for players who want pawns to go finish fresh zombie corpses automatically.");
            DrawToggleCard(listing, "Enable prioritized double tapping", "When this is on, selected work types can break away and finish fresh zombie corpses as a cleanup task.", ref enablePrioritizedDoubleTap);
            DrawDoubleTapWorkTypeChecklist(listing);
            if (!enablePrioritizedDoubleTap)
            {
                DrawInfoCard(listing, "Double tapping is off", "The checklist below is still saved, but pawns will ignore it until prioritized double tapping is turned on.");
            }

            DrawSectionLabel(listing, "Corpse handling", "Mostly simple quality of life rules for zombie remains once the fight is over.");
            DrawToggleCard(listing, "Allow zombie corpses by default", "Fresh zombie corpses start out allowed so colonists can haul, butcher, or double tap them without manual clicks.", ref autoAllowZombieCorpses);

            DrawSectionLabel(listing, "Zombie infection", "This only affects living infection progression. Zombies themselves now die like normal pawns.");
            DrawIntStepperCard(listing, "Days until infection reaches 99% transformation", "How many in game days it takes an untreated zombie infection to reach the final living transformation point and convert the victim into a lurker. It gets worse in daily steps.", ref infectionDaysToTurn, 1, 30, 1);

            DrawSectionLabel(listing, "Reset", "Use this if things get messy and you want to go back to a known good setup.");
            if (DrawActionCard(listing, "Reset settings to recommended defaults", "Puts every setting back to the recommended values."))
            {
                ResetToRecommendedDefaults();
            }

            listing.End();
            advancedViewHeight = Mathf.Max(1650f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawDebugTab(Rect rect)
        {
            BeginScrollableListing(rect, ref debugScrollPosition, ref debugViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "About this tab", "Use this tab to force events and test behavior on a live colony map. Great for tuning, bad for a normal playthrough.");
            DrawSectionLabel(listing, "Debug mode", "Use this for testing and screenshots. It is best left off during normal play.");
            DrawToggleCard(listing, "Enable debug controls", "Shows manual buttons for forced waves, herd crossings, moon events, grave events, and test spawns while a colony is loaded.", ref enableDebugControls);
            DrawSectionLabel(listing, "Debug spawn sizing", "These sizes are only used by manual debug spawns and forced test events.");
            DrawIntStepperCard(listing, "Debug spawn minimum", "Smallest group size used by manual spawns and forced events.", ref minGroupSize, 1, 60, 1);
            DrawIntStepperCard(listing, "Debug spawn maximum", "Largest group size used by manual spawns and forced events.", ref maxGroupSize, minGroupSize, 120, 1);

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            bool canUseDebug = enableDebugControls && component != null && component.HasUsableDebugMap();

            if (!enableDebugControls)
            {
                DrawInfoCard(listing, "Debug controls are hidden.", "Turn on debug controls above to show the manual test buttons.");
            }
            else if (!canUseDebug)
            {
                DrawInfoCard(listing, "Load a colony map first.", "The debug buttons only work while a valid colony map is open.");
            }
            else
            {
                DrawSectionLabel(listing, "Manual event buttons", "These actions fire right away on the current colony map.");
                DrawDebugActionButton(listing, component, "Force edge wave now", "Forced an edge wave.", "Could not force an edge wave.");
                DrawDebugActionButton(listing, component, "Force nightly edge wave now", "Forced the nightly edge wave.", "Could not force the nightly edge wave.");
                DrawDebugActionButton(listing, component, "Force huddled pack now", "Forced a huddled pack.", "Could not force a huddled pack.");
                DrawDebugActionButton(listing, component, "Force base push now", "Forced a base push.", "Could not force a base push.");
                DrawDebugActionButton(listing, component, "Force edge wanderers now", "Forced edge wanderers.", "Could not force edge wanderers.");
                DrawDebugActionButton(listing, component, "Force herd now", "Forced a herd crossing.", "Could not force a herd crossing.");
                DrawDebugActionButton(listing, component, "Force ground burst now", "Forced a ground burst.", "Could not force a ground burst.");

                DrawSectionLabel(listing, "Moon buttons", "Use these to force moon hordes right away while you test night pressure.");
                DrawDebugActionButton(listing, component, "Force full moon horde now", "Forced a full moon horde.", "Could not force a full moon horde.");
                DrawDebugActionButton(listing, component, "Force blood moon horde now", "Forced a blood moon horde.", "Could not force a blood moon horde.");

                DrawSectionLabel(listing, "Grave buttons", "Use these to test random graves or a specific grave strain.");
                DrawDebugActionButton(listing, component, "Force random grave event now", "Forced a grave event.", "Could not force a grave event.");
                DrawDebugActionButton(listing, component, "Force biter grave now", "Forced a biter grave.", "Could not force a biter grave.");
                DrawDebugActionButton(listing, component, "Force runt grave now", "Forced a runt grave.", "Could not force a runt grave.");
                DrawDebugActionButton(listing, component, "Force boomer grave now", "Forced a boomer grave.", "Could not force a boomer grave.");
                DrawDebugActionButton(listing, component, "Force sick grave now", "Forced a sick grave.", "Could not force a sick grave.");
                DrawDebugActionButton(listing, component, "Force drowned grave now", "Forced a drowned grave.", "Could not force a drowned grave.");
                DrawDebugActionButton(listing, component, "Force brute grave now", "Forced a brute grave.", "Could not force a brute grave.");
                DrawDebugActionButton(listing, component, "Force grabber grave now", "Forced a grabber grave.", "Could not force a grabber grave.");

                DrawSectionLabel(listing, "Manual spawn buttons", "Use these to drop in one of each strain for direct testing.");
                DrawDebugActionButton(listing, component, "Spawn biter now", "Spawned a biter.", "Could not spawn a biter.");
                DrawDebugActionButton(listing, component, "Spawn Bone Biter now", "Spawned a bone biter.", "Could not spawn a bone biter.");
                DrawDebugActionButton(listing, component, "Spawn runt now", "Spawned a runt.", "Could not spawn a runt.");
                DrawDebugActionButton(listing, component, "Spawn boomer now", "Spawned a boomer.", "Could not spawn a boomer.");
                DrawDebugActionButton(listing, component, "Spawn pregnant boomer now", "Spawned a pregnant boomer.", "Could not spawn a pregnant boomer.");
                DrawDebugActionButton(listing, component, "Spawn sick now", "Spawned a sick zombie.", "Could not spawn a sick zombie.");
                DrawDebugActionButton(listing, component, "Spawn drowned now", "Spawned a drowned zombie.", "Could not spawn a drowned zombie.");
                DrawDebugActionButton(listing, component, "Spawn brute now", "Spawned a brute.", "Could not spawn a brute.");
                DrawDebugActionButton(listing, component, "Spawn grabber now", "Spawned a grabber.", "Could not spawn a grabber.");
                DrawDebugActionButton(listing, component, "Spawn lurker now", "Spawned a lurker.", "Could not spawn a lurker.");
            }

            listing.End();
            debugViewHeight = Mathf.Max(2200f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawDoubleTapWorkTypeChecklist(Listing_Standard listing)
        {
            List<WorkTypeDef> workTypes = GetAvailableDoubleTapWorkTypes();
            if (workTypes == null)
            {
                workTypes = new List<WorkTypeDef>();
            }

            float descriptionHeight = CalculateWrappedTextHeight("Every loaded work type is listed here so the player can decide which jobs should break away and double tap fresh zombie corpses.", Mathf.Max(220f, listing.ColumnWidth - 28f));
            float rowHeight = 28f;
            float rowsHeight = Mathf.Max(rowHeight, workTypes.Count * rowHeight);
            float cardHeight = Mathf.Max(160f, 56f + descriptionHeight + rowsHeight + 24f);
            Rect row = DrawCard(listing, cardHeight);

            Rect titleRect = new Rect(row.x + 12f, row.y + 10f, row.width - 24f, 22f);
            Rect descRect = new Rect(row.x + 12f, row.y + 34f, row.width - 24f, descriptionHeight + 6f);

            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(titleRect, "Checked work types");
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(descRect, "Every loaded work type is listed here so the player can decide which jobs should break away and double tap fresh zombie corpses.");
            GUI.color = Color.white;

            if (workTypes.Count == 0)
            {
                Rect emptyRect = new Rect(row.x + 12f, descRect.yMax + 10f, row.width - 24f, 24f);
                GUI.color = SettingsTheme.WarningTint;
                Widgets.Label(emptyRect, "No work types are loaded right now.");
                GUI.color = Color.white;
                TooltipHandler.TipRegion(row, "Selected jobs will treat zombie double tapping as a top cleanup task while the feature is enabled.");
                return;
            }

            float startY = descRect.yMax + 10f;
            for (int i = 0; i < workTypes.Count; i++)
            {
                WorkTypeDef workType = workTypes[i];
                Rect itemRect = new Rect(row.x + 12f, startY + (i * rowHeight), row.width - 24f, 24f);
                bool selected = IsDoubleTapWorkTypeSelected(workType);
                Rect checkboxRect = new Rect(itemRect.x, itemRect.y, 24f, 24f);
                Widgets.Checkbox(checkboxRect.position, ref selected, 24f);
                SetDoubleTapWorkTypeSelected(workType, selected);

                GUI.color = SettingsTheme.Ink;
                Widgets.Label(new Rect(itemRect.x + 30f, itemRect.y + 2f, itemRect.width - 120f, 22f), ZombieDoubleTapUtility.GetWorkTypeDisplayLabel(workType));
                GUI.color = SettingsTheme.MutedInk;
                Text.Anchor = TextAnchor.MiddleRight;
                Widgets.Label(new Rect(itemRect.x, itemRect.y + 1f, itemRect.width - 4f, 22f), selected ? T("StateChecked") : T("StateUnchecked"));
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }

            TooltipHandler.TipRegion(row, "Selected jobs will treat zombie double tapping as a top cleanup task while the feature is enabled.");
        }

        private void BeginScrollableListing(Rect rect, ref Vector2 scrollPosition, ref float viewHeight, out Listing_Standard listing, out Rect viewRect)
        {
            viewRect = new Rect(0f, 0f, rect.width - 18f, viewHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            listing = new Listing_Standard();
            listing.Begin(viewRect);
        }

        private void DrawSectionLabel(Listing_Standard listing, string title, string description)
        {
            float descriptionHeight = CalculateWrappedTextHeight(description, listing.ColumnWidth - 28f);
            float sectionHeight = Mathf.Max(76f, 40f + descriptionHeight + 20f);
            Rect rect = listing.GetRect(sectionHeight);
            SettingsTheme.DrawSectionBand(rect);

            Rect titleRect = new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 26f);
            Rect descRect = new Rect(rect.x + 14f, rect.y + 44f, rect.width - 28f, descriptionHeight + 8f);

            Text.Font = GameFont.Medium;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(descRect, description);
            GUI.color = Color.white;
            listing.Gap(12f);
        }

        private void DrawPresetButtons(Listing_Standard listing)
        {
            Rect row = listing.GetRect(84f);
            float gap = 8f;
            float width = (row.width - (gap * 2f)) / 3f;

            if (SettingsTheme.DrawButton(new Rect(row.x, row.y, width, 34f), T("PresetCasual")))
            {
                ApplyCasualPreset();
            }

            if (SettingsTheme.DrawButton(new Rect(row.x + width + gap, row.y, width, 34f), T("PresetRecommended"), true))
            {
                ResetToRecommendedDefaults();
            }

            if (SettingsTheme.DrawButton(new Rect(row.x + ((width + gap) * 2f), row.y, width, 34f), T("PresetApocalypse")))
            {
                ApplyApocalypsePreset();
            }

            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(new Rect(row.x, row.y + 44f, row.width, 32f), "Casual is lighter. Recommended is the intended baseline. Apocalypse pushes things toward late game chaos.");
            GUI.color = Color.white;
            listing.Gap(10f);
        }

        private void DrawTextEntryCard(Listing_Standard listing, string label, string description, ref string value, string fallback)
        {
            float contentWidth = Mathf.Max(220f, listing.ColumnWidth - 32f);
            float descriptionHeight = CalculateWrappedTextHeight(description, contentWidth);
            float cardHeight = Mathf.Max(130f, 62f + descriptionHeight + 46f);
            Rect row = DrawCard(listing, cardHeight);
            Rect labelRect = new Rect(row.x + 12f, row.y + 10f, row.width - 24f, 22f);
            Rect descRect = new Rect(row.x + 12f, row.y + 34f, row.width - 24f, descriptionHeight + 4f);
            Rect shellRect = new Rect(row.x + 12f, descRect.yMax + 10f, row.width - 24f, 32f);
            Rect textRect = shellRect.ContractedBy(4f);

            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(labelRect, label);
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(descRect, description);
            GUI.color = Color.white;

            SettingsTheme.DrawTextFieldShell(shellRect);
            value = Widgets.TextField(textRect, value ?? fallback);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = fallback;
            }

            TooltipHandler.TipRegion(row, description);
        }

        private void DrawToggleCard(Listing_Standard listing, string label, string description, ref bool value)
        {
            float descriptionHeight = CalculateWrappedTextHeight(description, listing.ColumnWidth - 180f);
            float cardHeight = Mathf.Max(82f, 34f + descriptionHeight + 20f);
            Rect row = DrawCard(listing, cardHeight);
            Rect checkboxRect = new Rect(row.x + 12f, row.y + 14f, 24f, 24f);
            Rect pillRect = new Rect(row.x + row.width - 104f, row.y + 14f, 92f, 24f);
            Widgets.Checkbox(checkboxRect.position, ref value, 24f);
            DrawCardText(new Rect(row.x + 40f, row.y, row.width - 152f, row.height), label, description, null, 0f);
            SettingsTheme.DrawStatusPill(pillRect, value);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(pillRect, value ? T("StateEnabled") : T("StateDisabled"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(row, description);
        }

        private void DrawDifficultyCard(Listing_Standard listing)
        {
            string description = "Sets the normal daytime target population. Daytime target = colonists × outbreak intensity. At night, that target rises by 50 percent, and zombies also gain about 50 percent more movement speed at night. This setting also speeds up trickles, refill pressure, bursts, and grave timing so the rest of the outbreak keeps pace automatically. Default 4.0 means the mod tries to keep about four times your colonist count on the map during the day and about six times at night. You can now push this as high as 12.0 for very dense outbreaks.";
            float cardHeight = CalculateStepperCardHeight(listing, description);
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(row, "Outbreak intensity", description, null, 236f);
            DrawStepperControls(row, ref outbreakIntensity, 0.5f, 12f, 0.5f, $"{outbreakIntensity:0.0}x day  |  {NighttimeTargetMultiplier:0.0}x night");
        }

        private void DrawIntStepperCard(Listing_Standard listing, string label, string description, ref int value, int min, int max, int step)
        {
            float cardHeight = CalculateStepperCardHeight(listing, description);
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(row, label, description, null, 236f);
            DrawStepperControls(row, ref value, min, max, step, value.ToString());
        }

        private void DrawFloatStepperCard(Listing_Standard listing, string label, string description, ref float value, float min, float max, float step, string suffix)
        {
            float cardHeight = CalculateStepperCardHeight(listing, description);
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(row, label, description, null, 236f);
            DrawStepperControls(row, ref value, min, max, step, $"{value:0.0} {suffix}".Trim());
        }

        private void DrawPercentStepperCard(Listing_Standard listing, string label, string description, ref float value, float min, float max, float step)
        {
            float cardHeight = CalculateStepperCardHeight(listing, description);
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(row, label, description, null, 236f);
            DrawPercentControls(row, ref value, min, max, step, $"{value * 100f:0}%");
        }

        private void DrawVariantCard(Listing_Standard listing, string label, string description, ref bool value)
        {
            float descriptionHeight = CalculateWrappedTextHeight(description, listing.ColumnWidth - 180f);
            float cardHeight = Mathf.Max(82f, 34f + descriptionHeight + 20f);
            Rect row = DrawCard(listing, cardHeight);
            Rect checkboxRect = new Rect(row.x + 12f, row.y + 14f, 24f, 24f);
            Rect pillRect = new Rect(row.x + row.width - 104f, row.y + 14f, 92f, 24f);
            Widgets.Checkbox(checkboxRect.position, ref value, 24f);
            DrawCardText(new Rect(row.x + 40f, row.y, row.width - 152f, row.height), label, description, null, 0f);
            SettingsTheme.DrawStatusPill(pillRect, value);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(pillRect, value ? T("StateEnabled") : T("StateDisabled"));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(row, description);
        }

        private bool DrawActionCard(Listing_Standard listing, string label, string description)
        {
            float cardHeight = Mathf.Max(100f, 34f + 18f + CalculateWrappedTextHeight(description, Mathf.Max(160f, listing.ColumnWidth - 220f)));
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(new Rect(row.x, row.y, row.width - 176f, row.height), label, description, null, 0f);
            Rect buttonRect = new Rect(row.x + row.width - 164f, row.y + ((row.height - 34f) / 2f), 152f, 34f);
            return SettingsTheme.DrawButton(buttonRect, T("ButtonApply"));
        }

        private void DrawInfoCard(Listing_Standard listing, string label, string description)
        {
            float cardHeight = Mathf.Max(92f, 22f + 26f + CalculateWrappedTextHeight(description, Mathf.Max(220f, listing.ColumnWidth - 28f)) + 18f);
            Rect row = DrawCard(listing, cardHeight);
            DrawCardText(row, label, description, null, 0f);
        }

        private void DrawWarningCard(Listing_Standard listing, string description)
        {
            float cardHeight = Mathf.Max(70f, 22f + CalculateWrappedTextHeight(description, Mathf.Max(220f, listing.ColumnWidth - 28f)) + 20f);
            Rect row = listing.GetRect(cardHeight);
            SettingsTheme.DrawWarningCard(row);
            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(new Rect(row.x + 12f, row.y + 12f, row.width - 24f, row.height - 22f), description);
            GUI.color = Color.white;
        }

        private void DrawDebugActionButton(Listing_Standard listing, ZombieGameComponent component, string label, string successText, string failText)
        {
            float cardHeight = Mathf.Max(74f, 22f + CalculateWrappedTextHeight(label, Mathf.Max(160f, listing.ColumnWidth - 200f)) + 22f);
            Rect row = DrawCard(listing, cardHeight);
            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(new Rect(row.x + 12f, row.y + 12f, row.width - 184f, row.height - 24f), label);
            GUI.color = Color.white;
            Rect buttonRect = new Rect(row.x + row.width - 160f, row.y + ((row.height - 34f) / 2f), 148f, 34f);
            if (SettingsTheme.DrawButton(buttonRect, T("ButtonRunNow")))
            {
                bool success = ResolveDebugAction(component, label);
                ShowDebugResult(success, successText, failText);
            }
        }

        private bool ResolveDebugAction(ZombieGameComponent component, string label)
        {
            switch (label)
            {
                case "Force edge wave now":
                    return component.DebugForceEdgeWave();
                case "Force nightly edge wave now":
                    return component.DebugForceNightlyWave();
                case "Force huddled pack now":
                    return component.DebugForceHuddledPack();
                case "Force base push now":
                    return component.DebugForceBasePush();
                case "Force edge wanderers now":
                    return component.DebugForceEdgeWanderers();
                case "Force herd now":
                    return component.DebugForceHerd();
                case "Force ground burst now":
                    return component.DebugForceGroundBurst();
                case "Force random grave event now":
                    return component.DebugForceRandomGraveEvent();
                case "Force biter grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Biter);
                case "Force runt grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Runt);
                case "Force boomer grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Boomer);
                case "Force sick grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Sick);
                case "Force drowned grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Drowned);
                case "Force brute grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Brute);
                case "Spawn biter now":
                    return component.DebugSpawnVariant(ZombieVariant.Biter);
                case "Spawn Bone Biter now":
                    return component.DebugSpawnBoneBiter();
                case "Spawn runt now":
                    return component.DebugSpawnRunt();
                case "Spawn boomer now":
                    return component.DebugSpawnVariant(ZombieVariant.Boomer);
                case "Spawn pregnant boomer now":
                    return component.DebugSpawnPregnantBoomer();
                case "Spawn sick now":
                    return component.DebugSpawnVariant(ZombieVariant.Sick);
                case "Spawn drowned now":
                    return component.DebugSpawnVariant(ZombieVariant.Drowned);
                case "Spawn brute now":
                    return component.DebugSpawnVariant(ZombieVariant.Brute);
                case "Spawn grabber now":
                    return component.DebugSpawnVariant(ZombieVariant.Grabber);
                case "Force grabber grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Grabber);
                case "Spawn lurker now":
                    return component.DebugSpawnLurker();
                case "Force full moon horde now":
                    return component.DebugForceMoonEvent(false);
                case "Force blood moon horde now":
                    return component.DebugForceMoonEvent(true);
                default:
                    return false;
            }
        }

        private Rect DrawCard(Listing_Standard listing, float height)
        {
            Rect row = listing.GetRect(height);
            SettingsTheme.DrawCard(row);
            return row;
        }

        private float CalculateWrappedTextHeight(string text, float width)
        {
            Text.Font = GameFont.Small;
            return Mathf.Max(Text.LineHeight, Text.CalcHeight(text ?? string.Empty, width));
        }

        private float CalculateStepperCardHeight(Listing_Standard listing, string description)
        {
            float textWidth = Mathf.Max(120f, listing.ColumnWidth - 24f - 236f);
            float descriptionHeight = Mathf.Max(18f, CalculateWrappedTextHeight(description, textWidth));
            return Mathf.Max(110f, 36f + descriptionHeight + 48f);
        }

        private void DrawCardText(Rect row, string label, string description, string valueText = null, float reservedRight = 0f)
        {
            float labelHeight = 22f;
            float descriptionTop = 26f;
            float descriptionBottomPadding = 12f;
            float textWidth = Mathf.Max(120f, row.width - 24f - reservedRight);
            Rect textRect = new Rect(row.x + 12f, row.y + 10f, textWidth, row.height - 20f);
            float descriptionHeight = Mathf.Max(18f, CalculateWrappedTextHeight(description, textRect.width));
            float descriptionAvailableHeight = Mathf.Max(18f, row.yMax - (textRect.y + descriptionTop) - descriptionBottomPadding);

            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, labelHeight), label);
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(new Rect(textRect.x, textRect.y + descriptionTop, textRect.width, Mathf.Min(descriptionHeight + 2f, descriptionAvailableHeight)), description);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(valueText))
            {
                Rect pillRect = new Rect(row.x + row.width - 112f, row.y + 8f, 100f, 24f);
                SettingsTheme.DrawStatusPill(pillRect, true);
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = SettingsTheme.Ink;
                Widgets.Label(pillRect, valueText);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawStepperControls(Rect row, ref int value, int min, int max, int step, string displayText)
        {
            float buttonWidth = 36f;
            float valueWidth = 148f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (SettingsTheme.DrawButton(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            SettingsTheme.DrawValueShell(valueRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? value.ToString() : displayText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (SettingsTheme.DrawButton(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawStepperControls(Rect row, ref float value, float min, float max, float step, string displayText)
        {
            float buttonWidth = 36f;
            float valueWidth = 148f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (SettingsTheme.DrawButton(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            SettingsTheme.DrawValueShell(valueRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? $"{value:0.0}" : displayText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (SettingsTheme.DrawButton(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawPercentControls(Rect row, ref float value, float min, float max, float step, string displayText)
        {
            float buttonWidth = 36f;
            float valueWidth = 148f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (SettingsTheme.DrawButton(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            SettingsTheme.DrawValueShell(valueRect);
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? $"{value * 100f:0}%" : displayText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (SettingsTheme.DrawButton(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawFallbackWindow(Rect inRect)
        {
            ClampAndRepair();
            SettingsTheme.DrawBody(inRect);
            Rect inner = inRect.ContractedBy(12f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inner);

            Text.Font = GameFont.Medium;
            listing.Label("Zed Zed Zed Settings");
            Text.Font = GameFont.Small;
            listing.GapLine();

            listing.Label("Zombie family name");
            zombiePrefix = listing.TextEntry(zombiePrefix ?? "Zombie");
            listing.CheckboxLabeled("Show zombie HUD", ref showZombieCounter, "Shows the active zombie count and danger on your current home map.");
            listing.Label("Danger compares active zombies to your current target population. Daytime target = colonists × outbreak intensity. Night target = daytime target × 1.5.");
            listing.CheckboxLabeled("Enable edge trickle", ref enableEdgeTrickle, "Lets small zombie groups keep wandering in from the map edge.");
            listing.CheckboxLabeled("Enable moon events", ref enableMoonEvents, "Lets full moon and blood moon attacks happen.");
            listing.CheckboxLabeled("Scale zombie population to colonists", ref useColonistScaledPopulation, "Uses your colonist count to scale day, night, full moon, and blood moon pressure.");
            listing.CheckboxLabeled("Enable ground bursts", ref enableGroundBursts, "Lets buried zombie groups erupt from the ground.");
            listing.CheckboxLabeled("Enable grave events", ref enableGraveEvents, "Lets special spawning graves appear as events.");
            listing.CheckboxLabeled("Enable debug controls", ref enableDebugControls, "Shows manual test buttons in this settings window.");
            listing.Gap();
            listing.Label("Variant names");
            listing.Label("Biter");
            biterName = listing.TextEntry(biterName ?? "Biter");
            listing.Label("Runt");
            runtName = listing.TextEntry(runtName ?? "Runt");
            listing.Label("Boomer");
            boomerName = listing.TextEntry(boomerName ?? "Boomer");
            listing.Label("Sick");
            sickName = listing.TextEntry(sickName ?? "Sick");
            listing.Label("Drowned");
            drownedName = listing.TextEntry(drownedName ?? "Drowned");
            listing.Label("Brute");
            bruteName = listing.TextEntry(bruteName ?? "Brute");
            listing.Label("Grabber");
            grabberName = listing.TextEntry(grabberName ?? "Grabber");
            listing.Label("Lurker");
            lurkerName = listing.TextEntry(lurkerName ?? "Lurker");

            listing.GapLine();
            listing.Label($"Outbreak intensity: {outbreakIntensity:0.0}x day, {NighttimeTargetMultiplier:0.0}x night");
            outbreakIntensity = listing.Slider(outbreakIntensity, 0.5f, 12f);
            outbreakIntensity = Mathf.Round(outbreakIntensity * 2f) / 2f;
            listing.Label($"Debug spawn minimum: {minGroupSize}");
            minGroupSize = (int)listing.Slider(minGroupSize, 1, 60);
            listing.Label($"Debug spawn maximum: {maxGroupSize}");
            maxGroupSize = (int)listing.Slider(maxGroupSize, 1, 120);
            listing.Label($"Runner strain chance: {fastZombieChance:P0}");
            fastZombieChance = listing.Slider(fastZombieChance, 0f, 0.20f);
            listing.Label("Reanimation delay: disabled");
            listing.Label($"Days until infection reaches 99% transformation: {infectionDaysToTurn}");
            infectionDaysToTurn = (int)listing.Slider(infectionDaysToTurn, 1, 30);

            listing.GapLine();
            if (listing.ButtonText("Reset to recommended defaults"))
            {
                ResetToRecommendedDefaults();
            }

            listing.End();
            ClampAndRepair();
        }

        private static void ShowDebugResult(bool success, string successText, string failText)
        {
            Messages.Message(success ? successText : failText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
