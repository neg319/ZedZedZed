using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
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

        public static bool ShouldPreventRot(Thing thing)
        {
            return thing is Corpse corpse && IsZombieCorpse(corpse);
        }

        public static void FreezeRot(Corpse corpse)
        {
            if (!ShouldPreventRot(corpse))
            {
                return;
            }

            try
            {
                CompRottable rottable = corpse.TryGetComp<CompRottable>();
                if (rottable == null)
                {
                    return;
                }

                SetNumericFieldToZero(AccessTools.Field(typeof(CompRottable), "rotProgressInt"), rottable);
                SetNumericFieldToZero(AccessTools.Field(typeof(CompRottable), "rotProgress"), rottable);
                SetNumericFieldToZero(AccessTools.Field(typeof(CompRottable), "ticksUntilRotAtCurrentTemp"), rottable);
                SetNumericFieldToZero(AccessTools.Field(typeof(CompRottable), "rotDamage"), rottable);
            }
            catch
            {
            }
        }

        private static void SetNumericFieldToZero(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
            {
                return;
            }

            try
            {
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(instance, 0);
                }
                else if (field.FieldType == typeof(float))
                {
                    field.SetValue(instance, 0f);
                }
                else if (field.FieldType == typeof(double))
                {
                    field.SetValue(instance, 0d);
                }
                else if (field.FieldType == typeof(long))
                {
                    field.SetValue(instance, 0L);
                }
            }
            catch
            {
            }
        }

        public static void ApplyDefaultAllowState(Corpse corpse)
        {
            if (!ShouldAutoAllowZombieCorpses || corpse == null || corpse.Destroyed || !IsZombieCorpse(corpse))
            {
                return;
            }

            FreezeRot(corpse);

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
                FreezeRot(corpse);
            }
        }
    }
}
