using System;
using System.Reflection;
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
            return GenerateZombie(kind, faction, initialSpawn: true);
        }

        private static Pawn GenerateZombie(PawnKindDef kind, Faction faction, bool initialSpawn)
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

            FinalizeZombie(pawn, initialSpawn: initialSpawn, desiredFaction: faction);
            return pawn;
        }

        public static void FinalizeZombie(Pawn pawn, bool initialSpawn, Faction desiredFaction = null)
        {
            ConvertExistingPawnToZombie(pawn, pawn?.kindDef, desiredFaction, preserveName: false, preserveSkills: false, preserveRelations: false, initialSpawn: initialSpawn);
        }


        public static bool TrySpawnReanimatedZombieFromCorpse(Corpse corpse, out Pawn newPawn)
        {
            newPawn = null;
            Pawn sourcePawn = corpse?.InnerPawn;
            if (sourcePawn == null)
            {
                return false;
            }

            if (ZombieUtility.IsVariant(sourcePawn, ZombieVariant.Boomer))
            {
                return false;
            }

            ZombieVariant variant = ZombieUtility.GetVariant(sourcePawn);
            PawnKindDef kind = ZombieKindSelector.GetKindForVariant(variant, corpse.MapHeld)
                ?? sourcePawn.kindDef
                ?? ZombieKindSelector.GetKindForVariant(ZombieVariant.Biter, corpse.MapHeld);
            Faction desiredFaction = ZombieLurkerUtility.IsPassiveLurker(sourcePawn)
                ? null
                : (ZombieUtility.IsPlayerAlignedZombie(sourcePawn) ? Faction.OfPlayer : ZombieFactionUtility.GetOrCreateZombieFaction());
            bool preserveName = ZombieUtility.IsPlayerAlignedZombie(sourcePawn) || ZombieLurkerUtility.IsLurker(sourcePawn);
            bool preserveSkills = ZombieLurkerUtility.IsColonyLurker(sourcePawn);
            bool preserveRelations = ZombieLurkerUtility.IsColonyLurker(sourcePawn);
            return TrySpawnReanimatedPawnFromCorpse(corpse, kind, desiredFaction, preserveName, preserveSkills, preserveRelations, out newPawn);
        }

        public static bool TrySpawnReanimatedPawnFromCorpse(Corpse corpse, PawnKindDef kindDef, Faction desiredFaction, bool preserveName, bool preserveSkills, bool preserveRelations, out Pawn newPawn)
        {
            newPawn = null;
            Pawn sourcePawn = corpse?.InnerPawn;
            Map map = corpse?.MapHeld;
            if (sourcePawn == null || map == null || kindDef == null)
            {
                return false;
            }

            Faction generationFaction = desiredFaction ?? ZombieFactionUtility.GetOrCreateZombieFaction();
            Pawn generatedPawn = null;
            try
            {
                generatedPawn = GenerateZombie(kindDef, generationFaction, initialSpawn: false);
                if (generatedPawn == null)
                {
                    return false;
                }

                CopyReanimatedIdentity(sourcePawn, generatedPawn, preserveName, preserveSkills, preserveRelations);
                if (desiredFaction == null && ZombieLurkerUtility.IsLurker(generatedPawn))
                {
                    ZombieLurkerUtility.ClearFaction(generatedPawn);
                }

                IntVec3 spawnCell = corpse.PositionHeld;
                if (!spawnCell.IsValid || !spawnCell.InBounds(map))
                {
                    spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 8);
                }

                GenSpawn.Spawn(generatedPawn, spawnCell, map, WipeMode.Vanish);
                Current.Game?.GetComponent<ZombieGameComponent>()?.RegisterBehavior(generatedPawn, ZombieSpawnEventType.AssaultBase);
                ZombieUtility.PrepareZombieForReanimation(generatedPawn);
                ZombieUtility.PrepareSpawnedZombie(generatedPawn);
                if (preserveName && sourcePawn.Name != null)
                {
                    TrySetPawnName(generatedPawn, sourcePawn.Name);
                }
                ZombieUtility.RefreshDrownedState(generatedPawn);

                if (ZombieLurkerUtility.IsPassiveLurker(generatedPawn))
                {
                    generatedPawn.jobs?.StopAll();
                    ZombieLurkerUtility.EnsurePassiveLurkerBehavior(generatedPawn);
                }
                else if (ZombieLurkerUtility.IsColonyLurker(generatedPawn))
                {
                    ZombieLurkerUtility.EnsureColonyLurkerState(generatedPawn, emergencyStabilize: true, stopCurrentJobs: true);
                }
                else
                {
                    ZombieUtility.AssignInitialShambleJob(generatedPawn);
                    ZombieUtility.EnsureZombieAggression(generatedPawn);
                }

                ZombieUtility.MarkPawnGraphicsDirty(generatedPawn);
                corpse.Destroy(DestroyMode.Vanish);
                newPawn = generatedPawn;
                return true;
            }
            catch
            {
                try
                {
                    generatedPawn?.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }

                return false;
            }
        }

        public static void ConvertExistingPawnToZombie(Pawn pawn, PawnKindDef newKind, Faction desiredFaction, bool preserveName, bool preserveSkills, bool preserveRelations, bool initialSpawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (newKind != null)
            {
                TrySetKindDef(pawn, newKind);
            }

            if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRot))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRot);
            }

            ZombieInfectionUtility.ApplyReanimatedState(pawn);

            ZombieVariant variant = ZombieUtility.GetVariant(pawn);
            TryApplyNaturalPregnantBoomerState(pawn, variant, initialSpawn);
            if (variant == ZombieVariant.Runt)
            {
                ZombieRuntUtility.ApplyRuntAgeProfile(pawn);
            }

            if (pawn.story != null)
            {
                ZombieVariant visualVariant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, variant);
                ApplyZombieBackstories(pawn, variant, preserveSkills);
                pawn.story.skinColorOverride = ZombieVisualUtility.GetSkinColor(pawn, variant);
                TrySetHairColor(pawn, ZombieVisualUtility.GetHairColor(Color.gray, visualVariant));
                pawn.story.bodyType = ZombieVisualUtility.GetBodyType(variant, pawn, pawn.story.bodyType);
                if (pawn.style != null)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }

                if (variant == ZombieVariant.Runt)
                {
                    TrySetHairDef(pawn, "Bald");
                }
                else if (ZombieUtility.ShouldSpawnAsSkeletonBiter(pawn))
                {
                    TrySetHairDef(pawn, "Shaved");
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

            if (!preserveRelations)
            {
                try
                {
                    pawn.relations?.ClearAllRelations();
                }
                catch
                {
                }
            }

            if (!preserveName)
            {
                ZombieUtility.SetZombieDisplayName(pawn);
            }

            ZombieUtility.StripAllUsableItems(pawn);
            ZombieUtility.TrimZombieApparel(pawn);
            ZombieUtility.MarkZombieApparelTainted(pawn, degradeApparel: initialSpawn);
            ZombieUtility.ApplyLimbDecay(pawn);
            ZombieUtility.ApplyVariantHediffs(pawn);
            ZombieUtility.ApplyRuntLegDamage(pawn);
            ApplyZombieXenotype(pawn);

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

            ZombieLurkerUtility.EnsureEmotionlessLurker(pawn);
            ZombieUtility.MarkPawnGraphicsDirty(pawn);
        }


        private static void TryApplyNaturalPregnantBoomerState(Pawn pawn, ZombieVariant variant, bool initialSpawn)
        {
            if (!initialSpawn || pawn?.health?.hediffSet == null || variant != ZombieVariant.Boomer || pawn.gender != Gender.Female)
            {
                return;
            }

            if (ZombieDefOf.CZH_PregnantBoomer == null || pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_PregnantBoomer))
            {
                return;
            }

            if (!Rand.Chance(0.10f))
            {
                return;
            }

            try
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_PregnantBoomer);
            }
            catch
            {
            }
        }



        private static void CopyReanimatedIdentity(Pawn source, Pawn destination, bool preserveName, bool preserveSkills, bool preserveRelations)
        {
            if (source == null || destination == null)
            {
                return;
            }

            if (preserveName && source.Name != null)
            {
                TrySetPawnName(destination, source.Name);
            }

            TrySetGender(destination, source.gender);
            CopyAge(source, destination);
            CopyStoryIdentity(source, destination);
            if (preserveSkills)
            {
                CopySkills(source, destination);
            }

            if (preserveRelations)
            {
                CopyRelations(source, destination);
            }
        }

        private static void CopyStoryIdentity(Pawn source, Pawn destination)
        {
            if (source?.story == null || destination?.story == null)
            {
                return;
            }

            try
            {
                destination.story.skinColorOverride = source.story.skinColorOverride;
                destination.story.bodyType = source.story.bodyType;
            }
            catch
            {
            }

            CopyObjectMember(source.story, destination.story, "melanin");
            CopyObjectMember(source.story, destination.story, "hairDef");
            CopyObjectMember(source.story, destination.story, "hairColor");
            CopyObjectMember(source.story, destination.story, "headType");
            CopyObjectMember(source.story, destination.story, "crownType");
            CopyObjectMember(source.story, destination.story, "favoriteColor");
        }

        private static void CopyAge(Pawn source, Pawn destination)
        {
            if (source?.ageTracker == null || destination?.ageTracker == null)
            {
                return;
            }

            CopyObjectMember(source.ageTracker, destination.ageTracker, "ageBiologicalTicksInt");
            CopyObjectMember(source.ageTracker, destination.ageTracker, "ageChronologicalTicksInt");
            CopyObjectMember(source.ageTracker, destination.ageTracker, "birthAbsTicksInt");
        }


        private static void CopyRelations(Pawn source, Pawn destination)
        {
            if (source?.relations == null || destination?.relations == null)
            {
                return;
            }

            System.Collections.IEnumerable directRelations = null;
            try
            {
                directRelations = GetObjectMember(source.relations, "DirectRelations") as System.Collections.IEnumerable;
            }
            catch
            {
            }

            if (directRelations == null)
            {
                directRelations = GetObjectMember(source.relations, "directRelations") as System.Collections.IEnumerable;
            }

            if (directRelations == null)
            {
                return;
            }

            MethodInfo addDirectRelation = AccessTools.Method(source.relations.GetType(), "AddDirectRelation");
            if (addDirectRelation == null)
            {
                return;
            }

            foreach (object directRelation in directRelations)
            {
                if (directRelation == null)
                {
                    continue;
                }

                object relationDef = GetObjectMember(directRelation, "def");
                Pawn otherPawn = GetObjectMember(directRelation, "otherPawn") as Pawn;
                if (relationDef == null || otherPawn == null || otherPawn == destination)
                {
                    continue;
                }

                try
                {
                    addDirectRelation.Invoke(destination.relations, new object[] { relationDef, otherPawn });
                }
                catch
                {
                }
            }
        }

        private static void CopySkills(Pawn source, Pawn destination)
        {
            if (source?.skills?.skills == null || destination?.skills?.skills == null)
            {
                return;
            }

            foreach (SkillRecord sourceSkill in source.skills.skills)
            {
                if (sourceSkill?.def == null)
                {
                    continue;
                }

                for (int i = 0; i < destination.skills.skills.Count; i++)
                {
                    SkillRecord destinationSkill = destination.skills.skills[i];
                    if (destinationSkill?.def != sourceSkill.def)
                    {
                        continue;
                    }

                    SetObjectMember(destinationSkill, "levelInt", sourceSkill.Level);
                    SetObjectMember(destinationSkill, "passion", sourceSkill.passion);
                    SetObjectMember(destinationSkill, "xpSinceLastLevel", GetObjectMember(sourceSkill, "xpSinceLastLevel"));
                    SetObjectMember(destinationSkill, "xpSinceMidnight", GetObjectMember(sourceSkill, "xpSinceMidnight"));
                    SetObjectMember(destinationSkill, "totallyDisabled", GetObjectMember(sourceSkill, "totallyDisabled"));
                    break;
                }
            }
        }

        private static void TrySetGender(Pawn pawn, Gender gender)
        {
            if (pawn == null)
            {
                return;
            }

            SetObjectMember(pawn, "gender", gender);
            SetObjectMember(pawn, "genderInt", gender);
        }

        private static void CopyObjectMember(object source, object destination, string memberName)
        {
            object value = GetObjectMember(source, memberName);
            if (value != null)
            {
                SetObjectMember(destination, memberName, value);
            }
        }

        private static object GetObjectMember(object source, string memberName)
        {
            if (source == null || memberName.NullOrEmpty())
            {
                return null;
            }

            try
            {
                PropertyInfo property = AccessTools.Property(source.GetType(), memberName);
                if (property != null)
                {
                    return property.GetValue(source, null);
                }
            }
            catch
            {
            }

            try
            {
                FieldInfo field = AccessTools.Field(source.GetType(), memberName);
                if (field != null)
                {
                    return field.GetValue(source);
                }
            }
            catch
            {
            }

            return null;
        }

        private static void SetObjectMember(object destination, string memberName, object value)
        {
            if (destination == null || memberName.NullOrEmpty())
            {
                return;
            }

            try
            {
                PropertyInfo property = AccessTools.Property(destination.GetType(), memberName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(destination, value, null);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                FieldInfo field = AccessTools.Field(destination.GetType(), memberName);
                field?.SetValue(destination, value);
            }
            catch
            {
            }
        }

        public static void TrySetPawnName(Pawn pawn, Name name)
        {
            if (pawn == null || name == null)
            {
                return;
            }

            try
            {
                Traverse.Create(pawn).Property("Name").SetValue(name);
                return;
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn).Field("nameInt").SetValue(name);
            }
            catch
            {
            }
        }

        private static void TrySetKindDef(Pawn pawn, PawnKindDef kindDef)
        {
            if (pawn == null || kindDef == null)
            {
                return;
            }

            try
            {
                Traverse.Create(pawn).Field("kindDef").SetValue(kindDef);
            }
            catch
            {
            }

            try
            {
                Traverse.Create(pawn).Field("kindDefInt").SetValue(kindDef);
            }
            catch
            {
            }
        }

        private static void ApplyZombieXenotype(Pawn pawn)
        {
            if (pawn?.genes == null)
            {
                return;
            }

            string xenotypeName = ZombieUtility.IsSkeletonBiter(pawn) || ZombieUtility.ShouldSpawnAsSkeletonBiter(pawn)
                ? "Waster"
                : "Baseliner";
            XenotypeDef xenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail(xenotypeName);
            if (xenotype == null)
            {
                return;
            }

            try
            {
                MethodInfo setXenotype = AccessTools.Method(pawn.genes.GetType(), "SetXenotype", new[] { typeof(XenotypeDef) })
                    ?? AccessTools.Method(pawn.genes.GetType(), "SetXenotypeDirect", new[] { typeof(XenotypeDef) });
                if (setXenotype != null)
                {
                    setXenotype.Invoke(pawn.genes, new object[] { xenotype });
                    return;
                }
            }
            catch
            {
            }

            try
            {
                PropertyInfo xenotypeProperty = AccessTools.Property(pawn.genes.GetType(), "Xenotype");
                if (xenotypeProperty != null && xenotypeProperty.CanWrite)
                {
                    xenotypeProperty.SetValue(pawn.genes, xenotype, null);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                FieldInfo xenotypeField = AccessTools.Field(pawn.genes.GetType(), "xenotype")
                    ?? AccessTools.Field(pawn.genes.GetType(), "xenotypeDef");
                xenotypeField?.SetValue(pawn.genes, xenotype);
            }
            catch
            {
            }
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

        private static void ApplyZombieBackstories(Pawn pawn, ZombieVariant variant, bool preserveSkills)
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
            if (!preserveSkills)
            {
                ZombieBackstoryUtility.ApplySkillProfile(pawn, variant);
            }
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

        private static void TrySetHairDef(Pawn pawn, string hairDefName)
        {
            if (pawn?.story == null || hairDefName.NullOrEmpty())
            {
                return;
            }

            HairDef hairDef = DefDatabase<HairDef>.GetNamedSilentFail(hairDefName);
            if (hairDef == null)
            {
                return;
            }

            try
            {
                var hairDefProperty = AccessTools.Property(pawn.story.GetType(), "hairDef");
                if (hairDefProperty != null && hairDefProperty.CanWrite)
                {
                    hairDefProperty.SetValue(pawn.story, hairDef, null);
                    return;
                }
            }
            catch
            {
            }

            try
            {
                AccessTools.Field(pawn.story.GetType(), "hairDef")?.SetValue(pawn.story, hairDef);
            }
            catch
            {
            }
        }
    }
}
