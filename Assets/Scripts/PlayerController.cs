using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerChoices
{
    Hit,
    Stand,
    PlayEffect
}


public class PlayerController : MonoBehaviour
{
    public int PlayerID;
    public GameController controller;

    public bool standing = false;
    public bool over30 = false;

    public EffectCard effect_card = null;
    public List<NumberCard> number_cards = new List<NumberCard>();

    public int curr_total = 0;

    public List<Effects> active_effects = new List<Effects>();

    public string card_seq = "";

    void Start()
    {
        controller = FindAnyObjectByType<GameController>();
    }

    private void Update()
    {
        if (curr_total > 30)
        {
            over30 = true;
        }
    }

    public void AddNumberCard(NumberCard card)
    {
        if (card == null)
        {
            Debug.Log("Adding Null Number Card");
            return;
        }

        //Debug.Log("Player " + PlayerID + " got a " + card.card_value);
        number_cards.Add(card);
        curr_total += card.card_value;
        card_seq += $" {card.card_value} +";
    }

    public void AddEffectCard(EffectCard card)
    {
        if (card == null)
        {
            Debug.Log("Adding Null Effect Card");
            return;
        }

        //Debug.Log("Player " + PlayerID + " got a " + card.card_effect);
        effect_card = card;
    }

    public void ApplyCardEffect(Effects curr_effect)
    {
        Debug.Log(curr_effect + " Applied to Player " + PlayerID);

        card_seq += $" = {curr_total} ";

        switch (curr_effect)
        {
            case Effects.DoubleAll:
                curr_total *= 2;
                break;

            case Effects.EvenNumsDoubled:
                if (curr_total % 2 == 0)
                    curr_total *= 2;

                break;

            //case Effects.EvenNumsHalved:
            //    if (curr_total % 2 == 0)
            //        curr_total /= 2;

            //    break;

            case Effects.FlipSign:
                //curr_total = Mathf.Abs(curr_total);
                curr_total *= -1;
                break;

            case Effects.HalfAll:
                curr_total /= 2;
                break;

            case Effects.Minus5ToAll:
                curr_total -= 5;
                break;

            case Effects.OddNumsDoubled:
                if (curr_total % 2 != 0)
                    curr_total *= 2;

                break;

            //case Effects.OddNumsHalved:
            //    if (curr_total % 2 != 0)
            //        curr_total /= 2;

            //    break;

            case Effects.Plus5ToAll:
                curr_total += 5;
                break;

            case Effects.RoundCard:
                curr_total = (int)(System.Math.Round(curr_total / 10.0, System.MidpointRounding.AwayFromZero) * 10);
                break;

            case Effects.SwapToRight:
                //TODO:
                break;

            case Effects.Plus10ToAll:
                curr_total += 10;
                break;

            case Effects.Plus2ForAllPosCards:
                foreach (NumberCard nc in number_cards)
                {
                    if (nc.card_value > 0)
                        curr_total += 2;
                }

                break;

            case Effects.MultiplyByNumRounds:
                curr_total *= controller.round;
                break;

            case Effects.DoublePositive:
                if (curr_total > 0)
                    curr_total *= 2;

                break;
        }

        card_seq += $"w/ {curr_effect} = {curr_total} +";
    }
}
