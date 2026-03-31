using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieFactionUtility
    {
        public static Faction GetOrCreateZombieFaction()
        {
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail("CZH_ZombieHorde");
            if (def == null)
            {
                return null;
            }

            Faction existing = Find.FactionManager?.FirstFactionOfDef(def);
            if (existing != null)
            {
                return existing;
            }

            Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
            Find.FactionManager.Add(faction);
            return faction;
        }
    }
}
