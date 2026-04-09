using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    internal static class ZombieButcherSearchState
    {
        [ThreadStatic]
        internal static Bill CurrentBill;
    }


    [HarmonyPatch]
    public static class Patch_BloodMoonScreenTint
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(UIRoot_Play), "UIRootOnGUI")
                ?? AccessTools.Method(typeof(UIRoot), "UIRootOnGUI");
        }

        public static void Postfix()
        {
            if (Current.ProgramState != ProgramState.Playing || Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            if (component == null || !component.IsBloodMoonVisualActive(Find.CurrentMap))
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = component.GetBloodMoonTintColor();
            GUI.DrawTexture(new Rect(0f, 0f, UI.screenWidth, UI.screenHeight), BaseContent.WhiteTex);
            GUI.color = previousColor;
        }
    }


    internal static class ZombieFloorUtility
    {
        private static readonly HashSet<string> ZombieLeatherFloorDefNames = new HashSet<string>
        {
            "CZH_RottenLeatherFloor",
            "CZH_StitchedRottenLeatherFloor",
            "CZH_PatchworkRottenLeatherFloor",
            "CZH_MottledHideFloor"
        };

        internal static bool IsZombieLeatherFloor(TerrainDef terrain)
        {
            return terrain != null && ZombieLeatherFloorDefNames.Contains(terrain.defName);
        }

        internal static bool IsZombieLeatherFloor(BuildableDef buildable)
        {
            return IsZombieLeatherFloor(buildable as TerrainDef);
        }
    }

    [HarmonyPatch(typeof(GenConstruct), nameof(GenConstruct.CanBuildOnTerrain))]
    public static class Patch_GenConstruct_CanBuildOnTerrain_ZombieFloors
    {
        public static void Postfix(BuildableDef entDef, IntVec3 c, Map map, ref bool __result)
        {
            if (__result || entDef == null || map == null)
            {
                return;
            }

            TerrainDef currentTerrain = c.GetTerrain(map);
            if (ZombieFloorUtility.IsZombieLeatherFloor(entDef))
            {
                __result = true;
                return;
            }

            if (ZombieFloorUtility.IsZombieLeatherFloor(currentTerrain))
            {
                __result = true;
            }
        }
    }

    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    public static class Patch_PawnRenderer_RenderPawnAt
    {
        private static readonly Material DirtMat = MaterialPool.MatFrom("PawnOverlays/ZombieDirtBody", ShaderDatabase.CutoutSkin, Color.white);
        private static readonly PropertyInfo HumanlikeBodySetProperty = AccessTools.Property(typeof(MeshPool), "humanlikeBodySet") ?? AccessTools.Property(typeof(MeshPool), "humanlikeSet");

        private static Mesh GetHumanlikeBodyMesh(Rot4 rot)
        {
            try
            {
                object bodySet = HumanlikeBodySetProperty?.GetValue(null, null);
                if (bodySet == null)
                {
                    return null;
                }

                MethodInfo meshAt = AccessTools.Method(bodySet.GetType(), "MeshAt", new[] { typeof(Rot4) });
                if (meshAt == null)
                {
                    return null;
                }

                return meshAt.Invoke(bodySet, new object[] { rot }) as Mesh;
            }
            catch
            {
                return null;
            }
        }



        private static void DrawGrabberTongue(Pawn pawn, Vector3 drawLoc)
        {
            return;
        }


        public static void Postfix(PawnRenderer __instance, Vector3 drawLoc, Rot4? rotOverride = null, bool neverAimWeapon = false)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn))
            {
                return;
            }

            if (pawn?.RaceProps?.Humanlike != true)
            {
                return;
            }

            Rot4 rot = rotOverride ?? pawn.Rotation;
            Mesh mesh = GetHumanlikeBodyMesh(rot);
            if (mesh == null)
            {
                return;
            }
            Vector3 loc = drawLoc;
            loc.y += 0.003f;

            Color overlayColor = ZombieVisualUtility.GetOverlayColor(pawn);
            Material variantMat = MaterialPool.MatFrom(ZombieVisualUtility.GetVariantOverlayPath(pawn, ZombieUtility.GetVariant(pawn)), ShaderDatabase.CutoutSkin, overlayColor);

            DrawGrabberTongue(pawn, drawLoc);

            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Brute))
            {
                Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(2f, 2f, 2f));
                Graphics.DrawMesh(mesh, matrix, DirtMat, 0);
                Graphics.DrawMesh(mesh, matrix, variantMat, 0);
            }
            else if (ZombieUtility.IsVariant(pawn, ZombieVariant.Runt))
            {
                int ticks = Find.TickManager?.TicksGame ?? 0;
                float phase = (ticks * 0.18f) + (pawn.thingIDNumber % 17) * 0.37f;
                float bob = Mathf.Sin(phase) * 0.018f;
                float surge = Mathf.Cos(phase * 0.5f) * 0.012f;
                float roll = Mathf.Sin(phase) * 4.5f;
                float pitch = 82f + Mathf.Cos(phase) * 2.5f;

                Vector3 forward;
                switch (rot.AsInt)
                {
                    case 1:
                        forward = new Vector3(0.08f, 0f, 0f);
                        break;
                    case 2:
                        forward = new Vector3(0f, 0f, -0.08f);
                        break;
                    case 3:
                        forward = new Vector3(-0.08f, 0f, 0f);
                        break;
                    default:
                        forward = new Vector3(0f, 0f, 0.08f);
                        break;
                }

                Vector3 crawlLoc = loc + forward + new Vector3(0f, -0.055f + bob, surge);
                Quaternion crawlRotation = Quaternion.Euler(pitch, rot.AsAngle, roll);
                Matrix4x4 matrix = Matrix4x4.TRS(crawlLoc, crawlRotation, new Vector3(0.56f, 0.20f, 0.88f));
                Graphics.DrawMesh(mesh, matrix, DirtMat, 0);
                Graphics.DrawMesh(mesh, matrix, variantMat, 0);
            }
            else
            {
                Graphics.DrawMesh(mesh, loc, Quaternion.identity, DirtMat, 0);
                Graphics.DrawMesh(mesh, loc, Quaternion.identity, variantMat, 0);
            }
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Postfix(PawnGenerationRequest request, Pawn __result)
        {
            if (__result?.kindDef?.defName != null && __result.kindDef.defName.StartsWith("CZH_Zombie_"))
            {
                ZombiePawnFactory.FinalizeZombie(__result, initialSpawn: true);
            }
        }
    }

    
    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawnRelations")]
    public static class Patch_PawnGenerator_GeneratePawnRelations
    {
        public static bool Prefix(Pawn pawn)
        {
            if (ZombiePawnFactory.SuppressZombieRelationGeneration)
            {
                return false;
            }

            if (pawn?.kindDef?.defName != null && pawn.kindDef.defName.StartsWith("CZH_Zombie_"))
            {
                return false;
            }

            return true;
        }
    }
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    public static class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn) || ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                return true;
            }

            newEq?.Destroy(DestroyMode.Vanish);
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "DropBloodFilth")]
    public static class Patch_Pawn_HealthTracker_DropBloodFilth
    {
        public static bool Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn))
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "DropBloodSmear")]
    public static class Patch_Pawn_HealthTracker_DropBloodSmear
    {
        public static bool Prefix(Pawn_HealthTracker __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn))
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_ApparelTracker), "PsychologicallyNude", MethodType.Getter)]
    public static class Patch_Pawn_ApparelTracker_PsychologicallyNude
    {
        public static void Postfix(Pawn_ApparelTracker __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (ZombieLurkerUtility.IsLurker(pawn))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_FoodUtility_WillEat_Thing
    {
        private static MethodBase cachedTarget;

        public static bool Prepare()
        {
            cachedTarget = ResolveTargetMethod();
            return cachedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return cachedTarget ?? ResolveTargetMethod();
        }

        private static MethodBase ResolveTargetMethod()
        {
            foreach (MethodInfo method in typeof(FoodUtility).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "WillEat")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(Pawn) && parameters[1].ParameterType == typeof(Thing))
                {
                    return method;
                }
            }

            return null;
        }

        public static void Postfix(Pawn __0, Thing __1, ref bool __result)
        {
            Pawn p = __0;
            ThingDef def = __1?.def;
            if (ZombieLurkerUtility.ShouldBlockRottenFleshFor(p, def))
            {
                __result = false;
                return;
            }

            if (!__result && ZombieLurkerUtility.IsLurker(p) && ZombieLurkerUtility.IsPreferredLurkerFood(def))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_FoodUtility_WillEat_ThingDef
    {
        private static MethodBase cachedTarget;

        public static bool Prepare()
        {
            cachedTarget = ResolveTargetMethod();
            return cachedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return cachedTarget ?? ResolveTargetMethod();
        }

        private static MethodBase ResolveTargetMethod()
        {
            foreach (MethodInfo method in typeof(FoodUtility).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "WillEat")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(Pawn) && parameters[1].ParameterType == typeof(ThingDef))
                {
                    return method;
                }
            }

            return null;
        }

        public static void Postfix(Pawn __0, ThingDef __1, ref bool __result)
        {
            Pawn p = __0;
            ThingDef foodDef = __1;
            if (ZombieLurkerUtility.ShouldBlockRottenFleshFor(p, foodDef))
            {
                __result = false;
                return;
            }

            if (!__result && ZombieLurkerUtility.IsLurker(p) && ZombieLurkerUtility.IsPreferredLurkerFood(foodDef))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_FoodUtility_FoodOptimality
    {
        private static MethodBase cachedTarget;

        public static bool Prepare()
        {
            cachedTarget = ResolveTargetMethod();
            return cachedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return cachedTarget ?? ResolveTargetMethod();
        }

        private static MethodBase ResolveTargetMethod()
        {
            foreach (MethodInfo method in typeof(FoodUtility).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "FoodOptimality")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 3 && parameters[0].ParameterType == typeof(Pawn) && parameters[1].ParameterType == typeof(Thing) && parameters[2].ParameterType == typeof(ThingDef))
                {
                    return method;
                }
            }

            return null;
        }

        public static void Postfix(Pawn __0, Thing __1, ThingDef __2, ref float __result)
        {
            Pawn eater = __0;
            Thing foodSource = __1;
            ThingDef foodDef = __2;
            ThingDef def = foodSource?.def ?? foodDef;
            if (ZombieLurkerUtility.ShouldBlockRottenFleshFor(eater, def))
            {
                __result = -1000f;
                return;
            }

            if (ZombieLurkerUtility.IsLurker(eater))
            {
                __result += ZombieLurkerUtility.GetLurkerFoodPreferenceOffset(def);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill
    {
        public static void Prefix(Pawn __instance)
        {
            if (ZombieUtility.IsVariant(__instance, ZombieVariant.Boomer))
            {
                ZombieSpecialUtility.TriggerBoomerBurstOnly(__instance);
            }
        }

        public static void Postfix(Pawn __instance)
        {
            if (__instance == null || !__instance.Dead)
            {
                return;
            }

            ZombieGameComponent component = Current.Game?.GetComponent<ZombieGameComponent>();
            if (ZombieInfectionUtility.HasZombieInfection(__instance))
            {
                component?.RegisterDeadPawnForPostMortemInfection(__instance);
            }

            if (ZombieUtility.IsZombie(__instance))
            {
                ZombieSpecialUtility.HandleZombieDeathEffects(__instance);
                component?.RegisterDeadPawnForRecurringReanimation(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ButcherProducts))]
    public static class Patch_Pawn_ButcherProducts
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Thing> __result)
        {
            if (!ZombieUtility.IsZombie(__instance))
            {
                return;
            }

            __result = ZombieSpecialUtility.BuildZombieButcherProducts(__instance);
        }
    }

    [HarmonyPatch(typeof(Corpse), "ButcherProducts")]
    public static class Patch_Corpse_ButcherProducts
    {
        public static void Postfix(Corpse __instance, ref IEnumerable<Thing> __result)
        {
            if (!ZombieUtility.IsZombie(__instance?.InnerPawn))
            {
                return;
            }

            __result = ZombieSpecialUtility.BuildZombieButcherProducts(__instance.InnerPawn);
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
    public static class Patch_WorkGiver_DoBill_TryFindBestBillIngredients
    {
        public static void Prefix(Bill bill)
        {
            ZombieButcherSearchState.CurrentBill = bill;
        }

        public static void Finalizer()
        {
            ZombieButcherSearchState.CurrentBill = null;
        }
    }

    [HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.Allows), new[] { typeof(Thing) })]
    public static class Patch_ThingFilter_Allows_ZombieButchering
    {
        public static void Postfix(Thing t, ref bool __result)
        {
            if (__result)
            {
                return;
            }

            Bill currentBill = ZombieButcherSearchState.CurrentBill;
            if (currentBill?.recipe?.defName != "ButcherCorpseFlesh")
            {
                return;
            }

            if (t is Corpse corpse && ZombieUtility.IsZombie(corpse.InnerPawn))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(GlobalControls), nameof(GlobalControls.GlobalControlsOnGUI))]
    public static class Patch_GlobalControls_GlobalControlsOnGUI
    {
        public static void Postfix()
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game == null)
            {
                return;
            }

            if (Find.CurrentMap == null || !Find.CurrentMap.IsPlayerHome || !(CustomizableZombieHordeMod.Settings?.showZombieCounter ?? true))
            {
                return;
            }

            ZombieGameComponent component = Current.Game.GetComponent<ZombieGameComponent>();
            if (component == null)
            {
                return;
            }

            int currentCount = component.GetCurrentMapZombieCount();
            int cap = ZombieSpawnHelper.GetDynamicZombieCap(Find.CurrentMap);
            Rect rect = new Rect(UI.screenWidth - 252f, 6f, 242f, 34f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.DrawWindowBackground(rect);
            Widgets.Label(rect.ContractedBy(4f), "Zombies: " + currentCount + " / " + cap);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    public static class Patch_Pawn_HealthTracker_PreApplyDamage
    {
        public static void Prefix(Pawn_HealthTracker __instance, ref DamageInfo dinfo)
        {
            Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (victim == null || victim.health?.hediffSet == null)
            {
                return;
            }

            Pawn attacker = ZombieTraitUtility.ResolveDamageInstigatorPawn(dinfo.Instigator);
            if (ZombieTraitUtility.HasHeadHunter(attacker))
            {
                BodyPartRecord head = ZombieUtility.GetHeadPart(victim);
                if (head != null)
                {
                    try
                    {
                        dinfo.SetHitPart(head);
                    }
                    catch
                    {
                    }
                }
            }

            bool attackerIsZombie = ZombieUtility.IsZombie(attacker);
            bool victimIsZombie = ZombieUtility.IsZombie(victim);

            if (victimIsZombie && !attackerIsZombie)
            {
                float zombieDamageMultiplier = ZombieUtility.GetZombieIncomingDamageMultiplier(victim);

                float amount = dinfo.Amount * zombieDamageMultiplier;
                if (ZombieTraitUtility.HasSteadyHands(attacker) && ZombieTraitUtility.IsRangedAttack(attacker, dinfo))
                {
                    amount *= 1.30f;
                }

                dinfo.SetAmount(amount);
            }
            else if (!victimIsZombie && attackerIsZombie)
            {
                float amount = dinfo.Amount * 0.55f;
                if (ZombieTraitUtility.HasHardToKill(victim))
                {
                    amount *= 0.75f;
                }

                dinfo.SetAmount(amount);
            }
        }
    }


    [HarmonyPatch(typeof(Pawn_HealthTracker), "PostApplyDamage")]
    public static class Patch_Pawn_HealthTracker_PostApplyDamage
    {
        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (totalDamageDealt <= 0.01f)
            {
                return;
            }

            Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (victim == null)
            {
                return;
            }

            Pawn attacker = ZombieTraitUtility.ResolveDamageInstigatorPawn(dinfo.Instigator);
            ZombieSpecialUtility.NotifyBoneBiterDisturbed(victim);

            if (victim.Dead && ZombieInfectionUtility.HasZombieInfection(victim) && ZombieInfectionUtility.IsHeadOrChildPart(dinfo.HitPart, victim))
            {
                Current.Game?.GetComponent<ZombieGameComponent>()?.MarkInfectionHeadFatal(victim);
            }

            if (ZombieUtility.IsVariant(attacker, ZombieVariant.Boomer) && victim.IsColonist && !victim.Dead)
            {
                ZombieSpecialUtility.TriggerBoomerBurst(attacker, consumePawn: true);
                return;
            }

            bool isColonistShot = attacker != null
                && attacker.IsColonist
                && ZombieUtility.IsVariant(victim, ZombieVariant.Boomer)
                && (dinfo.Instigator is Projectile || attacker.equipment?.Primary?.def?.IsRangedWeapon == true);

            if (isColonistShot)
            {
                ZombieSpecialUtility.TriggerBoomerBurst(victim, consumePawn: true);
            }

            if (!ZombieUtility.IsZombie(victim)
                && ZombieUtility.IsZombie(attacker)
                && ZombieTraitUtility.HasQuickEscape(victim)
                && victim.Spawned
                && !victim.Downed
                && !victim.Dead)
            {
                ZombieTraitUtility.TryTriggerQuickEscape(victim, attacker);
            }
        }
    }

    [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Thing) })]
    public static class Patch_GenHostility_HostileTo_ThingThing
    {
        public static void Postfix(Thing a, Thing b, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (ShouldSuppressZombieHostility(a as Pawn, b as Pawn) || ShouldSuppressZombieHostility(b as Pawn, a as Pawn))
            {
                __result = false;
            }
        }

        private static bool ShouldSuppressZombieHostility(Pawn possibleZombie, Pawn otherPawn)
        {
            if (ZombieLurkerUtility.ShouldSuppressLurkerHostility(possibleZombie, otherPawn))
            {
                return true;
            }

            if (!ZombieUtility.IsZombie(possibleZombie))
            {
                return false;
            }

            if (ZombieRulesUtility.IsIgnoredByZombies(otherPawn))
            {
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    public static partial class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        private static MethodBase cachedTarget;

        public static bool Prepare()
        {
            cachedTarget = ResolveTargetMethod();
            return cachedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return cachedTarget ?? ResolveTargetMethod();
        }

        private static MethodBase ResolveTargetMethod()
        {
            MethodBase direct = AccessTools.Method(typeof(FloatMenuMakerMap), "ChoicesAtFor", new[] { typeof(Vector3), typeof(Pawn) });
            if (direct != null)
            {
                return direct;
            }

            foreach (MethodInfo method in typeof(FloatMenuMakerMap).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "ChoicesAtFor")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(Vector3) && parameters[1].ParameterType == typeof(Pawn) && typeof(List<FloatMenuOption>).IsAssignableFrom(method.ReturnType))
                {
                    return method;
                }
            }

            return null;
        }

        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> __result)
        {
            List<FloatMenuOption> opts = __result;
            if (pawn == null || opts == null || pawn.Map == null || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            IntVec3 cell = IntVec3.FromVector3(clickPos);
            AddCustomHumanlikeOrders(cell, pawn, opts);
        }
    }

    [HarmonyPatch]
    public static class Patch_FloatMenuMakerMap_AddHumanlikeOrders_Fallback
    {
        private static MethodBase cachedTarget;

        public static bool Prepare()
        {
            cachedTarget = ResolveTargetMethod();
            return cachedTarget != null;
        }

        public static MethodBase TargetMethod()
        {
            return cachedTarget ?? ResolveTargetMethod();
        }

        private static MethodBase ResolveTargetMethod()
        {
            foreach (MethodInfo method in typeof(FloatMenuMakerMap).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != "AddHumanlikeOrders")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 3 && parameters[0].ParameterType == typeof(Vector3) && parameters[1].ParameterType == typeof(Pawn) && typeof(List<FloatMenuOption>).IsAssignableFrom(parameters[2].ParameterType))
                {
                    return method;
                }
            }

            return null;
        }

        public static void Postfix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (pawn == null || opts == null || pawn.Map == null || !pawn.IsColonistPlayerControlled)
            {
                return;
            }

            IntVec3 cell = IntVec3.FromVector3(clickPos);
            Patch_FloatMenuMakerMap_ChoicesAtFor.AddCustomHumanlikeOrders(cell, pawn, opts);
        }
    }

    public static partial class Patch_FloatMenuMakerMap_ChoicesAtFor
    {
        public static void AddCustomHumanlikeOrders(IntVec3 cell, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!cell.InBounds(pawn.Map))
            {
                return;
            }

            List<Pawn> pawnsAtCell = cell.GetThingList(pawn.Map).OfType<Pawn>().ToList();
            if (pawnsAtCell.Count == 0)
            {
                return;
            }

            foreach (Pawn lurker in pawnsAtCell.Where(ZombieLurkerUtility.IsPassiveLurker))
            {
                if (!pawn.CanReach(lurker, Verse.AI.PathEndMode.Touch, Danger.Some))
                {
                    if (pawn.Drafted)
                    {
                        opts.Add(new FloatMenuOption("Cannot capture lurker: no path to target", null));
                    }
                    opts.Add(new FloatMenuOption("Cannot recruit lurker: no path to target", null));
                    opts.Add(new FloatMenuOption("Cannot tame lurker: no path to target", null));
                    continue;
                }

                if (!pawn.CanReserve(lurker))
                {
                    if (pawn.Drafted)
                    {
                        opts.Add(new FloatMenuOption("Cannot capture lurker: reserved", null));
                    }
                    opts.Add(new FloatMenuOption("Cannot recruit lurker: reserved", null));
                    opts.Add(new FloatMenuOption("Cannot tame lurker: reserved", null));
                    continue;
                }

                if (pawn.Drafted)
                {
                    Action startCapture = delegate
                    {
                        Job arrestJob = JobMaker.MakeJob(JobDefOf.Arrest, lurker);
                        pawn.jobs.TryTakeOrderedJob(arrestJob, JobTag.Misc);
                    };
                    opts.Add(new FloatMenuOption("Capture lurker", startCapture));
                }

                Thing food = ZombieLurkerUtility.FindAvailableTameFood(pawn, pawn.Map);
                if (food == null)
                {
                    opts.Add(new FloatMenuOption("Recruit lurker like a prisoner using stockpiled rotten flesh or human meat", null));
                    opts.Add(new FloatMenuOption("Tame lurker using stockpiled rotten flesh or human meat", null));
                    continue;
                }

                string foodLabel = food.LabelCap;
                Action startRecruit = delegate
                {
                    Job job = JobMaker.MakeJob(ZombieDefOf.CZH_RecruitLurker, lurker);
                    if (pawn.carryTracker?.CarriedThing != food)
                    {
                        job.targetB = food;
                    }
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                };

                Action startTame = delegate
                {
                    Job job = JobMaker.MakeJob(ZombieDefOf.CZH_TameLurker, lurker);
                    if (pawn.carryTracker?.CarriedThing != food)
                    {
                        job.targetB = food;
                    }
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                };

                opts.Add(new FloatMenuOption("Recruit lurker using " + foodLabel + " (warden style)", startRecruit));
                opts.Add(new FloatMenuOption("Tame lurker using " + foodLabel, startTame));
            }

            foreach (Pawn patient in pawnsAtCell.Where(ZombieBileUtility.NeedsBileTreatment))
            {
                if (!pawn.CanReach(patient, Verse.AI.PathEndMode.Touch, Danger.Some))
                {
                    opts.Add(new FloatMenuOption("Cannot administer bile treatment: no path", null));
                    continue;
                }

                if (!pawn.CanReserve(patient))
                {
                    opts.Add(new FloatMenuOption("Cannot administer bile treatment: reserved", null));
                    continue;
                }

                Thing kit = ZombieBileUtility.FindCarriedBileTreatmentKit(pawn);
                if (kit == null)
                {
                    opts.Add(new FloatMenuOption("Administer bile treatment (requires a bile med kit)", null));
                    continue;
                }

                string label = patient == pawn ? "Use bile med kit" : "Administer bile med kit to " + patient.LabelShortCap;
                opts.Add(new FloatMenuOption(label, delegate
                {
                    Job job = JobMaker.MakeJob(ZombieDefOf.CZH_AdministerBileTreatment, patient);
                    job.count = 1;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }));
            }
        }
    }



    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetInspectString))]
    public static class Patch_Pawn_GetInspectString
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            string extra = ZombieFeedbackUtility.GetPawnInspectString(__instance);
            if (extra.NullOrEmpty())
            {
                return;
            }

            __result = __result.NullOrEmpty() ? extra : __result + "\n" + extra;
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.GetInspectString))]
    public static class Patch_Corpse_GetInspectString
    {
        public static void Postfix(Corpse __instance, ref string __result)
        {
            string extra = ZombieFeedbackUtility.GetCorpseInspectString(__instance);
            if (extra.NullOrEmpty())
            {
                return;
            }

            __result = __result.NullOrEmpty() ? extra : __result + "\n" + extra;
        }
    }


}
