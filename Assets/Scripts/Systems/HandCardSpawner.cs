using UnityEngine;

public static class HandCardSpawner
{
    static int _nextId = 1;
    static int _nextDealUid = 1;

    public static int NextInstanceId() => _nextId++;

    static int NextDealUid() => _nextDealUid++;

    public static HandCard FromNumber(NumberCardData d)
    {
        if (d == null)
        {
            return SyntheticRank(Random.Range(4, 11), CardSuit.Club);
        }

        int raw = d.rank > 0 ? d.rank : 10;
        if (raw <= 0)
        {
            raw = 10;
        }

        int rank = Mathf.Clamp(raw, 2, 14);

        return new HandCard
        {
            instanceId = NextInstanceId(),
            dealUid = NextDealUid(),
            rank = rank,
            suit = d.isJoker ? CardSuit.None : d.suit,
            special = SpecialEffectType.None,
            displayName = string.IsNullOrEmpty(d.displayName) ? (d.isJoker ? "Joker" : $"{d.suit} {rank}") : d.displayName,
            isJoker = d.isJoker,
            removeAfterUse = false,
            returnsToPile = false,
            sourceCardId = d.cardId,
        };
    }

    public static HandCard FromSpecial(SpecialCardData d)
    {
        if (d == null)
        {
            return SyntheticRank(10, CardSuit.None);
        }

        int r = d.rankForScoring <= 0 ? 10 : d.rankForScoring;
        return new HandCard
        {
            instanceId = NextInstanceId(),
            dealUid = NextDealUid(),
            rank = Mathf.Clamp(r, 2, 14),
            suit = d.suit,
            special = d.effectType,
            displayName = string.IsNullOrEmpty(d.displayName) ? d.effectType.ToString() : d.displayName,
            isJoker = false,
            removeAfterUse = d.removeAfterUse,
            returnsToPile = !d.removeAfterUse,
            sourceCardId = d.cardId,
        };
    }

    public static HandCard SyntheticRank(int rank, CardSuit suit)
    {
        rank = Mathf.Clamp(rank, 2, 14);
        return new HandCard
        {
            instanceId = NextInstanceId(),
            dealUid = NextDealUid(),
            rank = rank,
            suit = suit,
            special = SpecialEffectType.None,
            displayName = $"Card {rank}",
            isJoker = false,
            removeAfterUse = false,
            returnsToPile = false,
            sourceCardId = "synthetic",
        };
    }

    public static HandCard SyntheticSpecial(SpecialEffectType effect)
    {
        bool consume =
            effect == SpecialEffectType.Reveal4Cards ||
            effect == SpecialEffectType.AoEAttack ||
            effect == SpecialEffectType.VoidCard ||
            effect == SpecialEffectType.ForcedAllIn;

        return new HandCard
        {
            instanceId = NextInstanceId(),
            dealUid = NextDealUid(),
            rank = 10,
            suit = CardSuit.None,
            special = effect,
            displayName = effect.ToString(),
            isJoker = false,
            removeAfterUse = consume,
            returnsToPile = !consume,
            sourceCardId = "synthetic_special",
        };
    }
}
