using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class CustomizableZombieHordeSettings : ModSettings
    {
        private Vector2 scrollPosition;
        private float settingsViewHeight = 1280f;

        public string zombiePrefix = "Zombie";
        public int minGroupSize = 5;
        public int maxGroupSize = 12;
        public float fastZombieChance = 0.04f;
        public float resurrectionDelayHours = 5f;

        public bool enableEdgeTrickle = true;
        public float trickleIntervalHours = 2.5f;
        public int trickleMinGroupSize = 1;
        public int trickleMaxGroupSize = 2;

        public int difficultyLevel = 0;

        public bool enableGroundBursts = true;
        public float groundBurstMinDays = 6f;
        public float groundBurstMaxDays = 12f;
        public int groundBurstMinGroupSize = 2;
        public int groundBurstMaxGroupSize = 5;

        public bool enableMoonEvents = true;
        public float bloodMoonChance = 0.12f;
        public int fullMoonBaseCount = 18;
        public int bloodMoonBaseCount = 40;

        public bool showZombieCounter = true;

        public bool allowBiters = true;
        public bool allowCrawlers = true;
        public bool allowBoomers = true;
        public bool allowSick = true;
        public bool allowDrowned = true;
        public bool allowTanks = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref zombiePrefix, "zombiePrefix", "Zombie");
            Scribe_Values.Look(ref minGroupSize, "minGroupSize", 5);
            Scribe_Values.Look(ref maxGroupSize, "maxGroupSize", 12);
            Scribe_Values.Look(ref fastZombieChance, "fastZombieChance", 0.04f);
            Scribe_Values.Look(ref resurrectionDelayHours, "resurrectionDelayHours", 5f);

            Scribe_Values.Look(ref enableEdgeTrickle, "enableEdgeTrickle", true);
            Scribe_Values.Look(ref trickleIntervalHours, "trickleIntervalHours", 2.5f);
            Scribe_Values.Look(ref trickleMinGroupSize, "trickleMinGroupSize", 1);
            Scribe_Values.Look(ref trickleMaxGroupSize, "trickleMaxGroupSize", 2);

            Scribe_Values.Look(ref difficultyLevel, "difficultyLevel", 0);

            Scribe_Values.Look(ref enableGroundBursts, "enableGroundBursts", true);
            Scribe_Values.Look(ref groundBurstMinDays, "groundBurstMinDays", 6f);
            Scribe_Values.Look(ref groundBurstMaxDays, "groundBurstMaxDays", 12f);
            Scribe_Values.Look(ref groundBurstMinGroupSize, "groundBurstMinGroupSize", 2);
            Scribe_Values.Look(ref groundBurstMaxGroupSize, "groundBurstMaxGroupSize", 5);

            Scribe_Values.Look(ref enableMoonEvents, "enableMoonEvents", true);
            Scribe_Values.Look(ref bloodMoonChance, "bloodMoonChance", 0.12f);
            Scribe_Values.Look(ref fullMoonBaseCount, "fullMoonBaseCount", 18);
            Scribe_Values.Look(ref bloodMoonBaseCount, "bloodMoonBaseCount", 40);

            Scribe_Values.Look(ref showZombieCounter, "showZombieCounter", true);

            Scribe_Values.Look(ref allowBiters, "allowBiters", true);
            Scribe_Values.Look(ref allowCrawlers, "allowCrawlers", true);
            Scribe_Values.Look(ref allowBoomers, "allowBoomers", true);
            Scribe_Values.Look(ref allowSick, "allowSick", true);
            Scribe_Values.Look(ref allowDrowned, "allowDrowned", true);
            Scribe_Values.Look(ref allowTanks, "allowTanks", true);
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

            listing.Label("Enabled zombie variants");
            listing.CheckboxLabeled("Standard Biters", ref allowBiters);
            listing.CheckboxLabeled("Crawlers", ref allowCrawlers);
            listing.CheckboxLabeled("Boomers", ref allowBoomers);
            listing.CheckboxLabeled("Sick", ref allowSick);
            listing.CheckboxLabeled("Drowned", ref allowDrowned);
            listing.CheckboxLabeled("Tanks", ref allowTanks);
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

            if (!allowBiters && !allowCrawlers && !allowBoomers && !allowSick && !allowDrowned && !allowTanks)
            {
                listing.GapLine();
                listing.Label("Warning: no variants are enabled. Standard Biters will be used as a safe fallback.");
            }

            listing.GapLine();
            listing.Label("Example names: " + ZombieDefUtility.ExampleNames(zombiePrefix));
            listing.End();

            settingsViewHeight = Mathf.Max(1280f, listing.CurHeight + 24f);
            Widgets.EndScrollView();
        }
    }
}
