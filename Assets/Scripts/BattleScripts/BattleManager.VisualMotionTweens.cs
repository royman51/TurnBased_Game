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

    private List<UnitVisualSnapshot> CaptureUnitVisualSnapshots(BattleUnit unit)
    {
        List<UnitVisualSnapshot> snapshots = new List<UnitVisualSnapshot>();

        if (unit == null)
        {
            return snapshots;
        }

        Transform defaultStanding = FindChildDeep(unit.transform, defaultStandingObjectName);
        Transform hitObject = FindChildDeep(unit.transform, hitObjectName);

        AddVisualSnapshotIfValid(snapshots, defaultStanding);
        AddVisualSnapshotIfValid(snapshots, hitObject);

        return snapshots;
    }

    private void AddVisualSnapshotIfValid(List<UnitVisualSnapshot> snapshots, Transform target)
    {
        if (target == null)
        {
            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].Transform == target)
            {
                return;
            }
        }

        UnitVisualSnapshot snapshot = new UnitVisualSnapshot();
        snapshot.Transform = target;
        snapshot.LocalPosition = target.localPosition;
        snapshot.WorldPosition = target.position;
        snapshots.Add(snapshot);
    }

    private void ApplyUnitRootAndVisualWorldPosition(
        BattleUnit unit,
        List<UnitVisualSnapshot> snapshots,
        Vector3 rootStartPosition,
        Vector3 rootTargetPosition
    )
    {
        if (unit == null)
        {
            return;
        }

        Vector3 delta = rootTargetPosition - rootStartPosition;

        unit.transform.position = rootTargetPosition;

        if (snapshots == null)
        {
            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].Transform != null)
            {
                snapshots[i].Transform.position = snapshots[i].WorldPosition + delta;
            }
        }
    }

    private void RestoreUnitVisualLocalPositions(List<UnitVisualSnapshot> snapshots)
    {
        if (snapshots == null)
        {
            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].Transform != null)
            {
                snapshots[i].Transform.localPosition = snapshots[i].LocalPosition;
            }
        }
    }

    private void RebaseUnitVisualWorldPositions(List<UnitVisualSnapshot> snapshots)
    {
        if (snapshots == null)
        {
            return;
        }

        for (int i = 0; i < snapshots.Count; i++)
        {
            UnitVisualSnapshot snapshot = snapshots[i];

            if (snapshot.Transform != null)
            {
                snapshot.WorldPosition = snapshot.Transform.position;
                snapshots[i] = snapshot;
            }
        }
    }

    private IEnumerator TweenUnitRootAndVisualPosition(
        BattleUnit unit,
        List<UnitVisualSnapshot> snapshots,
        Vector3 startPosition,
        Vector3 endPosition,
        float duration,
        TweenEase ease
    )
    {
        duration = Mathf.Max(0.01f, duration);
        RebaseUnitVisualWorldPositions(snapshots);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

            ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, position);

            yield return null;
        }

        ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, endPosition);
    }

    private IEnumerator TweenUnitRootAndVisualLaunch(
        BattleUnit unit,
        List<UnitVisualSnapshot> snapshots,
        Vector3 startPosition,
        Vector3 endPosition,
        float peakWorldY,
        float duration
    )
    {
        duration = Mathf.Max(0.01f, duration);
        RebaseUnitVisualWorldPositions(snapshots);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float xzT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, xzT);

            if (rawT < 0.5f)
            {
                float upT = BattleEaseUtility.GetEase(rawT / 0.5f, TweenEase.OutCubic);
                position.y = Mathf.Lerp(startPosition.y, peakWorldY, upT);
            }
            else
            {
                float downT = BattleEaseUtility.GetEase((rawT - 0.5f) / 0.5f, TweenEase.InOut);
                position.y = Mathf.Lerp(peakWorldY, endPosition.y, downT);
            }

            ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, position);

            yield return null;
        }

        ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, endPosition);
    }

    private IEnumerator TweenUnitRootAndVisualParabolicThrow(
        BattleUnit unit,
        List<UnitVisualSnapshot> snapshots,
        Vector3 startPosition,
        Vector3 endPosition,
        float arcHeight,
        float duration
    )
    {
        duration = Mathf.Max(0.01f, duration);
        RebaseUnitVisualWorldPositions(snapshots);

        Vector3 cameraStartPosition = battleCamera.transform.position;
        float cameraStartSize = battleCamera.orthographicSize;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);
            float cameraT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, moveT);

            // 포물선 보정입니다. 시작점에서 부드럽게 떠오르고, 끝점으로 내려앉습니다.
            float arcOffset = Mathf.Sin(rawT * Mathf.PI) * arcHeight;
            position.y += arcOffset;

            ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, position);

            Vector3 desiredCameraPosition = new Vector3(
                position.x,
                position.y + 0.35f,
                enemyApproachCameraOffset.z
            );

            battleCamera.transform.position = Vector3.Lerp(
                cameraStartPosition,
                desiredCameraPosition,
                cameraT
            );

            battleCamera.orthographicSize = Mathf.Lerp(
                cameraStartSize,
                enemyThrowChargeZoomOutSize,
                cameraT
            );

            battleCamera.transform.rotation = Quaternion.Slerp(
                battleCamera.transform.rotation,
                Quaternion.Euler(allyAttackDefaultCameraRotation),
                Mathf.Clamp01(Time.deltaTime * 6f)
            );

            yield return null;
        }

        ApplyUnitRootAndVisualWorldPosition(unit, snapshots, startPosition, endPosition);
        battleCamera.transform.position = new Vector3(endPosition.x, endPosition.y + 0.35f, enemyApproachCameraOffset.z);
        battleCamera.orthographicSize = enemyThrowChargeZoomOutSize;
    }


    private IEnumerator PlayWeaponStuckCameraTwitch(Vector3 hitDirection)
    {
        Vector3 originalPosition = battleCamera.transform.position;
        Quaternion originalRotation = battleCamera.transform.rotation;

        Vector3 sideDirection = new Vector3(-hitDirection.y, hitDirection.x, 0f);

        if (sideDirection.sqrMagnitude <= 0.0001f)
        {
            sideDirection = Vector3.up;
        }

        sideDirection.Normalize();

        float duration = 0.11f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float power = 1f - rawT;
            float wave = Mathf.Sin(rawT * Mathf.PI * 2.5f);

            battleCamera.transform.position =
                originalPosition +
                sideDirection * wave * basicAttackShakeAmount * 0.75f * power +
                hitDirection.normalized * wave * basicAttackShakeAmount * 0.35f * power;

            battleCamera.transform.rotation =
                originalRotation *
                Quaternion.Euler(0f, 0f, wave * basicAttackShakeRotationAmount * 0.85f * power);

            yield return null;
        }

        battleCamera.transform.position = originalPosition;
        battleCamera.transform.rotation = originalRotation;
    }

    private IEnumerator PlayBasicFinalPierceZoomOut()
    {
        yield return TweenCameraSize(
            Mathf.Max(normalCameraSize, basicAttackFinalZoomOutSize),
            Mathf.Max(0.01f, basicAttackFinalZoomOutTime)
        );

        yield return TweenCameraSize(
            normalCameraSize,
            Mathf.Max(0.01f, basicAttackFinalZoomReturnTime)
        );
    }

    private IEnumerator TweenBasicAttackDashWithLagCamera(Transform attackerTransform, Vector3 attackerStart, Vector3 attackerEnd, Vector3 targetPosition, float duration, float targetCameraSize)
    {
        duration = Mathf.Max(0.01f, duration);
        Coroutine afterimageCoroutine = StartAttackAfterimagesForTransform(attackerTransform, duration);
        Vector3 cameraStartPosition = battleCamera.transform.position;
        Vector3 cameraTargetPosition = new Vector3(targetPosition.x + basicAttackLagCameraOffset.x, targetPosition.y + basicAttackLagCameraOffset.y, basicAttackLagCameraOffset.z);
        float startSize = battleCamera.orthographicSize;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            float cameraT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            if (attackerTransform != null) attackerTransform.position = Vector3.Lerp(attackerStart, attackerEnd, moveT);

            Vector3 desiredCameraPosition = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, cameraT);
            battleCamera.transform.position = Vector3.Lerp(battleCamera.transform.position, desiredCameraPosition, Mathf.Clamp01(Time.deltaTime * basicAttackLagCameraFollowSpeed));
            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetCameraSize, BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine));
            battleCamera.transform.rotation = Quaternion.Slerp(battleCamera.transform.rotation, Quaternion.Euler(allyAttackDefaultCameraRotation), Mathf.Clamp01(Time.deltaTime * 10f));
            yield return null;
        }

        if (attackerTransform != null) attackerTransform.position = attackerEnd;

        if (afterimageCoroutine != null)
        {
            yield return afterimageCoroutine;
        }
    }

    private IEnumerator TweenTargetJumpAndKnockback(Transform target, Vector3 startPosition, Vector3 endPosition, float jumpHeight, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            Vector3 basePosition = Vector3.Lerp(startPosition, endPosition, moveT);
            float jumpOffset = Mathf.Sin(rawT * Mathf.PI) * jumpHeight;
            if (target != null) target.position = basePosition + Vector3.up * jumpOffset;
            yield return null;
        }

        if (target != null) target.position = endPosition;
    }

    private IEnumerator TweenAllyDashToImpactWithLagCamera(Transform attackerTransform, Vector3 attackerStart, Vector3 attackerEnd, Vector3 targetPosition, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        Coroutine afterimageCoroutine = StartAttackAfterimagesForTransform(attackerTransform, duration);
        Quaternion startRotation = battleCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(allyAttackDashCameraRotation);
        float startSize = battleCamera.orthographicSize;
        float targetSize = allyAttackDashZoomInSize;
        Vector3 cameraStartPosition = battleCamera.transform.position;
        Vector3 cameraTargetPosition = targetPosition + allyAttackDashCameraOffset;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            float cameraT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);
            if (attackerTransform != null) attackerTransform.position = Vector3.Lerp(attackerStart, attackerEnd, moveT);
            Vector3 desiredCameraPosition = Vector3.Lerp(cameraStartPosition, cameraTargetPosition, cameraT);
            battleCamera.transform.position = Vector3.Lerp(battleCamera.transform.position, desiredCameraPosition, Mathf.Clamp01(Time.deltaTime * allyAttackCameraFollowSpeed));
            battleCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic));
            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic));
            yield return null;
        }

        if (attackerTransform != null) attackerTransform.position = attackerEnd;

        if (afterimageCoroutine != null)
        {
            yield return afterimageCoroutine;
        }
    }

    private IEnumerator TweenTwoUnitsPosition(Transform firstTarget, Vector3 firstStart, Vector3 firstEnd, Transform secondTarget, Vector3 secondStart, Vector3 secondEnd, float duration, TweenEase ease)
    {
        duration = Mathf.Max(0.01f, duration);
        Coroutine afterimageCoroutine = StartAttackAfterimagesForTransform(firstTarget, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = BattleEaseUtility.GetEase(Mathf.Clamp01(timer / duration), ease);
            if (firstTarget != null) firstTarget.position = Vector3.Lerp(firstStart, firstEnd, t);
            if (secondTarget != null) secondTarget.position = Vector3.Lerp(secondStart, secondEnd, t);
            yield return null;
        }

        if (firstTarget != null) firstTarget.position = firstEnd;
        if (secondTarget != null) secondTarget.position = secondEnd;

        if (afterimageCoroutine != null)
        {
            yield return afterimageCoroutine;
        }
    }

}
