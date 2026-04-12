using HarmonyLib;
using Verse;

namespace ZedZedZedStatusCleanup;

[StaticConstructorOnStartup]
public static class ModMain
{
    static ModMain()
    {
        var harmony = new Harmony("vyberware.zedzedzed.statuscleanup");
        harmony.PatchAll();
        Log.Message("[Zed Zed Zed Status Cleanup] Loaded.");
    }
}
