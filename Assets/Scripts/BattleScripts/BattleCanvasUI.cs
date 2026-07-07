using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleCanvasUI : MonoBehaviour
{
    private BattleManager battleManager;
    private Camera battleCamera;

    private Canvas canvas;

    private CanvasGroup turnBannerGroup;
    private RectTransform turnBannerRoot;
    private TextMeshProUGUI turnText;

    [Header("전투 개시 버튼 폰트")]
    [SerializeField] private TMP_FontAsset executeCountFont; // ExcelsiorSans 1 SDF
    [SerializeField] private TMP_FontAsset executeReadyFont; // SCDream9 SDF

    [Header("전투 개시 버튼 텍스트 상태")]
    [SerializeField] private float executeCountFontSize = 55f;
    [SerializeField] private float executeReadyFontSize = 35f;

    [Header("전투 개시 버튼 텍스트 RectTransform")]
    [SerializeField] private Vector4 executeCountOffsets = new Vector4(46.2f, -18.3f, -46.2f, 18.3f);
    [SerializeField] private Vector4 executeReadyOffsets = new Vector4(49.8f, -8.7f, -49.8f, 8.7f);

    [Header("전투 개시 버튼 색상")]
    [SerializeField] private Color executeLockedImageColor = new Color32(0x45, 0x41, 0x73, 255);
    [SerializeField] private Color executeReadyImageColor = Color.white;

    [Header("전투 개시 버튼 페이드")]
    [SerializeField] private float executeButtonFadeTime = 0.18f;

    private RectTransform executeButtonRoot;
    private CanvasGroup executeButtonCanvasGroup;
    private Image executeButtonImage;
    private Button executeButton;
    private TextMeshProUGUI executeText;
    private RectTransform executeTextRect;
    private Coroutine executeButtonFadeCoroutine;

    private Image parryFlashImage;
    private CanvasGroup parryFlashGroup;

    public void Initialize(BattleManager manager, Camera camera)
    {
        battleManager = manager;
        battleCamera = camera;

        canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = battleCamera;
        canvas.planeDistance = 1f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        FindTurnBanner();
        FindExecuteButton();
        FindParryFlash();

        BattleEvents.OnTurnStarted += HandleTurnStarted;
        BattleEvents.OnPlanCountChanged += HandlePlanCountChanged;

        ResetExecuteButtonToCountState();
        SetExecuteButtonVisible(false, true);
        HideParryFlashImmediate();
    }

    private void OnDestroy()
    {
        BattleEvents.OnTurnStarted -= HandleTurnStarted;
        BattleEvents.OnPlanCountChanged -= HandlePlanCountChanged;
    }

    private void HandleTurnStarted(int turn)
    {
        ResetExecuteButtonToCountState();
        StartCoroutine(ShowTurnBanner(turn));
    }

    private void HandlePlanCountChanged(int plannedCount, int aliveCount, bool ready)
    {
        if (executeButton == null || executeButtonImage == null || executeText == null)
        {
            return;
        }

        executeButton.interactable = ready;

        if (ready)
        {
            ApplyExecuteReadyState();
        }
        else
        {
            ApplyExecuteCountState(plannedCount, aliveCount);
        }
    }

    public void SetExecuteButtonVisible(bool visible)
    {
        SetExecuteButtonVisible(visible, false);
    }

    public void SetExecuteButtonVisible(bool visible, bool immediate)
    {
        if (executeButtonRoot == null || executeButtonCanvasGroup == null)
        {
            return;
        }

        if (executeButtonFadeCoroutine != null)
        {
            StopCoroutine(executeButtonFadeCoroutine);
            executeButtonFadeCoroutine = null;
        }

        if (visible)
        {
            executeButtonRoot.gameObject.SetActive(true);
            executeButtonCanvasGroup.blocksRaycasts = true;
            executeButtonCanvasGroup.interactable = true;

            if (immediate)
            {
                executeButtonCanvasGroup.alpha = 1f;
            }
            else
            {
                executeButtonCanvasGroup.alpha = 0f;
                executeButtonFadeCoroutine = StartCoroutine(FadeExecuteButton(0f, 1f, true));
            }
        }
        else
        {
            executeButtonCanvasGroup.blocksRaycasts = false;
            executeButtonCanvasGroup.interactable = false;

            if (immediate)
            {
                executeButtonCanvasGroup.alpha = 0f;
                executeButtonRoot.gameObject.SetActive(false);
            }
            else
            {
                executeButtonFadeCoroutine = StartCoroutine(FadeExecuteButton(executeButtonCanvasGroup.alpha, 0f, false));
            }
        }
    }

    private IEnumerator FadeExecuteButton(float from, float to, bool keepActiveAfterFade)
    {
        float timer = 0f;
        float duration = Mathf.Max(0.01f, executeButtonFadeTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = BattleEaseUtility.GetEase(timer / duration, TweenEase.InOut);
            executeButtonCanvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        executeButtonCanvasGroup.alpha = to;

        if (!keepActiveAfterFade)
        {
            executeButtonRoot.gameObject.SetActive(false);
        }

        executeButtonFadeCoroutine = null;
    }

    private void ResetExecuteButtonToCountState()
    {
        ApplyExecuteCountState(0, 0);
    }

    private void ApplyExecuteCountState(int plannedCount, int aliveCount)
    {
        if (executeButtonImage != null)
        {
            Color color = executeLockedImageColor;
            color.a = 1f;
            executeButtonImage.color = color;
        }

        if (executeText != null)
        {
            executeText.text = $"{plannedCount}/{aliveCount}";

            if (executeCountFont != null)
            {
                executeText.font = executeCountFont;
            }

            executeText.fontSize = executeCountFontSize;
        }

        if (executeTextRect != null)
        {
            SetRectTransformOffsets(
                executeTextRect,
                executeCountOffsets.x,
                executeCountOffsets.y,
                executeCountOffsets.z,
                executeCountOffsets.w
            );
        }
    }

    private void ApplyExecuteReadyState()
    {
        if (executeButtonImage != null)
        {
            Color color = executeReadyImageColor;
            color.a = 1f;
            executeButtonImage.color = color;
        }

        if (executeText != null)
        {
            executeText.text = "실행";

            if (executeReadyFont != null)
            {
                executeText.font = executeReadyFont;
            }

            executeText.fontSize = executeReadyFontSize;
        }

        if (executeTextRect != null)
        {
            SetRectTransformOffsets(
                executeTextRect,
                executeReadyOffsets.x,
                executeReadyOffsets.y,
                executeReadyOffsets.z,
                executeReadyOffsets.w
            );
        }
    }

    private void SetRectTransformOffsets(
        RectTransform rectTransform,
        float left,
        float top,
        float right,
        float bottom
    )
    {
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    public IEnumerator ShowTurnBanner(int turn)
    {
        if (turnBannerRoot == null || turnBannerGroup == null || turnText == null)
        {
            yield break;
        }

        turnText.text = $"Turn {turn}";

        turnBannerRoot.gameObject.SetActive(true);
        turnBannerGroup.alpha = 0f;
        turnBannerRoot.localScale = Vector3.one * 1.15f;

        float timer = 0f;

        while (timer < battleManager.TurnBannerFadeTime)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(timer / battleManager.TurnBannerFadeTime, TweenEase.InOut);

            turnBannerGroup.alpha = Mathf.Lerp(0f, 1f, t);
            turnBannerRoot.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, t);

            yield return null;
        }

        turnBannerGroup.alpha = 1f;
        turnBannerRoot.localScale = Vector3.one;

        yield return new WaitForSeconds(battleManager.TurnBannerStayTime);

        timer = 0f;

        while (timer < battleManager.TurnBannerFadeTime)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(timer / battleManager.TurnBannerFadeTime, TweenEase.InOut);

            turnBannerGroup.alpha = Mathf.Lerp(1f, 0f, t);
            turnBannerRoot.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.96f, t);

            yield return null;
        }

        turnBannerGroup.alpha = 0f;
        turnBannerRoot.gameObject.SetActive(false);
    }

    public IEnumerator PlayParryFlash()
    {
        if (parryFlashImage == null || parryFlashGroup == null)
        {
            yield break;
        }

        parryFlashImage.gameObject.SetActive(true);
        parryFlashImage.color = battleManager.ParryFlashColor;
        parryFlashGroup.alpha = 0f;

        float timer = 0f;

        while (timer < battleManager.ParryFlashInTime)
        {
            timer += Time.deltaTime;
            float t = timer / Mathf.Max(0.01f, battleManager.ParryFlashInTime);

            parryFlashGroup.alpha = Mathf.Lerp(0f, battleManager.ParryFlashColor.a, t);

            yield return null;
        }

        timer = 0f;

        while (timer < battleManager.ParryFlashOutTime)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(timer / battleManager.ParryFlashOutTime, TweenEase.OutCubic);

            parryFlashGroup.alpha = Mathf.Lerp(battleManager.ParryFlashColor.a, 0f, t);

            yield return null;
        }

        HideParryFlashImmediate();
    }

    private void HideParryFlashImmediate()
    {
        if (parryFlashGroup != null)
        {
            parryFlashGroup.alpha = 0f;
        }

        if (parryFlashImage != null)
        {
            parryFlashImage.gameObject.SetActive(false);
        }
    }

    private void FindTurnBanner()
    {
        Transform root = FindChildDeep(transform, "TurnBanner");

        if (root == null)
        {
            Debug.LogWarning("BattleCanvas 아래에 TurnBanner를 찾지 못했습니다.");
            return;
        }

        turnBannerRoot = root as RectTransform;
        turnBannerGroup = root.GetComponent<CanvasGroup>();

        if (turnBannerGroup == null)
        {
            turnBannerGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        Transform textTransform = FindChildDeep(root, "TurnText");

        if (textTransform != null)
        {
            turnText = textTransform.GetComponent<TextMeshProUGUI>();
        }

        root.gameObject.SetActive(false);
    }

    private void FindExecuteButton()
    {
        Transform root = FindChildDeep(transform, "ExecuteImageButton");

        if (root == null)
        {
            Debug.LogWarning("BattleCanvas 아래에 ExecuteImageButton을 찾지 못했습니다.");
            return;
        }

        executeButtonRoot = root as RectTransform;

        executeButtonCanvasGroup = root.GetComponent<CanvasGroup>();

        if (executeButtonCanvasGroup == null)
        {
            executeButtonCanvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }

        executeButtonImage = root.GetComponent<Image>();

        if (executeButtonImage == null)
        {
            executeButtonImage = root.gameObject.AddComponent<Image>();
        }

        executeButton = root.GetComponent<Button>();

        if (executeButton == null)
        {
            executeButton = root.gameObject.AddComponent<Button>();
        }

        executeButton.transition = Selectable.Transition.None;
        executeButton.targetGraphic = executeButtonImage;
        executeButton.onClick.RemoveAllListeners();
        executeButton.onClick.AddListener(() => battleManager.RequestExecuteActions());

        Transform textTransform = FindChildDeep(root, "ExecuteText");

        if (textTransform != null)
        {
            executeText = textTransform.GetComponent<TextMeshProUGUI>();
            executeTextRect = textTransform as RectTransform;

            if (executeText != null)
            {
                executeText.raycastTarget = false;
            }
        }

        Color lockedColor = executeLockedImageColor;
        lockedColor.a = 1f;
        executeButtonImage.color = lockedColor;
    }

    private void FindParryFlash()
    {
        Transform root = FindChildDeep(transform, "ParryFlash");

        if (root == null)
        {
            Debug.LogWarning("BattleCanvas 아래에 ParryFlash를 찾지 못했습니다.");
            return;
        }

        parryFlashImage = root.GetComponent<Image>();

        if (parryFlashImage == null)
        {
            parryFlashImage = root.gameObject.AddComponent<Image>();
        }

        parryFlashImage.raycastTarget = false;

        parryFlashGroup = root.GetComponent<CanvasGroup>();

        if (parryFlashGroup == null)
        {
            parryFlashGroup = root.gameObject.AddComponent<CanvasGroup>();
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