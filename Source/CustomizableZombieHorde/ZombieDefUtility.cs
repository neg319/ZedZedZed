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
            { "CZH_Zombie_Runt", ZombieVariant.Runt },
            { "CZH_Zombie_Boomer", ZombieVariant.Boomer },
            { "CZH_Zombie_Sick", ZombieVariant.Sick },
            { "CZH_Zombie_Drowned", ZombieVariant.Drowned },
            { "CZH_Zombie_Brute", ZombieVariant.Brute },
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

            ApplyDynamicGraveLabel("CZH_Grave_Runt", ZombieVariant.Runt);
            ApplyDynamicGraveLabel("CZH_Grave_Brute", ZombieVariant.Brute);

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

        public static string GetDisplayLabelForPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Zombie";
            }

            if (ZombieUtility.IsSkeletonBiter(pawn) || ZombieUtility.ShouldSpawnAsSkeletonBiter(pawn))
            {
                return GetBoneBiterLabel();
            }

            if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_PregnantBoomer) == true)
            {
                return GetPregnantBoomerLabel();
            }

            return GetDisplayLabelForKind(pawn.kindDef);
        }

        public static string GetVariantLabel(ZombieVariant variant)
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            switch (variant)
            {
                case ZombieVariant.Biter:
                    return CleanVariantName(settings?.biterName, "Biter");
                case ZombieVariant.Runt:
                    return CleanVariantName(settings?.runtName, "Runt");
                case ZombieVariant.Boomer:
                    return CleanVariantName(settings?.boomerName, "Boomer");
                case ZombieVariant.Sick:
                    return CleanVariantName(settings?.sickName, "Sick");
                case ZombieVariant.Drowned:
                    return CleanVariantName(settings?.drownedName, "Drowned");
                case ZombieVariant.Brute:
                    return CleanVariantName(settings?.bruteName, "Brute");
                case ZombieVariant.Grabber:
                    return CleanVariantName(settings?.grabberName, "Grabber");
                case ZombieVariant.Lurker:
                    return CleanVariantName(settings?.lurkerName, "Lurker");
                default:
                    return "Biter";
            }
        }


        public static string GetBoneBiterLabel()
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            return CleanVariantName(settings?.boneBiterName, "Bone Biter");
        }

        public static string GetPregnantBoomerLabel()
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            return CleanVariantName(settings?.pregnantBoomerName, "Pregnant Boomer");
        }

        public static string GetChildhoodBackstoryDefName(ZombieVariant variant)
        {
            switch (variant)
            {
                case ZombieVariant.Runt:
                    return "CZH_BackstoryChild_Runt";
                case ZombieVariant.Brute:
                    return "CZH_BackstoryChild_Brute";
                default:
                    return "CZH_BackstoryChild_" + ZombieVariantUtility.GetDefaultVariantLabel(variant).Replace(" ", string.Empty);
            }
        }

        public static string GetAdulthoodBackstoryDefName(ZombieVariant variant)
        {
            switch (variant)
            {
                case ZombieVariant.Runt:
                    return "CZH_BackstoryAdult_Runt";
                case ZombieVariant.Brute:
                    return "CZH_BackstoryAdult_Brute";
                default:
                    return "CZH_BackstoryAdult_" + ZombieVariantUtility.GetDefaultVariantLabel(variant).Replace(" ", string.Empty);
            }
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
                BuildDisplayName(clean, GetBoneBiterLabel()),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Runt)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Boomer)),
                BuildDisplayName(clean, GetPregnantBoomerLabel()),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Sick)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Drowned)),
                BuildDisplayName(clean, GetVariantLabel(ZombieVariant.Brute)),
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

        private static void ApplyDynamicGraveLabel(string defName, ZombieVariant variant)
        {
            ThingDef grave = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (grave == null)
            {
                return;
            }

            string variantLabel = GetVariantLabel(variant);
            string lower = variantLabel.ToLowerInvariant();
            grave.label = lower + " grave";
            grave.description = "A collapsed grave that spits " + lower + "s out through the dirt.";

            if (variant == ZombieVariant.Brute)
            {
                grave.description = "A heaving grave that keeps forcing " + lower + " zombies up from the dirt.";
            }
        }

        private static string BuildDisplayName(string prefix, string variantLabel)
        {
            string cleanPrefix = CleanPrefix(prefix);
            string cleanVariant = CleanVariantName(variantLabel, "Biter");
            return (cleanPrefix + " " + cleanVariant).Trim();
        }
    }
}
