using System;
using System.Linq;
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

        [ThreadStatic]
        public static bool SuppressAutoFinalizePatch;

        public static Pawn GenerateZombie(PawnKindDef kind, Faction faction)
        {
            return GenerateZombie(kind, faction, initialSpawn: true);
        }

        private static Pawn GenerateZombie(PawnKindDef kind, Faction faction, bool initialSpawn)
        {
            Pawn pawn = null;
            bool previousSuppressRelations = SuppressZombieRelationGeneration;
            bool previousSuppressAutoFinalize = SuppressAutoFinalizePatch;
            SuppressZombieRelationGeneration = true;
            SuppressAutoFinalizePatch = true;
            try
            {
                try
                {
                    pawn = GeneratePawnWithBestAvailableOverload(kind, faction);
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
                SuppressZombieRelationGeneration = previousSuppressRelations;
                SuppressAutoFinalizePatch = previousSuppressAutoFinalize;
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
                if (Prefs.DevMode)
                {
                    Log.Warning("[Zed Zed Zed] Reanimation aborted because the corpse, source pawn, map, or pawn kind was missing.");
                }

                return false;
            }

            if (TrySpawnFreshReanimatedPawnFromCorpse(corpse, kindDef, desiredFaction, preserveName, preserveSkills, preserveRelations, out newPawn))
            {
                return true;
            }

            if (TryReanimateExistingPawnFromCorpse(corpse, kindDef, desiredFaction, preserveName, preserveSkills, preserveRelations, out newPawn))
            {
                return true;
            }

            if (Prefs.DevMode)
            {
                Log.Warning("[Zed Zed Zed] Reanimation failed after both the fresh spawn path and the direct resurrection path were attempted for " + sourcePawn.LabelCap + ".");
            }

            return false;
        }

        private static bool TrySpawnFreshReanimatedPawnFromCorpse(Corpse corpse, PawnKindDef kindDef, Faction desiredFaction, bool preserveName, bool preserveSkills, bool preserveRelations, out Pawn newPawn)
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
            IntVec3 originalCorpseCell = corpse.PositionHeld;
            bool corpseWasSpawned = corpse.Spawned;
            try
            {
                generatedPawn = GenerateZombie(kindDef, generationFaction, initialSpawn: false);
                if (generatedPawn == null)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Warning("[Zed Zed Zed] Reanimation failed because a new zombie pawn could not be generated.");
                    }

                    return false;
                }

                CopyReanimatedIdentity(sourcePawn, generatedPawn, preserveName, preserveSkills, preserveRelations);
                if (desiredFaction == null && ZombieLurkerUtility.IsLurker(generatedPawn))
                {
                    ZombieLurkerUtility.ClearFaction(generatedPawn);
                }

                IntVec3 spawnCell = FindBestReanimationCell(originalCorpseCell, map);

                if (corpseWasSpawned && !corpse.Destroyed)
                {
                    corpse.Destroy(DestroyMode.Vanish);
                }

                GenSpawn.Spawn(generatedPawn, spawnCell, map, WipeMode.Vanish);
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
                else if (ZombieUtility.IsPlayerAlignedZombie(generatedPawn))
                {
                    ZombieUtility.EnsureFriendlyZombieState(generatedPawn, stopCurrentJobs: true);
                }
                else
                {
                    Current.Game?.GetComponent<ZombieGameComponent>()?.RegisterBehavior(generatedPawn, ZombieSpawnEventType.AssaultBase);
                    ZombieUtility.AssignInitialShambleJob(generatedPawn);
                    ZombieUtility.EnsureZombieAggression(generatedPawn);
                }

                ZombieUtility.MarkPawnGraphicsDirty(generatedPawn);
                newPawn = generatedPawn;
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    generatedPawn?.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }

                if (Prefs.DevMode)
                {
                    Log.Warning("[Zed Zed Zed] Fresh spawn reanimation path failed: " + ex);
                }

                return false;
            }
        }

        private static bool TryReanimateExistingPawnFromCorpse(Corpse corpse, PawnKindDef kindDef, Faction desiredFaction, bool preserveName, bool preserveSkills, bool preserveRelations, out Pawn reanimatedPawn)
        {
            reanimatedPawn = null;
            Pawn sourcePawn = corpse?.InnerPawn;
            Map map = corpse?.MapHeld;
            if (sourcePawn == null || map == null || kindDef == null)
            {
                return false;
            }

            Name originalName = sourcePawn.Name;
            IntVec3 spawnCell = FindBestReanimationCell(corpse.PositionHeld, map);
            try
            {
                if (!ZombieUtility.TryResurrectZombie(sourcePawn) || sourcePawn.Dead)
                {
                    return false;
                }

                if (!sourcePawn.Spawned)
                {
                    if (corpse.Spawned && !corpse.Destroyed)
                    {
                        corpse.Destroy(DestroyMode.Vanish);
                    }

                    GenSpawn.Spawn(sourcePawn, spawnCell, map, WipeMode.Vanish);
                }
                else if (sourcePawn.PositionHeld.IsValid && sourcePawn.MapHeld == map && spawnCell.IsValid && sourcePawn.PositionHeld != spawnCell)
                {
                    sourcePawn.Position = spawnCell;
                }

                ConvertExistingPawnToZombie(sourcePawn, kindDef, desiredFaction, preserveName, preserveSkills, preserveRelations, initialSpawn: false);
                ZombieUtility.PrepareZombieForReanimation(sourcePawn);
                ZombieUtility.PrepareSpawnedZombie(sourcePawn);
                if (preserveName && originalName != null)
                {
                    TrySetPawnName(sourcePawn, originalName);
                }

                ZombieUtility.RefreshDrownedState(sourcePawn);

                if (ZombieLurkerUtility.IsPassiveLurker(sourcePawn))
                {
                    sourcePawn.jobs?.StopAll();
                    ZombieLurkerUtility.EnsurePassiveLurkerBehavior(sourcePawn);
                }
                else if (ZombieLurkerUtility.IsColonyLurker(sourcePawn))
                {
                    ZombieLurkerUtility.EnsureColonyLurkerState(sourcePawn, emergencyStabilize: true, stopCurrentJobs: true);
                }
                else if (ZombieUtility.IsPlayerAlignedZombie(sourcePawn))
                {
                    ZombieUtility.EnsureFriendlyZombieState(sourcePawn, stopCurrentJobs: true);
                }
                else
                {
                    Current.Game?.GetComponent<ZombieGameComponent>()?.RegisterBehavior(sourcePawn, ZombieSpawnEventType.AssaultBase);
                    ZombieUtility.AssignInitialShambleJob(sourcePawn);
                    ZombieUtility.EnsureZombieAggression(sourcePawn);
                }

                ZombieUtility.MarkPawnGraphicsDirty(sourcePawn);
                reanimatedPawn = sourcePawn;
                return sourcePawn.Spawned && !sourcePawn.Dead && !sourcePawn.Destroyed;
            }
            catch (Exception ex)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[Zed Zed Zed] Direct corpse reanimation failed, falling back to fresh pawn generation: " + ex);
                }

                return false;
            }
        }

        private static Pawn GeneratePawnWithBestAvailableOverload(PawnKindDef kind, Faction faction)
        {
            if (kind == null)
            {
                return null;
            }

            if (faction != null)
            {
                try
                {
                    MethodInfo generatePawnWithFaction = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnKindDef), typeof(Faction) });
                    if (generatePawnWithFaction != null)
                    {
                        return generatePawnWithFaction.Invoke(null, new object[] { kind, faction }) as Pawn;
                    }
                }
                catch
                {
                }
            }

            return PawnGenerator.GeneratePawn(kind);
        }

        private static IntVec3 FindBestReanimationCell(IntVec3 preferredCell, Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (preferredCell.IsValid && preferredCell.InBounds(map) && preferredCell.Standable(map))
            {
                return preferredCell;
            }

            if (preferredCell.IsValid && preferredCell.InBounds(map))
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(preferredCell, 2.9f, useCenter: true))
                {
                    if (cell.InBounds(map) && cell.Standable(map))
                    {
                        return cell;
                    }
                }
            }

            return CellFinder.RandomClosewalkCellNear(preferredCell.IsValid && preferredCell.InBounds(map) ? preferredCell : map.Center, map, 8);
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

            ZombieUtility.EnsureZombieInfectionState(pawn);

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

            TryAssignFaction(pawn, ZombieFactionUtility.GetOrCreateZombieFaction());

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
            ZombieUtility.NormalizeRareBiterClothing(pawn, initialSpawn);
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
            ZombieUtility.NormalizeCoreZombieState(pawn);
            if (pawn.needs?.mood != null)
            {
                pawn.needs.mood.CurLevel = 0.05f;
            }

            ZombieLurkerUtility.EnsureEmotionlessLurker(pawn);
            EnsureZombieVisualIntegrity(pawn);
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

        public static bool EnsureZombieVisualIntegrity(Pawn pawn, bool markGraphicsDirty = true)
        {
            if (pawn?.story == null || pawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            bool changed = false;
            ZombieVariant variant = ZombieLurkerUtility.GetEffectiveVisualVariant(pawn, ZombieUtility.GetVariant(pawn));

            try
            {
                BodyTypeDef fallbackBodyType = pawn.gender == Gender.Female ? BodyTypeDefOf.Female : BodyTypeDefOf.Male;
                BodyTypeDef desiredBodyType = ZombieVisualUtility.GetBodyType(variant, pawn, pawn.story.bodyType ?? fallbackBodyType);
                if (desiredBodyType != null && pawn.story.bodyType != desiredBodyType)
                {
                    pawn.story.bodyType = desiredBodyType;
                    changed = true;
                }
            }
            catch
            {
            }

            try
            {
                Color desiredSkinColor = ZombieVisualUtility.GetSkinColor(pawn, variant);
                if (!ColorsNearlyEqual(pawn.story.skinColorOverride, desiredSkinColor))
                {
                    pawn.story.skinColorOverride = desiredSkinColor;
                    changed = true;
                }
            }
            catch
            {
            }

            HeadTypeDef headType = GetObjectMember(pawn.story, "headType") as HeadTypeDef;
            if (headType == null)
            {
                HeadTypeDef fallbackHead = GetFallbackHeadType(pawn);
                if (fallbackHead != null)
                {
                    SetObjectMember(pawn.story, "headType", fallbackHead);
                    changed = true;
                }
            }

            HairDef hairDef = GetObjectMember(pawn.story, "hairDef") as HairDef;
            if (hairDef == null)
            {
                HairDef fallbackHair = DefDatabase<HairDef>.GetNamedSilentFail("Bald")
                    ?? DefDatabase<HairDef>.GetNamedSilentFail("Shaved")
                    ?? DefDatabase<HairDef>.AllDefsListForReading.FirstOrDefault();
                if (fallbackHair != null)
                {
                    SetObjectMember(pawn.story, "hairDef", fallbackHair);
                    changed = true;
                }
            }

            if (pawn.style != null)
            {
                if (BeardDefOf.NoBeard != null)
                {
                    SetObjectMember(pawn.style, "beardDef", BeardDefOf.NoBeard);
                }

                TattooDef noFaceTattoo = GetNoTattoo("Face");
                TattooDef noBodyTattoo = GetNoTattoo("Body");
                if (noFaceTattoo != null)
                {
                    SetObjectMember(pawn.style, "FaceTattoo", noFaceTattoo);
                    SetObjectMember(pawn.style, "faceTattoo", noFaceTattoo);
                    changed = true;
                }

                if (noBodyTattoo != null)
                {
                    SetObjectMember(pawn.style, "BodyTattoo", noBodyTattoo);
                    SetObjectMember(pawn.style, "bodyTattoo", noBodyTattoo);
                    changed = true;
                }
            }

            if (changed && markGraphicsDirty)
            {
                ZombieUtility.MarkPawnGraphicsDirty(pawn);
            }

            return changed;
        }


        private static bool ColorsNearlyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.001f
                && Mathf.Abs(a.g - b.g) < 0.001f
                && Mathf.Abs(a.b - b.b) < 0.001f
                && Mathf.Abs(a.a - b.a) < 0.001f;
        }

        private static HeadTypeDef GetFallbackHeadType(Pawn pawn)
        {
            return DefDatabase<HeadTypeDef>.AllDefsListForReading
                .FirstOrDefault(def => def != null && def.randomChosen && (def.gender == Gender.None || def.gender == pawn.gender))
                ?? DefDatabase<HeadTypeDef>.AllDefsListForReading
                    .FirstOrDefault(def => def != null && (def.gender == Gender.None || def.gender == pawn.gender))
                ?? DefDatabase<HeadTypeDef>.AllDefsListForReading.FirstOrDefault();
        }

        private static TattooDef GetNoTattoo(string areaKey)
        {
            return DefDatabase<TattooDef>.AllDefsListForReading
                .FirstOrDefault(def => def != null
                    && !def.defName.NullOrEmpty()
                    && def.defName.IndexOf("NoTattoo", StringComparison.OrdinalIgnoreCase) >= 0
                    && def.defName.IndexOf(areaKey, StringComparison.OrdinalIgnoreCase) >= 0)
                ?? DefDatabase<TattooDef>.AllDefsListForReading
                    .FirstOrDefault(def => def != null
                        && !def.defName.NullOrEmpty()
                        && def.defName.IndexOf("NoTattoo", StringComparison.OrdinalIgnoreCase) >= 0);
        }

    }
}
