using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

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
   
    public States game_state = States.Paused;
    public UiController UI;
    public int curr_player_turn = -1;

    public List<NumberCard> numberCards;
    public List<EffectCard> effectCards;

    public GameObject PlayerPrefab;

    public GameObject[] playersList;

    //Refs
    UiController UI_C;

    // Game Settings
    public int playerCount = 1;
    public int round = 1;

    void Start()
    {
        UI_C = GameObject.Find("Canvas").GetComponent<UiController>();
    }

    public void StartGame(int players)
    {
        playersList = new GameObject[players];

        playerCount = players;
        for (int i = 0; i < players; i++)
        {
            GameObject new_player = Instantiate(PlayerPrefab);
            PlayerController cont = new_player.GetComponent<PlayerController>();

            cont.PlayerID = i;
            cont.AddNumberCard(DrawNumberCard());
            cont.AddEffectCard(DrawEffectCard());

            playersList[i] = new_player;
        }

        curr_player_turn = 0;
        UI.InitPlayerUI();
    }

    NumberCard DrawNumberCard()
    {
        int randomNumber = Random.Range(-10, 11);

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
        int rand = Random.Range(0, 12);
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

    public void MarkCurrPlayerAsStanding()
    {
        playersList[curr_player_turn].GetComponent<PlayerController>().standing = true;
        MoveToNextPlayersTurn();
    }

    public void HitCurrPlayer()
    {
        PlayerController current_player = playersList[curr_player_turn].GetComponent<PlayerController>();
        NumberCard nc = DrawNumberCard();

        current_player.AddNumberCard(nc);

        if (current_player.curr_total > 30)
            current_player.over30 = true;

        GameObject.Find("Canvas").GetComponent<UiController>().AddVisualCardToDeck(nc);

        MoveToNextPlayersTurn();
    }

    public void PlayEffectCard(int playerID, Effects current_effect)
    {
        // Cards that effect Everyone
        if (current_effect == Effects.Minus5ToAll || current_effect == Effects.Plus5ToAll)
            for (int i = 0; i < playerCount - 1; i++)
                playersList[i].GetComponent<PlayerController>().ApplyCardEffect(current_effect);
        else
            playersList[playerID].GetComponent<PlayerController>().ApplyCardEffect(current_effect);
    }

    public void MoveToNextPlayersTurn()
    {
        Debug.Log($"[TURN] Current index: {curr_player_turn}");

        int nextPlayer = -1;

        // Start from next index
        for (int i = curr_player_turn + 1; i < playerCount; i++)
        {
            PlayerController pc = playersList[i].GetComponent<PlayerController>();

            Debug.Log($"[CHECK] Player {i} | Standing: {pc.standing} | Over30: {pc.over30}");

            if (!pc.standing && !pc.over30)
            {
                nextPlayer = i;
                break;
            }
        }

        // If found next player in list → go to them
        if (nextPlayer != -1)
        {
            curr_player_turn = nextPlayer;

            Debug.Log($"[TURN] Moving to Player {nextPlayer}");

            StartCoroutine(GoToNextTurn());
            return;
        }

        // If we reached here → we hit end of player list OR no valid players left ahead
        Debug.Log("[END OF LINE] Reached last player → Dealer Turn");

        DealersTurn();
    }

    //public void MoveToNextPlayersTurn()
    //{
    //    if (curr_player_turn == playerCount - 1)
    //    {
    //        if (playersList[curr_player_turn].GetComponent<PlayerController>().over30 || playersList[curr_player_turn].GetComponent<PlayerController>().standing)
    //        {
    //            GameOver();
    //            return;
    //        }

    //        DealersTurn();
    //        return;
    //    }

    //    bool foundNextTurn = false;

    //    for (int i = curr_player_turn + 1; i < playerCount; i++)
    //    {
    //        PlayerController pc = playersList[i].GetComponent<PlayerController>();

    //        if (!pc.standing && !pc.over30)
    //        {
    //            curr_player_turn = i;
    //            foundNextTurn = true;
    //            break;
    //        }
    //    }

    //    if (!foundNextTurn)
    //    {
    //        PlayerController pc = playersList[curr_player_turn].GetComponent<PlayerController>();

    //        if (pc.standing || pc.over30)
    //        {
    //            GameOver();
    //        }
    //        else
    //        {
    //            StartCoroutine(GoToNextTurn());
    //        }
    //    }
    //    else
    //    {
    //        StartCoroutine(GoToNextTurn());
    //    }
    //}

    public List<NumberCard> GetPlayerNumberCards(int playerID)
    { 
        foreach (GameObject pc in playersList)
        {
            if (pc.GetComponent<PlayerController>().PlayerID == playerID)
            {
                return pc.GetComponent<PlayerController>().number_cards;
            }
        }

        return null;
    }

    public EffectCard GetPlayerEffectCard(int playerID)
    {
        foreach (GameObject pc in playersList)
        {
            if (pc.GetComponent<PlayerController>().PlayerID == playerID)
            {
                return pc.GetComponent<PlayerController>().effect_card;
            }
        }

        return null;
    }

    public void DealersTurn()
    {
        int nextPlayer = -1;
        for (int i = 0; i < playerCount; i++)
        {
            if (!playersList[i].GetComponent<PlayerController>().standing && !playersList[i].GetComponent<PlayerController>().over30)
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
        foreach (GameObject pc in playersList)
        {
            PlayerController playCont = pc.GetComponent<PlayerController>();

            if (!playCont.standing && !playCont.over30)
            {
                playCont.ApplyCardEffect(drawnCard.card_effect);
            }
        }

        for (int i = 0; i < playerCount; i++)
        {
            if (playersList[i].GetComponent<PlayerController>().curr_total > 30)
            {
                playersList[i].GetComponent<PlayerController>().over30 = true;
                break;
            }
        }


        // Move on
        for (int i = 0; i < playerCount; i++)
        {
            if (!playersList[i].GetComponent<PlayerController>().standing && !playersList[i].GetComponent<PlayerController>().over30)
            {
                curr_player_turn = i;
                break;
            }
        }

        round++;
        StartCoroutine(GoToNextTurn());
    }

    IEnumerator GoToNextTurn()
    {
        yield return new WaitForSeconds(0.5f);

        UI_C.InitPlayerUI();
    }

    void GameOver()
    {
        List<PlayerController> winning_players = new List<PlayerController>();
        int target = int.MinValue;

        for (int i = 0; i < playerCount; i++)
        {
            PlayerController player = playersList[i].GetComponent<PlayerController>();

            if (winning_players.Count == 0)
            {
                Debug.Log("Adding Winner: " + player.PlayerID);
                winning_players.Add(player);
                target = player.curr_total;
            }
            else
            {
                if (player.curr_total > target)
                {
                    winning_players.Clear();
                    winning_players.Add(player);
                    Debug.Log("Adding Winner: " + player.PlayerID);
                }
                else if (player.curr_total == target)
                {
                    winning_players.Add(player);
                }
            }
        }

        Debug.Log("Winners: ");

        foreach (PlayerController player in winning_players)
            Debug.Log("Player " + player.PlayerID);

        UI_C.ShowWinningScreen();
        UI_C.UpdateWinnerScreen(winning_players[0].PlayerID);
    }
}
