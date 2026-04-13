using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UiController : MonoBehaviour
{
    private const int MinSupportedPlayers = 2;
    private const int MaxSupportedPlayers = 4;

    public GameController Controller;
    public GameObject start_menu;
    public GameObject game_settings;
    public GameObject HUD;
    public GameObject game_over;
    public GameObject UICardprefab;
    public GameObject OppInput;
    public Transform tableRoot;

    int card_total = 0;
    Effects curr_effect = Effects.None;

    void Start()
    {
        start_menu.SetActive(true);
        game_settings.SetActive(false);
        HUD.SetActive(false);
        game_over.SetActive(false);

        // Keep table seats hidden until a valid player count is selected.
        SetSeatVisibility(0);
    }

    public void OnPlayClick()
    {
        start_menu.SetActive(false);
        game_settings.SetActive(true);
    }

    public void OnOpponentCountSelected(int selectedCount)
    {
        int playerCount = NormalizePlayerCount(selectedCount);
        if (playerCount < MinSupportedPlayers || playerCount > MaxSupportedPlayers)
        {
            Debug.LogWarning($"Invalid player count selected: {selectedCount}");
            return;
        }

        if (!ValidateRequiredSeats(playerCount))
        {
            Debug.LogError("Cannot start game because seat setup is invalid.");
            return;
        }

        SetSeatVisibility(playerCount);
        game_settings.SetActive(false);
        Controller.StartGame(playerCount);
    }

    private int NormalizePlayerCount(int selectedCount)
    {
        if (selectedCount >= MinSupportedPlayers && selectedCount <= MaxSupportedPlayers)
        {
            return selectedCount;
        }

        // Backward-compatible fallback if old button bindings still pass opponent count.
        int opponentToPlayerCount = selectedCount + 1;
        if (opponentToPlayerCount >= MinSupportedPlayers && opponentToPlayerCount <= MaxSupportedPlayers)
        {
            return opponentToPlayerCount;
        }

        return selectedCount;
    }

    private bool ValidateRequiredSeats(int playerCount)
    {
        Transform resolvedTableRoot = GetTableRoot();
        if (resolvedTableRoot == null)
        {
            Debug.LogError("TableRoot was not found. Expected Canvas/TableRoot.");
            return false;
        }

        for (int i = 1; i <= playerCount; i++)
        {
            if (resolvedTableRoot.Find($"Seat_{i}") == null)
            {
                Debug.LogError($"Missing required seat object: Seat_{i}");
                return false;
            }
        }

        return true;
    }

    private void SetSeatVisibility(int activeSeatCount)
    {
        Transform resolvedTableRoot = GetTableRoot();
        if (resolvedTableRoot == null)
        {
            return;
        }

        for (int i = 1; i <= MaxSupportedPlayers; i++)
        {
            Transform seat = resolvedTableRoot.Find($"Seat_{i}");
            if (seat != null)
            {
                seat.gameObject.SetActive(i <= activeSeatCount);
            }
        }
    }

    private Transform GetTableRoot()
    {
        if (tableRoot != null)
        {
            return tableRoot;
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            return null;
        }

        Transform resolved = canvas.transform.Find("TableRoot");
        if (resolved != null)
        {
            tableRoot = resolved;
        }

        return resolved;
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
        card_total = Controller.GetPlayerTotal(Controller.curr_player_turn);
        HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();

        HUD.transform.Find("Card Seq").GetComponent<TMP_Text>().text = Controller.GetPlayerCardSequence(Controller.curr_player_turn);


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

        card_total = Controller.GetPlayerTotal(Controller.curr_player_turn);

        HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();
    }

    public void UpdatePlayerTotal()
    {
        //card_total = Controller.GetPlayerTotal(Controller.curr_player_turn);
        //HUD.transform.Find("Player Total").GetComponent<TMP_Text>().text = card_total.ToString();
    }


    public void OnCommitActionClick()
    {
        Controller.DrawAndCommitCurrentTurnAction();
    }

    public void OnEndTurnClick()
    {
        Controller.EndCurrentTurn();
    }

    public void OnPlayEffectButtonClick()
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
