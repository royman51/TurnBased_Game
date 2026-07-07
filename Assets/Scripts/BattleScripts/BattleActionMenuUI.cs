using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleActionMenuUI : MonoBehaviour
{
    private BattleManager battleManager;
    private BattleUnit owner;

    private Canvas worldCanvas;
    private CanvasGroup canvasGroup;

    private RectTransform infoPanelRoot;
    private Image portraitImage;
    private TextMeshProUGUI unitNameText;
    private RectTransform healthFillRect;
    private RectTransform actionButtonsRoot;

    private readonly Dictionary<BattleActionType, Image> buttonImages = new Dictionary<BattleActionType, Image>();
    private readonly Dictionary<BattleActionType, TextMeshProUGUI> buttonTexts = new Dictionary<BattleActionType, TextMeshProUGUI>();
    private readonly Dictionary<BattleActionType, Button> buttons = new Dictionary<BattleActionType, Button>();

    public BattleUnit Owner => owner;

    public void Initialize(BattleManager manager, BattleUnit unit)
    {
        battleManager = manager;
        owner = unit;

        worldCanvas = GetComponent<Canvas>();

        if (worldCanvas == null)
        {
            worldCanvas = gameObject.AddComponent<Canvas>();
        }

        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = battleManager.BattleCamera;
        worldCanvas.overrideSorting = true;
        worldCanvas.sortingOrder = battleManager.ActionMenuSortingOrder;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        FindReferences();
        SetupButtons();

        HideImmediate();
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf || battleManager == null || owner == null)
        {
            return;
        }

        transform.position = owner.transform.position + battleManager.ActionMenuWorldOffset;
        transform.localScale = Vector3.one * battleManager.ActionMenuWorldScale;

        if (battleManager.ActionMenuFaceCamera && battleManager.BattleCamera != null)
        {
            transform.rotation = battleManager.BattleCamera.transform.rotation;
        }
    }

    public void ShowUnitInfo(bool showButtons)
    {
        if (owner == null)
        {
            return;
        }

        if (unitNameText != null)
        {
            unitNameText.text = owner.gameObject.name;
        }

        if (portraitImage != null)
        {
            portraitImage.color = battleManager.PortraitEmptyColor;
        }

        SetHealthFill(owner.HealthRate);
        SetButtonsVisible(showButtons);
    }

    public void SetSelectedAction(BattleActionType selectedAction)
    {
        foreach (KeyValuePair<BattleActionType, Image> pair in buttonImages)
        {
            bool selected = pair.Key == selectedAction;

            pair.Value.color = selected ? battleManager.SelectedMenuColor : battleManager.NormalMenuColor;

            if (buttonTexts.ContainsKey(pair.Key))
            {
                buttonTexts[pair.Key].color = selected ? battleManager.SelectedMenuTextColor : battleManager.NormalMenuTextColor;
                buttonTexts[pair.Key].fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    public void SetButtonsInteractable(bool interactable)
    {
        foreach (Button button in buttons.Values)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    public void SetButtonsVisible(bool visible)
    {
        if (actionButtonsRoot != null)
        {
            actionButtonsRoot.gameObject.SetActive(visible);
        }
    }

    public void SetHealthFill(float value)
    {
        if (healthFillRect == null)
        {
            return;
        }

        value = Mathf.Clamp01(value);

        Vector2 anchorMax = healthFillRect.anchorMax;
        anchorMax.x = value;
        healthFillRect.anchorMax = anchorMax;

        healthFillRect.offsetMin = Vector2.zero;
        healthFillRect.offsetMax = Vector2.zero;
    }

    public IEnumerator TweenHealthFill(float from, float to, float duration)
    {
        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);
        duration = Mathf.Max(0.01f, duration);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(timer / duration, TweenEase.OutCubic);

            SetHealthFill(Mathf.Lerp(from, to, t));

            yield return null;
        }

        SetHealthFill(to);
    }

    public IEnumerator ShakeInfoPanelByDamage(int damage)
    {
        if (infoPanelRoot == null)
        {
            yield break;
        }

        Vector2 originalPosition = infoPanelRoot.anchoredPosition;
        float amount = Mathf.Clamp(
            damage * battleManager.DamagePanelShakePerDamage,
            0f,
            battleManager.DamagePanelMaxShake
        );

        float timer = 0f;
        float duration = Mathf.Max(0.01f, battleManager.DamagePanelShakeTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float power = 1f - rawT;

            infoPanelRoot.anchoredPosition = originalPosition + Random.insideUnitCircle * amount * power;

            yield return null;
        }

        infoPanelRoot.anchoredPosition = originalPosition;
    }

    public IEnumerator Fade(bool show)
    {
        gameObject.SetActive(true);

        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;

        float timer = 0f;
        float duration = Mathf.Max(0.01f, battleManager.MenuFadeTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(timer / duration, TweenEase.InOut);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        if (!show)
        {
            gameObject.SetActive(false);
        }
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        gameObject.SetActive(false);
    }

    private void FindReferences()
    {
        Transform infoPanel = FindChildDeep(transform, "Unit Info Panel");

        if (infoPanel != null)
        {
            infoPanelRoot = infoPanel as RectTransform;
        }

        Transform portrait = FindChildDeep(transform, "Portrait Empty Image");

        if (portrait != null)
        {
            portraitImage = portrait.GetComponent<Image>();
        }

        Transform name = FindChildDeep(transform, "Unit Name");

        if (name != null)
        {
            unitNameText = name.GetComponent<TextMeshProUGUI>();
        }

        Transform healthFill = FindChildDeep(transform, "Health Bar Fill");

        if (healthFill != null)
        {
            healthFillRect = healthFill as RectTransform;
        }

        Transform buttonsRoot = FindChildDeep(transform, "Action Buttons");

        if (buttonsRoot != null)
        {
            actionButtonsRoot = buttonsRoot as RectTransform;
        }
    }

    private void SetupButtons()
    {
        SetupButton("Attack", BattleActionType.Attack);
        SetupButton("Defend", BattleActionType.Defense);
        SetupButton("Item", BattleActionType.Item);
        SetupButton("EgoResonance", BattleActionType.EgoResonance);
    }

    private void SetupButton(string objectName, BattleActionType actionType)
    {
        Transform buttonTransform = FindChildDeep(transform, objectName);

        if (buttonTransform == null)
        {
            Debug.LogWarning($"{gameObject.name} 안에서 {objectName} 버튼을 찾지 못했습니다.");
            return;
        }

        Image buttonImage = buttonTransform.GetComponent<Image>();

        if (buttonImage == null)
        {
            buttonImage = buttonTransform.gameObject.AddComponent<Image>();
        }

        Button button = buttonTransform.GetComponent<Button>();

        if (button == null)
        {
            button = buttonTransform.gameObject.AddComponent<Button>();
        }

        button.transition = Selectable.Transition.None;
        button.targetGraphic = buttonImage;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => battleManager.OnActionMenuButtonClicked(owner, actionType));

        EventTrigger trigger = buttonTransform.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger = buttonTransform.gameObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((eventData) =>
        {
            battleManager.OnActionMenuButtonHovered(owner, actionType);
        });

        trigger.triggers.Add(pointerEnter);

        TextMeshProUGUI text = buttonTransform.GetComponentInChildren<TextMeshProUGUI>(true);

        Graphic[] childGraphics = buttonTransform.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in childGraphics)
        {
            if (graphic.transform != buttonTransform)
            {
                graphic.raycastTarget = false;
            }
        }

        buttonImage.raycastTarget = true;

        buttonImages[actionType] = buttonImage;
        buttons[actionType] = button;

        if (text != null)
        {
            text.raycastTarget = false;
            buttonTexts[actionType] = text;
        }
    }

    private Transform FindChildDeep(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildDeep(child, targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}