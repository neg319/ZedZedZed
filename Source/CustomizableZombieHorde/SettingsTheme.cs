using UnityEngine;
using Verse;

namespace CustomizableZombieHorde
{
    [StaticConstructorOnStartup]
    public static class SettingsTheme
    {
        private static Texture2D windowTexture;
        private static Texture2D headerTexture;
        private static Texture2D cardTexture;
        private static Texture2D buttonTexture;
        private static Texture2D buttonActiveTexture;
        private static Texture2D valueInsetTexture;
        private static Texture2D zombieIconTexture;

        public static readonly Color Ink = new Color(0.90f, 0.90f, 0.86f);
        public static readonly Color MutedInk = new Color(0.63f, 0.69f, 0.62f);
        public static readonly Color Accent = new Color(0.62f, 0.16f, 0.16f);
        public static readonly Color AccentSoft = new Color(0.42f, 0.11f, 0.11f);
        public static readonly Color Border = new Color(0.16f, 0.20f, 0.16f);
        public static readonly Color BorderBright = new Color(0.36f, 0.41f, 0.34f);
        public static readonly Color CardTint = new Color(0.28f, 0.31f, 0.27f);
        public static readonly Color WindowTint = new Color(0.22f, 0.24f, 0.21f);
        public static readonly Color HeaderTint = new Color(0.20f, 0.21f, 0.19f);
        public static readonly Color InsetTint = new Color(0.16f, 0.17f, 0.15f);
        public static readonly Color WarningTint = new Color(0.34f, 0.25f, 0.14f);
        public static readonly Color SuccessTint = new Color(0.18f, 0.29f, 0.20f);
        public static readonly Color DisabledTint = new Color(0.31f, 0.22f, 0.22f);

        private static Texture2D WindowTexture => windowTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/WindowGrunge", false) ?? Texture2D.whiteTexture;
        private static Texture2D HeaderTexture => headerTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/HeaderStrip", false) ?? Texture2D.whiteTexture;
        private static Texture2D CardTexture => cardTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/CardGrunge", false) ?? Texture2D.whiteTexture;
        private static Texture2D ButtonTexture => buttonTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/ButtonDark", false) ?? Texture2D.whiteTexture;
        private static Texture2D ButtonActiveTexture => buttonActiveTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/ButtonActive", false) ?? Texture2D.whiteTexture;
        private static Texture2D ValueInsetTexture => valueInsetTexture ??= ContentFinder<Texture2D>.Get("UI/Settings/ValueInset", false) ?? Texture2D.whiteTexture;
        private static Texture2D ZombieIconTexture => zombieIconTexture ??= ContentFinder<Texture2D>.Get("UI/Faction/Zombies", false) ?? Texture2D.whiteTexture;

        public static void DrawBody(Rect rect)
        {
            DrawTexturedRect(rect, WindowTexture, WindowTint);
            DrawOverlay(rect, new Color(0f, 0f, 0f, 0.24f));
            DrawBorder(rect, BorderBright, 2f);
            DrawBorder(rect.ContractedBy(3f), Border, 1f);
        }

        public static void DrawHeader(Rect rect)
        {
            DrawTexturedRect(rect, HeaderTexture, HeaderTint);
            DrawOverlay(rect, new Color(0f, 0f, 0f, 0.18f));
            DrawBorder(rect, AccentSoft, 2f);
            DrawBorder(rect.ContractedBy(3f), BorderBright, 1f);

            Rect iconRect = new Rect(rect.x + 14f, rect.y + 8f, 64f, 64f);
            DrawTexturedRect(iconRect, ZombieIconTexture, new Color(1f, 1f, 1f, 0.13f));

            Rect bloodLine = new Rect(rect.x, rect.yMax - 6f, rect.width, 6f);
            DrawTexturedRect(bloodLine, ValueInsetTexture, new Color(0.56f, 0.11f, 0.11f, 0.85f));
        }

        public static void DrawSectionBand(Rect rect)
        {
            DrawTexturedRect(rect, CardTexture, new Color(0.24f, 0.27f, 0.23f));
            DrawBorder(rect, BorderBright, 1f);
            Rect accentLine = new Rect(rect.x + 10f, rect.y + rect.height - 4f, Mathf.Max(120f, rect.width * 0.45f), 3f);
            DrawOverlay(accentLine, new Color(0.56f, 0.14f, 0.14f, 0.95f));
        }

        public static void DrawCard(Rect rect)
        {
            DrawTexturedRect(rect, CardTexture, CardTint);
            DrawOverlay(rect, new Color(0f, 0f, 0f, 0.12f));
            DrawBorder(rect, BorderBright, 1f);
        }

        public static void DrawWarningCard(Rect rect)
        {
            DrawTexturedRect(rect, CardTexture, WarningTint);
            DrawOverlay(rect, new Color(0f, 0f, 0f, 0.12f));
            DrawBorder(rect, new Color(0.58f, 0.43f, 0.20f), 1f);
        }

        public static void DrawTextFieldShell(Rect rect)
        {
            DrawTexturedRect(rect, ValueInsetTexture, InsetTint);
            DrawBorder(rect, BorderBright, 1f);
        }

        public static void DrawValueShell(Rect rect)
        {
            DrawTexturedRect(rect, ValueInsetTexture, InsetTint);
            DrawBorder(rect, BorderBright, 1f);
        }

        public static void DrawStatusPill(Rect rect, bool enabled)
        {
            DrawTexturedRect(rect, ValueInsetTexture, enabled ? SuccessTint : DisabledTint);
            DrawBorder(rect, enabled ? new Color(0.46f, 0.62f, 0.41f) : new Color(0.54f, 0.30f, 0.30f), 1f);
        }

        public static bool DrawButton(Rect rect, string label, bool active = false)
        {
            DrawTexturedRect(rect, active ? ButtonActiveTexture : ButtonTexture, active ? new Color(0.44f, 0.18f, 0.18f) : new Color(0.25f, 0.27f, 0.24f));
            if (Mouse.IsOver(rect))
            {
                DrawOverlay(rect, new Color(1f, 1f, 1f, 0.05f));
            }

            DrawBorder(rect, active ? new Color(0.74f, 0.32f, 0.32f) : BorderBright, active ? 2f : 1f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = active ? Ink : new Color(0.80f, 0.83f, 0.77f);
            Widgets.Label(rect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            return Widgets.ButtonInvisible(rect);
        }

        public static void DrawHeaderWatermark(Rect rect)
        {
            Rect watermarkRect = new Rect(rect.xMax - 100f, rect.y + 8f, 88f, 64f);
            DrawTexturedRect(watermarkRect, ZombieIconTexture, new Color(1f, 1f, 1f, 0.07f));
        }

        public static void DrawCounterPanel(Rect rect)
        {
            DrawOverlay(rect, new Color(0.04f, 0.05f, 0.04f, 1f));

            Rect topBand = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, 24f);
            DrawOverlay(topBand, new Color(0.11f, 0.12f, 0.10f, 0.96f));

            Rect bodyBand = new Rect(rect.x + 1f, rect.y + 25f, rect.width - 2f, rect.height - 30f);
            DrawOverlay(bodyBand, new Color(0.08f, 0.09f, 0.08f, 0.98f));

            DrawBorder(rect, BorderBright, 1f);
            DrawBorder(rect.ContractedBy(2f), Border, 1f);

            Rect accentStrip = new Rect(rect.x + 1f, rect.yMax - 5f, rect.width - 2f, 2f);
            DrawOverlay(accentStrip, new Color(0.56f, 0.12f, 0.12f, 0.92f));
        }

        private static void DrawTexturedRect(Rect rect, Texture2D texture, Color tint)
        {
            Color old = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private static void DrawOverlay(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, thickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, thickness, rect.height), color);
            Widgets.DrawBoxSolid(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
