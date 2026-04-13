using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum States
{
    Initalize,
    Paused,
    PlayerTurn,
    HouseTurn,
    EvalPhase
}
public enum Effects
{
    None = -1,
    EvenNumsDoubled,
    //EvenNumsHalved,
    OddNumsDoubled,
    //OddNumsHalved,
    Plus5ToAll,
    Minus5ToAll,
    RoundCard,
    DoubleAll,
    HalfAll,
    FlipSign,
    SwapToRight,
    Plus10ToAll,
    DoublePositive,
    Plus2ForAllPosCards,
    MultiplyByNumRounds,
}

[System.Serializable]
public class NumberCard
{
    public Sprite card;
    public int card_value;
}

[System.Serializable]
public class EffectCard
{
    public Sprite card;
    public Effects card_effect;
}

public class GameController : MonoBehaviour
{
    private const int MinSupportedPlayers = 2;
    private const int MaxSupportedPlayers = 4;
   
    public States game_state = States.Paused;
    public UiController UI;
    public int curr_player_turn = -1;

    public List<NumberCard> numberCards;
    public List<EffectCard> effectCards;

    public RulesetData rulesetData;
    private readonly List<PlayerEntity> runtimePlayers = new List<PlayerEntity>();

    //Refs
    UiController UI_C;

    // Game Settings
    public int playerCount = 1;
    public int round = 1;

    void Start()
    {
        UI_C = GameObject.Find("Canvas").GetComponent<UiController>();
        rulesetData = Resources.Load<RulesetData>("Rules/DefaultRulesetData");
        if (rulesetData == null)
        {
            rulesetData = ScriptableObject.CreateInstance<RulesetData>();
            Debug.LogWarning("Default rules asset missing. Using in-memory RulesetData fallback.");
        }
    }

    public void StartGame(int players)
    {
        if (players < MinSupportedPlayers || players > MaxSupportedPlayers)
        {
            Debug.LogError($"StartGame rejected invalid player count: {players}. Supported range is {MinSupportedPlayers}-{MaxSupportedPlayers}.");
            return;
        }

        playerCount = players;
        runtimePlayers.Clear();
        GameEvents.RaiseGameStarted(players);

        for (int i = 0; i < players; i++)
        {
            PlayerEntity entity = new PlayerEntity
            {
                PlayerID = i
            };

            entity.AddNumberCard(DrawNumberCard());
            entity.AddEffectCard(DrawEffectCard());
            runtimePlayers.Add(entity);
        }

        curr_player_turn = 0;
        GameEvents.RaiseTurnChanged(curr_player_turn);
        UI.InitPlayerUI();
    }

    NumberCard DrawNumberCard()
    {
        int randomNumber = RNGService.Instance.Range(-10, 11);

        if (randomNumber == 0 || randomNumber == 1 || randomNumber == -1)
            randomNumber = 10;

        foreach (NumberCard n in numberCards)
        {
            if (n.card_value == randomNumber)
                return n;
        }

        Debug.Log("NULL DRAWING: " + randomNumber);
        return null;
    }

    EffectCard DrawEffectCard()
    {
        int rand = RNGService.Instance.Range(0, 12);
        Effects randomEffect = (Effects) rand;
        //Effects randomEffect = Effects.DoubleAll;

        if (rand == 0)
            randomEffect = Effects.DoubleAll;

        foreach (EffectCard e in effectCards)
        {
            if (e.card_effect == randomEffect)
            {
                return e;
            }
        }

        Debug.Log("NULL DRAWING: " + rand);
        return null;
    }

    public void EndCurrentTurn()
    {
        runtimePlayers[curr_player_turn].Standing = true;
        MoveToNextPlayersTurn();
    }

    public void DrawAndCommitCurrentTurnAction()
    {
        PlayerEntity current_player = runtimePlayers[curr_player_turn];
        NumberCard nc = DrawNumberCard();

        current_player.AddNumberCard(nc);

        GameObject.Find("Canvas").GetComponent<UiController>().AddVisualCardToDeck(nc);
        GameEvents.RaiseCardDrawn(curr_player_turn);

        MoveToNextPlayersTurn();
    }

    public void PlayEffectCard(int playerID, Effects current_effect)
    {
        if (playerID < 0 || playerID >= runtimePlayers.Count)
        {
            Debug.LogWarning($"Invalid effect target player id: {playerID}");
            return;
        }

        // Cards that effect Everyone
        if (current_effect == Effects.Minus5ToAll || current_effect == Effects.Plus5ToAll)
        {
            for (int i = 0; i < playerCount; i++)
            {
                runtimePlayers[i].ApplyCardEffect(current_effect, round);
            }
        }
        else
        {
            runtimePlayers[playerID].ApplyCardEffect(current_effect, round);
        }
    }

    public void MoveToNextPlayersTurn()
    {
        Debug.Log($"[TURN] Current index: {curr_player_turn}");

        int nextPlayer = -1;

        // Start from next index
        for (int i = curr_player_turn + 1; i < playerCount; i++)
        {
            PlayerEntity pc = runtimePlayers[i];

            Debug.Log($"[CHECK] Player {i} | Standing: {pc.Standing} | Over30: {pc.Over30}");

            if (!pc.Standing && !pc.Over30)
            {
                nextPlayer = i;
                break;
            }
        }

        // If found next player in list → go to them
        if (nextPlayer != -1)
        {
            curr_player_turn = nextPlayer;
            GameEvents.RaiseTurnChanged(curr_player_turn);

            Debug.Log($"[TURN] Moving to Player {nextPlayer}");

            StartCoroutine(GoToNextTurn());
            return;
        }

        // If we reached here → we hit end of player list OR no valid players left ahead
        Debug.Log("[END OF LINE] Reached last player → Dealer Turn");

        DealersTurn();
    }

    public List<NumberCard> GetPlayerNumberCards(int playerID)
    { 
        foreach (PlayerEntity player in runtimePlayers)
        {
            if (player.PlayerID == playerID)
            {
                return player.NumberCards;
            }
        }

        return null;
    }

    public EffectCard GetPlayerEffectCard(int playerID)
    {
        foreach (PlayerEntity player in runtimePlayers)
        {
            if (player.PlayerID == playerID)
            {
                return player.EffectCard;
            }
        }

        return null;
    }

    public int GetPlayerTotal(int playerID)
    {
        if (playerID < 0 || playerID >= runtimePlayers.Count)
        {
            return 0;
        }

        return runtimePlayers[playerID].CurrentTotal;
    }

    public string GetPlayerCardSequence(int playerID)
    {
        if (playerID < 0 || playerID >= runtimePlayers.Count)
        {
            return string.Empty;
        }

        return runtimePlayers[playerID].CardSequence;
    }

    public void DealersTurn()
    {
        int nextPlayer = -1;
        for (int i = 0; i < playerCount; i++)
        {
            if (!runtimePlayers[i].Standing && !runtimePlayers[i].Over30)
            {
                nextPlayer = i;
                break;
            }
        }

        // GAME OVER
        if (nextPlayer == -1)
        {
            Debug.Log("GAME OVER!");
            GameOver();
            return;
        }

        // Draw effect card
        EffectCard drawnCard = DrawEffectCard();

        // Update UI
        UI_C.UpdateDealersCard(drawnCard);

        // Apply effect to everyone
        for (int i = 0; i < playerCount; i++)
        {
            PlayerEntity playCont = runtimePlayers[i];
            if (!playCont.Standing && !playCont.Over30)
            {
                playCont.ApplyCardEffect(drawnCard.card_effect, round);
            }
        }

        for (int i = 0; i < playerCount; i++)
        {
            if (runtimePlayers[i].CurrentTotal > 30)
            {
                runtimePlayers[i].Over30 = true;
                break;
            }
        }


        // Move on
        for (int i = 0; i < playerCount; i++)
        {
            if (!runtimePlayers[i].Standing && !runtimePlayers[i].Over30)
            {
                curr_player_turn = i;
                break;
            }
        }

        round++;
        GameEvents.RaiseRoundChanged(round);
        StartCoroutine(GoToNextTurn());
    }

    IEnumerator GoToNextTurn()
    {
        yield return new WaitForSeconds(0.5f);

        UI_C.InitPlayerUI();
    }

    void GameOver()
    {
        List<PlayerEntity> winning_players = new List<PlayerEntity>();
        int target = int.MinValue;

        for (int i = 0; i < playerCount; i++)
        {
            PlayerEntity player = runtimePlayers[i];

            if (winning_players.Count == 0)
            {
                Debug.Log("Adding Winner: " + player.PlayerID);
                winning_players.Add(player);
                target = player.CurrentTotal;
            }
            else
            {
                if (player.CurrentTotal > target)
                {
                    winning_players.Clear();
                    winning_players.Add(player);
                    Debug.Log("Adding Winner: " + player.PlayerID);
                }
                else if (player.CurrentTotal == target)
                {
                    winning_players.Add(player);
                }
            }
        }

        Debug.Log("Winners: ");

        foreach (PlayerEntity player in winning_players)
            Debug.Log("Player " + player.PlayerID);

        UI_C.ShowWinningScreen();
        UI_C.UpdateWinnerScreen(winning_players[0].PlayerID);
        GameEvents.RaiseGameEnded(winning_players[0].PlayerID);
    }
}
