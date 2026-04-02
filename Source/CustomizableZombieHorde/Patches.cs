using System.Reflection;
using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
    public static class Patch_PawnRenderer_RenderPawnAt
    {
        private static readonly Material DirtMat = MaterialPool.MatFrom("PawnOverlays/ZombieDirtBody", ShaderDatabase.CutoutSkin, Color.white);
        private static readonly Material GrabberTongueMat = MaterialPool.MatFrom("Things/Effect/GrabberTongue", ShaderDatabase.Transparent, new Color(0.58f, 0.78f, 0.38f, 0.95f));
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
            if (!ZombieUtility.IsVariant(pawn, ZombieVariant.Grabber) || Current.Game == null)
            {
                return;
            }

            ZombieGameComponent component = Current.Game.GetComponent<ZombieGameComponent>();
            Pawn target = component?.GetGrabberTongueTarget(pawn);
            if (target == null || target.Destroyed || !target.Spawned || target.Map != pawn.Map)
            {
                return;
            }

            Vector3 start = GetTongueMouthPoint(pawn, drawLoc);
            start.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            Vector3 end = target.DrawPos + (target.DrawPos - drawLoc).normalized * -0.10f;
            end.y = start.y;
            Vector3 delta = end - start;
            float length = delta.magnitude;
            if (length < 0.1f)
            {
                return;
            }

            Vector3 center = (start + end) * 0.5f;
            float angle = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            float pulse = 0.10f + Mathf.Abs(Mathf.Sin((Find.TickManager?.TicksGame ?? 0) * 0.18f)) * 0.06f;
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(pulse, 1f, length));
            Graphics.DrawMesh(MeshPool.plane10, matrix, GrabberTongueMat, 0);
        }

        private static Vector3 GetTongueMouthPoint(Pawn pawn, Vector3 drawLoc)
        {
            Vector3 forward;
            switch (pawn.Rotation.AsInt)
            {
                case 1:
                    forward = new Vector3(0.18f, 0f, 0f);
                    break;
                case 2:
                    forward = new Vector3(0f, 0f, -0.20f);
                    break;
                case 3:
                    forward = new Vector3(-0.18f, 0f, 0f);
                    break;
                default:
                    forward = new Vector3(0f, 0f, 0.20f);
                    break;
            }

            return drawLoc + forward + new Vector3(0f, 0f, 0.04f);
        }

        public static void Postfix(PawnRenderer __instance, Vector3 drawLoc, Rot4? rotOverride = null, bool neverAimWeapon = false)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn))
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

            if (ZombieUtility.IsVariant(pawn, ZombieVariant.Tank))
            {
                Matrix4x4 matrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(2f, 2f, 2f));
                Graphics.DrawMesh(mesh, matrix, DirtMat, 0);
                Graphics.DrawMesh(mesh, matrix, variantMat, 0);
            }
            else if (ZombieUtility.IsVariant(pawn, ZombieVariant.Crawler))
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

    [HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
    public static class Patch_Pawn_EquipmentTracker_AddEquipment
    {
        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (!ZombieUtility.IsZombie(pawn))
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

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill
    {
        public static void Postfix(Pawn __instance)
        {
            if (ZombieUtility.IsZombie(__instance))
            {
                ZombieSpecialUtility.HandleZombieDeathEffects(__instance);
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

            Rect rect = new Rect(UI.screenWidth - 238f, 6f, 228f, 34f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Widgets.DrawWindowBackground(rect);
            Widgets.Label(rect.ContractedBy(4f), "Zombies: " + component.GetCurrentMapZombieCount());
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

            if (ZombieUtility.IsZombie(victim) && !ZombieUtility.IsZombie(attacker))
            {
                dinfo.SetAmount(dinfo.Amount * 1.60f);
            }
            else if (!ZombieUtility.IsZombie(victim) && ZombieUtility.IsZombie(attacker))
            {
                dinfo.SetAmount(dinfo.Amount * 0.55f);
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
            if (!ZombieUtility.IsZombie(possibleZombie))
            {
                return false;
            }

            if (ZombieUtility.IsZombie(otherPawn))
            {
                return true;
            }

            return ZombieUtility.ShouldZombiesIgnore(otherPawn);
        }
    }

}
