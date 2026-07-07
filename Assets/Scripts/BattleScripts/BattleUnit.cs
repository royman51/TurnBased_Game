using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    [Header("유닛 종류")]
    [SerializeField] private BattleUnitSide unitSide = BattleUnitSide.Team;

    [Header("체력")]
    [SerializeField] private int maxHp = 100;
    [SerializeField] private int currentHp = 100;

    [Header("공격 데미지")]
    [SerializeField] private int attackDamage = 15;

    public BattleUnitSide UnitSide => unitSide;
    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public int AttackDamage => attackDamage;
    public bool IsDead => currentHp <= 0;

    public float HealthRate
    {
        get
        {
            if (maxHp <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)currentHp / maxHp);
        }
    }

    private void Awake()
    {
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        attackDamage = Mathf.Max(0, attackDamage);
    }

    public void SetSide(BattleUnitSide newSide)
    {
        unitSide = newSide;
    }

    public int TakeDamage(int damage)
    {
        if (IsDead)
        {
            return 0;
        }

        damage = Mathf.Max(0, damage);

        float beforeRate = HealthRate;

        int beforeHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - damage);
        int actualDamage = beforeHp - currentHp;

        float afterRate = HealthRate;

        Debug.Log($"{gameObject.name}이/가 {actualDamage} 피해를 받음. 현재 체력: {currentHp}/{maxHp}");

        BattleEvents.RaiseUnitDamaged(this, actualDamage, beforeRate, afterRate);

        if (IsDead)
        {
            Debug.Log($"{gameObject.name} 사망");
        }

        return actualDamage;
    }
}