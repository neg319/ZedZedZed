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
                return GenMath.RoundRandom(baseCount * 0.50f);
            }

            if (skyGlow <= 0.25f)
            {
                return GenMath.RoundRandom(baseCount * 1.50f);
            }

            if (skyGlow <= 0.40f)
            {
                return GenMath.RoundRandom(baseCount * 1.15f);
            }

            return baseCount;
        }

        public static bool SpawnWave(Map map, Faction faction = null, int? forcedCount = null, bool sendLetter = true, string customLetterLabel = null, string customLetterText = null, bool applyDifficulty = true)
        {
            if (map == null || map.Parent == null)
            {
                return false;
            }

            faction ??= ZombieFactionUtility.GetOrCreateZombieFaction();
            if (faction == null)
            {
                return false;
            }

            IntVec3 edgeCell;
            if (!TryFindZombieEntryCell(map, out edgeCell))
            {
                return false;
            }

            int count = forcedCount ?? Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.minGroupSize, CustomizableZombieHordeMod.Settings.maxGroupSize);
            if (applyDifficulty)
            {
                count = ApplyDifficultyMultiplier(count);
            }
            count = ApplyTimeOfDayMultiplier(map, count);
            if (count < 1)
            {
                count = 1;
            }

            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                PawnKindDef kind = ZombieKindSelector.GetRandomKind(map);
                Pawn pawn = ZombiePawnFactory.GenerateZombie(kind, faction);

                IntVec3 spawnCell = edgeCell;
                if (ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
                {
                    IntVec3 waterCell = FindWaterSpawnCell(map);
                    if (waterCell.IsValid)
                    {
                        spawnCell = waterCell;
                    }
                    else
                    {
                        pawn.Destroy(DestroyMode.Vanish);
                        pawn = ZombiePawnFactory.GenerateZombie(DefDatabase<PawnKindDef>.GetNamed("CZH_Zombie_Biter"), faction);
                        spawnCell = CellFinder.RandomSpawnCellForPawnNear(edgeCell, map, 8);
                    }
                }
                else
                {
                    spawnCell = CellFinder.RandomSpawnCellForPawnNear(edgeCell, map, 8);
                }

                GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                pawns.Add(pawn);
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            LordJob lordJob = new LordJob_AssaultColony(faction, false, false, false, false, false);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);

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
            if (map == null || map.Parent == null)
            {
                return false;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (faction == null)
            {
                return false;
            }

            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => cell.Standable(map) && !cell.Fogged(map)).ToList();
            if (homeCells == null || homeCells.Count == 0)
            {
                return false;
            }

            int count = forcedCount ?? Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.groundBurstMinGroupSize, CustomizableZombieHordeMod.Settings.groundBurstMaxGroupSize);
            count = ApplyDifficultyMultiplier(count);
            count = ApplyTimeOfDayMultiplier(map, count);
            if (count < 1)
            {
                count = 1;
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
                if (ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned))
                {
                    pawn.Destroy(DestroyMode.Vanish);
                    pawn = ZombiePawnFactory.GenerateZombie(DefDatabase<PawnKindDef>.GetNamed("CZH_Zombie_Biter"), faction);
                }

                GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                pawns.Add(pawn);

                for (int j = 0; j < 2; j++)
                {
                    FilthMaker.TryMakeFilth(spawnCell, map, ThingDefOf.Filth_Dirt);
                }
            }

            if (pawns.Count == 0)
            {
                return false;
            }

            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, false, false, false, false, false), map, pawns);
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


        private static bool TryFindZombieEntryCell(Map map, out IntVec3 edgeCell)
        {
            if (RCellFinder.TryFindRandomPawnEntryCell(out edgeCell, map, CellFinder.EdgeRoadChance_Ignore))
            {
                return true;
            }

            List<IntVec3> fallbackCells = map.AllCells.Where(cell => IsEdgeCell(cell, map) && cell.Standable(map) && !cell.Fogged(map)).ToList();
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
            return cell.x <= 0 || cell.z <= 0 || cell.x >= map.Size.x - 1 || cell.z >= map.Size.z - 1;
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
    }
}
