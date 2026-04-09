using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace CustomizableZombieHorde
{
    public sealed class ZombieGameComponent : GameComponent
    {
        private int nextTrickleTick = -1;
        private int nextGroundBurstTick = -1;
        private int nextGraveEventTick = -1;
        private Dictionary<int, int> corpseWakeTicks = new Dictionary<int, int>();
        private Dictionary<int, int> infectionReanimationTicks = new Dictionary<int, int>();
        private Dictionary<int, int> infectionReanimationStartTicks = new Dictionary<int, int>();
        private Dictionary<int, float> infectionReanimationStartSeverity = new Dictionary<int, float>();
        private Dictionary<int, int> deadInfectionProgressStartTicks = new Dictionary<int, int>();
        private Dictionary<int, int> deadInfectionProgressEndTicks = new Dictionary<int, int>();
        private Dictionary<int, float> deadInfectionProgressStartSeverity = new Dictionary<int, float>();
        private Dictionary<int, float> pendingDeadInfectionSeverityByPawnId = new Dictionary<int, float>();
        private int nextMoonEventDay = -1;
        private bool nextMoonIsBlood;
        private int lastSevenDayWarningForMoon = -1;
        private int lastOneDayWarningForMoon = -1;
        private int lastTriggeredMoonDay = -1;
        private int cachedCurrentMapZombieCount = 0;
        private int cachedZombieCountTick = -1;
        private int lastGuaranteeAttemptTick = -1;
        private int bloodMoonActiveUntilTick = -1;
        private int nextBloodMoonRushTick = -1;
        private Dictionary<int, int> lastNightlySpawnDayByMap = new Dictionary<int, int>();
        private Dictionary<int, int> zombieBehaviorByPawnId = new Dictionary<int, int>();
        private List<int> infectionHeadFatalPawnIds = new List<int>();
        private List<int> infectionLurkerPawnIds = new List<int>();

        public ZombieGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextTrickleTick, "nextTrickleTick", -1);
            Scribe_Values.Look(ref nextGroundBurstTick, "nextGroundBurstTick", -1);
            Scribe_Values.Look(ref nextGraveEventTick, "nextGraveEventTick", -1);
            Scribe_Collections.Look(ref corpseWakeTicks, "corpseWakeTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref infectionReanimationTicks, "infectionReanimationTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref infectionReanimationStartTicks, "infectionReanimationStartTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref infectionReanimationStartSeverity, "infectionReanimationStartSeverity", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref deadInfectionProgressStartTicks, "deadInfectionProgressStartTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref deadInfectionProgressEndTicks, "deadInfectionProgressEndTicks", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref deadInfectionProgressStartSeverity, "deadInfectionProgressStartSeverity", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref pendingDeadInfectionSeverityByPawnId, "pendingDeadInfectionSeverityByPawnId", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref nextMoonEventDay, "nextMoonEventDay", -1);
            Scribe_Values.Look(ref nextMoonIsBlood, "nextMoonIsBlood", false);
            Scribe_Values.Look(ref lastSevenDayWarningForMoon, "lastSevenDayWarningForMoon", -1);
            Scribe_Values.Look(ref lastOneDayWarningForMoon, "lastOneDayWarningForMoon", -1);
            Scribe_Values.Look(ref lastTriggeredMoonDay, "lastTriggeredMoonDay", -1);
            Scribe_Values.Look(ref cachedCurrentMapZombieCount, "cachedCurrentMapZombieCount", 0);
            Scribe_Values.Look(ref cachedZombieCountTick, "cachedZombieCountTick", -1);
            Scribe_Values.Look(ref lastGuaranteeAttemptTick, "lastGuaranteeAttemptTick", -1);
            Scribe_Values.Look(ref bloodMoonActiveUntilTick, "bloodMoonActiveUntilTick", -1);
            Scribe_Values.Look(ref nextBloodMoonRushTick, "nextBloodMoonRushTick", -1);
            Scribe_Collections.Look(ref lastNightlySpawnDayByMap, "lastNightlySpawnDayByMap", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref zombieBehaviorByPawnId, "zombieBehaviorByPawnId", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref infectionHeadFatalPawnIds, "infectionHeadFatalPawnIds", LookMode.Value);
            Scribe_Collections.Look(ref infectionLurkerPawnIds, "infectionLurkerPawnIds", LookMode.Value);
            base.ExposeData();

            corpseWakeTicks ??= new Dictionary<int, int>();
            infectionReanimationTicks ??= new Dictionary<int, int>();
            infectionReanimationStartTicks ??= new Dictionary<int, int>();
            infectionReanimationStartSeverity ??= new Dictionary<int, float>();
            deadInfectionProgressStartTicks ??= new Dictionary<int, int>();
            deadInfectionProgressEndTicks ??= new Dictionary<int, int>();
            deadInfectionProgressStartSeverity ??= new Dictionary<int, float>();
            pendingDeadInfectionSeverityByPawnId ??= new Dictionary<int, float>();
            lastNightlySpawnDayByMap ??= new Dictionary<int, int>();
            zombieBehaviorByPawnId ??= new Dictionary<int, int>();
            infectionHeadFatalPawnIds ??= new List<int>();
            infectionLurkerPawnIds ??= new List<int>();
        }

        public override void GameComponentTick()
        {
            if (Current.ProgramState != ProgramState.Playing || Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;

            if (ticksGame % 180 == 0)
            {
                SanitizeLivingZombies();
                HandleSickBloodContact();
                HandleZombieFeeding();
                HandleZombieInfectionProgression();
                HandleDeadInfectedCorpseProgression();
            }

            if (ticksGame % 12 == 0)
            {
                ZombieGrabberUtility.TickGrabbers();
                ZombieSpecialUtility.TickPendingSickSpewWarmups();
            }

            if (ticksGame % 120 == 0)
            {
                HandleZombieReanimation();
            }

            if (ticksGame % 300 == 0)
            {
                RefreshCurrentMapCount();
            }

            if (ticksGame % 900 == 0)
            {
                EnsureZombiePresence(ticksGame);
            }

            if (ticksGame % 300 == 0)
            {
                HandleGuaranteedNightlySpawns(ticksGame);
            }

            HandleTrickleSpawns(ticksGame);
            HandleGroundBursts(ticksGame);
            HandleGraveEvents(ticksGame);
            HandleActiveBloodMoon(ticksGame);
            HandleMoonCycle();
        }

        public int GetCurrentMapZombieCount()
        {
            if (Find.TickManager == null)
            {
                return 0;
            }

            int ticksGame = Find.TickManager.TicksGame;
            if (cachedZombieCountTick < 0 || ticksGame - cachedZombieCountTick > 300)
            {
                RefreshCurrentMapCount();
            }

            return cachedCurrentMapZombieCount;
        }

        public bool IsBloodMoonActive
        {
            get
            {
                int ticksGame = Find.TickManager?.TicksGame ?? 0;
                return bloodMoonActiveUntilTick >= 0 && ticksGame < bloodMoonActiveUntilTick;
            }
        }

        public bool HasUsableDebugMap()
        {
            return GetDebugTargetMap() != null;
        }

        public bool DebugForceEdgeWave()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            int count = Mathf.Max(1, Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.trickleMinGroupSize, CustomizableZombieHordeMod.Settings.trickleMaxGroupSize));
            bool result = ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: true, customLetterLabel: "Debug Edge Wave", customLetterText: "A debug zombie wave has been forced from the map edge.", applyDifficulty: false, ignoreCap: true, ignoreTimeOfDay: true);
            if (!result)
            {
                result = ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: true, customLetterLabel: "Debug Edge Wave", customLetterText: "A debug zombie wave has been forced near the map edge.", ignoreCap: true);
            }

            if (result && Find.TickManager != null)
            {
                nextTrickleTick = Find.TickManager.TicksGame + HoursToTicks(0.5f);
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceNightlyWave()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            int count = Mathf.Max(2, CustomizableZombieHordeMod.Settings.trickleMinGroupSize + 1);
            bool result = ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: true, customLetterLabel: "Debug Night Wave", customLetterText: "A debug nightly zombie wave has been forced onto the map.", applyDifficulty: false, ignoreCap: true, ignoreTimeOfDay: true);
            if (!result)
            {
                result = ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: true, customLetterLabel: "Debug Night Wave", customLetterText: "A debug nightly zombie wave has been forced near the map edge.", ignoreCap: true);
            }

            if (result)
            {
                lastNightlySpawnDayByMap ??= new Dictionary<int, int>();
                int currentDay = (Find.TickManager?.TicksGame ?? 0) / GenDate.TicksPerDay;
                lastNightlySpawnDayByMap[map.uniqueID] = currentDay;
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceHuddledPack()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            int count = Mathf.Max(2, CustomizableZombieHordeMod.Settings.trickleMinGroupSize + 1);
            bool result = ZombieSpawnHelper.SpawnHuddledPack(map, forcedCount: count, sendLetter: true, ignoreCap: true, ignoreTimeOfDay: true);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceBasePush()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            int count = Mathf.Max(2, CustomizableZombieHordeMod.Settings.trickleMinGroupSize + 1);
            bool result = ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: true, customLetterLabel: "Debug Base Push", customLetterText: "A debug zombie group has been forced toward your colony.", applyDifficulty: false, ignoreCap: true, ignoreTimeOfDay: true, behavior: ZombieSpawnEventType.AssaultBase);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceEdgeWanderers()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            int count = Mathf.Max(2, CustomizableZombieHordeMod.Settings.trickleMinGroupSize + 1);
            bool result = ZombieSpawnHelper.SpawnEdgeWanderers(map, forcedCount: count, sendLetter: true, ignoreCap: true, ignoreTimeOfDay: true);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceGroundBurst()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            bool result = ZombieSpawnHelper.SpawnGroundBurst(map, forcedCount: Mathf.Max(2, CustomizableZombieHordeMod.Settings.groundBurstMinGroupSize), sendLetter: true, ignoreCap: true, ignoreTimeOfDay: true, allowCenterFallback: true);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceRandomGraveEvent()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            bool result = ZombieSpawnHelper.SpawnRandomGraveEvent(map, sendLetter: true);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceVariantGraveEvent(ZombieVariant variant)
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            bool result = ZombieSpawnHelper.SpawnVariantGraveEvent(map, variant, sendLetter: true);
            if (result)
            {
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugForceMoonEvent(bool bloodMoon)
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
            ZombiePopulationState populationState = bloodMoon ? ZombiePopulationState.BloodMoon : ZombiePopulationState.FullMoon;
            int targetCount = ZombieSpawnHelper.GetRemainingCapacity(map, populationState);
            if (targetCount < 1)
            {
                targetCount = ZombieSpawnHelper.GetDynamicZombieCap(map, populationState);
            }

            bool result = bloodMoon
                ? ZombieSpawnHelper.SpawnHorde(
                    map,
                    targetCount,
                    groups: 3,
                    letterLabel: "Debug Blood Moon",
                    letterText: "A debug blood moon has been forced. A massive wave of " + prefix.ToLowerInvariant() + "s is descending on your colony.",
                    ignoreCap: false,
                    ignoreTimeOfDay: true,
                    populationState: ZombiePopulationState.BloodMoon)
                : ZombieSpawnHelper.SpawnHorde(
                    map,
                    targetCount,
                    groups: 2,
                    letterLabel: "Debug Full Moon",
                    letterText: "A debug full moon horde has been forced. The dead are closing in on your colony.",
                    ignoreCap: false,
                    ignoreTimeOfDay: true,
                    populationState: ZombiePopulationState.FullMoon);

            if (result)
            {
                ForceMoonRush(map, bloodMoon);
                int currentDay = (Find.TickManager?.TicksGame ?? 0) / GenDate.TicksPerDay;
                lastTriggeredMoonDay = currentDay;
                ScheduleNextMoon(currentDay);
                RefreshCurrentMapCount();
            }

            return result;
        }

        public bool DebugSpawnLurker()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Lurker");
            if (kind == null)
            {
                return false;
            }

            IntVec3 spawnCell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.Walkable(map) && !c.Fogged(map), out spawnCell))
            {
                spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            }

            if (!spawnCell.IsValid)
            {
                spawnCell = map.Center;
            }

            Pawn lurker = PawnGenerator.GeneratePawn(kind);
            if (lurker == null)
            {
                return false;
            }

            ZombieLurkerUtility.InitializeLurker(lurker);
            GenSpawn.Spawn(lurker, spawnCell, map);
            ZombieLurkerUtility.EnsurePassiveLurkerBehavior(lurker);
            Messages.Message("A debug lurker has been spawned.", lurker, MessageTypeDefOf.NeutralEvent);
            RefreshCurrentMapCount();
            return true;
        }

        public bool DebugSpawnBoneBiter()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Biter");
            if (kind == null)
            {
                return false;
            }

            IntVec3 spawnCell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.Walkable(map) && !c.Fogged(map), out spawnCell))
            {
                spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            }

            if (!spawnCell.IsValid)
            {
                spawnCell = map.Center;
            }

            Pawn boneBiter = ZombiePawnFactory.GenerateZombie(kind, ZombieFactionUtility.GetOrCreateZombieFaction());
            if (boneBiter == null)
            {
                return false;
            }

            if (boneBiter.health?.hediffSet != null && !boneBiter.health.hediffSet.HasHediff(ZombieDefOf.CZH_ZombieSkeletonBiter))
            {
                boneBiter.health.AddHediff(ZombieDefOf.CZH_ZombieSkeletonBiter);
            }

            ZombieUtility.SetZombieDisplayName(boneBiter);
            GenSpawn.Spawn(boneBiter, spawnCell, map);
            Messages.Message("A debug Bone Biter has been spawned.", boneBiter, MessageTypeDefOf.NeutralEvent);
            RefreshCurrentMapCount();
            return true;
        }

        public bool DebugSpawnRunt()
        {
            Map map = GetDebugTargetMap();
            if (map == null || ZombieDefOf.CZH_RuntKind == null)
            {
                return false;
            }

            IntVec3 spawnCell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.Walkable(map) && !c.Fogged(map), out spawnCell))
            {
                spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            }

            if (!spawnCell.IsValid)
            {
                spawnCell = map.Center;
            }

            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (faction == null)
            {
                return false;
            }

            Pawn runt = PawnGenerator.GeneratePawn(ZombieDefOf.CZH_RuntKind, faction);
            if (runt == null)
            {
                return false;
            }

            GenSpawn.Spawn(runt, spawnCell, map);
            RegisterBehavior(runt, ZombieSpawnEventType.AssaultBase);
            ZombieUtility.PrepareSpawnedZombie(runt);
            ZombieUtility.AssignInitialShambleJob(runt, ZombieSpawnEventType.AssaultBase);
            ZombieUtility.EnsureZombieAggression(runt);
            Messages.Message("A debug runt has been spawned.", runt, MessageTypeDefOf.NeutralEvent);
            RefreshCurrentMapCount();
            return true;
        }

        public bool DebugSpawnPregnantBoomer()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Boomer");
            Faction faction = ZombieFactionUtility.GetOrCreateZombieFaction();
            if (kind == null || faction == null)
            {
                return false;
            }

            IntVec3 spawnCell;
            if (!CellFinder.TryFindRandomCellNear(map.Center, map, 12, c => c.Walkable(map) && !c.Fogged(map), out spawnCell))
            {
                spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
            }

            if (!spawnCell.IsValid)
            {
                spawnCell = map.Center;
            }

            Pawn boomer = ZombiePawnFactory.GenerateZombie(kind, faction);
            if (boomer == null)
            {
                return false;
            }

            boomer.gender = Gender.Female;
            if (boomer.health?.hediffSet != null && ZombieDefOf.CZH_PregnantBoomer != null && !boomer.health.hediffSet.HasHediff(ZombieDefOf.CZH_PregnantBoomer))
            {
                boomer.health.AddHediff(ZombieDefOf.CZH_PregnantBoomer);
            }

            ZombieUtility.SetZombieDisplayName(boomer);
            ZombieUtility.MarkPawnGraphicsDirty(boomer);
            GenSpawn.Spawn(boomer, spawnCell, map);
            RegisterBehavior(boomer, ZombieSpawnEventType.AssaultBase);
            ZombieUtility.PrepareSpawnedZombie(boomer);
            ZombieUtility.AssignInitialShambleJob(boomer, ZombieSpawnEventType.AssaultBase);
            ZombieUtility.EnsureZombieAggression(boomer);
            Messages.Message("A debug pregnant boomer has been spawned.", boomer, MessageTypeDefOf.NeutralEvent);
            RefreshCurrentMapCount();
            return true;
        }


        public void RegisterBehavior(Pawn pawn, ZombieSpawnEventType behavior)
        {
            if (pawn == null)
            {
                return;
            }

            zombieBehaviorByPawnId ??= new Dictionary<int, int>();
            zombieBehaviorByPawnId[pawn.thingIDNumber] = (int)behavior;
        }

        public ZombieSpawnEventType GetAssignedBehavior(Pawn pawn)
        {
            if (pawn == null)
            {
                return ZombieSpawnEventType.AssaultBase;
            }

            if (zombieBehaviorByPawnId != null && zombieBehaviorByPawnId.TryGetValue(pawn.thingIDNumber, out int storedValue))
            {
                return (ZombieSpawnEventType)storedValue;
            }

            return ZombieSpawnEventType.AssaultBase;
        }

        public void ForgetBehavior(Pawn pawn)
        {
            if (pawn == null || zombieBehaviorByPawnId == null)
            {
                return;
            }

            zombieBehaviorByPawnId.Remove(pawn.thingIDNumber);
        }

        public bool HasActiveGrabberTongue(Pawn pawn)
        {
            return ZombieGrabberUtility.HasActiveTongue(pawn);
        }

        public Pawn GetGrabberTongueTarget(Pawn pawn)
        {
            return ZombieGrabberUtility.GetTongueTarget(pawn);
        }

        public bool IsBloodMoonVisualActive(Map map = null)
        {
            if (Find.TickManager == null)
            {
                return false;
            }

            if (bloodMoonActiveUntilTick < 0 || Find.TickManager.TicksGame >= bloodMoonActiveUntilTick)
            {
                return false;
            }

            map ??= Find.CurrentMap;
            return map == null || map.IsPlayerHome;
        }

        public Color GetBloodMoonTintColor()
        {
            return new Color(1f, 0.1f, 0.1f, 0.10f);
        }

        private Map GetDebugTargetMap()
        {
            if (Find.CurrentMap != null && Find.CurrentMap.IsPlayerHome)
            {
                return Find.CurrentMap;
            }

            return Find.Maps.FirstOrDefault(map => map.IsPlayerHome);
        }

        private void RefreshCurrentMapCount()
        {
            cachedZombieCountTick = Find.TickManager?.TicksGame ?? 0;
            Map map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome)
            {
                cachedCurrentMapZombieCount = 0;
                return;
            }

            cachedCurrentMapZombieCount = map.mapPawns.AllPawnsSpawned.Count(ZombieUtility.IsZombie);
        }

        private void EnsureZombiePresence(int ticksGame)
        {
            if (lastGuaranteeAttemptTick >= 0 && ticksGame - lastGuaranteeAttemptTick < 600)
            {
                return;
            }

            lastGuaranteeAttemptTick = ticksGame;

            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                int livingZombies = map.mapPawns?.AllPawnsSpawned?.Count(ZombieUtility.IsZombie) ?? 0;
                if (livingZombies > 0)
                {
                    continue;
                }

                int guaranteedCount = map.skyManager != null && map.skyManager.CurSkyGlow <= 0.25f ? Mathf.Max(1, CustomizableZombieHordeMod.Settings?.trickleMinGroupSize ?? 1) : 0;
                if (guaranteedCount > 0)
                {
                    if (!ZombieSpawnHelper.SpawnWave(map, forcedCount: guaranteedCount, sendLetter: false))
                    {
                        ZombieSpawnHelper.SpawnEmergencyPack(map, guaranteedCount, sendLetter: false);
                    }

                    nextTrickleTick = ticksGame + HoursToTicks(1.50f);
                }
            }
        }

        private void HandleGuaranteedNightlySpawns(int ticksGame)
        {
            lastNightlySpawnDayByMap ??= new Dictionary<int, int>();
            int currentDay = ticksGame / GenDate.TicksPerDay;

            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome || map.skyManager == null)
                {
                    continue;
                }

                if (map.skyManager.CurSkyGlow > 0.40f)
                {
                    continue;
                }

                int mapId = map.uniqueID;
                if (lastNightlySpawnDayByMap.TryGetValue(mapId, out int lastSpawnDay) && lastSpawnDay == currentDay)
                {
                    continue;
                }

                int guaranteedCount = 1;
                ZombieSpawnEventType behavior = PickNightlyBehavior();
                bool spawned = ZombieSpawnHelper.SpawnByBehavior(map, behavior, guaranteedCount, sendLetter: false, applyDifficulty: true, ignoreCap: false, ignoreTimeOfDay: false);
                if (!spawned)
                {
                    spawned = ZombieSpawnHelper.SpawnEmergencyPack(map, guaranteedCount, sendLetter: false, behavior: behavior);
                }

                if (spawned)
                {
                    lastNightlySpawnDayByMap[mapId] = currentDay;
                    RefreshCurrentMapCount();
                }
            }

            List<int> activeMapIds = Find.Maps.Select(map => map.uniqueID).ToList();
            List<int> staleMapIds = lastNightlySpawnDayByMap.Keys.Where(id => !activeMapIds.Contains(id)).ToList();
            foreach (int staleMapId in staleMapIds)
            {
                lastNightlySpawnDayByMap.Remove(staleMapId);
            }
        }

        private void HandleTrickleSpawns(int ticksGame)
        {
            if (!CustomizableZombieHordeMod.Settings.enableEdgeTrickle)
            {
                return;
            }

            bool anyHomeMapHasZombies = Find.Maps.Any(map => map.IsPlayerHome && map.mapPawns?.AllPawnsSpawned?.Any(ZombieUtility.IsZombie) == true);
            int emergencyLeadTime = HoursToTicks(2.50f);
            if (!anyHomeMapHasZombies && (nextTrickleTick < 0 || nextTrickleTick - ticksGame > emergencyLeadTime))
            {
                nextTrickleTick = ticksGame + HoursToTicks(0.50f);
            }

            if (nextTrickleTick < 0)
            {
                nextTrickleTick = ticksGame + HoursToTicks(0.50f);
                return;
            }

            if (ticksGame < nextTrickleTick)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                int count = Rand.RangeInclusive(CustomizableZombieHordeMod.Settings.trickleMinGroupSize, CustomizableZombieHordeMod.Settings.trickleMaxGroupSize);
                ZombieSpawnEventType behavior = PickRandomTrickleBehavior();
                if (!ZombieSpawnHelper.SpawnByBehavior(map, behavior, count, sendLetter: false, applyDifficulty: true, ignoreCap: false, ignoreTimeOfDay: false))
                {
                    ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: false, behavior: behavior);
                }
            }

            ScheduleNextTrickle(ticksGame);
        }

        private ZombieSpawnEventType PickRandomTrickleBehavior()
        {
            float roll = Rand.Value;
            if (roll < 0.40f)
            {
                return ZombieSpawnEventType.EdgeWander;
            }

            if (roll < 0.70f)
            {
                return ZombieSpawnEventType.HuddledPack;
            }

            return ZombieSpawnEventType.AssaultBase;
        }

        private ZombieSpawnEventType PickNightlyBehavior()
        {
            float roll = Rand.Value;
            if (roll < 0.45f)
            {
                return ZombieSpawnEventType.AssaultBase;
            }

            if (roll < 0.75f)
            {
                return ZombieSpawnEventType.EdgeWander;
            }

            return ZombieSpawnEventType.HuddledPack;
        }

        private void ScheduleNextTrickle(int ticksGame)
        {
            float hours = Mathf.Max(0.25f, CustomizableZombieHordeMod.Settings.trickleIntervalHours * Rand.Range(0.85f, 1.15f));
            nextTrickleTick = ticksGame + HoursToTicks(hours);
        }

        private void HandleGroundBursts(int ticksGame)
        {
            if (!CustomizableZombieHordeMod.Settings.enableGroundBursts)
            {
                return;
            }

            if (nextGroundBurstTick < 0)
            {
                ScheduleNextGroundBurst(ticksGame);
                return;
            }

            if (ticksGame < nextGroundBurstTick)
            {
                return;
            }

            List<Map> homeMaps = Find.Maps.Where(map => map.IsPlayerHome).ToList();
            if (homeMaps.Count > 0)
            {
                ZombieSpawnHelper.SpawnGroundBurst(homeMaps.RandomElement());
            }

            ScheduleNextGroundBurst(ticksGame);
        }

        private void ScheduleNextGroundBurst(int ticksGame)
        {
            float minDays = Mathf.Max(1f, CustomizableZombieHordeMod.Settings.groundBurstMinDays);
            float maxDays = Mathf.Max(minDays, CustomizableZombieHordeMod.Settings.groundBurstMaxDays);
            nextGroundBurstTick = ticksGame + DaysToTicks(Rand.Range(minDays, maxDays));
        }

        private void HandleGraveEvents(int ticksGame)
        {
            if (!CustomizableZombieHordeMod.Settings.enableGraveEvents)
            {
                return;
            }

            if (nextGraveEventTick < 0)
            {
                ScheduleNextGraveEvent(ticksGame);
                return;
            }

            if (ticksGame < nextGraveEventTick)
            {
                return;
            }

            List<Map> homeMaps = Find.Maps.Where(map => map.IsPlayerHome).ToList();
            if (homeMaps.Count > 0)
            {
                ZombieSpawnHelper.SpawnRandomGraveEvent(homeMaps.RandomElement(), sendLetter: true);
                RefreshCurrentMapCount();
            }

            ScheduleNextGraveEvent(ticksGame);
        }

        private void ScheduleNextGraveEvent(int ticksGame)
        {
            float minDays = Mathf.Max(3f, CustomizableZombieHordeMod.Settings.graveEventMinDays);
            float maxDays = Mathf.Max(minDays, CustomizableZombieHordeMod.Settings.graveEventMaxDays);
            nextGraveEventTick = ticksGame + DaysToTicks(Rand.Range(minDays, maxDays));
        }

        private void HandleMoonCycle()
        {
            if (!CustomizableZombieHordeMod.Settings.enableMoonEvents)
            {
                return;
            }

            int currentDay = Find.TickManager.TicksGame / GenDate.TicksPerDay;
            if (nextMoonEventDay < 0)
            {
                ScheduleNextMoon(currentDay);
                return;
            }

            if (currentDay >= nextMoonEventDay - 7 && lastSevenDayWarningForMoon != nextMoonEventDay)
            {
                SendMoonWarning(daysRemaining: 7, isBloodMoon: nextMoonIsBlood);
                lastSevenDayWarningForMoon = nextMoonEventDay;
            }

            if (currentDay >= nextMoonEventDay - 1 && lastOneDayWarningForMoon != nextMoonEventDay)
            {
                SendMoonWarning(daysRemaining: 1, isBloodMoon: nextMoonIsBlood);
                lastOneDayWarningForMoon = nextMoonEventDay;
            }

            if (currentDay < nextMoonEventDay || lastTriggeredMoonDay == nextMoonEventDay)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (!map.IsPlayerHome)
                {
                    continue;
                }

                string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
                if (nextMoonIsBlood)
                {
                    int targetCount = ZombieSpawnHelper.GetRemainingCapacity(map, ZombiePopulationState.BloodMoon);
                    if (targetCount < 1)
                    {
                        targetCount = ZombieSpawnHelper.GetDynamicZombieCap(map, ZombiePopulationState.BloodMoon);
                    }

                    if (ZombieSpawnHelper.SpawnHorde(
                        map,
                        targetCount,
                        groups: 3,
                        letterLabel: "Blood Moon Rising",
                        letterText: "A blood moon hangs over the colony. A massive wave of " + prefix.ToLowerInvariant() + "s surges toward your base from every direction. Keep everyone inside your defenses and expect a sustained assault.",
                        ignoreTimeOfDay: true,
                        populationState: ZombiePopulationState.BloodMoon))
                    {
                        ForceMoonRush(map, true);
                    }
                }
                else
                {
                    int targetCount = ZombieSpawnHelper.GetRemainingCapacity(map, ZombiePopulationState.FullMoon);
                    if (targetCount < 1)
                    {
                        targetCount = ZombieSpawnHelper.GetDynamicZombieCap(map, ZombiePopulationState.FullMoon);
                    }

                    if (ZombieSpawnHelper.SpawnHorde(
                        map,
                        targetCount,
                        groups: 2,
                        letterLabel: "Full Moon Rising",
                        letterText: "The full moon draws the dead from the dark. A larger horde of " + prefix.ToLowerInvariant() + "s is converging on your colony. Bring wandering workers home and prepare your perimeter.",
                        ignoreTimeOfDay: true,
                        populationState: ZombiePopulationState.FullMoon))
                    {
                        ForceMoonRush(map, false);
                    }
                }
            }

            lastTriggeredMoonDay = nextMoonEventDay;
            ScheduleNextMoon(currentDay);
        }

        private void ForceMoonRush(Map map, bool bloodMoon)
        {
            if (map == null)
            {
                return;
            }

            ForceAllMapZombiesToRush(map);
            if (bloodMoon)
            {
                StartBloodMoon(map);
            }
        }

        private void StartBloodMoon(Map map)
        {
            int ticksGame = Find.TickManager?.TicksGame ?? 0;
            int duration = HoursToTicks(Rand.Range(4f, 8f));
            bloodMoonActiveUntilTick = ticksGame + duration;
            nextBloodMoonRushTick = ticksGame + HoursToTicks(Rand.Range(0.75f, 1.25f));
            ForceAllMapZombiesToRush(map);
        }

        private void HandleActiveBloodMoon(int ticksGame)
        {
            if (bloodMoonActiveUntilTick < 0)
            {
                return;
            }

            if (ticksGame >= bloodMoonActiveUntilTick)
            {
                bloodMoonActiveUntilTick = -1;
                nextBloodMoonRushTick = -1;
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    ForceAllMapZombiesToRush(map);
                }
            }

            if (nextBloodMoonRushTick >= 0 && ticksGame >= nextBloodMoonRushTick)
            {
                foreach (Map map in Find.Maps)
                {
                    if (!map.IsPlayerHome)
                    {
                        continue;
                    }

                    int remainingCapacity = ZombieSpawnHelper.GetRemainingCapacity(map, ZombiePopulationState.BloodMoon);
                    if (remainingCapacity > 0)
                    {
                        int forcedCount = Mathf.Clamp(Mathf.CeilToInt(remainingCapacity * 0.35f), 1, remainingCapacity);
                        ZombieSpawnHelper.SpawnWave(
                            map,
                            forcedCount: forcedCount,
                            sendLetter: false,
                            applyDifficulty: true,
                            ignoreCap: false,
                            ignoreTimeOfDay: true,
                            behavior: ZombieSpawnEventType.AssaultBase,
                            populationState: ZombiePopulationState.BloodMoon);
                    }

                    ForceAllMapZombiesToRush(map);
                }

                nextBloodMoonRushTick = ticksGame + HoursToTicks(Rand.Range(0.75f, 1.25f));
            }
        }

        private void ForceAllMapZombiesToRush(Map map)
        {
            if (map?.mapPawns == null)
            {
                return;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!ZombieUtility.IsZombie(pawn) || pawn.Dead || pawn.Downed || ZombieLurkerUtility.IsColonyLurker(pawn))
                {
                    continue;
                }

                RegisterBehavior(pawn, ZombieSpawnEventType.AssaultBase);
                ZombieUtility.EnsureZombieAggression(pawn);
                ZombieUtility.AssignInitialShambleJob(pawn, ZombieSpawnEventType.AssaultBase);
            }
        }

        private void ScheduleNextMoon(int currentDay)
        {
            nextMoonEventDay = currentDay + Rand.RangeInclusive(28, 32);
            nextMoonIsBlood = Rand.Chance(CustomizableZombieHordeMod.Settings.bloodMoonChance);
            lastSevenDayWarningForMoon = -1;
            lastOneDayWarningForMoon = -1;
        }

        private void SendMoonWarning(int daysRemaining, bool isBloodMoon)
        {
            List<Map> homeMaps = Find.Maps.Where(map => map.IsPlayerHome).ToList();
            if (homeMaps.Count == 0)
            {
                return;
            }

            Map map = homeMaps[0];
            string label = isBloodMoon ? "Blood Moon Omen" : "Full Moon Omen";
            string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings.zombiePrefix);
            string text = isBloodMoon
                ? (daysRemaining == 1
                    ? "Tomorrow night, a blood moon will rise. Expect a massive surge of " + prefix.ToLowerInvariant() + "s and keep everyone close to your defenses."
                    : "In about a week, a blood moon will rise. The dead will gather in far greater numbers than usual, so use the time to stock medicine and shore up defenses.")
                : (daysRemaining == 1
                    ? "Tomorrow night, the full moon will rise. A larger wave of " + prefix.ToLowerInvariant() + "s is coming, so call workers back behind the perimeter before nightfall."
                    : "In about a week, the next full moon will rise. The dead are beginning to stir, so use the warning to prepare traps, medicine, and fallback positions.");

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(map.Center, map));
        }

        private void SanitizeLivingZombies()
        {
            HashSet<int> liveZombieIds = new HashSet<int>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!ZombieUtility.IsZombie(pawn))
                    {
                        if (ZombieRulesUtility.IsZombieAlignedCritter(pawn))
                        {
                            ZombieUtility.SetZombieDisplayName(pawn);
                        }

                        continue;
                    }

                    liveZombieIds.Add(pawn.thingIDNumber);

                    if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieRot) != true)
                    {
                        ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
                    }
                    else if (!ZombieInfectionUtility.HasReanimatedState(pawn))
                    {
                        ZombieInfectionUtility.ApplyReanimatedState(pawn);
                    }

                    ZombieUtility.SetZombieDisplayName(pawn);

                    if (ZombieLurkerUtility.IsLurker(pawn))
                    {
                        ZombieLurkerUtility.EnsureLurkerZombiePassiveTrait(pawn);
                        ZombieLurkerUtility.EnsureEmotionlessLurker(pawn);
                    }

                    if (ZombieLurkerUtility.IsColonyLurker(pawn))
                    {
                        ZombieLurkerUtility.EnsureColonyLurkerState(pawn);
                        ZombieUtility.RefreshDrownedState(pawn);
                        continue;
                    }

                    ZombieUtility.StripAllUsableItems(pawn);
                    ZombieUtility.MarkZombieApparelTainted(pawn, degradeApparel: false);
                    ZombieUtility.RefreshDrownedState(pawn);
                    ZombieUtility.HandleDrownedRegeneration(pawn);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    ZombieSpecialUtility.HandleDrownedBehavior(pawn);
                    ZombieSpecialUtility.HandleSickSpitAttack(pawn);
                }
            }

            if (zombieBehaviorByPawnId != null)
            {
                List<int> staleIds = zombieBehaviorByPawnId.Keys.Where(id => !liveZombieIds.Contains(id)).ToList();
                foreach (int staleId in staleIds)
                {
                    zombieBehaviorByPawnId.Remove(staleId);
                }
            }
        }

        private void HandleZombieFeeding()
        {
            foreach (Map map in Find.Maps)
            {
                ZombieSpecialUtility.HandleBoneBiterFeeding(map);
                ZombieSpecialUtility.HandleCorpseFeeding(map);
            }
        }

        private void HandleSickBloodContact()
        {
            foreach (Map map in Find.Maps)
            {
                ZombieSpecialUtility.HandleSickBloodContact(map);
            }
        }

        private void HandleZombieReanimation()
        {
            int ticksGame = Find.TickManager.TicksGame;
            HashSet<int> seenZombieCorpseIds = new HashSet<int>();
            HashSet<int> seenReanimatedCorpseIds = new HashSet<int>();

            foreach (Map map in Find.Maps)
            {
                List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
                foreach (Corpse corpse in corpses)
                {
                    Pawn pawn = corpse.InnerPawn;
                    if (pawn == null)
                    {
                        continue;
                    }

                    int corpseId = corpse.thingIDNumber;
                    bool isZombie = ZombieUtility.IsZombie(pawn);
                    bool hasReanimated = ZombieInfectionUtility.HasReanimatedState(pawn);

                    if (isZombie && !hasReanimated)
                    {
                        ZombieInfectionUtility.ApplyReanimatedState(pawn);
                        hasReanimated = ZombieInfectionUtility.HasReanimatedState(pawn);
                    }

                    if (hasReanimated)
                    {
                        seenZombieCorpseIds.Add(corpseId);
                        seenReanimatedCorpseIds.Add(corpseId);
                        corpseWakeTicks.Remove(corpseId);

                        if (!infectionReanimationTicks.TryGetValue(corpseId, out int reanimatedWakeTick))
                        {
                            ScheduleInfectionReanimation(corpse, fixedDelayTicks: GenDate.TicksPerHour);
                            continue;
                        }

                        if (ticksGame < reanimatedWakeTick)
                        {
                            continue;
                        }

                        bool canAttemptResurrection = ZombieInfectionUtility.CanReanimateFromReanimatedState(pawn);
                        bool shouldRiseThisHour = canAttemptResurrection && Rand.Chance(0.50f);
                        if (shouldRiseThisHour)
                        {
                            infectionReanimationTicks.Remove(corpseId);
                            infectionReanimationStartTicks?.Remove(corpseId);
                            infectionReanimationStartSeverity?.Remove(corpseId);

                            if (isZombie)
                            {
                                if (ZombieUtility.TryResurrectZombie(pawn))
                                {
                                    ZombieUtility.PrepareZombieForReanimation(pawn);
                                    ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
                                    ZombieUtility.EnsureZombieAggression(pawn);
                                    ZombieFeedbackUtility.TrySendReanimationWarning(pawn);
                                }
                                else
                                {
                                    ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
                                }
                            }
                            else if (!ZombieInfectionUtility.TryTurnDeadInfectedPawn(pawn, this))
                            {
                                ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
                            }
                        }
                        else
                        {
                            ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
                        }

                        continue;
                    }

                    if (isZombie)
                    {
                        seenZombieCorpseIds.Add(corpseId);

                        if (!ZombieUtility.CanReanimate(pawn))
                        {
                            corpseWakeTicks.Remove(corpseId);
                        }
                        else
                        {
                            if (!corpseWakeTicks.TryGetValue(corpseId, out int wakeTick))
                            {
                                corpseWakeTicks[corpseId] = ticksGame + HoursToTicks(CustomizableZombieHordeMod.Settings.resurrectionDelayHours * Rand.Range(0.8f, 1.25f));
                            }
                            else if (ticksGame >= wakeTick)
                            {
                                corpseWakeTicks.Remove(corpseId);
                                if (ZombieUtility.TryResurrectZombie(pawn))
                                {
                                    ZombieUtility.PrepareZombieForReanimation(pawn);
                                    ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
                                    ZombieUtility.EnsureZombieAggression(pawn);
                                    ZombieFeedbackUtility.TrySendReanimationWarning(pawn);
                                }
                            }
                        }
                    }
                }
            }

            List<int> staleZombieIds = corpseWakeTicks.Keys.Where(key => !seenZombieCorpseIds.Contains(key)).ToList();
            foreach (int staleId in staleZombieIds)
            {
                corpseWakeTicks.Remove(staleId);
            }

            List<int> staleInfectionIds = infectionReanimationTicks.Keys.Where(key => !seenReanimatedCorpseIds.Contains(key)).ToList();
            foreach (int staleId in staleInfectionIds)
            {
                infectionReanimationTicks.Remove(staleId);
                infectionReanimationStartTicks?.Remove(staleId);
                infectionReanimationStartSeverity?.Remove(staleId);
            }
        }

        public void MarkInfectionHeadFatal(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            infectionHeadFatalPawnIds ??= new List<int>();
            if (!infectionHeadFatalPawnIds.Contains(pawn.thingIDNumber))
            {
                infectionHeadFatalPawnIds.Add(pawn.thingIDNumber);
            }
        }

        public bool IsInfectionHeadFatal(Pawn pawn)
        {
            return pawn != null && infectionHeadFatalPawnIds != null && infectionHeadFatalPawnIds.Contains(pawn.thingIDNumber);
        }

        public void ClearInfectionHeadFatal(Pawn pawn)
        {
            if (pawn == null || infectionHeadFatalPawnIds == null)
            {
                return;
            }

            infectionHeadFatalPawnIds.Remove(pawn.thingIDNumber);
        }

        public void MarkInfectionShouldBecomeLurker(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            infectionLurkerPawnIds ??= new List<int>();
            if (!infectionLurkerPawnIds.Contains(pawn.thingIDNumber))
            {
                infectionLurkerPawnIds.Add(pawn.thingIDNumber);
            }
        }

        public bool ShouldBecomeLurkerAfterInfection(Pawn pawn)
        {
            return pawn != null && infectionLurkerPawnIds != null && infectionLurkerPawnIds.Contains(pawn.thingIDNumber);
        }

        public void ClearInfectionShouldBecomeLurker(Pawn pawn)
        {
            if (pawn == null || infectionLurkerPawnIds == null)
            {
                return;
            }

            infectionLurkerPawnIds.Remove(pawn.thingIDNumber);
        }

        public void RegisterDeadPawnForPostMortemInfection(Pawn pawn)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed || Find.TickManager == null)
            {
                return;
            }

            Hediff infection = ZombieInfectionUtility.GetZombieInfection(pawn);
            if (infection == null)
            {
                return;
            }

            pendingDeadInfectionSeverityByPawnId ??= new Dictionary<int, float>();
            pendingDeadInfectionSeverityByPawnId[pawn.thingIDNumber] = Mathf.Clamp01(infection.Severity);

            Corpse corpse = FindCorpseForPawn(pawn);
            if (corpse != null && !corpse.Destroyed)
            {
                RegisterDeadInfectedCorpse(corpse, infection.Severity, forceReschedule: true);
            }
        }

        public void RegisterDeadInfectedCorpse(Corpse corpse, float startSeverity, bool forceReschedule = false)
        {
            if (corpse == null || corpse.Destroyed || Find.TickManager == null)
            {
                return;
            }

            Pawn innerPawn = corpse.InnerPawn;
            if (innerPawn == null || !innerPawn.Dead || innerPawn.Destroyed || ZombieInfectionUtility.HasReanimatedState(innerPawn))
            {
                return;
            }

            deadInfectionProgressStartTicks ??= new Dictionary<int, int>();
            deadInfectionProgressEndTicks ??= new Dictionary<int, int>();
            deadInfectionProgressStartSeverity ??= new Dictionary<int, float>();

            int corpseId = corpse.thingIDNumber;
            if (!forceReschedule && deadInfectionProgressEndTicks.ContainsKey(corpseId))
            {
                return;
            }

            int startTick = Find.TickManager.TicksGame;
            deadInfectionProgressStartTicks[corpseId] = startTick;
            deadInfectionProgressStartSeverity[corpseId] = Mathf.Clamp(startSeverity, ZombieInfectionUtility.InitialInfectionSeverity, 1f);
            deadInfectionProgressEndTicks[corpseId] = startTick + Rand.RangeInclusive(GenDate.TicksPerHour, GenDate.TicksPerHour * 3);
            pendingDeadInfectionSeverityByPawnId?.Remove(innerPawn.thingIDNumber);
        }

        public void ClearDeadInfectedCorpse(Corpse corpse)
        {
            if (corpse == null)
            {
                return;
            }

            deadInfectionProgressStartTicks?.Remove(corpse.thingIDNumber);
            deadInfectionProgressEndTicks?.Remove(corpse.thingIDNumber);
            deadInfectionProgressStartSeverity?.Remove(corpse.thingIDNumber);
        }

        private void HandleDeadInfectedCorpseProgression()
        {
            if (Current.Game == null || Find.TickManager == null)
            {
                return;
            }

            int ticksGame = Find.TickManager.TicksGame;
            HashSet<int> seenCorpseIds = new HashSet<int>();
            HashSet<int> seenPendingPawnIds = new HashSet<int>();

            foreach (Map map in Find.Maps)
            {
                List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
                foreach (Corpse corpse in corpses)
                {
                    Pawn innerPawn = corpse?.InnerPawn;
                    if (innerPawn == null || !innerPawn.Dead || innerPawn.Destroyed)
                    {
                        continue;
                    }

                    int corpseId = corpse.thingIDNumber;
                    bool hasReanimated = ZombieInfectionUtility.HasReanimatedState(innerPawn);
                    bool isTracked = deadInfectionProgressEndTicks != null && deadInfectionProgressEndTicks.ContainsKey(corpseId);
                    Hediff infection = ZombieInfectionUtility.GetZombieInfection(innerPawn);
                    float pendingStartSeverity = ZombieInfectionUtility.InitialInfectionSeverity;
                    bool hasPendingSeverity = pendingDeadInfectionSeverityByPawnId != null
                        && pendingDeadInfectionSeverityByPawnId.TryGetValue(innerPawn.thingIDNumber, out pendingStartSeverity);

                    if (!hasReanimated && (infection != null || isTracked || hasPendingSeverity))
                    {
                        seenCorpseIds.Add(corpseId);
                        if (hasPendingSeverity)
                        {
                            seenPendingPawnIds.Add(innerPawn.thingIDNumber);
                        }

                        if (!isTracked)
                        {
                            float pendingOrCurrentSeverity = infection != null ? infection.Severity : pendingStartSeverity;
                            RegisterDeadInfectedCorpse(corpse, pendingOrCurrentSeverity, forceReschedule: true);
                            isTracked = true;
                        }

                        if (infection == null)
                        {
                            float restoreSeverity = ZombieInfectionUtility.InitialInfectionSeverity;
                            if (deadInfectionProgressStartSeverity != null && deadInfectionProgressStartSeverity.TryGetValue(corpseId, out float storedStartSeverity))
                            {
                                restoreSeverity = storedStartSeverity;
                            }
                            else if (hasPendingSeverity)
                            {
                                restoreSeverity = pendingStartSeverity;
                            }

                            infection = ZombieInfectionUtility.EnsureZombieInfection(innerPawn, restoreSeverity);
                            if (infection == null)
                            {
                                continue;
                            }
                        }

                        int startTick = ticksGame;
                        int endTick = ticksGame;
                        if (deadInfectionProgressStartTicks != null)
                        {
                            deadInfectionProgressStartTicks.TryGetValue(corpseId, out startTick);
                        }
                        if (deadInfectionProgressEndTicks != null)
                        {
                            deadInfectionProgressEndTicks.TryGetValue(corpseId, out endTick);
                        }

                        float startSeverity = infection.Severity;
                        if (deadInfectionProgressStartSeverity != null && deadInfectionProgressStartSeverity.TryGetValue(corpseId, out float storedSeverity))
                        {
                            startSeverity = storedSeverity;
                        }

                        float progress = endTick > startTick ? Mathf.InverseLerp(startTick, endTick, ticksGame) : 1f;
                        float progressedSeverity = Mathf.Lerp(startSeverity, 1f, progress);
                        infection.Severity = Mathf.Clamp(Mathf.Max(infection.Severity, progressedSeverity), ZombieInfectionUtility.InitialInfectionSeverity, 1f);

                        if (infection.Severity >= 0.999f)
                        {
                            infection.Severity = 1f;
                            ZombieInfectionUtility.PromoteDeadInfectionToReanimated(innerPawn, this);
                            ClearDeadInfectedCorpse(corpse);
                        }
                    }
                    else if (hasReanimated)
                    {
                        ClearDeadInfectedCorpse(corpse);
                    }
                }
            }

            List<int> staleCorpseIds = deadInfectionProgressEndTicks.Keys.Where(key => !seenCorpseIds.Contains(key)).ToList();
            foreach (int staleId in staleCorpseIds)
            {
                deadInfectionProgressEndTicks.Remove(staleId);
                deadInfectionProgressStartTicks?.Remove(staleId);
                deadInfectionProgressStartSeverity?.Remove(staleId);
            }

            if (pendingDeadInfectionSeverityByPawnId != null)
            {
                List<int> stalePendingIds = pendingDeadInfectionSeverityByPawnId.Keys.Where(id => seenPendingPawnIds.Contains(id)).ToList();
                foreach (int staleId in stalePendingIds)
                {
                    pendingDeadInfectionSeverityByPawnId.Remove(staleId);
                }
            }
        }

        public void RegisterDeadPawnForRecurringReanimation(Pawn pawn)
        {
            if (pawn == null || !pawn.Dead || pawn.Destroyed || Find.TickManager == null)
            {
                return;
            }

            Corpse corpse = FindCorpseForPawn(pawn);
            if (corpse == null || corpse.Destroyed)
            {
                return;
            }

            ZombieInfectionUtility.ApplyReanimatedState(pawn);
            ScheduleInfectionReanimation(corpse, forceReschedule: true, fixedDelayTicks: GenDate.TicksPerHour);
        }

        private static Corpse FindCorpseForPawn(Pawn pawn)
        {
            if (pawn?.Corpse != null && !pawn.Corpse.Destroyed)
            {
                return pawn.Corpse;
            }

            Map map = pawn?.MapHeld;
            if (map != null && pawn.PositionHeld.IsValid && pawn.PositionHeld.InBounds(map))
            {
                Corpse localCorpse = map.thingGrid?.ThingsAt(pawn.PositionHeld)?.OfType<Corpse>()?.FirstOrDefault(c => c.InnerPawn == pawn);
                if (localCorpse != null && !localCorpse.Destroyed)
                {
                    return localCorpse;
                }
            }

            foreach (Map searchMap in Find.Maps)
            {
                Corpse found = searchMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().FirstOrDefault(c => c.InnerPawn == pawn);
                if (found != null && !found.Destroyed)
                {
                    return found;
                }
            }

            return null;
        }

        public void ScheduleInfectionReanimation(Corpse corpse, bool forceReschedule = false, int? fixedDelayTicks = null)
        {
            if (corpse == null || Find.TickManager == null)
            {
                return;
            }

            infectionReanimationTicks ??= new Dictionary<int, int>();
            infectionReanimationStartTicks ??= new Dictionary<int, int>();
            infectionReanimationStartSeverity ??= new Dictionary<int, float>();
            deadInfectionProgressStartTicks ??= new Dictionary<int, int>();
            deadInfectionProgressEndTicks ??= new Dictionary<int, int>();
            deadInfectionProgressStartSeverity ??= new Dictionary<int, float>();
            pendingDeadInfectionSeverityByPawnId ??= new Dictionary<int, float>();
            int corpseId = corpse.thingIDNumber;
            if (!forceReschedule && infectionReanimationTicks.ContainsKey(corpseId))
            {
                return;
            }

            int startTick = Find.TickManager.TicksGame;
            infectionReanimationStartTicks[corpseId] = startTick;
            Hediff infection = ZombieInfectionUtility.GetZombieInfection(corpse.InnerPawn);
            if (infection != null && !ZombieInfectionUtility.HasReanimatedState(corpse.InnerPawn))
            {
                infectionReanimationStartSeverity[corpseId] = Mathf.Clamp01(infection.Severity);
            }
            else
            {
                infectionReanimationStartSeverity.Remove(corpseId);
            }

            int delayTicks = fixedDelayTicks ?? Rand.RangeInclusive(GenDate.TicksPerHour, GenDate.TicksPerHour * 3);
            infectionReanimationTicks[corpseId] = startTick + delayTicks;
        }

        public void ClearInfectionReanimation(Corpse corpse)
        {
            if (corpse == null || infectionReanimationTicks == null)
            {
                return;
            }

            infectionReanimationTicks.Remove(corpse.thingIDNumber);
            infectionReanimationStartTicks?.Remove(corpse.thingIDNumber);
            infectionReanimationStartSeverity?.Remove(corpse.thingIDNumber);
        }

        public bool TryGetInfectionReanimationTick(Corpse corpse, out int wakeTick)
        {
            wakeTick = -1;
            return corpse != null
                && infectionReanimationTicks != null
                && infectionReanimationTicks.TryGetValue(corpse.thingIDNumber, out wakeTick);
        }

        public bool TryGetInfectionReanimationWindow(Corpse corpse, out int startTick, out int wakeTick)
        {
            startTick = -1;
            wakeTick = -1;
            if (corpse == null || infectionReanimationTicks == null || !infectionReanimationTicks.TryGetValue(corpse.thingIDNumber, out wakeTick))
            {
                return false;
            }

            if (infectionReanimationStartTicks != null && infectionReanimationStartTicks.TryGetValue(corpse.thingIDNumber, out startTick))
            {
                return true;
            }

            startTick = wakeTick - GenDate.TicksPerHour;
            return true;
        }

        public bool TryGetInfectionReanimationStartSeverity(Corpse corpse, out float startSeverity)
        {
            startSeverity = 0f;
            return corpse != null
                && infectionReanimationStartSeverity != null
                && infectionReanimationStartSeverity.TryGetValue(corpse.thingIDNumber, out startSeverity);
        }

        private void HandleZombieInfectionProgression()
        {
            if (Current.Game == null)
            {
                return;
            }

            HashSet<int> seen = new HashSet<int>();
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
                {
                    if (ZombieUtility.IsZombie(pawn) && !ZombieInfectionUtility.HasReanimatedState(pawn))
                    {
                        ZombieInfectionUtility.ApplyReanimatedState(pawn);
                    }

                    if (!ZombieInfectionUtility.HasZombieInfection(pawn))
                    {
                        continue;
                    }

                    seen.Add(pawn.thingIDNumber);
                    ZombieInfectionUtility.ProgressLivingInfection(pawn, this);
                }

                foreach (Corpse corpse in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
                {
                    Pawn innerPawn = corpse?.InnerPawn;
                    if (innerPawn == null)
                    {
                        continue;
                    }

                    if (ZombieInfectionUtility.HasZombieInfection(innerPawn)
                        || ZombieInfectionUtility.HasReanimatedState(innerPawn)
                        || (deadInfectionProgressEndTicks != null && deadInfectionProgressEndTicks.ContainsKey(corpse.thingIDNumber)))
                    {
                        seen.Add(innerPawn.thingIDNumber);
                    }
                }
            }

            if (infectionHeadFatalPawnIds == null)
            {
                return;
            }

            infectionHeadFatalPawnIds.RemoveAll(id => !seen.Contains(id) && !(pendingDeadInfectionSeverityByPawnId?.ContainsKey(id) == true));
            infectionLurkerPawnIds?.RemoveAll(id => !seen.Contains(id) && !(pendingDeadInfectionSeverityByPawnId?.ContainsKey(id) == true));
        }

        private static int DaysToTicks(float days)
        {
            return (int)(days * GenDate.TicksPerDay);
        }

        private static int HoursToTicks(float hours)
        {
            return (int)(GenDate.TicksPerHour * hours);
        }
    }
}
