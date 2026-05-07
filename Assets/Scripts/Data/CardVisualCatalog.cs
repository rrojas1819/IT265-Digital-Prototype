using UnityEngine;

[CreateAssetMenu(fileName = "CardVisualCatalog", menuName = "ChaosPoker/Card Visual Catalog")]
public class CardVisualCatalog : ScriptableObject
{
    [Header("Clubs — rank 4..10 → index (ScoringRank − 4); pip art can still be wrong if array order ≠ art. Rank overlay always shows HandCard rank.")]
    public Sprite[] clubRankSprites = new Sprite[7];

    [Header("Diamond / Heart / Spade — rank slices for Full-* sheets")]
    [Tooltip("Seven slots: ranks 4..10. Populated via ChaosPoker → Repair Card Visual Catalog (Full-X_{rank−1}); high ranks clamp to avoid blank sprites.")]
    public Sprite[] diamondRankSprites = new Sprite[7];
    public Sprite[] heartRankSprites = new Sprite[7];
    public Sprite[] spadeRankSprites = new Sprite[7];

    [Tooltip("Fallback if a rank slice is missing (legacy single-frame bind).")]
    public Sprite diamondShared;
    public Sprite heartShared;
    public Sprite spadeShared;
    public Sprite jokerSprite;

    [Header("Specials (CARD_REFERENCE art)")]
    public Sprite aoeVolcano;
    public Sprite doubleDamage;
    public Sprite damagePerRound;
    public Sprite forcedAllIn;
    public Sprite heal30;
    public Sprite reveal4;
    public Sprite voidCard;

    public Sprite ResolveFace(HandCard h)
    {
        if (h.IsSpecial)
        {
            return h.special switch
            {
                SpecialEffectType.AoEAttack => aoeVolcano,
                SpecialEffectType.DoubleDamage => doubleDamage,
                SpecialEffectType.DamagePerRound => damagePerRound,
                SpecialEffectType.ForcedAllIn => forcedAllIn,
                SpecialEffectType.Heal30 => heal30,
                SpecialEffectType.Reveal4Cards => reveal4,
                SpecialEffectType.VoidCard => voidCard,
                _ => null,
            };
        }

        if (h.isJoker)
        {
            return jokerSprite;
        }

        int sr = h.ScoringRank;
        if (h.suit == CardSuit.Club && sr >= 4 && sr <= 10)
        {
            int idx = sr - 4;
            if (clubRankSprites != null && idx >= 0 && idx < clubRankSprites.Length)
            {
                return clubRankSprites[idx];
            }
        }

        if (sr >= 4 && sr <= 10 && !h.IsSpecial && !h.isJoker)
        {
            int idx = sr - 4;
            Sprite[] ranks = h.suit switch
            {
                CardSuit.Diamond => diamondRankSprites,
                CardSuit.Heart => heartRankSprites,
                CardSuit.Spade => spadeRankSprites,
                _ => null,
            };
            Sprite fromRanks = TryRankSlot(ranks, idx);
            if (fromRanks != null)
            {
                return fromRanks;
            }
        }

        return h.suit switch
        {
            CardSuit.Diamond => diamondShared,
            CardSuit.Heart => heartShared,
            CardSuit.Spade => spadeShared,
            CardSuit.Club when clubRankSprites is { Length: > 0 } =>
                clubRankSprites[Mathf.Clamp(sr - 4, 0, clubRankSprites.Length - 1)],
            _ => null,
        };
    }

    static Sprite TryRankSlot(Sprite[] ranks, int indexFromRank4)
    {
        if (ranks == null || ranks.Length <= 0)
        {
            return null;
        }

        indexFromRank4 = Mathf.Clamp(indexFromRank4, 0, ranks.Length - 1);
        Sprite s = ranks[indexFromRank4];
        if (s != null)
        {
            return s;
        }

        return null;
    }

    public bool UseRankOverlay(HandCard h)
    {
        return h.IsSuitedNumber;
    }
}
