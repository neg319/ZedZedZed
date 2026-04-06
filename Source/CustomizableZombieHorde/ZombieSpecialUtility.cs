using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieSpecialUtility
    {
        private static readonly HashSet<int> TriggeredBoomerBursts = new HashSet<int>();
        private static readonly Dictionary<int, int> LastSickSpitTickByPawn = new Dictionary<int, int>();
        private const int SickSpitCooldownTicks = 780;
        private const float SickSpitMinRange = 2.9f;
        private const float SickSpitMaxRange = 12.9f;
        private const float SickSpitChancePerCheck = 0.34f;
        private static bool suppressBoomerKillBurst;


        public static IEnumerable<Thing> BuildZombieButcherProducts(Pawn pawn)
        {
            List<Thing> result = new List<Thing>();
            if (pawn == null)
            {
                return result;
            }

            ZombieButcherProfile profile = ZombieVariantUtility.GetButcherProfile(ZombieVariantUtility.GetVariant(pawn));
            int fleshCount = profile?.FleshCount ?? 0;
            int leatherCount = profile?.LeatherCount ?? 0;

            TryAddStackedThing(result, ZombieDefOf.CZH_RottenFlesh, fleshCount, "rotten flesh");
            TryAddStackedThing(result, ZombieDefOf.CZH_RottenLeather, leatherCount, "rotten leather");

            int bileCount = ZombieBileUtility.GetButcheredBileCount(pawn);
            TryAddStackedThing(result, ZombieDefOf.CZH_ZombieBile, bileCount, "zombie bile");

            return result;
        }


        private static void TryAddStackedThing(List<Thing> result, ThingDef def, int count, string label)
        {
            if (result == null || def == null || count <= 0)
            {
                return;
            }

            try
            {
                Thing thing = ThingMaker.MakeThing(def);
                if (thing == null)
                {
                    return;
                }

                int stackLimit = thing.def?.stackLimit ?? count;
                thing.stackCount = count < 1 ? 1 : (count > stackLimit ? stackLimit : count);
                result.Add(thing);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to create {label} butcher product from def {def.defName}: {ex}");
            }
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
            if (pawn == null)
            {
                return;
            }

            ThingDef filth = ZombieDefOf.CZH_Filth_ZombieBlood;
            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Sick))
            {
                filth = ZombieDefOf.CZH_Filth_SickZombieBlood;
            }

            if (filth == null || !TryGetEffectLocation(pawn, out Map map, out IntVec3 pos))
            {
                return;
            }

            FilthMaker.TryMakeFilth(pos, map, filth);
        }

        public static void HandleZombieDeathEffects(Pawn pawn)
        {
            if (!ZombieUtility.IsZombie(pawn))
            {
                return;
            }

            switch (ZombieUtility.GetVariant(pawn))
            {
                case ZombieVariant.Boomer:
                    if (TriggeredBoomerBursts.Contains(pawn.thingIDNumber))
                    {
                        FinalizeBoomerBurst(pawn);
                    }
                    else
                    {
                        TriggerBoomerBurst(pawn, consumePawn: false, force: !suppressBoomerKillBurst);
                    }
                    break;
                case ZombieVariant.Sick:
                    DropZombieBlood(pawn);
                    DoSicknessBurst(pawn);
                    break;
                default:
                    DropZombieBlood(pawn);
                    break;
            }
        }

        public static bool TriggerBoomerBurst(Pawn pawn, bool consumePawn, bool force = false)
        {
            if (!TriggerBoomerBurstOnly(pawn, force))
            {
                return false;
            }

            Map map = pawn.MapHeld;
            IntVec3 pos = pawn.PositionHeld;
            if (consumePawn && !pawn.Dead && !pawn.Destroyed)
            {
                try
                {
                    suppressBoomerKillBurst = true;
                    pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 999f, 0f, -1f, null));
                }
                finally
                {
                    suppressBoomerKillBurst = false;
                }
            }

            ReplaceBoomerCorpseWithRottenFlesh(pawn, map, pos);
            return true;
        }

        public static bool TriggerBoomerBurstOnly(Pawn pawn, bool force = false)
        {
            if (pawn == null || pawn.DestroyedOrNull() || !ZombieUtility.IsVariant(pawn, ZombieVariant.Boomer))
            {
                return false;
            }

            int id = pawn.thingIDNumber;
            if (!force && TriggeredBoomerBursts.Contains(id))
            {
                return false;
            }

            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 pos))
            {
                return false;
            }

            TriggeredBoomerBursts.Add(id);
            DropZombieBlood(pawn);
            DoAcidBurst(map, pos, pawn);
            return true;
        }

        public static void FinalizeBoomerBurst(Pawn pawn)
        {
            if (pawn == null || !TriggeredBoomerBursts.Contains(pawn.thingIDNumber))
            {
                return;
            }

            ReplaceBoomerCorpseWithRottenFlesh(pawn, pawn.MapHeld, pawn.PositionHeld);
        }

        private static void ReplaceBoomerCorpseWithRottenFlesh(Pawn pawn, Map map, IntVec3 pos)
        {
            Corpse corpse = pawn?.Corpse;
            if (corpse != null)
            {
                map = corpse.MapHeld ?? map;
                if (corpse.PositionHeld.IsValid)
                {
                    pos = corpse.PositionHeld;
                }
            }
            else if ((map == null || !pos.IsValid || !pos.InBounds(map)) && pawn != null)
            {
                corpse = pawn.MapHeld?.thingGrid?.ThingsAt(pawn.PositionHeld).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn);
                if (corpse != null)
                {
                    map = corpse.MapHeld;
                    pos = corpse.PositionHeld;
                }
            }

            if (map == null || !pos.IsValid || !pos.InBounds(map))
            {
                return;
            }

            if (corpse != null && !corpse.Destroyed)
            {
                corpse.Destroy(DestroyMode.Vanish);
            }
            else if (pawn != null && !pawn.Destroyed && !pawn.Dead)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }

            ThingDef rottenFlesh = ZombieDefOf.CZH_RottenFlesh;
            if (rottenFlesh == null)
            {
                return;
            }

            Thing flesh = ThingMaker.MakeThing(rottenFlesh);
            flesh.stackCount = Rand.RangeInclusive(1, 10);
            GenPlace.TryPlaceThing(flesh, pos, map, ThingPlaceMode.Near);
        }

        public static void DoAcidBurst(Pawn pawn)
        {
            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 center))
            {
                return;
            }

            DoAcidBurst(map, center, pawn);
        }

        private static void DoAcidBurst(Map map, IntVec3 center, Pawn instigator)
        {
            if (map == null || !center.IsValid || !center.InBounds(map))
            {
                return;
            }

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
                    if (things[i] is Pawn target && target != instigator && !ZombieUtility.IsZombie(target))
                    {
                        float damage = Rand.Range(7f, 12f);
                        target.TakeDamage(new DamageInfo(ZombieDefOf.CZH_ZombieAcidBurn ?? DamageDefOf.Burn, damage, 0f, -1f, instigator));
                    }
                }
            }
        }

        private static bool TryGetEffectLocation(Pawn pawn, out Map map, out IntVec3 pos)
        {
            map = pawn?.MapHeld;
            if (map != null)
            {
                pos = pawn.PositionHeld;
                return pos.IsValid && pos.InBounds(map);
            }

            Corpse corpse = pawn?.Corpse;
            map = corpse?.MapHeld;
            if (map != null)
            {
                pos = corpse.PositionHeld;
                return pos.IsValid && pos.InBounds(map);
            }

            pos = IntVec3.Invalid;
            return false;
        }

        public static void DoSicknessBurst(Pawn pawn)
        {
            if (!TryGetEffectLocation(pawn, out Map map, out IntVec3 center))
            {
                return;
            }

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

        public static void HandleSickSpitAttack(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Sick) || pawn?.MapHeld == null || pawn.Dead || pawn.Downed || pawn.stances?.stunner?.Stunned == true)
            {
                return;
            }

            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int pawnId = pawn.thingIDNumber;
            if (LastSickSpitTickByPawn.TryGetValue(pawnId, out int lastSpitTick) && ticksGame - lastSpitTick < SickSpitCooldownTicks)
            {
                return;
            }

            Pawn prey = FindClosestLivingPrey(pawn, SickSpitMaxRange);
            if (prey == null || prey.Dead || prey.Destroyed || prey.MapHeld != pawn.MapHeld)
            {
                return;
            }

            float distanceSquared = pawn.PositionHeld.DistanceToSquared(prey.PositionHeld);
            if (distanceSquared < SickSpitMinRange * SickSpitMinRange || distanceSquared > SickSpitMaxRange * SickSpitMaxRange)
            {
                return;
            }

            if (!GenSight.LineOfSight(pawn.PositionHeld, prey.PositionHeld, pawn.MapHeld))
            {
                return;
            }

            if (pawn.CurJobDef == JobDefOf.AttackMelee && distanceSquared <= 5.8f)
            {
                return;
            }

            if (!Rand.Chance(SickSpitChancePerCheck))
            {
                return;
            }

            if (TryLaunchProjectile(pawn, prey, ZombieDefOf.CZH_SickSpitProjectile))
            {
                LastSickSpitTickByPawn[pawnId] = ticksGame;
            }
        }

        private static bool TryLaunchProjectile(Pawn launcher, Pawn target, ThingDef projectileDef)
        {
            if (launcher == null || target == null || projectileDef == null || launcher.MapHeld == null || !launcher.Spawned)
            {
                return false;
            }

            try
            {
                Thing thing = ThingMaker.MakeThing(projectileDef);
                if (!(thing is Projectile projectile))
                {
                    return false;
                }

                GenSpawn.Spawn(projectile, launcher.PositionHeld, launcher.MapHeld);
                Vector3 origin = launcher.DrawPos;
                LocalTargetInfo usedTarget = new LocalTargetInfo(target);
                LocalTargetInfo intendedTarget = new LocalTargetInfo(target);

                MethodInfo launchMethod = typeof(Projectile)
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(method => method.Name == "Launch")
                    .OrderByDescending(method => method.GetParameters().Length)
                    .FirstOrDefault();

                if (launchMethod == null)
                {
                    projectile.Destroy(DestroyMode.Vanish);
                    return false;
                }

                ParameterInfo[] parameters = launchMethod.GetParameters();
                object[] args = new object[parameters.Length];
                int localTargetCount = 0;

                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    Type parameterType = parameter.ParameterType;

                    if (typeof(Thing).IsAssignableFrom(parameterType))
                    {
                        if (parameter.Name != null && parameter.Name.ToLowerInvariant().Contains("equipment"))
                        {
                            args[i] = null;
                        }
                        else
                        {
                            args[i] = launcher;
                        }
                    }
                    else if (parameterType == typeof(Vector3))
                    {
                        args[i] = origin;
                    }
                    else if (parameterType == typeof(LocalTargetInfo))
                    {
                        args[i] = localTargetCount++ == 0 ? usedTarget : intendedTarget;
                    }
                    else if (parameterType == typeof(ProjectileHitFlags))
                    {
                        args[i] = ProjectileHitFlags.IntendedTarget;
                    }
                    else if (parameterType == typeof(bool))
                    {
                        args[i] = false;
                    }
                    else if (parameter.HasDefaultValue)
                    {
                        args[i] = parameter.DefaultValue;
                    }
                    else if (parameterType.IsValueType)
                    {
                        args[i] = Activator.CreateInstance(parameterType);
                    }
                    else
                    {
                        args[i] = null;
                    }
                }

                launchMethod.Invoke(projectile, args);
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[ZedZedZed] Failed to launch sick spit projectile: {ex}");
                return false;
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

        public static bool IsRainActive(Map map)
        {
            if (map?.weatherManager == null)
            {
                return false;
            }

            try
            {
                if (map.weatherManager.RainRate > 0.02f)
                {
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                WeatherDef weather = map.weatherManager.curWeather;
                return weather != null && weather.rainRate > 0.02f;
            }
            catch
            {
                return false;
            }
        }

        public static bool ShouldDrownedRoamFreely(Pawn pawn)
        {
            return pawn?.MapHeld != null && IsRainActive(pawn.MapHeld);
        }

        public static bool ShouldDrownedHoldWater(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || ShouldDrownedRoamFreely(pawn))
            {
                return false;
            }

            Pawn prey = FindClosestLivingPrey(pawn, 18f);
            return prey == null;
        }

        public static void HandleDrownedBehavior(Pawn pawn)
        {
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Drowned) || pawn?.MapHeld == null || pawn.jobs == null)
            {
                return;
            }

            if (ShouldDrownedRoamFreely(pawn))
            {
                return;
            }

            bool inWater = ZombieUtility.IsWaterCell(pawn.PositionHeld, pawn.MapHeld);
            Pawn prey = FindClosestLivingPrey(pawn, 18f);
            Pawn currentTarget = pawn.CurJob?.targetA.Thing as Pawn;

            if (currentTarget != null)
            {
                bool validTarget = !currentTarget.Dead && !currentTarget.Destroyed && !ZombieUtility.ShouldZombiesIgnore(currentTarget);
                if (!validTarget || pawn.PositionHeld.DistanceToSquared(currentTarget.PositionHeld) > 24f * 24f)
                {
                    currentTarget = null;
                }
            }

            if (prey == null && currentTarget == null)
            {
                if (inWater)
                {
                    if (pawn.CurJob != null && (pawn.CurJob.def == JobDefOf.AttackMelee || pawn.CurJob.def == JobDefOf.Goto))
                    {
                        pawn.jobs.StopAll();
                    }

                    return;
                }

                IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 35f);
                if (waterCell.IsValid && (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Goto || pawn.CurJob.targetA.Cell != waterCell))
                {
                    Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    returnToWater.expiryInterval = 900;
                    returnToWater.checkOverrideOnExpire = true;
                    returnToWater.locomotionUrgency = ZombieUtility.GetZombieUrgency(pawn);
                    pawn.jobs.TryTakeOrderedJob(returnToWater, JobTag.Misc);
                }

                return;
            }

            if (!inWater && prey != null && pawn.PositionHeld.DistanceToSquared(prey.PositionHeld) > 24f * 24f)
            {
                IntVec3 waterCell = FindNearestWaterCell(pawn.PositionHeld, pawn.MapHeld, 35f);
                if (waterCell.IsValid)
                {
                    Job returnToWater = JobMaker.MakeJob(JobDefOf.Goto, waterCell);
                    returnToWater.expiryInterval = 900;
                    returnToWater.checkOverrideOnExpire = true;
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
