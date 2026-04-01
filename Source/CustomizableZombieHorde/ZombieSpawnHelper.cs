using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace CustomizableZombieHorde
{
    public static class ZombieSpawnHelper
    {
        public static int ApplyDifficultyMultiplier(int baseCount)
        {
            float multiplier = CustomizableZombieHordeMod.Settings?.DifficultyMultiplier ?? 1f;
            return baseCount < 1 ? 1 : GenMath.RoundRandom(baseCount * multiplier);
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

        public static int GetDynamicZombieCap(Map map)
        {
            if (map == null)
            {
                return 0;
            }

            int colonists = map.mapPawns?.FreeColonistsSpawnedCount ?? 0;
            colonists = colonists < 1 ? 1 : colonists;
            int currentDay = (Find.TickManager?.TicksGame ?? 0) / GenDate.TicksPerDay;
            int seed = (map.uniqueID * 397) ^ currentDay;
            System.Random random = new System.Random(seed);
            int multiplier;

            float skyGlow = map.skyManager?.CurSkyGlow ?? 0.5f;
            if (skyGlow >= 0.60f)
            {
                multiplier = random.Next(1, 4);
            }
            else if (skyGlow <= 0.25f)
            {
                multiplier = random.Next(3, 7);
            }
            else
            {
                multiplier = random.Next(2, 5);
            }

            int maxAllowed = colonists * multiplier;
            int hardCap = colonists * 10;
            return maxAllowed > hardCap ? hardCap : maxAllowed;
        }

        private static int ClampSpawnCountToMapCap(Map map, int desiredCount)
        {
            if (map == null)
            {
                return 0;
            }

            int currentZombies = map.mapPawns?.AllPawnsSpawned?.Count(ZombieUtility.IsZombie) ?? 0;
            int remaining = GetDynamicZombieCap(map) - currentZombies;
            if (remaining <= 0)
            {
                return 0;
            }

            return desiredCount > remaining ? remaining : desiredCount;
        }

        public static bool SpawnWave(Map map, Faction faction = null, int? forcedCount = null, bool sendLetter = true, string customLetterLabel = null, string customLetterText = null, bool applyDifficulty = true)
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
            if (applyDifficulty)
            {
                count = ApplyDifficultyMultiplier(count);
            }
            count = ApplyTimeOfDayMultiplier(map, count);
            count = ClampSpawnCountToMapCap(map, count);
            if (count < 1)
            {
                return false;
            }

            List<Pawn> pawns = new List<Pawn>();
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

                IntVec3 spawnCell = ChooseSpawnCell(map, edgeCell, pawn);
                if (!spawnCell.IsValid)
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
                }

                try
                {
                    GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    pawns.Add(pawn);
                }
                catch
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            if (pawns.Count == 0)
            {
                return SpawnEmergencyPack(map, count, sendLetter, customLetterLabel, customLetterText);
            }

            TryAssignAssaultLordOrAggression(faction, map, pawns);

            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                string label = customLetterLabel ?? (prefix + " Horde");
                string text = customLetterText ?? ("A group of " + prefix.ToLowerInvariant() + "s has entered from the map edge and is converging on your colony.");
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(edgeCell, map));
            }

            return true;
        }

        public static bool SpawnHorde(Map map, int totalCount, int groups, string letterLabel, string letterText)
        {
            if (map == null)
            {
                return false;
            }

            int adjustedCount = ApplyDifficultyMultiplier(totalCount);
            adjustedCount = ApplyTimeOfDayMultiplier(map, adjustedCount);
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

                if (SpawnWave(map, forcedCount: thisGroup, sendLetter: false, applyDifficulty: false))
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

        public static bool SpawnGroundBurst(Map map, int? forcedCount = null, bool sendLetter = true)
        {
            if (map == null)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => cell.Standable(map) && !cell.Fogged(map)).ToList();
            if (homeCells == null || homeCells.Count == 0)
            {
                return false;
            }

            int count = forcedCount ?? Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.groundBurstMinGroupSize, CustomizableZombieHordeMod.Settings.groundBurstMaxGroupSize);
            count = ApplyDifficultyMultiplier(count);
            count = ApplyTimeOfDayMultiplier(map, count);
            count = ClampSpawnCountToMapCap(map, count);
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

                try
                {
                    GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    pawns.Add(pawn);
                }
                catch
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    continue;
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

            TryAssignAssaultLordOrAggression(faction, map, pawns);
            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                Find.LetterStack.ReceiveLetter(
                    prefix + " Ground Burst",
                    "The soil splits open inside your base and a small pack of " + prefix.ToLowerInvariant() + "s claws its way to the surface.",
                    LetterDefOf.ThreatSmall,
                    new TargetInfo(firstCell.IsValid ? firstCell : map.Center, map));
            }

            return true;
        }

        public static bool SpawnEmergencyPack(Map map, int forcedCount, bool sendLetter = false, string customLetterLabel = null, string customLetterText = null)
        {
            if (map == null)
            {
                return false;
            }

            forcedCount = ClampSpawnCountToMapCap(map, forcedCount);
            if (forcedCount < 1)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            List<Pawn> pawns = new List<Pawn>();
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

            for (int i = 0; i < forcedCount; i++)
            {
                PawnKindDef kind = ZombieKindSelector.GetRandomKind(map);
                Pawn pawn = ZombiePawnFactory.GenerateZombie(kind, faction);
                if (pawn == null)
                {
                    continue;
                }

                IntVec3 spawnCell = FindInsideNearEdgeSpawnCell(map, anchor);
                if (!spawnCell.IsValid)
                {
                    spawnCell = anchor;
                }

                try
                {
                    GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    pawns.Add(pawn);
                }
                catch
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            TryAssignAssaultLordOrAggression(faction, map, pawns);
            if (sendLetter)
            {
                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                string label = customLetterLabel ?? (prefix + " Horde");
                string text = customLetterText ?? ("A pack of " + prefix.ToLowerInvariant() + "s shambles onto the map and lurches toward your base.");
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(anchor, map));
            }

            return true;
        }

        private static void TryAssignAssaultLordOrAggression(Faction faction, Map map, List<Pawn> pawns)
        {
            if (pawns == null || pawns.Count == 0)
            {
                return;
            }

            foreach (Pawn pawn in pawns)
            {
                ZombieUtility.EnsureZombieAggression(pawn);
            }
        }

        private static IntVec3 ChooseSpawnCell(Map map, IntVec3 edgeCell, Pawn pawn)
        {
            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
            {
                IntVec3 waterCell = FindWaterSpawnCell(map);
                if (waterCell.IsValid)
                {
                    return waterCell;
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
            List<IntVec3> nearEdgeCells = map.AllCells.Where(cell => cell.Standable(map) && !cell.Fogged(map) && (insideOnly ? IsInsideNearEdgeCell(cell, map) : DistanceToEdge(cell, map) <= 8)).ToList();
            if (nearEdgeCells.Count == 0)
            {
                nearEdgeCells = map.AllCells.Where(cell => cell.Standable(map) && (insideOnly ? IsInsideNearEdgeCell(cell, map) : DistanceToEdge(cell, map) <= 8)).ToList();
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
            return distance >= 1 && distance <= 8;
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
