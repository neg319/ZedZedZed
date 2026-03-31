using HarmonyLib;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class CustomizableZombieHordeMod : Mod
    {
        public static CustomizableZombieHordeSettings Settings;

        public CustomizableZombieHordeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CustomizableZombieHordeSettings>();
            var harmony = new Harmony("vyberware.zedzedzed");
            harmony.PatchAll();
            ZombieDefUtility.ApplyDynamicLabels();
        }

        public override string SettingsCategory() => "Zed Zed Zed";

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Settings.DoWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ZombieDefUtility.ApplyDynamicLabels();
        }
    }
}
