using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieSpecialUtility
    {
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
                    if (things[i] is Pawn target && target != pawn)
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
                if (things[i] is Pawn target && target != source && ZombieTraitUtility.CanCatchZombieSickness(target) && Rand.Chance(chance))
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
            if (NearbyLivingPawnDetected(pawn, 12f))
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
