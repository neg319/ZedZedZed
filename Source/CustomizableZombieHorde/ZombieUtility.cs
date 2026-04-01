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
                case "CZH_Zombie_Grabber":
                    return ZombieVariant.Grabber;
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

            int woundCount = Math.Min(parts.Count, IsVariant(pawn, ZombieVariant.Crawler) ? Rand.RangeInclusive(1, 2) : Rand.RangeInclusive(1, 3));
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
            if (torso != null && !pawn.health.hediffSet.PartIsMissing(torso))
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

                float severity = torsoSeverity ? Rand.Range(1.6f, 3.0f) : Rand.Range(0.8f, 2.0f);
                if (IsVariant(pawn, ZombieVariant.Tank))
                {
                    severity *= 0.60f;
                }
                else if (IsVariant(pawn, ZombieVariant.Crawler))
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

        public static void SetZombieDisplayName(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            NameSingle name = new NameSingle(ZombieDefUtility.GetDisplayLabelForKind(pawn.kindDef));
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

            StripAllUsableItems(pawn);
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

            PrepareSpawnedZombie(pawn);
            if (pawn.Downed)
            {
                return;
            }

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
            SetZombieDisplayName(pawn);
            TryRecoverFromSpawnIncap(pawn);
        }

        public static LocomotionUrgency GetZombieUrgency(Pawn pawn)
        {
            if (IsVariant(pawn, ZombieVariant.Crawler))
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

            Pawn prey = ZombieSpecialUtility.FindClosestLivingPrey(pawn, ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) ? 12f : 10f);
            if (prey != null)
            {
                if (IsVariant(pawn, ZombieVariant.Grabber) && Current.Game != null)
                {
                    var component = Current.Game.GetComponent<ZombieGameComponent>();
                    if (component != null && component.HasActiveGrabberTongue(pawn))
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
