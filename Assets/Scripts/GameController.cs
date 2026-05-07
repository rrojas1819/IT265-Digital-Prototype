using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{

    public UiController UI;
    public int curr_player_turn = -1;

    public RulesetData rulesetData;
    [HideInInspector] public NumberCardData[] numberCardDataLibrary;
    [HideInInspector] public SpecialCardData[] specialCardDataLibrary;

    List<PlayerEntity> roster = new List<PlayerEntity>();
    DeckManager deck;
    public int strikesThisRound;

    public List<PlayerEntity> Roster => roster;
    public DeckManager Deck => deck;

    bool matchConcluded;
    public bool IsHotSeatMode;
    public bool RevealAllHandsThisTick;
    public bool KeepTurnAfterStrike;

    public int playerCount;
    public int matchRound = 1;

    public int GlobalDprRoundsRemaining;

    bool voidBanActive;
    SpecialEffectType voidBannedSpecialDraw;
    int voidDrawBanRank1 = -1;
    int voidDrawBanRank2 = -1;

    void Start()
    {
        rulesetData = Resources.Load<RulesetData>("Rules/DefaultRulesetData");
        if (rulesetData == null)
        {
            rulesetData = ScriptableObject.CreateInstance<RulesetData>();
            Debug.LogWarning("Default rules asset missing. Using in-memory RulesetData fallback.");
        }

        numberCardDataLibrary = Resources.LoadAll<NumberCardData>("Cards");
        specialCardDataLibrary = Resources.LoadAll<SpecialCardData>("Cards");
        specialCardDataLibrary = EnsureSpecialCoverage(specialCardDataLibrary);
        if (numberCardDataLibrary.Length == 0 || specialCardDataLibrary.Length == 0)
            Debug.LogWarning("Card data sparse in Resources/Cards — DeckManager synthesizes ranks.");
    }

    static SpecialCardData BuildSyntheticSpecialData(SpecialEffectType effect)
    {
        var d = ScriptableObject.CreateInstance<SpecialCardData>();
        d.cardId = $"runtime_{effect}";
        d.displayName = effect.ToString();
        d.suit = CardSuit.None;
        d.effectType = effect;
        d.rankForScoring = 10;
        d.removeAfterUse =
            effect == SpecialEffectType.Reveal4Cards ||
            effect == SpecialEffectType.AoEAttack ||
            effect == SpecialEffectType.ForcedAllIn ||
            effect == SpecialEffectType.VoidCard;
        d.oncePerGame = effect == SpecialEffectType.VoidCard;
        return d;
    }

    static SpecialCardData[] EnsureSpecialCoverage(SpecialCardData[] loaded)
    {
        var merged = new List<SpecialCardData>();
        if (loaded != null)
        {
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].effectType != SpecialEffectType.None)
                {
                    merged.Add(loaded[i]);
                }
            }
        }

        foreach (SpecialEffectType fx in new[]
        {
            SpecialEffectType.DoubleDamage,
            SpecialEffectType.DamagePerRound,
            SpecialEffectType.Heal30,
            SpecialEffectType.Reveal4Cards,
            SpecialEffectType.AoEAttack,
            SpecialEffectType.ForcedAllIn,
            SpecialEffectType.VoidCard,
        })
        {
            bool exists = false;
            for (int i = 0; i < merged.Count; i++)
            {
                if (merged[i] != null && merged[i].effectType == fx)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                merged.Add(BuildSyntheticSpecialData(fx));
            }
        }

        return merged.ToArray();
    }

    public void StartGame(int players, bool hotSeat = false)
    {
        if (players < 2 || players > 4)
        {
            Debug.LogError($"StartGame rejected player count {players}. Supported 2-4.");
            return;
        }

        matchConcluded = false;
        IsHotSeatMode = hotSeat;

        playerCount = players;
        roster.Clear();
        matchRound = 1;
        strikesThisRound = 0;

        int hp = rulesetData != null ? rulesetData.startingHealth : 180;

        for (int i = 0; i < players; i++)
        {
            var seat = new PlayerEntity
            {
                SeatIndex = i,
                IsHuman = hotSeat || i == 0,
                CurrentHp = hp,
                MaxHp = hp,
            };

            seat.Buffs.ResetBuffs();

            seat.TempShieldCharges = seat.IsHuman ? 0 : 1;
            roster.Add(seat);
        }

        voidBanActive = false;
        voidBannedSpecialDraw = SpecialEffectType.None;
        voidDrawBanRank1 = -1;
        voidDrawBanRank2 = -1;
        GlobalDprRoundsRemaining = 0;

        deck = new DeckManager(numberCardDataLibrary, specialCardDataLibrary);

        foreach (PlayerEntity seat in roster)
        {
            DealStarterHand(seat);
            Debug.Log($"Seat {seat.SeatIndex + 1} dealt {seat.Hand.Count} cards.");
            GameEvents.RaiseCardDrawn(seat.SeatIndex);
        }

        curr_player_turn = 0;

        GameEvents.RaiseGameStarted(players);
        GameEvents.RaiseTurnChanged(curr_player_turn);
        GameEvents.RaiseRoundChanged(matchRound);

        if (UI == null)
        {
            UI = FindFirstObjectByType<UiController>();
            if (UI == null)
            {
                Debug.LogError("GameController could not find UiController to render seats/cards.");
                return;
            }
        }

        UI.RefreshMatchPresentation(this);
    }

    void DealStarterHand(PlayerEntity seat)
    {
        var suits = new[] { CardSuit.Spade, CardSuit.Heart, CardSuit.Diamond, CardSuit.Club };
        foreach (CardSuit suit in suits)
        {
            if (!deck.TryPullFirst(h => CardDrawAllowed(h) && h.IsSuitedNumber && h.suit == suit, out HandCard pulled))
            {
                pulled = HandCardSpawner.SyntheticRank(RNGService.Instance.Range(4, 11), suit);
            }

            seat.Hand.Add(pulled);
        }

        HandCard starterSpecial = PullStarterSpecialCard();
        seat.Hand.Add(starterSpecial);

        if (!deck.TryPullFirst(CardDrawAllowed, out HandCard extra))
        {
            extra = HandCardSpawner.SyntheticRank(RNGService.Instance.Range(4, 11), (CardSuit)RNGService.Instance.Range(0, 4));
        }

        seat.Hand.Add(extra);

        RNGService.Instance.Shuffle(seat.Hand);
        deck.TopUp(seat, rulesetData, CardDrawAllowed);
    }

    HandCard PullStarterSpecialCard()
    {
        if (deck.TryPullFirst(h => CardDrawAllowed(h) && h.IsSpecial, out HandCard sp))
        {
            return sp;
        }

        SpecialCardData dd = FindSpecialCardData(SpecialEffectType.DoubleDamage);
        if (dd != null)
        {
            return HandCardSpawner.FromSpecial(dd);
        }

        SpecialCardData heal = FindSpecialCardData(SpecialEffectType.Heal30);
        if (heal != null)
        {
            return HandCardSpawner.FromSpecial(heal);
        }

        if (specialCardDataLibrary != null)
        {
            foreach (SpecialCardData c in specialCardDataLibrary)
            {
                if (c != null && c.effectType != SpecialEffectType.None)
                {
                    return HandCardSpawner.FromSpecial(c);
                }
            }
        }

        return HandCardSpawner.SyntheticSpecial(SpecialEffectType.DoubleDamage);
    }

    SpecialCardData FindSpecialCardData(SpecialEffectType fx)
    {
        if (specialCardDataLibrary == null)
        {
            return null;
        }

        foreach (SpecialCardData s in specialCardDataLibrary)
        {
            if (s != null && s.effectType == fx)
            {
                return s;
            }
        }

        return null;
    }

    public PlayerEntity GetSeat(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= roster.Count)
            return null;

        return roster[seatIndex];
    }

    public void CommitSelectedAction(List<int> selectedDealUids)
    {
        if (matchConcluded || !MatchInProgress())
            return;

        if (curr_player_turn < 0 || curr_player_turn >= roster.Count)
            return;

        PlayerEntity attacker = roster[curr_player_turn];
        if (!attacker.IsHuman || attacker.IsEliminated)
            return;

        int cap = rulesetData != null ? Mathf.Max(1, rulesetData.maxCardsPerAttack) : 5;
        List<HandCard> play = new List<HandCard>();
        if (selectedDealUids != null && selectedDealUids.Count > 0)
        {
            for (int i = 0; i < selectedDealUids.Count && play.Count < cap; i++)
            {
                int uid = selectedDealUids[i];
                for (int h = attacker.Hand.Count - 1; h >= 0; h--)
                {
                    if (attacker.Hand[h].dealUid == uid || (uid != 0 && attacker.Hand[h].dealUid == 0 && attacker.Hand[h].instanceId == uid))
                    {
                        play.Add(attacker.Hand[h]);
                        attacker.Hand.RemoveAt(h);
                        break;
                    }
                }
            }
        }

        if (play.Count == 0)
        {
            Debug.LogWarning("[GameController] No cards selected; STRIKE ignored.");
            UI?.RefreshMatchPresentation(this);
            return;
        }

        if (!ResolveStrike(curr_player_turn, play))
        {
            for (int i = 0; i < play.Count; i++)
            {
                attacker.Hand.Add(play[i]);
            }

            Debug.LogWarning("[GameController] Strike did not resolve (no valid target or no cards) — returned to hand.");
            UI?.RefreshMatchPresentation(this);
            return;
        }

        if (matchConcluded || EliminationWinnerGate())
            return;

        if (KeepTurnAfterStrike)
        {
            KeepTurnAfterStrike = false;
            UI?.RefreshMatchPresentation(this);
            return;
        }

        AdvanceSeat();
        if (matchConcluded || EliminationWinnerGate())
            return;

        TickBotsCascade(0);
        UI?.RefreshMatchPresentation(this);
    }

    public void CommitBuffActivation(List<int> selectedDealUids)
    {
        if (matchConcluded || !MatchInProgress())
        {
            return;
        }

        if (curr_player_turn < 0 || curr_player_turn >= roster.Count)
        {
            return;
        }

        PlayerEntity attacker = roster[curr_player_turn];
        if (!attacker.IsHuman || attacker.IsEliminated)
        {
            return;
        }

        if (selectedDealUids == null || selectedDealUids.Count != 2)
        {
            Debug.LogWarning("[GameController] Buff activation requires exactly two selected cards.");
            return;
        }

        var play = new List<HandCard>(2);
        for (int i = 0; i < 2; i++)
        {
            int uid = selectedDealUids[i];
            bool found = false;
            for (int h = 0; h < attacker.Hand.Count; h++)
            {
                if (attacker.Hand[h].dealUid == uid || (uid != 0 && attacker.Hand[h].dealUid == 0 && attacker.Hand[h].instanceId == uid))
                {
                    play.Add(attacker.Hand[h]);
                    attacker.Hand.RemoveAt(h);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                for (int r = play.Count - 1; r >= 0; r--)
                {
                    attacker.Hand.Add(play[r]);
                }

                Debug.LogWarning("[GameController] Buff: could not pull both cards from hand (IDs / order).");
                return;
            }
        }

        if (!StrikeResolution.TryValidateMatchingSuitNormals(play, out _))
        {
            for (int r = play.Count - 1; r >= 0; r--)
            {
                attacker.Hand.Add(play[r]);
            }

            Debug.LogWarning("[GameController] Buff: pick two number cards of the same suit.");
            return;
        }

        if (!StrikeResolution.ApplySuitBuffFromPlay(this, curr_player_turn, play))
        {
            for (int p = 0; p < play.Count; p++)
            {
                attacker.Hand.Add(play[p]);
            }

            Debug.LogError("[GameController] Buff apply failed after removal — hand restored.");
            return;
        }

        GameEvents.RaiseCardDiscarded(curr_player_turn);
        deck?.TopUp(attacker, rulesetData, CardDrawAllowed);

        if (matchConcluded || EliminationWinnerGate())
        {
            return;
        }

        AdvanceSeat();
        if (matchConcluded || EliminationWinnerGate())
        {
            return;
        }

        TickBotsCascade(0);
        UI?.RefreshMatchPresentation(this);
    }

    public void ApplyVoidBanFromCard()
    {
        voidBanActive = true;
        voidBannedSpecialDraw = (SpecialEffectType)RNGService.Instance.Range(1, 7);
        voidDrawBanRank1 = RNGService.Instance.Range(4, 11);
        voidDrawBanRank2 = RNGService.Instance.Range(4, 10);
        if (voidDrawBanRank2 >= voidDrawBanRank1) voidDrawBanRank2++;

        Debug.Log($"Void ban applied — {voidBannedSpecialDraw}, ranks {voidDrawBanRank1} and {voidDrawBanRank2}.");

        for (int i = 0; i < roster.Count; i++)
        {
            PlayerEntity p = roster[i];
            for (int h = p.Hand.Count - 1; h >= 0; h--)
            {
                if (!CardDrawAllowed(p.Hand[h]))
                    p.Hand.RemoveAt(h);
            }
            deck?.TopUp(p, rulesetData, CardDrawAllowed);
        }
    }

    bool CardDrawAllowed(HandCard h)
    {
        if (!voidBanActive)
        {
            return true;
        }

        if (h.IsSpecial && h.special == voidBannedSpecialDraw)
        {
            return false;
        }

        if (!h.IsSpecial && (h.ScoringRank == voidDrawBanRank1 || h.ScoringRank == voidDrawBanRank2))
        {
            return false;
        }

        return true;
    }

    public void AdvanceTurn()
    {
        if (matchConcluded || !MatchInProgress())
            return;

        AdvanceSeat();

        if (EliminationWinnerGate())
            return;

        TickBotsCascade(0);
        UI?.RefreshMatchPresentation(this);
    }

    void TickBotsCascade(int guard)
    {
        if (matchConcluded || guard > 32 || !MatchInProgress())
            return;

        if (roster[curr_player_turn].IsHuman || roster[curr_player_turn].IsEliminated)
            return;

        ResolveStrike(curr_player_turn, BuildBotPlayableChain(roster[curr_player_turn]));

        if (matchConcluded || EliminationWinnerGate())
            return;

        AdvanceSeat();

        if (matchConcluded || EliminationWinnerGate())
            return;

        TickBotsCascade(guard + 1);
    }

    static bool NumberSubsetHomogeneous(List<HandCard> set)
    {
        var numbers = new List<HandCard>();
        for (int i = 0; i < set.Count; i++)
        {
            if (set[i].IsSuitedNumber)
            {
                numbers.Add(set[i]);
            }
        }

        if (numbers.Count <= 1)
        {
            return true;
        }

        int r0 = numbers[0].ScoringRank;
        CardSuit s0 = numbers[0].suit;
        bool allSameRank = true;
        bool allSameSuit = true;
        for (int i = 1; i < numbers.Count; i++)
        {
            if (numbers[i].ScoringRank != r0)
            {
                allSameRank = false;
            }

            if (numbers[i].suit != s0)
            {
                allSameSuit = false;
            }
        }

        return allSameRank || allSameSuit;
    }

    List<HandCard> BuildBotPlayableChain(PlayerEntity attacker)
    {
        var played = new List<HandCard>();
        if (attacker == null || attacker.Hand.Count == 0)
        {
            return played;
        }

        int cap = Mathf.Clamp(rulesetData?.maxCardsPerAttack ?? 2, 1, 5);
        var byRank = new Dictionary<int, List<HandCard>>();
        var bySuit = new Dictionary<CardSuit, List<HandCard>>();
        var specials = new List<HandCard>();

        for (int i = 0; i < attacker.Hand.Count; i++)
        {
            HandCard c = attacker.Hand[i];
            if (c.IsSuitedNumber)
            {
                if (!byRank.TryGetValue(c.ScoringRank, out List<HandCard> rg))
                {
                    rg = new List<HandCard>();
                    byRank[c.ScoringRank] = rg;
                }

                rg.Add(c);

                if (!bySuit.TryGetValue(c.suit, out List<HandCard> sg))
                {
                    sg = new List<HandCard>();
                    bySuit[c.suit] = sg;
                }

                sg.Add(c);
            }
            else
            {
                specials.Add(c);
            }
        }

        bool missCombo = RNGService.Instance.Value() < 0.40f;

        List<HandCard> bestNumbers = null;
        if (!missCombo)
        {
            foreach (List<HandCard> g in byRank.Values)
            {
                if (bestNumbers == null || g.Count > bestNumbers.Count)
                    bestNumbers = g;
            }

            foreach (List<HandCard> g in bySuit.Values)
            {
                if (bestNumbers == null || g.Count > bestNumbers.Count)
                    bestNumbers = g;
            }
        }

        if (bestNumbers != null)
        {
            for (int i = 0; i < bestNumbers.Count && played.Count < cap; i++)
            {
                played.Add(bestNumbers[i]);
            }
        }

        for (int i = 0; i < specials.Count && played.Count < cap; i++)
        {
            played.Add(specials[i]);
        }

        if (played.Count == 0)
        {
            played.Add(attacker.Hand[attacker.Hand.Count - 1]);
        }

        if (!NumberSubsetHomogeneous(played))
        {
            played.Clear();
            played.Add(attacker.Hand[attacker.Hand.Count - 1]);
        }

        for (int i = 0; i < played.Count; i++)
        {
            for (int h = attacker.Hand.Count - 1; h >= 0; h--)
            {
                if (attacker.Hand[h] == played[i])
                {
                    attacker.Hand.RemoveAt(h);
                    break;
                }
            }
        }

        return played;
    }

    bool ResolveStrike(int attackerSeat, List<HandCard> playedOverride)
    {
        PlayerEntity attacker = roster[attackerSeat];
        deck?.TopUp(attacker, rulesetData, CardDrawAllowed);

        if (deck != null && deck.RemainingCount == 0)
        {
            attacker.SpadeBonusNextStrike = RNGService.Instance.Value() < 0.3f;
        }

        int targetSeat = BotAI.PickTargetSeat(attackerSeat, roster);
        if (targetSeat < 0)
        {
            return false;
        }

        PlayerEntity defender = roster[targetSeat];

        List<HandCard> played = playedOverride;
        if (played == null || played.Count == 0)
        {
            int k = Mathf.Min(Mathf.Clamp(rulesetData?.maxCardsPerAttack ?? 2, 1, 5), Mathf.Max(attacker.Hand.Count, 0));
            played = PullTopHandCards(attacker, k);
        }

        if (played == null || played.Count == 0)
        {
            return false;
        }

        StrikeResolution.Execute(this, attackerSeat, targetSeat, played);

        attacker.SpadeBonusNextStrike = RNGService.Instance.Value() < 0.05f;

        GameEvents.RaiseCardDiscarded(attackerSeat);

        deck?.TopUp(attacker, rulesetData, CardDrawAllowed);

        if (EliminationWinnerGate())
        {
            return true;
        }

        return true;
    }

    static List<HandCard> PullTopHandCards(PlayerEntity seat, int k)
    {
        var chunk = new List<HandCard>();
        if (k <= 0 || seat == null || seat.Hand.Count == 0)
        {
            return chunk;
        }

        k = Mathf.Min(k, seat.Hand.Count);

        for (int i = 0; i < k; i++)
        {
            int rear = seat.Hand.Count - 1;
            chunk.Insert(0, seat.Hand[rear]);
            seat.Hand.RemoveAt(rear);
        }

        return chunk;
    }

    void AdvanceSeat()
    {
        if (!MatchInProgress())
            return;

        int next = FindNextLivingSeat(curr_player_turn);
        if (next == -1)
            return;

        bool wrappedTable = next <= curr_player_turn;
        curr_player_turn = next;
        PlayerEntity active = roster[curr_player_turn];
        deck?.TopUp(active, rulesetData, CardDrawAllowed);

        if (!wrappedTable)
        {
            GameEvents.RaiseTurnChanged(curr_player_turn);
            return;
        }

        strikesThisRound = 0;

        TickGlobalDamagePerRound();
        TickDiamondShieldRounds();

        matchRound++;
        GameEvents.RaiseRoundChanged(matchRound);

        if (rulesetData != null && matchRound > rulesetData.maxRounds)
        {
            int forced = RoundEnd.WinnerByHighestHp(roster);
            if (forced < 0)
            {
                forced = 0;
            }

            Debug.Log($"Round cap hit, winner is seat {forced + 1}.");
            EndMatchForced(forced);
            GameEvents.RaiseTurnChanged(curr_player_turn);
            return;
        }

        GameEvents.RaiseTurnChanged(curr_player_turn);
    }

    bool EliminationWinnerGate()
    {
        if (matchConcluded)
        {
            return true;
        }

        if (TryResolveWinner(out int winnerSeat))
        {
            EndMatchForced(winnerSeat);
            return true;
        }

        if (LivingCount() == 0 && roster.Count > 0)
        {
            Debug.LogWarning("All seats eliminated simultaneously — awarding seat 0 as prototype fallback.");
            EndMatchForced(0);
            return true;
        }

        return false;
    }

    bool MatchInProgress() =>
        !matchConcluded &&
        roster.Count > 0 &&
        LivingCount() > 1;

    int LivingCount()
    {
        int n = 0;
        foreach (PlayerEntity p in roster)
        {
            if (!p.IsEliminated)
                n++;
        }

        return n;
    }

    int FindNextLivingSeat(int fromSeat)
    {
        if (LivingCount() == 0)
            return -1;

        for (int step = 1; step <= roster.Count; step++)
        {
            int idx = (fromSeat + step) % roster.Count;
            if (!roster[idx].IsEliminated)
                return idx;
        }

        return -1;
    }

    bool TryResolveWinner(out int winnerSeat)
    {
        winnerSeat = -1;

        int livingIdx = -1;
        int count = 0;
        for (int i = 0; i < roster.Count; i++)
        {
            if (!roster[i].IsEliminated)
            {
                livingIdx = i;
                count++;
            }
        }

        if (count != 1)
            return false;

        winnerSeat = livingIdx;

        return true;
    }

    void EndMatchForced(int winnerSeat)
    {
        if (winnerSeat < 0)
            winnerSeat = 0;

        matchConcluded = true;
        GameEvents.RaiseGameEnded(winnerSeat);
        UI?.ShowWinningScreen();
        UI?.UpdateWinnerScreen(winnerSeat);
    }

    void TickGlobalDamagePerRound()
    {
        if (GlobalDprRoundsRemaining <= 0 || rulesetData == null)
        {
            return;
        }

        int dmg = rulesetData.dprTickDamage;
        foreach (PlayerEntity p in roster)
        {
            if (p.IsEliminated)
            {
                continue;
            }

            p.ApplyDamage(dmg, false);
            GameEvents.RaisePlayerDamaged(p.SeatIndex, dmg);
            if (p.IsEliminated)
            {
                GameEvents.RaisePlayerEliminated(p.SeatIndex);
            }
        }

        GlobalDprRoundsRemaining--;
    }

    void TickDiamondShieldRounds()
    {
        foreach (PlayerEntity p in roster)
        {
            if (p.Buffs.DiamondShieldRoundsRemaining <= 0)
            {
                continue;
            }

            p.Buffs.DiamondShieldRoundsRemaining--;
            if (p.Buffs.DiamondShieldRoundsRemaining <= 0)
            {
                p.Buffs.DiamondShieldHp = 0;
            }
        }
    }
}
