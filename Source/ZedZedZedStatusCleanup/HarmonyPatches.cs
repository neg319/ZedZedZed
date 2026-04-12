using HarmonyLib;
using Verse;

namespace ZedZedZedStatusCleanup;

[HarmonyPatch(typeof(Hediff), nameof(Hediff.Visible), MethodType.Getter)]
internal static class Patch_Hediff_Visible
{
    private static void Postfix(Hediff __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        if (ZombieStatusConsolidationUtility.ShouldHide(__instance))
        {
            __result = false;
        }
    }
}

[HarmonyPatch(typeof(Hediff), nameof(Hediff.LabelBase), MethodType.Getter)]
internal static class Patch_Hediff_LabelBase
{
    private static void Postfix(Hediff __instance, ref string __result)
    {
        if (ZombieStatusConsolidationUtility.ShouldUseCombinedLabel(__instance))
        {
            __result = ZombieStatusConsolidationUtility.GetCombinedLabelBase(__instance);
        }
    }
}

[HarmonyPatch(typeof(Hediff), nameof(Hediff.LabelInBrackets), MethodType.Getter)]
internal static class Patch_Hediff_LabelInBrackets
{
    private static void Postfix(Hediff __instance, ref string __result)
    {
        if (ZombieStatusConsolidationUtility.ShouldUseCombinedLabel(__instance))
        {
            __result = ZombieStatusConsolidationUtility.GetCombinedLabelInBrackets(__instance, __result);
        }
    }
}
