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
            if (pawn == null || pawn.Faction == null)
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
            EnsureEmotionlessLurker(lurker);
            ClearGuestState(lurker);
            TrySetFaction(lurker, Faction.OfPlayer);
            ClearHostilityState(lurker);

            try
            {
                lurker.jobs?.StopAll();
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(lurker.jobs?.GetType(), "ClearQueuedJobs")?.Invoke(lurker.jobs, null);
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(lurker.mindState?.GetType(), "Reset", new[] { typeof(bool) })?.Invoke(lurker.mindState, new object[] { true });
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(lurker.drafter?.GetType(), "Notify_DraftedToggled")?.Invoke(lurker.drafter, null);
            }
            catch
            {
            }

            try
            {
                AccessTools.Method(lurker.playerSettings?.GetType(), "Notify_FactionChanged")?.Invoke(lurker.playerSettings, null);
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
            EnsureEmotionlessLurker(lurker);
            ZombiePawnFactory.FinalizeZombie(lurker, initialSpawn: true, desiredFaction: null);
            ClearFaction(lurker);
            lurker.jobs?.StopAll();
            EnsurePassiveLurkerBehavior(lurker);
        }

        public static void EnsureLurkerZombiePassiveTrait(Pawn pawn)
        {
            ZombieTraitUtility.EnsureTrait(pawn, ZombieDefOf.CZH_Trait_ZombiePassive);
        }

        public static void EnsureEmotionlessLurker(Pawn pawn)
        {
            if (!IsLurker(pawn) || pawn.needs == null || pawn.needs.AllNeeds == null)
            {
                return;
            }

            NeedDef moodDef = DefDatabase<NeedDef>.GetNamedSilentFail("Mood");
            NeedDef joyDef = DefDatabase<NeedDef>.GetNamedSilentFail("Joy");

            for (int i = pawn.needs.AllNeeds.Count - 1; i >= 0; i--)
            {
                Need need = pawn.needs.AllNeeds[i];
                if (need?.def == moodDef || need?.def == joyDef)
                {
                    pawn.needs.AllNeeds.RemoveAt(i);
                }
            }
        }

        public static void EnsurePassiveLurkerBehavior(Pawn pawn)
        {
            EnsureEmotionlessLurker(pawn);

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


        public static Thing FindAvailableTameFood(Pawn pawn, Map map)
        {
            Thing carried = FindCarriedTameFood(pawn);
            if (carried != null)
            {
                return carried;
            }

            if (map?.listerThings == null)
            {
                return null;
            }

            Thing best = null;
            float bestDistance = float.MaxValue;
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (!IsValidTameFood(thing) || thing.IsForbidden(pawn) || !pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some))
                {
                    continue;
                }

                if (thing.MapHeld == null || !thing.PositionHeld.IsValid || thing.PositionHeld.GetSlotGroup(thing.MapHeld) == null)
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(thing.PositionHeld);
                if (distance < bestDistance)
                {
                    best = thing;
                    bestDistance = distance;
                }
            }

            return best;
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

        public static float GetRecruitChance(Pawn recruiter, Thing food, Pawn lurker)
        {
            int social = recruiter?.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float chance = 0.20f + social * 0.05f;
            if (food?.def?.defName == HumanMeatDefName)
            {
                chance += 0.10f;
            }
            else if (food?.def == ZombieDefOf.CZH_RottenFlesh)
            {
                chance += 0.05f;
            }

            if (recruiter?.workSettings != null && recruiter.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
            {
                chance += 0.08f;
            }

            if (lurker?.story?.traits?.HasTrait(TraitDefOf.Psychopath) == true)
            {
                chance += 0.03f;
            }

            return Mathf.Clamp(chance, 0.12f, 0.98f);
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

        public static bool TryStartRecruitFromSocialTab(Pawn lurker, out string failureReason)
        {
            failureReason = null;
            if (!TryGetRecruitOrderData(lurker, out Pawn recruiter, out Thing food, out failureReason))
            {
                return false;
            }

            Job job = JobMaker.MakeJob(ZombieDefOf.CZH_RecruitLurker, lurker);
            if (recruiter.carryTracker?.CarriedThing != food && FindCarriedTameFood(recruiter) == null)
            {
                job.targetB = food;
            }
            job.count = 1;

            if (!recruiter.jobs.TryTakeOrderedJob(job, JobTag.Misc))
            {
                failureReason = recruiter.LabelShortCap + " could not start the recruit job.";
                return false;
            }

            return true;
        }

        public static bool TryGetRecruitOrderData(Pawn lurker, out Pawn recruiter, out Thing food, out string failureReason)
        {
            recruiter = null;
            food = null;
            failureReason = null;

            if (lurker == null || lurker.Destroyed || lurker.Dead)
            {
                failureReason = "Lurker is not available.";
                return false;
            }

            if (!IsPassiveLurker(lurker))
            {
                failureReason = "This lurker is no longer passive or recruitable.";
                return false;
            }

            if (lurker.MapHeld == null)
            {
                failureReason = "Lurker is not on a map.";
                return false;
            }

            float bestScore = float.MinValue;
            string lastReachIssue = null;
            string lastFoodIssue = null;

            List<Pawn> colonists = lurker.MapHeld.mapPawns?.FreeColonistsSpawned;
            if (colonists == null || colonists.Count == 0)
            {
                failureReason = "No free colonist is available to recruit this lurker.";
                return false;
            }

            for (int i = 0; i < colonists.Count; i++)
            {
                Pawn candidate = colonists[i];
                if (!IsValidRecruiter(candidate))
                {
                    continue;
                }

                if (!candidate.CanReserve(lurker))
                {
                    lastReachIssue = "Another pawn already reserved the lurker.";
                    continue;
                }

                if (!candidate.CanReach(lurker, PathEndMode.Touch, Danger.Some))
                {
                    lastReachIssue = candidate.LabelShortCap + " cannot reach the lurker.";
                    continue;
                }

                Thing candidateFood = FindAvailableTameFood(candidate, candidate.MapHeld);
                if (candidateFood == null)
                {
                    lastFoodIssue = "Needs rotten flesh or human meat in inventory or a stockpile.";
                    continue;
                }

                float score = ScoreRecruiter(candidate, lurker, candidateFood);
                if (score > bestScore)
                {
                    bestScore = score;
                    recruiter = candidate;
                    food = candidateFood;
                }
            }

            if (recruiter != null)
            {
                return true;
            }

            failureReason = lastFoodIssue ?? lastReachIssue ?? "No colonist is currently able to recruit this lurker.";
            return false;
        }

        public static string GetRecruitSocialCardLabel(Pawn lurker)
        {
            if (!TryGetRecruitOrderData(lurker, out Pawn recruiter, out Thing food, out _))
            {
                return "Recruit Lurker";
            }

            float chance = GetRecruitChance(recruiter, food, lurker);
            return "Recruit Lurker (" + chance.ToStringPercent("F0") + ")";
        }

        public static string GetRecruitSocialCardTooltip(Pawn lurker)
        {
            if (!TryGetRecruitOrderData(lurker, out Pawn recruiter, out Thing food, out string failureReason))
            {
                return failureReason ?? "No colonist can recruit this lurker right now.";
            }

            float chance = GetRecruitChance(recruiter, food, lurker);
            return recruiter.LabelShortCap + " will attempt to recruit this lurker using " + food.LabelCap + ". Success chance: " + chance.ToStringPercent("F0") + ".";
        }

        private static bool IsValidRecruiter(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || pawn.Downed || !pawn.Spawned || !pawn.IsColonistPlayerControlled)
            {
                return false;
            }

            if (pawn.InMentalState || pawn.WorkTagIsDisabled(WorkTags.Social))
            {
                return false;
            }

            return pawn.jobs != null;
        }

        private static float ScoreRecruiter(Pawn recruiter, Pawn lurker, Thing food)
        {
            int social = recruiter.skills?.GetSkill(SkillDefOf.Social)?.Level ?? 0;
            float score = social * 25f;
            if (recruiter.workSettings != null && recruiter.workSettings.WorkIsActive(WorkTypeDefOf.Warden))
            {
                score += 40f;
            }

            if (recruiter.carryTracker?.CarriedThing == food || FindCarriedTameFood(recruiter) == food)
            {
                score += 25f;
            }

            score -= recruiter.PositionHeld.DistanceToSquared(lurker.PositionHeld) * 0.02f;
            return score;
        }

        private static void ClearGuestState(Pawn pawn)
        {
            if (pawn?.guest == null)
            {
                return;
            }

            try
            {
                foreach (System.Reflection.MethodInfo method in typeof(Pawn_GuestTracker).GetMethods())
                {
                    if (method.Name != "SetGuestStatus")
                    {
                        continue;
                    }

                    System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                    object[] args = new object[parameters.Length];
                    bool supported = true;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType == typeof(Faction) || !parameterType.IsValueType)
                        {
                            args[i] = null;
                        }
                        else if (parameterType == typeof(bool))
                        {
                            args[i] = false;
                        }
                        else if (parameterType.IsEnum)
                        {
                            args[i] = Activator.CreateInstance(parameterType);
                        }
                        else
                        {
                            supported = false;
                            break;
                        }
                    }

                    if (supported)
                    {
                        method.Invoke(pawn.guest, args);
                    }
                }
            }
            catch
            {
            }
        }

        private static void ClearHostilityState(Pawn pawn)
        {
            try
            {
                if (pawn?.mindState?.mentalStateHandler?.InMentalState == true)
                {
                    pawn.mindState.mentalStateHandler.Reset();
                }
            }
            catch
            {
            }

            try
            {
                pawn?.mindState?.enemyTarget = null;
            }
            catch
            {
            }
        }

        private static void TrySetFaction(Pawn pawn, Faction faction)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.Faction == faction)
            {
                return;
            }

            try
            {
                pawn.SetFaction(faction);
                return;
            }
            catch
            {
            }

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
