using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieDefUtility
    {
        private static readonly Dictionary<string, string> Suffixes = new Dictionary<string, string>
        {
            { "CZH_Zombie_Biter", "Biter" },
            { "CZH_Zombie_Crawler", "Crawler" },
            { "CZH_Zombie_Boomer", "Boomer" },
            { "CZH_Zombie_Sick", "Sick" },
            { "CZH_Zombie_Drowned", "Drowned" },
            { "CZH_Zombie_Tank", "Tank" }
        };

        public static void ApplyDynamicLabels()
        {
            string prefix = CleanPrefix(CustomizableZombieHordeMod.Settings?.zombiePrefix ?? "Zombie");
            foreach (var pair in Suffixes)
            {
                PawnKindDef def = DefDatabase<PawnKindDef>.GetNamedSilentFail(pair.Key);
                if (def != null)
                {
                    def.label = (prefix + " " + pair.Value).Trim();
                }
            }

            IncidentDef incident = DefDatabase<IncidentDef>.GetNamedSilentFail("CZH_ZombieHorde");
            if (incident != null)
            {
                incident.label = prefix.ToLowerInvariant() + " horde";
                incident.letterLabel = prefix + " Horde Approaches";
                incident.letterText = "A shambling horde of " + prefix.ToLowerInvariant() + "s is closing in from the edge of the map.";
            }
        }

        public static string ExampleNames(string prefix)
        {
            string clean = CleanPrefix(prefix);
            return $"{clean} Biter, {clean} Crawler, {clean} Boomer, {clean} Sick, {clean} Drowned, {clean} Tank";
        }

        public static string CleanPrefix(string prefix)
        {
            if (prefix.NullOrEmpty())
            {
                return "Zombie";
            }

            return prefix.Trim();
        }
    }
}
