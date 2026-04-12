using System;
using System.Collections.Generic;
using Verse;

namespace ZedZedZedStatusCleanup;

internal enum ReanimationPhase
{
    None,
    Rot,
    Reanimating,
    Reanimated
}

internal static class ZombieStatusConsolidationUtility
{
    private const string ZombieInfection = "zombie infection";
    private const string ZombieRot = "zombie rot";
    private const string Reanimating = "reanimating";
    private const string FullyReanimated = "fully reanimated";
    private const string CombinedLabel = "Reanimation";
    private const string ReanimatingLabel = "Reanimating";
    private const string ReanimatedLabel = "Reanimated";

    public static bool IsZombieInfection(Hediff hediff)
    {
        return MatchesBaseLabel(hediff, ZombieInfection);
    }

    public static bool IsTrackedReanimationHediff(Hediff hediff)
    {
        return MatchesBaseLabel(hediff, ZombieRot)
            || MatchesBaseLabel(hediff, Reanimating)
            || MatchesBaseLabel(hediff, FullyReanimated);
    }

    public static bool ShouldHide(Hediff hediff)
    {
        if (hediff == null || hediff.pawn == null || !IsTrackedReanimationHediff(hediff))
        {
            return false;
        }

        string visibleAnchor = GetVisibleAnchorLabel(hediff.pawn);
        if (string.IsNullOrEmpty(visibleAnchor))
        {
            return false;
        }

        return !MatchesBaseLabel(hediff, visibleAnchor);
    }

    public static bool ShouldUseCombinedLabel(Hediff hediff)
    {
        return IsTrackedReanimationHediff(hediff) && !ShouldHide(hediff);
    }

    public static string GetCombinedLabelBase(Hediff hediff)
    {
        return ShouldUseCombinedLabel(hediff) ? CombinedLabel : hediff?.def?.label;
    }

    public static string GetCombinedLabelInBrackets(Hediff hediff, string original)
    {
        if (!ShouldUseCombinedLabel(hediff))
        {
            return original;
        }

        return GetPhaseBracketLabel(hediff.pawn);
    }

    private static string GetPhaseBracketLabel(Pawn pawn)
    {
        return GetVisiblePhase(pawn) switch
        {
            ReanimationPhase.Reanimating => ReanimatingLabel,
            ReanimationPhase.Reanimated => ReanimatedLabel,
            _ => null
        };
    }

    private static string GetVisibleAnchorLabel(Pawn pawn)
    {
        List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
        if (hediffs == null)
        {
            return null;
        }

        bool hasRot = ContainsBaseLabel(hediffs, ZombieRot);
        bool hasReanimating = ContainsBaseLabel(hediffs, Reanimating);
        bool hasFullyReanimated = ContainsBaseLabel(hediffs, FullyReanimated);

        return GetVisiblePhase(pawn) switch
        {
            ReanimationPhase.Reanimated => hasFullyReanimated ? FullyReanimated : hasReanimating ? Reanimating : hasRot ? ZombieRot : null,
            ReanimationPhase.Reanimating => hasReanimating ? Reanimating : hasRot ? ZombieRot : hasFullyReanimated ? FullyReanimated : null,
            ReanimationPhase.Rot => hasRot ? ZombieRot : hasReanimating ? Reanimating : hasFullyReanimated ? FullyReanimated : null,
            _ => null
        };
    }

    private static ReanimationPhase GetVisiblePhase(Pawn pawn)
    {
        List<Hediff> hediffs = pawn?.health?.hediffSet?.hediffs;
        if (hediffs == null)
        {
            return ReanimationPhase.None;
        }

        bool hasZombieInfection = ContainsBaseLabel(hediffs, ZombieInfection);
        bool hasRot = ContainsBaseLabel(hediffs, ZombieRot);
        bool hasReanimating = ContainsBaseLabel(hediffs, Reanimating);
        bool hasFullyReanimated = ContainsBaseLabel(hediffs, FullyReanimated);

        if (hasZombieInfection && (hasRot || hasReanimating || hasFullyReanimated))
        {
            return ReanimationPhase.Reanimating;
        }

        if (hasFullyReanimated)
        {
            return ReanimationPhase.Reanimated;
        }

        if (hasReanimating)
        {
            return ReanimationPhase.Reanimating;
        }

        if (hasRot)
        {
            return ReanimationPhase.Rot;
        }

        return ReanimationPhase.None;
    }

    private static bool ContainsBaseLabel(List<Hediff> hediffs, string expected)
    {
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (MatchesBaseLabel(hediffs[i], expected))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesBaseLabel(Hediff hediff, string expected)
    {
        string label = hediff?.def?.label;
        return string.Equals(Normalize(label), expected, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
