using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieUtility
    {
        private static readonly HashSet<BodyPartDef> LimbPartDefs = new HashSet<BodyPartDef>
        {
            BodyPartDefOf.Arm,
            BodyPartDefOf.Leg
        };

        public static bool IsZombie(Pawn pawn)
        {
            return ZombieRulesUtility.IsZombie(pawn);
        }

        public static ZombieVariant GetVariant(Pawn pawn)
        {
            return ZombieVariantUtility.GetVariant(pawn);
        }

        public static bool IsVariant(Pawn pawn, ZombieVariant variant)
        {
            return IsZombie(pawn) && GetVariant(pawn) == variant;
        }

        public static bool ShouldSpawnAsSkeletonBiter(Pawn pawn)
        {
            return pawn != null && IsVariant(pawn, ZombieVariant.Biter) && Mathf.Abs(pawn.thingIDNumber) % 20 == 0;
        }

        public static bool IsSkeletonBiter(Pawn pawn)
        {
            return pawn?.health?.hediffSet != null
                && IsVariant(pawn, ZombieVariant.Biter)
                && pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSkeletonBiter);
        }

        public static float GetZombieIncomingDamageMultiplier(Pawn pawn)
        {
            if (!IsZombie(pawn))
            {
                return 1f;
            }

            if (IsVariant(pawn, ZombieVariant.Biter))
            {
                return 4f;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 4f;
            }

            if (IsVariant(pawn, ZombieVariant.Boomer))
            {
                return 6.67f;
            }

            if (IsVariant(pawn, ZombieVariant.Sick))
            {
                return 2f;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return 4f;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return 1f;
            }

            if (IsVariant(pawn, ZombieVariant.Grabber))
            {
                return 2.86f;
            }

            return 1.60f;
        }

        public static BodyPartRecord GetHeadPart(Pawn pawn)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || pawn.health?.hediffSet == null)
            {
                return null;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part?.def == BodyPartDefOf.Head && !pawn.health.hediffSet.PartIsMissing(part))
                {
                    return part;
                }
            }

            return null;
        }

        public static bool ShouldZombiesIgnore(Pawn pawn)
        {
            return ZombieRulesUtility.IsIgnoredByZombies(pawn);
        }

        public static bool IsPlayerAlignedZombie(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn == null)
            {
                return false;
            }

            if (ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                return true;
            }

            if (pawn.Faction == Faction.OfPlayer)
            {
                return true;
            }

            try
            {
                return pawn.IsSlaveOfColony;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsColonyAlly(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer)
            {
                return true;
            }

            try
            {
                if (pawn.IsSlaveOfColony || pawn.IsPrisonerOfColony)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool IsEnemyOfColony(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed)
            {
                return false;
            }

            if (IsColonyAlly(pawn) || IsPlayerAlignedZombie(pawn) || ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                return false;
            }

            if (IsZombie(pawn))
            {
                return true;
            }

            Faction faction = pawn.Faction;
            return faction != null && !faction.IsPlayer && faction.HostileTo(Faction.OfPlayer);
        }

        public static bool ShouldZombieIgnoreTarget(Pawn zombie, Pawn target)
        {
            if (target == null)
            {
                return true;
            }

            if (IsPlayerAlignedZombie(zombie))
            {
                if (IsColonyAlly(target) || IsPlayerAlignedZombie(target) || ZombieLurkerUtility.IsPassiveLurker(target))
                {
                    return true;
                }

                return !IsEnemyOfColony(target);
            }

            return ShouldZombiesIgnore(target);
        }

        public static bool IsUnderColonyRestraint(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            try
            {
                if (pawn.IsPrisonerOfColony)
                {
                    return true;
                }
            }
            catch
            {
            }

            return pawn.Downed || pawn.CurJob?.def == JobDefOf.LayDown;
        }

        public static bool HasHeadDamageOrDestruction(Pawn pawn)
        {
            return ZombieRulesUtility.HasHeadDamageOrDestruction(pawn);
        }

        public static bool CanReanimate(Pawn pawn)
        {
            return ZombieRulesUtility.CanReanimate(pawn);
        }

        public static IEnumerable<BodyPartRecord> GetZombieLimbParts(Pawn pawn)
        {
            if (pawn?.RaceProps?.body?.AllParts == null)
            {
                yield break;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part != null && LimbPartDefs.Contains(part.def))
                {
                    yield return part;
                }
            }
        }

        public static void ApplyLimbDecay(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            List<BodyPartRecord> candidateParts = GetZombieLimbParts(pawn)
                .Where(part => part != null && !pawn.health.hediffSet.PartIsMissing(part))
                .ToList();
            if (candidateParts.Count == 0)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                candidateParts = candidateParts.Where(part => part.def == BodyPartDefOf.Leg).ToList();
            }

            int decayCount = GetInitialDecayPartCount(pawn, candidateParts.Count);
            foreach (BodyPartRecord part in candidateParts.InRandomOrder().Take(decayCount))
            {
                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieLimbDecay, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieLimbDecay, part);
                }
            }
        }

        public static void ApplyRuntLegDamage(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Runt) || pawn?.health?.hediffSet == null || pawn.RaceProps?.body?.AllParts == null)
            {
                return;
            }

            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (part?.def != BodyPartDefOf.Leg || pawn.health.hediffSet.PartIsMissing(part))
                {
                    continue;
                }

                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieOpenWound, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieOpenWound, part);
                }

                Hediff_Injury existingInjury = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .FirstOrDefault(injury => injury.Part == part && !injury.IsPermanent() && injury.Severity >= 12f);
                if (existingInjury != null)
                {
                    continue;
                }

                try
                {
                    Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, part);
                    if (injury != null)
                    {
                        injury.Severity = 12f;
                        pawn.health.AddHediff(injury, part);
                    }
                }
                catch
                {
                }
            }
        }

        public static void ApplyVisibleDecayInjuries(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            List<BodyPartRecord> parts = GetZombieLimbParts(pawn)
                .Where(part => part != null && !pawn.health.hediffSet.PartIsMissing(part))
                .InRandomOrder()
                .ToList();

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                parts = parts.Where(part => part.def == BodyPartDefOf.Leg).ToList();
            }

            int woundCount = Math.Min(parts.Count, GetInitialVisibleWoundCount(pawn));
            for (int i = 0; i < woundCount; i++)
            {
                BodyPartRecord part = parts[i];
                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieOpenWound, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieOpenWound, part);
                }

                TryAddStartingDecayDamage(pawn, part, torsoSeverity: false);
            }

            BodyPartRecord torso = pawn.RaceProps?.body?.corePart;
            if (torso != null && !pawn.health.hediffSet.PartIsMissing(torso) && Rand.Chance(GetInitialTorsoWoundChance(pawn)))
            {
                TryAddStartingDecayDamage(pawn, torso, torsoSeverity: true);
            }
        }

        private static void TryAddStartingDecayDamage(Pawn pawn, BodyPartRecord part, bool torsoSeverity)
        {
            if (pawn?.health == null || part == null)
            {
                return;
            }

            try
            {
                Hediff injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, part);
                if (injury == null)
                {
                    return;
                }

                float severity = torsoSeverity ? Rand.Range(0.8f, 1.8f) : Rand.Range(0.35f, 1.10f);
                if (IsVariant(pawn, ZombieVariant.Brute))
                {
                    severity *= 0.30f;
                }
                else if (IsVariant(pawn, ZombieVariant.Runt))
                {
                    severity *= 0.85f;
                }

                injury.Severity = severity;
                pawn.health.AddHediff(injury, part);
            }
            catch
            {
            }
        }

        private static int GetInitialDecayPartCount(Pawn pawn, int availableCount)
        {
            if (availableCount <= 0)
            {
                return 0;
            }

            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return Math.Min(availableCount, 2);
            }

            if (IsVariant(pawn, ZombieVariant.Brute) || IsVariant(pawn, ZombieVariant.Grabber))
            {
                return Math.Min(availableCount, 2);
            }

            return 1;
        }

        private static int GetInitialVisibleWoundCount(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 1;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return Rand.Chance(0.50f) ? 1 : 0;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return Rand.Chance(0.35f) ? 1 : 0;
            }

            return Rand.Chance(0.60f) ? 1 : 0;
        }

        private static float GetInitialTorsoWoundChance(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return 0.15f;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned))
            {
                return 0.20f;
            }

            if (IsVariant(pawn, ZombieVariant.Brute))
            {
                return 0.30f;
            }

            return 0.45f;
        }

        public static void NormalizeZombieCosmeticDamage(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            TrimExcessLimbDecay(pawn);
            TrimExcessOpenWounds(pawn);
        }

        private static void TrimExcessLimbDecay(Pawn pawn)
        {
            List<Hediff> limbDecay = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieLimbDecay && hediff.Part != null)
                .ToList();
            if (limbDecay.Count == 0)
            {
                return;
            }

            IEnumerable<Hediff> allowed = limbDecay.Where(hediff => LimbPartDefs.Contains(hediff.Part.def));
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                allowed = allowed.Where(hediff => hediff.Part.def == BodyPartDefOf.Leg);
            }

            int keepCount = GetInitialDecayPartCount(pawn, allowed.Count());
            HashSet<BodyPartRecord> keepParts = new HashSet<BodyPartRecord>(allowed
                .OrderByDescending(hediff => GetCosmeticPartPriority(pawn, hediff.Part))
                .Take(keepCount)
                .Select(hediff => hediff.Part));

            foreach (Hediff hediff in limbDecay)
            {
                if (!keepParts.Contains(hediff.Part))
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        private static void TrimExcessOpenWounds(Pawn pawn)
        {
            List<Hediff> openWounds = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == ZombieDefOf.CZH_ZombieOpenWound && hediff.Part != null)
                .ToList();
            if (openWounds.Count == 0)
            {
                return;
            }

            IEnumerable<Hediff> allowed = openWounds.Where(hediff => LimbPartDefs.Contains(hediff.Part.def));
            int keepCount = IsVariant(pawn, ZombieVariant.Runt) ? 2 : 1;
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                allowed = allowed.Where(hediff => hediff.Part.def == BodyPartDefOf.Leg);
            }

            HashSet<BodyPartRecord> keepParts = new HashSet<BodyPartRecord>(allowed
                .OrderByDescending(hediff => GetCosmeticPartPriority(pawn, hediff.Part))
                .Take(keepCount)
                .Select(hediff => hediff.Part));

            foreach (Hediff hediff in openWounds)
            {
                if (keepParts.Contains(hediff.Part))
                {
                    continue;
                }

                BodyPartRecord part = hediff.Part;
                pawn.health.RemoveHediff(hediff);
                RemoveCosmeticStartingInjuries(pawn, part);
            }
        }

        private static float GetCosmeticPartPriority(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null)
            {
                return 0f;
            }

            float injurySeverity = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.Part == part && !injury.IsPermanent())
                .Sum(injury => injury.Severity);
            float priority = injurySeverity;
            if (part.def == BodyPartDefOf.Leg)
            {
                priority += 0.25f;
            }

            return priority;
        }

        private static void RemoveCosmeticStartingInjuries(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet == null || part == null)
            {
                return;
            }

            List<Hediff_Injury> removable = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(injury => injury.Part == part && !injury.IsPermanent() && injury.Severity <= 1.20f)
                .ToList();
            foreach (Hediff_Injury injury in removable)
            {
                pawn.health.RemoveHediff(injury);
            }
        }

        public static void ApplyVariantHediffs(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Biter) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieBiter))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieBiter);
            }

            if (ShouldSpawnAsSkeletonBiter(pawn) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSkeletonBiter))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieSkeletonBiter);
            }

            if (IsVariant(pawn, ZombieVariant.Runt) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRunt))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRunt);
            }

            if (IsVariant(pawn, ZombieVariant.Brute) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieBrute))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieBrute);
            }
        }

        public static void SetZombieDisplayName(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            NameSingle name = new NameSingle(ZombieDefUtility.GetDisplayLabelForPawn(pawn));
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

        public static void StripAllUsableItems(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.equipment != null)
            {
                try
                {
                    pawn.equipment.DestroyAllEquipment();
                }
                catch
                {
                }
            }

            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    try
                    {
                        thing?.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }

            if (pawn.carryTracker?.innerContainer != null)
            {
                for (int i = pawn.carryTracker.innerContainer.Count - 1; i >= 0; i--)
                {
                    Thing thing = pawn.carryTracker.innerContainer[i];
                    try
                    {
                        thing?.Destroy(DestroyMode.Vanish);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static void TrimZombieApparel(Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return;
            }

            bool isBrute = IsVariant(pawn, ZombieVariant.Brute);
            List<Apparel> worn = pawn.apparel.WornApparel.ToList();
            List<Apparel> keep = new List<Apparel>();

            int basicTarget = isBrute ? Rand.RangeInclusive(0, 1) : Rand.RangeInclusive(0, 2);
            bool allowRareArmor = !isBrute && Rand.Chance(0.025f);
            bool allowRareHeadwear = Rand.Chance(isBrute ? 0.008f : 0.018f);
            int basicKept = 0;

            foreach (Apparel apparel in worn.InRandomOrder())
            {
                if (apparel == null)
                {
                    continue;
                }

                if (IsZombieArmor(apparel))
                {
                    if (allowRareArmor && keep.All(existing => !IsZombieArmor(existing)))
                    {
                        keep.Add(apparel);
                    }
                    continue;
                }

                if (IsZombieHeadwear(apparel))
                {
                    if (allowRareHeadwear && keep.All(existing => !IsZombieHeadwear(existing)))
                    {
                        keep.Add(apparel);
                    }
                    continue;
                }

                if (basicKept < basicTarget && IsBasicZombieClothing(apparel))
                {
                    keep.Add(apparel);
                    basicKept++;
                }
            }

            foreach (Apparel apparel in worn)
            {
                if (apparel == null || keep.Contains(apparel))
                {
                    continue;
                }

                try
                {
                    pawn.apparel.Remove(apparel);
                }
                catch
                {
                }

                try
                {
                    apparel.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }
        }

        private static bool IsBasicZombieClothing(Apparel apparel)
        {
            if (apparel?.def?.apparel == null)
            {
                return false;
            }

            if (IsZombieArmor(apparel) || IsZombieHeadwear(apparel))
            {
                return false;
            }

            foreach (BodyPartGroupDef group in apparel.def.apparel.bodyPartGroups ?? Enumerable.Empty<BodyPartGroupDef>())
            {
                string name = ((group?.defName ?? string.Empty) + ' ' + (group?.label ?? string.Empty)).ToLowerInvariant();
                if (name.Contains("torso") || name.Contains("legs") || name.Contains("shoulder") || name.Contains("waist"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsZombieHeadwear(Apparel apparel)
        {
            if (apparel?.def?.apparel == null)
            {
                return false;
            }

            foreach (BodyPartGroupDef group in apparel.def.apparel.bodyPartGroups ?? Enumerable.Empty<BodyPartGroupDef>())
            {
                string name = ((group?.defName ?? string.Empty) + ' ' + (group?.label ?? string.Empty)).ToLowerInvariant();
                if (name.Contains("head") || name.Contains("eye") || name.Contains("face") || name.Contains("jaw") || name.Contains("nose"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsZombieArmor(Apparel apparel)
        {
            if (apparel?.def == null)
            {
                return false;
            }

            float armorValue = 0f;
            foreach (StatModifier modifier in apparel.def.statBases ?? Enumerable.Empty<StatModifier>())
            {
                string statName = modifier?.stat?.defName ?? string.Empty;
                if (statName == "ArmorRating_Sharp" || statName == "ArmorRating_Blunt" || statName == "ArmorRating_Heat")
                {
                    armorValue += modifier.value;
                }
            }

            if (armorValue >= 0.55f)
            {
                return true;
            }

            string labelText = ((apparel.def.defName ?? string.Empty) + ' ' + (apparel.def.label ?? string.Empty)).ToLowerInvariant();
            return labelText.Contains("armor") || labelText.Contains("helmet") || labelText.Contains("flak") || labelText.Contains("shield belt");
        }

        public static void MarkZombieApparelTainted(Pawn pawn, bool degradeApparel)
        {
            if (pawn?.apparel?.WornApparel == null)
            {
                return;
            }

            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel == null)
                {
                    continue;
                }

                SetApparelTainted(apparel);
                if (degradeApparel)
                {
                    int minHitPoints = Math.Max(1, Mathf.RoundToInt(apparel.MaxHitPoints * 0.20f));
                    int maxHitPoints = Math.Max(minHitPoints, Mathf.RoundToInt(apparel.MaxHitPoints * 0.70f));
                    apparel.HitPoints = Math.Min(apparel.HitPoints, Rand.RangeInclusive(minHitPoints, maxHitPoints));
                }
            }
        }

        public static void SetApparelTainted(Apparel apparel)
        {
            if (apparel == null)
            {
                return;
            }

            try
            {
                AccessTools.PropertySetter(typeof(Apparel), "WornByCorpse")?.Invoke(apparel, new object[] { true });
            }
            catch
            {
            }

            try
            {
                Traverse.Create(apparel).Field("wornByCorpseInt").SetValue(true);
            }
            catch
            {
            }
        }

        public static void GiveRunnerSpeedIfRolled(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            float chance = CustomizableZombieHordeMod.Settings?.fastZombieChance ?? 0.04f;
            if (chance > 0f && Rand.Chance(chance) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRunner))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRunner);
            }
        }

        public static void PrepareZombieForReanimation(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Faction == null && !ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                pawn.SetFactionDirect(ZombieFactionUtility.GetOrCreateZombieFaction());
            }

            StripAllUsableItems(pawn);
            TrimZombieApparel(pawn);
            MarkZombieApparelTainted(pawn, degradeApparel: false);
            if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieRot))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieRot);
            }

            ApplyVariantHediffs(pawn);
            RefreshDrownedState(pawn);
            MarkPawnGraphicsDirty(pawn);
        }

        public static bool IsWaterCell(IntVec3 cell, Map map)
        {
            if (map == null || !cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            TerrainDef terrain = cell.GetTerrain(map);
            if (terrain == null)
            {
                return false;
            }

            string terrainName = (terrain.defName ?? string.Empty) + " " + (terrain.label ?? string.Empty);
            return terrainName.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("ocean", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("river", StringComparison.OrdinalIgnoreCase) >= 0
                || terrainName.IndexOf("marsh", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        private static bool IsExposedToRain(Pawn pawn)
        {
            return pawn?.Spawned == true
                && pawn.MapHeld != null
                && ZombieSpecialUtility.IsRainActive(pawn.MapHeld)
                && !pawn.PositionHeld.Roofed(pawn.MapHeld);
        }

        public static void RefreshDrownedState(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Drowned) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            bool inWater = pawn.Spawned && IsWaterCell(pawn.Position, pawn.Map);
            bool rainFed = IsExposedToRain(pawn);
            bool hasWater = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedWater);
            bool hasLand = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedLand);

            if (inWater || rainFed)
            {
                if (hasLand)
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieDrownedLand));
                }

                if (!hasWater)
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieDrownedWater);
                }
            }
            else
            {
                if (hasWater)
                {
                    pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(ZombieDefOf.CZH_ZombieDrownedWater));
                }

                if (!hasLand)
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieDrownedLand);
                }
            }
        }

        public static void HandleDrownedRegeneration(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Drowned) || pawn?.health?.hediffSet == null || !pawn.Spawned)
            {
                return;
            }

            bool inWater = IsWaterCell(pawn.PositionHeld, pawn.MapHeld);
            bool inRain = IsExposedToRain(pawn);
            if ((!inWater && !inRain) || !pawn.IsHashIntervalTick(300))
            {
                return;
            }

            Hediff_Injury injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(hediff => !hediff.IsPermanent() && hediff.Part != null && !pawn.health.hediffSet.PartIsMissing(hediff.Part))
                .OrderByDescending(hediff => hediff.Severity)
                .FirstOrDefault();
            if (injury == null)
            {
                return;
            }

            injury.Heal(inWater ? 0.40f : 0.24f);
        }

        public static void EnsureZombieAggression(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.jobs == null)
            {
                return;
            }

            if (ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                ZombieLurkerUtility.EnsurePassiveLurkerBehavior(pawn);
                return;
            }

            if (ZombieLurkerUtility.IsColonyLurker(pawn) || IsUnderColonyRestraint(pawn))
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Drowned) && ZombieSpecialUtility.ShouldDrownedHoldWater(pawn))
            {
                PrepareSpawnedZombie(pawn);
                if (pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.AttackMelee || pawn.CurJob.def == JobDefOf.Goto))
                {
                    pawn.jobs.StopAll();
                }

                return;
            }

            PrepareSpawnedZombie(pawn);
            if (pawn.Downed)
            {
                return;
            }

            if (ZombieSpecialUtility.HandleBoneBiterBehavior(pawn))
            {
                return;
            }

            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;
            if (currentTarget != null && IsZombie(currentTarget))
            {
                pawn.jobs.StopAll();
                currentTarget = null;
            }

            if (currentTarget != null && !currentTarget.Dead && !currentTarget.Destroyed && !ShouldZombieIgnoreTarget(pawn, currentTarget))
            {
                return;
            }

            if (IsBadZombieJob(pawn.CurJob, pawn.MapHeld))
            {
                pawn.jobs.StopAll();
            }

            AssignBehaviorJob(pawn);
        }

        private static void TryEndZombieMentalState(Pawn pawn)
        {
            object handler = pawn?.mindState?.mentalStateHandler;
            if (handler == null)
            {
                return;
            }

            try
            {
                if ((bool)(AccessTools.Property(handler.GetType(), "InMentalState")?.GetValue(handler, null) ?? false))
                {
                    AccessTools.Method(handler.GetType(), "Reset")?.Invoke(handler, null);
                }
            }
            catch
            {
            }
        }

        public static void PrepareSpawnedZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TryEndZombieMentalState(pawn);
            StripAllUsableItems(pawn);
            TrimZombieApparel(pawn);
            MarkZombieApparelTainted(pawn, degradeApparel: false);
            SetZombieDisplayName(pawn);
            TryRecoverFromSpawnIncap(pawn);
        }

        public static LocomotionUrgency GetZombieUrgency(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Runt))
            {
                return LocomotionUrgency.Amble;
            }

            return IsVariant(pawn, ZombieVariant.Grabber) ? LocomotionUrgency.Walk : LocomotionUrgency.Walk;
        }

        public static void AssignInitialShambleJob(Pawn pawn)
        {
            ZombieSpawnEventType behavior = Current.Game?.GetComponent<ZombieGameComponent>()?.GetAssignedBehavior(pawn) ?? ZombieSpawnEventType.AssaultBase;
            AssignInitialShambleJob(pawn, behavior);
        }

        public static void AssignInitialShambleJob(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null || pawn.Downed)
            {
                return;
            }

            IntVec3 targetCell = ZombieSpecialUtility.FindInitialBehaviorCell(pawn, behavior);
            if (!targetCell.IsValid || targetCell == pawn.PositionHeld)
            {
                return;
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, targetCell);
                moveJob.expiryInterval = behavior == ZombieSpawnEventType.HuddledPack ? 1500 : 1100;
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = GetZombieUrgency(pawn);
                pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        private static bool IsBadZombieJob(Job job, Map map)
        {
            if (job == null)
            {
                return true;
            }

            string defName = job.def?.defName ?? string.Empty;
            if (defName.Contains("ExitMap") || defName.Contains("LeaveMap"))
            {
                return true;
            }

            if (map != null && job.def == JobDefOf.Goto && job.targetA.IsValid)
            {
                IntVec3 cell = job.targetA.Cell;
                if (!cell.IsValid || !cell.InBounds(map) || ZombieSpecialUtility.DistanceToNearestEdge(cell, map) < 5)
                {
                    return true;
                }
            }

            return false;
        }

        private static void TryRecoverFromSpawnIncap(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || !pawn.Downed)
            {
                return;
            }

            List<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().OrderByDescending(injury => injury.Severity).ToList();
            for (int pass = 0; pass < 3 && pawn.Downed; pass++)
            {
                for (int i = 0; i < injuries.Count && pawn.Downed; i++)
                {
                    Hediff_Injury injury = injuries[i];
                    if (injury == null)
                    {
                        continue;
                    }

                    injury.Severity *= 0.45f;
                    if (injury.Severity < 0.20f)
                    {
                        try
                        {
                            pawn.health.RemoveHediff(injury);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (pawn.Downed)
            {
                try
                {
                    Traverse.Create(pawn.health).Field("forceIncap").SetValue(false);
                }
                catch
                {
                }

                try
                {
                    Traverse.Create(pawn.health).Field("forceDowned").SetValue(false);
                }
                catch
                {
                }
            }
        }

        private static void AssignBehaviorJob(Pawn pawn)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null)
            {
                return;
            }

            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(pawn, ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) ? 26f : 10f);
            if (prey != null)
            {
                if (IsVariant(pawn, ZombieVariant.Grabber))
                {
                    if (Current.Game != null)
                    {
                        var component = Current.Game.GetComponent<ZombieGameComponent>();
                        if (component != null && component.HasActiveGrabberTongue(pawn))
                        {
                            return;
                        }
                    }

                    float distanceSquared = pawn.PositionHeld.DistanceToSquared(prey.PositionHeld);
                    if (distanceSquared <= ZombieGrabberUtility.HoldStartRange * ZombieGrabberUtility.HoldStartRange && ZombieGrabberUtility.TryForceTongueStart(pawn, prey))
                    {
                        return;
                    }
                }

                try
                {
                    Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, prey);
                    attackJob.expiryInterval = 700;
                    attackJob.checkOverrideOnExpire = true;
                    attackJob.locomotionUrgency = GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
                    return;
                }
                catch
                {
                }
            }

            Corpse corpse = ZombieSpecialUtility.FindNearbyFreshCorpse(pawn, 5f);
            if (corpse != null)
            {
                try
                {
                    Job feedJob = JobMaker.MakeJob(JobDefOf.Goto, corpse.Position);
                    feedJob.expiryInterval = 450;
                    feedJob.checkOverrideOnExpire = true;
                    feedJob.locomotionUrgency = GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(feedJob, JobTag.Misc);
                    return;
                }
                catch
                {
                }
            }

            ZombieSpawnEventType behavior = Current.Game?.GetComponent<ZombieGameComponent>()?.GetAssignedBehavior(pawn) ?? ZombieSpawnEventType.AssaultBase;
            IntVec3 shambleCell = ZombieSpecialUtility.FindBehaviorCell(pawn, behavior);
            if (!shambleCell.IsValid || shambleCell == pawn.PositionHeld)
            {
                return;
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, shambleCell);
                moveJob.expiryInterval = behavior == ZombieSpawnEventType.HuddledPack ? 1500 : 900;
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = GetZombieUrgency(pawn);
                pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
            }
            catch
            {
            }
        }

        public static void MarkPawnGraphicsDirty(Pawn pawn)
        {
            object renderer = pawn?.Drawer?.renderer;
            if (renderer == null)
            {
                return;
            }

            try
            {
                var directMethod = AccessTools.Method(renderer.GetType(), "SetAllGraphicsDirty");
                if (directMethod != null)
                {
                    directMethod.Invoke(renderer, null);
                    return;
                }

                object graphics = Traverse.Create(renderer).Property("graphics").GetValue();
                if (graphics == null)
                {
                    return;
                }

                var graphicsMethod = AccessTools.Method(graphics.GetType(), "SetAllGraphicsDirty");
                graphicsMethod?.Invoke(graphics, null);
            }
            catch
            {
            }
        }

        public static bool TryResurrectZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (!pawn.Dead)
            {
                return true;
            }

            Corpse corpse = ResolveCorpseForResurrection(pawn);
            Type[] candidateTypes =
            {
                typeof(ResurrectionUtility),
                AccessTools.TypeByName("RimWorld.DebugToolsSpawning"),
                AccessTools.TypeByName("Verse.DebugToolsSpawning")
            };

            foreach (Type type in candidateTypes)
            {
                if (type == null)
                {
                    continue;
                }

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                }
                catch
                {
                    continue;
                }

                foreach (MethodInfo method in methods)
                {
                    if (method == null)
                    {
                        continue;
                    }

                    if (!string.Equals(method.Name, "Resurrect", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(method.Name, "TryResurrect", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!TryBuildResurrectionArgs(method, pawn, corpse, out object[] args))
                    {
                        continue;
                    }

                    try
                    {
                        object result = method.Invoke(null, args);
                        if (!pawn.Dead)
                        {
                            return true;
                        }

                        if (result is bool success && success && !pawn.Dead)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return !pawn.Dead;
        }

        private static Corpse ResolveCorpseForResurrection(Pawn pawn)
        {
            if (pawn?.Corpse != null && !pawn.Corpse.Destroyed)
            {
                return pawn.Corpse;
            }

            Map map = pawn?.MapHeld;
            if (map != null && pawn.PositionHeld.IsValid && pawn.PositionHeld.InBounds(map))
            {
                Corpse localCorpse = map.thingGrid?.ThingsAt(pawn.PositionHeld)?.OfType<Corpse>()?.FirstOrDefault(c => c.InnerPawn == pawn);
                if (localCorpse != null && !localCorpse.Destroyed)
                {
                    return localCorpse;
                }
            }

            foreach (Map searchMap in Find.Maps)
            {
                Corpse found = searchMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn && !c.Destroyed);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static bool TryBuildResurrectionArgs(MethodInfo method, Pawn pawn, Corpse corpse, out object[] args)
        {
            args = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters == null || parameters.Length == 0 || parameters.Length > 4)
            {
                return false;
            }

            bool assignedPawnOrCorpse = false;
            object[] built = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;

                if (parameterType.IsAssignableFrom(typeof(Pawn)))
                {
                    built[i] = pawn;
                    assignedPawnOrCorpse = true;
                    continue;
                }

                if (corpse != null && parameterType.IsAssignableFrom(typeof(Corpse)))
                {
                    built[i] = corpse;
                    assignedPawnOrCorpse = true;
                    continue;
                }

                if (parameter.IsOptional)
                {
                    built[i] = parameter.DefaultValue is DBNull ? Type.Missing : parameter.DefaultValue;
                    continue;
                }

                return false;
            }

            if (!assignedPawnOrCorpse)
            {
                return false;
            }

            args = built;
            return true;
        }
    }
}
