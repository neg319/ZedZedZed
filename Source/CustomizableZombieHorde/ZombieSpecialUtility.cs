using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieSpecialUtility
    {

        public static IEnumerable<Thing> BuildZombieButcherProducts(Pawn pawn)
        {
            List<Thing> result = new List<Thing>();
            if (pawn == null)
            {
                return result;
            }

            int fleshCount = ZombieUtility.IsVariant(pawn, ZombieVariant.Tank) ? 18 : ZombieUtility.IsVariant(pawn, ZombieVariant.Crawler) ? 5 : 9;
            int leatherCount = ZombieUtility.IsVariant(pawn, ZombieVariant.Tank) ? 14 : ZombieUtility.IsVariant(pawn, ZombieVariant.Crawler) ? 4 : 7;

            if (ZombieDefOf.CZH_RottenFlesh != null && fleshCount > 0)
            {
                Thing flesh = ThingMaker.MakeThing(ZombieDefOf.CZH_RottenFlesh);
                flesh.stackCount = fleshCount;
                result.Add(flesh);
            }

            if (ZombieDefOf.CZH_RottenLeather != null && leatherCount > 0)
            {
                Thing leather = ThingMaker.MakeThing(ZombieDefOf.CZH_RottenLeather);
                leather.stackCount = leatherCount;
                result.Add(leather);
            }

            return result;
        }

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

        public static IntVec3 FindInitialBehaviorCell(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            switch (behavior)
            {
                case ZombieSpawnEventType.EdgeWander:
                    return FindEdgePatrolCell(pawn);
                case ZombieSpawnEventType.HuddledPack:
                    return FindHuddleCell(pawn);
                case ZombieSpawnEventType.GroundBurst:
                    return FindAssaultCell(pawn);
                default:
                    return FindAssaultCell(pawn);
            }
        }

        public static IntVec3 FindBehaviorCell(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn?.MapHeld == null)
            {
                return IntVec3.Invalid;
            }

            switch (behavior)
            {
                case ZombieSpawnEventType.EdgeWander:
                    return FindEdgePatrolCell(pawn);
                case ZombieSpawnEventType.HuddledPack:
                    return FindHuddleCell(pawn);
                case ZombieSpawnEventType.GroundBurst:
                    return FindAssaultCell(pawn);
                default:
                    return FindAssaultCell(pawn);
            }
        }

        public static IntVec3 FindAssaultCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            IntVec3 targetCenter = GetPlayerBaseCenter(map);
            IntVec3 from = pawn.PositionHeld;
            IntVec3 anchor = new IntVec3((from.x + targetCenter.x) / 2, 0, (from.z + targetCenter.z) / 2);
            List<IntVec3> options = GenRadial.RadialCellsAround(anchor, 8f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6)
                .ToList();
            if (options.Count > 0)
            {
                return options.RandomElement();
            }

            return targetCenter;
        }

        public static IntVec3 FindHuddleCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<Pawn> nearbyZombies = map.mapPawns.AllPawnsSpawned
                .Where(other => other != pawn && ZombieUtility.IsZombie(other) && !other.Dead && !other.Destroyed && other.PositionHeld.DistanceToSquared(pawn.PositionHeld) <= 225f)
                .ToList();

            IntVec3 packCenter = pawn.PositionHeld;
            if (nearbyZombies.Count > 0)
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

                packCenter = new IntVec3(sumX / count, 0, sumZ / count);
            }
            else if (DistanceToNearestEdge(packCenter, map) < 7)
            {
                packCenter = FindInteriorNearEdgeCell(map, pawn.PositionHeld);
            }

            for (int i = 0; i < 12; i++)
            {
                IntVec3 candidate = CellFinder.RandomClosewalkCellNear(packCenter, map, 4);
                if (candidate.IsValid && candidate.Standable(map) && DistanceToNearestEdge(candidate, map) >= 6)
                {
                    return candidate;
                }
            }

            return FindInteriorNearEdgeCell(map, packCenter);
        }

        public static IntVec3 FindEdgePatrolCell(Pawn pawn)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<IntVec3> options = GenRadial.RadialCellsAround(pawn.PositionHeld, 14f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 16)
                .ToList();

            if (options.Count > 0)
            {
                int step = ((pawn.thingIDNumber / 11) + ((Find.TickManager?.TicksGame ?? 0) / 3200)) % options.Count;
                return options[step];
            }

            return FindInteriorNearEdgeCell(map, pawn.PositionHeld);
        }

        public static IntVec3 FindInteriorNearEdgeCell(Map map, IntVec3 anchor)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<IntVec3> options = GenRadial.RadialCellsAround(anchor, 16f, true)
                .Where(cell => cell.InBounds(map) && cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 18)
                .ToList();
            if (options.Count > 0)
            {
                return options.RandomElement();
            }

            foreach (IntVec3 cell in map.AllCells.InRandomOrder())
            {
                if (cell.Standable(map) && DistanceToNearestEdge(cell, map) >= 6 && DistanceToNearestEdge(cell, map) <= 18)
                {
                    return cell;
                }
            }

            return map.Center;
        }

        public static IntVec3 GetPlayerBaseCenter(Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            List<IntVec3> homeCells = map.areaManager?.Home?.ActiveCells?.Where(cell => cell.Standable(map)).ToList();
            if (homeCells != null && homeCells.Count > 0)
            {
                int sumX = 0;
                int sumZ = 0;
                foreach (IntVec3 cell in homeCells)
                {
                    sumX += cell.x;
                    sumZ += cell.z;
                }

                return new IntVec3(sumX / homeCells.Count, 0, sumZ / homeCells.Count);
            }

            if (map.mapPawns?.FreeColonistsSpawned != null && map.mapPawns.FreeColonistsSpawned.Count > 0)
            {
                int sumX = 0;
                int sumZ = 0;
                int count = 0;
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!colonist.Spawned)
                    {
                        continue;
                    }

                    count++;
                    sumX += colonist.Position.x;
                    sumZ += colonist.Position.z;
                }

                if (count > 0)
                {
                    return new IntVec3(sumX / count, 0, sumZ / count);
                }
            }

            return map.Center;
        }

        public static int DistanceToNearestEdge(IntVec3 cell, Map map)
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
                foreach (Thing thing in cell.GetThingList(map))
                {
                    if (thing is Pawn target && target != pawn && !target.Dead && !ZombieUtility.ShouldZombiesIgnore(target))
                    {
                        ZombieTraitUtility.TryApplyZombieSickness(target, 0.22f);
                    }
                }
            }
        }

        public static void HandleSickBloodContact(Map map)
        {
            if (map == null || ZombieDefOf.CZH_Filth_SickZombieBlood == null)
            {
                return;
            }

            List<Thing> filth = map.listerThings.ThingsOfDef(ZombieDefOf.CZH_Filth_SickZombieBlood);
            for (int i = 0; i < filth.Count; i++)
            {
                IntVec3 cell = filth[i].Position;
                List<Thing> things = cell.GetThingList(map);
                for (int j = 0; j < things.Count; j++)
                {
                    if (things[j] is Pawn pawn && !pawn.Dead && !ZombieUtility.ShouldZombiesIgnore(pawn))
                    {
                        ZombieTraitUtility.TryApplyZombieSickness(pawn, 0.035f);
                    }
                }
            }
        }

        public static void HandleDrownedBehavior(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null)
            {
                return;
            }

            if (ZombieUtility.IsWaterCell(pawn.PositionHeld, pawn.MapHeld))
            {
                return;
            }

            Pawn prey = FindClosestLivingPrey(pawn, 8f);
            if (prey == null)
            {
                IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 20f);
                if (waterCell.IsValid && pawn.CurJobDef != JobDefOf.Goto)
                {
                    Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    returnToWater.expiryInterval = 700;
                    returnToWater.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(returnToWater, JobTag.Misc);
                }
            }
        }

        private static IntVec3 FindNearestWaterCell(IntVec3 origin, Map map, float radius)
        {
            float radiusSquared = radius * radius;
            IntVec3 best = IntVec3.Invalid;
            float bestDistance = float.MaxValue;
            foreach (IntVec3 cell in map.AllCells)
            {
                if (!ZombieUtility.IsWaterCell(cell, map) || !cell.Walkable(map))
                {
                    continue;
                }

                float distance = origin.DistanceToSquared(cell);
                if (distance > radiusSquared || distance >= bestDistance)
                {
                    continue;
                }

                best = cell;
                bestDistance = distance;
            }

            return best;
        }
    }
}
