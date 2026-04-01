using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieSpecialUtility
    {
        public static Pawn FindClosestLivingPrey(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Pawn best = null;
            float bestDistance = float.MaxValue;
            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (other == pawn || other.Dead || other.Destroyed || !other.RaceProps.IsFlesh || ZombieUtility.ShouldZombiesIgnore(other))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(other.PositionHeld);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                if (!GenSight.LineOfSight(pawn.PositionHeld, other.PositionHeld, pawn.MapHeld))
                {
                    continue;
                }

                best = other;
                bestDistance = distance;
            }

            return best;
        }

        public static Corpse FindNearbyFreshCorpse(Pawn pawn, float radius)
        {
            if (pawn?.MapHeld == null)
            {
                return null;
            }

            float radiusSquared = radius * radius;
            Corpse best = null;
            float bestDistance = float.MaxValue;
            foreach (Corpse corpse in pawn.MapHeld.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
            {
                if (corpse.Destroyed || ZombieUtility.IsZombie(corpse.InnerPawn))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                best = corpse;
                bestDistance = distance;
            }

            return best;
        }

        public static IntVec3 FindHordeShambleCell(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            Map map = pawn.MapHeld;
            List<Pawn> nearbyZombies = map.mapPawns.AllPawnsSpawned
                .Where(other => other != pawn && ZombieUtility.IsZombie(other) && !other.Dead && !other.Destroyed && other.PositionHeld.DistanceToSquared(pawn.PositionHeld) <= 225f)
                .ToList();

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            bool huddle = ((pawn.thingIDNumber / 7) + (ticksGame / 1800)) % 2 == 0;
            if (nearbyZombies.Count >= 2 && huddle)
            {
                int sumX = pawn.PositionHeld.x;
                int sumZ = pawn.PositionHeld.z;
                int count = 1;
                foreach (Pawn zombie in nearbyZombies)
                {
                    count++;
                    sumX += zombie.PositionHeld.x;
                    sumZ += zombie.PositionHeld.z;
                }

                IntVec3 packCenter = new IntVec3(sumX / count, 0, sumZ / count);
                for (int i = 0; i < 12; i++)
                {
                    IntVec3 candidate = CellFinder.RandomClosewalkCellNear(packCenter, map, 3);
                    if (candidate.IsValid && candidate.Standable(map))
                    {
                        return candidate;
                    }
                }
            }

            return FindEdgeShuffleCell(pawn);
        }

        private static IntVec3 FindEdgeShuffleCell(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            List<IntVec3> options = GenRadial.RadialCellsAround(pawn.PositionHeld, 10f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToEdge(cell, map) >= 2 && DistanceToEdge(cell, map) <= 8)
                .ToList();

            if (options.Count > 0)
            {
                int step = ((pawn.thingIDNumber / 11) + ((Find.TickManager?.TicksGame ?? 0) / 2500)) % options.Count;
                return options[step];
            }

            return FindAnyNearEdgeCell(map);
        }

        private static IntVec3 FindAnyNearEdgeCell(Map map)
        {
            foreach (IntVec3 cell in map.AllCells.InRandomOrder())
            {
                if (cell.Standable(map) && DistanceToEdge(cell, map) >= 2 && DistanceToEdge(cell, map) <= 8)
                {
                    return cell;
                }
            }

            return map?.Center ?? IntVec3.Invalid;
        }

        private static int DistanceToEdge(IntVec3 cell, Map map)
        {
            int xDistance = cell.x < map.Size.x - 1 - cell.x ? cell.x : map.Size.x - 1 - cell.x;
            int zDistance = cell.z < map.Size.z - 1 - cell.z ? cell.z : map.Size.z - 1 - cell.z;
            return xDistance < zDistance ? xDistance : zDistance;
        }

        public static void HandleCorpseFeeding(Map map)
        {
            if (map == null)
            {
                return;
            }

            List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
            foreach (Corpse corpse in corpses)
            {
                if (corpse.Destroyed || ZombieUtility.IsZombie(corpse.InnerPawn))
                {
                    continue;
                }

                int nearbyZombies = map.mapPawns.AllPawnsSpawned.Count(pawn => ZombieUtility.IsZombie(pawn) && !pawn.Dead && !pawn.Destroyed && pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld) <= 2.9f * 2.9f);
                if (nearbyZombies <= 0)
                {
                    continue;
                }

                for (int i = 0; i < nearbyZombies; i++)
                {
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, map, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                }

                if (Rand.Chance(0.10f * nearbyZombies))
                {
                    corpse.Destroy(DestroyMode.Vanish);
                }
            }
        }

        public static bool MapHasWater(Map map)
        {
            if (map == null)
            {
                return false;
            }

            foreach (IntVec3 cell in map.AllCells)
            {
                if (ZombieUtility.IsWaterCell(cell, map))
                {
                    return true;
                }
            }

            return false;
        }

        public static void DropZombieBlood(Pawn pawn)
        {
            if (pawn?.MapHeld == null)
            {
                return;
            }

            ThingDef filth = ZombieDefOf.CZH_Filth_ZombieBlood;
            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Sick))
            {
                filth = ZombieDefOf.CZH_Filth_SickZombieBlood;
            }

            if (filth != null)
            {
                FilthMaker.TryMakeFilth(pawn.PositionHeld, pawn.MapHeld, filth);
            }
        }

        public static void HandleZombieDeathEffects(Pawn pawn)
        {
            if (!ZombieUtility.IsZombie(pawn) || pawn.MapHeld == null)
            {
                return;
            }

            DropZombieBlood(pawn);

            switch (ZombieUtility.GetVariant(pawn))
            {
                case ZombieVariant.Boomer:
                    DoAcidBurst(pawn);
                    break;
                case ZombieVariant.Sick:
                    DoSicknessBurst(pawn);
                    break;
            }
        }

        public static void DoAcidBurst(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            IntVec3 center = pawn.PositionHeld;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2.9f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, map, ZombieDefOf.CZH_Filth_ZombieAcid);
                List<Thing> things = cell.GetThingList(map);
                for (int i = 0; i < things.Count; i++)
                {
                    if (things[i] is Pawn target && target != pawn && !ZombieUtility.IsZombie(target))
                    {
                        float damage = Rand.Range(7f, 12f);
                        target.TakeDamage(new DamageInfo(ZombieDefOf.CZH_ZombieAcidBurn ?? DamageDefOf.Burn, damage, 0f, -1f, pawn));
                    }
                }
            }
        }

        public static void DoSicknessBurst(Pawn pawn)
        {
            Map map = pawn.MapHeld;
            IntVec3 center = pawn.PositionHeld;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, 2.4f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                FilthMaker.TryMakeFilth(cell, map, ZombieDefOf.CZH_Filth_SickZombieBlood);
                TryApplySicknessToLivingThings(cell, map, pawn, 0.22f);
            }
        }

        public static void HandleSickBloodContact(Map map)
        {
            if (map?.mapPawns?.AllPawnsSpawned == null)
            {
                return;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.Dead || pawn.Destroyed || !pawn.RaceProps.IsFlesh || ZombieUtility.IsZombie(pawn))
                {
                    continue;
                }

                if (!CellContainsFilthDef(pawn.PositionHeld, map, ZombieDefOf.CZH_Filth_SickZombieBlood))
                {
                    continue;
                }

                if (ZombieTraitUtility.CanCatchZombieSickness(pawn) && Rand.Chance(0.08f))
                {
                    pawn.health.AddHediff(ZombieDefOf.CZH_ZombieSickness);
                }
            }
        }

        private static void TryApplySicknessToLivingThings(IntVec3 cell, Map map, Pawn source, float chance)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn target && target != source && !ZombieUtility.IsZombie(target) && ZombieTraitUtility.CanCatchZombieSickness(target) && Rand.Chance(chance))
                {
                    target.health.AddHediff(ZombieDefOf.CZH_ZombieSickness);
                }
            }
        }

        private static bool CellContainsFilthDef(IntVec3 cell, Map map, ThingDef filthDef)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i]?.def == filthDef)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<Thing> BuildZombieButcherProducts(Pawn zombie)
        {
            List<Thing> things = new List<Thing>();
            if (!ZombieUtility.IsZombie(zombie))
            {
                return things;
            }

            Thing flesh = ThingMaker.MakeThing(ZombieDefOf.CZH_RottenFlesh);
            if (flesh != null)
            {
                flesh.stackCount = ZombieUtility.IsVariant(zombie, ZombieVariant.Tank) ? 55 : 28;
                things.Add(flesh);
            }

            Thing leather = ThingMaker.MakeThing(ZombieDefOf.CZH_RottenLeather);
            if (leather != null)
            {
                leather.stackCount = ZombieUtility.IsVariant(zombie, ZombieVariant.Tank) ? 34 : 18;
                things.Add(leather);
            }

            return things;
        }

        public static bool NearbyLivingPawnDetected(Pawn pawn, float radius = 12f)
        {
            if (pawn?.MapHeld?.mapPawns?.AllPawnsSpawned == null)
            {
                return false;
            }

            float radiusSquared = radius * radius;
            foreach (Pawn other in pawn.MapHeld.mapPawns.AllPawnsSpawned)
            {
                if (other == pawn || other.Dead || other.Destroyed || !other.RaceProps.IsFlesh || ZombieUtility.IsZombie(other) || ZombieUtility.ShouldZombiesIgnore(other))
                {
                    continue;
                }

                if (pawn.PositionHeld.DistanceToSquared(other.PositionHeld) <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        public static void HandleDrownedBehavior(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || !pawn.Spawned)
            {
                return;
            }

            ZombieUtility.RefreshDrownedState(pawn);
            if (NearbyLivingPawnDetected(pawn, 8f))
            {
                return;
            }

            if (ZombieUtility.IsWaterCell(pawn.PositionHeld, pawn.MapHeld))
            {
                return;
            }

            IntVec3 nearestWater = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 24);
            if (nearestWater.IsValid)
            {
                pawn.jobs?.StopAll();
                pawn.pather?.StartPath(nearestWater, PathEndMode.OnCell);
            }
        }

        private static IntVec3 FindNearestWaterCell(IntVec3 origin, Map map, int maxRadius)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestDistance = 999999f;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, maxRadius, true))
            {
                if (!cell.InBounds(map) || !ZombieUtility.IsWaterCell(cell, map) || !cell.Walkable(map))
                {
                    continue;
                }

                float distance = origin.DistanceToSquared(cell);
                if (distance < bestDistance)
                {
                    best = cell;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
