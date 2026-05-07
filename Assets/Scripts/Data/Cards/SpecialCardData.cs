using UnityEngine;

public enum SpecialEffectType
{
    None,
    DoubleDamage,
    DamagePerRound,
    Heal30,
    Reveal4Cards,
    AoEAttack,
    ForcedAllIn,
    VoidCard
}

[CreateAssetMenu(fileName = "SpecialCardData", menuName = "ChaosPoker/Cards/Special Card")]
public class SpecialCardData : CardData
{
    [Header("Special Card Rules")]
    public SpecialEffectType effectType = SpecialEffectType.None;
    public bool removeAfterUse;
    public bool oncePerGame;

    [Tooltip("Rank fed into poker scoring when this special is played with number cards.")]
    [Range(2, 14)] public int rankForScoring = 10;
}
