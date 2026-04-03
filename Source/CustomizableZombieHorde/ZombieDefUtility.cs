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
            { "CZH_Zombie_Grabber", "Grabber" },
            { "CZH_Zombie_Lurker", "Lurker" }
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
            return ZombieVariantUtility.GetVariantLabel(variant);
        }



        public static string GetChildhoodBackstoryDefName(ZombieVariant variant)
        {
            return "CZH_BackstoryChild_" + GetVariantLabel(variant).Replace(" ", string.Empty);
        }

        public static string GetAdulthoodBackstoryDefName(ZombieVariant variant)
        {
            return "CZH_BackstoryAdult_" + GetVariantLabel(variant).Replace(" ", string.Empty);
        }

        public static string GetGraveLetterLabel(ZombieVariant variant)
        {
            return GetVariantLabel(variant) + " Grave Awakened";
        }

        public static string GetGraveLetterText(ZombieVariant variant)
        {
            string type = GetVariantLabel(variant).ToLowerInvariant();
            return "The ground has split open and a " + type + " grave has burst up like an infestation. Destroy the grave quickly or more " + type + " corpses will keep clawing their way out.";
        }

        public static string ExampleNames(string prefix)
        {
            string clean = CleanPrefix(prefix);
            return $"{clean} Biter, {clean} Crawler, {clean} Boomer, {clean} Sick, {clean} Drowned, {clean} Heavy, {clean} Lurker";
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
