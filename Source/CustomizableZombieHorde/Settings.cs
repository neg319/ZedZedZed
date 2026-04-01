using UnityEngine;
using Verse;
using RimWorld;

namespace CustomizableZombieHorde
{
    public sealed class CustomizableZombieHordeSettings : ModSettings
    {
        private Vector2 scrollPosition;
        private float settingsViewHeight = 2500f;

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
            base.ExposeData();
        }

        public float DifficultyMultiplier => 1f + (Mathf.Max(0, difficultyLevel) * 0.25f);

        public void DoWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 18f, settingsViewHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("Zombie family name prefix");
            zombiePrefix = listing.TextEntry(zombiePrefix ?? "Zombie");
            listing.GapLine();

            listing.Label($"Difficulty level: {difficultyLevel} ({DifficultyMultiplier:0.00}x zombies)");
            difficultyLevel = Mathf.RoundToInt(listing.Slider(difficultyLevel, 0, 8));
            listing.CheckboxLabeled("Show zombie counter on map", ref showZombieCounter, "Shows the number of zombies currently on the viewed player home map.");
            listing.Label("Active zombies are capped by colonist count. Daytime stays lighter, nighttime gets denser, and the cap never rises above 10x your colonists.");
            listing.GapLine();

            listing.CheckboxLabeled("Constant edge trickle", ref enableEdgeTrickle, "Zombies will keep shambling in from the map edges in small groups.");
            listing.Label($"Trickle interval in hours: {trickleIntervalHours:0.0}");
            trickleIntervalHours = listing.Slider(trickleIntervalHours, 0.5f, 24f);
            listing.Label($"Trickle minimum group size: {trickleMinGroupSize}");
            trickleMinGroupSize = Mathf.RoundToInt(listing.Slider(trickleMinGroupSize, 1, 12));
            listing.Label($"Trickle maximum group size: {trickleMaxGroupSize}");
            trickleMaxGroupSize = Mathf.RoundToInt(listing.Slider(trickleMaxGroupSize, 1, 24));
            if (trickleMaxGroupSize < trickleMinGroupSize)
            {
                trickleMaxGroupSize = trickleMinGroupSize;
            }
            listing.GapLine();

            listing.CheckboxLabeled("Full moon and blood moon events", ref enableMoonEvents, "Roughly every 30 days, a larger horde will attack. Rare blood moons send a much larger horde.");
            listing.Label($"Full moon base horde size: {fullMoonBaseCount}");
            fullMoonBaseCount = Mathf.RoundToInt(listing.Slider(fullMoonBaseCount, 6, 80));
            listing.Label($"Blood moon base horde size: {bloodMoonBaseCount}");
            bloodMoonBaseCount = Mathf.RoundToInt(listing.Slider(bloodMoonBaseCount, 12, 140));
            listing.Label($"Blood moon chance per moon: {bloodMoonChance * 100f:0}%");
            bloodMoonChance = listing.Slider(bloodMoonChance, 0.01f, 0.50f);
            listing.GapLine();

            listing.CheckboxLabeled("Ground burst events", ref enableGroundBursts, "Small zombie groups can erupt from the ground inside your base like a twisted infestation.");
            listing.Label($"Ground burst minimum days between events: {groundBurstMinDays:0.0}");
            groundBurstMinDays = listing.Slider(groundBurstMinDays, 1f, 20f);
            listing.Label($"Ground burst maximum days between events: {groundBurstMaxDays:0.0}");
            groundBurstMaxDays = listing.Slider(groundBurstMaxDays, 1f, 30f);
            if (groundBurstMaxDays < groundBurstMinDays)
            {
                groundBurstMaxDays = groundBurstMinDays;
            }
            listing.Label($"Ground burst minimum group size: {groundBurstMinGroupSize}");
            groundBurstMinGroupSize = Mathf.RoundToInt(listing.Slider(groundBurstMinGroupSize, 1, 12));
            listing.Label($"Ground burst maximum group size: {groundBurstMaxGroupSize}");
            groundBurstMaxGroupSize = Mathf.RoundToInt(listing.Slider(groundBurstMaxGroupSize, 1, 18));
            if (groundBurstMaxGroupSize < groundBurstMinGroupSize)
            {
                groundBurstMaxGroupSize = groundBurstMinGroupSize;
            }
            listing.GapLine();

            listing.CheckboxLabeled("Rare zombie grave events", ref enableGraveEvents, "Rare special events can create a zombie grave that keeps spawning one type of zombie until the grave is destroyed.");
            listing.Label($"Grave event minimum days between events: {graveEventMinDays:0.0}");
            graveEventMinDays = listing.Slider(graveEventMinDays, 3f, 30f);
            listing.Label($"Grave event maximum days between events: {graveEventMaxDays:0.0}");
            graveEventMaxDays = listing.Slider(graveEventMaxDays, 3f, 40f);
            if (graveEventMaxDays < graveEventMinDays)
            {
                graveEventMaxDays = graveEventMinDays;
            }
            listing.GapLine();

            listing.Label("Enabled zombie variants");
            listing.CheckboxLabeled("Standard Biters", ref allowBiters);
            listing.CheckboxLabeled("Crawlers", ref allowCrawlers);
            listing.CheckboxLabeled("Boomers", ref allowBoomers);
            listing.CheckboxLabeled("Sick", ref allowSick);
            listing.CheckboxLabeled("Drowned", ref allowDrowned);
            listing.CheckboxLabeled("Heavies", ref allowHeavies);
            listing.CheckboxLabeled("Grabbers", ref allowGrabbers);
            listing.GapLine();

            listing.Label($"Fast zombie chance: {fastZombieChance * 100f:0}%");
            fastZombieChance = listing.Slider(fastZombieChance, 0f, 0.20f);
            listing.Label($"Reanimation delay in hours: {resurrectionDelayHours:0.0}");
            resurrectionDelayHours = listing.Slider(resurrectionDelayHours, 0.5f, 24f);
            listing.GapLine();

            listing.Label($"Manual horde event minimum size: {minGroupSize}");
            minGroupSize = Mathf.RoundToInt(listing.Slider(minGroupSize, 1, 60));
            listing.Label($"Manual horde event maximum size: {maxGroupSize}");
            maxGroupSize = Mathf.RoundToInt(listing.Slider(maxGroupSize, 1, 120));
            if (maxGroupSize < minGroupSize)
            {
                maxGroupSize = minGroupSize;
            }

            if (!allowBiters && !allowCrawlers && !allowBoomers && !allowSick && !allowDrowned && !allowHeavies && !allowGrabbers)
            {
                listing.GapLine();
                listing.Label("Warning: no variants are enabled. Standard Biters will be used as a safe fallback.");
            }

            listing.GapLine();
            listing.Label("Example names: " + ZombieDefUtility.ExampleNames(zombiePrefix));
            listing.GapLine();

            if (listing.ButtonText("Reset settings to recommended defaults"))
            {
                ResetToRecommendedDefaults();
            }
            listing.GapLine();

            listing.CheckboxLabeled("Enable debug controls", ref enableDebugControls, "Shows manual buttons to force zombie waves, moon events, and ground bursts while a colony is loaded.");
            if (enableDebugControls)
            {
                DrawDebugControls(listing);
            }

            listing.End();

            settingsViewHeight = Mathf.Max(2500f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }

        private void DrawDebugControls(Listing_Standard listing)
        {
            listing.GapLine();
            listing.Label("Debug controls");

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            bool canUseDebug = component != null && component.HasUsableDebugMap();
            if (!canUseDebug)
            {
                listing.Label("Load into a colony map to use the debug spawn buttons.");
                return;
            }

            if (listing.ButtonText("Force edge wave now"))
            {
                ShowDebugResult(component.DebugForceEdgeWave(), "Forced an edge wave.", "Could not force an edge wave.");
            }

            if (listing.ButtonText("Force nightly edge wave now"))
            {
                ShowDebugResult(component.DebugForceNightlyWave(), "Forced the nightly edge wave.", "Could not force the nightly edge wave.");
            }

            if (listing.ButtonText("Force huddled pack now"))
            {
                ShowDebugResult(component.DebugForceHuddledPack(), "Forced a huddled pack.", "Could not force a huddled pack.");
            }

            if (listing.ButtonText("Force base push now"))
            {
                ShowDebugResult(component.DebugForceBasePush(), "Forced a base push.", "Could not force a base push.");
            }

            if (listing.ButtonText("Force edge wanderers now"))
            {
                ShowDebugResult(component.DebugForceEdgeWanderers(), "Forced edge wanderers.", "Could not force edge wanderers.");
            }

            if (listing.ButtonText("Force ground burst now"))
            {
                ShowDebugResult(component.DebugForceGroundBurst(), "Forced a ground burst.", "Could not force a ground burst.");
            }

            if (listing.ButtonText("Force random grave event now"))
            {
                ShowDebugResult(component.DebugForceRandomGraveEvent(), "Forced a grave event.", "Could not force a grave event.");
            }

            if (listing.ButtonText("Force biter grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Biter), "Forced a biter grave.", "Could not force a biter grave.");
            }

            if (listing.ButtonText("Force crawler grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Crawler), "Forced a crawler grave.", "Could not force a crawler grave.");
            }

            if (listing.ButtonText("Force boomer grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Boomer), "Forced a boomer grave.", "Could not force a boomer grave.");
            }

            if (listing.ButtonText("Force sick grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Sick), "Forced a sick grave.", "Could not force a sick grave.");
            }

            if (listing.ButtonText("Force drowned grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Drowned), "Forced a drowned grave.", "Could not force a drowned grave.");
            }

            if (listing.ButtonText("Force heavy grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Tank), "Forced a heavy grave.", "Could not force a heavy grave.");
            }

            if (listing.ButtonText("Force grabber grave now"))
            {
                ShowDebugResult(component.DebugForceVariantGraveEvent(ZombieVariant.Grabber), "Forced a grabber grave.", "Could not force a grabber grave.");
            }

            if (listing.ButtonText("Force full moon horde now"))
            {
                ShowDebugResult(component.DebugForceMoonEvent(false), "Forced a full moon horde.", "Could not force a full moon horde.");
            }

            if (listing.ButtonText("Force blood moon horde now"))
            {
                ShowDebugResult(component.DebugForceMoonEvent(true), "Forced a blood moon horde.", "Could not force a blood moon horde.");
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
        }

        private static void ShowDebugResult(bool success, string successText, string failText)
        {
            Messages.Message(success ? successText : failText, success ? MessageTypeDefOf.NeutralEvent : MessageTypeDefOf.RejectInput, false);
        }
    }
}
