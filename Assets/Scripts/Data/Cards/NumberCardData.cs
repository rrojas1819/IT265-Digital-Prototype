using UnityEngine;

[CreateAssetMenu(fileName = "NumberCardData", menuName = "ChaosPoker/Cards/Number Card")]
public class NumberCardData : CardData
{
    [Header("Number Card Values")]
    [Range(1, 13)] public int rank = 9;
    public int value = 9;
}
