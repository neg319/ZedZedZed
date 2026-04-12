using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public class IncidentWorker_LurkerArrives : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null || !map.IsPlayerHome || !base.CanFireNowSub(parms))
            {
                return false;
            }

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (ZombieLurkerUtility.IsPassiveLurker(pawn))
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("CZH_Zombie_Lurker");
            if (kind == null)
            {
                return false;
            }

            Pawn lurker = PawnGenerator.GeneratePawn(kind);
            if (lurker == null)
            {
                return false;
            }

            ZombieLurkerUtility.InitializeLurker(lurker);

            IntVec3 spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 18);
            if (!spawnCell.IsValid)
            {
                spawnCell = map.Center;
            }

            GenSpawn.Spawn(lurker, spawnCell, map);
            ZombieLurkerUtility.EnsurePassiveLurkerBehavior(lurker);

            ZombieFeedbackUtility.SendLurkerArrivalLetter(map, spawnCell);
            return true;
        }
    }
}
