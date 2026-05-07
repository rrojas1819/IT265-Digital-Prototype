using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
    private const int MinSupportedPlayers = 2;
    private const int MaxSupportedPlayers = 4;
    private const float BuffTableRootPadding = 24f;

    public GameController Controller;
    public GameObject start_menu;
    public GameObject game_settings;
    public GameObject HUD;
    public GameObject game_over;
    public GameObject UICardprefab;
    public GameObject OppInput;
    public Transform tableRoot;

    public TMP_Text matchStatusText;

    public GameObject seatViewTemplatePrefab;

    [Tooltip("Optional; if null, loads Resources/Visuals/DefaultCardVisualCatalog.")]
    public CardVisualCatalog cardVisualCatalog;

    [SerializeField]
    [Tooltip("Logs [Sel] lines for card clicks and chain checks — disable for builds.")]
    bool debugSelection;

    public bool DebugSelection => debugSelection;

    int pendingTotalPlayers;
    bool _pendingHotSeatMode;
    Button _modeToggleBtn;
    TMP_Text _modeToggleLabel;
    readonly List<CardController> selectedCards = new List<CardController>();

    List<int> pendingHandDealUids = new List<int>();
    GameObject cardZoomPreviewObject;
    Image cardZoomPreviewImage;
    TMP_Text cardZoomTitle;
    TMP_Text cardZoomBody;

    void Awake()
    {
        FixCanvasScaleIfCorrupted();
        EnsureTableRootHierarchyExists();
        EnsureBuffButtonExists();
        if (matchStatusText != null)
        {
            EnsureStatusBannerDoesNotEatClicks();
        }
    }

    void FixCanvasScaleIfCorrupted()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && rt.localScale.sqrMagnitude < 1e-6f)
        {
            rt.localScale = Vector3.one;
        }
    }

    void Start()
    {
        start_menu.SetActive(true);
        game_settings.SetActive(false);
        if (HUD != null)
        {
            HUD.SetActive(false);
        }

        if (game_over != null)
        {
            game_over.SetActive(false);
        }

        SetTableRootActive(false);
        SetSeatVisibility(0);

        Transform zoomTf = transform.Find("CardZoomPreview");
        if (zoomTf != null)
        {
            cardZoomPreviewObject = zoomTf.gameObject;
            cardZoomPreviewImage = zoomTf.GetComponent<Image>();
            EnsureZoomPreviewTexts(zoomTf as RectTransform);
            cardZoomPreviewObject.SetActive(false);
        }

        if (cardVisualCatalog == null)
        {
            cardVisualCatalog = Resources.Load<CardVisualCatalog>("Visuals/DefaultCardVisualCatalog");
        }

        if (cardVisualCatalog == null)
        {
            Debug.LogWarning(
                "[UiController] CardVisualCatalog not assigned and Resources.Load failed. " +
                "Assign DefaultCardVisualCatalog on the Canvas UiController, or run menu: ChaosPoker/Repair Card Visual Catalog.");
        }
    }

    void EnsureZoomPreviewTexts(RectTransform zoomRt)
    {
        if (zoomRt == null)
        {
            return;
        }

        Transform tTitle = zoomRt.Find("CardZoomTitle");
        if (tTitle != null)
        {
            cardZoomTitle = tTitle.GetComponent<TMP_Text>();
            tTitle.gameObject.SetActive(false);
        }

        Transform tBody = zoomRt.Find("CardZoomBody");
        if (tBody != null)
        {
            cardZoomBody = tBody.GetComponent<TMP_Text>();
            tBody.gameObject.SetActive(false);
        }

        if (cardZoomPreviewImage != null)
        {
            cardZoomPreviewImage.preserveAspect = true;
            cardZoomPreviewImage.raycastTarget = false;
            cardZoomPreviewImage.color = Color.white;
        }
    }

    public void OnPlayClick()
    {
        start_menu.SetActive(false);
        game_settings.SetActive(true);
        EnsureModeToggleExists();
    }

    void EnsureModeToggleExists()
    {
        if (game_settings == null || _modeToggleBtn != null)
            return;

        Sprite btnSprite = null;
        Color btnBgColor = Color.white;
        TMP_FontAsset btnFont = null;
        Color btnTextColor = new Color(1f, 0.784f, 0f, 1f);
        float btnFontSize = 65f;

        if (start_menu != null)
        {
            foreach (Button b in start_menu.GetComponentsInChildren<Button>(true))
            {
                if (b.gameObject.name == "Play_BTN")
                {
                    Image playImg = b.GetComponent<Image>();
                    if (playImg != null)
                    {
                        btnSprite = playImg.sprite;
                        btnBgColor = playImg.color;
                    }
                    TMP_Text playTxt = b.GetComponentInChildren<TMP_Text>(true);
                    if (playTxt != null)
                    {
                        btnFont = playTxt.font;
                        btnTextColor = playTxt.color;
                        btnFontSize = playTxt.fontSize;
                    }
                    break;
                }
            }
        }

        GameObject go = new GameObject("ModeToggleBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(game_settings.transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500f, 80f);
        rt.anchoredPosition = new Vector2(0f, -120f);

        Image img = go.GetComponent<Image>();
        img.sprite = btnSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0f, 0f, 0f, 0f);

        _modeToggleBtn = go.GetComponent<Button>();
        _modeToggleBtn.targetGraphic = img;
        _modeToggleBtn.onClick.AddListener(OnModeToggleClick);

        GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        _modeToggleLabel = labelGo.GetComponent<TextMeshProUGUI>();
        _modeToggleLabel.raycastTarget = false;
        _modeToggleLabel.alignment = TextAlignmentOptions.Center;
        _modeToggleLabel.fontSize = btnFontSize;
        _modeToggleLabel.color = btnTextColor;
        if (btnFont != null)
            _modeToggleLabel.font = btnFont;
        UpdateModeToggleLabel();
    }

    void OnModeToggleClick()
    {
        _pendingHotSeatMode = !_pendingHotSeatMode;
        UpdateModeToggleLabel();
    }

    void UpdateModeToggleLabel()
    {
        if (_modeToggleLabel != null)
            _modeToggleLabel.text = _pendingHotSeatMode ? "Mode: Pass & Play" : "Mode: vs Bots";
    }

    public void OnPlayerCountPreview(int totalPlayers)
    {
        if (totalPlayers < MinSupportedPlayers || totalPlayers > MaxSupportedPlayers)
        {
            Debug.LogWarning($"Invalid player count: {totalPlayers}. Pick {MinSupportedPlayers}–{MaxSupportedPlayers}.");
            return;
        }

        if (!ValidateRequiredSeats(totalPlayers))
        {
            Debug.LogError("Seat objects missing under TableRoot; cannot preview.");
            return;
        }

        pendingTotalPlayers = totalPlayers;
        SetTableRootActive(true);
        SetSeatVisibility(totalPlayers);
        game_settings.SetActive(false);

        if (Controller == null)
        {
            Debug.LogError("UiController.Controller is not assigned to Game Controller.");
            return;
        }

        Controller.StartGame(totalPlayers, _pendingHotSeatMode);
    }

    public void OnConfirmPlayerCount()
    {
        int n = pendingTotalPlayers;
        if (n < MinSupportedPlayers || n > MaxSupportedPlayers)
        {
            Debug.LogWarning($"Pick 2, 3, or 4 first, then Continue. Defaulting to {MinSupportedPlayers}.");
            n = MinSupportedPlayers;
            if (!ValidateRequiredSeats(n))
            {
                return;
            }

            pendingTotalPlayers = n;
        }

        if (!ValidateRequiredSeats(n))
        {
            return;
        }

        SetTableRootActive(true);
        SetSeatVisibility(n);
        game_settings.SetActive(false);
        if (Controller == null)
        {
            Debug.LogError("UiController.Controller is not assigned to Game Controller.");
            return;
        }

        Controller.StartGame(n, _pendingHotSeatMode);
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
            if (ResolveSeatTransform(resolvedTableRoot, i) == null)
            {
                Debug.LogError($"Missing required seat object Seat_{i} under TableRoot.");
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
            Transform seat = ResolveSeatTransform(resolvedTableRoot, i);
            if (seat != null)
            {
                seat.gameObject.SetActive(i <= activeSeatCount);
            }
        }
    }

    private void SetTableRootActive(bool isActive)
    {
        Transform resolvedTableRoot = GetTableRoot();
        if (resolvedTableRoot == null)
        {
            return;
        }

        resolvedTableRoot.gameObject.SetActive(isActive);
    }

    private void EnsureTableRootHierarchyExists()
    {
        Transform resolvedTableRoot = GetTableRoot();
        if (resolvedTableRoot != null)
        {
            return;
        }

        Transform canvasTf = transform;
        if (canvasTf == null)
        {
            Debug.LogError("Cannot build TableRoot; UiController must live on Canvas.");
            return;
        }

        GameObject tableRootGo = new GameObject("TableRoot", typeof(RectTransform));
        tableRootGo.transform.SetParent(canvasTf, false);
        RectTransform tableRt = tableRootGo.GetComponent<RectTransform>();
        tableRt.anchorMin = Vector2.zero;
        tableRt.anchorMax = Vector2.one;
        tableRt.offsetMin = Vector2.zero;
        tableRt.offsetMax = Vector2.zero;
        tableRt.pivot = new Vector2(0.5f, 0.5f);

        for (int i = 1; i <= MaxSupportedPlayers; i++)
        {
            GameObject seatGo = new GameObject($"Seat_{i}", typeof(RectTransform));
            seatGo.transform.SetParent(tableRootGo.transform, false);
            RectTransform seatRt = seatGo.GetComponent<RectTransform>();
            seatRt.anchorMin = new Vector2(0.1f + (i - 1) * 0.2f, 0.15f);
            seatRt.anchorMax = new Vector2(0.25f + (i - 1) * 0.2f, 0.35f);
            seatRt.offsetMin = Vector2.zero;
            seatRt.offsetMax = Vector2.zero;
            seatRt.pivot = new Vector2(0.5f, 0.5f);

            if (seatViewTemplatePrefab != null)
            {
                GameObject seatView = Instantiate(seatViewTemplatePrefab, seatGo.transform);
                RectTransform seatViewRt = seatView.GetComponent<RectTransform>();
                if (seatViewRt != null)
                {
                    seatViewRt.anchorMin = Vector2.zero;
                    seatViewRt.anchorMax = Vector2.one;
                    seatViewRt.offsetMin = Vector2.zero;
                    seatViewRt.offsetMax = Vector2.zero;
                }
            }
            else
            {
                GameObject labelGo = new GameObject("SeatLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(seatGo.transform, false);
                RectTransform labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
                label.text = $"Seat {i}";
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 28;
            }
        }

        tableRoot = tableRootGo.transform;
    }

    private static Transform ResolveSeatTransform(Transform tableRootTransform, int seatIndexOneBased)
    {
        if (tableRootTransform == null || seatIndexOneBased < 1 || seatIndexOneBased > MaxSupportedPlayers)
        {
            return null;
        }

        return tableRootTransform.Find($"Seat_{seatIndexOneBased}");
    }

    private Transform GetTableRoot()
    {
        if (tableRoot != null)
        {
            return tableRoot;
        }

        Transform canvasRt = transform;
        for (int i = 0; i < canvasRt.childCount; i++)
        {
            Transform child = canvasRt.GetChild(i);
            if (child.name == "TableRoot")
            {
                tableRoot = child;
                return child;
            }
        }

        return null;
    }

    void PruneDestroyedCardSelections()
    {
        for (int i = selectedCards.Count - 1; i >= 0; i--)
        {
            if (selectedCards[i] == null)
            {
                selectedCards.RemoveAt(i);
            }
        }
    }

    static int PendingHandKey(in HandCard h) => h.dealUid != 0 ? h.dealUid : h.instanceId;

    void UnregisterPendingHandDealUid(int uid)
    {
        for (int i = pendingHandDealUids.Count - 1; i >= 0; i--)
        {
            if (pendingHandDealUids[i] == uid)
            {
                pendingHandDealUids.RemoveAt(i);
                return;
            }
        }
    }

    void TrimPendingHandSelectionsToSeat(PlayerEntity p)
    {
        if (pendingHandDealUids.Count == 0 || p == null || p.Hand == null)
        {
            return;
        }

        var available = new Dictionary<int, int>();
        foreach (HandCard hc in p.Hand)
        {
            int k = PendingHandKey(hc);
            available[k] = available.TryGetValue(k, out int c) ? c + 1 : 1;
        }

        var kept = new List<int>(pendingHandDealUids.Count);
        foreach (int uid in pendingHandDealUids)
        {
            if (available.TryGetValue(uid, out int n) && n > 0)
            {
                kept.Add(uid);
                available[uid] = n - 1;
            }
        }

        pendingHandDealUids.Clear();
        pendingHandDealUids.AddRange(kept);
    }

    void RebindHumanSelectionFromPending(List<CardController> spawnedInDeckOrder)
    {
        if (spawnedInDeckOrder == null || pendingHandDealUids.Count == 0)
        {
            return;
        }

        var used = new HashSet<CardController>();
        foreach (int uid in pendingHandDealUids)
        {
            for (int i = 0; i < spawnedInDeckOrder.Count; i++)
            {
                CardController cc = spawnedInDeckOrder[i];
                if (cc == null || used.Contains(cc))
                {
                    continue;
                }

                if (PendingHandKey(cc.HeldCard) == uid)
                {
                    used.Add(cc);
                    cc.SetSelected(true);
                    selectedCards.Add(cc);
                    break;
                }
            }
        }
    }

    void SyncTableActionLabels()
    {
        Transform root = GetTableRoot();
        if (root == null)
        {
            return;
        }

        foreach (string nm in new[] { "Strike", "EndTurn" })
        {
            Transform t = root.Find(nm);
            if (t == null)
            {
                continue;
            }

            TMP_Text tmp = t.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = "STRIKE";
                tmp.raycastTarget = false;
            }
        }

        Transform buffTf = root.Find("BuffBtn");
        if (buffTf != null)
        {
            TMP_Text btmp = buffTf.GetComponentInChildren<TMP_Text>(true);
            if (btmp != null)
            {
                btmp.text = "BUFF";
                btmp.raycastTarget = false;
            }

            foreach (Text lt in buffTf.GetComponentsInChildren<Text>(true))
            {
                if (lt != null && lt.gameObject.name == "Text")
                {
                    lt.text = "BUFF";
                }
            }
        }
    }

    void EnsureBuffButtonExists()
    {
        Transform root = GetTableRoot();
        if (root == null)
        {
            return;
        }

        Transform buffTf = root.Find("BuffBtn");
        Button btn;
        if (buffTf != null)
        {
            btn = buffTf.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnBuffActivationClick);
                btn.onClick.AddListener(OnBuffActivationClick);
            }

            PositionBuffBottomLeftInTableRoot();
            UpdateBuffButtonPresentation(Controller);
            return;
        }

        GameObject go = new GameObject("BuffBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(417.5182f, 83.0042f);

        Image img = go.GetComponent<Image>();
        img.sprite = SpriteFromWhiteTexture();
        img.type = Image.Type.Simple;
        img.color = new Color(1f, 0.5478113f, 0f, 1f);

        btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnBuffActivationClick);

        GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        Text text = labelGo.GetComponent<Text>();
        text.raycastTarget = false;
        text.text = "BUFF";
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        PositionBuffBottomLeftInTableRoot();
        UpdateBuffButtonPresentation(Controller);
    }

    void PositionBuffBottomLeftInTableRoot()
    {
        Transform root = GetTableRoot();
        if (root == null)
        {
            return;
        }

        Transform buffTf = root.Find("BuffBtn");
        if (buffTf == null)
        {
            return;
        }

        RectTransform buffRt = buffTf as RectTransform;
        if (buffRt == null)
        {
            return;
        }

        Transform strikeTf = root.Find("Strike");
        if (strikeTf == null)
        {
            strikeTf = root.Find("EndTurn");
        }

        RectTransform strikeRt = strikeTf as RectTransform;
        if (strikeRt != null)
        {
            buffRt.sizeDelta = strikeRt.sizeDelta;
        }

        buffRt.anchorMin = new Vector2(0f, 0f);
        buffRt.anchorMax = new Vector2(0f, 0f);
        buffRt.pivot = new Vector2(0f, 0f);
        buffRt.anchoredPosition = new Vector2(BuffTableRootPadding, BuffTableRootPadding);
    }

    void EnsureStrikeButtonWired()
    {
        Transform root = GetTableRoot();
        if (root == null)
        {
            return;
        }

        foreach (string nm in new[] { "Strike", "EndTurn" })
        {
            Transform t = root.Find(nm);
            if (t == null)
            {
                continue;
            }

            Button b = t.GetComponent<Button>();
            if (b == null)
            {
                continue;
            }

            if (b.onClick.GetPersistentEventCount() == 0)
            {
                b.onClick.AddListener(OnEndTurnClick);
            }
        }
    }

    static Sprite SpriteFromWhiteTexture()
    {
        Texture2D t = Texture2D.whiteTexture;
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static bool IsEligibleForNormalChain(in HandCard h)
    {
        return h.IsSuitedNumber;
    }

    static bool StrikeNumberSubsetHomogeneous(List<HandCard> set)
    {
        var numbers = new List<HandCard>();
        if (set != null)
        {
            for (int i = 0; i < set.Count; i++)
            {
                if (IsEligibleForNormalChain(set[i]))
                {
                    numbers.Add(set[i]);
                }
            }
        }

        if (numbers.Count <= 1)
        {
            return true;
        }

        int r0 = numbers[0].ScoringRank;
        bool allSameRank = true;
        CardSuit s0 = numbers[0].suit;
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

    bool SetAllowsHomogeneousChain(List<HandCard> set)
    {
        if (set == null || set.Count == 0)
        {
            return true;
        }

        bool ok = StrikeNumberSubsetHomogeneous(set);
        if (debugSelection)
        {
            string dump = FormatHandCardsForDiag(set);
            Debug.Log($"[Sel] trial=[{dump}] numberSubsetHomogeneous={ok} -> {(ok ? "ACCEPT" : "REJECT")}");
        }

        if (!ok && set.Count == 2 && IsEligibleForNormalChain(set[0]) && IsEligibleForNormalChain(set[1]))
        {
            Debug.Log(
                $"[UiController] Strike chain rejected (pair): [{FormatHandCardsForDiag(set)}] — " +
                "number cards need the same rank or the same suit.");
        }

        return ok;
    }

    static string StatusTextForStrikeChainRejection(List<HandCard> trial)
    {
        if (trial == null || trial.Count == 0)
        {
            return "Pick number cards that share ONE rank or ONE suit. You can add specials/jokers to the same strike.";
        }

        if (!StrikeNumberSubsetHomogeneous(trial))
        {
            if (trial.Count >= 3)
            {
                return "Your NUMBER cards must all share ONE rank or ONE suit. Specials/jokers can tag along — drop the odd number card out.";
            }

            if (trial.Count == 1)
            {
                return "Invalid chain — check card data.";
            }

            if (trial.Count == 2)
            {
                HandCard a = trial[0];
                HandCard b = trial[1];
                if (IsEligibleForNormalChain(a) && IsEligibleForNormalChain(b))
                {
                    return
                        $"{a.ShortLabel()} + {b.ShortLabel()}: number cards need the same rank (e.g. two 8s) or the same suit (e.g. two Hearts).";
                }
            }

            return "Mix specials with one homogeneous chain — your NUMBER cards must share one rank or one suit.";
        }

        if (trial.Count >= 3)
        {
            return "Every NUMBER card you selected must share ONE rank or ONE suit across all of them (specials/jokers are OK).";
        }

        if (trial.Count == 1)
        {
            return IsEligibleForNormalChain(trial[0])
                ? "Pick another number card that shares the same rank or the same suit, or add a special."
                : "Tap STRIKE to play this special, or add number cards that share one rank or one suit.";
        }

        HandCard x = trial[0];
        HandCard y = trial[1];
        if (IsEligibleForNormalChain(x) && IsEligibleForNormalChain(y))
        {
            if (x.ScoringRank == y.ScoringRank || (x.suit == y.suit && x.suit != CardSuit.None))
            {
                return "That pair should be valid — enable Debug Selection on UiController and check the Console.";
            }

            return
                $"{x.ShortLabel()} + {y.ShortLabel()}: " +
                "need the same rank (e.g. two 8s) or the same suit (e.g. two Hearts). " +
                "Yours mix rank and suit (like 8 and 6, or ♦ and ♥).";
        }

        return "Tap STRIKE to commit, or add number cards that share one rank or one suit.";
    }

    static string FormatHandCardsForDiag(List<HandCard> set)
    {
        if (set == null || set.Count == 0)
        {
            return "";
        }

        var parts = new List<string>(set.Count);
        for (int i = 0; i < set.Count; i++)
        {
            HandCard h = set[i];
            parts.Add($"{h.ScoringRank} {h.suit} uid={h.dealUid}");
        }

        return string.Join("; ", parts);
    }

    public void RefreshMatchPresentation(GameController game)
    {
        if (game == null)
        {
            return;
        }

        PruneDestroyedCardSelections();
        SyncTableActionLabels();
        EnsureStrikeButtonWired();
        PositionBuffBottomLeftInTableRoot();

        PlayerEntity turnSeatForSelection = game.GetSeat(game.curr_player_turn);
        if (turnSeatForSelection != null && turnSeatForSelection.IsHuman)
        {
            TrimPendingHandSelectionsToSeat(turnSeatForSelection);
        }
        else
        {
            pendingHandDealUids.Clear();
        }

        selectedCards.Clear();

        Transform root = GetTableRoot();
        if (root == null)
        {
            if (matchStatusText != null)
            {
                matchStatusText.text = BuildMatchStatusLine(game);
                EnsureStatusBannerDoesNotEatClicks();
            }

            return;
        }

        for (int i = 1; i <= MaxSupportedPlayers; i++)
        {
            Transform seatTf = ResolveSeatTransform(root, i);
            if (seatTf == null)
            {
                continue;
            }

            bool active = i <= game.playerCount;
            seatTf.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            PlayerEntity p = GetDisplaySeatPlayer(game, i);
            ApplySeatPresentation(seatTf, p);
            RenderSeatHand(seatTf, p);
            RenderSeatClassArea(seatTf, p);
        }

        game.RevealAllHandsThisTick = false;

        if (matchStatusText != null)
        {
            matchStatusText.text = BuildMatchStatusLine(game);
            EnsureStatusBannerDoesNotEatClicks();
        }

        UpdateBuffButtonPresentation(game);

        if (debugSelection)
        {
            PlayerEntity acting = game.GetSeat(game.curr_player_turn);
            if (acting != null && acting.IsHuman && acting.Hand.Count > 0)
            {
                Debug.Log($"[Sel] hand seat {acting.SeatIndex + 1}: {acting.HandSummary}");
            }
        }
    }

    void EnsureStatusBannerDoesNotEatClicks()
    {
        if (matchStatusText == null)
        {
            return;
        }

        matchStatusText.raycastTarget = false;
        Transform t = matchStatusText.transform.parent;
        int guard = 0;
        while (t != null && guard++ < 12)
        {
            if (t.name == "TableRoot")
            {
                break;
            }

            Image panelImg = t.GetComponent<Image>();
            if (panelImg != null)
            {
                panelImg.raycastTarget = false;
            }

            t = t.parent;
        }
    }

    string BuildMatchStatusLine(GameController game)
    {
        PlayerEntity acting = game.GetSeat(game.curr_player_turn);
        string seatLabel = acting != null ? $"Seat {acting.SeatIndex + 1}" : "?";
        string role = acting != null && acting.IsHuman ? "You" : "Bot";
        string line =
            $"Round {game.matchRound} — {seatLabel} ({role}) — HP {acting?.CurrentHp ?? 0}/{acting?.MaxHp ?? 0} — Strike / Buff (two same suit).";
        if (acting != null && acting.IsHuman && StrikeChainOkButBuffInvalid())
        {
            line += "\n<color=#ccccaa>Strike OK — BUFF needs two same-suit cards.</color>";
        }

        return line;
    }

    bool StrikeChainOkButBuffInvalid()
    {
        if (selectedCards.Count != 2)
        {
            return false;
        }

        var trial = new List<HandCard>(2)
        {
            selectedCards[0].HeldCard,
            selectedCards[1].HeldCard,
        };
        if (!SetAllowsHomogeneousChain(trial))
        {
            return false;
        }

        return !StrikeResolution.IsValidBuffPair(trial, out _);
    }

    public bool CanCommitBuff()
    {
        if (selectedCards.Count != 2)
        {
            return false;
        }

        var pair = new List<HandCard> { selectedCards[0].HeldCard, selectedCards[1].HeldCard };
        return StrikeResolution.IsValidBuffPair(pair, out _);
    }

    void UpdateBuffButtonPresentation(GameController game)
    {
        Transform root = GetTableRoot();
        Transform buffTf = root?.Find("BuffBtn");
        if (buffTf == null)
        {
            return;
        }

        Button btn = buffTf.GetComponent<Button>();
        Image img = buffTf.GetComponent<Image>();
        if (btn == null)
        {
            return;
        }

        bool humanTurn = game != null && game.GetSeat(game.curr_player_turn)?.IsHuman == true;
        bool canBuff = humanTurn && CanCommitBuff();
        btn.interactable = canBuff;
        if (img != null)
        {
            Color c = img.color;
            c.a = humanTurn && !canBuff ? 0.5f : 1f;
            img.color = c;
        }
    }

    static Transform ResolveClassAreaTransform(Transform seatRoot)
    {
        if (seatRoot == null)
        {
            return null;
        }

        Transform t = seatRoot.Find("SeatView_Template/ClassArea");
        if (t != null)
        {
            return t;
        }

        foreach (Transform child in seatRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == "ClassArea")
            {
                return child;
            }
        }

        return null;
    }

    static void RenderSeatClassArea(Transform seatRoot, PlayerEntity p)
    {
        Transform classArea = ResolveClassAreaTransform(seatRoot);
        if (classArea == null)
        {
            return;
        }

        for (int i = classArea.childCount - 1; i >= 0; i--)
        {
            Destroy(classArea.GetChild(i).gameObject);
        }

        GameObject row = new GameObject("BuffLabels", typeof(RectTransform), typeof(TextMeshProUGUI));
        row.transform.SetParent(classArea, false);
        RectTransform rt = row.GetComponent<RectTransform>();

        TMP_Text tmp = row.GetComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 17;
        tmp.color = new Color(0.95f, 0.93f, 0.85f, 1f);

        tmp.text = p == null ? "" : FormatSuitBuffZoneText(p.Buffs);
        tmp.enableAutoSizing = false;
        tmp.ForceMeshUpdate();

        LayoutBuffLabelInClassArea(rt, tmp, classArea as RectTransform);
    }

    static void LayoutBuffLabelInClassArea(RectTransform labelRt, TMP_Text tmp, RectTransform classAreaRt)
    {
        Vector2 pref = tmp.GetPreferredValues(tmp.text);
        float w = Mathf.Max(pref.x + 16f, 140f);
        float h = Mathf.Max(pref.y + 8f, 36f);

        if (classAreaRt != null)
        {
            float areaW = classAreaRt.rect.width;
            if (areaW >= 48f && areaW >= w * 0.9f)
            {
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                return;
            }
        }

        labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.65f);
        labelRt.pivot = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = Vector2.zero;
        labelRt.sizeDelta = new Vector2(w, h);
    }

    static string FormatSuitBuffZoneText(PlayerBuffController buffs)
    {
        if (buffs == null)
        {
            return "Buffs: —";
        }

        bool anyBuff = buffs.HasSpadeBuff || buffs.HasHeartBuff || buffs.HasDiamondBuff || buffs.HasClubBuff;
        if (!anyBuff)
        {
            return "Buffs: <color=#888888>None locked</color>";
        }

        var parts = new System.Collections.Generic.List<string>(4);
        if (buffs.HasSpadeBuff)
        {
            parts.Add("♠ Spades");
        }

        if (buffs.HasHeartBuff)
        {
            parts.Add("♥ Hearts");
        }

        if (buffs.HasDiamondBuff)
        {
            parts.Add("♦ Diamonds");
        }

        if (buffs.HasClubBuff)
        {
            parts.Add("♣ Clubs");
        }

        return "Buffs: " + string.Join(" · ", parts);
    }

    PlayerEntity GetDisplaySeatPlayer(GameController game, int displaySeatOneBased)
    {
        if (!game.IsHotSeatMode)
            return game.GetSeat(displaySeatOneBased - 1);

        int rotated = (game.curr_player_turn + displaySeatOneBased - 1) % game.playerCount;
        return game.GetSeat(rotated);
    }

    static void ApplySeatPresentation(Transform seatRoot, PlayerEntity p)
    {
        if (p == null)
        {
            return;
        }

        Slider slider = seatRoot.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = Mathf.Max(1, p.MaxHp);
            slider.value = Mathf.Clamp(p.CurrentHp, 0, p.MaxHp);
        }

        TMP_Text[] texts = seatRoot.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text t in texts)
        {
            string n = t.gameObject.name;
            if (n == "HealthNumberText")
            {
                t.text = p.CurrentHp.ToString();
            }
            else if (n == "NameText")
            {
                t.text = p.IsHuman ? $"Player {p.SeatIndex + 1}" : $"Seat {p.SeatIndex + 1}";
            }
        }
    }

    void RenderSeatHand(Transform seatRoot, PlayerEntity p)
    {
        if (seatRoot == null || p == null)
            return;

        bool isActiveSeat = Controller != null && p.SeatIndex == Controller.curr_player_turn;
        bool revealed = Controller != null && Controller.RevealAllHandsThisTick;

        if (!isActiveSeat && !revealed)
        {
            Transform ha = seatRoot.Find("SeatView_Template/HandArea");
            if (ha == null)
            {
                foreach (Transform child in seatRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null && child.name == "HandArea") { ha = child; break; }
                }
            }
            if (ha != null)
            {
                for (int i = ha.childCount - 1; i >= 0; i--)
                    Destroy(ha.GetChild(i).gameObject);

                var backHlg = ha.GetComponent<HorizontalLayoutGroup>();
                if (backHlg != null)
                {
                    backHlg.spacing = Mathf.Max(backHlg.spacing, 4f);
                    backHlg.childForceExpandWidth = false;
                    backHlg.childForceExpandHeight = false;
                }

                for (int i = 0; i < p.Hand.Count; i++)
                {
                    GameObject placeholder = new GameObject("CardBack", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    placeholder.transform.SetParent(ha, false);
                    placeholder.GetComponent<Image>().color = new Color(0.15f, 0.45f, 0.85f, 1f);
                    LayoutElement le = placeholder.GetComponent<LayoutElement>();
                    le.minWidth = 100f;
                    le.preferredWidth = 100f;
                    le.minHeight = 150f;
                    le.preferredHeight = 150f;
                    le.flexibleWidth = 0f;
                    le.flexibleHeight = 0f;
                }
            }
            return;
        }

        Transform handArea = seatRoot.Find("SeatView_Template/HandArea");
        if (handArea == null)
        {
            foreach (Transform child in seatRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == "HandArea")
                {
                    handArea = child;
                    break;
                }
            }
        }

        if (handArea == null)
        {
            return;
        }

        var hlg = handArea.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.spacing = Mathf.Max(hlg.spacing, 4f);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        for (int i = handArea.childCount - 1; i >= 0; i--)
        {
            Destroy(handArea.GetChild(i).gameObject);
        }

        if (UICardprefab == null)
        {
            return;
        }

        bool humanTurnThisSeat =
            p.IsHuman &&
            Controller != null &&
            p.SeatIndex == Controller.curr_player_turn &&
            pendingHandDealUids.Count > 0;

        var spawnedInDeckOrder = new List<CardController>(p.Hand.Count);

        for (int i = 0; i < p.Hand.Count; i++)
        {
            GameObject cardGo = Instantiate(UICardprefab, handArea, false);
            CardController cc = cardGo.GetComponent<CardController>();
            if (cc != null)
            {
                bool selectable = p.IsHuman && Controller != null && p.SeatIndex == Controller.curr_player_turn;
                cc.Init(this, p.Hand[i], selectable, cardVisualCatalog);
                spawnedInDeckOrder.Add(cc);

                LayoutElement layout = cardGo.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    layout = cardGo.AddComponent<LayoutElement>();
                }

                RectTransform crt = cardGo.GetComponent<RectTransform>();
                if (crt != null)
                {
                    layout.minWidth = Mathf.Max(layout.minWidth, crt.sizeDelta.x);
                    layout.preferredWidth = Mathf.Max(layout.preferredWidth, crt.sizeDelta.x > 1f ? crt.sizeDelta.x : 100f);
                    layout.minHeight = Mathf.Max(layout.minHeight, crt.sizeDelta.y);
                    layout.preferredHeight = Mathf.Max(layout.preferredHeight, crt.sizeDelta.y > 1f ? crt.sizeDelta.y : 150f);
                }

                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;
            }
        }

        if (humanTurnThisSeat)
        {
            RebindHumanSelectionFromPending(spawnedInDeckOrder);
        }
    }

    public void ToggleCardSelection(CardController card)
    {
        if (card == null)
        {
            return;
        }

        HandCard hc = card.HeldCard;
        if (debugSelection)
        {
            Debug.Log(
                $"[Sel] click ScoringRank={hc.ScoringRank} suit={hc.suit} dealUid={hc.dealUid} " +
                $"IsSuitedNumber={hc.IsSuitedNumber}");
        }

        bool already = selectedCards.Contains(card);
        if (already)
        {
            if (debugSelection)
            {
                Debug.Log("[Sel] click target IS already-selected card -> TOGGLE OFF");
            }

            selectedCards.Remove(card);
            UnregisterPendingHandDealUid(PendingHandKey(card.HeldCard));
            card.SetSelected(false);
            if (matchStatusText != null && Controller != null)
            {
                matchStatusText.text = BuildMatchStatusLine(Controller);
            }

            UpdateBuffButtonPresentation(Controller);
            return;
        }

        int maxSelectable = Controller != null && Controller.rulesetData != null
            ? Mathf.Max(1, Controller.rulesetData.maxCardsPerAttack)
            : 5;
        if (selectedCards.Count >= maxSelectable)
        {
            if (debugSelection)
            {
                Debug.Log($"[Sel] max selectable reached ({maxSelectable}) -> ignore click");
            }

            return;
        }

        if (debugSelection && selectedCards.Count > 0)
        {
            var before = new List<HandCard>(selectedCards.Count);
            for (int i = 0; i < selectedCards.Count; i++)
            {
                if (selectedCards[i] != null)
                {
                    before.Add(selectedCards[i].HeldCard);
                }
            }

            Debug.Log($"[Sel] selected before: [{FormatHandCardsForDiag(before)}]");
        }

        var trial = new List<HandCard>(selectedCards.Count + 1);
        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] != null)
            {
                trial.Add(selectedCards[i].HeldCard);
            }
        }

        trial.Add(card.HeldCard);
        if (!SetAllowsHomogeneousChain(trial))
        {
            if (debugSelection)
            {
                Debug.Log("[Sel] SetAllowsHomogeneousChain returned false — see chain line above.");
            }

            if (matchStatusText != null)
            {
                matchStatusText.text = StatusTextForStrikeChainRejection(trial);
            }

            return;
        }

        selectedCards.Add(card);
        pendingHandDealUids.Add(PendingHandKey(card.HeldCard));
        card.SetSelected(true);
        if (matchStatusText != null && Controller != null)
        {
            matchStatusText.text = BuildMatchStatusLine(Controller);
        }

        UpdateBuffButtonPresentation(Controller);
    }

    public void ShowCardZoom(Sprite sprite, string title, string body)
    {
        if (cardZoomPreviewObject == null || cardZoomPreviewImage == null)
        {
            return;
        }

        cardZoomPreviewImage.sprite = sprite;
        cardZoomPreviewImage.color = Color.white;
        cardZoomPreviewImage.preserveAspect = true;

        if (cardZoomTitle != null)
        {
            cardZoomTitle.gameObject.SetActive(false);
        }

        if (cardZoomBody != null)
        {
            cardZoomBody.gameObject.SetActive(false);
        }

        cardZoomPreviewObject.SetActive(true);
    }

    public void HideCardZoom()
    {
        if (cardZoomPreviewObject != null)
        {
            cardZoomPreviewObject.SetActive(false);
        }
    }

    public void OnCommitActionClick() => CommitStrikeFromSelection();

    public void OnEndTurnClick() => OnCommitActionClick();

    public void OnBuffActivationClick()
    {
        if (Controller == null)
        {
            return;
        }

        if (!CanCommitBuff())
        {
            return;
        }

        if (selectedCards.Count != 2)
        {
            Debug.LogWarning("[UiController] Buff requires exactly two selected cards (same suit, number cards).");
            if (matchStatusText != null)
            {
                matchStatusText.text = "Select exactly two same-suit number cards, then tap BUFF.";
            }

            return;
        }

        var ids = new List<int>(2);
        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] != null)
            {
                ids.Add(PendingHandKey(selectedCards[i].HeldCard));
            }
        }

        pendingHandDealUids.Clear();
        selectedCards.Clear();
        HideCardZoom();
        Controller.CommitBuffActivation(ids);
    }

    void CommitStrikeFromSelection()
    {
        if (Controller == null)
        {
            return;
        }

        var ids = new List<int>(selectedCards.Count);
        for (int i = 0; i < selectedCards.Count; i++)
        {
            if (selectedCards[i] != null)
            {
                ids.Add(PendingHandKey(selectedCards[i].HeldCard));
            }
        }

        pendingHandDealUids.Clear();
        selectedCards.Clear();
        HideCardZoom();
        Controller.CommitSelectedAction(ids);
    }

    public void ShowWinningScreen()
    {
        start_menu.SetActive(false);
        game_settings.SetActive(false);
        SetTableRootActive(false);
        if (HUD != null)
        {
            HUD.SetActive(false);
        }

        if (game_over != null)
        {
            game_over.SetActive(true);
        }
    }

    public void OnPlayAgainClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void UpdateWinnerScreen(int winnerSeatIndex)
    {
        if (game_over == null)
            return;

        Transform wt = game_over.transform.Find("Winner Text");
        if (wt != null)
        {
            TMP_Text t = wt.GetComponent<TMP_Text>();
            if (t != null)
                t.text = $"Player {winnerSeatIndex + 1} Wins!";
        }

        Transform gt = game_over.transform.Find("Game Over Text");
        if (gt != null)
        {
            TMP_Text t = gt.GetComponent<TMP_Text>();
            if (t != null)
            {
                bool hotSeat = Controller != null && Controller.IsHotSeatMode;
                if (hotSeat)
                    t.text = "Game Over!";
                else
                    t.text = winnerSeatIndex == 0 ? "You Win!" : "You Lose!";
            }
        }
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
