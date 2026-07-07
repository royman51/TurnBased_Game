// SPLIT BUILD: DAMAGE_DISPLAY_TEMPLATE_ORIGIN_FIXED_V11_SPLIT
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class BattleManager
{
    private IEnumerator PlayDamageFeedbackTogether(BattleUnit damagedUnit, float beforeHealthRate, float afterHealthRate, int damage, Vector3 hitDirection)
    {
        Coroutine healthInfoCoroutine = StartCoroutine(ShowDamageHealthInfo(damagedUnit, beforeHealthRate, afterHealthRate, damage));
        Coroutine hitReactionCoroutine = StartCoroutine(PlayHitReaction(damagedUnit, hitDirection));
        yield return healthInfoCoroutine;
        yield return hitReactionCoroutine;
    }

    private IEnumerator PlayDamageFeedbackWithFlashOnly(BattleUnit damagedUnit, float beforeHealthRate, float afterHealthRate, int damage, Vector3 hitDirection)
    {
        Coroutine healthInfoCoroutine = StartCoroutine(ShowDamageHealthInfo(damagedUnit, beforeHealthRate, afterHealthRate, damage));
        Coroutine hitFlashCoroutine = StartCoroutine(PlayHitFlashOnly(damagedUnit, hitDirection));
        yield return healthInfoCoroutine;
        yield return hitFlashCoroutine;
    }

    private IEnumerator ShowDamageHealthInfo(BattleUnit unit, float beforeRate, float afterRate, int damage)
    {
        if (unit == null || !unitMenus.ContainsKey(unit)) yield break;

        BattleActionMenuUI menu = unitMenus[unit];
        menu.ShowUnitInfo(false);
        yield return menu.Fade(true);

        Coroutine healthCoroutine = StartCoroutine(menu.TweenHealthFill(beforeRate, afterRate, healthDecreaseTweenTime));
        Coroutine shakeCoroutine = StartCoroutine(menu.ShakeInfoPanelByDamage(damage));
        yield return healthCoroutine;
        yield return shakeCoroutine;
        yield return new WaitForSeconds(damageInfoStayTime);
        yield return menu.Fade(false);
    }

    private IEnumerator WaitForParryInput()
    {
        parryInputSucceeded = false;
        float timer = 0f;
        float duration = Mathf.Max(0.01f, parryInputWindowTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                parryInputSucceeded = true;
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator PlayParryScreenEffect(Vector3 direction)
    {
        Coroutine flash = StartCoroutine(battleCanvasUI.PlayParryFlash());
        Coroutine shake = StartCoroutine(PlayParryCameraShake(direction));
        yield return flash;
        yield return shake;
    }

    private IEnumerator PlayParryCameraShake(Vector3 hitDirection)
    {
        yield return CameraShake(hitDirection, parryCameraShakeDuration, parryCameraShakePositionAmount, parryCameraShakeRotationAmount, parryCameraShakeSpeed);
    }

    private IEnumerator PlayHitCameraShake(Vector3 hitDirection)
    {
        yield return CameraShake(hitDirection, hitCameraShakeDuration, hitCameraShakePositionAmount, hitCameraShakeRotationAmount, hitCameraShakeSpeed);
    }

    private IEnumerator PlayBasicAttackImpactCameraShake(Vector3 hitDirection)
    {
        yield return CameraShake(hitDirection, basicAttackShakeDuration, basicAttackShakeAmount, basicAttackShakeRotationAmount, basicAttackShakeSpeed);
    }

    private IEnumerator PlayAllyPierceImpactCameraShake(Vector3 hitDirection)
    {
        yield return CameraShake(hitDirection, allyAttackPierceShakeDuration, allyAttackPierceShakeAmount, allyAttackPierceShakeRotationAmount, allyAttackPierceShakeSpeed);
    }

    private IEnumerator CameraShake(Vector3 hitDirection, float duration, float amount, float rotationAmount, float speed)
    {
        Vector3 originalPosition = battleCamera.transform.position;
        Quaternion originalRotation = battleCamera.transform.rotation;
        Vector3 sideDirection = new Vector3(-hitDirection.y, hitDirection.x, 0f);
        if (sideDirection.sqrMagnitude <= 0.0001f) sideDirection = Vector3.up;
        sideDirection.Normalize();

        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float power = 1f - rawT;
            float waveA = Mathf.Sin(timer * speed);
            float waveB = Mathf.Sin(timer * speed * 1.45f);
            Vector3 offset = sideDirection * waveA * amount * power + hitDirection.normalized * waveB * amount * 0.45f * power;
            float rotationOffset = waveA * rotationAmount * power;
            battleCamera.transform.position = originalPosition + offset;
            battleCamera.transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, rotationOffset);
            yield return null;
        }

        battleCamera.transform.position = originalPosition;
        battleCamera.transform.rotation = originalRotation;
    }

    private IEnumerator PlayHitReaction(BattleUnit hitUnit, Vector3 hitDirection)
    {
        if (hitUnit == null) yield break;

        Transform hitTransform = hitUnit.transform;
        Vector3 originalPosition = hitTransform.position;
        Vector3 originalScale = hitTransform.localScale;
        Vector3 originalEuler = hitTransform.rotation.eulerAngles;
        Vector3 knockbackDirection = hitDirection;
        if (knockbackDirection.sqrMagnitude <= 0.0001f) knockbackDirection = Vector3.right;
        knockbackDirection.Normalize();
        Vector3 shakeDirection = new Vector3(-knockbackDirection.y, knockbackDirection.x, 0f);
        if (shakeDirection.sqrMagnitude <= 0.0001f) shakeDirection = Vector3.up;
        shakeDirection.Normalize();
        Vector3 knockbackPosition = originalPosition + knockbackDirection * hitKnockbackDistance;

        SpriteRenderer[] renderers = hitTransform.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].color = hitReactionColor;
        }

        float timer = 0f;
        float duration = Mathf.Max(0.01f, hitReactionTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float punchT = Mathf.Sin(rawT * Mathf.PI);
            float power = 1f - rawT;
            Vector3 knockbackOffset = Vector3.Lerp(Vector3.zero, knockbackPosition - originalPosition, punchT);
            Vector3 shakeOffset = shakeDirection * Mathf.Sin(timer * hitShakeSpeed) * hitShakeAmount * power;
            hitTransform.position = originalPosition + knockbackOffset + shakeOffset;
            float rotationShake = Mathf.Sin(timer * hitShakeSpeed * 0.75f) * hitRotationShakeZ * power;
            hitTransform.rotation = Quaternion.Euler(originalEuler.x, originalEuler.y, originalEuler.z + rotationShake);
            Vector3 squashScale = new Vector3(originalScale.x * hitSquashX, originalScale.y * hitSquashY, originalScale.z);
            hitTransform.localScale = Vector3.Lerp(originalScale, squashScale, punchT);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = Color.Lerp(hitReactionColor, originalColors[i], rawT);
            }
            yield return null;
        }

        hitTransform.position = originalPosition;
        hitTransform.rotation = Quaternion.Euler(originalEuler);
        hitTransform.localScale = originalScale;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = originalColors[i];
        }
    }

    private IEnumerator PlayHitFlashOnly(BattleUnit hitUnit, Vector3 hitDirection)
    {
        if (hitUnit == null) yield break;

        SpriteRenderer[] renderers = hitUnit.transform.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            renderers[i].color = hitReactionColor;
        }

        float timer = 0f;
        float duration = Mathf.Max(0.01f, hitReactionTime);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].color = Color.Lerp(hitReactionColor, originalColors[i], t);
            }
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = originalColors[i];
        }
    }

    private Coroutine StartAttackAfterimagesForTransform(Transform targetTransform, float duration)
    {
        if (targetTransform == null)
        {
            return null;
        }

        BattleUnit afterimageUnit = targetTransform.GetComponent<BattleUnit>();

        if (afterimageUnit == null)
        {
            afterimageUnit = targetTransform.GetComponentInParent<BattleUnit>();
        }

        if (afterimageUnit == null)
        {
            return null;
        }

        return StartCoroutine(SpawnAttackAfterimagesForDuration(afterimageUnit, duration));
    }

    private IEnumerator SpawnAttackAfterimagesForDuration(BattleUnit unit, float duration)
    {
        if (!attackAfterimageEnabled || unit == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);

        float timer = 0f;
        float nextSpawnTime = 0f;
        float interval = Mathf.Max(0.01f, attackAfterimageInterval);

        SpawnAttackAfterimage(unit);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            nextSpawnTime -= Time.deltaTime;

            if (nextSpawnTime <= 0f)
            {
                SpawnAttackAfterimage(unit);
                nextSpawnTime = interval;
            }

            yield return null;
        }
    }

    private void SpawnAttackAfterimage(BattleUnit unit)
    {
        if (!attackAfterimageEnabled || unit == null)
        {
            return;
        }

        SpriteRenderer[] sourceRenderers = unit.GetComponentsInChildren<SpriteRenderer>();

        if (sourceRenderers == null || sourceRenderers.Length <= 0)
        {
            return;
        }

        GameObject afterimageRoot = new GameObject(unit.gameObject.name + "_Afterimage");
        afterimageRoot.transform.position = Vector3.zero;
        afterimageRoot.transform.rotation = Quaternion.identity;
        afterimageRoot.transform.localScale = Vector3.one;

        List<SpriteRenderer> afterimageRenderers = new List<SpriteRenderer>();
        Color selectedAfterimageTint = unit.UnitSide == BattleUnitSide.Enemy
            ? enemyAttackAfterimageTint
            : attackAfterimageTint;

        foreach (SpriteRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer == null || !sourceRenderer.enabled || sourceRenderer.sprite == null)
            {
                continue;
            }

            GameObject afterimageObject = new GameObject(sourceRenderer.gameObject.name + "_AfterimageSprite");
            afterimageObject.transform.SetParent(afterimageRoot.transform, true);
            afterimageObject.transform.position = sourceRenderer.transform.position;
            afterimageObject.transform.rotation = sourceRenderer.transform.rotation;
            afterimageObject.transform.localScale = sourceRenderer.transform.lossyScale;

            SpriteRenderer afterimageRenderer = afterimageObject.AddComponent<SpriteRenderer>();
            afterimageRenderer.sprite = sourceRenderer.sprite;
            afterimageRenderer.flipX = sourceRenderer.flipX;
            afterimageRenderer.flipY = sourceRenderer.flipY;
            afterimageRenderer.drawMode = sourceRenderer.drawMode;
            afterimageRenderer.size = sourceRenderer.size;
            afterimageRenderer.tileMode = sourceRenderer.tileMode;
            afterimageRenderer.maskInteraction = sourceRenderer.maskInteraction;
            afterimageRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
            afterimageRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            afterimageRenderer.sortingOrder = sourceRenderer.sortingOrder + attackAfterimageSortingOrderOffset;
            afterimageRenderer.sharedMaterial = sourceRenderer.sharedMaterial;

            Color sourceColor = sourceRenderer.color;
            Color afterimageColor = new Color(
                sourceColor.r * selectedAfterimageTint.r,
                sourceColor.g * selectedAfterimageTint.g,
                sourceColor.b * selectedAfterimageTint.b,
                Mathf.Min(sourceColor.a, attackAfterimageAlpha)
            );

            afterimageRenderer.color = afterimageColor;
            afterimageRenderers.Add(afterimageRenderer);
        }

        if (afterimageRenderers.Count <= 0)
        {
            Destroy(afterimageRoot);
            return;
        }

        StartCoroutine(FadeAndDestroyAfterimage(afterimageRoot, afterimageRenderers.ToArray()));
    }

    private IEnumerator FadeAndDestroyAfterimage(GameObject afterimageRoot, SpriteRenderer[] afterimageRenderers)
    {
        float duration = Mathf.Max(0.01f, attackAfterimageLifeTime);
        float timer = 0f;

        Color[] startColors = new Color[afterimageRenderers.Length];

        for (int i = 0; i < afterimageRenderers.Length; i++)
        {
            if (afterimageRenderers[i] != null)
            {
                startColors[i] = afterimageRenderers[i].color;
            }
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float alphaRate = 1f - t;

            for (int i = 0; i < afterimageRenderers.Length; i++)
            {
                if (afterimageRenderers[i] == null)
                {
                    continue;
                }

                Color color = startColors[i];
                color.a *= alphaRate;
                afterimageRenderers[i].color = color;
            }

            yield return null;
        }

        if (afterimageRoot != null)
        {
            Destroy(afterimageRoot);
        }
    }

    private GameObject FindInactiveSceneObjectByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            return null;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];

            if (obj == null || obj.name != targetName)
            {
                continue;
            }

            if (!obj.scene.IsValid())
            {
                continue;
            }

            if ((obj.hideFlags & HideFlags.HideInHierarchy) != 0)
            {
                continue;
            }

            return obj;
        }

        return null;
    }

    private void SetupDamageDisplayTemplate()
    {
        if (damageDisplayTemplate != null)
        {
            return;
        }

        if (string.IsNullOrEmpty(damageDisplayObjectName))
        {
            return;
        }

        /*
         * V15 수정입니다.
         * 이전 방식은 이름으로 찾은 오브젝트의 RectTransform 값을 복사했지만,
         * 복사 이후 parent/layout/anchoredPosition 보정 때문에 다른 위치로 튈 수 있었습니다.
         * 이번 방식은 원본 DamageDisplay의 "월드 위치"를 최우선으로 고정합니다.
         */
        GameObject found = GameObject.Find(damageDisplayObjectName);

        if (found == null)
        {
            found = FindInactiveSceneObjectByName(damageDisplayObjectName);
        }

        if (found != null)
        {
            damageDisplayTemplate = found;
        }
    }

    private void ShowDamageDisplay(BattleUnit unit, int damageAmount)
    {
        if (damageAmount <= 0)
        {
            return;
        }

        if (damageDisplayTemplate == null)
        {
            SetupDamageDisplayTemplate();
        }

        if (damageDisplayTemplate == null)
        {
            Debug.LogWarning("DamageDisplay 원본을 찾지 못했습니다. 씬 안에 DamageDisplay 오브젝트가 있는지 확인해주세요.");
            return;
        }

        Transform templateTransform = damageDisplayTemplate.transform;
        Transform templateParent = templateTransform.parent;

        Vector3 templateWorldPosition = templateTransform.position;
        Quaternion templateWorldRotation = templateTransform.rotation;
        Vector3 templateLocalScale = GetDamageDisplayBaseScale(templateTransform);

        RectTransform templateRect = templateTransform as RectTransform;
        Vector2 templateAnchoredPosition = templateRect != null ? templateRect.anchoredPosition : Vector2.zero;
        Vector3 templateAnchoredPosition3D = templateRect != null ? templateRect.anchoredPosition3D : Vector3.zero;
        Vector2 templateAnchorMin = templateRect != null ? templateRect.anchorMin : Vector2.zero;
        Vector2 templateAnchorMax = templateRect != null ? templateRect.anchorMax : Vector2.zero;
        Vector2 templatePivot = templateRect != null ? templateRect.pivot : Vector2.zero;
        Vector2 templateSizeDelta = templateRect != null ? templateRect.sizeDelta : Vector2.zero;

        /*
         * 원본이 꺼져 있어도 복사본은 생성할 수 있습니다.
         * 여기서 중요한 건 Instantiate 직후 parent/layout이 위치를 바꾸더라도,
         * 바로 아래에서 다시 원본의 월드 위치로 강제 고정한다는 점입니다.
         */
        GameObject displayObject = Instantiate(damageDisplayTemplate);
        displayObject.name = damageDisplayTemplate.name + "_Clone";

        if (templateParent != null)
        {
            displayObject.transform.SetParent(templateParent, true);
        }

        RectTransform displayRect = displayObject.transform as RectTransform;

        if (displayRect != null && templateRect != null)
        {
            displayRect.anchorMin = templateAnchorMin;
            displayRect.anchorMax = templateAnchorMax;
            displayRect.pivot = templatePivot;
            displayRect.sizeDelta = templateSizeDelta;
            displayRect.anchoredPosition = templateAnchoredPosition;
            displayRect.anchoredPosition3D = templateAnchoredPosition3D;
        }

        displayObject.transform.position = templateWorldPosition;
        displayObject.transform.rotation = templateWorldRotation;
        displayObject.transform.localScale = templateLocalScale;

        /*
         * 부모에 LayoutGroup이 있으면 복사본 위치를 강제로 재배치할 수 있어서 무시하도록 합니다.
         */
        LayoutElement layoutElement = displayObject.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = displayObject.AddComponent<LayoutElement>();
        }

        layoutElement.ignoreLayout = true;

        if (templateParent != null)
        {
            displayObject.transform.SetSiblingIndex(templateTransform.GetSiblingIndex() + 1);
        }

        displayObject.SetActive(true);

        /*
         * SetActive 이후에도 레이아웃/캔버스가 한 번 더 만질 수 있으므로 다시 원본 위치로 고정합니다.
         */
        if (displayRect != null && templateRect != null)
        {
            displayRect.anchorMin = templateAnchorMin;
            displayRect.anchorMax = templateAnchorMax;
            displayRect.pivot = templatePivot;
            displayRect.sizeDelta = templateSizeDelta;
            displayRect.anchoredPosition = templateAnchoredPosition;
            displayRect.anchoredPosition3D = templateAnchoredPosition3D;
        }

        displayObject.transform.position = templateWorldPosition;
        displayObject.transform.rotation = templateWorldRotation;
        displayObject.transform.localScale = templateLocalScale;

        TMP_Text amountText = null;

        Transform amountTransform = FindChildDeep(displayObject.transform, damageAmountTextName);

        if (amountTransform == null && damageAmountTextName != "Amount")
        {
            amountTransform = FindChildDeep(displayObject.transform, "Amount");
        }

        if (amountTransform != null)
        {
            amountText = amountTransform.GetComponent<TMP_Text>();
        }

        if (amountText == null)
        {
            amountText = displayObject.GetComponentInChildren<TMP_Text>(true);
        }

        if (amountText != null)
        {
            amountText.text = damageAmount.ToString();
        }

        CanvasGroup canvasGroup = displayObject.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = displayObject.AddComponent<CanvasGroup>();
        }

        StartCoroutine(AnimateDamageDisplay(displayObject, canvasGroup));
    }

    private IEnumerator AnimateDamageDisplay(GameObject displayObject, CanvasGroup canvasGroup)
    {
        if (displayObject == null || canvasGroup == null)
        {
            yield break;
        }

        Transform displayTransform = displayObject.transform;

        /*
         * V16 핵심입니다.
         * 데미지 표시의 애니메이션 기준 크기를 임의의 큰 값이 아니라,
         * 원본 DamageDisplay가 씬에 배치된 실제 localScale 그대로 사용합니다.
         * 즉 원본 크기 = 기본 크기입니다.
         */
        Vector3 baseScale = GetDamageDisplayBaseScale(displayTransform);

        float safeStartScale = Mathf.Clamp(damageDisplayStartScale, 0.01f, 1.5f);
        float safePopScale = Mathf.Clamp(damageDisplayPopScale, 0.01f, 1.5f);

        Vector3 startScale = baseScale * safeStartScale;
        Vector3 popScale = baseScale * safePopScale;

        /*
         * V16 핵심입니다.
         * UI RectTransform이라도 anchoredPosition을 건드리지 않고,
         * 원본 복사본의 현재 월드 위치에서 위로만 이동합니다.
         * 그래서 Canvas/Anchor/부모가 달라도 "원본이 보이는 그 자리"를 기준으로 뜹니다.
         */
        Vector3 startWorldPosition = displayTransform.position;
        Vector3 endWorldPosition = startWorldPosition + Vector3.up * GetDamageDisplayWorldRiseDistance(displayTransform);

        float fadeInTime = Mathf.Max(0.01f, damageDisplayFadeInTime);
        float settleTime = Mathf.Max(0.01f, damageDisplaySettleTime);
        float floatTime = Mathf.Max(0.01f, damageDisplayFloatTime);

        canvasGroup.alpha = 0f;
        displayTransform.localScale = startScale;
        displayTransform.position = startWorldPosition;

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / fadeInTime), TweenEase.OutCubic);

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            displayTransform.localScale = Vector3.Lerp(startScale, popScale, t);
            displayTransform.position = startWorldPosition;

            yield return null;
        }

        timer = 0f;

        while (timer < settleTime)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / settleTime), TweenEase.SmoothSine);

            displayTransform.localScale = Vector3.Lerp(popScale, baseScale, t);
            displayTransform.position = startWorldPosition;

            yield return null;
        }

        timer = 0f;

        while (timer < floatTime)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / floatTime);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            displayTransform.localScale = baseScale;
            displayTransform.position = Vector3.Lerp(startWorldPosition, endWorldPosition, t);

            yield return null;
        }

        if (displayObject != null)
        {
            Destroy(displayObject);
        }
    }

    private Vector3 GetDamageDisplayBaseScale(Transform sourceTransform)
    {
        /*
         *
         * 더 이상 sourceTransform.localScale, 부모 Canvas 스케일, 인스펙터에 남아 있는 이전 값의 영향을 받지 않습니다.
         */
        return damageDisplayExactBaseScale;
    }

    private float GetDamageDisplayWorldRiseDistance(Transform displayTransform)
    {
        Canvas parentCanvas = displayTransform.GetComponentInParent<Canvas>();

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
        {
            return damageDisplayRiseDistance;
        }

        return damageDisplayRiseDistance * 0.01f;
    }

    private void SetupHealAudioSource()
    {
        if (healAudioSource == null)
        {
            healAudioSource = hitAudioSource;
        }

        if (healAudioSource == null)
        {
            healAudioSource = GetComponent<AudioSource>();
        }

        if (healAudioSource == null)
        {
            healAudioSource = gameObject.AddComponent<AudioSource>();
        }

        healAudioSource.playOnAwake = false;
        healAudioSource.spatialBlend = 0f;
    }

}
