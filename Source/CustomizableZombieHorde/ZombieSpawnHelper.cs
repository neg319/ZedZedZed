using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public enum ZombiePopulationState
    {
        Auto,
        Day,
        Night,
        FullMoon,
        BloodMoon
    }

    public static class ZombieSpawnHelper
    {
        public static int ApplyDifficultyMultiplier(int baseCount)
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            float multiplier = settings?.DaytimeTargetMultiplier ?? 4f;
            return baseCount < 1 ? 1 : GenMath.RoundRandom(baseCount * Mathf.Max(0.25f, multiplier / 4f));
        }

        public static int ApplyTimeOfDayMultiplier(Map map, int baseCount)
        {
            if (map?.skyManager == null)
            {
                return baseCount;
            }

            float skyGlow = map.skyManager.CurSkyGlow;
            if (skyGlow >= 0.60f)
            {
                return GenMath.RoundRandom(baseCount * 0.45f);
            }

            if (skyGlow <= 0.25f)
            {
                return GenMath.RoundRandom(baseCount * 1.20f);
            }

            if (skyGlow <= 0.40f)
            {
                return GenMath.RoundRandom(baseCount * 0.85f);
            }

            return baseCount;
        }

        public static bool IsNight(Map map)
        {
            return map?.skyManager != null && map.skyManager.CurSkyGlow <= 0.40f;
        }

        public static int GetCurrentZombieCount(Map map)
        {
            return map?.mapPawns?.AllPawnsSpawned?.Count(ZombieUtility.IsZombie) ?? 0;
        }

        public static int GetRemainingCapacity(Map map, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return 0;
            }

            int remaining = GetDynamicZombieCap(map, populationState) - GetCurrentZombieCount(map);
            return remaining > 0 ? remaining : 0;
        }

        public static int GetDynamicZombieCap(Map map, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return 0;
            }

            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            if (settings == null)
            {
                return 0;
            }

            if (!settings.useColonistScaledPopulation)
            {
                return GetLegacyDynamicZombieCap(map, populationState);
            }

            int colonists = map.mapPawns?.FreeColonistsSpawnedCount ?? 0;
            colonists = colonists < 1 ? 1 : colonists;
            ZombiePopulationState resolvedState = ResolvePopulationState(map, populationState);

            float multiplier = settings.DaytimeTargetMultiplier;
            switch (resolvedState)
            {
                case ZombiePopulationState.Night:
                    multiplier = settings.NighttimeTargetMultiplier;
                    break;
                case ZombiePopulationState.FullMoon:
                    multiplier = Mathf.Max(settings.NighttimeTargetMultiplier, Mathf.Max(1f, (settings.fullMoonColonistMultiplierMin + settings.fullMoonColonistMultiplierMax) * 0.5f));
                    break;
                case ZombiePopulationState.BloodMoon:
                    multiplier = Mathf.Max(settings.NighttimeTargetMultiplier, Mathf.Max(1f, (settings.bloodMoonColonistMultiplierMin + settings.bloodMoonColonistMultiplierMax) * 0.5f));
                    break;
            }

            int cap = Mathf.Max(1, Mathf.RoundToInt(colonists * multiplier));
            int hardCap = Mathf.Max(colonists, Mathf.RoundToInt(colonists * Mathf.Max(multiplier, settings.NighttimeTargetMultiplier) * 2f));
            return Mathf.Min(cap, hardCap);
        }

        private static int GetLegacyDynamicZombieCap(Map map, ZombiePopulationState populationState)
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            ZombiePopulationState resolvedState = ResolvePopulationState(map, populationState);
            if (resolvedState == ZombiePopulationState.FullMoon)
            {
                return settings != null ? settings.ScaleSpawnCountByOutbreak(settings.fullMoonBaseCount, 6) : 12;
            }

            if (resolvedState == ZombiePopulationState.BloodMoon)
            {
                return settings != null ? settings.ScaleSpawnCountByOutbreak(settings.bloodMoonBaseCount, 12) : 24;
            }

            int colonists = map.mapPawns?.FreeColonistsSpawnedCount ?? 0;
            colonists = colonists < 1 ? 1 : colonists;
            int currentDay = (Find.TickManager?.TicksGame ?? 0) / GenDate.TicksPerDay;
            int seed = (map.uniqueID * 397) ^ currentDay;
            System.Random random = new System.Random(seed);
            int multiplier = resolvedState == ZombiePopulationState.Night ? random.Next(3, 7) : random.Next(1, 4);
            int cap = colonists * multiplier;
            int hardCap = colonists * 10;
            return cap > hardCap ? hardCap : cap;
        }

        private static ZombiePopulationState ResolvePopulationState(Map map, ZombiePopulationState populationState)
        {
            if (populationState != ZombiePopulationState.Auto)
            {
                return populationState;
            }

            return IsNight(map) ? ZombiePopulationState.Night : ZombiePopulationState.Day;
        }

        private static void GetMultiplierRange(CustomizableZombieHordeSettings settings, ZombiePopulationState populationState, out int minMultiplier, out int maxMultiplier)
        {
            switch (populationState)
            {
                case ZombiePopulationState.BloodMoon:
                    minMultiplier = Mathf.Max(1, settings.bloodMoonColonistMultiplierMin);
                    maxMultiplier = Mathf.Max(minMultiplier, settings.bloodMoonColonistMultiplierMax);
                    break;
                case ZombiePopulationState.FullMoon:
                    minMultiplier = Mathf.Max(1, settings.fullMoonColonistMultiplierMin);
                    maxMultiplier = Mathf.Max(minMultiplier, settings.fullMoonColonistMultiplierMax);
                    break;
                case ZombiePopulationState.Night:
                    minMultiplier = Mathf.Max(1, settings.nightColonistMultiplierMin);
                    maxMultiplier = Mathf.Max(minMultiplier, settings.nightColonistMultiplierMax);
                    break;
                default:
                    minMultiplier = Mathf.Max(1, settings.dayColonistMultiplierMin);
                    maxMultiplier = Mathf.Max(minMultiplier, settings.dayColonistMultiplierMax);
                    break;
            }
        }

        private static int ClampSpawnCountToMapCap(Map map, int desiredCount, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return 0;
            }

            int remaining = GetRemainingCapacity(map, populationState);
            if (remaining <= 0)
            {
                return 0;
            }

            return desiredCount > remaining ? remaining : desiredCount;
        }

        private static int FinalizeSpawnCount(Map map, int count, bool applyDifficulty, bool applyTimeOfDay, bool ignoreCap, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (count < 1)
            {
                count = 1;
            }

            if (applyDifficulty)
            {
                count = ApplyDifficultyMultiplier(count);
            }

            if (applyTimeOfDay)
            {
                count = ApplyTimeOfDayMultiplier(map, count);
            }

            if (!ignoreCap)
            {
                count = ClampSpawnCountToMapCap(map, count, populationState);
            }

            return count;
        }

        public static bool SpawnByBehavior(Map map, ZombieSpawnEventType behavior, int? forcedCount = null, bool sendLetter = true, bool applyDifficulty = true, bool ignoreCap = false, bool ignoreTimeOfDay = false, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            switch (behavior)
            {
                case ZombieSpawnEventType.HuddledPack:
                    return SpawnHuddledPack(map, forcedCount, sendLetter, ignoreCap, ignoreTimeOfDay, applyDifficulty, populationState);
                case ZombieSpawnEventType.EdgeWander:
                    return SpawnEdgeWanderers(map, forcedCount, sendLetter, ignoreCap, ignoreTimeOfDay, applyDifficulty, populationState);
                case ZombieSpawnEventType.GroundBurst:
                    return SpawnGroundBurst(map, forcedCount, sendLetter, ignoreCap, ignoreTimeOfDay, allowCenterFallback: true, populationState: populationState);
                case ZombieSpawnEventType.Herd:
                    return SpawnHerd(map, forcedCount, sendLetter, ignoreCap: true, applyDifficulty: false, ignoreTimeOfDay: true);
                default:
                    return SpawnWave(map, forcedCount: forcedCount, sendLetter: sendLetter, applyDifficulty: applyDifficulty, ignoreCap: ignoreCap, ignoreTimeOfDay: ignoreTimeOfDay, behavior: ZombieSpawnEventType.AssaultBase, populationState: populationState);
            }
        }

        public static bool SpawnWave(Map map, Faction faction = null, int? forcedCount = null, bool sendLetter = true, string customLetterLabel = null, string customLetterText = null, bool applyDifficulty = true, bool ignoreCap = false, bool ignoreTimeOfDay = false, ZombieSpawnEventType behavior = ZombieSpawnEventType.AssaultBase, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return false;
            }

            faction ??= ZombieFactionUtility.GetOrCreateZombieFaction();

            IntVec3 edgeCell;
            if (!TryFindZombieEntryCell(map, out edgeCell))
            {
                edgeCell = FindAnyStandableCell(map);
                if (!edgeCell.IsValid)
                {
                    return false;
                }
            }

            int count = forcedCount ?? Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.minGroupSize, CustomizableZombieHordeMod.Settings.maxGroupSize);
            count = FinalizeSpawnCount(map, count, applyDifficulty, !ignoreTimeOfDay, ignoreCap, populationState);
            if (count < 1)
            {
                return false;
            }

            List<Pawn> pawns = SpawnPack(map, faction, count, behavior, edgeCell, canUseWater: true);
            if (pawns.Count == 0)
            {
                return SpawnEmergencyPack(map, count, sendLetter, customLetterLabel, customLetterText, ignoreCap: ignoreCap, behavior: behavior, populationState: populationState);
            }

            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                string label = customLetterLabel ?? (prefix + " Horde");
                string text = customLetterText ?? DescribeBehavior(prefix, behavior);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(edgeCell, map));
            }

            return true;
        }

        public static bool SpawnHuddledPack(Map map, int? forcedCount = null, bool sendLetter = true, bool ignoreCap = false, bool ignoreTimeOfDay = false, bool applyDifficulty = true, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
            return SpawnWave(map, forcedCount: forcedCount, sendLetter: sendLetter, customLetterLabel: prefix + " Huddle", customLetterText: "A knot of " + prefix.ToLowerInvariant() + "s has gathered just inside the map. They will bunch up first, then start drifting deeper across the map, so they can become a real threat if you leave them alone.", applyDifficulty: applyDifficulty, ignoreCap: ignoreCap, ignoreTimeOfDay: ignoreTimeOfDay, behavior: ZombieSpawnEventType.HuddledPack, populationState: populationState);
        }

        public static bool SpawnEdgeWanderers(Map map, int? forcedCount = null, bool sendLetter = true, bool ignoreCap = false, bool ignoreTimeOfDay = false, bool applyDifficulty = true, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
            return SpawnWave(map, forcedCount: forcedCount, sendLetter: sendLetter, customLetterLabel: prefix + " Edge Wanderers", customLetterText: "A band of " + prefix.ToLowerInvariant() + "s is slowly drifting along the edge of the map, probing the colony perimeter for an opening.", applyDifficulty: applyDifficulty, ignoreCap: ignoreCap, ignoreTimeOfDay: ignoreTimeOfDay, behavior: ZombieSpawnEventType.EdgeWander, populationState: populationState);
        }

        public static int GetRecommendedHerdCount(Map map)
        {
            int colonists = map?.mapPawns?.FreeColonistsSpawnedCount ?? 0;
            colonists = colonists < 1 ? 1 : colonists;
            return Mathf.Clamp(colonists * 10, 50, 75);
        }

        public static bool SpawnHerd(Map map, int? forcedCount = null, bool sendLetter = true, string customLetterLabel = null, string customLetterText = null, bool ignoreCap = true, bool applyDifficulty = false, bool ignoreTimeOfDay = true)
        {
            if (map == null)
            {
                return false;
            }

            int count = forcedCount ?? GetRecommendedHerdCount(map);
            count = Mathf.Clamp(count, 50, 75);
            count = FinalizeSpawnCount(map, count, applyDifficulty, !ignoreTimeOfDay, ignoreCap, ZombiePopulationState.Auto);
            count = Mathf.Clamp(count, 50, 75);
            if (count < 1)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (faction == null)
            {
                return false;
            }

            ZombieHerdDirection direction = GetRandomHerdDirection();
            List<IntVec3> lineCells = BuildHerdSpawnLine(map, direction, count);
            if (lineCells.Count == 0)
            {
                return false;
            }

            int spawnedCount = 0;
            IntVec3 firstCell = IntVec3.Invalid;
            int biterSpawnCount = 0;
            IntVec3 firstBiterSpawnCell = IntVec3.Invalid;
            List<Pawn> spawnedPawns = new List<Pawn>();

            for (int i = 0; i < lineCells.Count; i++)
            {
                PawnKindDef kind = ZombieKindSelector.GetRandomKind(map);
                Pawn pawn = ZombiePawnFactory.GenerateZombie(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                if (pawn.Faction == null)
                {
                    try
                    {
                        pawn.SetFactionDirect(faction);
                    }
                    catch
                    {
                    }
                }

                IntVec3 spawnCell = lineCells[i];
                if (!SpawnZombiePawn(map, pawn, spawnCell, ZombieSpawnEventType.Herd, direction))
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
                }

                if (!firstCell.IsValid)
                {
                    firstCell = spawnCell;
                }

                spawnedCount++;
                spawnedPawns.Add(pawn);
                if (ZombieUtility.IsVariant(pawn, ZombieVariant.Biter))
                {
                    biterSpawnCount++;
                    if (!firstBiterSpawnCell.IsValid)
                    {
                        firstBiterSpawnCell = spawnCell;
                    }
                }
            }

            TrySpawnRareRuntWithBiters(map, faction, biterSpawnCount, firstBiterSpawnCell, ZombieSpawnEventType.Herd, spawnedPawns);
            if (spawnedCount < 1)
            {
                return false;
            }

            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                string label = customLetterLabel ?? (prefix + " Herd");
                string text = customLetterText ?? "A broad herd of " + prefix.ToLowerInvariant() + "s is crossing the map in a rolling wall of bodies. They are trying to move through the area, not stage a clean raid, but anything too close can still get swallowed by the flow.";
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(firstCell.IsValid ? firstCell : map.Center, map));
            }

            return true;
        }

        public static bool SpawnHorde(Map map, int totalCount, int groups, string letterLabel, string letterText, bool ignoreCap = false, bool ignoreTimeOfDay = false, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return false;
            }

            int adjustedCount = FinalizeSpawnCount(map, totalCount, applyDifficulty: true, applyTimeOfDay: !ignoreTimeOfDay, ignoreCap: ignoreCap, populationState: populationState);
            groups = groups < 1 ? 1 : groups;
            int basePerGroup = adjustedCount / groups;
            int remainder = adjustedCount % groups;
            bool anySpawned = false;

            for (int i = 0; i < groups; i++)
            {
                int thisGroup = basePerGroup + (i < remainder ? 1 : 0);
                if (thisGroup < 1)
                {
                    thisGroup = 1;
                }

                if (SpawnWave(map, forcedCount: thisGroup, sendLetter: false, applyDifficulty: false, ignoreCap: ignoreCap, ignoreTimeOfDay: ignoreTimeOfDay, behavior: ZombieSpawnEventType.AssaultBase, populationState: populationState))
                {
                    anySpawned = true;
                }
            }

            if (anySpawned)
            {
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.ThreatBig, new TargetInfo(map.Center, map));
            }

            return anySpawned;
        }

        public static bool SpawnGroundBurst(Map map, int? forcedCount = null, bool sendLetter = true, bool ignoreCap = false, bool ignoreTimeOfDay = false, bool allowCenterFallback = true, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => cell.Standable(map) && !cell.Fogged(map)).ToList();
            if ((homeCells == null || homeCells.Count == 0) && allowCenterFallback)
            {
                homeCells = GenRadial.RadialCellsAround(map.Center, 12f, true)
                    .Where(cell => cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map))
                    .ToList();
            }

            if (homeCells == null || homeCells.Count == 0)
            {
                return false;
            }

            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            int groundBurstMin = settings?.GetEffectiveGroundBurstMinGroupSize() ?? 2;
            int groundBurstMax = settings?.GetEffectiveGroundBurstMaxGroupSize() ?? groundBurstMin;
            int count = forcedCount ?? Rand.RangeInclusive(groundBurstMin, groundBurstMax);
            count = FinalizeSpawnCount(map, count, applyDifficulty: true, applyTimeOfDay: !ignoreTimeOfDay, ignoreCap: ignoreCap, populationState: populationState);
            if (count < 1)
            {
                return false;
            }

            List<Pawn> pawns = new List<Pawn>();
            IntVec3 firstCell = IntVec3.Invalid;
            for (int i = 0; i < count; i++)
            {
                IntVec3 spawnCell = homeCells.RandomElement();
                if (!firstCell.IsValid)
                {
                    firstCell = spawnCell;
                }

                Pawn pawn = ZombiePawnFactory.GenerateZombie(ZombieKindSelector.GetRandomKind(map), faction);
                if (pawn == null)
                {
                    continue;
                }

                if (ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    pawn = ZombiePawnFactory.GenerateZombie(DefDatabase<PawnKindDef>.GetNamed("CZH_Zombie_Biter"), faction);
                    if (pawn == null)
                    {
                        continue;
                    }
                }

                if (SpawnZombiePawn(map, pawn, spawnCell, ZombieSpawnEventType.GroundBurst))
                {
                    pawns.Add(pawn);
                }
                else
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }

                for (int j = 0; j < 2; j++)
                {
                    FilthMaker.TryMakeFilth(spawnCell, map, ThingDefOf.Filth_Dirt);
                }
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                Find.LetterStack.ReceiveLetter(
                    prefix + " Ground Burst",
                    "The soil splits open inside your base and a small pack of " + prefix.ToLowerInvariant() + "s claws its way up through the floor. This one starts inside your walls, so move fast and keep civilians clear.",
                    LetterDefOf.ThreatSmall,
                    new TargetInfo(firstCell.IsValid ? firstCell : map.Center, map));
            }

            return true;
        }

        public static bool SpawnRandomGraveEvent(Map map, bool sendLetter = true)
        {
            if (map == null)
            {
                return false;
            }

            List<ZombieVariant> options = new List<ZombieVariant>();
            foreach (ZombieVariant variant in new[] { ZombieVariant.Biter, ZombieVariant.Runt, ZombieVariant.Boomer, ZombieVariant.Sick, ZombieVariant.Drowned, ZombieVariant.Brute, ZombieVariant.Grabber })
            {
                if (ZombieKindSelector.IsVariantEnabled(variant, map))
                {
                    options.Add(variant);
                }
            }

            if (options.Count == 0)
            {
                options.Add(ZombieVariant.Biter);
            }

            return SpawnVariantGraveEvent(map, options.RandomElement(), sendLetter);
        }

        public static bool SpawnVariantGraveEvent(Map map, ZombieVariant variant, bool sendLetter = true)
        {
            if (map == null)
            {
                return false;
            }

            ThingDef graveDef = GetGraveThingDef(variant);
            if (graveDef == null)
            {
                return false;
            }

            IntVec3 graveCell = FindGraveEventCell(map, graveDef);
            if (!graveCell.IsValid)
            {
                return false;
            }

            Thing grave = ThingMaker.MakeThing(graveDef);
            if (grave == null)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (faction != null)
            {
                try
                {
                    var setFactionTwoArgs = AccessTools.Method(grave.GetType(), "SetFaction", new[] { typeof(Faction), typeof(Pawn) });
                    if (setFactionTwoArgs != null)
                    {
                        setFactionTwoArgs.Invoke(grave, new object[] { faction, null });
                    }
                    else
                    {
                        var setFactionOneArg = AccessTools.Method(grave.GetType(), "SetFaction", new[] { typeof(Faction) });
                        if (setFactionOneArg != null)
                        {
                            setFactionOneArg.Invoke(grave, new object[] { faction });
                        }
                    }
                }
                catch
                {
                }
            }

            try
            {
                GenSpawn.Spawn(grave, graveCell, map, WipeMode.Vanish);
            }
            catch
            {
                grave.Destroy(DestroyMode.Vanish);
                return false;
            }

            ThingWithComps graveWithComps = grave as ThingWithComps;
            CompZombieGraveSpawner comp = graveWithComps?.GetComp<CompZombieGraveSpawner>();
            comp?.TriggerInitialBurst();

            if (sendLetter)
            {
                string label = ZombieDefUtility.GetGraveLetterLabel(variant);
                string text = ZombieDefUtility.GetGraveLetterText(variant);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(graveCell, map));
            }

            return true;
        }

        public static bool SpawnVariantPackAround(Map map, IntVec3 center, ZombieVariant variant, int count, ZombieSpawnEventType behavior = ZombieSpawnEventType.HuddledPack, bool ignoreCap = true, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null || !center.IsValid)
            {
                return false;
            }

            if (!ignoreCap)
            {
                count = ClampSpawnCountToMapCap(map, count, populationState);
            }

            if (count < 1)
            {
                return false;
            }

            PawnKindDef kind = ZombieKindSelector.GetKindForVariant(variant, map) ?? DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Biter");
            if (kind == null)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            bool anySpawned = false;
            int biterSpawnCount = 0;
            IntVec3 firstBiterSpawnCell = IntVec3.Invalid;
            for (int i = 0; i < count; i++)
            {
                Pawn pawn = ZombiePawnFactory.GenerateZombie(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                IntVec3 spawnCell = FindBurstCellNear(map, center, i == 0 ? 1 : 4, i == 0 ? 2 : 7);
                if (!spawnCell.IsValid)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
                }

                if (SpawnZombiePawn(map, pawn, spawnCell, behavior))
                {
                    anySpawned = true;
                    if (ZombieUtility.IsVariant(pawn, ZombieVariant.Biter))
                    {
                        biterSpawnCount++;
                        if (!firstBiterSpawnCell.IsValid)
                        {
                            firstBiterSpawnCell = spawnCell;
                        }
                    }

                    for (int j = 0; j < 2; j++)
                    {
                        FilthMaker.TryMakeFilth(spawnCell, map, ThingDefOf.Filth_Dirt);
                    }
                }
                else
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            TrySpawnRareRuntWithBiters(map, faction, biterSpawnCount, firstBiterSpawnCell, behavior);
            return anySpawned;
        }

        private static ThingDef GetGraveThingDef(ZombieVariant variant)
        {
            return ZombieVariantUtility.GetGraveThingDef(variant);
        }

        private static IntVec3 FindGraveEventCell(Map map, ThingDef graveDef)
        {
            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => CanPlaceGraveAt(cell, map, graveDef)).ToList();
            if (homeCells != null && homeCells.Count > 0)
            {
                for (int i = 0; i < 40; i++)
                {
                    IntVec3 candidate = homeCells.RandomElement();
                    if (DistanceToBaseCenter(candidate, map) <= 16f)
                    {
                        return candidate;
                    }
                }

                return homeCells.RandomElement();
            }

            List<IntVec3> centerCells = GenRadial.RadialCellsAround(map.Center, 14f, true)
                .Where(cell => CanPlaceGraveAt(cell, map, graveDef))
                .ToList();
            if (centerCells.Count > 0)
            {
                return centerCells.RandomElement();
            }

            if (graveDef != null)
            {
                for (int x = 0; x < map.Size.x; x++)
                {
                    for (int z = 0; z < map.Size.z; z++)
                    {
                        IntVec3 cell = new IntVec3(x, 0, z);
                        if (CanPlaceGraveAt(cell, map, graveDef))
                        {
                            return cell;
                        }
                    }
                }
            }

            return FindAnyStandableCell(map);
        }

        private static bool CanPlaceGraveAt(IntVec3 cell, Map map, ThingDef graveDef)
        {
            if (map == null || graveDef == null || !cell.InBounds(map))
            {
                return false;
            }

            CellRect occupiedRect = GenAdj.OccupiedRect(cell, Rot4.North, graveDef.Size);
            foreach (IntVec3 occupiedCell in occupiedRect)
            {
                if (!occupiedCell.InBounds(map) || !occupiedCell.Walkable(map) || occupiedCell.GetEdifice(map) != null)
                {
                    return false;
                }
            }

            return true;
        }

        private static float DistanceToBaseCenter(IntVec3 cell, Map map)
        {
            IntVec3 center = ZombieSpecialUtility.GetPlayerBaseCenter(map);
            if (!center.IsValid)
            {
                center = map.Center;
            }

            return cell.DistanceTo(center);
        }

        private static IntVec3 FindBurstCellNear(Map map, IntVec3 center, int minRadius, int maxRadius)
        {
            List<IntVec3> cells = GenRadial.RadialCellsAround(center, maxRadius, true)
                .Where(cell => cell.InBounds(map)
                    && cell.Walkable(map)
                    && cell.GetEdifice(map) == null
                    && cell.DistanceTo(center) >= minRadius)
                .ToList();
            if (cells.Count > 0)
            {
                return cells.RandomElement();
            }

            return CellFinder.RandomClosewalkCellNear(center, map, Mathf.Max(1, maxRadius));
        }

        public static bool SpawnEmergencyPack(Map map, int forcedCount, bool sendLetter = false, string customLetterLabel = null, string customLetterText = null, bool ignoreCap = false, ZombieSpawnEventType behavior = ZombieSpawnEventType.AssaultBase, ZombiePopulationState populationState = ZombiePopulationState.Auto)
        {
            if (map == null)
            {
                return false;
            }

            if (!ignoreCap)
            {
                forcedCount = ClampSpawnCountToMapCap(map, forcedCount, populationState);
            }

            if (forcedCount < 1)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            IntVec3 anchor = FindAnyStandableCellNearEdge(map, insideOnly: true);
            if (!anchor.IsValid)
            {
                anchor = FindAnyStandableCellNearEdge(map, insideOnly: false);
            }

            if (!anchor.IsValid)
            {
                anchor = FindAnyStandableCell(map);
            }

            if (!anchor.IsValid)
            {
                return false;
            }

            List<Pawn> pawns = SpawnPack(map, faction, forcedCount, behavior, anchor, canUseWater: true);
            if (pawns.Count == 0)
            {
                return false;
            }

            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                string label = customLetterLabel ?? (prefix + " Horde");
                string text = customLetterText ?? DescribeBehavior(prefix, behavior);
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(anchor, map));
            }

            return true;
        }

        private static List<Pawn> SpawnPack(Map map, Faction faction, int count, ZombieSpawnEventType behavior, IntVec3 anchor, bool canUseWater)
        {
            List<Pawn> pawns = new List<Pawn>();
            int biterSpawnCount = 0;
            IntVec3 firstBiterSpawnCell = IntVec3.Invalid;
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = ZombieKindSelector.GetRandomKind(map);
                Pawn pawn = ZombiePawnFactory.GenerateZombie(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                if (pawn.Faction == null && faction != null)
                {
                    try
                    {
                        pawn.SetFactionDirect(faction);
                    }
                    catch
                    {
                    }
                }

                IntVec3 spawnCell = ChooseSpawnCell(map, anchor, pawn, behavior, canUseWater);
                if (!spawnCell.IsValid)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
                }

                if (SpawnZombiePawn(map, pawn, spawnCell, behavior))
                {
                    pawns.Add(pawn);
                    if (ZombieUtility.IsVariant(pawn, ZombieVariant.Biter))
                    {
                        biterSpawnCount++;
                        if (!firstBiterSpawnCell.IsValid)
                        {
                            firstBiterSpawnCell = spawnCell;
                        }
                    }
                }
                else
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            TrySpawnRareRuntWithBiters(map, faction, biterSpawnCount, firstBiterSpawnCell, behavior, pawns);
            return pawns;
        }

        private static bool SpawnZombiePawn(Map map, Pawn pawn, IntVec3 spawnCell, ZombieSpawnEventType behavior, ZombieHerdDirection? herdDirection = null)
        {
            try
            {
                if (pawn == null)
                {
                    return false;
                }

                GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
                component?.RegisterBehavior(pawn, behavior);
                if (behavior == ZombieSpawnEventType.Herd && herdDirection.HasValue)
                {
                    component?.RegisterHerdDirection(pawn, herdDirection.Value);
                }
                ZombieUtility.PrepareSpawnedZombie(pawn);
                if (pawn.Downed)
                {
                    return false;
                }

                ZombieUtility.AssignInitialShambleJob(pawn, behavior);
                ZombieUtility.EnsureZombieAggression(pawn);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySpawnRareRuntWithBiters(Map map, Faction faction, int biterSpawnCount, IntVec3 anchorCell, ZombieSpawnEventType behavior, List<Pawn> spawnedPawns = null)
        {
            PawnKindDef runtKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Runt");
            if (map == null || faction == null || biterSpawnCount < 3 || !anchorCell.IsValid || runtKind == null)
            {
                return;
            }

            float spawnChance = Mathf.Clamp01(0.015f * biterSpawnCount);
            if (!Rand.Chance(spawnChance))
            {
                return;
            }

            try
            {
                Pawn runt = ZombiePawnFactory.GenerateZombie(runtKind, faction);
                if (runt == null)
                {
                    return;
                }

                IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(anchorCell, map, 4);
                if (!spawnCell.IsValid || !spawnCell.InBounds(map))
                {
                    spawnCell = anchorCell;
                }

                if (SpawnZombiePawn(map, runt, spawnCell, behavior))
                {
                    spawnedPawns?.Add(runt);
                }
                else
                {
                    runt.Destroy(DestroyMode.Vanish);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to spawn rare runt with biter group: {ex}");
            }
        }


        private static string DescribeBehavior(string prefix, ZombieSpawnEventType behavior)
        {
            switch (behavior)
            {
                case ZombieSpawnEventType.HuddledPack:
                    return "A knot of " + prefix.ToLowerInvariant() + "s has huddled together just inside the map, swaying and waiting to be stirred up. Keep noncombatants away until you decide how to engage it.";
                case ZombieSpawnEventType.EdgeWander:
                    return "A group of " + prefix.ToLowerInvariant() + "s is shambling around the approaches to your base, drifting between aimless wandering and sudden pushes inward.";
                case ZombieSpawnEventType.GroundBurst:
                    return "A pack of " + prefix.ToLowerInvariant() + "s has burst from the ground inside your colony. This breach began inside your defenses, so respond quickly.";
                case ZombieSpawnEventType.Herd:
                    return "A herd of " + prefix.ToLowerInvariant() + "s is crossing the map in a broad moving lane. It may ignore distant targets, but anything close to the flow can still get swallowed up.";
                default:
                    return "A group of " + prefix.ToLowerInvariant() + "s has drifted in from off map and is leaning toward your colony. Pull back exposed workers and set a firing line before they get close.";
            }
        }

        private static ZombieHerdDirection GetRandomHerdDirection()
        {
            int roll = Rand.RangeInclusive(0, 3);
            switch (roll)
            {
                case 0:
                    return ZombieHerdDirection.NorthToSouth;
                case 1:
                    return ZombieHerdDirection.SouthToNorth;
                case 2:
                    return ZombieHerdDirection.WestToEast;
                default:
                    return ZombieHerdDirection.EastToWest;
            }
        }

        private static List<IntVec3> BuildHerdSpawnLine(Map map, ZombieHerdDirection direction, int count)
        {
            List<IntVec3> cells = new List<IntVec3>();
            if (map == null || count < 1)
            {
                return cells;
            }

            int minMargin = 6;
            int maxX = map.Size.x - 7;
            int maxZ = map.Size.z - 7;
            bool vertical = direction == ZombieHerdDirection.NorthToSouth || direction == ZombieHerdDirection.SouthToNorth;
            int startFixed = direction == ZombieHerdDirection.NorthToSouth ? maxZ : direction == ZombieHerdDirection.SouthToNorth ? minMargin : direction == ZombieHerdDirection.WestToEast ? minMargin : maxX;
            int spanStart = minMargin;
            int spanEnd = vertical ? maxX : maxZ;
            float spanLength = Mathf.Max(1f, spanEnd - spanStart);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                int lane = Mathf.RoundToInt(spanStart + spanLength * t);
                int laneJitter = Rand.RangeInclusive(-2, 2);
                int inwardStagger = Rand.RangeInclusive(0, 3);
                int waveOffset = Mathf.RoundToInt(Mathf.Sin((i * 0.45f) + Rand.Value * 0.6f) * 2f);
                int resolvedLane = lane + laneJitter + waveOffset;
                IntVec3 ideal;
                if (vertical)
                {
                    int fixedZ = direction == ZombieHerdDirection.NorthToSouth ? startFixed - inwardStagger : startFixed + inwardStagger;
                    ideal = new IntVec3(Mathf.Clamp(resolvedLane, minMargin, maxX), 0, Mathf.Clamp(fixedZ, minMargin, maxZ));
                }
                else
                {
                    int fixedX = direction == ZombieHerdDirection.WestToEast ? startFixed + inwardStagger : startFixed - inwardStagger;
                    ideal = new IntVec3(Mathf.Clamp(fixedX, minMargin, maxX), 0, Mathf.Clamp(resolvedLane, minMargin, maxZ));
                }

                IntVec3 resolved = ResolveHerdSpawnCell(map, ideal, direction);
                if (resolved.IsValid && !cells.Contains(resolved))
                {
                    cells.Add(resolved);
                }
            }

            return cells;
        }

        private static IntVec3 ResolveHerdSpawnCell(Map map, IntVec3 ideal, ZombieHerdDirection direction)
        {
            if (ideal.InBounds(map) && ideal.Standable(map))
            {
                return ideal;
            }

            int primaryRadius = 8;
            for (int radius = 1; radius <= primaryRadius; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(ideal, radius, true))
                {
                    if (!cell.InBounds(map) || !cell.Standable(map))
                    {
                        continue;
                    }

                    bool stillNearEdge = direction == ZombieHerdDirection.NorthToSouth || direction == ZombieHerdDirection.SouthToNorth
                        ? Mathf.Abs(cell.z - ideal.z) <= 4
                        : Mathf.Abs(cell.x - ideal.x) <= 4;
                    if (stillNearEdge)
                    {
                        return cell;
                    }
                }
            }

            return CellFinder.RandomClosewalkCellNear(ideal, map, 10);
        }

        private static IntVec3 ChooseSpawnCell(Map map, IntVec3 edgeCell, Pawn pawn, ZombieSpawnEventType behavior, bool canUseWater)
        {
            if (canUseWater && ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
            {
                IntVec3 waterCell = FindWaterSpawnCell(map);
                if (waterCell.IsValid)
                {
                    return waterCell;
                }
            }

            if (behavior == ZombieSpawnEventType.HuddledPack)
            {
                IntVec3 huddleCell = ZombieSpecialUtility.FindInteriorNearEdgeCell(map, edgeCell);
                if (huddleCell.IsValid)
                {
                    return huddleCell;
                }
            }

            IntVec3 nearEdge = FindInsideNearEdgeSpawnCell(map, edgeCell);
            if (nearEdge.IsValid)
            {
                return nearEdge;
            }

            IntVec3 fallbackEdge = FindAnyStandableCellNearEdge(map, insideOnly: true);
            if (fallbackEdge.IsValid)
            {
                return fallbackEdge;
            }

            fallbackEdge = FindAnyStandableCellNearEdge(map, insideOnly: false);
            if (fallbackEdge.IsValid)
            {
                return fallbackEdge;
            }

            return FindAnyStandableCell(map);
        }

        private static IntVec3 FindInteriorApproachSpawnCell(Map map, IntVec3 preferredAnchor, Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 baseCenter = ZombieSpecialUtility.GetPlayerBaseCenter(map);
            if (baseCenter.IsValid)
            {
                List<IntVec3> baseBand = GenRadial.RadialCellsAround(baseCenter, 38f, true)
                    .Where(cell => cell.InBounds(map)
                        && cell.Standable(map)
                        && !cell.Fogged(map)
                        && ZombieSpecialUtility.DistanceToNearestEdge(cell, map) >= 12
                        && cell.DistanceToSquared(baseCenter) >= 12f * 12f)
                    .ToList();
                if (baseBand.Count > 0)
                {
                    return baseBand.RandomElement();
                }
            }

            IntVec3 wanderAnchor = ZombieSpecialUtility.FindEdgePatrolCell(pawn);
            if (wanderAnchor.IsValid && wanderAnchor.InBounds(map) && wanderAnchor.Standable(map) && ZombieSpecialUtility.DistanceToNearestEdge(wanderAnchor, map) >= 10)
            {
                return wanderAnchor;
            }

            List<IntVec3> interiorCells = map.AllCells
                .Where(cell => cell.Standable(map)
                    && !cell.Fogged(map)
                    && ZombieSpecialUtility.DistanceToNearestEdge(cell, map) >= 12)
                .ToList();
            if (interiorCells.Count > 0)
            {
                return interiorCells.RandomElement();
            }

            return FindAnyStandableCell(map);
        }

        private static bool TryFindZombieEntryCell(Map map, out IntVec3 edgeCell)
        {
            IntVec3 rawEdgeCell;
            if (RCellFinder.TryFindRandomPawnEntryCell(out rawEdgeCell, map, CellFinder.EdgeRoadChance_Ignore))
            {
                IntVec3 insideCell = FindInsideNearEdgeSpawnCell(map, rawEdgeCell);
                if (insideCell.IsValid)
                {
                    edgeCell = insideCell;
                    return true;
                }

                edgeCell = rawEdgeCell;
                return true;
            }

            List<IntVec3> fallbackCells = map.AllCells.Where(cell => IsInsideNearEdgeCell(cell, map) && cell.Standable(map) && !cell.Fogged(map)).ToList();
            if (fallbackCells.Count == 0)
            {
                fallbackCells = map.AllCells.Where(cell => IsInsideNearEdgeCell(cell, map) && cell.Standable(map)).ToList();
            }

            if (fallbackCells.Count > 0)
            {
                edgeCell = fallbackCells.RandomElement();
                return true;
            }

            fallbackCells = map.AllCells.Where(cell => IsEdgeCell(cell, map) && cell.Standable(map) && !cell.Fogged(map)).ToList();
            if (fallbackCells.Count == 0)
            {
                fallbackCells = map.AllCells.Where(cell => IsEdgeCell(cell, map) && cell.Standable(map)).ToList();
            }

            if (fallbackCells.Count > 0)
            {
                edgeCell = fallbackCells.RandomElement();
                return true;
            }

            edgeCell = IntVec3.Invalid;
            return false;
        }

        private static bool IsEdgeCell(IntVec3 cell, Map map)
        {
            return cell.x <= 1 || cell.z <= 1 || cell.x >= map.Size.x - 2 || cell.z >= map.Size.z - 2;
        }

        private static IntVec3 FindWaterSpawnCell(Map map)
        {
            List<IntVec3> waterCells = map.AllCells.Where(cell => ZombieUtility.IsWaterCell(cell, map) && cell.Walkable(map) && !cell.Fogged(map)).ToList();
            if (waterCells.Count == 0)
            {
                waterCells = map.AllCells.Where(cell => ZombieUtility.IsWaterCell(cell, map) && cell.Walkable(map)).ToList();
            }

            return waterCells.Count > 0 ? waterCells.RandomElement() : IntVec3.Invalid;
        }

        private static IntVec3 FindAnyStandableCellNearEdge(Map map, bool insideOnly)
        {
            List<IntVec3> nearEdgeCells = map.AllCells.Where(cell => cell.Standable(map) && !cell.Fogged(map) && (insideOnly ? IsInsideNearEdgeCell(cell, map) : (DistanceToEdge(cell, map) >= 5 && DistanceToEdge(cell, map) <= 18))).ToList();
            if (nearEdgeCells.Count == 0)
            {
                nearEdgeCells = map.AllCells.Where(cell => cell.Standable(map) && (insideOnly ? IsInsideNearEdgeCell(cell, map) : (DistanceToEdge(cell, map) >= 5 && DistanceToEdge(cell, map) <= 18))).ToList();
            }

            return nearEdgeCells.Count > 0 ? nearEdgeCells.RandomElement() : IntVec3.Invalid;
        }

        private static IntVec3 FindInsideNearEdgeSpawnCell(Map map, IntVec3 preferredAnchor)
        {
            List<IntVec3> nearPreferred = GenRadial.RadialCellsAround(preferredAnchor, 12f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && !cell.Fogged(map) && IsInsideNearEdgeCell(cell, map))
                .ToList();
            if (nearPreferred.Count > 0)
            {
                return nearPreferred.RandomElement();
            }

            nearPreferred = GenRadial.RadialCellsAround(preferredAnchor, 12f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && IsInsideNearEdgeCell(cell, map))
                .ToList();
            if (nearPreferred.Count > 0)
            {
                return nearPreferred.RandomElement();
            }

            return FindAnyStandableCellNearEdge(map, insideOnly: true);
        }

        private static bool IsInsideNearEdgeCell(IntVec3 cell, Map map)
        {
            int distance = DistanceToEdge(cell, map);
            return distance >= 6 && distance <= 18;
        }

        private static IntVec3 FindAnyStandableCell(Map map)
        {
            List<IntVec3> cells = map.AllCells.Where(cell => cell.Standable(map) && !cell.Fogged(map)).ToList();
            if (cells.Count == 0)
            {
                cells = map.AllCells.Where(cell => cell.Standable(map)).ToList();
            }

            return cells.Count > 0 ? cells.RandomElement() : IntVec3.Invalid;
        }

        private static int DistanceToEdge(IntVec3 cell, Map map)
        {
            int xDistance = cell.x < map.Size.x - 1 - cell.x ? cell.x : map.Size.x - 1 - cell.x;
            int zDistance = cell.z < map.Size.z - 1 - cell.z ? cell.z : map.Size.z - 1 - cell.z;
            return xDistance < zDistance ? xDistance : zDistance;
        }
    }
}
