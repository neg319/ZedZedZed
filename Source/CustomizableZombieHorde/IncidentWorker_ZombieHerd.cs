using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class IncidentWorker_ZombieHerd : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map map && map.IsPlayerHome;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            return ZombieSpawnHelper.SpawnHerd(map);
        }
    }
}
