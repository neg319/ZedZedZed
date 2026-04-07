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

        public ZombieGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextTrickleTick, "nextTrickleTick", -1);
            Scribe_Values.Look(ref nextGroundBurstTick, "nextGroundBurstTick", -1);
            Scribe_Values.Look(ref nextGraveEventTick, "nextGraveEventTick", -1);
            Scribe_Collections.Look(ref corpseWakeTicks, "corpseWakeTicks", LookMode.Value, LookMode.Value);
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
            base.ExposeData();
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
            }

            if (ticksGame % 12 == 0)
            {
                ZombieGrabberUtility.TickGrabbers();
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
            Messages.Message("A debug bone biter has been spawned.", boneBiter, MessageTypeDefOf.NeutralEvent);
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
                        continue;
                    }

                    liveZombieIds.Add(pawn.thingIDNumber);

                    if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieRot) != true)
                    {
                        ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
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
            HashSet<int> seenCorpseIds = new HashSet<int>();

            foreach (Map map in Find.Maps)
            {
                List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
                foreach (Corpse corpse in corpses)
                {
                    Pawn pawn = corpse.InnerPawn;
                    if (!ZombieUtility.IsZombie(pawn))
                    {
                        continue;
                    }

                    int corpseId = corpse.thingIDNumber;
                    seenCorpseIds.Add(corpseId);

                    if (!ZombieUtility.CanReanimate(pawn))
                    {
                        corpseWakeTicks.Remove(corpseId);
                        continue;
                    }

                    if (!corpseWakeTicks.TryGetValue(corpseId, out int wakeTick))
                    {
                        corpseWakeTicks[corpseId] = ticksGame + HoursToTicks(CustomizableZombieHordeMod.Settings.resurrectionDelayHours * Rand.Range(0.8f, 1.25f));
                        continue;
                    }

                    if (ticksGame < wakeTick)
                    {
                        continue;
                    }

                    corpseWakeTicks.Remove(corpseId);
                    if (!ZombieUtility.TryResurrectZombie(pawn))
                    {
                        continue;
                    }

                    ZombieUtility.PrepareZombieForReanimation(pawn);
                    ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    ZombieFeedbackUtility.TrySendReanimationWarning(pawn);
                }
            }

            List<int> staleIds = corpseWakeTicks.Keys.Where(key => !seenCorpseIds.Contains(key)).ToList();
            foreach (int staleId in staleIds)
            {
                corpseWakeTicks.Remove(staleId);
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
                    if (!ZombieInfectionUtility.HasZombieInfection(pawn))
                    {
                        continue;
                    }

                    seen.Add(pawn.thingIDNumber);
                    ZombieInfectionUtility.ProgressLivingInfection(pawn, this);
                }

                List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().ToList();
                foreach (Corpse corpse in corpses)
                {
                    Pawn innerPawn = corpse?.InnerPawn;
                    if (!ZombieInfectionUtility.HasZombieInfection(innerPawn))
                    {
                        continue;
                    }

                    seen.Add(innerPawn.thingIDNumber);
                    ZombieInfectionUtility.ProgressDeadInfection(innerPawn, this);
                }
            }

            if (infectionHeadFatalPawnIds == null)
            {
                return;
            }

            infectionHeadFatalPawnIds.RemoveAll(id => !seen.Contains(id));
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
