using System;

public static class BattleEvents
{
    // 옵저버 패턴
    public static event Action<int> OnTurnStarted;
    public static event Action<BattleUnit, BattleActionType> OnActionPlanned;
    public static event Action<int, int, bool> OnPlanCountChanged;
    public static event Action<BattleUnit, int, float, float> OnUnitDamaged;

    public static void RaiseTurnStarted(int turn)
    {
        OnTurnStarted?.Invoke(turn);
    }

    public static void RaiseActionPlanned(BattleUnit unit, BattleActionType actionType)
    {
        OnActionPlanned?.Invoke(unit, actionType);
    }

    public static void RaisePlanCountChanged(int plannedCount, int aliveCount, bool ready)
    {
        OnPlanCountChanged?.Invoke(plannedCount, aliveCount, ready);
    }

    public static void RaiseUnitDamaged(BattleUnit unit, int damage, float beforeRate, float afterRate)
    {
        OnUnitDamaged?.Invoke(unit, damage, beforeRate, afterRate);
    }
}