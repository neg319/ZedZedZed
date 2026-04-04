using System;
using System.Collections.Generic;
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

            switch (archetype)
            {
                case "Builder": SetSkills(pawn, (SkillDefOf.Construction, 9), (SkillDefOf.Crafting, 5), (SkillDefOf.Mining, 3)); break;
                case "Miner": SetSkills(pawn, (SkillDefOf.Mining, 9), (SkillDefOf.Construction, 4), (SkillDefOf.Melee, 3)); break;
                case "Farmer": SetSkills(pawn, (SkillDefOf.Plants, 9), (SkillDefOf.Animals, 5), (SkillDefOf.Cooking, 3)); break;
                case "Doctor": SetSkills(pawn, (SkillDefOf.Medicine, 9), (SkillDefOf.Intellectual, 5), (SkillDefOf.Social, 2)); break;
                case "Warden": SetSkills(pawn, (SkillDefOf.Social, 8), (SkillDefOf.Melee, 5), (SkillDefOf.Shooting, 3)); break;
                case "Slave": SetSkills(pawn, (SkillDefOf.Plants, 5), (SkillDefOf.Mining, 5), (SkillDefOf.Construction, 5), (SkillDefOf.Melee, 2)); break;
                case "Chef": SetSkills(pawn, (SkillDefOf.Cooking, 9), (SkillDefOf.Plants, 4), (SkillDefOf.Crafting, 4)); break;
                case "Scientist": SetSkills(pawn, (SkillDefOf.Intellectual, 9), (SkillDefOf.Medicine, 4), (SkillDefOf.Crafting, 3)); break;
                default: SetSkills(pawn, (SkillDefOf.Animals, 4), (SkillDefOf.Plants, 4), (SkillDefOf.Melee, 3)); break;
            }
        }

        private static void SetSkills(Pawn pawn, params (SkillDef, int)[] pairs)
        {
            foreach (var pair in pairs)
            {
                SkillRecord record = pawn.skills?.GetSkill(pair.Item1);
                if (record != null)
                {
                    record.Level = pair.Item2;
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
                // use thinkData constant hash for stable per pawn assignment without save component
                int index = Math.Abs(existing.GetHashCode()) % LurkerArchetypes.Length;
                return LurkerArchetypes[index];
            }

            return LurkerArchetypes[Math.Abs(existing.GetHashCode()) % LurkerArchetypes.Length];
        }
    }
}
