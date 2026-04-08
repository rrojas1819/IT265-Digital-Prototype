using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UiController : MonoBehaviour
{
    public GameController Controller;
    public GameObject start_menu;
    public GameObject game_settings;
    public GameObject HUD;
    public GameObject game_over;
    public TMP_InputField Player_Count_Input;
    public GameObject UICardprefab;
    public GameObject OppInput;

    int card_total = 0;
    Effects curr_effect = Effects.None;

    void Start()
    {
        start_menu.SetActive(true);
        game_settings.SetActive(false);
        HUD.SetActive(false);
        game_over.SetActive(false);
    }

    public void OnPlayClick()
    {
        start_menu.SetActive(false);
        game_settings.SetActive(true);
    }

    public void OnGameSettingsClick()
    {
        game_settings.SetActive(false);
        int player_count = int.Parse(Player_Count_Input.text);

        if (player_count <= 0)
        {
            Debug.Log("PlayerCount <= 0");
            return;
        }

        Controller.StartGame(player_count);
    }

    public void InitPlayerUI()
    {
        curr_effect = Effects.None;
        card_total = 0;

        HUD.SetActive(true);
        HUD.transform.Find("Player Text").GetComponent<TMP_Text>().text = "Player " + (Controller.curr_player_turn).ToString() + "'s Turn";

        //Clear all cards
        foreach (Transform child in HUD.transform.Find("Player Cards").transform)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in HUD.transform.Find("Player Effect Card").transform)
        {
            Destroy(child.gameObject);
        }

        // Add all number cards
        foreach (NumberCard n in Controller.GetPlayerNumberCards(Controller.curr_player_turn))
        {
            GameObject card = Instantiate(UICardprefab);
            card.GetComponent<CardController>().card_sprite = n.card;
            card.GetComponent<CardController>().card_number = n.card_value;
            card.transform.SetParent(HUD.transform.Find("Player Cards").transform);
        }

        // Count Total
        card_total = Controller.playersList[Controller.curr_player_turn].GetComponent<PlayerController>().curr_total;
        HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();

        HUD.transform.Find("Card Seq").GetComponent<TMP_Text>().text = Controller.playersList[Controller.curr_player_turn].GetComponent<PlayerController>().card_seq;


        // Set Effect Card
        GameObject eCard = Instantiate(UICardprefab);
        EffectCard PEC = Controller.GetPlayerEffectCard(Controller.curr_player_turn);
        eCard.GetComponent<CardController>().card_sprite = PEC.card;
        eCard.transform.SetParent(HUD.transform.Find("Player Effect Card").transform);

        curr_effect = PEC.card_effect;
    }

    public void UpdateDealersCard(EffectCard ec)
    {
        GameObject card = Instantiate(UICardprefab);
        card.GetComponent<CardController>().card_sprite = ec.card;

        card.transform.SetParent(HUD.transform.Find("Dealer Cards").transform);
    }

    public void AddVisualCardToDeck(NumberCard nc)
    {
        GameObject card = Instantiate(UICardprefab);
        card.GetComponent<CardController>().card_sprite = nc.card;

        card.transform.SetParent(HUD.transform.Find("Player Cards").transform);

        card_total = Controller.playersList[Controller.curr_player_turn].GetComponent<PlayerController>().curr_total;

        HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();
    }

    public void UpdatePlayerTotal()
    {
        //card_total = Controller.playersList[Controller.curr_player_turn].GetComponent<PlayerController>().curr_total;
        //HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();
    }


    public void HitButtonClick()
    {
        Controller.HitCurrPlayer();
    }

    public void StandButtonClick()
    {
        Controller.MarkCurrPlayerAsStanding();
    }

    public void PlayEffectButtunCLick()
    {
        int playerNum;

        if (!int.TryParse(OppInput.GetComponent<TMP_InputField>().text, out playerNum))
        {
            playerNum = Controller.curr_player_turn;
        }
 
        Controller.PlayEffectCard(playerNum, curr_effect);

        InitPlayerUI();
    }

    public void ShowWinningScreen()
    {
        start_menu.SetActive(false);
        game_settings.SetActive(false);
        HUD.SetActive(false);
        game_over.SetActive(true);
    }

    public void OnPlayAgainClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UpdateWinnerScreen(int winner)
    {
        game_over.transform.Find("Winner Text").GetComponent<TMP_Text>().text = "Player " + winner + " Won!";
    }

    public void OnQuitClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


}
