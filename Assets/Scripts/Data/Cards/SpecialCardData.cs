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
}
