using HarmonyLib;
using Verse;

namespace CustomizableZombieHorde
{
    [StaticConstructorOnStartup]
    public static class ZedZedZedBootstrap
    {
        static ZedZedZedBootstrap()
        {
            try
            {
                var harmony = new Harmony("vyberware.zedzedzed");
                harmony.PatchAll();
            }
            catch (System.Exception ex)
            {
                Log.Error("[Zed Zed Zed] Harmony patching failed. " + ex);
            }

            try
            {
                ZombieDefUtility.ApplyDynamicLabels();
            }
            catch (System.Exception ex)
            {
                Log.Error("[Zed Zed Zed] Failed to apply dynamic labels. " + ex);
            }
        }
    }

    public sealed class CustomizableZombieHordeMod : Mod
    {
        public static CustomizableZombieHordeSettings Settings;

        public CustomizableZombieHordeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CustomizableZombieHordeSettings>();
        }

        public override string SettingsCategory() => "Zed Zed Zed";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            if (Settings == null)
            {
                Settings = GetSettings<CustomizableZombieHordeSettings>();
            }
            Settings.DoWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();

            try
            {
                ZombieDefUtility.ApplyDynamicLabels();
            }
            catch (System.Exception ex)
            {
                Log.Error("[Zed Zed Zed] Failed to apply dynamic labels while saving settings. " + ex);
            }
        }
    }
}
