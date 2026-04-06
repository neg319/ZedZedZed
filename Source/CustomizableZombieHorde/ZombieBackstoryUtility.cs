using System;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieBackstoryUtility
    {
        private static readonly string[] LurkerArchetypes =
        {
            "Builder", "Miner", "Farmer", "Doctor", "Warden", "Slave", "Chef", "Scientist"
        };

        public static BackstoryDef GetChildhood(ZombieVariant variant, Pawn pawn)
        {
            string defName = variant == ZombieVariant.Lurker
                ? "CZH_BackstoryChild_Lurker" + GetOrAssignLurkerArchetype(pawn)
                : ZombieDefUtility.GetChildhoodBackstoryDefName(variant);
            return DefDatabase<BackstoryDef>.GetNamedSilentFail(defName);
        }

        public static BackstoryDef GetAdulthood(ZombieVariant variant, Pawn pawn)
        {
            string defName = variant == ZombieVariant.Lurker
                ? "CZH_BackstoryAdult_Lurker" + GetOrAssignLurkerArchetype(pawn)
                : ZombieDefUtility.GetAdulthoodBackstoryDefName(variant);
            return DefDatabase<BackstoryDef>.GetNamedSilentFail(defName);
        }

        public static void ApplySkillProfile(Pawn pawn, ZombieVariant variant)
        {
            if (pawn?.skills == null)
            {
                return;
            }

            if (variant == ZombieVariant.Lurker)
            {
                ApplyLurkerSkills(pawn, GetOrAssignLurkerArchetype(pawn));
                return;
            }

            foreach (SkillRecord skill in pawn.skills.skills)
            {
                skill.Level = GetZombieSkillLevel(variant, skill.def);
                skill.passion = Passion.None;
                skill.xpSinceLastLevel = 0f;
            }
        }

        private static int GetZombieSkillLevel(ZombieVariant variant, SkillDef def)
        {
            if (def == SkillDefOf.Melee)
            {
                switch (variant)
                {
                    case ZombieVariant.Tank: return 8;
                    case ZombieVariant.Biter: return 7;
                    case ZombieVariant.Grabber: return 6;
                    case ZombieVariant.Crawler: return 4;
                    case ZombieVariant.Drowned: return 5;
                    case ZombieVariant.Boomer: return 2;
                    case ZombieVariant.Sick: return 3;
                }
            }

            if (def == SkillDefOf.Shooting)
            {
                return 0;
            }

            if (def == SkillDefOf.Construction || def == SkillDefOf.Mining || def == SkillDefOf.Crafting)
            {
                return variant == ZombieVariant.Tank ? 2 : variant == ZombieVariant.Grabber ? 1 : 0;
            }

            if (def == SkillDefOf.Plants)
            {
                return variant == ZombieVariant.Drowned ? 1 : 0;
            }

            if (def == SkillDefOf.Medicine)
            {
                return variant == ZombieVariant.Sick ? 1 : 0;
            }

            return 0;
        }

        private static void ApplyLurkerSkills(Pawn pawn, string archetype)
        {
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                skill.Level = 0;
                skill.passion = Passion.None;
                skill.xpSinceLastLevel = 0f;
            }

            // Every lurker gets a little low-randomized usefulness and basic self-defense.
            SetSkills(
                pawn,
                (SkillDefOf.Melee, StableRange(pawn, archetype + "_Melee", 1, 4)),
                (SkillDefOf.Shooting, StableRange(pawn, archetype + "_Shooting", 1, 4)),
                (SkillDefOf.Plants, StableRange(pawn, archetype + "_PlantsMinor", 0, 3)),
                (SkillDefOf.Crafting, StableRange(pawn, archetype + "_CraftMinor", 0, 3)),
                (SkillDefOf.Construction, StableRange(pawn, archetype + "_BuildMinor", 0, 3)),
                (SkillDefOf.Mining, StableRange(pawn, archetype + "_MineMinor", 0, 3)),
                (SkillDefOf.Cooking, StableRange(pawn, archetype + "_CookMinor", 0, 3)),
                (SkillDefOf.Animals, StableRange(pawn, archetype + "_AnimalMinor", 0, 3)),
                (SkillDefOf.Medicine, StableRange(pawn, archetype + "_MedicMinor", 0, 3)),
                (SkillDefOf.Intellectual, StableRange(pawn, archetype + "_ThinkMinor", 0, 3)),
                (SkillDefOf.Social, StableRange(pawn, archetype + "_SocialMinor", 0, 3))
            );

            switch (archetype)
            {
                case "Builder":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Construction, StableRange(pawn, "Builder_Construction", 4, 8)),
                        (SkillDefOf.Crafting, StableRange(pawn, "Builder_Crafting", 3, 6)),
                        (SkillDefOf.Mining, StableRange(pawn, "Builder_Mining", 2, 5))
                    );
                    break;
                case "Miner":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Mining, StableRange(pawn, "Miner_Mining", 4, 8)),
                        (SkillDefOf.Construction, StableRange(pawn, "Miner_Construction", 2, 5)),
                        (SkillDefOf.Melee, StableRange(pawn, "Miner_Melee", 2, 5))
                    );
                    break;
                case "Farmer":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Plants, StableRange(pawn, "Farmer_Plants", 4, 8)),
                        (SkillDefOf.Animals, StableRange(pawn, "Farmer_Animals", 2, 5)),
                        (SkillDefOf.Cooking, StableRange(pawn, "Farmer_Cooking", 1, 4))
                    );
                    break;
                case "Doctor":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Medicine, StableRange(pawn, "Doctor_Medicine", 4, 8)),
                        (SkillDefOf.Intellectual, StableRange(pawn, "Doctor_Intellectual", 2, 5)),
                        (SkillDefOf.Social, StableRange(pawn, "Doctor_Social", 1, 4))
                    );
                    break;
                case "Warden":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Social, StableRange(pawn, "Warden_Social", 4, 8)),
                        (SkillDefOf.Melee, StableRange(pawn, "Warden_Melee", 2, 5)),
                        (SkillDefOf.Shooting, StableRange(pawn, "Warden_Shooting", 2, 5))
                    );
                    break;
                case "Slave":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Plants, StableRange(pawn, "Slave_Plants", 3, 6)),
                        (SkillDefOf.Mining, StableRange(pawn, "Slave_Mining", 3, 6)),
                        (SkillDefOf.Construction, StableRange(pawn, "Slave_Construction", 3, 6))
                    );
                    break;
                case "Chef":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Cooking, StableRange(pawn, "Chef_Cooking", 4, 8)),
                        (SkillDefOf.Plants, StableRange(pawn, "Chef_Plants", 2, 5)),
                        (SkillDefOf.Crafting, StableRange(pawn, "Chef_Crafting", 2, 5))
                    );
                    break;
                case "Scientist":
                    SetSkills(
                        pawn,
                        (SkillDefOf.Intellectual, StableRange(pawn, "Scientist_Intellectual", 4, 8)),
                        (SkillDefOf.Medicine, StableRange(pawn, "Scientist_Medicine", 2, 5)),
                        (SkillDefOf.Crafting, StableRange(pawn, "Scientist_Crafting", 1, 4))
                    );
                    break;
                default:
                    SetSkills(
                        pawn,
                        (SkillDefOf.Animals, StableRange(pawn, "Default_Animals", 2, 4)),
                        (SkillDefOf.Plants, StableRange(pawn, "Default_Plants", 2, 4)),
                        (SkillDefOf.Melee, StableRange(pawn, "Default_Melee", 1, 4))
                    );
                    break;
            }
        }

        private static int StableRange(Pawn pawn, string salt, int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }

            string seed = (pawn?.GetUniqueLoadID() ?? "lurker") + "_" + salt;
            int hash = Math.Abs(seed.GetHashCode());
            return minInclusive + (hash % (maxInclusive - minInclusive + 1));
        }

        private static void SetSkills(Pawn pawn, params (SkillDef, int)[] pairs)
        {
            foreach (var pair in pairs)
            {
                SkillRecord record = pawn.skills?.GetSkill(pair.Item1);
                if (record != null)
                {
                    record.Level = Math.Max(record.Level, pair.Item2);
                    record.passion = Passion.None;
                    record.xpSinceLastLevel = 0f;
                }
            }
        }

        private static string GetOrAssignLurkerArchetype(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Builder";
            }

            string existing = pawn.GetUniqueLoadID();
            if (pawn.mindState != null)
            {
                int index = Math.Abs(existing.GetHashCode()) % LurkerArchetypes.Length;
                return LurkerArchetypes[index];
            }

            return LurkerArchetypes[Math.Abs(existing.GetHashCode()) % LurkerArchetypes.Length];
        }
    }
}
