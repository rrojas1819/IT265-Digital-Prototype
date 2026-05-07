using System;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager
{
    List<HandCard> drawPile = new List<HandCard>();

    public int RemainingCount => drawPile.Count;

    public DeckManager(NumberCardData[] numbers, SpecialCardData[] specials)
    {
        if (numbers != null)
        {
            foreach (NumberCardData c in numbers)
            {
                if (c == null)
                {
                    continue;
                }

                for (int i = 0; i < 3; i++)
                {
                    drawPile.Add(HandCardSpawner.FromNumber(c));
                }
            }
        }

        if (specials != null)
        {
            foreach (SpecialCardData s in specials)
            {
                if (s == null || s.effectType == SpecialEffectType.None)
                {
                    continue;
                }

                drawPile.Add(HandCardSpawner.FromSpecial(s));
            }
        }

        const int MinDeckSize = 28;
        while (drawPile.Count < MinDeckSize)
        {
            int r = RNGService.Instance.Range(4, 11);
            CardSuit s = (CardSuit)RNGService.Instance.Range(0, 4);
            drawPile.Add(HandCardSpawner.SyntheticRank(r, s));
        }

        RNGService.Instance.Shuffle(drawPile);
    }

    public bool TryPullFirst(Predicate<HandCard> match, out HandCard card)
    {
        card = default;
        if (match == null || drawPile.Count == 0)
        {
            return false;
        }

        for (int i = drawPile.Count - 1; i >= 0; i--)
        {
            if (match(drawPile[i]))
            {
                card = drawPile[i];
                drawPile.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void ReturnToPile(HandCard card)
    {
        drawPile.Add(card);
        RNGService.Instance.Shuffle(drawPile);
    }

    public void ReturnMany(IEnumerable<HandCard> cards)
    {
        if (cards == null)
        {
            return;
        }

        foreach (HandCard c in cards)
        {
            drawPile.Add(c);
        }

        RNGService.Instance.Shuffle(drawPile);
    }

    public void TopUp(PlayerEntity seat, RulesetData rules, Func<HandCard, bool> cardAllowed = null)
    {
        int cap = rules != null ? Mathf.Max(1, rules.maxHandSize) : 6;
        int guard = 0;
        while (seat.Hand.Count < cap && drawPile.Count > 0 && guard < 512)
        {
            guard++;
            HandCard top = drawPile[drawPile.Count - 1];
            if (cardAllowed != null && !cardAllowed(top))
            {
                HandCard blocked = top;
                drawPile.RemoveAt(drawPile.Count - 1);
                drawPile.Insert(RNGService.Instance.Range(0, drawPile.Count + 1), blocked);
                continue;
            }

            drawPile.RemoveAt(drawPile.Count - 1);
            seat.Hand.Add(top);
        }

        int synthGuard = 0;
        while (seat.Hand.Count < cap && synthGuard < 512)
        {
            synthGuard++;
            int r = RNGService.Instance.Range(4, 11);
            CardSuit s = (CardSuit)RNGService.Instance.Range(0, 4);
            HandCard synth = HandCardSpawner.SyntheticRank(r, s);
            if (cardAllowed != null && !cardAllowed(synth))
            {
                continue;
            }

            seat.Hand.Add(synth);
        }
    }
}
public static class HandEval
{
    public static int ScorePlay(List<HandCard> played, RulesetData rules)
    {
        if (played == null || played.Count == 0)
            return rules != null ? rules.duplicateRankBonus : 5;

        var freq = new Dictionary<int, int>();
        var suitedRanks = new List<int>(played.Count);
        var suitedSuits = new List<CardSuit>(played.Count);
        foreach (HandCard c in played)
        {
            int r = Mathf.Clamp(c.ScoringRank, 2, 14);
            if (!freq.ContainsKey(r))
                freq[r] = 0;
            freq[r]++;
            if (c.IsSuitedNumber)
            {
                suitedRanks.Add(r);
                suitedSuits.Add(c.suit);
            }
        }

        int sum = 0;
        foreach (HandCard c in played)
            sum += Mathf.Clamp(c.ScoringRank, 2, 14);

        int bestPairRank = -1;
        int tripsRank = -1;
        int bestCount = 1;

        foreach (KeyValuePair<int, int> kv in freq)
        {
            int r = kv.Key;
            int c = kv.Value;
            if (r == 10 && c >= 4)
                return rules != null ? rules.duplicateRankBonus * 8 : 120;

            if (c >= 3)
                tripsRank = Mathf.Max(tripsRank, r);
            if (c >= 2)
                bestPairRank = Mathf.Max(bestPairRank, r);
            if (c > bestCount)
                bestCount = c;
        }

        int distinctPairs = 0;
        foreach (KeyValuePair<int, int> kv in freq)
        {
            if (kv.Value >= 2)
            {
                distinctPairs++;
            }
        }

        bool hasFullHouse = tripsRank >= 0 && distinctPairs >= 2;
        bool hasFourKind = bestCount >= 4;
        bool hasFiveKind = bestCount >= 5;

        bool isFlush = false;
        bool isStraight = false;
        if (suitedRanks.Count >= 5)
        {
            CardSuit s0 = suitedSuits[0];
            isFlush = true;
            for (int i = 1; i < suitedSuits.Count; i++)
            {
                if (suitedSuits[i] != s0)
                {
                    isFlush = false;
                    break;
                }
            }

            var uniqueRanks = new List<int>(new HashSet<int>(suitedRanks));
            uniqueRanks.Sort();
            if (uniqueRanks.Count >= 5)
            {
                int run = 1;
                for (int i = 1; i < uniqueRanks.Count; i++)
                {
                    if (uniqueRanks[i] == uniqueRanks[i - 1] + 1)
                    {
                        run++;
                        if (run >= 5)
                        {
                            isStraight = true;
                            break;
                        }
                    }
                    else
                    {
                        run = 1;
                    }
                }
            }
        }

        bool isStraightFlush = isStraight && isFlush;

        float mult = 1f;
        if (tripsRank >= 0)
            mult *= rules != null ? rules.tripleDamageMultiplier : 1.25f;
        if (isStraightFlush)
            mult *= 1.5f;
        else if (isStraight || isFlush)
            mult *= 1.2f;

        int pairBonus = bestPairRank >= 0 ? (rules != null ? rules.duplicateRankBonus : 5) : 0;
        int bonus = pairBonus;
        int dupBonus = rules != null ? rules.duplicateRankBonus : 5;
        if (hasFullHouse)
            bonus += dupBonus * 4;
        if (hasFourKind)
            bonus += dupBonus * 6;
        if (hasFiveKind)
            bonus += dupBonus * 8;
        if (isStraight)
            bonus += dupBonus * 3;
        if (isFlush)
            bonus += dupBonus * 3;

        return Mathf.Max(1, Mathf.RoundToInt(sum * mult + bonus));
    }
}

public static class Damage
{
    public static int ToDamage(int rawScore, PlayerEntity attacker, PlayerEntity defender, RulesetData rules)
    {
        int basePow = Mathf.Max(6, rawScore);
        if (attacker != null && attacker.SpadeBonusNextStrike)
        {
            basePow += rules != null ? rules.spadeBonusDamage : 2;
        }

        if (attacker != null && attacker.Buffs.ClubBonusDamage > 0)
        {
            basePow += attacker.Buffs.ClubBonusDamage;
        }

        if (attacker != null && attacker.Buffs.SpadeStrikeBonus > 0)
        {
            basePow += attacker.Buffs.SpadeStrikeBonus;
        }

        int dmg = basePow;

        if (defender != null && defender.Buffs.OngoingFlatDefense > 0)
        {
            dmg = Mathf.Max(1, dmg - defender.Buffs.OngoingFlatDefense);
        }

        if (defender != null && defender.Buffs.DiamondShieldHp > 0 && dmg > 0)
        {
            int absorb = Mathf.Min(dmg, defender.Buffs.DiamondShieldHp);
            dmg -= absorb;
            defender.Buffs.DiamondShieldHp -= absorb;
        }

        if (defender != null && defender.TempShieldCharges > 0)
        {
            int block = Mathf.Min(defender.TempShieldCharges, rules != null ? rules.diamondFlatDefense : 3);
            dmg = Mathf.Max(1, dmg - block);
            defender.TempShieldCharges--;
        }

        return Mathf.Clamp(dmg, 4, rules != null ? rules.startingHealth : 260);
    }
}
public static class BotAI
{
    public static int PickTargetSeat(int attacker, IList<PlayerEntity> roster)
    {
        for (int step = 1; step <= roster.Count; step++)
        {
            int idx = (attacker + step) % roster.Count;
            if (!roster[idx].IsEliminated)
                return idx;
        }

        return -1;
    }
}

public static class RoundEnd
{
    public static int WinnerByHighestHp(IList<PlayerEntity> roster)
    {
        int pick = -1;
        int best = int.MinValue;
        for (int i = 0; i < roster.Count; i++)
        {
            if (roster[i].IsEliminated)
                continue;
            int hp = roster[i].CurrentHp;
            if (hp > best)
            {
                best = hp;
                pick = i;
            }
            else if (hp == best && pick >= 0)
            {
                if (roster[i].SeatIndex < roster[pick].SeatIndex)
                    pick = i;
            }
        }

        return pick;
    }
}

