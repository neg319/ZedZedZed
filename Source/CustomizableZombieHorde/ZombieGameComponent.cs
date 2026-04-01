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
        private Dictionary<int, int> corpseWakeTicks = new Dictionary<int, int>();
        private int nextMoonEventDay = -1;
        private bool nextMoonIsBlood;
        private int lastSevenDayWarningForMoon = -1;
        private int lastOneDayWarningForMoon = -1;
        private int lastTriggeredMoonDay = -1;
        private int cachedCurrentMapZombieCount = 0;
        private int cachedZombieCountTick = -1;
        private int lastGuaranteeAttemptTick = -1;
        private Dictionary<int, int> lastNightlySpawnDayByMap = new Dictionary<int, int>();

        public ZombieGameComponent(Game game)
        {
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref nextTrickleTick, "nextTrickleTick", -1);
            Scribe_Values.Look(ref nextGroundBurstTick, "nextGroundBurstTick", -1);
            Scribe_Collections.Look(ref corpseWakeTicks, "corpseWakeTicks", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref nextMoonEventDay, "nextMoonEventDay", -1);
            Scribe_Values.Look(ref nextMoonIsBlood, "nextMoonIsBlood", false);
            Scribe_Values.Look(ref lastSevenDayWarningForMoon, "lastSevenDayWarningForMoon", -1);
            Scribe_Values.Look(ref lastOneDayWarningForMoon, "lastOneDayWarningForMoon", -1);
            Scribe_Values.Look(ref lastTriggeredMoonDay, "lastTriggeredMoonDay", -1);
            Scribe_Values.Look(ref cachedCurrentMapZombieCount, "cachedCurrentMapZombieCount", 0);
            Scribe_Values.Look(ref cachedZombieCountTick, "cachedZombieCountTick", -1);
            Scribe_Values.Look(ref lastGuaranteeAttemptTick, "lastGuaranteeAttemptTick", -1);
            Scribe_Collections.Look(ref lastNightlySpawnDayByMap, "lastNightlySpawnDayByMap", LookMode.Value, LookMode.Value);
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
            bool result = ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: true, customLetterLabel: "Debug Edge Wave", customLetterText: "A debug zombie wave has been forced from the map edge.");
            if (!result)
            {
                result = ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: true, customLetterLabel: "Debug Edge Wave", customLetterText: "A debug zombie wave has been forced near the map edge.");
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
            bool result = ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: true, customLetterLabel: "Debug Night Wave", customLetterText: "A debug nightly zombie wave has been forced onto the map.");
            if (!result)
            {
                result = ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: true, customLetterLabel: "Debug Night Wave", customLetterText: "A debug nightly zombie wave has been forced near the map edge.");
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

        public bool DebugForceGroundBurst()
        {
            Map map = GetDebugTargetMap();
            if (map == null)
            {
                return false;
            }

            bool result = ZombieSpawnHelper.SpawnGroundBurst(map, forcedCount: Mathf.Max(2, CustomizableZombieHordeMod.Settings.groundBurstMinGroupSize), sendLetter: true);
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
            bool result = bloodMoon
                ? ZombieSpawnHelper.SpawnHorde(
                    map,
                    CustomizableZombieHordeMod.Settings.bloodMoonBaseCount,
                    groups: 3,
                    letterLabel: "Debug Blood Moon",
                    letterText: "A debug blood moon has been forced. A massive wave of " + prefix.ToLowerInvariant() + "s is descending on your colony.")
                : ZombieSpawnHelper.SpawnHorde(
                    map,
                    CustomizableZombieHordeMod.Settings.fullMoonBaseCount,
                    groups: 2,
                    letterLabel: "Debug Full Moon",
                    letterText: "A debug full moon horde has been forced. The dead are closing in on your colony.");

            if (result)
            {
                int currentDay = (Find.TickManager?.TicksGame ?? 0) / GenDate.TicksPerDay;
                lastTriggeredMoonDay = currentDay;
                ScheduleNextMoon(currentDay);
                RefreshCurrentMapCount();
            }

            return result;
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

                int guaranteedCount = Mathf.Max(1, CustomizableZombieHordeMod.Settings?.trickleMinGroupSize ?? 1);
                if (!ZombieSpawnHelper.SpawnWave(map, forcedCount: guaranteedCount, sendLetter: false))
                {
                    ZombieSpawnHelper.SpawnEmergencyPack(map, guaranteedCount, sendLetter: false);
                }

                nextTrickleTick = ticksGame + HoursToTicks(0.50f);
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

                int guaranteedCount = Mathf.Max(2, CustomizableZombieHordeMod.Settings?.trickleMinGroupSize ?? 2);
                bool spawned = ZombieSpawnHelper.SpawnWave(map, forcedCount: guaranteedCount, sendLetter: false);
                if (!spawned)
                {
                    spawned = ZombieSpawnHelper.SpawnEmergencyPack(map, guaranteedCount, sendLetter: false);
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
            int emergencyLeadTime = HoursToTicks(0.75f);
            if (!anyHomeMapHasZombies && (nextTrickleTick < 0 || nextTrickleTick - ticksGame > emergencyLeadTime))
            {
                nextTrickleTick = ticksGame + HoursToTicks(0.10f);
            }

            if (nextTrickleTick < 0)
            {
                nextTrickleTick = ticksGame + HoursToTicks(0.10f);
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
                if (!ZombieSpawnHelper.SpawnWave(map, forcedCount: count, sendLetter: false))
                {
                    ZombieSpawnHelper.SpawnEmergencyPack(map, count, sendLetter: false);
                }
            }

            ScheduleNextTrickle(ticksGame);
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
                    ZombieSpawnHelper.SpawnHorde(
                        map,
                        CustomizableZombieHordeMod.Settings.bloodMoonBaseCount,
                        groups: 3,
                        letterLabel: "Blood Moon Rising",
                        letterText: "A blood moon hangs over the colony. A massive wave of " + prefix.ToLowerInvariant() + "s surges toward your base from every direction.");
                }
                else
                {
                    ZombieSpawnHelper.SpawnHorde(
                        map,
                        CustomizableZombieHordeMod.Settings.fullMoonBaseCount,
                        groups: 2,
                        letterLabel: "Full Moon Rising",
                        letterText: "The full moon draws the dead from the dark. A larger horde of " + prefix.ToLowerInvariant() + "s is converging on your colony.");
                }
            }

            lastTriggeredMoonDay = nextMoonEventDay;
            ScheduleNextMoon(currentDay);
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
                    ? "Tomorrow night, a blood moon will rise. Expect a massive surge of " + prefix.ToLowerInvariant() + "s."
                    : "In about a week, a blood moon will rise. The dead will gather in far greater numbers than usual.")
                : (daysRemaining == 1
                    ? "Tomorrow night, the full moon will rise. A larger wave of " + prefix.ToLowerInvariant() + "s is coming."
                    : "In about a week, the next full moon will rise. The dead are beginning to stir.");

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new TargetInfo(map.Center, map));
        }

        private void SanitizeLivingZombies()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (!ZombieUtility.IsZombie(pawn))
                    {
                        continue;
                    }

                    if (pawn.health?.hediffSet?.HasHediff(ZombieDefOf.CZH_ZombieRot) != true)
                    {
                        ZombiePawnFactory.FinalizeZombie(pawn, initialSpawn: false);
                    }

                    ZombieUtility.StripWeaponsAndWeaponInventory(pawn);
                    ZombieUtility.MarkZombieApparelTainted(pawn, degradeApparel: false);
                    ZombieUtility.RefreshDrownedState(pawn);
                    ZombieUtility.EnsureZombieAggression(pawn);
                    ZombieSpecialUtility.HandleDrownedBehavior(pawn);
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

                    if (ZombieUtility.HasHeadDamageOrDestruction(pawn))
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
                    if (pawn.MapHeld != null && pawn.Faction != null)
                    {
                        try
                        {
                            LordMaker.MakeNewLord(pawn.Faction, new LordJob_AssaultColony(pawn.Faction, false, false, false, false, false), pawn.MapHeld, new List<Pawn> { pawn });
                        }
                        catch
                        {
                            ZombieUtility.EnsureZombieAggression(pawn);
                        }
                    }
                    else
                    {
                        ZombieUtility.EnsureZombieAggression(pawn);
                    }
                }
            }

            List<int> staleIds = corpseWakeTicks.Keys.Where(key => !seenCorpseIds.Contains(key)).ToList();
            foreach (int staleId in staleIds)
            {
                corpseWakeTicks.Remove(staleId);
            }
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
