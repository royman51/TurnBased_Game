
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
    private void PlayHealSound()
    {
        if (healSound == null)
        {
            return;
        }

        if (healAudioSource == null)
        {
            SetupHealAudioSource();
        }

        if (healAudioSource == null)
        {
            return;
        }

        healAudioSource.pitch = 1f;
        healAudioSource.PlayOneShot(healSound, healSoundVolume);
    }

    private IEnumerator PlayItemThrowAndCameraFollow(
        Transform itemObject,
        Vector3 originalLocalPosition,
        Quaternion originalLocalRotation,
        Vector3 originalLocalScale
    )
    {
        float duration = Mathf.Max(0.01f, itemThrowTime);
        float timer = 0f;
        float startCameraSize = battleCamera.orthographicSize;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float moveT = BattleEaseUtility.GetEase(rawT, TweenEase.SmoothSine);
            float height = Mathf.Sin(rawT * Mathf.PI) * itemThrowHeight;

            itemObject.localPosition = originalLocalPosition + Vector3.up * height;
            itemObject.localRotation = originalLocalRotation * Quaternion.Euler(0f, 360f * itemThrowSpinCount * rawT, 0f);
            itemObject.localScale = originalLocalScale;

            Vector3 desiredCameraPosition = new Vector3(
                itemObject.position.x,
                itemObject.position.y + itemCameraOffset.y,
                itemCameraOffset.z
            );

            battleCamera.transform.position = Vector3.Lerp(
                battleCamera.transform.position,
                desiredCameraPosition,
                Mathf.Clamp01(Time.deltaTime * itemCameraFollowSpeed)
            );

            battleCamera.orthographicSize = Mathf.Lerp(startCameraSize, itemCameraZoomSize, moveT);
            battleCamera.transform.rotation = Quaternion.Slerp(
                battleCamera.transform.rotation,
                Quaternion.Euler(allyAttackDefaultCameraRotation),
                Mathf.Clamp01(Time.deltaTime * 8f)
            );

            yield return null;
        }

        itemObject.localPosition = originalLocalPosition;
        itemObject.localRotation = originalLocalRotation;
        itemObject.localScale = originalLocalScale;
    }

    private IEnumerator SpinAndFadeItemObject(
        Transform itemObject,
        Vector3 originalLocalPosition,
        Quaternion originalLocalRotation,
        Vector3 originalLocalScale,
        List<SpriteRenderer> spriteRenderers,
        List<Graphic> uiGraphics,
        List<Color> spriteOriginalColors,
        List<Color> graphicOriginalColors
    )
    {
        if (itemObject == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, itemFadeSpinTime);
        float timer = 0f;
        Vector3 popScale = originalLocalScale * 1.18f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float t = BattleEaseUtility.GetEase(rawT, TweenEase.OutCubic);

            itemObject.localPosition = originalLocalPosition;
            itemObject.localRotation = originalLocalRotation * Quaternion.Euler(0f, 360f * itemFadeSpinCount * t, 0f);
            itemObject.localScale = Vector3.Lerp(originalLocalScale, popScale, Mathf.Sin(rawT * Mathf.PI));

            SetItemVisualAlpha(spriteRenderers, uiGraphics, spriteOriginalColors, graphicOriginalColors, 1f - t);

            yield return null;
        }

        SetItemVisualAlpha(spriteRenderers, uiGraphics, spriteOriginalColors, graphicOriginalColors, 0f);
    }

    private void SetItemVisualAlpha(
        List<SpriteRenderer> spriteRenderers,
        List<Graphic> uiGraphics,
        List<Color> spriteOriginalColors,
        List<Color> graphicOriginalColors,
        float alphaMultiplier
    )
    {
        alphaMultiplier = Mathf.Clamp01(alphaMultiplier);

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] == null)
            {
                continue;
            }

            Color color = i < spriteOriginalColors.Count ? spriteOriginalColors[i] : Color.white;
            color.a *= alphaMultiplier;
            spriteRenderers[i].color = color;
        }

        for (int i = 0; i < uiGraphics.Count; i++)
        {
            if (uiGraphics[i] == null)
            {
                continue;
            }

            Color color = i < graphicOriginalColors.Count ? graphicOriginalColors[i] : Color.white;
            color.a *= alphaMultiplier;
            uiGraphics[i].color = color;
        }
    }

    private IEnumerator ShowHealHealthInfo(BattleUnit unit, float beforeRate, float afterRate, int healAmount)
    {
        if (unit == null || !unitMenus.ContainsKey(unit))
        {
            yield break;
        }

        BattleActionMenuUI menu = unitMenus[unit];
        menu.ShowUnitInfo(false);
        yield return menu.Fade(true);

        yield return menu.TweenHealthFill(beforeRate, afterRate, healthDecreaseTweenTime);
        yield return new WaitForSeconds(Mathf.Max(0f, healInfoStayTime));
        yield return menu.Fade(false);
    }

    private int HealBattleUnit(BattleUnit unit, int healAmount, out float beforeRate, out float afterRate)
    {
        beforeRate = unit != null ? unit.HealthRate : 0f;
        afterRate = beforeRate;

        if (unit == null || healAmount <= 0)
        {
            return 0;
        }

        float maxHealth;
        MemberInfo maxMember;
        bool hasMaxHealth = TryGetNumericMember(unit, new string[]
        {
            "MaxHealth", "maxHealth", "m_maxHealth", "maxHp", "maxHP", "MaxHP", "m_maxHp"
        }, out maxHealth, out maxMember);

        float currentHealth;
        MemberInfo currentMember;
        bool hasCurrentHealth = TryGetNumericMember(unit, new string[]
        {
            "CurrentHealth", "currentHealth", "m_currentHealth", "Health", "health", "HP", "hp", "CurrentHP", "currentHP", "currentHp", "m_hp"
        }, out currentHealth, out currentMember);

        MethodInfo healMethod = FindMethodWithIntParameter(unit, new string[]
        {
            "Heal", "Recover", "RecoverHealth", "RestoreHealth", "AddHealth", "IncreaseHealth"
        });

        if (healMethod != null)
        {
            object result = healMethod.Invoke(unit, new object[] { healAmount });
            afterRate = unit.HealthRate;

            if (result is int intResult)
            {
                return Mathf.Max(0, intResult);
            }

            if (result is float floatResult)
            {
                return Mathf.Max(0, Mathf.RoundToInt(floatResult));
            }

            if (hasMaxHealth)
            {
                return Mathf.Max(0, Mathf.RoundToInt((afterRate - beforeRate) * maxHealth));
            }

            return healAmount;
        }

        if (hasCurrentHealth && hasMaxHealth && currentMember != null)
        {
            float clampedMax = Mathf.Max(1f, maxHealth);
            float newHealth = Mathf.Clamp(currentHealth + healAmount, 0f, clampedMax);
            bool setSucceeded = TrySetNumericMember(unit, currentMember, newHealth);

            if (setSucceeded)
            {
                afterRate = unit.HealthRate;
                return Mathf.Max(0, Mathf.RoundToInt(newHealth - currentHealth));
            }
        }

        Debug.LogWarning("BattleUnit에서 체력 회복용 메서드/필드를 찾지 못했습니다. Heal, CurrentHealth, MaxHealth 중 하나를 확인해주세요.");
        return 0;
    }

    private MethodInfo FindMethodWithIntParameter(object target, string[] methodNames)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        foreach (string methodName in methodNames)
        {
            MethodInfo method = type.GetMethod(methodName, flags, null, new System.Type[] { typeof(int) }, null);

            if (method != null)
            {
                return method;
            }
        }

        return null;
    }

    private bool TryGetNumericMember(object target, string[] names, out float value, out MemberInfo member)
    {
        value = 0f;
        member = null;

        if (target == null)
        {
            return false;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        System.Type type = target.GetType();

        foreach (string name in names)
        {
            FieldInfo field = type.GetField(name, flags);

            if (field != null && TryConvertToFloat(field.GetValue(target), out value))
            {
                member = field;
                return true;
            }

            PropertyInfo property = type.GetProperty(name, flags);

            if (property != null && property.CanRead && TryConvertToFloat(property.GetValue(target, null), out value))
            {
                member = property;
                return true;
            }
        }

        return false;
    }

    private bool TrySetNumericMember(object target, MemberInfo member, float value)
    {
        if (target == null || member == null)
        {
            return false;
        }

        try
        {
            if (member is FieldInfo field)
            {
                object converted = ConvertFloatToNumericType(value, field.FieldType);

                if (converted == null)
                {
                    return false;
                }

                field.SetValue(target, converted);
                return true;
            }

            if (member is PropertyInfo property)
            {
                MethodInfo setter = property.GetSetMethod(true);

                if (setter == null)
                {
                    return false;
                }

                object converted = ConvertFloatToNumericType(value, property.PropertyType);

                if (converted == null)
                {
                    return false;
                }

                setter.Invoke(target, new object[] { converted });
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private bool TryConvertToFloat(object rawValue, out float value)
    {
        value = 0f;

        if (rawValue == null)
        {
            return false;
        }

        if (rawValue is int intValue)
        {
            value = intValue;
            return true;
        }

        if (rawValue is float floatValue)
        {
            value = floatValue;
            return true;
        }

        if (rawValue is double doubleValue)
        {
            value = (float)doubleValue;
            return true;
        }

        if (rawValue is long longValue)
        {
            value = longValue;
            return true;
        }

        return false;
    }

    private object ConvertFloatToNumericType(float value, System.Type targetType)
    {
        if (targetType == typeof(int))
        {
            return Mathf.RoundToInt(value);
        }

        if (targetType == typeof(float))
        {
            return value;
        }

        if (targetType == typeof(double))
        {
            return (double)value;
        }

        if (targetType == typeof(long))
        {
            return (long)Mathf.RoundToInt(value);
        }

        return null;
    }

}
