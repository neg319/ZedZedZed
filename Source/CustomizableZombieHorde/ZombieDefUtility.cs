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
            { "CZH_Zombie_Tank", "Heavy" },
            { "CZH_Zombie_Grabber", "Grabber" }
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

        public static string GetDisplayLabelForKind(PawnKindDef kindDef)
        {
            if (kindDef == null)
            {
                return "Zombie";
            }

            string prefix = CleanPrefix(CustomizableZombieHordeMod.Settings?.zombiePrefix ?? "Zombie");
            if (Suffixes.TryGetValue(kindDef.defName ?? string.Empty, out string suffix))
            {
                return (prefix + " " + suffix).Trim();
            }

            return kindDef.label.NullOrEmpty() ? prefix : kindDef.label.CapitalizeFirst();
        }

        public static string GetVariantLabel(ZombieVariant variant)
        {
            switch (variant)
            {
                case ZombieVariant.Crawler:
                    return "Crawler";
                case ZombieVariant.Boomer:
                    return "Boomer";
                case ZombieVariant.Sick:
                    return "Sick";
                case ZombieVariant.Drowned:
                    return "Drowned";
                case ZombieVariant.Tank:
                    return "Heavy";
                case ZombieVariant.Grabber:
                    return "Grabber";
                default:
                    return "Biter";
            }
        }

        public static string GetGraveLetterLabel(ZombieVariant variant)
        {
            return GetVariantLabel(variant) + " Grave Awakened";
        }

        public static string GetGraveLetterText(ZombieVariant variant)
        {
            string type = GetVariantLabel(variant).ToLowerInvariant();
            return "The ground has split open and a " + type + " grave has burst up like an infestation. Destroy the grave or more " + type + " zombies will keep crawling out of it.";
        }

        public static string ExampleNames(string prefix)
        {
            string clean = CleanPrefix(prefix);
            return $"{clean} Biter, {clean} Crawler, {clean} Boomer, {clean} Sick, {clean} Drowned, {clean} Heavy";
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
