using System;
using System.Linq;
using RimWorld;
using Verse;

namespace CustomizableZombieHorde
{
    public static class ZombieFactionUtility
    {
        public static Faction GetOrCreateZombieFaction()
        {
            FactionDef def = DefDatabase<FactionDef>.GetNamedSilentFail("CZH_ZombieHorde");
            Faction existing = def != null ? Find.FactionManager?.FirstFactionOfDef(def) : null;
            if (existing != null)
            {
                return existing;
            }

            if (def != null)
            {
                try
                {
                    Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
                    if (faction != null)
                    {
                        Find.FactionManager?.Add(faction);
                        return faction;
                    }
                }
                catch
                {
                }
            }

            return GetFallbackHostileFaction();
        }

        public static Faction GetFallbackHostileFaction()
        {
            FactionManager manager = Find.FactionManager;
            if (manager == null)
            {
                return null;
            }

            if (Faction.OfAncientsHostile != null)
            {
                return Faction.OfAncientsHostile;
            }

            Faction directEnemy = manager.AllFactionsListForReading.FirstOrDefault(f => f != null && !f.IsPlayer && f.HostileTo(Faction.OfPlayer));
            if (directEnemy != null)
            {
                return directEnemy;
            }

            FactionDef pirateDef = DefDatabase<FactionDef>.GetNamedSilentFail("Pirate")
                ?? DefDatabase<FactionDef>.GetNamedSilentFail("RoughOutlander")
                ?? DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderRough");

            if (pirateDef != null)
            {
                Faction existing = manager.FirstFactionOfDef(pirateDef);
                if (existing != null)
                {
                    return existing;
                }

                try
                {
                    Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(pirateDef));
                    if (faction != null)
                    {
                        manager.Add(faction);
                        return faction;
                    }
                }
                catch
                {
                }
            }

            return manager.AllFactionsListForReading.FirstOrDefault(f => f != null && !f.IsPlayer);
        }
    }
}
