using System;
using UnityEngine;

[Serializable]
public struct HandCard : IEquatable<HandCard>
{
    public int instanceId;
    public int dealUid;
    public int rank;
    public CardSuit suit;
    public SpecialEffectType special;
    public string displayName;
    public bool isJoker;
    public bool removeAfterUse;
    public bool returnsToPile;
    public string sourceCardId;

    public bool IsSpecial => special != SpecialEffectType.None;

    public bool IsSuitedNumber => !IsSpecial && !isJoker && suit != CardSuit.None;

    public bool Equals(HandCard other) =>
        dealUid != 0 && other.dealUid != 0 ? dealUid == other.dealUid : instanceId == other.instanceId;

    public override bool Equals(object obj) => obj is HandCard other && Equals(other);

    public override int GetHashCode() => (dealUid != 0 ? dealUid : instanceId);

    public static bool operator ==(HandCard a, HandCard b) => a.Equals(b);

    public static bool operator !=(HandCard a, HandCard b) => !a.Equals(b);

    public int ScoringRank => Mathf.Clamp(rank <= 0 ? 10 : rank, 2, 14);

    public string UiRankLabel => ScoringRank switch
    {
        11 => "J",
        12 => "Q",
        13 => "K",
        14 => "A",
        _ => ScoringRank.ToString(),
    };

    public string ShortLabel()
    {
        if (IsSpecial)
        {
            return string.IsNullOrEmpty(displayName) ? special.ToString() : displayName;
        }

        if (isJoker)
        {
            return "JK";
        }

        char s = suit switch
        {
            CardSuit.Club => 'C',
            CardSuit.Diamond => 'D',
            CardSuit.Heart => 'H',
            CardSuit.Spade => 'S',
            _ => '?',
        };

        return $"{s}{UiRankLabel}";
    }
}
