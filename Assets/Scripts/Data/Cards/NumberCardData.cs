using UnityEngine;

[CreateAssetMenu(fileName = "NumberCardData", menuName = "ChaosPoker/Cards/Number Card")]
public class NumberCardData : CardData
{
    [Header("Number Card Values")]
    [Range(1, 14)] public int rank = 9;

    [Tooltip("Uses joker art; cannot earn permanent suit buffs from four-of-a-kind.")]
    public bool isJoker;
}
