using System.Collections.Generic;
using UnityEngine;

public static class StrikeResolution
{
    public static void Execute(GameController game, int attackerSeat, int defenderSeat, List<HandCard> played)
    {
        PlayerEntity attacker = game.Roster[attackerSeat];
        PlayerEntity defender = game.Roster[defenderSeat];
        List<PlayerEntity> roster = game.Roster;
        RulesetData rules = game.rulesetData;
        DeckManager deck = game.Deck;

        if (played == null || played.Count == 0 || attacker == null || defender == null)
            return;

        bool hasForcedAllIn = false;
        bool playedJoker = false;
        for (int i = 0; i < played.Count; i++)
        {
            if (played[i].special == SpecialEffectType.ForcedAllIn) hasForcedAllIn = true;
            if (played[i].isJoker) playedJoker = true;
        }

        if (hasForcedAllIn)
        {
            ResolveForcedAllInDuel(game, attackerSeat, defenderSeat, played);
            return;
        }

        ProcessSpecials(game, attackerSeat, defenderSeat, played, attacker, defender, roster, rules, playedJoker);

        int raw = HandEval.ScorePlay(played, rules);
        game.strikesThisRound++;
        int dmg = Damage.ToDamage(raw, attacker, defender, rules);

        bool playedDoubleDamage = false;
        for (int i = 0; i < played.Count; i++)
        {
            if (played[i].special == SpecialEffectType.DoubleDamage)
            {
                playedDoubleDamage = true;
                break;
            }
        }

        if (attacker.DoubleNextStrike || playedDoubleDamage)
        {
            dmg *= 2;
        }

        if (attacker.DoubleNextStrike)
        {
            attacker.DoubleNextStrike = false;
        }

        defender.ApplyDamage(dmg, false);
        GameEvents.RaisePlayerDamaged(defenderSeat, dmg);
        if (defender.IsEliminated)
        {
            GameEvents.RaisePlayerEliminated(defenderSeat);
        }

        if (attacker.Buffs.SpadeChipEveryoneOnAttack)
        {
            int chip = (rules != null ? rules.spadeBonusDamage : 2) + game.matchRound;
            for (int i = 0; i < roster.Count; i++)
            {
                if (i == attackerSeat || roster[i].IsEliminated)
                {
                    continue;
                }

                roster[i].ApplyDamage(chip, true);
                GameEvents.RaisePlayerDamaged(i, chip);
                if (roster[i].IsEliminated)
                {
                    GameEvents.RaisePlayerEliminated(i);
                }
            }
        }

        FinishPlayedCards(played, deck);
    }

    static void ResolveForcedAllInDuel(GameController game, int attackerSeat, int defenderSeat, List<HandCard> initialPlayed)
    {
        PlayerEntity attacker = game.Roster[attackerSeat];
        PlayerEntity defender = game.Roster[defenderSeat];
        List<PlayerEntity> roster = game.Roster;
        RulesetData rules = game.rulesetData;
        DeckManager deck = game.Deck;

        bool playedJoker = false;
        for (int i = 0; i < initialPlayed.Count; i++)
        {
            if (initialPlayed[i].isJoker) playedJoker = true;
        }

        ProcessSpecials(game, attackerSeat, defenderSeat, initialPlayed, attacker, defender, roster, rules, playedJoker);

        var attackerFull = new List<HandCard>(initialPlayed);
        attackerFull.AddRange(DrainHand(attacker));

        List<HandCard> defenderFull = DrainHand(defender);

        int rawAtk = HandEval.ScorePlay(attackerFull, rules);
        int rawDef = HandEval.ScorePlay(defenderFull, rules);

        int dmgToDef = Damage.ToDamage(rawAtk, attacker, defender, rules);
        bool duelPlayedDoubleDamage = false;
        for (int i = 0; i < initialPlayed.Count; i++)
        {
            if (initialPlayed[i].special == SpecialEffectType.DoubleDamage)
            {
                duelPlayedDoubleDamage = true;
                break;
            }
        }

        if (attacker.DoubleNextStrike || duelPlayedDoubleDamage)
        {
            dmgToDef *= 2;
        }

        if (attacker.DoubleNextStrike)
        {
            attacker.DoubleNextStrike = false;
        }

        int dmgToAtk = Damage.ToDamage(rawDef, defender, attacker, rules);

        defender.ApplyDamage(dmgToDef, false);
        GameEvents.RaisePlayerDamaged(defenderSeat, dmgToDef);
        if (defender.IsEliminated)
        {
            GameEvents.RaisePlayerEliminated(defenderSeat);
        }

        attacker.ApplyDamage(dmgToAtk, false);
        GameEvents.RaisePlayerDamaged(attackerSeat, dmgToAtk);
        if (attacker.IsEliminated)
        {
            GameEvents.RaisePlayerEliminated(attackerSeat);
        }

        game.strikesThisRound++;

        if (attacker.Buffs.SpadeChipEveryoneOnAttack && !attacker.IsEliminated)
        {
            int chip = (rules != null ? rules.spadeBonusDamage : 2) + game.matchRound;
            for (int i = 0; i < roster.Count; i++)
            {
                if (i == attackerSeat || roster[i].IsEliminated)
                {
                    continue;
                }

                roster[i].ApplyDamage(chip, true);
                GameEvents.RaisePlayerDamaged(i, chip);
                if (roster[i].IsEliminated)
                {
                    GameEvents.RaisePlayerEliminated(i);
                }
            }
        }

        FinishPlayedCards(attackerFull, deck);
        FinishPlayedCards(defenderFull, deck);
    }

    static List<HandCard> DrainHand(PlayerEntity seat)
    {
        var list = new List<HandCard>();
        if (seat == null)
        {
            return list;
        }

        while (seat.Hand.Count > 0)
        {
            int rear = seat.Hand.Count - 1;
            list.Add(seat.Hand[rear]);
            seat.Hand.RemoveAt(rear);
        }

        return list;
    }

    static void ProcessSpecials(
        GameController game,
        int attackerSeat,
        int defenderSeat,
        List<HandCard> played,
        PlayerEntity attacker,
        PlayerEntity defender,
        List<PlayerEntity> roster,
        RulesetData rules,
        bool playedJoker)
    {
        int aoe = rules != null ? rules.aoeDamagePerOpponent : 22;
        int healAmt = rules != null ? rules.healSpecialAmount : 30;

        for (int i = 0; i < played.Count; i++)
        {
            HandCard c = played[i];
            switch (c.special)
            {
                case SpecialEffectType.AoEAttack:
                    for (int s = 0; s < roster.Count; s++)
                    {
                        if (s == attackerSeat || roster[s].IsEliminated)
                        {
                            continue;
                        }

                        roster[s].ApplyDamage(aoe, false);
                        GameEvents.RaisePlayerDamaged(s, aoe);
                        if (roster[s].IsEliminated)
                        {
                            GameEvents.RaisePlayerEliminated(s);
                        }
                    }

                    break;
                case SpecialEffectType.Heal30:
                    attacker.ApplyHeal(healAmt);
                    GameEvents.RaisePlayerHealed(attackerSeat, healAmt);
                    break;
                case SpecialEffectType.DamagePerRound:
                    game.GlobalDprRoundsRemaining = Mathf.Max(game.GlobalDprRoundsRemaining, rules != null ? rules.dprRoundDuration : 2);
                    break;
                case SpecialEffectType.Reveal4Cards:
                    game.RevealAllHandsThisTick = true;
                    game.KeepTurnAfterStrike = true;
                    break;
                case SpecialEffectType.ForcedAllIn:
                    break;
                case SpecialEffectType.VoidCard:
                    if (!attacker.Buffs.VoidCardUsedThisMatch)
                    {
                        attacker.Buffs.VoidCardUsedThisMatch = true;
                        game.ApplyVoidBanFromCard();
                        game.KeepTurnAfterStrike = true;
                    }

                    break;
            }
        }
    }

    public static bool IsValidBuffPair(List<HandCard> played, out CardSuit suit)
    {
        return TryValidateMatchingSuitNormals(played, out suit);
    }

    public static bool TryValidateMatchingSuitNormals(List<HandCard> played, out CardSuit suit)
    {
        suit = CardSuit.None;
        if (played == null || played.Count != 2)
        {
            return false;
        }

        if (!played[0].IsSuitedNumber)
        {
            return false;
        }

        suit = played[0].suit;
        for (int i = 0; i < played.Count; i++)
        {
            HandCard p = played[i];
            if (!p.IsSuitedNumber || p.suit != suit)
            {
                return false;
            }
        }

        return true;
    }

    public static bool ApplySuitBuffFromPlay(GameController game, int attackerSeat, List<HandCard> played)
    {
        PlayerEntity attacker = game.Roster[attackerSeat];
        if (played == null || attacker == null)
            return false;

        if (!TryValidateMatchingSuitNormals(played, out CardSuit suit))
            return false;

        GrantSuitBuffForSuit(suit, attacker, game.rulesetData, game.Roster, attackerSeat);
        Debug.Log($"Granted {suit} suit buff.");
        FinishPlayedCards(played, game.Deck);
        return true;
    }

    static void GrantSuitBuffForSuit(
        CardSuit suit,
        PlayerEntity attacker,
        RulesetData rules,
        List<PlayerEntity> roster,
        int attackerSeat)
    {
        switch (suit)
        {
            case CardSuit.Club:
                attacker.Buffs.HasClubBuff = true;
                attacker.Buffs.ClubBonusDamage += 3;
                StealOneRandomCard(attacker, roster, attackerSeat);
                break;
            case CardSuit.Diamond:
                attacker.Buffs.HasDiamondBuff = true;
                attacker.Buffs.OngoingFlatDefense += 1;
                if (!attacker.Buffs.DiamondShieldUsedThisMatch)
                {
                    attacker.Buffs.DiamondShieldUsedThisMatch = true;
                    attacker.Buffs.DiamondShieldHp = 20;
                    attacker.Buffs.DiamondShieldRoundsRemaining = 1;
                }

                break;
            case CardSuit.Heart:
                attacker.Buffs.HasHeartBuff = true;
                attacker.Buffs.HeartImmuneChip = true;
                attacker.Buffs.HeartDeathNegationAvailable = true;
                break;
            case CardSuit.Spade:
                attacker.Buffs.HasSpadeBuff = true;
                attacker.Buffs.SpadeChipEveryoneOnAttack = true;
                attacker.Buffs.SpadeStrikeBonus += rules != null ? rules.spadeBonusDamage : 2;
                break;
        }
    }

    static void StealOneRandomCard(PlayerEntity thief, List<PlayerEntity> roster, int thiefSeat)
    {
        var donors = new List<int>();
        for (int i = 0; i < roster.Count; i++)
        {
            if (i == thiefSeat || roster[i].IsEliminated || roster[i].Hand.Count == 0)
            {
                continue;
            }

            donors.Add(i);
        }

        if (donors.Count == 0)
        {
            return;
        }

        int pick = donors[RNGService.Instance.Range(0, donors.Count)];
        PlayerEntity victim = roster[pick];
        int idx = RNGService.Instance.Range(0, victim.Hand.Count);
        HandCard stolen = victim.Hand[idx];
        victim.Hand.RemoveAt(idx);
        thief.Hand.Add(stolen);
        Debug.Log($"Seat {thief.SeatIndex + 1} stole from seat {pick + 1}.");
    }

    static void FinishPlayedCards(List<HandCard> played, DeckManager deck)
    {
        if (deck == null)
        {
            return;
        }

        for (int i = 0; i < played.Count; i++)
        {
            HandCard c = played[i];
            if (c.returnsToPile)
            {
                deck.ReturnToPile(c);
            }
        }
    }
}
