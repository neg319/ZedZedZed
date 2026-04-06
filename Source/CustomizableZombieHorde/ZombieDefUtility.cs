using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieDefUtility
    {
        private static readonly Dictionary<string, ZombieVariant> KindToVariant = new Dictionary<string, ZombieVariant>
        {
            { "CZH_Zombie_Biter", ZombieVariant.Biter },
            { "CZH_Zombie_Crawler", ZombieVariant.Crawler },
            { "CZH_Zombie_Boomer", ZombieVariant.Boomer },
            { "CZH_Zombie_Sick", ZombieVariant.Sick },
            { "CZH_Zombie_Drowned", ZombieVariant.Drowned },
            { "CZH_Zombie_Tank", ZombieVariant.Tank },
            { "CZH_Zombie_Grabber", ZombieVariant.Grabber },
            { "CZH_Zombie_Lurker", ZombieVariant.Lurker }
        };

        public static void ApplyDynamicLabels()
        {
            string prefix = CleanPrefix(CustomizableZombieHordeMod.Settings?.zombiePrefix ?? "Zombie");
            foreach (var pair in KindToVariant)
            {
                PawnKindDef def = DefDatabase<PawnKindDef>.GetNamedSilentFail(pair.Key);
                if (def != null)
                {
                    def.label = BuildDisplayName(prefix, GetVariantLabel(pair.Value));
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
            if (KindToVariant.TryGetValue(kindDef.defName ?? string.Empty, out ZombieVariant variant))
            {
                return BuildDisplayName(prefix, GetVariantLabel(variant));
            }

            return kindDef.label.NullOrEmpty() ? prefix : kindDef.label.CapitalizeFirst();
        }

        public static string GetVariantLabel(ZombieVariant variant)
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            switch (variant)
            {
                case ZombieVariant.Biter:
                    return CleanVariantName(settings?.biterName, "Biter");
                case ZombieVariant.Crawler:
                    return CleanVariantName(settings?.crawlerName, "Crawler");
                case ZombieVariant.Boomer:
                    return CleanVariantName(settings?.boomerName, "Boomer");
                case ZombieVariant.Sick:
                    return CleanVariantName(settings?.sickName, "Sick");
                case ZombieVariant.Drowned:
                    return CleanVariantName(settings?.drownedName, "Drowned");
                case ZombieVariant.Tank:
                    return CleanVariantName(settings?.heavyName, "Heavy");
                case ZombieVariant.Grabber:
                    return CleanVariantName(settings?.grabberName, "Grabber");
                case ZombieVariant.Lurker:
                    return CleanVariantName(settings?.lurkerName, "Lurker");
                default:
                    return "Biter";
            }
        }



        public static string GetChildhoodBackstoryDefName(ZombieVariant variant)
        {
            return "CZH_BackstoryChild_" + ZombieVariantUtility.GetDefaultVariantLabel(variant).Replace(" ", string.Empty);
        }

        public static string GetAdulthoodBackstoryDefName(ZombieVariant variant)
        {
            return "CZH_BackstoryAdult_" + ZombieVariantUtility.GetDefaultVariantLabel(variant).Replace(" ", string.Empty);
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
            return string.Join(", ",
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Biter)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Crawler)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Boomer)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Sick)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Drowned)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Tank)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Grabber)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Lurker)));
        }

        public static string CleanPrefix(string prefix)
        {
            if (prefix.NullOrEmpty())
            {
                return "Zombie";
            }

            return prefix.Trim();
        }

        private static string CleanVariantName(string value, string fallback)
        {
            if (value.NullOrEmpty())
            {
                return fallback;
            }

            string trimmed = value.Trim();
            return trimmed.NullOrEmpty() ? fallback : trimmed;
        }

        private static string BuildDisplayName(string prefix, string variantLabel)
        {
            string cleanPrefix = CleanPrefix(prefix);
            string cleanVariant = CleanVariantName(variantLabel, "Biter");
            return (cleanPrefix + " " + cleanVariant).Trim();
        }
    }
}
