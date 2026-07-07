using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class BattleManager
{
    [Header("결과 화면")]
    [SerializeField] private RectTransform resultScreen;
    [SerializeField] private Image resultScreenImage;

    [SerializeField] private Image resultGuyImage;
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text resultDescriptionText;

    [SerializeField] private Sprite resultGuyDeadSprite;
    [SerializeField] private Sprite resultGuyWinSprite;

    [SerializeField] private float resultFadeInTime = 0.65f;
    [SerializeField] private float resultTextFadeOutTime = 0.55f;
    [SerializeField] private float resultStayTime = 3f;

    [SerializeField] private float resultGuyStartX = 4.42f;
    [SerializeField] private float resultGuyEndX = -21.6f;

    private bool isResultScreenPlaying;

    private Color resultScreenOriginalColor = Color.white;
    private Color resultGuyOriginalColor = Color.white;
    private Color resultTitleOriginalColor = Color.white;
    private Color resultDescriptionOriginalColor = Color.white;

    /*
     * BattleManager.cs에서 호출하는 함수 이름입니다.
     * 기존 코드가 PlayBattleResultAndQuit(true / false)를 호출하므로,
     * 이 함수가 있어야 컴파일 에러가 나지 않습니다.
     */
    private IEnumerator PlayBattleResultAndQuit(bool isWin)
    {
        yield return PlayResultScreen(isWin);
    }

    private IEnumerator PlayResultScreen(bool isWin)
    {
        if (isResultScreenPlaying)
        {
            yield break;
        }

        isResultScreenPlaying = true;

        SetupResultScreenReferences();

        if (resultScreen == null)
        {
            Debug.LogWarning("ResultScreen을 찾지 못했습니다.");
            yield break;
        }

        resultScreen.gameObject.SetActive(true);

        ApplyResultScreenTextAndImage(isWin);
        CacheResultOriginalColors();

        RectTransform guyRect = null;
        Vector2 guyStartPosition = Vector2.zero;
        Vector2 guyEndPosition = Vector2.zero;

        if (resultGuyImage != null)
        {
            guyRect = resultGuyImage.rectTransform;

            guyStartPosition = guyRect.anchoredPosition;
            guyStartPosition.x = resultGuyStartX;

            guyEndPosition = guyStartPosition;
            guyEndPosition.x = resultGuyEndX;

            guyRect.anchoredPosition = guyStartPosition;
        }

        SetResultScreenAlpha(0f);
        SetResultGuyAlpha(0f);
        SetResultTitleAlpha(0f);
        SetResultDescriptionAlpha(0f);

        float timer = 0f;
        float duration = Mathf.Max(0.01f, resultFadeInTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            SetResultScreenAlpha(t);
            SetResultGuyAlpha(t);
            SetResultTitleAlpha(t);
            SetResultDescriptionAlpha(t);

            if (guyRect != null)
            {
                guyRect.anchoredPosition = Vector2.Lerp(guyStartPosition, guyEndPosition, t);
            }

            yield return null;
        }

        SetResultScreenAlpha(1f);
        SetResultGuyAlpha(1f);
        SetResultTitleAlpha(1f);
        SetResultDescriptionAlpha(1f);

        if (guyRect != null)
        {
            guyRect.anchoredPosition = guyEndPosition;
        }

        yield return new WaitForSeconds(resultStayTime);

        yield return FadeOutResultTextsOnly();

        QuitGameOrStopPlayMode();
    }

    private void SetupResultScreenReferences()
    {
        if (resultScreen == null)
        {
            GameObject foundResultScreen = FindInactiveObjectByName("ResultScreen");

            if (foundResultScreen != null)
            {
                resultScreen = foundResultScreen.GetComponent<RectTransform>();
            }
        }

        if (resultScreen == null)
        {
            return;
        }

        if (resultScreenImage == null)
        {
            resultScreenImage = resultScreen.GetComponent<Image>();
        }

        if (resultGuyImage == null)
        {
            Transform guy = FindChildDeepIncludingInactive(resultScreen, "Guy");

            if (guy != null)
            {
                resultGuyImage = guy.GetComponent<Image>();
            }
        }

        if (resultTitleText == null)
        {
            Transform title = FindChildDeepIncludingInactive(resultScreen, "TitleText");

            if (title != null)
            {
                resultTitleText = title.GetComponent<TMP_Text>();
            }
        }

        if (resultDescriptionText == null)
        {
            Transform description = FindChildDeepIncludingInactive(resultScreen, "DescriptionText");

            if (description != null)
            {
                resultDescriptionText = description.GetComponent<TMP_Text>();
            }
        }

        if (resultGuyDeadSprite == null)
        {
            resultGuyDeadSprite = Resources.Load<Sprite>("dead");
        }

        if (resultGuyWinSprite == null)
        {
            resultGuyWinSprite = Resources.Load<Sprite>("win");
        }
    }

    private void ApplyResultScreenTextAndImage(bool isWin)
    {
        if (isWin)
        {
            if (resultGuyImage != null && resultGuyWinSprite != null)
            {
                resultGuyImage.sprite = resultGuyWinSprite;
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = "살해함";
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text = "성공적으로 살해했습니다!";
            }
        }
        else
        {
            if (resultGuyImage != null && resultGuyDeadSprite != null)
            {
                resultGuyImage.sprite = resultGuyDeadSprite;
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = "살해됨";
            }

            if (resultDescriptionText != null)
            {
                resultDescriptionText.text = "사인은 죽음이였습니다.";
            }
        }
    }

    private void CacheResultOriginalColors()
    {
        if (resultScreenImage != null)
        {
            resultScreenOriginalColor = resultScreenImage.color;
        }

        if (resultGuyImage != null)
        {
            resultGuyOriginalColor = resultGuyImage.color;
        }

        if (resultTitleText != null)
        {
            resultTitleOriginalColor = resultTitleText.color;
        }

        if (resultDescriptionText != null)
        {
            resultDescriptionOriginalColor = resultDescriptionText.color;
        }
    }

    private IEnumerator FadeOutResultTextsOnly()
    {
        float titleStartAlpha = resultTitleText != null ? resultTitleText.color.a : 1f;
        float descriptionStartAlpha = resultDescriptionText != null ? resultDescriptionText.color.a : 1f;

        float timer = 0f;
        float duration = Mathf.Max(0.01f, resultTextFadeOutTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            if (resultTitleText != null)
            {
                SetTextAlpha(
                    resultTitleText,
                    resultTitleOriginalColor,
                    Mathf.Lerp(titleStartAlpha, 0f, t)
                );
            }

            if (resultDescriptionText != null)
            {
                SetTextAlpha(
                    resultDescriptionText,
                    resultDescriptionOriginalColor,
                    Mathf.Lerp(descriptionStartAlpha, 0f, t)
                );
            }

            yield return null;
        }

        SetResultTitleAlpha(0f);
        SetResultDescriptionAlpha(0f);
    }

    private void SetResultScreenAlpha(float alpha)
    {
        if (resultScreenImage == null)
        {
            return;
        }

        Color color = resultScreenOriginalColor;
        color.a = alpha;
        resultScreenImage.color = color;
    }

    private void SetResultGuyAlpha(float alpha)
    {
        if (resultGuyImage == null)
        {
            return;
        }

        Color color = resultGuyOriginalColor;
        color.a = alpha;
        resultGuyImage.color = color;
    }

    private void SetResultTitleAlpha(float alpha)
    {
        if (resultTitleText == null)
        {
            return;
        }

        SetTextAlpha(resultTitleText, resultTitleOriginalColor, alpha);
    }

    private void SetResultDescriptionAlpha(float alpha)
    {
        if (resultDescriptionText == null)
        {
            return;
        }

        SetTextAlpha(resultDescriptionText, resultDescriptionOriginalColor, alpha);
    }

    private void SetTextAlpha(TMP_Text text, Color originalColor, float alpha)
    {
        if (text == null)
        {
            return;
        }

        Color color = originalColor;
        color.a = alpha;

        text.color = color;
        text.ForceMeshUpdate();
    }

    private GameObject FindInactiveObjectByName(string objectName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform target in allTransforms)
        {
            if (target == null)
            {
                continue;
            }

            if (target.name != objectName)
            {
                continue;
            }

            if (!target.gameObject.scene.IsValid())
            {
                continue;
            }

            return target.gameObject;
        }

        return null;
    }

    private Transform FindChildDeepIncludingInactive(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildDeepIncludingInactive(child, targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void QuitGameOrStopPlayMode()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}