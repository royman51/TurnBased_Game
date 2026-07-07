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
    private IEnumerator PlayAllyBasicPierceAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        Transform attackerTransform = attacker.transform;
        Transform targetTransform = target.transform;

        Vector3 attackerOriginalPosition = attackerTransform.position;
        Vector3 attackerOriginalEuler = attackerTransform.rotation.eulerAngles;
        Vector3 attackerOriginalScale = attackerTransform.localScale;

        Vector3 targetOriginalPosition = targetTransform.position;
        Vector3 targetOriginalEuler = targetTransform.rotation.eulerAngles;
        Vector3 targetOriginalScale = targetTransform.localScale;

        List<UnitVisualSnapshot> targetVisualSnapshots = CaptureUnitVisualSnapshots(target);

        Vector3 directionToTarget = targetOriginalPosition - attackerOriginalPosition;
        if (directionToTarget.sqrMagnitude <= 0.0001f)
        {
            directionToTarget = Vector3.right;
        }
        directionToTarget.Normalize();

        float openingCameraTime = Mathf.Max(basicAttackOpeningCameraTime, 0.34f);
        float approachTime = Mathf.Max(basicAttackApproachTime, 0.55f);
        float quickZoomTime = Mathf.Max(basicAttackQuickZoomTime, 0.16f);
        float firstDashTime = Mathf.Max(basicAttackFirstDashTime, 0.18f);
        float secondDashTime = Mathf.Max(basicAttackSecondDashTime, basicAttackSecondStuckStartDelay + 0.12f);
        float secondPullBackTime = Mathf.Max(basicAttackSecondPullBackTime, 0.42f);
        float secondPullBackDistance = Mathf.Max(basicAttackSecondPullBackDistance, 1.05f);
        float secondChargeWaitTime = Mathf.Max(basicAttackSecondChargeWaitTime, 0.5f);
        float secondChargeDriftBackDistance = Mathf.Max(basicAttackSecondChargeDriftBackDistance, 0.65f);
        float secondChargeZoomSize = Mathf.Min(basicAttackHitZoomSize - 0.1f, basicAttackSecondChargeZoomSize);
        float secondForwardExtraDistance = Mathf.Max(basicAttackSecondForwardExtraDistance, 2.4f);
        float finalRetreatTime = Mathf.Max(basicAttackFinalRetreatTime, 0.34f);
        float cameraReturnTime = Mathf.Max(basicAttackCameraReturnTime, 0.45f);
        float targetJumpTime = Mathf.Max(basicAttackTargetJumpTime, 0.34f);
        float finalPierceTime = Mathf.Max(basicAttackFinalPierceTime, 0.18f);

        SetUnitHitVisual(attacker, false);

        int firstDamage = Mathf.Max(1, Mathf.CeilToInt(attacker.AttackDamage * 0.5f));

        Vector3 cameraAllyPosition = new Vector3(
            attackerOriginalPosition.x + basicAttackOpeningCameraOffset.x,
            attackerOriginalPosition.y + basicAttackOpeningCameraOffset.y,
            basicAttackOpeningCameraOffset.z
        );

        Vector3 cameraTargetPosition = new Vector3(
            targetOriginalPosition.x + basicAttackTargetCameraOffset.x,
            targetOriginalPosition.y + basicAttackTargetCameraOffset.y,
            basicAttackTargetCameraOffset.z
        );

        float finalTargetX = directionToTarget.x >= 0f
            ? Mathf.Abs(basicAttackFinalTargetWorldX)
            : -Mathf.Abs(basicAttackFinalTargetWorldX);

        Vector3 approachPosition = targetOriginalPosition - directionToTarget * basicAttackApproachDistance;
        Vector3 pullBackPosition = approachPosition - directionToTarget * basicAttackQuickPullBackDistance;
        Vector3 firstImpactPosition = targetOriginalPosition - directionToTarget * 0.08f;

        Vector3 firstKnockbackPosition = new Vector3(
            Mathf.Lerp(targetOriginalPosition.x, finalTargetX, 0.45f),
            targetOriginalPosition.y,
            targetOriginalPosition.z
        );

        Vector3 secondStartPosition = firstImpactPosition - directionToTarget * secondPullBackDistance;
        Vector3 secondChargePosition = secondStartPosition - directionToTarget * secondChargeDriftBackDistance;

        Vector3 secondImpactPosition =
            firstKnockbackPosition +
            directionToTarget * secondForwardExtraDistance;

        Vector3 finalAttackerPiercePosition =
            secondImpactPosition +
            directionToTarget * Mathf.Max(basicAttackFinalPierceDistance, 3.65f);

        Vector3 finalTargetPosition = new Vector3(
            finalTargetX,
            targetOriginalPosition.y,
            targetOriginalPosition.z
        );

        yield return TweenCameraPositionSizeRotation(
            cameraAllyPosition,
            basicAttackOpeningZoomSize,
            allyAttackDefaultCameraRotation,
            openingCameraTime,
            TweenEase.OutCubic
        );

        Coroutine approachCameraCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraTargetPosition,
            normalCameraSize + basicAttackApproachZoomOutSizeOffset,
            approachTime,
            TweenEase.SmoothSine
        ));

        Coroutine approachUnitCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            attackerOriginalPosition,
            approachPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            approachTime,
            TweenEase.OutCubic,
            true
        ));

        yield return approachUnitCoroutine;
        yield return approachCameraCoroutine;

        Coroutine quickZoomCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraTargetPosition,
            basicAttackHitZoomSize,
            quickZoomTime,
            TweenEase.OutCubic
        ));

        Coroutine quickPullBackCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            approachPosition,
            pullBackPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            quickZoomTime,
            TweenEase.OutCubic,
            true
        ));

        yield return quickPullBackCoroutine;
        yield return quickZoomCoroutine;

        SetUnitHitVisual(attacker, true);

        yield return TweenBasicAttackDashWithLagCamera(
            attackerTransform,
            pullBackPosition,
            firstImpactPosition,
            targetOriginalPosition,
            firstDashTime,
            basicAttackHitZoomSize - 0.25f
        );

        float beforeRate = target.HealthRate;
        int firstActualDamage = target.TakeDamage(firstDamage);
        PlayRandomHitSound(firstActualDamage);
        ShowDamageDisplay(target, firstActualDamage);
        float middleRate = target.HealthRate;

        if (recordTeamDamage)
        {
            AddTeamDamageRecord(attacker, firstActualDamage);
        }

        Coroutine firstShakeCoroutine = StartCoroutine(PlayBasicAttackImpactCameraShake(directionToTarget));
        Coroutine firstDamageFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeRate,
            middleRate,
            firstActualDamage,
            directionToTarget
        ));

        Coroutine firstImpactCameraCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraTargetPosition,
            normalCameraSize + basicAttackImpactZoomOutOffset,
            targetJumpTime,
            TweenEase.OutCubic
        ));

        Coroutine firstTargetLaunchCoroutine = StartCoroutine(TweenUnitRootAndVisualLaunch(
            target,
            targetVisualSnapshots,
            targetOriginalPosition,
            firstKnockbackPosition,
            basicAttackLaunchWorldY,
            targetJumpTime
        ));

        yield return firstTargetLaunchCoroutine;
        yield return firstImpactCameraCoroutine;
        yield return firstShakeCoroutine;

        // 1타 이후 2타 준비. Hit를 끄고 0.5초에 가까워질수록 점점 덜 움직이는 후퇴를 계속합니다.
        SetUnitHitVisual(attacker, false);

        yield return TweenTransformPositionRotationScale(
            attackerTransform,
            firstImpactPosition,
            secondStartPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            secondPullBackTime,
            TweenEase.SmoothSine,
            true
        );

        Coroutine chargeCameraCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraTargetPosition,
            secondChargeZoomSize,
            secondChargeWaitTime,
            TweenEase.SmoothSine
        ));

        Coroutine chargeDriftCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            secondStartPosition,
            secondChargePosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            secondChargeWaitTime,
            TweenEase.OutCubic,
            true
        ));

        yield return chargeDriftCoroutine;
        yield return chargeCameraCoroutine;

        SetUnitHitVisual(attacker, true);

        Coroutine secondZoomCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraTargetPosition,
            Mathf.Min(basicAttackHitZoomSize, secondChargeZoomSize - 0.15f),
            quickZoomTime,
            TweenEase.OutCubic
        ));

        // 2타 돌진은 시작 후 0.25초가 지난 시점부터 대상이 공격자 위치를 따라오도록 처리합니다.
        // 이전에는 돌진이 끝난 뒤에야 대상 이동이 시작돼서, 색만 바뀌고 실제 끌림이 잘 보이지 않았습니다.
        yield return TweenSecondAttackDashAndDragTargetAfterDelay(
            attackerTransform,
            target,
            targetVisualSnapshots,
            secondChargePosition,
            secondImpactPosition,
            firstKnockbackPosition,
            directionToTarget,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            secondDashTime,
            Mathf.Max(0f, basicAttackSecondStuckStartDelay)
        );

        yield return secondZoomCoroutine;

        // 2타 돌진 중 끌려온 현재 위치부터 마지막 10 피해 전까지 대상이 계속 끌려갑니다.
        int stuckCount = Mathf.Max(1, basicAttackSecondStuckShakeCount);
        int stuckDamage = Mathf.Max(1, basicAttackSecondStuckDamage);
        float stuckHoldTime = Mathf.Max(0.01f, basicAttackSecondImpactHoldTime);
        float stuckTwitchMoveTime = 0.13f;

        Vector3 stuckDragStartPosition = targetTransform.position;
        Vector3 stuckDragEndPosition = Vector3.Lerp(stuckDragStartPosition, finalTargetPosition, 0.62f);
        Vector3 currentTargetDragPosition = stuckDragStartPosition;

        for (int i = 0; i < stuckCount; i++)
        {
            float tickBeforeRate = target.HealthRate;
            int actualTickDamage = target.TakeDamage(stuckDamage);
            float tickAfterRate = target.HealthRate;

            PlayRandomHitSound(actualTickDamage);
            ShowDamageDisplay(target, actualTickDamage);

            if (recordTeamDamage)
            {
                AddTeamDamageRecord(attacker, actualTickDamage);
            }

            Vector3 nextTargetDragPosition = Vector3.Lerp(
                stuckDragStartPosition,
                stuckDragEndPosition,
                (i + 1f) / stuckCount
            );

            StartCoroutine(PlayHitFlashOnly(target, directionToTarget));
            Coroutine stuckTwitchCoroutine = StartCoroutine(PlayWeaponStuckCameraTwitch(directionToTarget));

            yield return TweenUnitRootAndVisualPosition(
                target,
                targetVisualSnapshots,
                currentTargetDragPosition,
                nextTargetDragPosition,
                stuckTwitchMoveTime,
                TweenEase.OutCubic
            );

            currentTargetDragPosition = nextTargetDragPosition;

            yield return stuckTwitchCoroutine;
            yield return new WaitForSeconds(stuckHoldTime);
        }

        // 걸림을 뚫고 앞으로 크게 관통합니다.
        // 이때도 대상은 무기에 끌려가며, 10 피해는 끌려간 뒤 관통이 끝나는 순간에 들어갑니다.
        Coroutine finalZoomCoroutine = StartCoroutine(PlayBasicFinalPierceZoomOut());
        Coroutine finalTargetMoveCoroutine = StartCoroutine(TweenUnitRootAndVisualPosition(
            target,
            targetVisualSnapshots,
            currentTargetDragPosition,
            finalTargetPosition,
            finalPierceTime,
            TweenEase.OutCubic
        ));

        yield return TweenTransformPositionRotationScale(
            attackerTransform,
            secondImpactPosition,
            finalAttackerPiercePosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            finalPierceTime,
            TweenEase.OutCubic,
            true
        );

        yield return finalTargetMoveCoroutine;

        int finalPierceDamage = Mathf.Max(1, basicAttackFinalPierceDamage);
        float finalBeforeRate = target.HealthRate;
        int finalActualDamage = target.TakeDamage(finalPierceDamage);
        float finalAfterRate = target.HealthRate;

        PlayRandomHitSound(finalActualDamage);
        ShowDamageDisplay(target, finalActualDamage);

        if (recordTeamDamage)
        {
            AddTeamDamageRecord(attacker, finalActualDamage);
        }

        Coroutine finalShakeCoroutine = StartCoroutine(PlayBasicAttackImpactCameraShake(directionToTarget));
        Coroutine finalDamageFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            finalBeforeRate,
            finalAfterRate,
            finalActualDamage,
            directionToTarget
        ));

        yield return finalShakeCoroutine;
        yield return finalZoomCoroutine;

        SetUnitHitVisual(attacker, false);

        Coroutine cameraReturnCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            allyAttackBaseCameraPosition,
            normalCameraSize,
            cameraReturnTime,
            TweenEase.SmoothSine
        ));

        Coroutine attackerReturnCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            finalAttackerPiercePosition,
            attackerOriginalPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            finalRetreatTime,
            TweenEase.SmoothSine,
            true
        ));

        yield return attackerReturnCoroutine;
        yield return cameraReturnCoroutine;
        yield return firstDamageFeedbackCoroutine;
        yield return finalDamageFeedbackCoroutine;

        attackerTransform.position = attackerOriginalPosition;
        attackerTransform.rotation = Quaternion.Euler(attackerOriginalEuler);
        attackerTransform.localScale = attackerOriginalScale;

        targetTransform.position = finalTargetPosition;
        targetTransform.rotation = Quaternion.Euler(targetOriginalEuler);
        targetTransform.localScale = targetOriginalScale;
        RestoreUnitVisualLocalPositions(targetVisualSnapshots);

        SetUnitHitVisual(attacker, false);

        battleCamera.transform.position = allyAttackBaseCameraPosition;
        battleCamera.orthographicSize = normalCameraSize;
        battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
    }

    private IEnumerator TweenSecondAttackDashAndDragTargetAfterDelay(
        Transform attackerTransform,
        BattleUnit target,
        List<UnitVisualSnapshot> targetVisualSnapshots,
        Vector3 attackerStartPosition,
        Vector3 attackerEndPosition,
        Vector3 targetStartPosition,
        Vector3 directionToTarget,
        float attackerStartZ,
        Vector3 attackerScale,
        float duration,
        float dragStartDelay
    )
    {
        duration = Mathf.Max(0.01f, duration);
        dragStartDelay = Mathf.Clamp(dragStartDelay, 0f, duration * 0.95f);

        float timer = 0f;
        Vector3 baseEuler = attackerTransform.rotation.eulerAngles;
        Vector3 lastTargetPosition = targetStartPosition;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float attackerT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);

            Vector3 attackerPosition = Vector3.Lerp(attackerStartPosition, attackerEndPosition, attackerT);
            attackerTransform.position = attackerPosition;
            attackerTransform.rotation = Quaternion.Euler(baseEuler.x, baseEuler.y, attackerStartZ);
            attackerTransform.localScale = attackerScale;

            if (timer >= dragStartDelay)
            {
                float dragDuration = Mathf.Max(0.01f, duration - dragStartDelay);
                float dragT = BattleEaseUtility.GetEase(Mathf.Clamp01((timer - dragStartDelay) / dragDuration), TweenEase.SmoothSine);

                // 대상은 공격자의 앞쪽, 즉 무기에 걸린 것처럼 보이는 위치를 따라갑니다.
                Vector3 targetFollowPosition = attackerPosition + directionToTarget * 0.22f;
                lastTargetPosition = Vector3.Lerp(targetStartPosition, targetFollowPosition, dragT);

                ApplyUnitRootAndVisualWorldPosition(
                    target,
                    targetVisualSnapshots,
                    targetStartPosition,
                    lastTargetPosition
                );
            }

            yield return null;
        }

        attackerTransform.position = attackerEndPosition;
        attackerTransform.rotation = Quaternion.Euler(baseEuler.x, baseEuler.y, attackerStartZ);
        attackerTransform.localScale = attackerScale;
    }

    private IEnumerator PlayAllyStrongPierceAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        Transform attackerTransform = attacker.transform;
        Transform targetTransform = target.transform;

        Vector3 attackerOriginalPosition = attackerTransform.position;
        Vector3 attackerOriginalEuler = attackerTransform.rotation.eulerAngles;
        Vector3 attackerOriginalScale = attackerTransform.localScale;
        Vector3 targetOriginalPosition = targetTransform.position;
        Vector3 targetOriginalEuler = targetTransform.rotation.eulerAngles;
        Vector3 targetOriginalScale = targetTransform.localScale;

        Vector3 directionToTarget = targetOriginalPosition - attackerOriginalPosition;
        if (directionToTarget.sqrMagnitude <= 0.0001f) directionToTarget = Vector3.right;
        directionToTarget.Normalize();

        float strongCloseCameraTime = Mathf.Max(allyAttackCloseCameraTime, 1.5f);
        float strongDashToImpactTime = Mathf.Max(allyAttackDashToImpactTime, 0.26f);
        float strongDashThroughTime = Mathf.Max(allyAttackDashThroughTime, 0.40f);
        float strongReturnTime = Mathf.Max(allyAttackReturnTime, 0.75f);

        SetUnitHitVisual(attacker, false);

        Vector3 pullBackPosition = attackerOriginalPosition - directionToTarget * allyAttackPullBackDistance;
        Vector3 impactPosition = targetOriginalPosition - directionToTarget * 0.05f;
        Vector3 pierceEndPosition = targetOriginalPosition + directionToTarget * allyAttackPierceDistance;
        Vector3 targetKnockbackPosition = targetOriginalPosition + directionToTarget * allyAttackEnemyDragDistance;

        Coroutine openingCameraCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            allyAttackCloseCameraPosition,
            allyAttackStrongZoomInSize,
            allyAttackLeftCameraRotation,
            strongCloseCameraTime,
            TweenEase.SmoothSine
        ));

        Coroutine pullBackCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            attackerOriginalPosition,
            pullBackPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            strongCloseCameraTime,
            TweenEase.SmoothSine,
            true
        ));

        yield return openingCameraCoroutine;
        yield return pullBackCoroutine;

        SetUnitHitVisual(attacker, true);

        yield return TweenAllyDashToImpactWithLagCamera(
            attackerTransform,
            pullBackPosition,
            impactPosition,
            targetOriginalPosition,
            strongDashToImpactTime
        );

        float beforeRate = target.HealthRate;
        int actualDamage = target.TakeDamage(attacker.AttackDamage * 2);
        PlayRandomHitSound(actualDamage);
        ShowDamageDisplay(target, actualDamage);
        float afterRate = target.HealthRate;

        if (recordTeamDamage) AddTeamDamageRecord(attacker, actualDamage);

        Coroutine impactShakeCoroutine = StartCoroutine(PlayAllyPierceImpactCameraShake(directionToTarget));
        Coroutine damageFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeRate,
            afterRate,
            actualDamage,
            directionToTarget
        ));

        Coroutine afterPierceCameraCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            targetOriginalPosition + allyAttackDashCameraOffset,
            allyAttackDashZoomInSize,
            strongDashThroughTime,
            TweenEase.OutCubic
        ));

        yield return TweenTwoUnitsPosition(
            attackerTransform,
            impactPosition,
            pierceEndPosition,
            targetTransform,
            targetOriginalPosition,
            targetKnockbackPosition,
            strongDashThroughTime,
            TweenEase.OutCubic
        );

        yield return afterPierceCameraCoroutine;
        yield return impactShakeCoroutine;
        SetUnitHitVisual(attacker, false);

        Coroutine returnCameraCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            allyAttackBaseCameraPosition,
            allyAttackReturnZoomOutSize,
            allyAttackDefaultCameraRotation,
            allyAttackCameraReturnTime,
            TweenEase.SmoothSine
        ));

        Coroutine returnUnitsCoroutine = StartCoroutine(TweenTwoUnitsPosition(
            attackerTransform,
            pierceEndPosition,
            attackerOriginalPosition,
            targetTransform,
            targetKnockbackPosition,
            targetOriginalPosition,
            strongReturnTime,
            TweenEase.SmoothSine
        ));

        yield return returnUnitsCoroutine;
        yield return returnCameraCoroutine;
        yield return damageFeedbackCoroutine;

        attackerTransform.position = attackerOriginalPosition;
        attackerTransform.rotation = Quaternion.Euler(attackerOriginalEuler);
        attackerTransform.localScale = attackerOriginalScale;
        targetTransform.position = targetOriginalPosition;
        targetTransform.rotation = Quaternion.Euler(targetOriginalEuler);
        targetTransform.localScale = targetOriginalScale;
        SetUnitHitVisual(attacker, false);
        battleCamera.transform.position = allyAttackBaseCameraPosition;
        battleCamera.orthographicSize = normalCameraSize;
        battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
    }

}
