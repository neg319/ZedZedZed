using UnityEngine;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombiePawnFactory
    {
        [ThreadStatic]
        public static bool SuppressZombieRelationGeneration;

        public static Pawn GenerateZombie(PawnKindDef kind, Faction faction)
        {
            Pawn pawn = null;
            SuppressZombieRelationGeneration = true;
            try
            {
                try
                {
                    pawn = PawnGenerator.GeneratePawn(kind);
                }
                catch
                {
                    try
                    {
                        pawn = PawnGenerator.GeneratePawn(kind);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            finally
            {
                SuppressZombieRelationGeneration = false;
            }

            FinalizeZombie(pawn, initialSpawn: true, desiredFaction: faction);
            return pawn;
        }

        public static void FinalizeZombie(Pawn pawn, bool initialSpawn, Faction desiredFaction = null)
        {
            if (pawn == null)
            {
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRot))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRot);
            }

            if (pawn.story != null)
            {
                ZombieVariant variant = ZombieUtility.GetVariant(pawn);
                ZombieVariant visualVariant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, variant);
                ApplyZombieBackstories(pawn, variant);
                pawn.story.skinColorOverride = ZombieVisualUtility.GetSkinColor(pawn, variant);
                TrySetHairColor(pawn, ZombieVisualUtility.GetHairColor(Color.gray, visualVariant));
                pawn.story.bodyType = ZombieVisualUtility.GetBodyType(variant, pawn, pawn.story.bodyType);
                if (pawn.style != null)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }
            }

            if (desiredFaction != null)
            {
                TryAssignFaction(pawn, desiredFaction);
            }
            else if (ZombieLurkerUtility.IsLurker(pawn))
            {
                ZombieLurkerUtility.ClearFaction(pawn);
            }

            try
            {
                pawn.relations?.ClearAllRelations();
            }
            catch
            {
            }

            ZombieUtility.SetZombieDisplayName(pawn);
            ZombieUtility.StripAllUsableItems(pawn);
            ZombieUtility.TrimZombieApparel(pawn);
            ZombieUtility.MarkZombieApparelTainted(pawn, degradeApparel: initialSpawn);
            ZombieUtility.ApplyLimbDecay(pawn);
            ZombieUtility.ApplyVariantHediffs(pawn);

            if (initialSpawn)
            {
                ZombieUtility.ApplyVisibleDecayInjuries(pawn);
                ZombieUtility.GiveRunnerSpeedIfRolled(pawn);
            }

            ZombieUtility.RefreshDrownedState(pawn);
            if (pawn.needs?.mood != null)
            {
                pawn.needs.mood.CurLevel = 0.05f;
            }

            ZombieUtility.MarkPawnGraphicsDirty(pawn);
        }


        private static void TryAssignFaction(Pawn pawn, Faction faction)
        {
            if (pawn == null || faction == null || pawn.Faction == faction)
            {
                return;
            }

            try
            {
                var setFactionDirect = AccessTools.Method(typeof(Pawn), "SetFactionDirect", new[] { typeof(Faction) });
                if (setFactionDirect != null)
                {
                    setFactionDirect.Invoke(pawn, new object[] { faction });
                    return;
                }
            }
            catch
            {
            }

            try
            {
                var setFaction = AccessTools.Method(typeof(Pawn), "SetFaction", new[] { typeof(Faction), typeof(Pawn) });
                if (setFaction != null)
                {
                    setFaction.Invoke(pawn, new object[] { faction, null });
                }
            }
            catch
            {
            }
        }

        private static void ApplyZombieBackstories(Pawn pawn, ZombieVariant variant)
        {
            if (pawn?.story == null)
            {
                return;
            }

            BackstoryDef childhood = ZombieBackstoryUtility.GetChildhood(variant, pawn);
            BackstoryDef adulthood = ZombieBackstoryUtility.GetAdulthood(variant, pawn);

            TrySetBackstory(pawn.story, "Childhood", childhood);
            TrySetBackstory(pawn.story, "Adulthood", adulthood);
            ForceBackstoryFields(pawn.story, childhood, adulthood);
            ZombieBackstoryUtility.ApplySkillProfile(pawn, variant);
        }

        private static void TrySetBackstory(Pawn_StoryTracker story, string propertyName, BackstoryDef backstory)
        {
            if (story == null || backstory == null)
            {
                return;
            }

            try
            {
                var property = AccessTools.Property(story.GetType(), propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(story, backstory, null);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                var field = AccessTools.Field(story.GetType(), propertyName.ToLowerInvariant());
                field?.SetValue(story, backstory);
            }
            catch
            {
            }
        }


        private static void ForceBackstoryFields(Pawn_StoryTracker story, BackstoryDef childhood, BackstoryDef adulthood)
        {
            if (story == null)
            {
                return;
            }

            try
            {
                AccessTools.Field(story.GetType(), "childhood")?.SetValue(story, childhood);
                AccessTools.Field(story.GetType(), "adulthood")?.SetValue(story, adulthood);
                AccessTools.Field(story.GetType(), "childhoodIdentifier")?.SetValue(story, childhood?.identifier);
                AccessTools.Field(story.GetType(), "adulthoodIdentifier")?.SetValue(story, adulthood?.identifier);
            }
            catch
            {
            }

            try
            {
                var resolve = AccessTools.Method(story.GetType(), "ResolveStoryDisabledWorkTagsFromBackstoriesAndTraits");
                resolve?.Invoke(story, null);
            }
            catch
            {
            }
        }

        private static void TrySetHairColor(Pawn pawn, Color hairColor)
        {
            if (pawn?.story == null)
            {
                return;
            }

            try
            {
                var hairColorProperty = AccessTools.Property(pawn.story.GetType(), "hairColor");
                if (hairColorProperty != null && hairColorProperty.CanWrite)
                {
                    hairColorProperty.SetValue(pawn.story, hairColor, null);
                }
            }
            catch
            {
            }
        }
    }
}
