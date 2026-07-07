using UnityEngine;

public enum BattleUnitSide
{
    Team,
    Enemy
}

public enum BattleActionType
{
    Attack,
    Defense,
    Item,
    EgoResonance
}

public enum TweenEase
{
    InOut,
    OutCubic,
    OutBack,
    SmoothSine
}

public static class BattleEaseUtility
{
    public static float GetEase(float t, TweenEase ease)
    {
        t = Mathf.Clamp01(t);

        if (ease == TweenEase.OutCubic)
        {
            float reverse = 1f - t;
            return 1f - reverse * reverse * reverse;
        }

        if (ease == TweenEase.OutBack)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        if (ease == TweenEase.SmoothSine)
        {
            return 0.5f - Mathf.Cos(t * Mathf.PI) * 0.5f;
        }

        return t * t * (3f - 2f * t);
    }
}