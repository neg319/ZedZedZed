using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class IncidentWorker_ZombieHorde : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            int forcedCount = GenMath.RoundRandom(parms.points / 35f);
            forcedCount = forcedCount < 3 ? 3 : forcedCount;
            return ZombieSpawnHelper.SpawnWave(map, forcedCount: forcedCount);
        }
    }
}
