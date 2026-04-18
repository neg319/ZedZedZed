using System.Reflection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CustomizableZombieHorde
{
    internal static class ZZZLocalize
    {
        private static readonly Dictionary<string, Dictionary<string, string>> LocalizedTextByFolder = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ChineseSimplified"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "已启用",
                ["ZZZ_ButtonApply"] = "应用",
                ["ZZZ_ButtonRunNow"] = "立即执行",
                ["ZZZ_CorpseNoSpoil"] = "这个僵尸尸体不会腐烂。",
                ["ZZZ_HUDDanger"] = "危险度：{0}%",
                ["ZZZ_HUDDangerOff"] = "危险度：关闭",
                ["ZZZ_LurkerRecruitFail"] = "招募尝试失败。无需抓捕，管理者之后还可以继续尝试。",
                ["ZZZ_PresetApocalypse"] = "末日",
                ["ZZZ_PresetCasual"] = "轻松",
                ["ZZZ_PresetRecommended"] = "推荐",
                ["ZZZ_SettingsHint"] = "在进行中的存档里也尽量保持清晰易读。",
                ["ZZZ_SettingsSubtitle"] = "尸潮节奏、变种控制、殖民地清理，还有几个危险的小测试工具。",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed 设置",
                ["ZZZ_StateChecked"] = "已勾选",
                ["ZZZ_StateDisabled"] = "已禁用",
                ["ZZZ_StateEnabled"] = "已启用",
                ["ZZZ_StateUnchecked"] = "未勾选",
                ["ZZZ_TabColony"] = "殖民地",
                ["ZZZ_TabDebug"] = "调试",
                ["ZZZ_TabEvents"] = "事件",
                ["ZZZ_TabNames"] = "名称",
                ["ZZZ_TabOverview"] = "概览",
                ["ZZZ_TabVariants"] = "变种",
            },
            ["ChineseTraditional"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "已啟用",
                ["ZZZ_ButtonApply"] = "套用",
                ["ZZZ_ButtonRunNow"] = "立即執行",
                ["ZZZ_CorpseNoSpoil"] = "這具殭屍屍體不會腐爛。",
                ["ZZZ_HUDDanger"] = "危險度：{0}%",
                ["ZZZ_HUDDangerOff"] = "危險度：關閉",
                ["ZZZ_LurkerRecruitFail"] = "招募嘗試失敗。管理者之後還可以在不抓捕的情況下繼續嘗試。",
                ["ZZZ_PresetApocalypse"] = "末日",
                ["ZZZ_PresetCasual"] = "輕鬆",
                ["ZZZ_PresetRecommended"] = "推薦",
                ["ZZZ_SettingsHint"] = "在進行中的存檔裡也盡量保持清楚易讀。",
                ["ZZZ_SettingsSubtitle"] = "屍潮節奏、變種控制、殖民地清理，還有幾個危險的小型測試工具。",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed 設定",
                ["ZZZ_StateChecked"] = "已勾選",
                ["ZZZ_StateDisabled"] = "已停用",
                ["ZZZ_StateEnabled"] = "已啟用",
                ["ZZZ_StateUnchecked"] = "未勾選",
                ["ZZZ_TabColony"] = "殖民地",
                ["ZZZ_TabDebug"] = "除錯",
                ["ZZZ_TabEvents"] = "事件",
                ["ZZZ_TabNames"] = "名稱",
                ["ZZZ_TabOverview"] = "概覽",
                ["ZZZ_TabVariants"] = "變種",
            },
            ["English"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "ACTIVE",
                ["ZZZ_ButtonApply"] = "Apply",
                ["ZZZ_ButtonRunNow"] = "Run now",
                ["ZZZ_CorpseNoSpoil"] = "This zombie corpse does not rot.",
                ["ZZZ_HUDDanger"] = "Danger: {0}%",
                ["ZZZ_HUDDangerOff"] = "Danger: off",
                ["ZZZ_LurkerRecruitFail"] = "Recruitment failed. A warden can try again.",
                ["ZZZ_PresetApocalypse"] = "Apocalypse",
                ["ZZZ_PresetCasual"] = "Casual",
                ["ZZZ_PresetRecommended"] = "Recommended",
                ["ZZZ_SettingsHint"] = "Made for live saves.",
                ["ZZZ_SettingsSubtitle"] = "Outbreak settings, strain control, and colony cleanup.",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed Settings",
                ["ZZZ_StateChecked"] = "Checked",
                ["ZZZ_StateDisabled"] = "Disabled",
                ["ZZZ_StateEnabled"] = "Enabled",
                ["ZZZ_StateUnchecked"] = "Unchecked",
                ["ZZZ_TabColony"] = "Colony",
                ["ZZZ_TabDebug"] = "Debug",
                ["ZZZ_TabEvents"] = "Events",
                ["ZZZ_TabNames"] = "Names",
                ["ZZZ_TabOverview"] = "Overview",
                ["ZZZ_TabVariants"] = "Variants",
            },
            ["French"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "ACTIF",
                ["ZZZ_ButtonApply"] = "Appliquer",
                ["ZZZ_ButtonRunNow"] = "Exécuter",
                ["ZZZ_CorpseNoSpoil"] = "Ce cadavre de zombie ne pourrit pas.",
                ["ZZZ_HUDDanger"] = "Danger : {0}%",
                ["ZZZ_HUDDangerOff"] = "Danger : désactivé",
                ["ZZZ_LurkerRecruitFail"] = "La tentative de recrutement a échoué. Un gardien peut réessayer sans capturer le lurker.",
                ["ZZZ_PresetApocalypse"] = "Apocalypse",
                ["ZZZ_PresetCasual"] = "Détendu",
                ["ZZZ_PresetRecommended"] = "Recommandé",
                ["ZZZ_SettingsHint"] = "Écrit pour rester lisible même au milieu d’une sauvegarde en cours.",
                ["ZZZ_SettingsSubtitle"] = "Rythme de l’épidémie, contrôle des souches, nettoyage de la colonie et quelques petits outils de test dangereux.",
                ["ZZZ_SettingsTitle"] = "Paramètres de Zed Zed Zed",
                ["ZZZ_StateChecked"] = "Coché",
                ["ZZZ_StateDisabled"] = "Désactivé",
                ["ZZZ_StateEnabled"] = "Activé",
                ["ZZZ_StateUnchecked"] = "Décoché",
                ["ZZZ_TabColony"] = "Colonie",
                ["ZZZ_TabDebug"] = "Débogage",
                ["ZZZ_TabEvents"] = "Événements",
                ["ZZZ_TabNames"] = "Noms",
                ["ZZZ_TabOverview"] = "Vue d’ensemble",
                ["ZZZ_TabVariants"] = "Variantes",
            },
            ["German"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "AKTIV",
                ["ZZZ_ButtonApply"] = "Anwenden",
                ["ZZZ_ButtonRunNow"] = "Jetzt ausführen",
                ["ZZZ_CorpseNoSpoil"] = "Dieser Zombiekadaver verrottet nicht.",
                ["ZZZ_HUDDanger"] = "Gefahr: {0}%",
                ["ZZZ_HUDDangerOff"] = "Gefahr: aus",
                ["ZZZ_LurkerRecruitFail"] = "Der Rekrutierungsversuch ist fehlgeschlagen. Ein Aufseher kann es weiter versuchen, ohne den Lurker gefangen zu nehmen.",
                ["ZZZ_PresetApocalypse"] = "Apokalypse",
                ["ZZZ_PresetCasual"] = "Locker",
                ["ZZZ_PresetRecommended"] = "Empfohlen",
                ["ZZZ_SettingsHint"] = "Geschrieben, damit es selbst mitten in einem laufenden Spielstand gut lesbar bleibt.",
                ["ZZZ_SettingsSubtitle"] = "Ausbruchstempo, Variantenkontrolle, Aufräumen der Kolonie und ein paar gefährliche kleine Testwerkzeuge.",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed Einstellungen",
                ["ZZZ_StateChecked"] = "Markiert",
                ["ZZZ_StateDisabled"] = "Deaktiviert",
                ["ZZZ_StateEnabled"] = "Aktiviert",
                ["ZZZ_StateUnchecked"] = "Nicht markiert",
                ["ZZZ_TabColony"] = "Kolonie",
                ["ZZZ_TabDebug"] = "Debug",
                ["ZZZ_TabEvents"] = "Ereignisse",
                ["ZZZ_TabNames"] = "Namen",
                ["ZZZ_TabOverview"] = "Übersicht",
                ["ZZZ_TabVariants"] = "Varianten",
            },
            ["Japanese"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "稼働中",
                ["ZZZ_ButtonApply"] = "適用",
                ["ZZZ_ButtonRunNow"] = "今すぐ実行",
                ["ZZZ_CorpseNoSpoil"] = "このゾンビの死体は腐敗しません。",
                ["ZZZ_HUDDanger"] = "危険度: {0}%",
                ["ZZZ_HUDDangerOff"] = "危険度: オフ",
                ["ZZZ_LurkerRecruitFail"] = "勧誘に失敗しました。捕縛しなくても、看守はこのルーカーに再挑戦できます。",
                ["ZZZ_PresetApocalypse"] = "アポカリプス",
                ["ZZZ_PresetCasual"] = "カジュアル",
                ["ZZZ_PresetRecommended"] = "推奨",
                ["ZZZ_SettingsHint"] = "進行中のセーブでも読みやすいように調整しています。",
                ["ZZZ_SettingsSubtitle"] = "発生ペース、変種の制御、コロニーの後始末、それから少し危険なテスト用ツール。",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed 設定",
                ["ZZZ_StateChecked"] = "選択済み",
                ["ZZZ_StateDisabled"] = "無効",
                ["ZZZ_StateEnabled"] = "有効",
                ["ZZZ_StateUnchecked"] = "未選択",
                ["ZZZ_TabColony"] = "コロニー",
                ["ZZZ_TabDebug"] = "デバッグ",
                ["ZZZ_TabEvents"] = "イベント",
                ["ZZZ_TabNames"] = "名前",
                ["ZZZ_TabOverview"] = "概要",
                ["ZZZ_TabVariants"] = "変種",
            },
            ["Korean"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "활성",
                ["ZZZ_ButtonApply"] = "적용",
                ["ZZZ_ButtonRunNow"] = "지금 실행",
                ["ZZZ_CorpseNoSpoil"] = "이 좀비 시체는 썩지 않습니다.",
                ["ZZZ_HUDDanger"] = "위험도: {0}%",
                ["ZZZ_HUDDangerOff"] = "위험도: 꺼짐",
                ["ZZZ_LurkerRecruitFail"] = "영입 시도에 실패했습니다. 간수는 루커를 포획하지 않고도 다시 시도할 수 있습니다.",
                ["ZZZ_PresetApocalypse"] = "아포칼립스",
                ["ZZZ_PresetCasual"] = "가볍게",
                ["ZZZ_PresetRecommended"] = "추천",
                ["ZZZ_SettingsHint"] = "진행 중인 저장에서도 읽기 쉽도록 작성했습니다.",
                ["ZZZ_SettingsSubtitle"] = "발병 흐름, 변종 제어, 식민지 정리, 그리고 조금 위험한 테스트 도구 몇 가지.",
                ["ZZZ_SettingsTitle"] = "Zed Zed Zed 설정",
                ["ZZZ_StateChecked"] = "체크됨",
                ["ZZZ_StateDisabled"] = "비활성화",
                ["ZZZ_StateEnabled"] = "활성화",
                ["ZZZ_StateUnchecked"] = "체크 안 됨",
                ["ZZZ_TabColony"] = "식민지",
                ["ZZZ_TabDebug"] = "디버그",
                ["ZZZ_TabEvents"] = "이벤트",
                ["ZZZ_TabNames"] = "이름",
                ["ZZZ_TabOverview"] = "개요",
                ["ZZZ_TabVariants"] = "변종",
            },
            ["Polish"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "AKTYWNE",
                ["ZZZ_ButtonApply"] = "Zastosuj",
                ["ZZZ_ButtonRunNow"] = "Uruchom teraz",
                ["ZZZ_CorpseNoSpoil"] = "Te zwłoki zombie nie gniją.",
                ["ZZZ_HUDDanger"] = "Zagrożenie: {0}%",
                ["ZZZ_HUDDangerOff"] = "Zagrożenie: wyłączone",
                ["ZZZ_LurkerRecruitFail"] = "Próba rekrutacji nie powiodła się. Strażnik może próbować dalej bez pojmania lurkera.",
                ["ZZZ_PresetApocalypse"] = "Apokalipsa",
                ["ZZZ_PresetCasual"] = "Luźno",
                ["ZZZ_PresetRecommended"] = "Zalecane",
                ["ZZZ_SettingsHint"] = "Napisane tak, żeby dało się to czytać nawet w trakcie aktywnego sejwa.",
                ["ZZZ_SettingsSubtitle"] = "Tempo plagi, kontrola odmian, sprzątanie kolonii i kilka niebezpiecznych narzędzi testowych.",
                ["ZZZ_SettingsTitle"] = "Ustawienia Zed Zed Zed",
                ["ZZZ_StateChecked"] = "Zaznaczone",
                ["ZZZ_StateDisabled"] = "Wyłączone",
                ["ZZZ_StateEnabled"] = "Włączone",
                ["ZZZ_StateUnchecked"] = "Niezaznaczone",
                ["ZZZ_TabColony"] = "Kolonia",
                ["ZZZ_TabDebug"] = "Debug",
                ["ZZZ_TabEvents"] = "Zdarzenia",
                ["ZZZ_TabNames"] = "Nazwy",
                ["ZZZ_TabOverview"] = "Przegląd",
                ["ZZZ_TabVariants"] = "Warianty",
            },
            ["PortugueseBrazilian"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "ATIVO",
                ["ZZZ_ButtonApply"] = "Aplicar",
                ["ZZZ_ButtonRunNow"] = "Executar agora",
                ["ZZZ_CorpseNoSpoil"] = "Este cadáver de zumbi não apodrece.",
                ["ZZZ_HUDDanger"] = "Perigo: {0}%",
                ["ZZZ_HUDDangerOff"] = "Perigo: desligado",
                ["ZZZ_LurkerRecruitFail"] = "A tentativa de recrutamento falhou. Um carcereiro pode tentar de novo sem capturar o lurker.",
                ["ZZZ_PresetApocalypse"] = "Apocalipse",
                ["ZZZ_PresetCasual"] = "Leve",
                ["ZZZ_PresetRecommended"] = "Recomendado",
                ["ZZZ_SettingsHint"] = "Escrito para continuar legível no meio de um save em andamento.",
                ["ZZZ_SettingsSubtitle"] = "Ritmo do surto, controle das variantes, limpeza da colônia e algumas ferramentinhas de teste perigosas.",
                ["ZZZ_SettingsTitle"] = "Configurações do Zed Zed Zed",
                ["ZZZ_StateChecked"] = "Marcado",
                ["ZZZ_StateDisabled"] = "Desativado",
                ["ZZZ_StateEnabled"] = "Ativado",
                ["ZZZ_StateUnchecked"] = "Desmarcado",
                ["ZZZ_TabColony"] = "Colônia",
                ["ZZZ_TabDebug"] = "Depuração",
                ["ZZZ_TabEvents"] = "Eventos",
                ["ZZZ_TabNames"] = "Nomes",
                ["ZZZ_TabOverview"] = "Visão geral",
                ["ZZZ_TabVariants"] = "Variantes",
            },
            ["Russian"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "АКТИВНО",
                ["ZZZ_ButtonApply"] = "Применить",
                ["ZZZ_ButtonRunNow"] = "Запустить",
                ["ZZZ_CorpseNoSpoil"] = "Этот труп зомби не разлагается.",
                ["ZZZ_HUDDanger"] = "Опасность: {0}%",
                ["ZZZ_HUDDangerOff"] = "Опасность: выкл.",
                ["ZZZ_LurkerRecruitFail"] = "Попытка вербовки не удалась. Надзиратель может попробовать снова, не захватывая луркера.",
                ["ZZZ_PresetApocalypse"] = "Апокалипсис",
                ["ZZZ_PresetCasual"] = "Легко",
                ["ZZZ_PresetRecommended"] = "Рекомендуется",
                ["ZZZ_SettingsHint"] = "Сделано так, чтобы всё оставалось читаемым прямо во время живого сохранения.",
                ["ZZZ_SettingsSubtitle"] = "Ритм вспышки, контроль разновидностей, уборка колонии и несколько опасных тестовых инструментов.",
                ["ZZZ_SettingsTitle"] = "Настройки Zed Zed Zed",
                ["ZZZ_StateChecked"] = "Отмечено",
                ["ZZZ_StateDisabled"] = "Выключено",
                ["ZZZ_StateEnabled"] = "Включено",
                ["ZZZ_StateUnchecked"] = "Не отмечено",
                ["ZZZ_TabColony"] = "Колония",
                ["ZZZ_TabDebug"] = "Отладка",
                ["ZZZ_TabEvents"] = "События",
                ["ZZZ_TabNames"] = "Имена",
                ["ZZZ_TabOverview"] = "Обзор",
                ["ZZZ_TabVariants"] = "Варианты",
            },
            ["Spanish"] = new Dictionary<string, string>
            {
                ["ZZZ_BadgeActive"] = "ACTIVO",
                ["ZZZ_ButtonApply"] = "Aplicar",
                ["ZZZ_ButtonRunNow"] = "Ejecutar ahora",
                ["ZZZ_CorpseNoSpoil"] = "Este cadáver zombi no se pudre.",
                ["ZZZ_HUDDanger"] = "Peligro: {0}%",
                ["ZZZ_HUDDangerOff"] = "Peligro: desactivado",
                ["ZZZ_LurkerRecruitFail"] = "El intento de reclutamiento falló. Un encargado puede seguir intentándolo sin capturar al lurker.",
                ["ZZZ_PresetApocalypse"] = "Apocalipsis",
                ["ZZZ_PresetCasual"] = "Suave",
                ["ZZZ_PresetRecommended"] = "Recomendado",
                ["ZZZ_SettingsHint"] = "Escrito para seguir siendo legible en medio de una partida activa.",
                ["ZZZ_SettingsSubtitle"] = "Ritmo del brote, control de variantes, limpieza de la colonia y unas cuantas herramientas de prueba peligrosas.",
                ["ZZZ_SettingsTitle"] = "Configuración de Zed Zed Zed",
                ["ZZZ_StateChecked"] = "Marcado",
                ["ZZZ_StateDisabled"] = "Desactivado",
                ["ZZZ_StateEnabled"] = "Activado",
                ["ZZZ_StateUnchecked"] = "Sin marcar",
                ["ZZZ_TabColony"] = "Colonia",
                ["ZZZ_TabDebug"] = "Depuración",
                ["ZZZ_TabEvents"] = "Eventos",
                ["ZZZ_TabNames"] = "Nombres",
                ["ZZZ_TabOverview"] = "Resumen",
                ["ZZZ_TabVariants"] = "Variantes",
            },
        };

        public static string T(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string activeFolder = LanguageDatabase.activeLanguage?.folderName;
            if (!string.IsNullOrEmpty(activeFolder) &&
                LocalizedTextByFolder.TryGetValue(activeFolder, out Dictionary<string, string> activeLanguageStrings) &&
                activeLanguageStrings.TryGetValue(key, out string activeValue) &&
                !string.IsNullOrWhiteSpace(activeValue))
            {
                return activeValue;
            }

            if (LocalizedTextByFolder.TryGetValue("English", out Dictionary<string, string> englishStrings) &&
                englishStrings.TryGetValue(key, out string englishValue) &&
                !string.IsNullOrWhiteSpace(englishValue))
            {
                return englishValue;
            }

            return key;
        }

        public static string TShort(string key)
        {
            return T("ZZZ_" + key);
        }

        public static string Format(string key, params object[] args)
        {
            string template = T(key);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }


    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_Pawn_JobTracker_StartJob_BlockZombieMapExit
    {
        public static bool Prefix(Pawn ___pawn, ref Job newJob)
        {
            Pawn pawn = ___pawn;
            if (pawn == null || newJob == null || !ZombieUtility.IsZombie(pawn) || pawn.Dead || pawn.Destroyed)
            {
                return true;
            }

            if (ZombieUtility.IsPlayerAlignedZombie(pawn) || ZombieLurkerUtility.IsColonyLurker(pawn))
            {
                return true;
            }

            if (!ZombieUtility.IsBadZombieJob(pawn, newJob, pawn.MapHeld))
            {
                return true;
            }

            Job replacementJob = ZombieUtility.CreateReplacementZombieMoveJob(pawn);
            if (replacementJob != null)
            {
                newJob = replacementJob;
                return true;
            }

            Job idleJob = JobMaker.MakeJob(JobDefOf.Wait, pawn.PositionHeld);
            idleJob.expiryInterval = 90;
            idleJob.checkOverrideOnExpire = true;
            idleJob.locomotionUrgency = LocomotionUrgency.Walk;
            newJob = idleJob;
            return true;
        }
    }

    [HarmonyPatch]
    public static class Patch_Verb_TryStartCastOn_ZombieNoiseAttraction
    {
        public static MethodBase TargetMethod()
        {
            return typeof(Verb).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "TryStartCastOn"
                    && method.ReturnType == typeof(bool)
                    && method.GetParameters().Length >= 1
                    && method.GetParameters()[0].ParameterType == typeof(LocalTargetInfo));
        }

        public static void Postfix(Verb __instance, LocalTargetInfo __0, bool __result)
        {
            if (!__result)
            {
                return;
            }

            Pawn attacker = __instance?.CasterPawn;
            if (attacker == null)
            {
                return;
            }

            ZombieSpecialUtility.NotifyWeaponFired(attacker, __instance, __0);
        }
    }

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


        public static void Prefix(PawnRenderer __instance)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (ZombieUtility.IsZombie(pawn))
            {
                ZombiePawnFactory.EnsureZombieVisualIntegrity(pawn, markGraphicsDirty: false);
            }
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
            if (__result?.kindDef?.defName == null || !__result.kindDef.defName.StartsWith("CZH_Zombie_"))
            {
                return;
            }

            if (ZombiePawnFactory.SuppressAutoFinalizePatch)
            {
                return;
            }

            ZombiePawnFactory.FinalizeZombie(__result, initialSpawn: true);
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

    [HarmonyPatch(typeof(PawnRelationWorker_Parent), "CreateRelation")]
    public static class Patch_PawnRelationWorker_Parent_CreateRelation
    {
        private static bool HasUnsafeParticipant(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (ZombieUtility.IsZombie(pawn))
            {
                return true;
            }

            if (pawn.Name != null && !(pawn.Name is NameTriple))
            {
                return true;
            }

            return false;
        }

        public static bool Prefix(Pawn generated, Pawn other, ref PawnGenerationRequest request)
        {
            if (HasUnsafeParticipant(generated) || HasUnsafeParticipant(other))
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
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
    public static class Patch_Pawn_Tick_HostileZombieNullRefShield
    {
        public static Exception Finalizer(Exception __exception, Pawn __instance)
        {
            if (__exception == null)
            {
                return null;
            }

            if (!(__exception is NullReferenceException) || __instance == null || !ZombieUtility.IsZombie(__instance))
            {
                return __exception;
            }

            try
            {
                ZombieUtility.NormalizeCoreZombieState(__instance);
                ZombieUtility.PrepareSpawnedZombie(__instance);
                if (!ZombieLurkerUtility.IsLurker(__instance) && !ZombieUtility.IsPlayerAlignedZombie(__instance))
                {
                    ZombieUtility.EnsureZombieAggression(__instance);
                }
            }
            catch
            {
            }

            return null;
        }
    }



    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill
    {
        public static bool Prefix(Pawn __instance)
        {
            if (ZombieUtility.IsVariant(__instance, ZombieVariant.Boomer))
            {
                ZombieSpecialUtility.TriggerBoomerBurstOnly(__instance);
            }

            return true;
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
                if (ZombieUtility.IsVariant(__instance, ZombieVariant.Runt))
                {
                    ZombieRuntUtility.NormalizeRuntCorpseForButchering(__instance);
                }

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


    [HarmonyPatch(typeof(Pawn), nameof(Pawn.MainDesc))]
    public static class Patch_Pawn_MainDesc_RuntAgeLabel
    {
        public static void Postfix(Pawn __instance, ref string __result)
        {
            __result = ZombieRuntUtility.ApplyRuntMonthAgeLabelToDescription(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.SpawnSetup))]
    public static class Patch_Corpse_SpawnSetup_RuntButchering
    {
        public static void Postfix(Corpse __instance)
        {
            Pawn innerPawn = __instance?.InnerPawn;
            if (innerPawn == null || !ZombieUtility.IsZombie(innerPawn))
            {
                return;
            }

            if (ZombieUtility.IsVariant(innerPawn, ZombieVariant.Runt))
            {
                ZombieRuntUtility.NormalizeRuntCorpseForButchering(innerPawn);
            }

            ZombiePawnFactory.EnsureZombieVisualIntegrity(innerPawn);
        }
    }


    [HarmonyPatch(typeof(Corpse), nameof(Corpse.SpawnSetup))]
    public static class Patch_Corpse_SpawnSetup_ScheduleZombieReanimation
    {
        public static void Postfix(Corpse __instance)
        {
            Pawn innerPawn = __instance?.InnerPawn;
            if (innerPawn == null || !innerPawn.Dead || !ZombieUtility.IsZombie(innerPawn) || ZombieUtility.IsVariant(innerPawn, ZombieVariant.Boomer))
            {
                return;
            }

            Current.Game?.GetComponent<ZombieGameComponent>()?.ScheduleZombieCorpseWake(__instance);
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
            int currentCount = ZombieSpawnHelper.GetCurrentZombieCount(Find.CurrentMap);
            if (component != null)
            {
                component.NotifyZombieCountChanged();
            }

            int cap = ZombieSpawnHelper.GetDynamicZombieCap(Find.CurrentMap);
            float capPercent = cap > 0 ? (currentCount / (float)cap) * 100f : 0f;
            string familyLabel = GetCounterDisplayLabel();
            string countText = familyLabel + ": " + currentCount;
            string dangerText = cap > 0 ? ZZZLocalize.Format("ZZZ_HUDDanger", capPercent.ToString("0")) : ZZZLocalize.T("ZZZ_HUDDangerOff");

            Rect rect = new Rect(UI.screenWidth - 362f, 6f, 168f, 62f);
            Rect countRect = new Rect(rect.x + 8f, rect.y + 5f, rect.width - 16f, 24f);
            Rect detailRect = new Rect(rect.x + 8f, rect.y + 32f, rect.width - 16f, 22f);

            SettingsTheme.DrawCounterPanel(rect);

            Text.Anchor = TextAnchor.MiddleCenter;

            Text.Font = GameFont.Small;
            GUI.color = SettingsTheme.Ink;
            Widgets.Label(countRect, countText);

            Text.Font = GameFont.Tiny;
            GUI.color = SettingsTheme.MutedInk;
            Widgets.Label(detailRect, dangerText);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private static string GetCounterDisplayLabel()
        {
            string prefix = ZombieDefUtility.CleanPrefix(CustomizableZombieHordeMod.Settings?.zombiePrefix ?? "Zombie");
            string familyName = string.IsNullOrWhiteSpace(prefix) ? "Zombie" : prefix.CapitalizeFirst();

            if (familyName.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                return familyName;
            }

            if (familyName.Equals("Zombie", StringComparison.OrdinalIgnoreCase))
            {
                return "Zombies";
            }

            return familyName + "s";
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill_ZombieBrainRule
    {
        private static readonly HashSet<int> ActiveKills = new HashSet<int>();

        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance == null || !ZombieUtility.IsZombie(__instance) || __instance.Destroyed)
            {
                return;
            }

            int id = __instance.thingIDNumber;
            if (!ActiveKills.Add(id))
            {
                return;
            }

            try
            {
                DamageInfo sourceDamage = dinfo ?? default(DamageInfo);
                Pawn attacker = ZombieTraitUtility.ResolveDamageInstigatorPawn(sourceDamage.Instigator);
                ZombieUtility.EnsureZombieBrainDestroyed(__instance, attacker, sourceDamage);
            }
            finally
            {
                ActiveKills.Remove(id);
            }
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
            bool isLiveCombatContext = attacker != null || dinfo.Instigator != null;

            if (victimIsZombie && !attackerIsZombie && isLiveCombatContext)
            {
                float zombieDamageMultiplier = ZombieUtility.GetZombieIncomingDamageMultiplier(victim);
                float amount = dinfo.Amount * zombieDamageMultiplier;

                bool headHit = ZombieInfectionUtility.IsHeadOrChildPart(dinfo.HitPart, victim);
                if (headHit)
                {
                    // Any real head or face hit should be immediately decisive.
                    amount = Mathf.Max(amount * 8.0f, 999f);
                }

                if (ZombieTraitUtility.HasSteadyHands(attacker) && ZombieTraitUtility.IsRangedAttack(attacker, dinfo))
                {
                    amount *= headHit ? 1.25f : 1.10f;
                }

                dinfo.SetAmount(amount);
            }
            else if (!victimIsZombie && attackerIsZombie)
            {
                float amount = dinfo.Amount * ZombieUtility.GetZombieOutgoingDamageMultiplier(attacker, victim);
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
        public static void Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, ref float __state)
        {
            Pawn victim = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            __state = GetBleedingSeverityOnPart(victim, dinfo.HitPart);
        }

        public static void Postfix(Pawn_HealthTracker __instance, DamageInfo dinfo, float totalDamageDealt, float __state)
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

            bool isLiveCombatContext = attacker != null || dinfo.Instigator != null;
            if (ZombieUtility.IsZombie(victim) && (attacker == null || !ZombieUtility.IsZombie(attacker)) && isLiveCombatContext)
            {
                bool headHit = ZombieInfectionUtility.IsHeadOrChildPart(dinfo.HitPart, victim);
                bool loudAttack = attacker != null && ZombieTraitUtility.IsRangedAttack(attacker, dinfo);

                if (headHit)
                {
                    ZombieUtility.DestroyZombieBrain(victim, attacker, dinfo);
                }
                else if (!victim.Dead)
                {
                    float brainDestroyChance = ZombieUtility.GetBrainDestroyChance(victim, attacker, dinfo, totalDamageDealt);
                    if (brainDestroyChance > 0f && Rand.Chance(brainDestroyChance))
                    {
                        ZombieUtility.DestroyZombieBrain(victim, attacker, dinfo);
                    }
                }

                if (attacker != null)
                {
                    ZombieSpecialUtility.NotifyCombatAttraction(attacker, victim, loudAttack);
                }
            }

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
                && !victim.Dead)
            {
                bool openedWound = DidZombieOpenBleedingWound(victim, dinfo, __state);
                float infectionChance = ZombieInfectionUtility.GetZombieBiteInfectionChance(attacker);

                if (ZombieInfectionUtility.IsZombieBiteDamage(dinfo))
                {
                    infectionChance += 0.02f;
                }

                if (openedWound)
                {
                    infectionChance += 0.02f;
                }

                if (totalDamageDealt >= 6f)
                {
                    infectionChance += 0.01f;
                }

                infectionChance = Mathf.Clamp(infectionChance, 0f, ZombieUtility.IsVariant(attacker, ZombieVariant.Sick) ? 0.18f : 0.14f);

                BodyPartRecord infectionPart = ZombieInfectionUtility.ResolveAmputationFriendlyInfectionPart(victim, dinfo.HitPart);
                ZombieTraitUtility.TryApplyZombieSickness(victim, infectionChance, infectionPart);
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

        private static bool DidZombieOpenBleedingWound(Pawn victim, DamageInfo dinfo, float bleedingSeverityBefore)
        {
            if (victim?.health?.hediffSet == null)
            {
                return false;
            }

            float bleedingSeverityAfter = GetBleedingSeverityOnPart(victim, dinfo.HitPart);
            return bleedingSeverityAfter > bleedingSeverityBefore + 0.001f;
        }

        private static float GetBleedingSeverityOnPart(Pawn pawn, BodyPartRecord part)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
            {
                return 0f;
            }

            IEnumerable<Hediff_Injury> injuries = pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>();
            if (part != null)
            {
                injuries = injuries.Where(injury => injury?.Part == part);
            }

            return injuries
                .Where(injury => injury != null && injury.BleedRate > 0f)
                .Sum(injury => injury.Severity);
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

            if (ZombieUtility.IsPlayerAlignedZombie(possibleZombie) && ZombieUtility.IsColonyAlly(otherPawn))
            {
                return true;
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
        private static void AddOptionIfMissing(List<FloatMenuOption> opts, string label, Action action, bool insertAtTop = false)
        {
            if (opts == null || label.NullOrEmpty())
            {
                return;
            }

            if (opts.Any(existing => existing != null && string.Equals(existing.Label, label, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            FloatMenuOption option = new FloatMenuOption(label, action);
            if (insertAtTop)
            {
                opts.Insert(0, option);
                return;
            }

            opts.Add(option);
        }

        private static void AddManualDoubleTapOrders(Pawn pawn, List<FloatMenuOption> opts, List<Pawn> pawnsAtCell, List<Corpse> corpsesAtCell)
        {
            foreach (Pawn targetPawn in pawnsAtCell.Where(ZombieDoubleTapUtility.CanPlayerOrderDoubleTapPawn))
            {
                string label = "Double Tap " + targetPawn.LabelShortCap;
                if (!pawn.CanReach(targetPawn, Verse.AI.PathEndMode.Touch, Danger.Deadly))
                {
                    AddOptionIfMissing(opts, label + ": no path", null, true);
                    continue;
                }

                if (!pawn.CanReserve(targetPawn))
                {
                    AddOptionIfMissing(opts, label + ": reserved", null, true);
                    continue;
                }

                AddOptionIfMissing(opts, label, delegate
                {
                    ZombieDoubleTapUtility.TryStartManualDoubleTap(pawn, targetPawn);
                }, true);
            }

            foreach (Corpse corpse in corpsesAtCell.Where(ZombieDoubleTapUtility.CanPlayerOrderDoubleTapCorpse))
            {
                string label = "Double Tap " + corpse.LabelShortCap;
                if (!pawn.CanReach(corpse, Verse.AI.PathEndMode.Touch, Danger.Deadly))
                {
                    AddOptionIfMissing(opts, label + ": no path", null, true);
                    continue;
                }

                if (!pawn.CanReserve(corpse))
                {
                    AddOptionIfMissing(opts, label + ": reserved", null, true);
                    continue;
                }

                AddOptionIfMissing(opts, label, delegate
                {
                    ZombieDoubleTapUtility.TryStartManualDoubleTap(pawn, corpse);
                }, true);
            }
        }

        public static void AddCustomHumanlikeOrders(IntVec3 cell, Pawn pawn, List<FloatMenuOption> opts)
        {
            if (!cell.InBounds(pawn.Map))
            {
                return;
            }

            List<Thing> thingsAtCell = cell.GetThingList(pawn.Map);
            List<Pawn> pawnsAtCell = thingsAtCell.OfType<Pawn>().ToList();
            List<Corpse> corpsesAtCell = thingsAtCell.OfType<Corpse>().ToList();

            AddManualDoubleTapOrders(pawn, opts, pawnsAtCell, corpsesAtCell);

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
                    opts.Add(new FloatMenuOption("Recruit a lurker with rotten flesh or human meat", null));
                    opts.Add(new FloatMenuOption("Tame a lurker with rotten flesh or human meat", null));
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
                    opts.Add(new FloatMenuOption("Administer bile treatment", null));
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



    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Patch_Pawn_GetGizmos_DoubleTapCommand
    {
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendDoubleTapGizmo(__result, __instance);
        }

        private static IEnumerable<Gizmo> AppendDoubleTapGizmo(IEnumerable<Gizmo> values, Pawn pawn)
        {
            if (values != null)
            {
                foreach (Gizmo gizmo in values)
                {
                    yield return gizmo;
                }
            }

            if (pawn == null || !pawn.IsColonistPlayerControlled || pawn.Dead || pawn.Destroyed || !pawn.Spawned)
            {
                yield break;
            }

            Command_Action command = new Command_Action
            {
                defaultLabel = "Double Tap",
                defaultDesc = "Finish a downed target with a head shot.",
                action = delegate
                {
                    ManualDoubleTapGizmoUtility.BeginDoubleTapTargeting(pawn);
                }
            };

            yield return command;
        }
    }

    internal static class ManualDoubleTapGizmoUtility
    {
        internal static void BeginDoubleTapTargeting(Pawn actor)
        {
            if (actor == null || actor.MapHeld == null)
            {
                return;
            }

            TargetingParameters parameters = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetItems = true,
                canTargetBuildings = false,
                canTargetLocations = false,
                validator = target => ZombieDoubleTapUtility.CanDoubleTapThing(target.Thing)
            };

            Action<LocalTargetInfo> action = delegate(LocalTargetInfo target)
            {
                ZombieDoubleTapUtility.TryStartManualDoubleTap(actor, target.Thing);
            };

            object targeter = Find.Targeter;
            if (targeter == null)
            {
                return;
            }

            MethodInfo method = targeter.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(candidate => candidate.Name == "BeginTargeting")
                .OrderBy(candidate => candidate.GetParameters().Length)
                .FirstOrDefault(candidate =>
                {
                    ParameterInfo[] parametersInfo = candidate.GetParameters();
                    return parametersInfo.Length >= 2
                        && parametersInfo[0].ParameterType == typeof(TargetingParameters)
                        && parametersInfo[1].ParameterType.IsAssignableFrom(typeof(Action<LocalTargetInfo>));
                });

            if (method == null)
            {
                return;
            }

            ParameterInfo[] callParameters = method.GetParameters();
            object[] args = new object[callParameters.Length];
            args[0] = parameters;
            args[1] = action;

            for (int i = 2; i < callParameters.Length; i++)
            {
                Type parameterType = callParameters[i].ParameterType;

                if (parameterType == typeof(Pawn))
                {
                    args[i] = actor;
                }
                else if (parameterType == typeof(bool))
                {
                    args[i] = false;
                }
                else if (parameterType.IsValueType)
                {
                    args[i] = Activator.CreateInstance(parameterType);
                }
                else
                {
                    args[i] = null;
                }
            }

            try
            {
                method.Invoke(targeter, args);
            }
            catch
            {
            }
        }
    }


    [HarmonyPatch]
    public static class Patch_Hediff_TendableNow_ZombieInfection
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Hediff), "TendableNow")
                ?? AccessTools.Method(typeof(Hediff), "TendableNow");
        }

        public static void Postfix(Hediff __instance, ref bool __result)
        {
            if (__instance?.def != ZombieDefOf.CZH_ZombieSickness)
            {
                return;
            }

            if (ZombieUtility.IsZombie(__instance.pawn))
            {
                __result = false;
                return;
            }

            if (!ZombieInfectionUtility.CanCureZombieInfection(__instance.pawn))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Hediff), "LabelBase", MethodType.Getter)]
    public static class Patch_Hediff_LabelBase_ZombieInfection
    {
        public static void Postfix(Hediff __instance, ref string __result)
        {
            if (__instance?.def == ZombieDefOf.CZH_ZombieSickness && ZombieUtility.IsZombie(__instance.pawn))
            {
                __result = "Zombie infection";
            }
        }
    }

    [HarmonyPatch(typeof(Hediff), "Label", MethodType.Getter)]
    public static class Patch_Hediff_Label_ZombieInfection
    {
        public static void Postfix(Hediff __instance, ref string __result)
        {
            if (__instance?.def == ZombieDefOf.CZH_ZombieSickness && ZombieUtility.IsZombie(__instance.pawn))
            {
                __result = "Zombie infection";
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

    [HarmonyPatch(typeof(Corpse), nameof(Corpse.SpawnSetup))]
    public static class Patch_Corpse_SpawnSetup_AutoAllowZombieCorpses
    {
        public static void Postfix(Corpse __instance, Map map)
        {
            if (map == null || __instance == null)
            {
                return;
            }

            ZombieCorpseUtility.ApplyDefaultAllowState(__instance);
        }
    }


    [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.Active), MethodType.Getter)]
    public static class Patch_CompRottable_Active_PreventZombieCorpseRot
    {
        public static void Postfix(CompRottable __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (ZombieCorpseUtility.ShouldPreventRot(__instance?.parent))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompInspectStringExtra))]
    public static class Patch_CompRottable_Inspect_PreventZombieCorpseRot
    {
        public static bool Prefix(CompRottable __instance, ref string __result)
        {
            if (!ZombieCorpseUtility.ShouldPreventRot(__instance?.parent))
            {
                return true;
            }

            __result = ZZZLocalize.T("ZZZ_CorpseNoSpoil");
            return false;
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




    internal static class ZombiePrisonerEnslavementUtility
    {
        private static readonly PropertyInfo GuestWillProperty = AccessTools.Property(typeof(Pawn_GuestTracker), "Will");
        private static readonly MethodInfo GuestWillSetter = GuestWillProperty?.GetSetMethod(true);
        private static readonly FieldInfo GuestWillField = AccessTools.Field(typeof(Pawn_GuestTracker), "will") ?? AccessTools.Field(typeof(Pawn_GuestTracker), "willInt");
        private static readonly PropertyInfo GuestInteractionModeProperty = AccessTools.Property(typeof(Pawn_GuestTracker), "interactionMode")
            ?? AccessTools.Property(typeof(Pawn_GuestTracker), "InteractionMode");
        private static readonly FieldInfo GuestInteractionModeField = AccessTools.Field(typeof(Pawn_GuestTracker), "interactionMode")
            ?? AccessTools.Field(typeof(Pawn_GuestTracker), "interactionModeInt")
            ?? AccessTools.Field(typeof(Pawn_GuestTracker), "slaveInteractionMode")
            ?? AccessTools.Field(typeof(Pawn_GuestTracker), "slaveInteractionModeInt");
        private static readonly MethodInfo EnslavePrisonerMethod = typeof(GenGuest).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == "EnslavePrisoner" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Pawn));

        internal static bool IsZombiePrisonerAwaitingEnslavement(Pawn pawn)
        {
            if (pawn == null || !ZombieUtility.IsZombie(pawn) || ZombieLurkerUtility.IsLurker(pawn))
            {
                return false;
            }

            try
            {
                if (!pawn.IsPrisonerOfColony)
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            object interactionMode = null;
            try
            {
                interactionMode = pawn.guest == null ? null : GuestInteractionModeProperty?.GetValue(pawn.guest, null);
            }
            catch
            {
            }

            if (interactionMode == null)
            {
                try
                {
                    interactionMode = GuestInteractionModeField?.GetValue(pawn.guest);
                }
                catch
                {
                }
            }

            if (interactionMode == null && pawn.guest != null)
            {
                try
                {
                    foreach (FieldInfo field in typeof(Pawn_GuestTracker).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field == null || field.FieldType == null)
                        {
                            continue;
                        }

                        string fieldName = field.Name ?? string.Empty;
                        string typeName = field.FieldType.Name ?? string.Empty;
                        if (fieldName.IndexOf("interaction", StringComparison.OrdinalIgnoreCase) < 0 && typeName.IndexOf("InteractionMode", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        object value = field.GetValue(pawn.guest);
                        if (value != null)
                        {
                            interactionMode = value;
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            if (interactionMode == null && pawn.guest != null)
            {
                try
                {
                    foreach (PropertyInfo property in typeof(Pawn_GuestTracker).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                        {
                            continue;
                        }

                        string propertyName = property.Name ?? string.Empty;
                        string typeName = property.PropertyType?.Name ?? string.Empty;
                        if (propertyName.IndexOf("interaction", StringComparison.OrdinalIgnoreCase) < 0 && typeName.IndexOf("InteractionMode", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        object value = property.GetValue(pawn.guest, null);
                        if (value != null)
                        {
                            interactionMode = value;
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            if (interactionMode == null)
            {
                return false;
            }

            Def def = interactionMode as Def;
            string token = def?.defName ?? def?.label ?? interactionMode.ToString();
            return !token.NullOrEmpty() && token.IndexOf("enslav", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool TryGetGuestWill(Pawn pawn, out float will)
        {
            will = 0f;
            if (pawn?.guest == null)
            {
                return false;
            }

            try
            {
                if (GuestWillProperty != null)
                {
                    object value = GuestWillProperty.GetValue(pawn.guest, null);
                    if (value is float floatValue)
                    {
                        will = floatValue;
                        return true;
                    }

                    if (value is double doubleValue)
                    {
                        will = (float)doubleValue;
                        return true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (GuestWillField != null)
                {
                    object value = GuestWillField.GetValue(pawn.guest);
                    if (value is float floatValue)
                    {
                        will = floatValue;
                        return true;
                    }

                    if (value is double doubleValue)
                    {
                        will = (float)doubleValue;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        internal static bool TrySetGuestWill(Pawn pawn, float will)
        {
            if (pawn?.guest == null)
            {
                return false;
            }

            try
            {
                if (GuestWillSetter != null)
                {
                    GuestWillSetter.Invoke(pawn.guest, new object[] { will });
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                if (GuestWillField != null)
                {
                    if (GuestWillField.FieldType == typeof(float))
                    {
                        GuestWillField.SetValue(pawn.guest, will);
                    }
                    else if (GuestWillField.FieldType == typeof(double))
                    {
                        GuestWillField.SetValue(pawn.guest, (double)will);
                    }
                    else
                    {
                        GuestWillField.SetValue(pawn.guest, Convert.ChangeType(will, GuestWillField.FieldType));
                    }

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        internal static void TryFinishZombieEnslavement(Pawn pawn)
        {
            if (!IsZombiePrisonerAwaitingEnslavement(pawn))
            {
                return;
            }

            if (!TryGetGuestWill(pawn, out float will) || will > 1.0f)
            {
                return;
            }

            TrySetGuestWill(pawn, 0f);

            try
            {
                EnslavePrisonerMethod?.Invoke(null, new object[] { pawn });
            }
            catch
            {
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_GuestTracker), "get_Recruitable")]
    public static class Patch_PawnGuestTracker_Recruitable
    {
        public static void Postfix(Pawn_GuestTracker __instance, ref bool __result)
        {
            if (!__result || __instance == null)
            {
                return;
            }

            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn == null || !ZombieUtility.IsZombie(pawn) || ZombieLurkerUtility.IsLurker(pawn))
            {
                return;
            }

            if (ZombiePrisonerEnslavementUtility.IsZombiePrisonerAwaitingEnslavement(pawn))
            {
                return;
            }

            __result = false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt), "Interacted")]
    public static class Patch_InteractionWorker_EnslaveAttempt_ZombieFinisher
    {
        public static void Postfix(Pawn recipient)
        {
            ZombiePrisonerEnslavementUtility.TryFinishZombieEnslavement(recipient);
        }
    }



    [HarmonyPatch(typeof(HediffSet), "get_PainTotal")]
    public static class Patch_HediffSet_PainTotal_ZombieIndifference
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            Pawn pawn = null;
            try
            {
                pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            }
            catch
            {
            }

            if (pawn != null && ZombieUtility.IsZombie(pawn))
            {
                __result = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(ThoughtWorker), "CurrentState", new[] { typeof(Pawn) })]
    public static class Patch_ThoughtWorker_CurrentState_ZombieIndifference
    {
        public static bool Prefix(ThoughtWorker __instance, Pawn p, ref ThoughtState __result)
        {
            if (!ZombieUtility.IsZombie(p))
            {
                return true;
            }

            __result = ThoughtState.Inactive;
            return false;
        }
    }



    [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedsTrackerTick")]
    public static class Patch_Pawn_NeedsTracker_NeedsTrackerTick_HostileZombieSkip
    {
        public static bool Prefix(Pawn_NeedsTracker __instance)
        {
            Pawn pawn = null;
            try
            {
                pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            }
            catch
            {
            }

            if (pawn != null && ZombieUtility.IsZombie(pawn) && !ZombieLurkerUtility.IsLurker(pawn) && !ZombieUtility.IsPlayerAlignedZombie(pawn))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedsTrackerTickInterval")]
    public static class Patch_Pawn_NeedsTracker_NeedsTrackerTickInterval_HostileZombieSkip
    {
        public static bool Prefix(Pawn_NeedsTracker __instance)
        {
            Pawn pawn = null;
            try
            {
                pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            }
            catch
            {
            }

            if (pawn != null && ZombieUtility.IsZombie(pawn) && !ZombieLurkerUtility.IsLurker(pawn) && !ZombieUtility.IsPlayerAlignedZombie(pawn))
            {
                return false;
            }

            return true;
        }
    }

}
