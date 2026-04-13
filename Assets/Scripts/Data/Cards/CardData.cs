using UnityEngine;

public enum CardSuit
{
    Spade,
    Heart,
    Diamond,
    Club,
    None
}

public abstract class CardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName;

    [Header("Visuals")]
    public Sprite cardSprite;

    [Header("Rules")]
    public CardSuit suit = CardSuit.None;
}
