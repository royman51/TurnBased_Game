// SPLIT BUILD: REAL_LATEST_THROW_TARGET_UI_RED_AFTERIMAGE_BUILD_V10_SPLIT
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
    private void SetupUIClickAudioSource()
    {
        if (uiClickAudioSource == null)
        {
            uiClickAudioSource = GetComponent<AudioSource>();
        }

        if (uiClickAudioSource == null)
        {
            uiClickAudioSource = gameObject.AddComponent<AudioSource>();
        }

        uiClickAudioSource.playOnAwake = false;
        uiClickAudioSource.spatialBlend = 0f;
    }

    private void PlayUIClickSound()
    {
        if (uiClickSound == null)
        {
            return;
        }

        if (uiClickAudioSource == null)
        {
            SetupUIClickAudioSource();
        }

        if (uiClickAudioSource == null)
        {
            return;
        }

        float minPitch = Mathf.Min(uiClickPitchRange.x, uiClickPitchRange.y);
        float maxPitch = Mathf.Max(uiClickPitchRange.x, uiClickPitchRange.y);

        uiClickAudioSource.pitch = Random.Range(minPitch, maxPitch);
        uiClickAudioSource.PlayOneShot(uiClickSound, uiClickSoundVolume);
    }

    private void SetupHitAudioSource()
    {
        if (hitAudioSource == null)
        {
            hitAudioSource = GetComponent<AudioSource>();
        }

        if (hitAudioSource == null)
        {
            hitAudioSource = gameObject.AddComponent<AudioSource>();
        }

        hitAudioSource.playOnAwake = false;
        hitAudioSource.spatialBlend = 0f;
    }

    private void PlayRandomHitSound(int damageAmount)
    {
        if (damageAmount <= 0 || hitAudioSource == null)
        {
            return;
        }

        AudioClip selectedClip = null;

        if (hitSoundA != null && hitSoundB != null)
        {
            selectedClip = Random.value < 0.5f ? hitSoundA : hitSoundB;
        }
        else if (hitSoundA != null)
        {
            selectedClip = hitSoundA;
        }
        else if (hitSoundB != null)
        {
            selectedClip = hitSoundB;
        }

        if (selectedClip == null)
        {
            return;
        }

        float minPitch = Mathf.Min(hitSoundPitchRange.x, hitSoundPitchRange.y);
        float maxPitch = Mathf.Max(hitSoundPitchRange.x, hitSoundPitchRange.y);

        hitAudioSource.pitch = Random.Range(minPitch, maxPitch);
        hitAudioSource.PlayOneShot(selectedClip, hitSoundVolume);
    }

    private void UpdatePlanningCameraMotion()
    {
        if (battleCamera == null || isSelectionCameraTweening) return;

        Vector3 targetEuler;

        if (isSelectingAction && currentSelectingUnit != null)
        {
            Vector2 mouseRatio = GetMouseRatioFromScreenCenter();
            float actionYawOffset = GetActionSelectionYawOffset(selectedMenuAction);
            targetEuler = allyAttackDefaultCameraRotation + new Vector3(
                -mouseRatio.y * cursorCameraPitchStrength,
                mouseRatio.x * cursorCameraYawStrength + actionYawOffset,
                -mouseRatio.x * cursorCameraRollStrength
            );
        }
        else
        {
            float sway = Mathf.Sin(Time.time * idleCameraSwaySpeed);
            targetEuler = allyAttackDefaultCameraRotation + new Vector3(0f, sway * idleCameraSwayYaw, sway * idleCameraSwayRoll);
        }

        battleCamera.transform.rotation = Quaternion.Slerp(
            battleCamera.transform.rotation,
            Quaternion.Euler(targetEuler),
            Mathf.Clamp01(Time.deltaTime * planningCameraRotationSmooth)
        );
    }

    private Vector2 GetMouseRatioFromScreenCenter()
    {
        Vector2 mouse = Input.mousePosition;
        float x = Screen.width > 0 ? (mouse.x / Screen.width - 0.5f) * 2f : 0f;
        float y = Screen.height > 0 ? (mouse.y / Screen.height - 0.5f) * 2f : 0f;
        return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
    }

    private float GetActionSelectionYawOffset(BattleActionType actionType)
    {
        switch (actionType)
        {
            case BattleActionType.Attack: return -actionButtonRotationStep;
            case BattleActionType.Defense: return -actionButtonRotationStep * 0.35f;
            case BattleActionType.Item: return actionButtonRotationStep * 0.35f;
            case BattleActionType.EgoResonance: return actionButtonRotationStep;
            default: return 0f;
        }
    }
}
