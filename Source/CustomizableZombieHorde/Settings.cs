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
            Advanced,
            Debug
        }

        private SettingsTab selectedTab = SettingsTab.Overview;

        private Vector2 overviewScrollPosition;
        private Vector2 eventsScrollPosition;
        private Vector2 variantsScrollPosition;
        private Vector2 advancedScrollPosition;
        private Vector2 debugScrollPosition;

        private float overviewViewHeight = 1100f;
        private float eventsViewHeight = 1500f;
        private float variantsViewHeight = 1500f;
        private float advancedViewHeight = 1200f;
        private float debugViewHeight = 1400f;

        public string zombiePrefix = "Zombie";
        public int minGroupSize = 3;
        public int maxGroupSize = 7;
        public float fastZombieChance = 0.02f;
        public float resurrectionDelayHours = 5f;

        public bool enableEdgeTrickle = true;
        public float trickleIntervalHours = 2.75f;
        public int trickleMinGroupSize = 1;
        public int trickleMaxGroupSize = 2;

        public int difficultyLevel = 1;

        public bool enableGroundBursts = true;
        public float groundBurstMinDays = 5f;
        public float groundBurstMaxDays = 10f;
        public int groundBurstMinGroupSize = 2;
        public int groundBurstMaxGroupSize = 4;

        public bool enableGraveEvents = true;
        public float graveEventMinDays = 8f;
        public float graveEventMaxDays = 16f;

        public bool enableMoonEvents = true;
        public float bloodMoonChance = 0.10f;
        public int fullMoonBaseCount = 12;
        public int bloodMoonBaseCount = 24;

        public bool showZombieCounter = true;
        public bool enableDebugControls = false;

        public bool allowBiters = true;
        public bool allowCrawlers = true;
        public bool allowBoomers = true;
        public bool allowSick = true;
        public bool allowDrowned = true;
        public bool allowHeavies = true;
        public bool allowGrabbers = true;

        public string biterName = "Biter";
        public string crawlerName = "Crawler";
        public string boomerName = "Boomer";
        public string sickName = "Sick";
        public string drownedName = "Drowned";
        public string heavyName = "Heavy";
        public string grabberName = "Grabber";
        public string lurkerName = "Lurker";

        public override void ExposeData()
        {
            Scribe_Values.Look(ref zombiePrefix, "zombiePrefix", "Zombie");
            Scribe_Values.Look(ref minGroupSize, "minGroupSize", 3);
            Scribe_Values.Look(ref maxGroupSize, "maxGroupSize", 7);
            Scribe_Values.Look(ref fastZombieChance, "fastZombieChance", 0.02f);
            Scribe_Values.Look(ref resurrectionDelayHours, "resurrectionDelayHours", 5f);

            Scribe_Values.Look(ref enableEdgeTrickle, "enableEdgeTrickle", true);
            Scribe_Values.Look(ref trickleIntervalHours, "trickleIntervalHours", 2.75f);
            Scribe_Values.Look(ref trickleMinGroupSize, "trickleMinGroupSize", 1);
            Scribe_Values.Look(ref trickleMaxGroupSize, "trickleMaxGroupSize", 2);

            Scribe_Values.Look(ref difficultyLevel, "difficultyLevel", 1);

            Scribe_Values.Look(ref enableGroundBursts, "enableGroundBursts", true);
            Scribe_Values.Look(ref groundBurstMinDays, "groundBurstMinDays", 5f);
            Scribe_Values.Look(ref groundBurstMaxDays, "groundBurstMaxDays", 10f);
            Scribe_Values.Look(ref groundBurstMinGroupSize, "groundBurstMinGroupSize", 2);
            Scribe_Values.Look(ref groundBurstMaxGroupSize, "groundBurstMaxGroupSize", 4);

            Scribe_Values.Look(ref enableGraveEvents, "enableGraveEvents", true);
            Scribe_Values.Look(ref graveEventMinDays, "graveEventMinDays", 8f);
            Scribe_Values.Look(ref graveEventMaxDays, "graveEventMaxDays", 16f);

            Scribe_Values.Look(ref enableMoonEvents, "enableMoonEvents", true);
            Scribe_Values.Look(ref bloodMoonChance, "bloodMoonChance", 0.10f);
            Scribe_Values.Look(ref fullMoonBaseCount, "fullMoonBaseCount", 12);
            Scribe_Values.Look(ref bloodMoonBaseCount, "bloodMoonBaseCount", 24);

            Scribe_Values.Look(ref showZombieCounter, "showZombieCounter", true);
            Scribe_Values.Look(ref enableDebugControls, "enableDebugControls", false);

            Scribe_Values.Look(ref allowBiters, "allowBiters", true);
            Scribe_Values.Look(ref allowCrawlers, "allowCrawlers", true);
            Scribe_Values.Look(ref allowBoomers, "allowBoomers", true);
            Scribe_Values.Look(ref allowSick, "allowSick", true);
            Scribe_Values.Look(ref allowDrowned, "allowDrowned", true);
            Scribe_Values.Look(ref allowHeavies, "allowHeavies", true);
            Scribe_Values.Look(ref allowGrabbers, "allowGrabbers", true);

            Scribe_Values.Look(ref biterName, "biterName", "Biter");
            Scribe_Values.Look(ref crawlerName, "crawlerName", "Crawler");
            Scribe_Values.Look(ref boomerName, "boomerName", "Boomer");
            Scribe_Values.Look(ref sickName, "sickName", "Sick");
            Scribe_Values.Look(ref drownedName, "drownedName", "Drowned");
            Scribe_Values.Look(ref heavyName, "heavyName", "Heavy");
            Scribe_Values.Look(ref grabberName, "grabberName", "Grabber");
            Scribe_Values.Look(ref lurkerName, "lurkerName", "Lurker");

            base.ExposeData();
            ClampAndRepair();
        }

        public float DifficultyMultiplier => 1f + (Mathf.Max(0, difficultyLevel) * 0.25f);

        public void DoWindowContents(Rect inRect)
        {
            try
            {
                ClampAndRepair();

                Rect headerRect = new Rect(inRect.x, inRect.y, inRect.width, 82f);
                Rect tabsRect = new Rect(inRect.x, headerRect.yMax + 8f, inRect.width, 34f);
                Rect bodyRect = new Rect(inRect.x, tabsRect.yMax + 8f, inRect.width, inRect.height - (tabsRect.yMax - inRect.y) - 8f);

                DrawHeader(headerRect);
                DrawTabButtons(tabsRect);

                Widgets.DrawMenuSection(bodyRect);
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

            enableEdgeTrickle = true;
            trickleIntervalHours = 2.75f;
            trickleMinGroupSize = 1;
            trickleMaxGroupSize = 2;

            difficultyLevel = 1;

            enableGroundBursts = true;
            groundBurstMinDays = 5f;
            groundBurstMaxDays = 10f;
            groundBurstMinGroupSize = 2;
            groundBurstMaxGroupSize = 4;

            enableGraveEvents = true;
            graveEventMinDays = 8f;
            graveEventMaxDays = 16f;

            enableMoonEvents = true;
            bloodMoonChance = 0.10f;
            fullMoonBaseCount = 12;
            bloodMoonBaseCount = 24;

            showZombieCounter = true;
            enableDebugControls = false;

            allowBiters = true;
            allowCrawlers = true;
            allowBoomers = true;
            allowSick = true;
            allowDrowned = true;
            allowHeavies = true;
            allowGrabbers = true;

            biterName = "Biter";
            crawlerName = "Crawler";
            boomerName = "Boomer";
            sickName = "Sick";
            drownedName = "Drowned";
            heavyName = "Heavy";
            grabberName = "Grabber";
            lurkerName = "Lurker";
        }

        private void ApplyCasualPreset()
        {
            ResetToRecommendedDefaults();
            difficultyLevel = 0;
            trickleIntervalHours = 4f;
            trickleMinGroupSize = 1;
            trickleMaxGroupSize = 1;
            fullMoonBaseCount = 8;
            bloodMoonBaseCount = 16;
            bloodMoonChance = 0.05f;
            groundBurstMinDays = 8f;
            groundBurstMaxDays = 14f;
            graveEventMinDays = 12f;
            graveEventMaxDays = 20f;
            fastZombieChance = 0.01f;
        }

        private void ApplyApocalypsePreset()
        {
            ResetToRecommendedDefaults();
            difficultyLevel = 4;
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
            crawlerName = NormalizeVariantName(crawlerName, "Crawler");
            boomerName = NormalizeVariantName(boomerName, "Boomer");
            sickName = NormalizeVariantName(sickName, "Sick");
            drownedName = NormalizeVariantName(drownedName, "Drowned");
            heavyName = NormalizeVariantName(heavyName, "Heavy");
            grabberName = NormalizeVariantName(grabberName, "Grabber");
            lurkerName = NormalizeVariantName(lurkerName, "Lurker");
            difficultyLevel = Mathf.Clamp(difficultyLevel, 0, 8);

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

            bloodMoonChance = Mathf.Clamp(bloodMoonChance, 0.01f, 0.50f);
            fastZombieChance = Mathf.Clamp(fastZombieChance, 0f, 0.20f);
            resurrectionDelayHours = Mathf.Clamp(resurrectionDelayHours, 0.5f, 24f);

            groundBurstMinDays = Mathf.Clamp(groundBurstMinDays, 1f, 20f);
            groundBurstMaxDays = Mathf.Clamp(groundBurstMaxDays, groundBurstMinDays, 30f);
            groundBurstMinGroupSize = Mathf.Clamp(groundBurstMinGroupSize, 1, 12);
            groundBurstMaxGroupSize = Mathf.Clamp(groundBurstMaxGroupSize, groundBurstMinGroupSize, 18);

            graveEventMinDays = Mathf.Clamp(graveEventMinDays, 3f, 30f);
            graveEventMaxDays = Mathf.Clamp(graveEventMaxDays, graveEventMinDays, 40f);
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

        private void DrawHeader(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            Rect titleRect = new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 28f);
            Rect subtitleRect = new Rect(rect.x + 12f, rect.y + 36f, rect.width - 24f, 22f);
            Rect hintRect = new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, 18f);

            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Zed Zed Zed Settings");
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(subtitleRect, "A cleaner control panel for outbreak pacing, variants, colony tools, and debug actions.");
            Widgets.Label(hintRect, "Use the tabs below. Most values now use step buttons instead of long sliders.");
            GUI.color = Color.white;
        }

        private void DrawTabButtons(Rect rect)
        {
            string[] labels = { "Overview", "Events", "Variants", "Advanced", "Debug" };
            SettingsTab[] tabs =
            {
                SettingsTab.Overview,
                SettingsTab.Events,
                SettingsTab.Variants,
                SettingsTab.Advanced,
                SettingsTab.Debug
            };

            float gap = 6f;
            float width = (rect.width - (gap * (labels.Length - 1))) / labels.Length;

            for (int i = 0; i < labels.Length; i++)
            {
                Rect buttonRect = new Rect(rect.x + (i * (width + gap)), rect.y, width, rect.height);
                bool active = selectedTab == tabs[i];
                Color old = GUI.color;
                GUI.color = active ? new Color(0.82f, 0.88f, 0.72f) : Color.white;
                if (Widgets.ButtonText(buttonRect, labels[i]))
                {
                    selectedTab = tabs[i];
                }
                GUI.color = old;
            }
        }

        private void DrawOverviewTab(Rect rect)
        {
            BeginScrollableListing(rect, ref overviewScrollPosition, ref overviewViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "What this tab does", "Overview is the quick setup tab. Use it for presets, the family name, the on-map counter, overall difficulty, and the broad pacing values that shape the mod before you dig into the detailed tabs.");
            DrawSectionLabel(listing, "Quick setup", "Start here if you just want the mod to feel good without hand tuning every system.");
            DrawPresetButtons(listing);
            DrawTextEntryCard(listing, "Family name", "This changes undead naming in pawn labels, letters, and other player-facing text.", ref zombiePrefix, "Zombie");
            DrawToggleCard(listing, "Show map counter", "Shows the number of active undead on the viewed home map.", ref showZombieCounter);
            DrawDifficultyCard(listing);
            DrawInfoCard(listing, "Example names", ZombieDefUtility.ExampleNames(zombiePrefix));

            DrawSectionLabel(listing, "At a glance", "These values shape the broad feel of the outbreak before you get into the detailed tabs.");
            DrawIntStepperCard(listing, "Manual horde minimum size", "Used by manual horde debug spawns and related force-spawn actions.", ref minGroupSize, 1, 60, 1);
            DrawIntStepperCard(listing, "Manual horde maximum size", "Used by manual horde debug spawns and related force-spawn actions.", ref maxGroupSize, minGroupSize, 120, 1);
            DrawPercentStepperCard(listing, "Runner strain chance", "Chance for a newly created corpse to roll the fast strain.", ref fastZombieChance, 0f, 0.20f, 0.01f);
            DrawFloatStepperCard(listing, "Reanimation delay", "How long an intact corpse stays down before it can rise again.", ref resurrectionDelayHours, 0.5f, 24f, 0.5f, "hours");

            listing.End();
            overviewViewHeight = Mathf.Max(1100f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawEventsTab(Rect rect)
        {
            BeginScrollableListing(rect, ref eventsScrollPosition, ref eventsViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "What this tab does", "Events controls when pressure arrives and what form it takes. Use this tab to tune the steady edge trickle, moon attacks, underground bursts, and grave incidents so your colony gets the kind of pacing you want.");
            DrawSectionLabel(listing, "Constant edge trickle", "Small groups entering from the edge keep pressure on the colony between major events.");
            DrawToggleCard(listing, "Enable constant edge trickle", "Undead keep shambling in from the map edges in small groups.", ref enableEdgeTrickle);
            DrawFloatStepperCard(listing, "Trickle interval", "Lower values mean small groups arrive more often.", ref trickleIntervalHours, 0.5f, 24f, 0.25f, "hours");
            DrawIntStepperCard(listing, "Trickle minimum group size", "Smallest group the edge trickle can send.", ref trickleMinGroupSize, 1, 12, 1);
            DrawIntStepperCard(listing, "Trickle maximum group size", "Largest group the edge trickle can send.", ref trickleMaxGroupSize, trickleMinGroupSize, 24, 1);

            DrawSectionLabel(listing, "Moon events", "Large attacks tied to the lunar cycle. Blood moons are rarer but much more dangerous.");
            DrawToggleCard(listing, "Enable full moon and blood moon events", "Roughly every 30 days, a larger horde attacks. Rare blood moons send a much larger horde.", ref enableMoonEvents);
            DrawIntStepperCard(listing, "Full moon base horde size", "Base group count before other scaling is applied.", ref fullMoonBaseCount, 6, 80, 2);
            DrawIntStepperCard(listing, "Blood moon base horde size", "Base group count before other scaling is applied.", ref bloodMoonBaseCount, fullMoonBaseCount, 140, 2);
            DrawPercentStepperCard(listing, "Blood moon chance", "Chance that a full moon escalates into a blood moon.", ref bloodMoonChance, 0.01f, 0.50f, 0.01f);

            DrawSectionLabel(listing, "Ground bursts", "Infestation-style eruptions that can surface inside your base.");
            DrawToggleCard(listing, "Enable ground bursts", "Small undead groups can erupt from the ground inside your colony.", ref enableGroundBursts);
            DrawFloatStepperCard(listing, "Minimum days between bursts", "Shortest possible gap between ground burst events.", ref groundBurstMinDays, 1f, 20f, 0.5f, "days");
            DrawFloatStepperCard(listing, "Maximum days between bursts", "Longest possible gap between ground burst events.", ref groundBurstMaxDays, groundBurstMinDays, 30f, 0.5f, "days");
            DrawIntStepperCard(listing, "Ground burst minimum group size", "Smallest underground group.", ref groundBurstMinGroupSize, 1, 12, 1);
            DrawIntStepperCard(listing, "Ground burst maximum group size", "Largest underground group.", ref groundBurstMaxGroupSize, groundBurstMinGroupSize, 18, 1);

            DrawSectionLabel(listing, "Grave events", "Rare special events that create a spawning grave until the colony destroys it.");
            DrawToggleCard(listing, "Enable grave events", "Rare special events can create a variant grave that keeps spawning more bodies.", ref enableGraveEvents);
            DrawFloatStepperCard(listing, "Minimum days between grave events", "Shortest possible gap between grave events.", ref graveEventMinDays, 3f, 30f, 0.5f, "days");
            DrawFloatStepperCard(listing, "Maximum days between grave events", "Longest possible gap between grave events.", ref graveEventMaxDays, graveEventMinDays, 40f, 0.5f, "days");

            listing.End();
            eventsViewHeight = Mathf.Max(1500f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawVariantsTab(Rect rect)
        {
            BeginScrollableListing(rect, ref variantsScrollPosition, ref variantsViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "What this tab does", "Variants lets you decide which special strains are allowed to appear and what they are called in game. Turn off types you do not want in the pool, or rename them so the mod matches the theme and tone of your playthrough.");
            DrawSectionLabel(listing, "Variant roster", "Turn individual strains on or off. Disabled strains will not appear in standard random waves or grave events.");
            DrawVariantCard(listing, "Standard Biters", "Baseline shamblers. The common core of most packs and hordes.", ref allowBiters);
            DrawVariantCard(listing, "Crawlers", "Slow dragging corpses that pressure choke points and add creep factor.", ref allowCrawlers);
            DrawVariantCard(listing, "Boomers", "Unstable corpses that can burst in acid and punish tight formations.", ref allowBoomers);
            DrawVariantCard(listing, "Sick", "Disease-spreading corpses that contaminate blood trails and can infect colonists.", ref allowSick);
            DrawVariantCard(listing, "Drowned", "Water-adapted corpses that are strongest near rivers, marshes, and shorelines.", ref allowDrowned);
            DrawVariantCard(listing, "Heavies", "Large resilient corpses that soak damage and hit harder in melee.", ref allowHeavies);
            DrawVariantCard(listing, "Grabbers", "Corpse grapplers that pin colonists in place until they break free.", ref allowGrabbers);

            if (!allowBiters && !allowCrawlers && !allowBoomers && !allowSick && !allowDrowned && !allowHeavies && !allowGrabbers)
            {
                DrawWarningCard(listing, "No variants are enabled. Standard Biters will be used as a safe fallback.");
            }

            DrawSectionLabel(listing, "Custom variant names", "These names replace the built in variant titles in pawn labels, letters, grave warnings, and other player facing text.");
            DrawTextEntryCard(listing, "Biter name", "The name used for the baseline shambler strain.", ref biterName, "Biter");
            DrawTextEntryCard(listing, "Crawler name", "The name used for the dragging low to the ground strain.", ref crawlerName, "Crawler");
            DrawTextEntryCard(listing, "Boomer name", "The name used for the unstable bursting strain.", ref boomerName, "Boomer");
            DrawTextEntryCard(listing, "Sick name", "The name used for the plague spreading strain.", ref sickName, "Sick");
            DrawTextEntryCard(listing, "Drowned name", "The name used for the blue waterlogged strain.", ref drownedName, "Drowned");
            DrawTextEntryCard(listing, "Heavy name", "The name used for the large tank like strain.", ref heavyName, "Heavy");
            DrawTextEntryCard(listing, "Grabber name", "The name used for the grappling strain.", ref grabberName, "Grabber");
            DrawTextEntryCard(listing, "Lurker name", "The name used for the passive tameable strain.", ref lurkerName, "Lurker");
            DrawInfoCard(listing, "Example names", ZombieDefUtility.ExampleNames(zombiePrefix));

            DrawSectionLabel(listing, "Recommended setup", "Most players will want every strain enabled. Turn off a strain only if you dislike its gameplay role.");
            if (DrawActionCard(listing, "Enable every strain", "Restores the full special infected roster with one click."))
            {
                allowBiters = true;
                allowCrawlers = true;
                allowBoomers = true;
                allowSick = true;
                allowDrowned = true;
                allowHeavies = true;
                allowGrabbers = true;
            }

            listing.End();
            variantsViewHeight = Mathf.Max(1500f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawAdvancedTab(Rect rect)
        {
            BeginScrollableListing(rect, ref advancedScrollPosition, ref advancedViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "What this tab does", "Advanced is for deeper tuning after the main setup feels right. Use it to fine tune population pressure, reanimation timing, manual event sizes, and other values that are most useful when you are balancing the mod for your preferred difficulty.");
            DrawSectionLabel(listing, "Population tuning", "Fine control for players who want to tune pacing instead of using presets.");
            DrawDifficultyCard(listing);
            DrawPercentStepperCard(listing, "Runner strain chance", "Chance for a newly created corpse to roll the fast strain.", ref fastZombieChance, 0f, 0.20f, 0.01f);
            DrawFloatStepperCard(listing, "Reanimation delay", "How long an intact corpse stays down before it can rise again.", ref resurrectionDelayHours, 0.5f, 24f, 0.5f, "hours");

            DrawSectionLabel(listing, "Manual controls and safety", "These values matter most when you are forcing events, testing balance, or comparing settings.");
            DrawIntStepperCard(listing, "Manual horde minimum size", "Used by manual horde and force-spawn actions.", ref minGroupSize, 1, 60, 1);
            DrawIntStepperCard(listing, "Manual horde maximum size", "Used by manual horde and force-spawn actions.", ref maxGroupSize, minGroupSize, 120, 1);
            DrawToggleCard(listing, "Show map counter", "Shows the number of active undead currently on the viewed player home map.", ref showZombieCounter);

            DrawSectionLabel(listing, "Reset", "Use this if the tuning gets messy and you want to return to a known good baseline.");
            if (DrawActionCard(listing, "Reset settings to recommended defaults", "Restores the recommended values across every tab."))
            {
                ResetToRecommendedDefaults();
            }

            listing.End();
            advancedViewHeight = Mathf.Max(1200f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawDebugTab(Rect rect)
        {
            BeginScrollableListing(rect, ref debugScrollPosition, ref debugViewHeight, out Listing_Standard listing, out Rect viewRect);

            DrawInfoCard(listing, "What this tab does", "Debug gives you manual control over the mod for testing. Use it to force specific events, spawn edge waves, trigger moon attacks, or create a lurker on demand while you are verifying behavior in a live colony.");
            DrawSectionLabel(listing, "Debug mode", "Use this tab for testing and screenshots. Leave it off during normal play.");
            DrawToggleCard(listing, "Enable debug controls", "Shows manual buttons to force waves, moon events, and burst events while a colony is loaded.", ref enableDebugControls);

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            bool canUseDebug = enableDebugControls && component != null && component.HasUsableDebugMap();

            if (!enableDebugControls)
            {
                DrawInfoCard(listing, "Debug controls are currently hidden.", "Enable debug controls above to reveal the manual spawn actions.");
            }
            else if (!canUseDebug)
            {
                DrawInfoCard(listing, "Load a colony map first.", "The debug spawn buttons only work while a valid colony map is open.");
            }
            else
            {
                DrawSectionLabel(listing, "Manual event buttons", "These actions fire immediately on the current colony map.");
                DrawDebugActionButton(listing, component, "Force edge wave now", "Forced an edge wave.", "Could not force an edge wave.");
                DrawDebugActionButton(listing, component, "Force nightly edge wave now", "Forced the nightly edge wave.", "Could not force the nightly edge wave.");
                DrawDebugActionButton(listing, component, "Force huddled pack now", "Forced a huddled pack.", "Could not force a huddled pack.");
                DrawDebugActionButton(listing, component, "Force base push now", "Forced a base push.", "Could not force a base push.");
                DrawDebugActionButton(listing, component, "Force edge wanderers now", "Forced edge wanderers.", "Could not force edge wanderers.");
                DrawDebugActionButton(listing, component, "Force ground burst now", "Forced a ground burst.", "Could not force a ground burst.");
                DrawDebugActionButton(listing, component, "Force random grave event now", "Forced a grave event.", "Could not force a grave event.");
                DrawDebugActionButton(listing, component, "Force biter grave now", "Forced a biter grave.", "Could not force a biter grave.");
                DrawDebugActionButton(listing, component, "Force crawler grave now", "Forced a crawler grave.", "Could not force a crawler grave.");
                DrawDebugActionButton(listing, component, "Force boomer grave now", "Forced a boomer grave.", "Could not force a boomer grave.");
                DrawDebugActionButton(listing, component, "Force sick grave now", "Forced a sick grave.", "Could not force a sick grave.");
                DrawDebugActionButton(listing, component, "Force drowned grave now", "Forced a drowned grave.", "Could not force a drowned grave.");
                DrawDebugActionButton(listing, component, "Force heavy grave now", "Forced a heavy grave.", "Could not force a heavy grave.");
                DrawDebugActionButton(listing, component, "Spawn lurker now", "Spawned a lurker.", "Could not spawn a lurker.");
                DrawDebugActionButton(listing, component, "Force grabber grave now", "Forced a grabber grave.", "Could not force a grabber grave.");
                DrawDebugActionButton(listing, component, "Force full moon horde now", "Forced a full moon horde.", "Could not force a full moon horde.");
                DrawDebugActionButton(listing, component, "Force blood moon horde now", "Forced a blood moon horde.", "Could not force a blood moon horde.");
            }

            listing.End();
            debugViewHeight = Mathf.Max(1400f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
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
            Rect rect = listing.GetRect(50f);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 26f), title);
            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(rect.x, rect.y + 26f, rect.width, 22f), description);
            GUI.color = Color.white;
            listing.Gap(6f);
        }

        private void DrawPresetButtons(Listing_Standard listing)
        {
            Rect row = listing.GetRect(70f);
            float gap = 8f;
            float width = (row.width - (gap * 2f)) / 3f;

            if (Widgets.ButtonText(new Rect(row.x, row.y, width, 32f), "Casual"))
            {
                ApplyCasualPreset();
            }

            if (Widgets.ButtonText(new Rect(row.x + width + gap, row.y, width, 32f), "Recommended"))
            {
                ResetToRecommendedDefaults();
            }

            if (Widgets.ButtonText(new Rect(row.x + ((width + gap) * 2f), row.y, width, 32f), "Apocalypse"))
            {
                ApplyApocalypsePreset();
            }

            GUI.color = Color.gray;
            Widgets.Label(new Rect(row.x, row.y + 38f, row.width, 24f), "Casual lowers pressure. Recommended restores the intended baseline. Apocalypse pushes the mod toward late-game chaos.");
            GUI.color = Color.white;
            listing.Gap(8f);
        }

        private void DrawTextEntryCard(Listing_Standard listing, string label, string description, ref string value, string fallback)
        {
            Rect row = DrawCard(listing, 108f);
            Rect labelRect = new Rect(row.x + 12f, row.y + 8f, row.width - 24f, 22f);
            Rect descRect = new Rect(row.x + 12f, row.y + 30f, row.width - 24f, 36f);
            Rect textRect = new Rect(row.x + 12f, row.y + 70f, row.width - 24f, 28f);

            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(labelRect, label);
            GUI.color = Color.gray;
            Widgets.Label(descRect, description);
            GUI.color = Color.white;

            value = Widgets.TextField(textRect, value ?? fallback);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = fallback;
            }

            TooltipHandler.TipRegion(row, description);
        }

        private void DrawToggleCard(Listing_Standard listing, string label, string description, ref bool value)
        {
            Rect row = DrawCard(listing, 58f);
            Rect checkboxRect = new Rect(row.x + 12f, row.y + 12f, 24f, 24f);
            Widgets.Checkbox(checkboxRect.position, ref value, 24f);
            DrawCardText(new Rect(row.x + 40f, row.y, row.width - 52f, row.height), label, description);
            TooltipHandler.TipRegion(row, description);
        }

        private void DrawDifficultyCard(Listing_Standard listing)
        {
            Rect row = DrawCard(listing, 96f);
            string description = "Global population multiplier for major spawns. Higher values mean more bodies in serious events.";
            DrawCardText(row, "Difficulty", description);
            DrawStepperControls(row, ref difficultyLevel, 0, 8, 1, $"Level {difficultyLevel}  |  {DifficultyMultiplier:0.00}x");
        }

        private void DrawIntStepperCard(Listing_Standard listing, string label, string description, ref int value, int min, int max, int step)
        {
            Rect row = DrawCard(listing, 96f);
            DrawCardText(row, label, description);
            DrawStepperControls(row, ref value, min, max, step, value.ToString());
        }

        private void DrawFloatStepperCard(Listing_Standard listing, string label, string description, ref float value, float min, float max, float step, string suffix)
        {
            Rect row = DrawCard(listing, 96f);
            DrawCardText(row, label, description);
            DrawStepperControls(row, ref value, min, max, step, $"{value:0.0} {suffix}".Trim());
        }

        private void DrawPercentStepperCard(Listing_Standard listing, string label, string description, ref float value, float min, float max, float step)
        {
            Rect row = DrawCard(listing, 96f);
            DrawCardText(row, label, description);
            DrawPercentControls(row, ref value, min, max, step, $"{value * 100f:0}%");
        }

        private void DrawVariantCard(Listing_Standard listing, string label, string description, ref bool value)
        {
            Rect row = DrawCard(listing, 58f);
            Rect checkboxRect = new Rect(row.x + 12f, row.y + 12f, 24f, 24f);
            Widgets.Checkbox(checkboxRect.position, ref value, 24f);
            DrawCardText(new Rect(row.x + 40f, row.y, row.width - 52f, row.height), label, description, value ? "Enabled" : "Disabled");
            TooltipHandler.TipRegion(row, description);
        }

        private bool DrawActionCard(Listing_Standard listing, string label, string description)
        {
            Rect row = DrawCard(listing, 86f);
            DrawCardText(row, label, description);
            Rect buttonRect = new Rect(row.x + row.width - 164f, row.y + 22f, 152f, 32f);
            return Widgets.ButtonText(buttonRect, "Apply");
        }

        private void DrawInfoCard(Listing_Standard listing, string label, string description)
        {
            Rect row = DrawCard(listing, 84f);
            DrawCardText(row, label, description);
        }

        private void DrawWarningCard(Listing_Standard listing, string description)
        {
            Rect row = DrawCard(listing, 56f);
            Color old = GUI.color;
            GUI.color = new Color(1f, 0.93f, 0.7f);
            Widgets.DrawMenuSection(row);
            GUI.color = old;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(row.x + 12f, row.y + 10f, row.width - 24f, 36f), description);
        }

        private void DrawDebugActionButton(Listing_Standard listing, ZombieGameComponent component, string label, string successText, string failText)
        {
            Rect row = DrawCard(listing, 54f);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(row.x + 12f, row.y + 16f, row.width - 184f, 24f), label);
            Rect buttonRect = new Rect(row.x + row.width - 160f, row.y + 11f, 148f, 32f);
            if (Widgets.ButtonText(buttonRect, "Run now"))
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
                case "Force ground burst now":
                    return component.DebugForceGroundBurst();
                case "Force random grave event now":
                    return component.DebugForceRandomGraveEvent();
                case "Force biter grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Biter);
                case "Force crawler grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Crawler);
                case "Force boomer grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Boomer);
                case "Force sick grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Sick);
                case "Force drowned grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Drowned);
                case "Force heavy grave now":
                    return component.DebugForceVariantGraveEvent(ZombieVariant.Tank);
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
            Widgets.DrawMenuSection(row);
            return row;
        }

        private void DrawCardText(Rect row, string label, string description, string valueText = null)
        {
            float reservedRight = string.IsNullOrEmpty(valueText) ? 196f : 228f;
            Rect textRect = new Rect(row.x + 12f, row.y + 8f, Mathf.Max(120f, row.width - reservedRight), row.height - 16f);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, 22f), label);
            GUI.color = Color.gray;
            Widgets.Label(new Rect(textRect.x, textRect.y + 20f, textRect.width, Mathf.Max(18f, textRect.height - 20f)), description);
            GUI.color = Color.white;

            if (!string.IsNullOrEmpty(valueText))
            {
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(row.x + row.width - 220f, row.y + 8f, 96f, 22f), valueText);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawStepperControls(Rect row, ref int value, int min, int max, int step, string displayText)
        {
            float buttonWidth = 34f;
            float valueWidth = 140f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (Widgets.ButtonText(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            Widgets.DrawBoxSolid(valueRect, new Color(0.16f, 0.16f, 0.16f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? value.ToString() : displayText);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawStepperControls(Rect row, ref float value, float min, float max, float step, string displayText)
        {
            float buttonWidth = 34f;
            float valueWidth = 140f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (Widgets.ButtonText(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            Widgets.DrawBoxSolid(valueRect, new Color(0.16f, 0.16f, 0.16f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? $"{value:0.0}" : displayText);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawPercentControls(Rect row, ref float value, float min, float max, float step, string displayText)
        {
            float buttonWidth = 34f;
            float valueWidth = 140f;
            float totalWidth = buttonWidth + 8f + valueWidth + 8f + buttonWidth;
            float controlsY = row.y + row.height - 42f;
            Rect minusRect = new Rect(row.x + row.width - totalWidth - 12f, controlsY, buttonWidth, 30f);
            Rect valueRect = new Rect(minusRect.xMax + 8f, controlsY, valueWidth, 30f);
            Rect plusRect = new Rect(valueRect.xMax + 8f, controlsY, buttonWidth, 30f);

            if (Widgets.ButtonText(minusRect, "-"))
            {
                value = Mathf.Max(min, value - step);
            }

            Widgets.DrawBoxSolid(valueRect, new Color(0.16f, 0.16f, 0.16f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(valueRect, string.IsNullOrEmpty(displayText) ? $"{value * 100f:0}%" : displayText);
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(plusRect, "+"))
            {
                value = Mathf.Min(max, value + step);
            }
        }

        private void DrawFallbackWindow(Rect inRect)
        {
            ClampAndRepair();
            Widgets.DrawMenuSection(inRect);
            Rect inner = inRect.ContractedBy(12f);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inner);

            Text.Font = GameFont.Medium;
            listing.Label("Zed Zed Zed Settings");
            Text.Font = GameFont.Small;
            listing.GapLine();

            listing.Label("Family name prefix");
            zombiePrefix = listing.TextEntry(zombiePrefix ?? "Zombie");
            listing.CheckboxLabeled("Show zombie counter", ref showZombieCounter, "Show the current undead count on player home maps.");
            listing.CheckboxLabeled("Enable edge trickle", ref enableEdgeTrickle, "Allow small groups to keep wandering in from the map edge.");
            listing.CheckboxLabeled("Enable moon events", ref enableMoonEvents, "Enable full moon and blood moon attacks.");
            listing.CheckboxLabeled("Enable ground bursts", ref enableGroundBursts, "Allow buried groups to erupt from the ground.");
            listing.CheckboxLabeled("Enable grave events", ref enableGraveEvents, "Allow special grave structures to spawn as incidents.");
            listing.CheckboxLabeled("Enable debug controls", ref enableDebugControls, "Show manual debug spawn tools in this settings window.");
            listing.Gap();
            listing.Label("Variant names");
            listing.Label("Biter");
            biterName = listing.TextEntry(biterName ?? "Biter");
            listing.Label("Crawler");
            crawlerName = listing.TextEntry(crawlerName ?? "Crawler");
            listing.Label("Boomer");
            boomerName = listing.TextEntry(boomerName ?? "Boomer");
            listing.Label("Sick");
            sickName = listing.TextEntry(sickName ?? "Sick");
            listing.Label("Drowned");
            drownedName = listing.TextEntry(drownedName ?? "Drowned");
            listing.Label("Heavy");
            heavyName = listing.TextEntry(heavyName ?? "Heavy");
            listing.Label("Grabber");
            grabberName = listing.TextEntry(grabberName ?? "Grabber");
            listing.Label("Lurker");
            lurkerName = listing.TextEntry(lurkerName ?? "Lurker");

            listing.GapLine();
            listing.Label($"Difficulty: {difficultyLevel}");
            difficultyLevel = (int)listing.Slider(difficultyLevel, 0, 8);
            listing.Label($"Min group size: {minGroupSize}");
            minGroupSize = (int)listing.Slider(minGroupSize, 1, 60);
            listing.Label($"Max group size: {maxGroupSize}");
            maxGroupSize = (int)listing.Slider(maxGroupSize, 1, 120);
            listing.Label($"Fast strain chance: {fastZombieChance:P0}");
            fastZombieChance = listing.Slider(fastZombieChance, 0f, 0.20f);
            listing.Label($"Reanimation delay (hours): {resurrectionDelayHours:0.0}");
            resurrectionDelayHours = listing.Slider(resurrectionDelayHours, 0.5f, 24f);

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
