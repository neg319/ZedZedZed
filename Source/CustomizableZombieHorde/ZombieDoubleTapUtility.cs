using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieDoubleTapUtility
    {
        private static readonly string[] RecommendedWorkTypeNames =
        {
            "Doctor",
            "Warden",
            "Haul",
            "Clean"
        };


        public static string GetWorkTypeDisplayLabel(WorkTypeDef workType)
        {
            if (workType == null)
            {
                return string.Empty;
            }

            if (!workType.labelShort.NullOrEmpty())
            {
                return workType.labelShort.CapitalizeFirst();
            }

            if (!workType.label.NullOrEmpty())
            {
                return workType.label.CapitalizeFirst();
            }

            return workType.defName;
        }

        public static IEnumerable<WorkTypeDef> GetAvailableWorkTypes()
        {
            List<WorkTypeDef> workTypes = new List<WorkTypeDef>();

            if (DefDatabase<WorkTypeDef>.AllDefsListForReading != null)
            {
                workTypes.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => def != null));
            }

            foreach (System.Reflection.FieldInfo field in typeof(WorkTypeDefOf).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.FieldType != typeof(WorkTypeDef))
                {
                    continue;
                }

                WorkTypeDef def = field.GetValue(null) as WorkTypeDef;
                if (def != null)
                {
                    workTypes.Add(def);
                }
            }

            foreach (string defName in RecommendedWorkTypeNames)
            {
                WorkTypeDef def = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                if (def != null)
                {
                    workTypes.Add(def);
                }
            }

            return workTypes
                .Where(def => def != null && !def.defName.NullOrEmpty())
                .Distinct()
                .OrderByDescending(def => def.naturalPriority)
                .ThenBy(def => GetWorkTypeDisplayLabel(def))
                .ToList();
        }

        public static List<string> GetDefaultWorkTypeDefNames()
        {
            List<string> defaults = RecommendedWorkTypeNames
                .Where(defName => DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName) != null)
                .ToList();

            if (defaults.Count > 0)
            {
                return defaults;
            }

            WorkTypeDef fallback = GetAvailableWorkTypes().FirstOrDefault();
            return fallback != null ? new List<string> { fallback.defName } : new List<string>();
        }

        public static void HandlePrioritizedDoubleTap()
        {
            CustomizableZombieHordeSettings settings = CustomizableZombieHordeMod.Settings;
            if (settings == null)
            {
                return;
            }

            bool prioritize = settings.enablePrioritizedDoubleTap;
            foreach (Map map in Find.Maps)
            {
                List<Pawn> pawns = map.mapPawns.AllPawnsSpawned
                    .Where(pawn => prioritize ? IsEligibleWorker(pawn) : IsEligibleFallbackWorker(pawn))
                    .ToList();

                foreach (Pawn pawn in pawns)
                {
                    if (prioritize)
                    {
                        TryAssignDoubleTapJob(pawn);
                    }
                    else
                    {
                        TryAssignFallbackDoubleTapJob(pawn);
                    }
                }
            }
        }

        public static bool IsEligibleFallbackWorker(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
            {
                return false;
            }

            if (!pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer || pawn.IsPrisonerOfColony)
            {
                return false;
            }

            if (pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.stances?.FullBodyBusy == true)
            {
                return false;
            }

            JobDef currentJob = pawn.CurJobDef;
            if (currentJob == ZombieDefOf.CZH_DoubleTapZombieCorpse)
            {
                return false;
            }

            if (currentJob == null)
            {
                return true;
            }

            string jobName = currentJob.defName ?? string.Empty;
            if (currentJob == JobDefOf.Wait || currentJob == JobDefOf.Goto || jobName.IndexOf("wander", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return jobName.IndexOf("haul", System.StringComparison.OrdinalIgnoreCase) >= 0
                || jobName.IndexOf("clean", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsEligibleWorker(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
            {
                return false;
            }

            if (!pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer || pawn.IsPrisonerOfColony)
            {
                return false;
            }

            if (pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.workSettings == null)
            {
                return false;
            }

            if (pawn.stances?.FullBodyBusy == true)
            {
                return false;
            }

            if (pawn.CurJobDef == ZombieDefOf.CZH_DoubleTapZombieCorpse)
            {
                return false;
            }

            return PawnMatchesSelectedWorkTypes(pawn);
        }

        public static bool PawnMatchesSelectedWorkTypes(Pawn pawn)
        {
            List<string> selectedDefNames = CustomizableZombieHordeMod.Settings?.prioritizedDoubleTapWorkTypeDefs;
            if (pawn?.workSettings == null)
            {
                return false;
            }

            if (selectedDefNames == null || selectedDefNames.Count == 0)
            {
                selectedDefNames = GetDefaultWorkTypeDefNames();
            }

            foreach (string defName in selectedDefNames)
            {
                WorkTypeDef workType = DefDatabase<WorkTypeDef>.GetNamedSilentFail(defName);
                if (workType != null && pawn.workSettings.WorkIsActive(workType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryAssignFallbackDoubleTapJob(Pawn pawn)
        {
            if (!IsEligibleFallbackWorker(pawn))
            {
                return false;
            }

            Thing targetThing = FindBestThingForDoubleTap(pawn, 30f);
            if (targetThing == null)
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == ZombieDefOf.CZH_DoubleTapZombieCorpse && pawn.CurJob.targetA.Thing == targetThing)
            {
                return true;
            }

            Job job = JobMaker.MakeJob(ZombieDefOf.CZH_DoubleTapZombieCorpse, targetThing);
            job.expiryInterval = 1800;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        public static bool TryAssignDoubleTapJob(Pawn pawn)
        {
            if (!IsEligibleWorker(pawn))
            {
                return false;
            }

            Thing targetThing = FindBestThingForDoubleTap(pawn);
            if (targetThing == null)
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == ZombieDefOf.CZH_DoubleTapZombieCorpse && pawn.CurJob.targetA.Thing == targetThing)
            {
                return true;
            }

            Job job = JobMaker.MakeJob(ZombieDefOf.CZH_DoubleTapZombieCorpse, targetThing);
            job.expiryInterval = 3000;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        public static Corpse FindBestCorpseForDoubleTap(Pawn pawn, float maxDistance = 80f)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            float bestDistance = float.MaxValue;
            Corpse bestCorpse = null;
            float maxDistanceSquared = maxDistance * maxDistance;

            foreach (Corpse corpse in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
            {
                if (!CanDoubleTapCorpse(corpse))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(corpse.PositionHeld);
                if (distance > maxDistanceSquared || distance >= bestDistance)
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(corpse, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                bestDistance = distance;
                bestCorpse = corpse;
            }

            return bestCorpse;
        }

        public static Thing FindBestThingForDoubleTap(Pawn pawn, float maxDistance = 80f)
        {
            Map map = pawn?.MapHeld;
            if (map == null)
            {
                return null;
            }

            float bestDistance = float.MaxValue;
            Thing bestThing = null;
            float maxDistanceSquared = maxDistance * maxDistance;

            foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
            {
                if (!ZombieFeignDeathUtility.CanAutoDoubleTapPawn(other))
                {
                    continue;
                }

                float distance = pawn.PositionHeld.DistanceToSquared(other.PositionHeld);
                if (distance > maxDistanceSquared || distance >= bestDistance)
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(other, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                bestDistance = distance;
                bestThing = other;
            }

            Corpse bestCorpse = FindBestCorpseForDoubleTap(pawn, maxDistance);
            if (bestCorpse != null)
            {
                float corpseDistance = pawn.PositionHeld.DistanceToSquared(bestCorpse.PositionHeld);
                if (corpseDistance < bestDistance)
                {
                    bestThing = bestCorpse;
                }
            }

            return bestThing;
        }

        public static bool CanDoubleTapThing(Thing thing)
        {
            if (thing is Corpse corpse)
            {
                return CanPlayerOrderDoubleTapCorpse(corpse);
            }

            if (thing is Pawn pawn)
            {
                return CanPlayerOrderDoubleTapPawn(pawn);
            }

            return false;
        }

        public static bool CanPlayerOrderDoubleTapPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null || !pawn.Downed)
            {
                return false;
            }

            if (pawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            return !ZombieRulesUtility.HasHeadDamageOrDestruction(pawn) && !ZombieInfectionUtility.IsSkullMissing(pawn);
        }

        public static bool CanDoubleTapPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.Spawned || pawn.MapHeld == null || !pawn.Downed)
            {
                return false;
            }

            if (pawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (ZombieRulesUtility.HasHeadDamageOrDestruction(pawn) || ZombieInfectionUtility.IsSkullMissing(pawn))
            {
                return false;
            }

            return ZombieUtility.IsZombie(pawn)
                || pawn.IsColonist
                || pawn.IsPrisonerOfColony
                || pawn.IsSlaveOfColony
                || ZombieInfectionUtility.HasZombieInfection(pawn)
                || ZombieInfectionUtility.HasReanimatedState(pawn);
        }

        public static bool TryStartManualDoubleTap(Pawn actor, Thing targetThing)
        {
            if (actor == null || actor.Dead || actor.Destroyed || !actor.Spawned || actor.MapHeld == null || !actor.IsColonistPlayerControlled)
            {
                return false;
            }

            if (actor.Downed || actor.InMentalState || actor.stances?.FullBodyBusy == true)
            {
                return false;
            }

            if (!CanDoubleTapThing(targetThing))
            {
                return false;
            }

            if (!actor.CanReserveAndReach(targetThing, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            if (actor.CurJob != null && actor.CurJob.def == ZombieDefOf.CZH_DoubleTapZombieCorpse && actor.CurJob.targetA.Thing == targetThing)
            {
                return true;
            }

            if (actor.drafter != null && actor.Drafted)
            {
                actor.drafter.Drafted = false;
            }

            Job job = JobMaker.MakeJob(ZombieDefOf.CZH_DoubleTapZombieCorpse, targetThing);
            job.expiryInterval = 3000;
            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            return true;
        }

        public static bool PerformDoubleTap(Pawn actor, Thing targetThing)
        {
            if (targetThing is Corpse corpse)
            {
                return PerformDoubleTap(actor, corpse);
            }

            if (targetThing is Pawn pawn)
            {
                return PerformDoubleTap(actor, pawn);
            }

            return false;
        }

        public static bool CanPlayerOrderDoubleTapCorpse(Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (corpse == null || corpse.Destroyed || innerPawn == null || !innerPawn.Dead || innerPawn.Destroyed)
            {
                return false;
            }

            if (innerPawn.RaceProps?.Humanlike != true)
            {
                return false;
            }

            if (ZombieRulesUtility.HasHeadDamageOrDestruction(innerPawn) || ZombieInfectionUtility.IsSkullMissing(innerPawn))
            {
                return false;
            }

            return corpse.Spawned && corpse.MapHeld != null;
        }

        public static bool CanDoubleTapCorpse(Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (corpse == null || corpse.Destroyed || innerPawn == null || !innerPawn.Dead || innerPawn.Destroyed)
            {
                return false;
            }

            if (ZombieUtility.IsVariant(innerPawn, ZombieVariant.Boomer))
            {
                return false;
            }

            bool canRiseAgain = ZombieUtility.IsZombie(innerPawn)
                || ZombieInfectionUtility.HasZombieInfection(innerPawn)
                || ZombieInfectionUtility.HasReanimatedState(innerPawn);

            if (!canRiseAgain)
            {
                return false;
            }

            if (ZombieRulesUtility.HasHeadDamageOrDestruction(innerPawn) || ZombieInfectionUtility.IsSkullMissing(innerPawn))
            {
                return false;
            }

            return corpse.Spawned && corpse.MapHeld != null;
        }

        public static bool PerformDoubleTap(Pawn actor, Pawn target)
        {
            if (!CanDoubleTapPawn(target))
            {
                return false;
            }

            BodyPartRecord head = ZombieUtility.GetHeadPart(target);
            BodyPartRecord brain = FindBodyPart(target, "Brain");
            BodyPartRecord skull = FindBodyPart(target, "Skull");
            BodyPartRecord fatalPart = head ?? skull ?? brain;

            try
            {
                if (fatalPart != null)
                {
                    target.TakeDamage(new DamageInfo(DamageDefOf.Cut, 999f, 999f, -1f, actor, fatalPart));
                }
                else
                {
                    target.TakeDamage(new DamageInfo(DamageDefOf.Cut, 999f, 999f, -1f, actor));
                }
            }
            catch
            {
            }

            if (!target.Dead)
            {
                try
                {
                    if (fatalPart != null)
                    {
                        target.Kill(new DamageInfo(DamageDefOf.Cut, 999f, 999f, -1f, actor, fatalPart));
                    }
                    else
                    {
                        target.Kill(new DamageInfo(DamageDefOf.Cut, 999f, 999f, -1f, actor));
                    }
                }
                catch
                {
                }
            }

            Corpse corpse = target.Corpse;
            if (corpse != null && corpse.Spawned)
            {
                Pawn innerPawn = corpse.InnerPawn ?? target;
                bool changed = false;
                changed |= EnsureCutTrauma(innerPawn, brain, 2.5f, actor);
                changed |= EnsureCutTrauma(innerPawn, skull ?? head, 14f, actor);
                changed |= EnsureCutTrauma(innerPawn, head, 10f, actor);

                ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
                component?.MarkInfectionHeadFatal(innerPawn);
                component?.ClearZombieCorpseWake(corpse);
                component?.ClearInfectionReanimation(corpse);
                component?.ClearDeadInfectedCorpse(corpse);

                if (corpse.MapHeld != null)
                {
                    FilthMaker.TryMakeFilth(corpse.PositionHeld, corpse.MapHeld, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
                }

                return changed || innerPawn.Dead;
            }

            return target.Dead;
        }

        public static bool PerformDoubleTap(Pawn actor, Corpse corpse)
        {
            Pawn innerPawn = corpse?.InnerPawn;
            if (!CanDoubleTapCorpse(corpse) || innerPawn == null)
            {
                return false;
            }

            BodyPartRecord head = ZombieUtility.GetHeadPart(innerPawn);
            BodyPartRecord brain = FindBodyPart(innerPawn, "Brain");
            BodyPartRecord skull = FindBodyPart(innerPawn, "Skull");

            bool changed = false;
            changed |= EnsureCutTrauma(innerPawn, brain, 2.5f, actor);
            changed |= EnsureCutTrauma(innerPawn, skull ?? head, 14f, actor);
            changed |= EnsureCutTrauma(innerPawn, head, 10f, actor);

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            component?.MarkInfectionHeadFatal(innerPawn);
            component?.ClearZombieCorpseWake(corpse);
            component?.ClearInfectionReanimation(corpse);
            component?.ClearDeadInfectedCorpse(corpse);

            if (corpse.MapHeld != null)
            {
                FilthMaker.TryMakeFilth(corpse.PositionHeld, corpse.MapHeld, ZombieDefOf.CZH_Filth_ZombieBlood ?? ThingDefOf.Filth_Blood);
            }

            return changed || ZombieRulesUtility.HasHeadDamageOrDestruction(innerPawn) || ZombieInfectionUtility.IsSkullMissing(innerPawn);
        }

        private static bool EnsureCutTrauma(Pawn pawn, BodyPartRecord part, float severity, Pawn instigator)
        {
            if (pawn?.health?.hediffSet == null || part == null || pawn.health.hediffSet.PartIsMissing(part))
            {
                return false;
            }

            try
            {
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Cut, severity, 999f, -1f, instigator, part));
            }
            catch
            {
            }

            Hediff_Injury injury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .FirstOrDefault(existing => existing.Part == part && existing.def == HediffDefOf.Cut);

            if (injury == null)
            {
                injury = HediffMaker.MakeHediff(HediffDefOf.Cut, pawn, part) as Hediff_Injury;
                if (injury == null)
                {
                    return false;
                }

                injury.Severity = severity;
                pawn.health.AddHediff(injury, part);
                return true;
            }

            if (injury.Severity < severity)
            {
                injury.Severity = severity;
            }

            return true;
        }

        private static BodyPartRecord FindBodyPart(Pawn pawn, string defName)
        {
            if (pawn?.RaceProps?.body?.AllParts == null || defName.NullOrEmpty())
            {
                return null;
            }

            return pawn.RaceProps.body.AllParts.FirstOrDefault(part => part?.def?.defName == defName && !pawn.health.hediffSet.PartIsMissing(part));
        }
    }
}
