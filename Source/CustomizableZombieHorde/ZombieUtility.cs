using System;
using System.Collections.Generic;
using System.Linq;
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
            BodyPartDefOf.Hand,
            BodyPartDefOf.Leg
        };

        public static bool IsZombie(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieRot) == true)
            {
                return true;
            }

            return pawn.kindDef?.defName?.StartsWith("CZH_Zombie_") == true;
        }

        public static ZombieVariant GetVariant(Pawn pawn)
        {
            string defName = pawn?.kindDef?.defName ?? string.Empty;
            switch (defName)
            {
                case "CZH_Zombie_Crawler":
                    return ZombieVariant.Crawler;
                case "CZH_Zombie_Boomer":
                    return ZombieVariant.Boomer;
                case "CZH_Zombie_Sick":
                    return ZombieVariant.Sick;
                case "CZH_Zombie_Drowned":
                    return ZombieVariant.Drowned;
                case "CZH_Zombie_Tank":
                    return ZombieVariant.Tank;
                default:
                    return ZombieVariant.Biter;
            }
        }

        public static bool IsVariant(Pawn pawn, ZombieVariant variant)
        {
            return IsZombie(pawn) && GetVariant(pawn) == variant;
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
            return pawn != null && (IsZombie(pawn) || ZombieTraitUtility.IsIgnoredByZombies(pawn));
        }

        public static bool HasHeadDamageOrDestruction(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return true;
            }

            if (!pawn.health.hediffSet.HasHead)
            {
                return true;
            }

            BodyPartRecord head = pawn.RaceProps?.body?.AllParts?.FirstOrDefault(part => part.def == BodyPartDefOf.Head);
            if (head == null)
            {
                return true;
            }

            return pawn.health.hediffSet.hediffs.Any(hediff => IsHeadPartOrChild(hediff.Part, head) && (hediff is Hediff_Injury || hediff is Hediff_MissingPart));
        }

        private static bool IsHeadPartOrChild(BodyPartRecord part, BodyPartRecord head)
        {
            for (BodyPartRecord current = part; current != null; current = current.parent)
            {
                if (current == head)
                {
                    return true;
                }
            }

            return false;
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

            foreach (BodyPartRecord part in GetZombieLimbParts(pawn))
            {
                if (!pawn.health.hediffSet.PartIsMissing(part) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieLimbDecay, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieLimbDecay, part);
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
                .Where(part => !pawn.health.hediffSet.PartIsMissing(part))
                .InRandomOrder()
                .ToList();

            int woundCount = Math.Min(parts.Count, Rand.RangeInclusive(1, 4));
            for (int i = 0; i < woundCount; i++)
            {
                BodyPartRecord part = parts[i];
                if (!pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieOpenWound, part))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieOpenWound, part);
                }

                TryAddStartingDecayDamage(pawn, part);
            }
        }

        private static void TryAddStartingDecayDamage(Pawn pawn, BodyPartRecord part)
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

                float severity = Rand.Range(2.0f, 4.0f);
                if (IsVariant(pawn, ZombieVariant.Tank))
                {
                    severity *= 0.65f;
                }

                injury.Severity = severity;
                pawn.health.AddHediff(injury, part);
            }
            catch
            {
            }
        }

        public static void ApplyVariantHediffs(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return;
            }

            if (IsVariant(pawn, ZombieVariant.Crawler) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieCrawler))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieCrawler);
            }

            if (IsVariant(pawn, ZombieVariant.Tank) && !pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieTank))
            {
                pawn.health.AddHediff(ZombieDefOf.CZH_ZombieTank);
            }
        }

        public static void StripWeaponsAndWeaponInventory(Pawn pawn)
        {
            if (pawn?.equipment != null && pawn.equipment.Primary != null)
            {
                pawn.equipment.DestroyAllEquipment();
            }

            if (pawn?.inventory?.innerContainer == null)
            {
                return;
            }

            for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0; i--)
            {
                Thing thing = pawn.inventory.innerContainer[i];
                if (thing?.def?.IsWeapon == true)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
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

            if (pawn.Faction == null)
            {
                pawn.SetFactionDirect(ZombieFactionUtility.GetOrCreateZombieFaction());
            }

            StripWeaponsAndWeaponInventory(pawn);
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

        public static void RefreshDrownedState(Pawn pawn)
        {
            if (!IsVariant(pawn, ZombieVariant.Drowned) || pawn?.health?.hediffSet == null)
            {
                return;
            }

            bool inWater = pawn.Spawned && IsWaterCell(pawn.Position, pawn.Map);
            bool hasWater = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedWater);
            bool hasLand = pawn.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieDrownedLand);

            if (inWater)
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

        public static void EnsureZombieAggression(Pawn pawn)
        {
            if (!IsZombie(pawn) || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.jobs == null)
            {
                return;
            }

            TryEndZombieMentalState(pawn);

            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;
            if (currentTarget != null && IsZombie(currentTarget))
            {
                pawn.jobs.StopAll();
                currentTarget = null;
            }

            if (currentTarget != null && !currentTarget.Dead && !currentTarget.Destroyed && !ShouldZombiesIgnore(currentTarget))
            {
                return;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Goto && pawn.pather?.Moving == true)
            {
                return;
            }

            AssignHordeJob(pawn);
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

        private static void AssignHordeJob(Pawn pawn)
        {
            if (pawn?.MapHeld == null || pawn.jobs == null)
            {
                return;
            }

            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(pawn, 28f);
            if (prey != null)
            {
                try
                {
                    Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, prey);
                    attackJob.expiryInterval = 900;
                    attackJob.checkOverrideOnExpire = true;
                    attackJob.locomotionUrgency = LocomotionUrgency.Walk;
                    pawn.jobs.TryTakeOrderedJob(attackJob, JobTag.Misc);
                    return;
                }
                catch
                {
                }
            }

            Corpse corpse = ZombieSpecialUtility.FindNearbyFreshCorpse(pawn, 7f);
            if (corpse != null)
            {
                try
                {
                    Job feedJob = JobMaker.MakeJob(JobDefOf.Goto, corpse.Position);
                    feedJob.expiryInterval = 500;
                    feedJob.checkOverrideOnExpire = true;
                    feedJob.locomotionUrgency = LocomotionUrgency.Walk;
                    pawn.jobs.TryTakeOrderedJob(feedJob, JobTag.Misc);
                    return;
                }
                catch
                {
                }
            }

            IntVec3 shambleCell = ZombieSpecialUtility.FindHordeShambleCell(pawn);
            if (!shambleCell.IsValid || shambleCell == pawn.PositionHeld)
            {
                return;
            }

            try
            {
                Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, shambleCell);
                moveJob.expiryInterval = 700;
                moveJob.checkOverrideOnExpire = true;
                moveJob.locomotionUrgency = LocomotionUrgency.Walk;
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

            try
            {
                var resurrectMethod = AccessTools.Method(typeof(ResurrectionUtility), "Resurrect", new[] { typeof(Pawn) });
                if (resurrectMethod != null)
                {
                    resurrectMethod.Invoke(null, new object[] { pawn });
                    return true;
                }

                var tryResurrectMethod = AccessTools.Method(typeof(ResurrectionUtility), "TryResurrect", new[] { typeof(Pawn) });
                if (tryResurrectMethod != null)
                {
                    object result = tryResurrectMethod.Invoke(null, new object[] { pawn });
                    return !(result is bool success) || success;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
