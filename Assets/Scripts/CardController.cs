using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite card_sprite;

    private UiController ownerUi;
    private Image img;
    private TMP_Text rankOverlay;
    private HandCard heldCard;
    private bool isSelectable;
    private bool isSelected;

    public HandCard HeldCard => heldCard;
    public int DealUid => heldCard.dealUid;

    void Awake()
    {
        img = GetComponent<Image>();
        EnsureRankOverlay();
    }

    void EnsureRankOverlay()
    {
        Transform existing = transform.Find("RankOverlay");
        if (existing != null)
        {
            rankOverlay = existing.GetComponent<TMP_Text>();
            ApplyRankOverlayStyle(rankOverlay);
            return;
        }

        GameObject go = new GameObject("RankOverlay", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -6f);
        rt.sizeDelta = new Vector2(80f, 36f);
        rankOverlay = go.GetComponent<TextMeshProUGUI>();
        rankOverlay.alignment = TextAlignmentOptions.Center;
        rankOverlay.text = "";
        ApplyRankOverlayStyle(rankOverlay);
    }

    static void ApplyRankOverlayStyle(TMP_Text tmp)
    {
        if (tmp == null)
        {
            return;
        }

        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = Color.black;
        tmp.raycastTarget = false;
    }

    public void Init(UiController owner, HandCard card, bool selectable, CardVisualCatalog catalog)
    {
        ownerUi = owner;
        heldCard = card;
        isSelectable = selectable;

        Sprite resolved = catalog != null ? catalog.ResolveFace(card) : null;
        if (resolved == null && catalog != null)
        {
            Debug.LogWarning($"[CardController] No sprite for {card.ShortLabel()} — check CardVisualCatalog and PNG import (Sprite 2D UI).");
        }

        card_sprite = resolved != null ? resolved : card_sprite;
        if (img == null)
        {
            img = GetComponent<Image>();
        }

        if (rankOverlay != null)
        {
            bool show = catalog != null && catalog.UseRankOverlay(card);
            ApplyRankOverlayStyle(rankOverlay);
            rankOverlay.gameObject.SetActive(show);
            rankOverlay.text = show ? card.UiRankLabel : string.Empty;
        }

        ApplyVisualState();
    }

    void Start()
    {
        ApplyVisualState();
    }

    void ApplyVisualState()
    {
        if (img == null)
        {
            img = GetComponent<Image>();
        }

        if (img != null && card_sprite != null)
        {
            img.sprite = card_sprite;
        }

        transform.localScale = isSelected ? new Vector3(1.1f, 1.1f, 1f) : Vector3.one;
        if (img != null)
        {
            img.color = isSelectable ? (isSelected ? new Color(0.9f, 1f, 0.9f, 1f) : Color.white) : new Color(0.75f, 0.75f, 0.75f, 0.9f);
            img.raycastTarget = isSelectable;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSelectable || ownerUi == null)
        {
            return;
        }

        ownerUi.ToggleCardSelection(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerUi == null || (heldCard.dealUid == 0 && heldCard.instanceId == 0))
        {
            return;
        }

        ownerUi.ShowCardZoom(card_sprite, CardRulesText.ZoomTitle(heldCard), CardRulesText.ZoomBody(heldCard));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ownerUi?.HideCardZoom();
    }
}
