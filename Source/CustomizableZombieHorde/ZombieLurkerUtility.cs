using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace CustomizableZombieHorde
{
    public static class ZombieLurkerUtility
    {
        private const string HumanMeatDefName = "Meat_Human";

        public static bool IsLurker(Pawn pawn)
        {
            return pawn?.kindDef?.defName == "CZH_Zombie_Lurker";
        }

        public static bool IsPassiveLurker(Pawn pawn)
        {
            return IsLurker(pawn) && pawn.Faction == null;
        }

        public static bool IsColonyLurker(Pawn pawn)
        {
            return IsLurker(pawn) && pawn.Faction == Faction.OfPlayer;
        }

        public static ZombieVariant GetEffectiveVisualVariant(Pawn pawn, ZombieVariant actualVariant)
        {
            if (actualVariant != ZombieVariant.Lurker)
            {
                return actualVariant;
            }

            return GetVisualVariantForLurker(pawn);
        }

        public static ZombieVariant GetVisualVariantForLurker(Pawn pawn)
        {
            int roll = Math.Abs(pawn?.thingIDNumber ?? 0) % 7;
            switch (roll)
            {
                case 1:
                    return ZombieVariant.Crawler;
                case 2:
                    return ZombieVariant.Boomer;
                case 3:
                    return ZombieVariant.Sick;
                case 4:
                    return ZombieVariant.Drowned;
                case 5:
                    return ZombieVariant.Tank;
                case 6:
                    return ZombieVariant.Grabber;
                default:
                    return ZombieVariant.Biter;
            }
        }

        public static void ClearFaction(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            TrySetFaction(pawn, null);
        }

        public static void JoinColony(Pawn lurker, Pawn tamer)
        {
            if (lurker == null)
            {
                return;
            }

            EnsureLurkerZombiePassiveTrait(lurker);
            TrySetFaction(lurker, Faction.OfPlayer);

            try
            {
                lurker.jobs?.StopAll();
            }
            catch
            {
            }

            ZombieUtility.MarkPawnGraphicsDirty(lurker);
            ZombieFeedbackUtility.SendLurkerTamedMessage(lurker, tamer);
        }

        public static void InitializeLurker(Pawn lurker)
        {
            if (lurker == null)
            {
                return;
            }

            EnsureLurkerZombiePassiveTrait(lurker);
            ZombiePawnFactory.FinalizeZombie(lurker, initialSpawn: true, desiredFaction: null);
            ClearFaction(lurker);
            lurker.jobs?.StopAll();
            EnsurePassiveLurkerBehavior(lurker);
        }

        public static void EnsureLurkerZombiePassiveTrait(Pawn pawn)
        {
            ZombieTraitUtility.EnsureTrait(pawn, ZombieDefOf.CZH_Trait_ZombiePassive);
        }

        public static void EnsurePassiveLurkerBehavior(Pawn pawn)
        {
            if (!IsPassiveLurker(pawn) || pawn.Dead || pawn.Destroyed || !pawn.Spawned || pawn.jobs == null || pawn.Downed)
            {
                return;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Goto && pawn.CurJob.expiryInterval > 0)
            {
                return;
            }

            IntVec3 destination = CellFinder.RandomClosewalkCellNear(pawn.PositionHeld, pawn.MapHeld, 10);
            if (!destination.IsValid || destination == pawn.PositionHeld)
            {
                return;
            }

            Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, destination);
            moveJob.expiryInterval = Rand.RangeInclusive(900, 1800);
            moveJob.checkOverrideOnExpire = true;
            moveJob.locomotionUrgency = LocomotionUrgency.Amble;
            pawn.jobs.TryTakeOrderedJob(moveJob, JobTag.Misc);
        }

        public static bool ShouldSuppressLurkerHostility(Pawn a, Pawn b)
        {
            if (IsPassiveLurker(a) && (b == null || ZombieUtility.IsZombie(b) || b.Faction == Faction.OfPlayer))
            {
                return true;
            }

            if (IsPassiveLurker(b) && (a == null || ZombieUtility.IsZombie(a) || a.Faction == Faction.OfPlayer))
            {
                return true;
            }

            return false;
        }

        public static Thing FindCarriedTameFood(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            Thing carried = pawn.carryTracker?.CarriedThing;
            if (IsValidTameFood(carried))
            {
                return carried;
            }

            if (pawn.inventory?.innerContainer != null)
            {
                for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
                {
                    Thing thing = pawn.inventory.innerContainer[i];
                    if (IsValidTameFood(thing))
                    {
                        return thing;
                    }
                }
            }

            return null;
        }

        public static bool IsValidTameFood(Thing thing)
        {
            if (thing == null || thing.stackCount < 1)
            {
                return false;
            }

            string defName = thing.def?.defName ?? string.Empty;
            return thing.def == ZombieDefOf.CZH_RottenFlesh || defName == HumanMeatDefName;
        }

        public static float GetTameChance(Pawn tamer, Thing food)
        {
            int animals = tamer?.skills?.GetSkill(SkillDefOf.Animals)?.Level ?? 0;
            int social = tamer?.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float chance = 0.18f + animals * 0.035f + social * 0.01f;
            if (food?.def?.defName == HumanMeatDefName)
            {
                chance += 0.12f;
            }
            else if (food?.def == ZombieDefOf.CZH_RottenFlesh)
            {
                chance += 0.06f;
            }

            return Mathf.Clamp(chance, 0.08f, 0.92f);
        }

        public static bool ConsumeOneUnit(Pawn pawn, Thing preferredFood = null)
        {
            Thing food = preferredFood;
            if (!IsValidTameFood(food))
            {
                food = FindCarriedTameFood(pawn);
            }

            if (food == null)
            {
                return false;
            }

            if (food.stackCount > 1)
            {
                food.stackCount -= 1;
            }
            else
            {
                try
                {
                    food.Destroy(DestroyMode.Vanish);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static void TrySetFaction(Pawn pawn, Faction faction)
        {
            try
            {
                AccessTools.Method(typeof(Pawn), "SetFaction", new[] { typeof(Faction), typeof(Pawn) })?.Invoke(pawn, new object[] { faction, null });
                return;
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(typeof(Pawn), "SetFactionDirect", new[] { typeof(Faction) })?.Invoke(pawn, new object[] { faction });
            }
            catch
            {
            }
        }
    }
}
