using System.Collections.Generic;
using UnityEngine;

public class PlayerEntity
{
    public int PlayerID;
    public bool Standing;
    public bool Over30;
    public EffectCard EffectCard;
    public readonly List<NumberCard> NumberCards = new List<NumberCard>();
    public int CurrentTotal;
    public readonly List<Effects> ActiveEffects = new List<Effects>();
    public string CardSequence = string.Empty;

    public void AddNumberCard(NumberCard card)
    {
        if (card == null)
        {
            Debug.LogWarning("Attempted to add null NumberCard to PlayerEntity.");
            return;
        }

        NumberCards.Add(card);
        CurrentTotal += card.card_value;
        CardSequence += $" {card.card_value} +";
        Over30 = CurrentTotal > 30;
    }

    public void AddEffectCard(EffectCard card)
    {
        if (card == null)
        {
            Debug.LogWarning("Attempted to add null EffectCard to PlayerEntity.");
            return;
        }

        EffectCard = card;
    }

    public void ApplyCardEffect(Effects effect, int currentRound)
    {
        CardSequence += $" = {CurrentTotal} ";

        switch (effect)
        {
            case Effects.DoubleAll:
                CurrentTotal *= 2;
                break;
            case Effects.EvenNumsDoubled:
                if (CurrentTotal % 2 == 0) CurrentTotal *= 2;
                break;
            case Effects.FlipSign:
                CurrentTotal *= -1;
                break;
            case Effects.HalfAll:
                CurrentTotal /= 2;
                break;
            case Effects.Minus5ToAll:
                CurrentTotal -= 5;
                break;
            case Effects.OddNumsDoubled:
                if (CurrentTotal % 2 != 0) CurrentTotal *= 2;
                break;
            case Effects.Plus5ToAll:
                CurrentTotal += 5;
                break;
            case Effects.RoundCard:
                CurrentTotal = (int)(System.Math.Round(CurrentTotal / 10.0, System.MidpointRounding.AwayFromZero) * 10);
                break;
            case Effects.Plus10ToAll:
                CurrentTotal += 10;
                break;
            case Effects.Plus2ForAllPosCards:
                foreach (NumberCard nc in NumberCards)
                {
                    if (nc.card_value > 0) CurrentTotal += 2;
                }
                break;
            case Effects.MultiplyByNumRounds:
                CurrentTotal *= currentRound;
                break;
            case Effects.DoublePositive:
                if (CurrentTotal > 0) CurrentTotal *= 2;
                break;
            case Effects.SwapToRight:
                // Reserved for multiplayer seat swap logic in later phase.
                break;
        }

        Over30 = CurrentTotal > 30;
        CardSequence += $"w/ {effect} = {CurrentTotal} +";
    }
}
