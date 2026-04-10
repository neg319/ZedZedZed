using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    public static class ZombieCorpseUtility
    {
        public static bool ShouldAutoAllowZombieCorpses => CustomizableZombieHordeMod.Settings?.autoAllowZombieCorpses ?? true;

        public static bool IsZombieCorpse(Corpse corpse)
        {
            return corpse?.InnerPawn != null && ZombieUtility.IsZombie(corpse.InnerPawn);
        }

        public static void ApplyDefaultAllowState(Corpse corpse)
        {
            if (!ShouldAutoAllowZombieCorpses || corpse == null || corpse.Destroyed || !IsZombieCorpse(corpse))
            {
                return;
            }

            try
            {
                corpse.SetForbidden(false, false);
            }
            catch
            {
            }
        }

        public static void EnsureZombieCorpsesAllowed(Map map)
        {
            if (!ShouldAutoAllowZombieCorpses || map == null)
            {
                return;
            }

            List<Corpse> corpses = map.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse)?.OfType<Corpse>()?.ToList();
            if (corpses == null)
            {
                return;
            }

            foreach (Corpse corpse in corpses)
            {
                ApplyDefaultAllowState(corpse);
            }
        }
    }
}
