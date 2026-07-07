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

    private IEnumerator PlayEnemyAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        bool useVariant =
            enemyUseBasicVariantAttack &&
            Random.value <= Mathf.Clamp01(enemyBasicVariantAttackChance);

        if (useVariant)
        {
            yield return PlayEnemyBasicVariantAttackMotion(attacker, target, recordTeamDamage);
        }
        else
        {
            yield return PlayEnemyNormalAttackMotion(attacker, target, recordTeamDamage);
        }
    }

    private IEnumerator PlayEnemyBasicVariantAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

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
            directionToTarget = Vector3.left;
        }
        directionToTarget.Normalize();

        bool canParry =
            attacker.UnitSide == BattleUnitSide.Enemy &&
            target.UnitSide == BattleUnitSide.Team &&
            IsUnitDefending(target);

        Vector3 cameraStartPosition = battleCamera.transform.position;
        Vector3 cameraAttackPosition = new Vector3(
            (attackerOriginalPosition.x + targetOriginalPosition.x) * 0.5f,
            (attackerOriginalPosition.y + targetOriginalPosition.y) * 0.5f + 0.25f,
            enemyApproachCameraOffset.z
        );

        Vector3 standPosition = targetOriginalPosition - directionToTarget * Mathf.Max(0.1f, enemyBasicVariantStandDistance);
        Vector3 pullBackPosition = standPosition - directionToTarget * Mathf.Max(0.05f, enemyBasicVariantPullBackDistance);
        Vector3 firstImpactPosition = targetOriginalPosition - directionToTarget * 0.08f;
        Vector3 secondStartPosition = firstImpactPosition - directionToTarget * Mathf.Max(0.1f, enemyBasicVariantPullBackDistance * 0.75f);
        Vector3 secondImpactPosition = targetOriginalPosition + directionToTarget * 0.22f;

        Vector3 firstKnockbackPosition = targetOriginalPosition + directionToTarget * Mathf.Max(0f, enemyBasicVariantFirstKnockbackDistance);
        Vector3 secondKnockbackPosition = targetOriginalPosition + directionToTarget * Mathf.Max(0f, enemyBasicVariantSecondKnockbackDistance);

        SetUnitHitVisual(attacker, false);

        // 적 변형 기본 공격 접근: 일반 적 공격처럼 카메라가 적을 살짝 느리게 따라가며 줌인합니다.
        yield return TweenEnemyApproachWithLagCamera(
            attackerTransform,
            attackerOriginalPosition,
            standPosition,
            cameraStartPosition,
            enemyApproachCameraOffset,
            Mathf.Max(0.01f, enemyBasicVariantApproachTime),
            Mathf.Min(normalCameraSize, enemyBasicVariantCameraZoomSize)
        );

        Coroutine parryCoroutine = null;
        if (canParry)
        {
            parryCoroutine = StartCoroutine(WaitForParryInput());
        }

        Coroutine firstPullCameraCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            cameraAttackPosition,
            Mathf.Min(normalCameraSize, enemyBasicVariantCameraZoomSize + 0.25f),
            allyAttackDefaultCameraRotation,
            Mathf.Max(0.01f, enemyBasicVariantPullBackTime),
            TweenEase.SmoothSine
        ));

        yield return TweenTransformPositionRotationScale(
            attackerTransform,
            standPosition,
            pullBackPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            Mathf.Max(0.01f, enemyBasicVariantPullBackTime),
            TweenEase.SmoothSine,
            true
        );

        yield return firstPullCameraCoroutine;

        SetUnitHitVisual(attacker, true);

        yield return TweenEnemyBasicVariantDashWithLagCamera(
            attacker,
            attackerTransform,
            pullBackPosition,
            firstImpactPosition,
            targetOriginalPosition,
            Mathf.Max(0.01f, enemyBasicVariantDashTime),
            Mathf.Min(normalCameraSize, enemyBasicVariantImpactZoomSize)
        );

        if (parryCoroutine != null)
        {
            yield return parryCoroutine;
        }

        if (canParry && parryInputSucceeded)
        {
            float beforeEnemyRate = attacker.HealthRate;
            int parryDamage = attacker.TakeDamage(target.AttackDamage);
            PlayRandomHitSound(parryDamage);
            ShowDamageDisplay(attacker, parryDamage);
            float afterEnemyRate = attacker.HealthRate;

            StartCoroutine(PlayParryScreenEffect(-directionToTarget));
            SetUnitHitVisual(attacker, true);

            Coroutine damageFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
                attacker,
                beforeEnemyRate,
                afterEnemyRate,
                parryDamage,
                -directionToTarget
            ));

            yield return TweenTransformPositionRotationScale(
                attackerTransform,
                attackerTransform.position,
                attackerOriginalPosition,
                attackerTransform.rotation.eulerAngles.z,
                attackerOriginalEuler.z,
                attackerTransform.localScale,
                attackerOriginalScale,
                defenseHitEnemyReturnTime,
                TweenEase.OutCubic,
                true
            );

            SetUnitHitVisual(attacker, false);
            yield return damageFeedbackCoroutine;
            yield return TweenCameraPositionSizeRotation(
                allyAttackBaseCameraPosition,
                normalCameraSize,
                allyAttackDefaultCameraRotation,
                Mathf.Max(0.01f, cameraSmoothReturnTime),
                TweenEase.SmoothSine
            );
            yield break;
        }

        float beforeFirstRate = target.HealthRate;
        int firstDamage = target.TakeDamage(Mathf.Max(1, enemyBasicVariantFirstDamage));
        PlayRandomHitSound(firstDamage);
        ShowDamageDisplay(target, firstDamage);
        float afterFirstRate = target.HealthRate;

        Coroutine firstZoomCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraAttackPosition,
            Mathf.Min(normalCameraSize, enemyBasicVariantImpactZoomSize),
            Mathf.Max(0.01f, enemyFirstHitZoomTime),
            TweenEase.OutCubic
        ));

        Coroutine firstFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeFirstRate,
            afterFirstRate,
            firstDamage,
            directionToTarget
        ));

        Coroutine firstShakeCoroutine = StartCoroutine(PlayBasicAttackImpactCameraShake(directionToTarget));
        yield return TweenUnitRootAndVisualLaunch(
            target,
            targetVisualSnapshots,
            targetOriginalPosition,
            firstKnockbackPosition,
            targetOriginalPosition.y + Mathf.Max(0.05f, enemyBasicVariantLaunchHeight),
            Mathf.Max(0.01f, enemyBasicVariantDashTime + 0.08f)
        );
        yield return firstZoomCoroutine;
        yield return firstShakeCoroutine;

        SetUnitHitVisual(attacker, false);

        Coroutine secondPullCameraCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraAttackPosition,
            Mathf.Min(normalCameraSize, enemyBasicVariantCameraZoomSize),
            Mathf.Max(0.01f, enemyBasicVariantPullBackTime + enemyBasicVariantSecondWaitTime),
            TweenEase.SmoothSine
        ));

        yield return TweenTransformPositionRotationScale(
            attackerTransform,
            firstImpactPosition,
            secondStartPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            Mathf.Max(0.01f, enemyBasicVariantPullBackTime + enemyBasicVariantSecondWaitTime),
            TweenEase.SmoothSine,
            true
        );

        yield return secondPullCameraCoroutine;

        SetUnitHitVisual(attacker, true);

        Coroutine secondZoomCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            cameraAttackPosition,
            Mathf.Min(normalCameraSize, enemyBasicVariantImpactZoomSize - 0.15f),
            Mathf.Max(0.01f, enemyBasicVariantDashTime),
            TweenEase.OutCubic
        ));

        yield return TweenEnemyBasicVariantDashWithLagCamera(
            attacker,
            attackerTransform,
            secondStartPosition,
            secondImpactPosition,
            firstKnockbackPosition,
            Mathf.Max(0.01f, enemyBasicVariantDashTime),
            Mathf.Min(normalCameraSize, enemyBasicVariantImpactZoomSize - 0.15f)
        );

        float beforeSecondRate = target.HealthRate;
        int secondDamage = target.TakeDamage(Mathf.Max(1, enemyBasicVariantSecondDamage));
        PlayRandomHitSound(secondDamage);
        ShowDamageDisplay(target, secondDamage);
        float afterSecondRate = target.HealthRate;

        Coroutine secondFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeSecondRate,
            afterSecondRate,
            secondDamage,
            directionToTarget
        ));

        Coroutine secondShakeCoroutine = StartCoroutine(PlayHitCameraShake(directionToTarget));
        yield return TweenUnitRootAndVisualPosition(
            target,
            targetVisualSnapshots,
            firstKnockbackPosition,
            secondKnockbackPosition,
            Mathf.Max(0.01f, enemyBasicVariantDashTime + 0.12f),
            TweenEase.OutCubic
        );

        yield return secondZoomCoroutine;
        yield return secondShakeCoroutine;

        SetUnitHitVisual(attacker, false);

        Coroutine cameraReturnCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            allyAttackBaseCameraPosition,
            normalCameraSize,
            allyAttackDefaultCameraRotation,
            Mathf.Max(0.01f, enemyBasicVariantCameraReturnTime),
            TweenEase.SmoothSine
        ));

        Coroutine attackerReturnCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            secondImpactPosition,
            attackerOriginalPosition,
            attackerOriginalEuler.z,
            attackerOriginalEuler.z,
            attackerOriginalScale,
            attackerOriginalScale,
            Mathf.Max(0.01f, enemyBasicVariantReturnTime),
            TweenEase.SmoothSine,
            true
        ));

        yield return attackerReturnCoroutine;
        yield return cameraReturnCoroutine;
        yield return firstFeedbackCoroutine;
        yield return secondFeedbackCoroutine;

        attackerTransform.position = attackerOriginalPosition;
        attackerTransform.rotation = Quaternion.Euler(attackerOriginalEuler);
        attackerTransform.localScale = attackerOriginalScale;

        targetTransform.position = secondKnockbackPosition;
        targetTransform.rotation = Quaternion.Euler(targetOriginalEuler);
        targetTransform.localScale = targetOriginalScale;
        RestoreUnitVisualLocalPositions(targetVisualSnapshots);

        battleCamera.transform.position = allyAttackBaseCameraPosition;
        battleCamera.orthographicSize = normalCameraSize;
        battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
    }


    private IEnumerator TweenEnemyBasicVariantDashWithLagCamera(
        BattleUnit attackerUnit,
        Transform attackerTransform,
        Vector3 attackerStart,
        Vector3 attackerEnd,
        Vector3 cameraTargetWorldPosition,
        float duration,
        float targetCameraSize
    )
    {
        duration = Mathf.Max(0.01f, duration);

        Vector3 cameraStartPosition = battleCamera.transform.position;
        Vector3 cameraEndPosition = new Vector3(
            cameraTargetWorldPosition.x,
            cameraTargetWorldPosition.y + 0.28f,
            enemyApproachCameraOffset.z
        );

        float startSize = battleCamera.orthographicSize;
        Quaternion startRotation = battleCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(allyAttackDefaultCameraRotation);

        float timer = 0f;

        Coroutine afterimageCoroutine = null;
        if (attackerUnit != null)
        {
            afterimageCoroutine = StartCoroutine(SpawnAttackAfterimagesForDuration(attackerUnit, duration));
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            float cameraT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            if (attackerTransform != null)
            {
                attackerTransform.position = Vector3.Lerp(attackerStart, attackerEnd, moveT);
            }

            Vector3 desiredCameraPosition = Vector3.Lerp(cameraStartPosition, cameraEndPosition, cameraT);
            battleCamera.transform.position = Vector3.Lerp(
                battleCamera.transform.position,
                desiredCameraPosition,
                Mathf.Clamp01(Time.deltaTime * Mathf.Max(0.01f, enemyApproachCameraFollowSpeed + 1.4f))
            );

            battleCamera.orthographicSize = Mathf.Lerp(
                startSize,
                targetCameraSize,
                BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic)
            );

            battleCamera.transform.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                cameraT
            );

            yield return null;
        }

        if (attackerTransform != null)
        {
            attackerTransform.position = attackerEnd;
        }

        battleCamera.transform.position = cameraEndPosition;
        battleCamera.orthographicSize = targetCameraSize;
        battleCamera.transform.rotation = targetRotation;

        if (afterimageCoroutine != null)
        {
            yield return afterimageCoroutine;
        }
    }

    private IEnumerator PlayEnemyNormalAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

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
            directionToTarget = Vector3.left;
        }
        directionToTarget.Normalize();

        bool canParry =
            attacker.UnitSide == BattleUnitSide.Enemy &&
            target.UnitSide == BattleUnitSide.Team &&
            IsUnitDefending(target);

        Vector3 attackStandPosition = targetOriginalPosition - directionToTarget * Mathf.Max(0.1f, enemyApproachStandDistance);
        Vector3 cameraFollowStart = battleCamera.transform.position;
        Vector3 cameraArrivalPosition = new Vector3(
            attackerOriginalPosition.x + enemyApproachCameraOffset.x,
            attackerOriginalPosition.y + enemyApproachCameraOffset.y,
            enemyApproachCameraOffset.z
        );

        // 적 차례 시작: 카메라가 적을 살짝 느리게 따라가며 도착까지 서서히 줌인합니다.
        yield return TweenEnemyApproachWithLagCamera(
            attackerTransform,
            attackerOriginalPosition,
            attackStandPosition,
            cameraFollowStart,
            enemyApproachCameraOffset,
            Mathf.Max(0.01f, Mathf.Min(enemyApproachTime, 0.85f)),
            Mathf.Min(normalCameraSize, enemyApproachCameraSize)
        );

        Coroutine parryCoroutine = null;
        if (canParry)
        {
            parryCoroutine = StartCoroutine(WaitForParryInput());
        }

        if (parryCoroutine != null)
        {
            yield return parryCoroutine;
        }

        if (canParry && parryInputSucceeded)
        {
            float beforeEnemyRate = attacker.HealthRate;
            int parryDamage = attacker.TakeDamage(target.AttackDamage);
            PlayRandomHitSound(parryDamage);
            ShowDamageDisplay(attacker, parryDamage);
            float afterEnemyRate = attacker.HealthRate;

            StartCoroutine(PlayParryScreenEffect(-directionToTarget));
            SetUnitHitVisual(attacker, true);

            Coroutine damageFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
                attacker,
                beforeEnemyRate,
                afterEnemyRate,
                parryDamage,
                -directionToTarget
            ));

            yield return TweenTransformPositionRotationScale(
                attackerTransform,
                attackerTransform.position,
                attackerOriginalPosition,
                attackerTransform.rotation.eulerAngles.z,
                attackerOriginalEuler.z,
                attackerTransform.localScale,
                attackerOriginalScale,
                defenseHitEnemyReturnTime,
                TweenEase.OutCubic,
                true
            );

            SetUnitHitVisual(attacker, false);
            yield return damageFeedbackCoroutine;

            attackerTransform.position = attackerOriginalPosition;
            attackerTransform.rotation = Quaternion.Euler(attackerOriginalEuler);
            attackerTransform.localScale = attackerOriginalScale;

            yield return TweenCameraPositionSizeRotation(
                allyAttackBaseCameraPosition,
                normalCameraSize,
                allyAttackDefaultCameraRotation,
                Mathf.Max(0.01f, cameraSmoothReturnTime),
                TweenEase.SmoothSine
            );
            yield break;
        }

        Vector3 impactCameraPosition = new Vector3(
            targetOriginalPosition.x,
            targetOriginalPosition.y + 0.35f,
            enemyApproachCameraOffset.z
        );

        // 도착 후 카메라 크기를 기본값으로 빠르게 맞춥니다.
        yield return TweenCameraPositionSizeRotation(
            impactCameraPosition,
            normalCameraSize,
            allyAttackDefaultCameraRotation,
            Mathf.Max(0.01f, enemyArrivalToDefaultSizeTime),
            TweenEase.OutCubic
        );

        // 첫 타격 직전부터 적의 Hit 이미지를 켭니다.
        // 이 이미지는 첫 찌르기 → 들어올림 → 던지기 직후까지 유지됩니다.
        SetUnitHitVisual(attacker, true);

        // 첫 타격: 10 피해와 동시에 빠른 줌인.
        float beforeFirstRate = target.HealthRate;
        int firstDamage = target.TakeDamage(Mathf.Max(1, enemyFirstHitDamage));
        PlayRandomHitSound(firstDamage);
        ShowDamageDisplay(target, firstDamage);
        float afterFirstRate = target.HealthRate;

        Coroutine firstZoomCoroutine = StartCoroutine(TweenCameraPositionAndSize(
            impactCameraPosition,
            Mathf.Min(normalCameraSize, enemyFirstHitZoomSize),
            Mathf.Max(0.01f, enemyFirstHitZoomTime),
            TweenEase.OutCubic
        ));

        Coroutine firstFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeFirstRate,
            afterFirstRate,
            firstDamage,
            directionToTarget
        ));

        StartCoroutine(PlayHitCameraShake(directionToTarget));
        yield return firstZoomCoroutine;

        // 5초 동안 던지기 준비: 카메라는 점점 줌아웃되고, 적은 -25도까지 뚝뚝 끊기며 기울고,
        // 아군은 위로 끌어올려집니다.
        // 이 구간은 무기가 몸에 걸려있는 연출이므로 Hit 이미지를 계속 유지합니다.
        SetUnitHitVisual(attacker, true);
        Vector3 targetLiftPosition = targetOriginalPosition + Vector3.up * Mathf.Max(0f, enemyThrowLiftHeight);
        yield return PlayEnemyThrowCharge(
            attacker,
            target,
            targetVisualSnapshots,
            attackStandPosition,
            targetOriginalPosition,
            targetLiftPosition,
            impactCameraPosition
        );

        // 오른쪽으로 날립니다.
        // 포물선 대신, 들어올리기 전의 높이까지 부드럽게 내려오며 오른쪽으로 이동합니다.
        // 날아가는 동안 화면 흔들림과 적 Y 360도 회전을 동시에 재생합니다.
        Vector3 throwStartPosition = targetLiftPosition;
        Vector3 throwEndPosition = new Vector3(
            enemyThrowTargetWorldPosition.x,
            targetOriginalPosition.y,
            enemyThrowTargetWorldPosition.z
        );

        Coroutine throwShakeCoroutine = StartCoroutine(PlayEnemyThrowReleaseCameraShake(Vector3.right));
        Coroutine enemySpinCoroutine = StartCoroutine(TweenEnemyThrowSpinY(attackerTransform, attackerTransform.rotation.eulerAngles, Mathf.Max(0.01f, enemyThrowSpinYTime)));

        yield return TweenUnitRootAndVisualThrowToHeight(
            target,
            targetVisualSnapshots,
            throwStartPosition,
            throwEndPosition,
            Mathf.Max(0.01f, Mathf.Min(enemyThrowTime, 0.55f))
        );

        yield return throwShakeCoroutine;
        yield return enemySpinCoroutine;

        // 던지기 동작이 끝나고, 들어올리기 전 높이에 도달하면 찌르고 있는 자세를 해제합니다.
        SetUnitHitVisual(attacker, false);

        // 들기 전 높이에 도달한 직후 25 피해.
        float beforeThrowRate = target.HealthRate;
        int throwDamage = target.TakeDamage(Mathf.Max(1, enemyThrowDamage));
        PlayRandomHitSound(throwDamage);
        ShowDamageDisplay(target, throwDamage);
        float afterThrowRate = target.HealthRate;

        Coroutine throwFeedbackCoroutine = StartCoroutine(PlayDamageFeedbackWithFlashOnly(
            target,
            beforeThrowRate,
            afterThrowRate,
            throwDamage,
            Vector3.right
        ));

        StartCoroutine(PlayHitCameraShake(Vector3.right));

        // 피격 직후 빠른 줌인.
        yield return TweenCameraPositionAndSize(
            new Vector3(throwEndPosition.x, throwEndPosition.y + 0.3f, enemyApproachCameraOffset.z),
            Mathf.Min(normalCameraSize, enemyThrowImpactZoomSize),
            Mathf.Max(0.01f, enemyThrowImpactZoomTime),
            TweenEase.OutCubic
        );

        // 천천히 기본값으로 복귀.
        Coroutine cameraReturnCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            allyAttackBaseCameraPosition,
            normalCameraSize,
            allyAttackDefaultCameraRotation,
            Mathf.Max(0.01f, enemyThrowCameraReturnTime),
            TweenEase.SmoothSine
        ));

        Coroutine attackerReturnCoroutine = StartCoroutine(TweenTransformPositionRotationScale(
            attackerTransform,
            attackStandPosition,
            attackerOriginalPosition,
            attackerTransform.rotation.eulerAngles.z,
            attackerOriginalEuler.z,
            attackerTransform.localScale,
            attackerOriginalScale,
            Mathf.Max(0.01f, attackReturnTime),
            TweenEase.SmoothSine,
            true
        ));

        yield return attackerReturnCoroutine;
        yield return cameraReturnCoroutine;
        yield return firstFeedbackCoroutine;
        yield return throwFeedbackCoroutine;

        attackerTransform.position = attackerOriginalPosition;
        attackerTransform.rotation = Quaternion.Euler(attackerOriginalEuler);
        attackerTransform.localScale = attackerOriginalScale;

        targetTransform.position = throwEndPosition;
        targetTransform.rotation = Quaternion.Euler(targetOriginalEuler);
        targetTransform.localScale = targetOriginalScale;
        RestoreUnitVisualLocalPositions(targetVisualSnapshots);

        SetUnitHitVisual(attacker, false);
        battleCamera.transform.position = allyAttackBaseCameraPosition;
        battleCamera.orthographicSize = normalCameraSize;
        battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
    }

    private IEnumerator TweenEnemyApproachWithLagCamera(
        Transform enemyTransform,
        Vector3 enemyStart,
        Vector3 enemyEnd,
        Vector3 cameraStart,
        Vector3 cameraOffset,
        float duration,
        float targetCameraSize
    )
    {
        duration = Mathf.Max(0.01f, duration);
        float startSize = battleCamera.orthographicSize;
        float timer = 0f;

        Coroutine afterimageCoroutine = null;
        BattleUnit afterimageUnit = enemyTransform != null ? enemyTransform.GetComponent<BattleUnit>() : null;
        if (afterimageUnit != null)
        {
            afterimageCoroutine = StartCoroutine(SpawnAttackAfterimagesForDuration(afterimageUnit, duration));
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            float cameraT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            if (enemyTransform != null)
            {
                enemyTransform.position = Vector3.Lerp(enemyStart, enemyEnd, moveT);
            }

            Vector3 desiredCameraPosition = Vector3.Lerp(
                cameraStart,
                new Vector3(enemyTransform.position.x + cameraOffset.x, enemyTransform.position.y + cameraOffset.y, cameraOffset.z),
                cameraT
            );

            battleCamera.transform.position = Vector3.Lerp(
                battleCamera.transform.position,
                desiredCameraPosition,
                Mathf.Clamp01(Time.deltaTime * Mathf.Max(0.1f, enemyApproachCameraFollowSpeed))
            );

            battleCamera.orthographicSize = Mathf.Lerp(startSize, targetCameraSize, cameraT);
            battleCamera.transform.rotation = Quaternion.Slerp(
                battleCamera.transform.rotation,
                Quaternion.Euler(allyAttackDefaultCameraRotation),
                Mathf.Clamp01(Time.deltaTime * 8f)
            );

            yield return null;
        }

        if (enemyTransform != null)
        {
            enemyTransform.position = enemyEnd;
        }

        if (afterimageCoroutine != null)
        {
            StopCoroutine(afterimageCoroutine);
        }
    }

    private IEnumerator PlayEnemyThrowCharge(
        BattleUnit enemy,
        BattleUnit target,
        List<UnitVisualSnapshot> targetVisualSnapshots,
        Vector3 enemyStartPosition,
        Vector3 targetStartPosition,
        Vector3 targetLiftPosition,
        Vector3 cameraAnchorPosition
    )
    {
        float duration = Mathf.Max(0.01f, Mathf.Min(enemyThrowChargeTime, 3.25f));
        RebaseUnitVisualWorldPositions(targetVisualSnapshots);
        // 기존에는 enemyThrowStepInterval 값으로 각도를 계단식으로 끊었지만,
        // 지금은 전체 던지기 준비가 더 자연스럽게 보이도록 부드러운 보간만 사용합니다.

        // 던지기 준비 구간 전체에서 적은 찌르고 있는 Hit 이미지 상태를 유지합니다.
        SetUnitHitVisual(enemy, true);

        Transform enemyTransform = enemy.transform;
        Vector3 enemyOriginalEuler = enemyTransform.rotation.eulerAngles;
        Vector3 cameraStartPosition = battleCamera.transform.position;
        float cameraStartSize = battleCamera.orthographicSize;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float smoothT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);
            float tiltZ = Mathf.LerpAngle(enemyOriginalEuler.z, enemyThrowTiltZ, smoothT);
            enemyTransform.rotation = Quaternion.Euler(enemyOriginalEuler.x, enemyOriginalEuler.y, tiltZ);

            Vector3 liftedPosition = Vector3.Lerp(targetStartPosition, targetLiftPosition, smoothT);
            ApplyUnitRootAndVisualWorldPosition(target, targetVisualSnapshots, targetStartPosition, liftedPosition);

            Vector3 desiredCameraPosition = Vector3.Lerp(
                cameraStartPosition,
                new Vector3(cameraAnchorPosition.x, cameraAnchorPosition.y + 0.7f, enemyApproachCameraOffset.z),
                smoothT
            );

            battleCamera.transform.position = desiredCameraPosition;
            battleCamera.orthographicSize = Mathf.Lerp(cameraStartSize, enemyThrowChargeZoomOutSize, smoothT);
            battleCamera.transform.rotation = Quaternion.Slerp(
                battleCamera.transform.rotation,
                Quaternion.Euler(allyAttackDefaultCameraRotation),
                Mathf.Clamp01(Time.deltaTime * 4f)
            );

            yield return null;
        }

        enemyTransform.rotation = Quaternion.Euler(enemyOriginalEuler.x, enemyOriginalEuler.y, enemyThrowTiltZ);
        ApplyUnitRootAndVisualWorldPosition(target, targetVisualSnapshots, targetStartPosition, targetLiftPosition);
        battleCamera.orthographicSize = enemyThrowChargeZoomOutSize;
    }


    private IEnumerator TweenUnitRootAndVisualThrowToHeight(
        BattleUnit unit,
        List<UnitVisualSnapshot> visualSnapshots,
        Vector3 startPosition,
        Vector3 endPosition,
        float duration
    )
    {
        duration = Mathf.Max(0.01f, duration);
        RebaseUnitVisualWorldPositions(visualSnapshots);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);

            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, t);
            ApplyUnitRootAndVisualWorldPosition(unit, visualSnapshots, startPosition, currentPosition);

            Vector3 desiredCameraPosition = new Vector3(
                currentPosition.x,
                currentPosition.y + 0.25f,
                enemyApproachCameraOffset.z
            );

            battleCamera.transform.position = Vector3.Lerp(
                battleCamera.transform.position,
                desiredCameraPosition,
                Mathf.Clamp01(Time.deltaTime * 4.5f)
            );

            yield return null;
        }

        ApplyUnitRootAndVisualWorldPosition(unit, visualSnapshots, startPosition, endPosition);
    }

    private IEnumerator PlayEnemyThrowReleaseCameraShake(Vector3 hitDirection)
    {
        Vector3 originalPosition = battleCamera.transform.position;
        Quaternion originalRotation = battleCamera.transform.rotation;
        Vector3 sideDirection = new Vector3(-hitDirection.y, hitDirection.x, 0f);

        if (sideDirection.sqrMagnitude <= 0.0001f)
        {
            sideDirection = Vector3.up;
        }

        sideDirection.Normalize();

        float duration = Mathf.Max(0.01f, enemyThrowReleaseShakeDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float power = 1f - rawT;
            float waveA = Mathf.Sin(timer * enemyThrowReleaseShakeSpeed);
            float waveB = Mathf.Sin(timer * enemyThrowReleaseShakeSpeed * 1.37f);

            Vector3 offset =
                sideDirection * waveA * enemyThrowReleaseShakeAmount * power +
                hitDirection.normalized * waveB * enemyThrowReleaseShakeAmount * 0.55f * power;

            float rotationOffset = waveA * enemyThrowReleaseShakeRotationAmount * power;

            battleCamera.transform.position = originalPosition + offset;
            battleCamera.transform.rotation = originalRotation * Quaternion.Euler(0f, 0f, rotationOffset);

            yield return null;
        }

        battleCamera.transform.position = originalPosition;
        battleCamera.transform.rotation = originalRotation;
    }

    private IEnumerator TweenEnemyThrowSpinY(Transform enemyTransform, Vector3 startEuler, float duration)
    {
        if (enemyTransform == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float rawT = Mathf.Clamp01(timer / duration);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);
            float y = startEuler.y + 360f * t;

            enemyTransform.rotation = Quaternion.Euler(startEuler.x, y, startEuler.z);
            yield return null;
        }

        enemyTransform.rotation = Quaternion.Euler(startEuler.x, startEuler.y, startEuler.z);
    }

}
