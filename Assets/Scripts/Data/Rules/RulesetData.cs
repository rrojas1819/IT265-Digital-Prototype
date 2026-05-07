using UnityEngine;

[CreateAssetMenu(fileName = "RulesetData", menuName = "ChaosPoker/Rules/Ruleset")]
public class RulesetData : ScriptableObject
{
    [Header("Match Rules")]
    public int minPlayers = 3;
    public int maxPlayers = 6;
    public int maxRounds = 10;
    public int startingHealth = 180;
    public int maxHandSize = 6;
    public int maxCardsPerAttack = 5;

    [Header("Damage Tuning")]
    public int duplicateRankBonus = 5;
    [Range(0f, 2f)] public float tripleDamageMultiplier = 1.3f;
    [Range(0f, 1f)] public float straightFlushTargetHpPercent = 0.5f;

    [Header("Suit Buffs")]
    public int spadeBonusDamage = 2;
    public int diamondFlatDefense = 3;
    public int jokerChipDamagePerRound = 2;

    [Header("Special strike tuning")]
    public int aoeDamagePerOpponent = 22;
    public int dprTickDamage = 10;
    public int dprRoundDuration = 2;
    public int healSpecialAmount = 30;
}
