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
    private void SetUnitHitVisual(BattleUnit unit, bool hitEnabled)
    {
        if (unit == null) return;
        Transform defaultStanding = FindChildDeep(unit.transform, defaultStandingObjectName);
        Transform hitObject = FindChildDeep(unit.transform, hitObjectName);
        if (defaultStanding != null) defaultStanding.gameObject.SetActive(!hitEnabled);
        if (hitObject != null) hitObject.gameObject.SetActive(hitEnabled);
    }

    private BattleUnit ChooseEnemyAttackTarget()
    {
        List<BattleUnit> aliveTeamUnits = teamUnits.Where(unit => unit != null && !unit.IsDead).ToList();
        if (aliveTeamUnits.Count <= 0) return null;

        int maxDamage = 0;
        foreach (BattleUnit unit in aliveTeamUnits)
        {
            int damage = teamDamageDealtThisTurn.ContainsKey(unit) ? teamDamageDealtThisTurn[unit] : 0;
            maxDamage = Mathf.Max(maxDamage, damage);
        }

        if (maxDamage <= 0) return aliveTeamUnits[Random.Range(0, aliveTeamUnits.Count)];

        List<BattleUnit> candidates = aliveTeamUnits.Where(unit => teamDamageDealtThisTurn.ContainsKey(unit) && teamDamageDealtThisTurn[unit] == maxDamage).ToList();
        return candidates.Count <= 0 ? aliveTeamUnits[Random.Range(0, aliveTeamUnits.Count)] : candidates[Random.Range(0, candidates.Count)];
    }

    public BattleUnit GetFirstAliveEnemy()
    {
        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy != null && !enemy.IsDead) return enemy;
        }
        return null;
    }

    private bool IsUnitDefending(BattleUnit unit)
    {
        return unit != null && plannedActions.ContainsKey(unit) && plannedActions[unit] == BattleActionType.Defense;
    }

    private void AddTeamDamageRecord(BattleUnit attacker, int damage)
    {
        if (attacker == null || attacker.UnitSide != BattleUnitSide.Team) return;
        if (!teamDamageDealtThisTurn.ContainsKey(attacker)) teamDamageDealtThisTurn[attacker] = 0;
        teamDamageDealtThisTurn[attacker] += Mathf.Max(0, damage);
    }

    private void ResetTurnDamageRecord()
    {
        teamDamageDealtThisTurn.Clear();
        foreach (BattleUnit unit in teamUnits)
        {
            if (unit != null) teamDamageDealtThisTurn[unit] = 0;
        }
    }

    private void UpdateExecuteButtonState()
    {
        int aliveCount = GetAliveTeamCount();
        int plannedCount = GetPlannedAliveTeamCount();
        bool ready = aliveCount > 0 && plannedCount >= aliveCount;

        // 옵저버 패턴
        BattleEvents.RaisePlanCountChanged(plannedCount, aliveCount, ready);
    }

    private bool AreAllAliveTeamUnitsPlanned()
    {
        foreach (BattleUnit unit in teamUnits)
        {
            if (unit == null || unit.IsDead) continue;
            if (!plannedActions.ContainsKey(unit)) return false;
        }
        return true;
    }

    private int GetAliveTeamCount() => teamUnits.Count(unit => unit != null && !unit.IsDead);
    private int GetPlannedAliveTeamCount() => teamUnits.Count(unit => unit != null && !unit.IsDead && plannedActions.ContainsKey(unit));
    private bool AreAllTeamUnitsDead() => teamUnits.All(unit => unit == null || unit.IsDead);
    private bool AreAllEnemyUnitsDead() => enemyUnits.All(unit => unit == null || unit.IsDead);

    private BattleActionType GetPreviousAction(BattleActionType action)
    {
        int value = (int)action - 1;
        if (value < 0) value = 3;
        return (BattleActionType)value;
    }

    private BattleActionType GetNextAction(BattleActionType action)
    {
        int value = (int)action + 1;
        if (value > 3) value = 0;
        return (BattleActionType)value;
    }

    private BattleActionType GetAvailableActionFromSelection(BattleActionType action, int direction)
    {
        if (currentSelectingUnit == null)
        {
            return action;
        }

        BattleActionType result = action;

        for (int i = 0; i < 4; i++)
        {
            if (result != BattleActionType.EgoResonance || CanUseEgoResonance(currentSelectingUnit))
            {
                return result;
            }

            result = direction < 0 ? GetPreviousAction(result) : GetNextAction(result);
        }

        return BattleActionType.Attack;
    }


    private void SetupTurnScreenFade()
    {
        if (screenFadeoutTurnImage == null)
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            foreach (Image image in images)
            {
                if (image != null && image.gameObject.name == screenFadeoutTurnImageName)
                {
                    screenFadeoutTurnImage = image;
                    break;
                }
            }
        }

        if (screenFadeoutTurnImage != null)
        {
            Color color = screenFadeoutTurnImage.color;
            color.a = 0f;
            screenFadeoutTurnImage.color = color;
            screenFadeoutTurnImage.gameObject.SetActive(true);
            screenFadeoutTurnImage.raycastTarget = false;
        }
    }

    private IEnumerator PlayTurnTransitionFadeAndReset()
    {
        PlayUIClickSound();

        if (battleCamera != null)
        {
            battleCamera.transform.position = allyAttackBaseCameraPosition;
            battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
            battleCamera.orthographicSize = normalCameraSize;
        }

        yield return FadeTurnScreen(1f, Mathf.Max(0.01f, turnFadeInTime));
        yield return TweenUnitsToDefaultPositions();
        yield return new WaitForSeconds(Mathf.Max(1f, turnFadeHoldTime));
        yield return FadeTurnScreen(0f, Mathf.Max(0.01f, turnFadeOutTime));
    }

    private IEnumerator FadeTurnScreen(float targetAlpha, float duration)
    {
        if (screenFadeoutTurnImage == null)
        {
            SetupTurnScreenFade();
        }

        if (screenFadeoutTurnImage == null)
        {
            yield break;
        }

        screenFadeoutTurnImage.gameObject.SetActive(true);

        Color startColor = screenFadeoutTurnImage.color;
        float startAlpha = startColor.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), TweenEase.SmoothSine);
            Color color = screenFadeoutTurnImage.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            screenFadeoutTurnImage.color = color;
            yield return null;
        }

        Color finalColor = screenFadeoutTurnImage.color;
        finalColor.a = targetAlpha;
        screenFadeoutTurnImage.color = finalColor;
    }

    private void FindBattleUnits()
    {
        teamUnits.Clear();
        enemyUnits.Clear();

        foreach (GameObject obj in FindGameObjectsWithTagSafe("Team"))
        {
            BattleUnit unit = obj.GetComponent<BattleUnit>();
            if (unit == null) unit = obj.AddComponent<BattleUnit>();
            unit.SetSide(BattleUnitSide.Team);
            teamUnits.Add(unit);
        }

        foreach (GameObject obj in FindGameObjectsWithTagSafe("Enemy"))
        {
            BattleUnit unit = obj.GetComponent<BattleUnit>();
            if (unit == null) unit = obj.AddComponent<BattleUnit>();
            unit.SetSide(BattleUnitSide.Enemy);
            enemyUnits.Add(unit);
        }

        teamUnits.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));
        enemyUnits.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));
        CacheUnitDefaultPositions();
    }

    private void CacheUnitDefaultPositions()
    {
        unitDefaultPositions.Clear();
        foreach (BattleUnit unit in teamUnits)
        {
            if (unit != null) unitDefaultPositions[unit] = unit.transform.position;
        }
        foreach (BattleUnit unit in enemyUnits)
        {
            if (unit != null) unitDefaultPositions[unit] = unit.transform.position;
        }
    }

    private void ResetUnitsToDefaultPositions()
    {
        foreach (KeyValuePair<BattleUnit, Vector3> pair in unitDefaultPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.position = pair.Value;
                RestoreUnitVisualLocalPositions(CaptureUnitVisualSnapshots(pair.Key));
            }
        }
    }

    private IEnumerator TweenUnitsToDefaultPositions()
    {
        float duration = Mathf.Max(0.01f, nextTurnPositionResetTweenTime);

        Dictionary<BattleUnit, Vector3> startPositions = new Dictionary<BattleUnit, Vector3>();
        Dictionary<BattleUnit, List<UnitVisualSnapshot>> visualSnapshots = new Dictionary<BattleUnit, List<UnitVisualSnapshot>>();

        foreach (KeyValuePair<BattleUnit, Vector3> pair in unitDefaultPositions)
        {
            if (pair.Key == null)
            {
                continue;
            }

            startPositions[pair.Key] = pair.Key.transform.position;
            visualSnapshots[pair.Key] = CaptureUnitVisualSnapshots(pair.Key);
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), TweenEase.SmoothSine);

            foreach (KeyValuePair<BattleUnit, Vector3> pair in unitDefaultPositions)
            {
                BattleUnit unit = pair.Key;

                if (unit == null || !startPositions.ContainsKey(unit))
                {
                    continue;
                }

                Vector3 position = Vector3.Lerp(startPositions[unit], pair.Value, t);

                if (visualSnapshots.ContainsKey(unit))
                {
                    ApplyUnitRootAndVisualWorldPosition(unit, visualSnapshots[unit], startPositions[unit], position);
                }
                else
                {
                    unit.transform.position = position;
                }
            }

            yield return null;
        }

        foreach (KeyValuePair<BattleUnit, Vector3> pair in unitDefaultPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.transform.position = pair.Value;

                if (visualSnapshots.ContainsKey(pair.Key))
                {
                    RestoreUnitVisualLocalPositions(visualSnapshots[pair.Key]);
                }
            }
        }
    }

    private GameObject[] FindGameObjectsWithTagSafe(string targetTag)
    {
        try
        {
            return GameObject.FindGameObjectsWithTag(targetTag);
        }
        catch
        {
            Debug.LogError($"{targetTag} 태그가 없습니다.");
            return new GameObject[0];
        }
    }

    private int ExtractNumber(string objectName)
    {
        Match match = Regex.Match(objectName, @"\d+");
        return match.Success ? int.Parse(match.Value) : 9999;
    }

    private void SetupCanvasUI()
    {
        if (battleCanvasUI == null)
        {
            GameObject canvasObject = GameObject.Find("BattleCanvas");
            if (canvasObject != null)
            {
                battleCanvasUI = canvasObject.GetComponent<BattleCanvasUI>();
                if (battleCanvasUI == null) battleCanvasUI = canvasObject.AddComponent<BattleCanvasUI>();
            }
        }

        if (battleCanvasUI == null)
        {
            Debug.LogError("BattleCanvas를 찾지 못했습니다.");
            return;
        }

        battleCanvasUI.Initialize(this, battleCamera);
    }

    private void SetupActionMenus()
    {
        unitMenus.Clear();
        List<BattleUnit> allUnits = new List<BattleUnit>();
        allUnits.AddRange(teamUnits);
        allUnits.AddRange(enemyUnits);

        Transform template = null;
        foreach (BattleUnit unit in allUnits)
        {
            Transform found = FindChildDeep(unit.transform, "ActionMenu");
            if (found != null)
            {
                template = found;
                break;
            }
        }

        foreach (BattleUnit unit in allUnits)
        {
            Transform menuRoot = FindChildDeep(unit.transform, "ActionMenu");
            if (menuRoot == null && template != null)
            {
                GameObject clone = Instantiate(template.gameObject, unit.transform);
                clone.name = "ActionMenu";
                menuRoot = clone.transform;
            }

            if (menuRoot == null)
            {
                Debug.LogWarning($"{unit.gameObject.name}에 ActionMenu가 없고 복제할 템플릿도 없습니다.");
                continue;
            }

            BattleActionMenuUI menu = menuRoot.GetComponent<BattleActionMenuUI>();
            if (menu == null) menu = menuRoot.gameObject.AddComponent<BattleActionMenuUI>();
            menu.Initialize(this, unit);
            unitMenus[unit] = menu;
        }
    }

    private Transform FindChildDeep(Transform root, string targetName)
    {
        if (root.name == targetName) return root;
        foreach (Transform child in root)
        {
            Transform result = FindChildDeep(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    private void CreateEventSystemIfNeeded()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private BattleUnit GetBattleUnitUnderMouse()
    {
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
        if (hit2D.collider != null)
        {
            BattleUnit unit = hit2D.collider.GetComponentInParent<BattleUnit>();
            if (unit != null) return unit;
        }

        if (Physics.Raycast(ray, out RaycastHit hit3D, 1000f))
        {
            BattleUnit unit = hit3D.collider.GetComponentInParent<BattleUnit>();
            if (unit != null) return unit;
        }

        return null;
    }

    private IEnumerator PlayTurnStartIntro(bool isFirstTurn)
    {
        BattleEvents.RaiseTurnStarted(currentTurn);
        if (isFirstTurn) battleCamera.orthographicSize = Mathf.Max(0.01f, firstTurnCameraStartSize);
        yield return TweenCameraSize(normalCameraSize, cameraIntroTweenTime);
        yield return new WaitForSeconds(turnBannerFadeTime + turnBannerStayTime);
    }

    private void StartCameraFocus(BattleUnit unit)
    {
        if (cameraFocusCoroutine != null) StopCoroutine(cameraFocusCoroutine);
        cameraFocusCoroutine = StartCoroutine(FocusCameraOnUnit(unit));
    }

    private IEnumerator FocusCameraOnUnit(BattleUnit unit)
    {
        isSelectionCameraTweening = true;
        Vector3 targetPosition = new Vector3(unit.transform.position.x + cameraFocusOffset.x, unit.transform.position.y + cameraFocusOffset.y, battleCamera.transform.position.z);
        yield return TweenCameraPositionSizeRotation(targetPosition, focusCameraSize, allyAttackDefaultCameraRotation, cameraFocusTweenTime, TweenEase.SmoothSine);
        isSelectionCameraTweening = false;
    }

    private IEnumerator RestoreCameraAfterSelection()
    {
        if (cameraFocusCoroutine != null)
        {
            StopCoroutine(cameraFocusCoroutine);
            cameraFocusCoroutine = null;
        }

        Vector3 targetPosition = new Vector3(defaultCameraPosition.x, defaultCameraPosition.y, battleCamera.transform.position.z);
        yield return TweenCameraPositionSizeRotation(targetPosition, normalCameraSize, allyAttackDefaultCameraRotation, cameraSmoothReturnTime, TweenEase.SmoothSine);
    }

    private IEnumerator PlayAttackCameraRotation(bool isTeamAttack)
    {
        Vector3 leftUp = CombineCameraRotations(cameraLeftRotation, cameraUpRotation);
        Vector3 rightDown = CombineCameraRotations(cameraRightRotation, cameraDownRotation);
        Vector3 start = isTeamAttack ? leftUp : rightDown;
        Vector3 end = isTeamAttack ? rightDown : leftUp;
        start = WeakenCameraRotation(start);
        end = WeakenCameraRotation(end);
        yield return TweenCameraRotationTo(start, cameraAttackRotationStartTime, TweenEase.SmoothSine);
        yield return TweenCameraRotationTo(allyAttackDefaultCameraRotation, cameraAttackRotationToCenterTime, TweenEase.SmoothSine);
        yield return TweenCameraRotationTo(end, cameraAttackRotationToEndTime, TweenEase.OutBack);
        yield return TweenCameraRotationTo(allyAttackDefaultCameraRotation, cameraAttackRotationRestoreTime, TweenEase.SmoothSine);
    }

    private Vector3 CombineCameraRotations(Vector3 horizontal, Vector3 vertical)
    {
        return cameraCenterRotation + (horizontal - cameraCenterRotation) + (vertical - cameraCenterRotation);
    }

    private Vector3 WeakenCameraRotation(Vector3 target)
    {
        return cameraCenterRotation + (target - cameraCenterRotation) * cameraAttackRotationStrength;
    }

    private IEnumerator TweenCameraSize(float targetSize, float duration)
    {
        float startSize = battleCamera.orthographicSize;
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), TweenEase.InOut);
            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
        battleCamera.orthographicSize = targetSize;
    }

    private IEnumerator TweenCameraPositionAndSize(Vector3 targetPosition, float targetSize, float duration)
    {
        yield return TweenCameraPositionAndSize(targetPosition, targetSize, duration, TweenEase.InOut);
    }

    private IEnumerator TweenCameraPositionAndSize(Vector3 targetPosition, float targetSize, float duration, TweenEase ease)
    {
        Vector3 startPosition = battleCamera.transform.position;
        float startSize = battleCamera.orthographicSize;
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            battleCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
        battleCamera.transform.position = targetPosition;
        battleCamera.orthographicSize = targetSize;
    }

    private IEnumerator TweenCameraPositionSizeRotation(Vector3 targetPosition, float targetSize, Vector3 targetEuler, float duration, TweenEase ease)
    {
        Vector3 startPosition = battleCamera.transform.position;
        float startSize = battleCamera.orthographicSize;
        Quaternion startRotation = battleCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            battleCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            battleCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        battleCamera.transform.position = targetPosition;
        battleCamera.orthographicSize = targetSize;
        battleCamera.transform.rotation = targetRotation;
    }

    private IEnumerator TweenCameraRotationTo(Vector3 targetEuler, float duration, TweenEase ease)
    {
        Quaternion startRotation = battleCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(targetEuler);
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            battleCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        battleCamera.transform.rotation = targetRotation;
    }

    private IEnumerator TweenTransformPositionRotationScale(Transform target, Vector3 startPosition, Vector3 endPosition, float startZ, float endZ, Vector3 startScale, Vector3 endScale, float duration, TweenEase ease, bool useAfterimages = false)
    {
        Vector3 baseEuler = target.rotation.eulerAngles;
        float timer = 0f;
        duration = Mathf.Max(0.01f, duration);

        Coroutine afterimageCoroutine = null;

        if (useAfterimages)
        {
            BattleUnit afterimageUnit = target.GetComponent<BattleUnit>();

            if (afterimageUnit == null)
            {
                afterimageUnit = target.GetComponentInParent<BattleUnit>();
            }

            if (afterimageUnit != null)
            {
                afterimageCoroutine = StartCoroutine(SpawnAttackAfterimagesForDuration(afterimageUnit, duration));
            }
        }
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            target.position = Vector3.Lerp(startPosition, endPosition, t);
            target.rotation = Quaternion.Euler(baseEuler.x, baseEuler.y, Mathf.LerpAngle(startZ, endZ, t));
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        target.position = endPosition;
        target.rotation = Quaternion.Euler(baseEuler.x, baseEuler.y, endZ);
        target.localScale = endScale;

        if (afterimageCoroutine != null)
        {
            yield return afterimageCoroutine;
        }
    }
}
