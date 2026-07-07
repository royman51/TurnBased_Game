
using System.Collections;
using UnityEngine;

public interface IBattleCommand
{
    BattleUnit Actor { get; }
    BattleActionType ActionType { get; }

    IEnumerator Execute(BattleManager manager);
}

public abstract class BattleCommandBase : IBattleCommand
{
    public BattleUnit Actor { get; private set; }
    public BattleActionType ActionType { get; private set; }

    protected BattleCommandBase(BattleUnit actor, BattleActionType actionType)
    {
        Actor = actor;
        ActionType = actionType;
    }

    public abstract IEnumerator Execute(BattleManager manager);
}

// 커맨드 패턴
public class AttackBattleCommand : BattleCommandBase
{
    public AttackBattleCommand(BattleUnit actor) : base(actor, BattleActionType.Attack)
    {
    }

    public override IEnumerator Execute(BattleManager manager)
    {
        BattleUnit target = manager.GetFirstAliveEnemy();

        if (target == null)
        {
            yield break;
        }

        yield return manager.PlayBasicAttackMotion(Actor, target, true);
    }
}

// 커맨드 패턴
public class DefenseBattleCommand : BattleCommandBase
{
    public DefenseBattleCommand(BattleUnit actor) : base(actor, BattleActionType.Defense)
    {
    }

    public override IEnumerator Execute(BattleManager manager)
    {
        Debug.Log($"{Actor.gameObject.name} 방어 준비");
        yield return null;
    }
}

// 커맨드 패턴
public class ItemBattleCommand : BattleCommandBase
{
    public ItemBattleCommand(BattleUnit actor) : base(actor, BattleActionType.Item)
    {
    }

    public override IEnumerator Execute(BattleManager manager)
    {
        yield return manager.PlayItemHealMotion(Actor);
    }
}

// 커맨드 패턴
public class EgoResonanceBattleCommand : BattleCommandBase
{
    public EgoResonanceBattleCommand(BattleUnit actor) : base(actor, BattleActionType.EgoResonance)
    {
    }

    public override IEnumerator Execute(BattleManager manager)
    {
        BattleUnit target = manager.GetFirstAliveEnemy();

        if (target == null)
        {
            yield break;
        }

        if (!manager.CanUseEgoResonance(Actor))
        {
            Debug.Log($"{Actor.gameObject.name}은 이미 자아 공명을 사용했습니다.");
            yield break;
        }

        yield return manager.PlayStrongAttackMotion(Actor, target, true);
    }
}

public static class BattleCommandFactory
{
    public static IBattleCommand Create(BattleUnit actor, BattleActionType actionType)
    {
        switch (actionType)
        {
            case BattleActionType.Attack:
                return new AttackBattleCommand(actor);

            case BattleActionType.Defense:
                return new DefenseBattleCommand(actor);

            case BattleActionType.Item:
                return new ItemBattleCommand(actor);

            case BattleActionType.EgoResonance:
                return new EgoResonanceBattleCommand(actor);

            default:
                return new AttackBattleCommand(actor);
        }
    }
}
