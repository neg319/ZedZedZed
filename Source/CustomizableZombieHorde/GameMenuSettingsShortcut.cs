using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    public sealed class Dialog_ZedZedZedSettingsQuickAccess : Window
    {
        public Dialog_ZedZedZedSettingsQuickAccess()
        {
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = true;
            doCloseX = true;
            doCloseButton = false;
            draggable = true;
            resizeable = false;
            onlyOneOfTypeAllowed = true;
            preventCameraMotion = true;
        }

        public override Vector2 InitialSize => new Vector2(1060f, 820f);

        public override void DoWindowContents(Rect inRect)
        {
            if (CustomizableZombieHordeMod.Settings == null)
            {
                LoadedModManager.GetMod<CustomizableZombieHordeMod>();
            }

            if (CustomizableZombieHordeMod.Settings == null)
            {
                return;
            }

            Rect contentRect = inRect.ContractedBy(6f);
            CustomizableZombieHordeMod.Settings.DoWindowContents(contentRect);
        }

        public override void PreClose()
        {
            base.PreClose();

            try
            {
                LoadedModManager.GetMod<CustomizableZombieHordeMod>()?.WriteSettings();
            }
            catch (Exception ex)
            {
                Log.Error("[Zed Zed Zed] Failed to save settings after closing the quick access window. " + ex);
            }
        }
    }

    [HarmonyPatch]
    public static class Patch_GameMenu_ZedZedZedShortcut
    {
        private static Type cachedGameMenuType;

        public static MethodBase TargetMethod()
        {
            cachedGameMenuType = AccessTools.TypeByName("RimWorld.Dialog_GameMenu")
                ?? AccessTools.TypeByName("Verse.Dialog_GameMenu")
                ?? AccessTools.TypeByName("LudeonTK.Dialog_GameMenu")
                ?? AccessTools.TypeByName("Dialog_GameMenu");

            if (cachedGameMenuType == null)
            {
                cachedGameMenuType = AccessTools.TypeByName("Verse.Dialog_OptionLister")
                    ?? AccessTools.TypeByName("LudeonTK.Dialog_OptionLister")
                    ?? AccessTools.TypeByName("Dialog_OptionLister");
            }

            return cachedGameMenuType == null
                ? null
                : AccessTools.Method(cachedGameMenuType, "DoWindowContents", new[] { typeof(Rect) });
        }

        public static void Postfix(object __instance, Rect inRect)
        {
            if (Current.ProgramState != ProgramState.Playing || Event.current == null)
            {
                return;
            }

            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout && Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseUp)
            {
                return;
            }

            if (!IsGameMenuInstance(__instance))
            {
                return;
            }

            float leftColumnX = inRect.x + (inRect.width * 0.048f);
            float buttonWidth = inRect.width * 0.459f;
            float top = inRect.y + (inRect.height * 0.049f);
            float buttonHeight = inRect.height * 0.110f;
            float gap = inRect.height * 0.025f;

            Rect settingsRect = new Rect(leftColumnX, top + ((buttonHeight + gap) * 4f), buttonWidth, buttonHeight);
            Rect quitToMenuRect = new Rect(leftColumnX, top + ((buttonHeight + gap) * 5f), buttonWidth, buttonHeight);
            Rect quitToOsRect = new Rect(leftColumnX, top + ((buttonHeight + gap) * 6f), buttonWidth, buttonHeight);

            DrawButtonBackdrop(settingsRect);
            DrawButtonBackdrop(quitToMenuRect);
            DrawButtonBackdrop(quitToOsRect);

            if (Widgets.ButtonText(settingsRect, "Zed Zed Zed settings"))
            {
                OpenSettingsWindow();
            }

            if (Widgets.ButtonText(quitToMenuRect, "Quit to main menu"))
            {
                GenScene.GoToMainMenu();
            }

            if (Widgets.ButtonText(quitToOsRect, "Quit to OS"))
            {
                Current.Root?.Shutdown();
            }
        }

        private static bool IsGameMenuInstance(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            Type type = instance.GetType();
            string typeName = type.FullName ?? type.Name;
            if (typeName.IndexOf("GameMenu", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return cachedGameMenuType != null
                && (type == cachedGameMenuType || cachedGameMenuType.IsAssignableFrom(type))
                && typeName.IndexOf("OptionLister", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawButtonBackdrop(Rect rect)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.06f, 0.08f, 0.11f, 1f);
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = previousColor;
        }

        private static void OpenSettingsWindow()
        {
            if (Find.WindowStack == null)
            {
                return;
            }

            if (Find.WindowStack.IsOpen<Dialog_ZedZedZedSettingsQuickAccess>())
            {
                return;
            }

            Find.WindowStack.Add(new Dialog_ZedZedZedSettingsQuickAccess());
        }
    }
}
