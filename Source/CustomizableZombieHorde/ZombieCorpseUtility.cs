using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

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
                MethodInfo setForbiddenMethod = corpse.GetType().GetMethod("SetForbidden", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(bool), typeof(bool) }, null);
                if (setForbiddenMethod != null)
                {
                    setForbiddenMethod.Invoke(corpse, new object[] { false, false });
                    return;
                }

                if (corpse.AllComps != null)
                {
                    foreach (ThingComp comp in corpse.AllComps)
                    {
                        if (comp == null || comp.GetType().Name != "CompForbiddable")
                        {
                            continue;
                        }

                        PropertyInfo forbiddenProperty = comp.GetType().GetProperty("Forbidden", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (forbiddenProperty != null && forbiddenProperty.CanWrite)
                        {
                            forbiddenProperty.SetValue(comp, false, null);
                            return;
                        }
                    }
                }
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
