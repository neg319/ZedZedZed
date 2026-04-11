using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieResurrectionService
    {
        private static readonly MethodInfo DirectResurrectWithSideEffects = AccessTools.Method(typeof(ResurrectionUtility), "ResurrectWithSideEffects", new[] { typeof(Pawn) });

        public static bool TryRaiseCorpse(Corpse corpse, ZombieRaiseMode mode, ZombieGameComponent component, out Pawn risenPawn)
        {
            risenPawn = null;
            if (!CorpseEligibilityUtility.CanRise(corpse, mode))
            {
                return false;
            }

            Pawn sourcePawn = corpse.InnerPawn;
            if (sourcePawn == null)
            {
                return false;
            }

            RiseProfile profile = BuildProfile(corpse, mode, component);
            Pawn preservedPawn = sourcePawn;
            Name preservedName = sourcePawn.Name;

            if (TryVanillaResurrection(corpse, sourcePawn))
            {
                risenPawn = sourcePawn;
                NormalizeRaisedPawn(risenPawn, corpse, preservedName, profile, component);
                return risenPawn != null && !risenPawn.Dead && !risenPawn.Destroyed;
            }

            if (ZombiePawnFactory.TrySpawnReanimatedPawnFromCorpse(corpse, profile.KindDef, profile.DesiredFaction, profile.PreserveName, profile.PreserveSkills, profile.PreserveRelations, out risenPawn))
            {
                if (profile.Mode == ZombieRaiseMode.ColonyLurker)
                {
                    ZombieLurkerUtility.EnsureColonyLurkerState(risenPawn, emergencyStabilize: true, stopCurrentJobs: true);
                }
                else if (profile.Mode == ZombieRaiseMode.InfectedZombie)
                {
                    ZombieUtility.EnsureZombieAggression(risenPawn);
                }

                return risenPawn != null && !risenPawn.Dead && !risenPawn.Destroyed;
            }

            risenPawn = preservedPawn;
            return false;
        }

        private static RiseProfile BuildProfile(Corpse corpse, ZombieRaiseMode mode, ZombieGameComponent component)
        {
            Pawn pawn = corpse?.InnerPawn;
            Map map = corpse?.MapHeld ?? pawn?.MapHeld;
            RiseProfile profile = new RiseProfile
            {
                Mode = mode,
                PreserveName = false,
                PreserveSkills = false,
                PreserveRelations = false,
                DesiredFaction = ZombieFactionUtility.GetOrCreateZombieFaction(),
                KindDef = ZombieKindSelector.GetRandomKind(map)
            };

            switch (mode)
            {
                case ZombieRaiseMode.ZombieCorpse:
                    ZombieVariant variant = ZombieUtility.GetVariant(pawn);
                    profile.KindDef = ZombieKindSelector.GetKindForVariant(variant, map)
                        ?? pawn?.kindDef
                        ?? ZombieKindSelector.GetKindForVariant(ZombieVariant.Biter, map);
                    profile.DesiredFaction = ZombieLurkerUtility.IsPassiveLurker(pawn)
                        ? null
                        : (ZombieUtility.IsPlayerAlignedZombie(pawn) ? Faction.OfPlayer : ZombieFactionUtility.GetOrCreateZombieFaction());
                    profile.PreserveName = ZombieUtility.IsPlayerAlignedZombie(pawn) || ZombieLurkerUtility.IsLurker(pawn);
                    profile.PreserveSkills = ZombieLurkerUtility.IsColonyLurker(pawn);
                    profile.PreserveRelations = ZombieLurkerUtility.IsColonyLurker(pawn);
                    break;

                case ZombieRaiseMode.ColonyLurker:
                    profile.KindDef = ZombieKindSelector.GetKindForVariant(ZombieVariant.Lurker, map)
                        ?? ZombieKindSelector.GetRandomKind(map);
                    profile.DesiredFaction = Faction.OfPlayer;
                    profile.PreserveName = true;
                    profile.PreserveSkills = true;
                    profile.PreserveRelations = true;
                    break;

                case ZombieRaiseMode.InfectedZombie:
                    profile.KindDef = ZombieKindSelector.GetRandomKind(map);
                    profile.DesiredFaction = ZombieFactionUtility.GetOrCreateZombieFaction();
                    break;
            }

            return profile;
        }

        private static bool TryVanillaResurrection(Corpse corpse, Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (!pawn.Dead)
            {
                return true;
            }

            if (DirectResurrectWithSideEffects != null)
            {
                try
                {
                    DirectResurrectWithSideEffects.Invoke(null, new object[] { pawn });
                    if (!pawn.Dead && !(pawn.ParentHolder is Corpse))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            foreach (MethodInfo method in typeof(ResurrectionUtility).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (!string.Equals(method.Name, "ResurrectWithSideEffects", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryBuildVanillaArgs(method, pawn, corpse, out object[] args))
                {
                    continue;
                }

                try
                {
                    method.Invoke(null, args);
                    if (!pawn.Dead && !(pawn.ParentHolder is Corpse))
                    {
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryBuildVanillaArgs(MethodInfo method, Pawn pawn, Corpse corpse, out object[] args)
        {
            args = null;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters == null || parameters.Length == 0)
            {
                return false;
            }

            bool assignedPawn = false;
            object[] built = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType() : parameter.ParameterType;
                if (parameterType == null)
                {
                    return false;
                }

                if (!assignedPawn && parameterType.IsAssignableFrom(typeof(Pawn)))
                {
                    built[i] = pawn;
                    assignedPawn = true;
                    continue;
                }

                if (corpse != null && parameterType.IsAssignableFrom(typeof(Corpse)))
                {
                    built[i] = corpse;
                    continue;
                }

                if (parameter.IsOptional)
                {
                    built[i] = parameter.DefaultValue is DBNull ? Type.Missing : parameter.DefaultValue;
                    continue;
                }

                if (string.Equals(parameterType.FullName, "RimWorld.ResurrectionParams", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parameterType.Name, "ResurrectionParams", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        built[i] = Activator.CreateInstance(parameterType);
                        continue;
                    }
                    catch
                    {
                    }
                }

                built[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }

            if (!assignedPawn)
            {
                return false;
            }

            args = built;
            return true;
        }

        private static void NormalizeRaisedPawn(Pawn pawn, Corpse originalCorpse, Name preservedName, RiseProfile profile, ZombieGameComponent component)
        {
            if (pawn == null || pawn.Destroyed)
            {
                return;
            }

            Map map = originalCorpse?.MapHeld ?? pawn.MapHeld;
            IntVec3 spawnCell = FindBestSpawnCell(originalCorpse?.PositionHeld ?? pawn.PositionHeld, map);

            bool corpseStillHoldingPawn = originalCorpse != null && !originalCorpse.Destroyed && originalCorpse.InnerPawn == pawn && pawn.ParentHolder is Corpse;
            if (!corpseStillHoldingPawn && originalCorpse != null && originalCorpse.Spawned && !originalCorpse.Destroyed)
            {
                try
                {
                    originalCorpse.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                }
            }

            if (!pawn.Spawned && map != null && spawnCell.IsValid)
            {
                try
                {
                    GenSpawn.Spawn(pawn, spawnCell, map, WipeMode.Vanish);
                }
                catch
                {
                }
            }
            else if (pawn.Spawned && map != null && spawnCell.IsValid && pawn.MapHeld == map && pawn.PositionHeld != spawnCell)
            {
                try
                {
                    pawn.Position = spawnCell;
                }
                catch
                {
                }
            }

            try
            {
                pawn.jobs?.StopAll();
            }
            catch
            {
            }

            ZombiePawnFactory.ConvertExistingPawnToZombie(pawn, profile.KindDef, profile.DesiredFaction, profile.PreserveName, profile.PreserveSkills, profile.PreserveRelations, initialSpawn: false);
            if (profile.PreserveName && preservedName != null)
            {
                ZombiePawnFactory.TrySetPawnName(pawn, preservedName);
            }

            ZombieUtility.PrepareZombieForReanimation(pawn);
            ZombieUtility.PrepareSpawnedZombie(pawn);
            ZombieUtility.RefreshDrownedState(pawn);

            if (profile.Mode == ZombieRaiseMode.ColonyLurker)
            {
                ZombieLurkerUtility.EnsureColonyLurkerState(pawn, emergencyStabilize: true, stopCurrentJobs: true);
            }
            else if (ZombieUtility.IsPlayerAlignedZombie(pawn))
            {
                ZombieUtility.EnsureFriendlyZombieState(pawn, stopCurrentJobs: true);
            }
            else if (ZombieLurkerUtility.IsPassiveLurker(pawn))
            {
                ZombieLurkerUtility.EnsurePassiveLurkerBehavior(pawn);
            }
            else
            {
                component?.RegisterBehavior(pawn, ZombieSpawnEventType.AssaultBase);
                ZombieUtility.AssignInitialShambleJob(pawn);
                ZombieUtility.EnsureZombieAggression(pawn);
            }

            ZombieUtility.MarkPawnGraphicsDirty(pawn);
        }

        private static IntVec3 FindBestSpawnCell(IntVec3 origin, Map map)
        {
            if (map == null)
            {
                return IntVec3.Invalid;
            }

            if (origin.IsValid && origin.InBounds(map) && origin.Walkable(map))
            {
                return origin;
            }

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin.IsValid ? origin : map.Center, 4.9f, true))
            {
                if (cell.InBounds(map) && cell.Walkable(map))
                {
                    return cell;
                }
            }

            return CellFinderLoose.RandomCellWith(c => c.InBounds(map) && c.Walkable(map), map);
        }

        private sealed class RiseProfile
        {
            public ZombieRaiseMode Mode;
            public PawnKindDef KindDef;
            public Faction DesiredFaction;
            public bool PreserveName;
            public bool PreserveSkills;
            public bool PreserveRelations;
        }
    }
}
