// SPLIT BUILD: V20_ENEMY_BASIC_VARIANT
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// REAL_LATEST_THROW_TARGET_UI_RED_AFTERIMAGE_BUILD - 던지기 목표 좌표 + UI 클릭음 + 적 잔상 빨간색
public partial class BattleManager : MonoBehaviour
{
    private class UnitVisualSnapshot
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Vector3 WorldPosition;
    }

    // 싱글톤 패턴
    public static BattleManager Instance { get; private set; }


    [Header("카메라")]
    [SerializeField] private Camera battleCamera;
    [SerializeField] private float firstTurnCameraStartSize = 0.01f;
    [SerializeField] private float normalCameraSize = 5f;
    [SerializeField] private float focusCameraSize = 2.85f;
    [SerializeField] private float attackCameraSize = 4.2f;
    [SerializeField] private Vector2 cameraFocusOffset = new Vector2(0f, 0.5f);
    [SerializeField] private float cameraIntroTweenTime = 0.65f;
    [SerializeField] private float cameraFocusTweenTime = 0.45f;
    [SerializeField] private float cameraSmoothReturnTime = 0.58f;

    [Header("공격 중 카메라 회전")]
    [SerializeField] private Vector3 cameraCenterRotation = new Vector3(5.012f, 0f, 0.783f);
    [SerializeField] private Vector3 cameraLeftRotation = new Vector3(5.000f, -2.007f, -0.175f);
    [SerializeField] private Vector3 cameraRightRotation = new Vector3(4.977f, 3.391f, 0.294f);
    [SerializeField] private Vector3 cameraUpRotation = new Vector3(4.709f, 0f, 0f);
    [SerializeField] private Vector3 cameraDownRotation = new Vector3(5.309f, 0f, 0f);
    [SerializeField] private float cameraAttackRotationStartTime = 0.22f;
    [SerializeField] private float cameraAttackRotationToCenterTime = 0.36f;
    [SerializeField] private float cameraAttackRotationToEndTime = 0.42f;
    [SerializeField] private float cameraAttackRotationRestoreTime = 0.28f;
    [SerializeField] private float cameraAttackRotationStrength = 0.85f;

    [Header("대기 / 선택 중 카메라")]
    [SerializeField] private float idleCameraSwaySpeed = 0.65f;
    [SerializeField] private float idleCameraSwayYaw = 4.5f;
    [SerializeField] private float idleCameraSwayRoll = 0.35f;
    [SerializeField] private float planningCameraRotationSmooth = 8f;
    [SerializeField] private float cursorCameraYawStrength = 8.5f;
    [SerializeField] private float cursorCameraPitchStrength = 2.2f;
    [SerializeField] private float cursorCameraRollStrength = 0.85f;
    [SerializeField] private float actionButtonRotationStep = 3.2f;

    [Header("적중 카메라 흔들림")]
    [SerializeField] private float hitCameraShakeDuration = 0.16f;
    [SerializeField] private float hitCameraShakePositionAmount = 0.045f;
    [SerializeField] private float hitCameraShakeRotationAmount = 0.55f;
    [SerializeField] private float hitCameraShakeSpeed = 48f;

    [Header("패링")]
    [SerializeField] private float parryInputWindowTime = 0.22f;
    [SerializeField] private Color parryFlashColor = new Color(1f, 0.95f, 0.45f, 0.85f);
    [SerializeField] private float parryFlashInTime = 0.035f;
    [SerializeField] private float parryFlashOutTime = 0.18f;
    [SerializeField] private float parryCameraShakeDuration = 0.24f;
    [SerializeField] private float parryCameraShakePositionAmount = 0.12f;
    [SerializeField] private float parryCameraShakeRotationAmount = 1.7f;
    [SerializeField] private float parryCameraShakeSpeed = 72f;

    [Header("방어 적중 연출")]
    [SerializeField] private float defenseHitEnemyReturnTime = 0.22f;

    [Header("턴 표시")]
    [SerializeField] private int currentTurn = 1;
    [SerializeField] private float turnBannerFadeTime = 0.25f;
    [SerializeField] private float turnBannerStayTime = 0.8f;

    [Header("빌보드 행동 메뉴")]
    [SerializeField] private Vector3 actionMenuWorldOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float actionMenuWorldScale = 0.0045f;
    [SerializeField] private bool actionMenuFaceCamera = true;
    [SerializeField] private int actionMenuSortingOrder = 100;

    [Header("상태창")]
    [SerializeField] private Color portraitEmptyColor = Color.white;
    [SerializeField] private float healthDecreaseTweenTime = 0.28f;
    [SerializeField] private float damageInfoStayTime = 0.25f;
    [SerializeField] private float damagePanelShakeTime = 0.28f;
    [SerializeField] private float damagePanelShakePerDamage = 1.15f;
    [SerializeField] private float damagePanelMaxShake = 32f;

    [Header("행동 메뉴 색상")]
    [SerializeField] private Color normalMenuColor = new Color(0.08f, 0.06f, 0.08f, 0.75f);
    [SerializeField] private Color normalMenuTextColor = Color.white;
    [SerializeField] private Color selectedMenuColor = new Color(1.4f, 1.25f, 0.45f, 0.95f);
    [SerializeField] private Color selectedMenuTextColor = new Color(1.8f, 1.6f, 0.6f, 1f);
    [SerializeField] private float menuFadeTime = 0.18f;

    [Header("실행 버튼")]
    [SerializeField] private Color executeButtonLockedColor = new Color(0.35f, 0.35f, 0.35f, 0.65f);
    [SerializeField] private Color executeButtonReadyColor = new Color(1f, 0.92f, 0.35f, 0.95f);

    [Header("자아 공명 사용 제한")]
    [SerializeField] private bool egoResonanceOneUsePerUnit = true;
    [SerializeField] private float egoResonanceDisabledAlpha = 0.35f;

    [Header("공격 연출")]
    [SerializeField] private float attackMoveTime = 0.62f;
    [SerializeField] private float attackReadyPullTime = 0.22f;
    [SerializeField] private float attackDashTime = 0.16f;
    [SerializeField] private float attackImpactStopTime = 0.08f;
    [SerializeField] private float attackRecoilTime = 0.23f;
    [SerializeField] private float attackReturnTime = 0.62f;
    [SerializeField] private float attackStandDistance = 1.25f;
    [SerializeField] private float attackReadyPullDistance = 0.18f;
    [SerializeField] private float attackLungeDistance = 0.35f;
    [SerializeField] private float attackRecoilDistance = 0.12f;
    [SerializeField] private float readyRotationZ = 35f;
    [SerializeField] private float hitRotationZ = -40f;
    [SerializeField] private float slowSwingRotationZ = -43f;

    [Header("아군 기본 공격 / 2연격")]
    [SerializeField] private float basicAttackOpeningCameraTime = 0.34f;
    [SerializeField] private float basicAttackApproachTime = 0.55f;
    [SerializeField] private float basicAttackQuickZoomTime = 0.16f;
    [SerializeField] private float basicAttackFirstDashTime = 0.18f;
    [SerializeField] private float basicAttackSecondDashTime = 0.18f;
    [SerializeField] private float basicAttackSecondPullBackTime = 0.42f;
    [SerializeField] private float basicAttackSecondPullBackDistance = 1.05f;
    [SerializeField] private float basicAttackSecondChargeWaitTime = 0.5f;
    [SerializeField] private float basicAttackSecondChargeZoomSize = 2.75f;
    [SerializeField] private float basicAttackSecondForwardExtraDistance = 0.55f;
    [SerializeField] private float basicAttackFinalRetreatTime = 0.34f;
    [SerializeField] private float basicAttackDashThroughTime = 0.18f;
    [SerializeField] private float basicAttackCameraReturnTime = 0.45f;
    [SerializeField] private float basicAttackOpeningZoomSize = 3.75f;
    [SerializeField] private float basicAttackApproachZoomOutSizeOffset = 1f;
    [SerializeField] private float basicAttackHitZoomSize = 3.25f;
    [SerializeField] private float basicAttackImpactZoomOutOffset = 2f;
    [SerializeField] private Vector3 basicAttackOpeningCameraOffset = new Vector3(-1.35f, -0.85f, -9.94f);
    [SerializeField] private Vector3 basicAttackTargetCameraOffset = new Vector3(0f, -0.35f, -9.94f);
    [SerializeField] private Vector3 basicAttackLagCameraOffset = new Vector3(0f, -0.15f, -9.94f);
    [SerializeField] private float basicAttackApproachDistance = 1.15f;
    [SerializeField] private float basicAttackQuickPullBackDistance = 0.36f;
    [SerializeField] private float basicAttackFirstKnockbackDistance = 0.55f;
    [SerializeField] private float basicAttackSecondKnockbackDistance = 0.85f;
    [SerializeField] private float basicAttackTargetJumpHeight = 0.38f;
    [SerializeField] private float basicAttackTargetJumpTime = 0.22f;
    [SerializeField] private float basicAttackLagCameraFollowSpeed = 5.5f;
    [SerializeField] private float basicAttackSecondChargeDriftBackDistance = 0.65f;
    [SerializeField] private int basicAttackSecondStuckShakeCount = 3;
    [SerializeField] private int basicAttackSecondStuckDamage = 1;
    [SerializeField] private int basicAttackFinalPierceDamage = 10;
    [SerializeField] private float basicAttackFinalPierceDistance = 3.65f;
    [SerializeField] private float basicAttackFinalPierceTime = 0.18f;
    [SerializeField] private float basicAttackFinalZoomOutSize = 8f;
    [SerializeField] private float basicAttackFinalZoomOutTime = 0.12f;
    [SerializeField] private float basicAttackFinalZoomReturnTime = 0.72f;
    [SerializeField] private float basicAttackLaunchWorldY = -1.49f;
    [SerializeField] private float basicAttackFinalTargetWorldX = 9f;
    [SerializeField] private float basicAttackSecondImpactHoldTime = 0.08f;
    [SerializeField] private float basicAttackSecondStuckStartDelay = 0.25f;
    [SerializeField] private float basicAttackShakeDuration = 0.13f;
    [SerializeField] private float basicAttackShakeAmount = 0.11f;
    [SerializeField] private float basicAttackShakeRotationAmount = 1.05f;
    [SerializeField] private float basicAttackShakeSpeed = 86f;

    [Header("아군 강공격 연출")]
    [SerializeField] private float allyAttackPullBackDistance = 0.55f;
    [SerializeField] private float allyAttackDashToImpactTime = 0.26f;
    [SerializeField] private float allyAttackDashThroughTime = 0.40f;
    [SerializeField] private float allyAttackPierceDistance = 1.35f;
    [SerializeField] private float allyAttackEnemyDragDistance = 1.15f;
    [SerializeField] private float allyAttackReturnTime = 0.75f;
    [SerializeField] private string defaultStandingObjectName = "default_standing";
    [SerializeField] private string hitObjectName = "Hit";

    [Header("아군 강공격 카메라")]
    [SerializeField] private Vector3 allyAttackBaseCameraPosition = new Vector3(0f, 0.66f, -9.94f);
    [SerializeField] private Vector3 allyAttackCloseCameraPosition = new Vector3(-7.45f, -2.68f, -10.23f);
    [SerializeField] private Vector3 allyAttackLeftCameraRotation = new Vector3(12.241f, -42.484f, -6.339f);
    [SerializeField] private Vector3 allyAttackDefaultCameraRotation = new Vector3(5.012f, 0f, 0.783f);
    [SerializeField] private Vector3 idleCameraDefaultRotation = new Vector3(8.25f, 0f, 0.783f);
    [SerializeField] private Vector3 allyAttackDashCameraRotation = new Vector3(1.696f, 33.602f, -5.486f);
    [SerializeField] private float allyAttackCloseCameraTime = 1.5f;
    [SerializeField] private float allyAttackStrongZoomInSize = 2.15f;
    [SerializeField] private float allyAttackDashZoomInSize = 1.85f;
    [SerializeField] private float allyAttackReturnZoomOutSize = 5f;
    [SerializeField] private float allyAttackCameraFollowSpeed = 3.4f;
    [SerializeField] private Vector3 allyAttackDashCameraOffset = new Vector3(0f, 0.35f, -10.15f);
    [SerializeField] private float allyAttackCameraReturnTime = 2f;
    [SerializeField] private float allyAttackPierceShakeDuration = 0.22f;
    [SerializeField] private float allyAttackPierceShakeAmount = 0.28f;
    [SerializeField] private float allyAttackPierceShakeRotationAmount = 3.2f;
    [SerializeField] private float allyAttackPierceShakeSpeed = 105f;

    [Header("피격 반응")]
    [SerializeField] private Color hitReactionColor = new Color(1.8f, 0.15f, 0.15f, 1f);
    [SerializeField] private float hitReactionTime = 0.22f;
    [SerializeField] private float hitKnockbackDistance = 0.22f;
    [SerializeField] private float hitShakeAmount = 0.06f;
    [SerializeField] private float hitShakeSpeed = 55f;
    [SerializeField] private float hitRotationShakeZ = 7f;
    [SerializeField] private float hitSquashX = 1.08f;
    [SerializeField] private float hitSquashY = 0.9f;

    [Header("피격음")]
    [SerializeField] private AudioSource hitAudioSource;
    [SerializeField] private AudioClip hitSoundA;
    [SerializeField] private AudioClip hitSoundB;
    [SerializeField] private Vector2 hitSoundPitchRange = new Vector2(0.86f, 1.16f);
    [SerializeField] private float hitSoundVolume = 1f;

    [Header("양식 / 아이템 회복 연출")]
    [SerializeField] private string itemObjectName = "ItemObj";
    [SerializeField] private int itemHealAmount = 20;
    [SerializeField] private AudioSource healAudioSource;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private float healSoundVolume = 1f;

    [Header("UI 클릭음")]
    [SerializeField] private AudioSource uiClickAudioSource;
    [SerializeField] private AudioClip uiClickSound;
    [SerializeField] private Vector2 uiClickPitchRange = new Vector2(0.96f, 1.04f);
    [SerializeField] private float uiClickSoundVolume = 0.8f;
    [SerializeField] private float itemCameraZoomSize = 2.55f;
    [SerializeField] private float itemCameraFocusTime = 0.35f;
    [SerializeField] private Vector3 itemCameraOffset = new Vector3(0f, 0.55f, -9.94f);
    [SerializeField] private float itemThrowHeight = 1.65f;
    [SerializeField] private float itemThrowTime = 1.15f;
    [SerializeField] private float itemThrowSpinCount = 2.4f;
    [SerializeField] private float itemCameraFollowSpeed = 5.5f;
    [SerializeField] private float itemReturnHoldTime = 0.08f;
    [SerializeField] private float itemFadeSpinTime = 0.35f;
    [SerializeField] private float itemFadeSpinCount = 1.2f;
    [SerializeField] private float healInfoStayTime = 0.35f;

    [Header("공격 잔상")]
    [SerializeField] private bool attackAfterimageEnabled = true;
    [SerializeField] private float attackAfterimageInterval = 0.035f;
    [SerializeField] private float attackAfterimageLifeTime = 0.22f;
    [SerializeField] private float attackAfterimageAlpha = 0.38f;
    [SerializeField] private Color attackAfterimageTint = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color enemyAttackAfterimageTint = new Color(1f, 0.08f, 0.08f, 1f);
    [SerializeField] private int attackAfterimageSortingOrderOffset = -1;

    [Header("적 턴")]
    [SerializeField] private float enemyTurnDelay = 1f;
    [SerializeField] private float nextTurnDelay = 0.65f;
    [SerializeField] private float nextTurnPositionResetTweenTime = 0.45f;

    [Header("턴 전환 페이드")]
    [SerializeField] private Image screenFadeoutTurnImage;
    [SerializeField] private string screenFadeoutTurnImageName = "ScreenFadeoutTurn";
    [SerializeField] private float turnFadeInTime = 0.32f;
    [SerializeField] private float turnFadeHoldTime = 1f;
    [SerializeField] private float turnFadeOutTime = 0.42f;

    [Header("적 턴 던지기 연출")]
    [SerializeField] private float enemyApproachTime = 0.85f;
    [SerializeField] private float enemyApproachStandDistance = 1.1f;
    [SerializeField] private float enemyApproachCameraSize = 3.15f;
    [SerializeField] private float enemyApproachCameraFollowSpeed = 3.2f;
    [SerializeField] private Vector3 enemyApproachCameraOffset = new Vector3(0f, 0.35f, -9.94f);
    [SerializeField] private float enemyArrivalToDefaultSizeTime = 0.12f;
    [SerializeField] private int enemyFirstHitDamage = 10;
    [SerializeField] private float enemyFirstHitZoomSize = 2.65f;
    [SerializeField] private float enemyFirstHitZoomTime = 0.1f;
    [SerializeField] private float enemyThrowChargeTime = 3.25f;
    [SerializeField] private float enemyThrowChargeZoomOutSize = 8f;
    [SerializeField] private float enemyThrowTiltZ = -25f;
    [SerializeField] private float enemyThrowStepInterval = 0.25f;
    [SerializeField] private float enemyThrowLiftHeight = 2.6f;
    [SerializeField] private float enemyThrowRightDistance = 4.4f;
    [SerializeField] private Vector3 enemyThrowTargetWorldPosition = new Vector3(1.12f, -3.95f, -1.32f);
    [SerializeField] private float enemyThrowArcHeight = 2.2f;
    [SerializeField] private float enemyThrowTime = 0.55f;
    [SerializeField] private int enemyThrowDamage = 25;
    [SerializeField] private float enemyThrowImpactZoomSize = 2.25f;
    [SerializeField] private float enemyThrowImpactZoomTime = 0.12f;
    [SerializeField] private float enemyThrowCameraReturnTime = 0.8f;
    [SerializeField] private float enemyThrowReleaseShakeDuration = 0.28f;
    [SerializeField] private float enemyThrowReleaseShakeAmount = 0.22f;
    [SerializeField] private float enemyThrowReleaseShakeRotationAmount = 2.4f;
    [SerializeField] private float enemyThrowReleaseShakeSpeed = 96f;
    [SerializeField] private float enemyThrowSpinYTime = 0.42f;

    [Header("적 추가 공격 / 변형 기본 공격")]
    [SerializeField] private bool enemyUseBasicVariantAttack = true;
    [Range(0f, 1f)]
    [SerializeField] private float enemyBasicVariantAttackChance = 0.45f;
    [SerializeField] private int enemyBasicVariantFirstDamage = 7;
    [SerializeField] private int enemyBasicVariantSecondDamage = 13;
    [SerializeField] private float enemyBasicVariantApproachTime = 0.48f;
    [SerializeField] private float enemyBasicVariantPullBackTime = 0.2f;
    [SerializeField] private float enemyBasicVariantDashTime = 0.18f;
    [SerializeField] private float enemyBasicVariantSecondWaitTime = 0.22f;
    [SerializeField] private float enemyBasicVariantReturnTime = 0.42f;
    [SerializeField] private float enemyBasicVariantStandDistance = 1.05f;
    [SerializeField] private float enemyBasicVariantPullBackDistance = 0.75f;
    [SerializeField] private float enemyBasicVariantFirstKnockbackDistance = 0.65f;
    [SerializeField] private float enemyBasicVariantSecondKnockbackDistance = 1.15f;
    [SerializeField] private float enemyBasicVariantLaunchHeight = 0.85f;
    [SerializeField] private float enemyBasicVariantCameraZoomSize = 3.15f;
    [SerializeField] private float enemyBasicVariantImpactZoomSize = 2.65f;
    [SerializeField] private float enemyBasicVariantCameraReturnTime = 0.45f;

    [Header("UI")]
    [SerializeField] private BattleCanvasUI battleCanvasUI;

    [Header("데미지 표시")]
    [SerializeField] private GameObject damageDisplayTemplate;
    [SerializeField] private string damageDisplayObjectName = "DamageDisplay";
    [SerializeField] private string damageAmountTextName = "Amout";
    [SerializeField] private Vector3 damageDisplayWorldOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField] private Vector2 damageDisplayScreenOffset = new Vector2(0f, 80f);
    [SerializeField] private float damageDisplayFadeInTime = 0.1f;
    [SerializeField] private float damageDisplayPopScale = 1.08f;
    [SerializeField] private float damageDisplayStartScale = 0.88f;
    [SerializeField] private float damageDisplaySettleTime = 0.12f;
    [SerializeField] private float damageDisplayFloatTime = 0.55f;
    [SerializeField] private float damageDisplayRiseDistance = 45f;
    [SerializeField] private Vector3 damageDisplayExactBaseScale = new Vector3(0.002502506f, 0.002333028f, 0.00560765f);

    private bool hasDamageDisplayTemplateOrigin;
    private Vector3 damageDisplayTemplateLocalPosition;
    private Vector3 damageDisplayTemplateWorldPosition;
    private Quaternion damageDisplayTemplateLocalRotation;
    private Vector3 damageDisplayTemplateLocalScale = Vector3.one;
    private Vector2 damageDisplayTemplateAnchoredPosition;
    private Vector3 damageDisplayTemplateAnchoredPosition3D;
    private Vector2 damageDisplayTemplateAnchorMin;
    private Vector2 damageDisplayTemplateAnchorMax;
    private Vector2 damageDisplayTemplatePivot;
    private Vector2 damageDisplayTemplateSizeDelta;

    public Camera BattleCamera => battleCamera;
    public Vector3 ActionMenuWorldOffset => actionMenuWorldOffset;
    public float ActionMenuWorldScale => actionMenuWorldScale;
    public bool ActionMenuFaceCamera => actionMenuFaceCamera;
    public int ActionMenuSortingOrder => actionMenuSortingOrder;
    public Color PortraitEmptyColor => portraitEmptyColor;
    public Color NormalMenuColor => normalMenuColor;
    public Color NormalMenuTextColor => normalMenuTextColor;
    public Color SelectedMenuColor => selectedMenuColor;
    public Color SelectedMenuTextColor => selectedMenuTextColor;
    public float MenuFadeTime => menuFadeTime;
    public float DamagePanelShakeTime => damagePanelShakeTime;
    public float DamagePanelShakePerDamage => damagePanelShakePerDamage;
    public float DamagePanelMaxShake => damagePanelMaxShake;
    public Color ExecuteButtonLockedColor => executeButtonLockedColor;
    public Color ExecuteButtonReadyColor => executeButtonReadyColor;
    public float TurnBannerFadeTime => turnBannerFadeTime;
    public float TurnBannerStayTime => turnBannerStayTime;
    public Color ParryFlashColor => parryFlashColor;
    public float ParryFlashInTime => parryFlashInTime;
    public float ParryFlashOutTime => parryFlashOutTime;

    private readonly List<BattleUnit> teamUnits = new List<BattleUnit>();
    private readonly List<BattleUnit> enemyUnits = new List<BattleUnit>();
    private readonly Dictionary<BattleUnit, BattleActionType> plannedActions = new Dictionary<BattleUnit, BattleActionType>();
    private readonly Dictionary<BattleUnit, int> teamDamageDealtThisTurn = new Dictionary<BattleUnit, int>();
    private readonly Dictionary<BattleUnit, BattleActionMenuUI> unitMenus = new Dictionary<BattleUnit, BattleActionMenuUI>();
    private readonly Dictionary<BattleUnit, Vector3> unitDefaultPositions = new Dictionary<BattleUnit, Vector3>();
    private readonly HashSet<BattleUnit> egoResonanceUsedUnits = new HashSet<BattleUnit>();
    private readonly List<IBattleCommand> selectedCommands = new List<IBattleCommand>();

    private Vector3 defaultCameraPosition;
    private BattleUnit currentSelectingUnit;
    private BattleActionMenuUI currentMenu;
    private BattleActionType selectedMenuAction = BattleActionType.Attack;
    private bool isPlayerPlanningPhase;
    private bool isSelectingAction;
    private bool executeRequested;
    private bool parryInputSucceeded;
    private bool isSelectionCameraTweening;
    private Coroutine cameraFocusCoroutine;
    private Coroutine menuFadeCoroutine;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        if (battleCamera == null)
        {
            Debug.LogError("Battle Camera가 없습니다.");
            return;
        }

        battleCamera.orthographic = true;
        defaultCameraPosition = allyAttackBaseCameraPosition;
        battleCamera.transform.position = defaultCameraPosition;
        battleCamera.transform.rotation = Quaternion.Euler(allyAttackDefaultCameraRotation);
        battleCamera.orthographicSize = normalCameraSize;

        SetupHitAudioSource();
        SetupHealAudioSource();
        SetupUIClickAudioSource();
        SetupDamageDisplayTemplate();
        SetupTurnScreenFade();
        CreateEventSystemIfNeeded();
        FindBattleUnits();
        SetupCanvasUI();
        SetupActionMenus();

        StartCoroutine(BattleRoutine());
    }

    private void Update()
    {
        if (isPlayerPlanningPhase && !executeRequested)
        {
            UpdatePlanningCameraMotion();
        }

        if (!isPlayerPlanningPhase)
        {
            return;
        }

        if (isSelectingAction)
        {
            HandleActionMenuKeyboardInput();
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandlePlanningMouseClick();
        }
    }

    private IEnumerator BattleRoutine()
    {
        bool isFirstTurn = true;

        while (true)
        {
            if (AreAllEnemyUnitsDead())
            {
                yield return PlayBattleResultAndQuit(true);
                yield break;
            }

            if (AreAllTeamUnitsDead())
            {
                yield return PlayBattleResultAndQuit(false);
                yield break;
            }

            selectedCommands.Clear();
            plannedActions.Clear();
            ResetTurnDamageRecord();

            yield return PlayTurnStartIntro(isFirstTurn);
            isFirstTurn = false;

            yield return ShowTurnBordersForPlanning();

            yield return PlayerPlanningPhase();
            yield return RestoreCameraAfterSelection();

            yield return HideTurnBordersUntilNextTurn();

            yield return ExecuteSelectedCommands();
            yield return RestoreCameraAfterSelection();

            if (AreAllEnemyUnitsDead())
            {
                yield return PlayBattleResultAndQuit(true);
                yield break;
            }

            yield return new WaitForSeconds(Mathf.Max(enemyTurnDelay, 1f));
            yield return ExecuteEnemyActions();
            yield return RestoreCameraAfterSelection();

            if (AreAllTeamUnitsDead())
            {
                yield return PlayBattleResultAndQuit(false);
                yield break;
            }

            yield return PlayTurnTransitionFadeAndReset();
            currentTurn++;
            yield return new WaitForSeconds(nextTurnDelay);
        }
    }

    private IEnumerator PlayerPlanningPhase()
    {
        isPlayerPlanningPhase = true;
        executeRequested = false;

        battleCanvasUI.SetExecuteButtonVisible(true);
        UpdateExecuteButtonState();

        while (!executeRequested)
        {
            UpdateExecuteButtonState();
            yield return null;
        }

        if (currentMenu != null)
        {
            yield return CloseActionMenuRoutine(true);
        }

        isPlayerPlanningPhase = false;
        battleCanvasUI.SetExecuteButtonVisible(false);
        selectedCommands.Clear();

        foreach (BattleUnit unit in teamUnits)
        {
            if (unit == null || unit.IsDead)
            {
                continue;
            }

            if (plannedActions.ContainsKey(unit))
            {
                selectedCommands.Add(BattleCommandFactory.Create(unit, plannedActions[unit]));
            }
        }
    }

    public void RequestExecuteActions()
    {
        if (!isPlayerPlanningPhase)
        {
            return;
        }

        if (!AreAllAliveTeamUnitsPlanned())
        {
            Debug.Log("아직 모든 아군의 행동이 선택되지 않았습니다.");
            return;
        }

        PlayUIClickSound();
        executeRequested = true;
    }

    private void HandlePlanningMouseClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        BattleUnit clickedUnit = GetBattleUnitUnderMouse();

        if (clickedUnit != null && clickedUnit.UnitSide == BattleUnitSide.Team && !clickedUnit.IsDead)
        {
            OpenOrSwitchActionMenu(clickedUnit);
            return;
        }

        if (isSelectingAction)
        {
            StartCoroutine(CloseActionMenuRoutine(true));
        }
    }

    private void HandleActionMenuKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            selectedMenuAction = GetPreviousAction(selectedMenuAction);
            if (currentMenu != null) currentMenu.SetSelectedAction(selectedMenuAction);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            selectedMenuAction = GetNextAction(selectedMenuAction);
            if (currentMenu != null) currentMenu.SetSelectedAction(selectedMenuAction);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmCurrentSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(CloseActionMenuRoutine(true));
        }
    }

    private void OpenOrSwitchActionMenu(BattleUnit unit)
    {
        if (!unitMenus.ContainsKey(unit))
        {
            Debug.LogWarning($"{unit.gameObject.name}의 ActionMenu를 찾지 못했습니다.");
            return;
        }

        if (currentMenu != null && currentMenu != unitMenus[unit])
        {
            currentMenu.HideImmediate();
        }

        currentSelectingUnit = unit;
        currentMenu = unitMenus[unit];
        isSelectingAction = true;
        battleCanvasUI.SetExecuteButtonVisible(false);

        selectedMenuAction = plannedActions.ContainsKey(unit) ? plannedActions[unit] : BattleActionType.Attack;

        currentMenu.ShowUnitInfo(true);
        currentMenu.SetButtonsInteractable(true);

        if (!CanUseEgoResonance(unit) && selectedMenuAction == BattleActionType.EgoResonance)
        {
            selectedMenuAction = BattleActionType.Attack;
        }

        currentMenu.SetSelectedAction(selectedMenuAction);
        ApplyEgoResonanceButtonState(currentMenu, unit);

        if (menuFadeCoroutine != null)
        {
            StopCoroutine(menuFadeCoroutine);
        }

        menuFadeCoroutine = StartCoroutine(currentMenu.Fade(true));
        StartCameraFocus(unit);
    }

    private IEnumerator CloseActionMenuRoutine(bool restoreCamera)
    {
        isSelectingAction = false;
        isSelectionCameraTweening = true;

        if (currentMenu != null)
        {
            currentMenu.SetButtonsInteractable(false);
            yield return currentMenu.Fade(false);
        }

        currentMenu = null;
        currentSelectingUnit = null;

        if (restoreCamera)
        {
            yield return RestoreCameraAfterSelection();
        }

        isSelectionCameraTweening = false;

        if (isPlayerPlanningPhase && !executeRequested)
        {
            battleCanvasUI.SetExecuteButtonVisible(true);
        }

        UpdateExecuteButtonState();
    }

    public void OnActionMenuButtonClicked(BattleUnit owner, BattleActionType actionType)
    {
        if (!isSelectingAction || owner != currentSelectingUnit) return;

        PlayUIClickSound();

        if (actionType == BattleActionType.EgoResonance && !CanUseEgoResonance(owner))
        {
            Debug.Log($"{owner.gameObject.name}은 이미 자아 공명을 사용했습니다.");
            return;
        }

        selectedMenuAction = actionType;
        ConfirmCurrentSelection();
    }

    public void OnActionMenuButtonHovered(BattleUnit owner, BattleActionType actionType)
    {
        if (!isSelectingAction || owner != currentSelectingUnit) return;

        if (actionType == BattleActionType.EgoResonance && !CanUseEgoResonance(owner))
        {
            return;
        }

        selectedMenuAction = actionType;
        if (currentMenu != null) currentMenu.SetSelectedAction(selectedMenuAction);
    }

    private void ConfirmCurrentSelection()
    {
        if (!isSelectingAction || currentSelectingUnit == null) return;

        if (selectedMenuAction == BattleActionType.EgoResonance && !CanUseEgoResonance(currentSelectingUnit))
        {
            Debug.Log($"{currentSelectingUnit.gameObject.name}은 이미 자아 공명을 사용했습니다.");
            selectedMenuAction = BattleActionType.Attack;
            if (currentMenu != null)
            {
                currentMenu.SetSelectedAction(selectedMenuAction);
                ApplyEgoResonanceButtonState(currentMenu, currentSelectingUnit);
            }
            return;
        }

        plannedActions[currentSelectingUnit] = selectedMenuAction;

        // 옵저버 패턴
        BattleEvents.RaiseActionPlanned(currentSelectingUnit, selectedMenuAction);

        Debug.Log($"{currentSelectingUnit.gameObject.name} 선택: {selectedMenuAction}");
        StartCoroutine(CloseActionMenuRoutine(true));
        UpdateExecuteButtonState();
    }

    private IEnumerator ExecuteSelectedCommands()
    {
        foreach (IBattleCommand command in selectedCommands)
        {
            if (command == null || command.Actor == null || command.Actor.IsDead) continue;
            yield return command.Execute(this);
        }
    }

    private IEnumerator ExecuteEnemyActions()
    {
        foreach (BattleUnit enemy in enemyUnits)
        {
            if (enemy == null || enemy.IsDead) continue;

            BattleUnit target = ChooseEnemyAttackTarget();
            if (target == null) yield break;

            yield return PlayAttackMotion(enemy, target, false);
            yield return new WaitForSeconds(0.25f);
        }
    }

    public IEnumerator PlayAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null) yield break;

        if (attacker.UnitSide == BattleUnitSide.Team)
        {
            yield return PlayBasicAttackMotion(attacker, target, recordTeamDamage);
        }
        else
        {
            yield return PlayEnemyAttackMotion(attacker, target, recordTeamDamage);
        }
    }

    public IEnumerator PlayBasicAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null) yield break;
        yield return PlayAllyBasicPierceAttackMotion(attacker, target, recordTeamDamage);
    }

    public IEnumerator PlayStrongAttackMotion(BattleUnit attacker, BattleUnit target, bool recordTeamDamage)
    {
        if (attacker == null || target == null) yield break;

        yield return PlayAllyStrongPierceAttackMotion(attacker, target, recordTeamDamage);

        if (attacker.UnitSide == BattleUnitSide.Team)
        {
            MarkEgoResonanceUsed(attacker);
        }
    }

    public IEnumerator PlayItemHealMotion(BattleUnit actor)
    {
        if (actor == null || actor.IsDead)
        {
            yield break;
        }

        Transform actorTransform = actor.transform;
        Transform itemObject = FindChildDeep(actorTransform, itemObjectName);

        Vector3 actorCameraPosition = new Vector3(
            actorTransform.position.x + itemCameraOffset.x,
            actorTransform.position.y + itemCameraOffset.y,
            itemCameraOffset.z
        );

        yield return TweenCameraPositionSizeRotation(
            actorCameraPosition,
            itemCameraZoomSize,
            allyAttackDefaultCameraRotation,
            itemCameraFocusTime,
            TweenEase.SmoothSine
        );

        if (itemObject == null)
        {
            Debug.LogWarning($"{actor.gameObject.name} 내부에서 {itemObjectName} 오브젝트를 찾지 못했습니다. 회복만 적용합니다.");

            float beforeRate;
            float afterRate;
            int actualHeal = HealBattleUnit(actor, itemHealAmount, out beforeRate, out afterRate);

            PlayHealSound();
            yield return ShowHealHealthInfo(actor, beforeRate, afterRate, actualHeal);
            yield return TweenCameraPositionSizeRotation(
                allyAttackBaseCameraPosition,
                normalCameraSize,
                allyAttackDefaultCameraRotation,
                cameraSmoothReturnTime,
                TweenEase.SmoothSine
            );
            yield break;
        }

        bool itemWasActive = itemObject.gameObject.activeSelf;
        Vector3 itemOriginalLocalPosition = itemObject.localPosition;
        Quaternion itemOriginalLocalRotation = itemObject.localRotation;
        Vector3 itemOriginalLocalScale = itemObject.localScale;

        List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>(itemObject.GetComponentsInChildren<SpriteRenderer>(true));
        List<Graphic> uiGraphics = new List<Graphic>(itemObject.GetComponentsInChildren<Graphic>(true));
        List<Color> spriteOriginalColors = new List<Color>();
        List<Color> graphicOriginalColors = new List<Color>();

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            spriteOriginalColors.Add(spriteRenderers[i].color);
        }

        for (int i = 0; i < uiGraphics.Count; i++)
        {
            graphicOriginalColors.Add(uiGraphics[i].color);
        }

        itemObject.gameObject.SetActive(true);
        SetItemVisualAlpha(spriteRenderers, uiGraphics, spriteOriginalColors, graphicOriginalColors, 1f);
        itemObject.localPosition = itemOriginalLocalPosition;
        itemObject.localRotation = itemOriginalLocalRotation;
        itemObject.localScale = itemOriginalLocalScale;

        yield return PlayItemThrowAndCameraFollow(
            itemObject,
            itemOriginalLocalPosition,
            itemOriginalLocalRotation,
            itemOriginalLocalScale
        );

        itemObject.localPosition = itemOriginalLocalPosition;
        itemObject.localRotation = itemOriginalLocalRotation;
        itemObject.localScale = itemOriginalLocalScale;

        yield return new WaitForSeconds(Mathf.Max(0f, itemReturnHoldTime));

        float healBeforeRate;
        float healAfterRate;
        int healedAmount = HealBattleUnit(actor, itemHealAmount, out healBeforeRate, out healAfterRate);

        PlayHealSound();

        Coroutine healInfoCoroutine = StartCoroutine(ShowHealHealthInfo(actor, healBeforeRate, healAfterRate, healedAmount));
        Coroutine itemFadeCoroutine = StartCoroutine(SpinAndFadeItemObject(
            itemObject,
            itemOriginalLocalPosition,
            itemOriginalLocalRotation,
            itemOriginalLocalScale,
            spriteRenderers,
            uiGraphics,
            spriteOriginalColors,
            graphicOriginalColors
        ));
        Coroutine cameraReturnCoroutine = StartCoroutine(TweenCameraPositionSizeRotation(
            allyAttackBaseCameraPosition,
            normalCameraSize,
            allyAttackDefaultCameraRotation,
            itemFadeSpinTime + 0.18f,
            TweenEase.SmoothSine
        ));

        yield return itemFadeCoroutine;
        yield return cameraReturnCoroutine;
        yield return healInfoCoroutine;

        itemObject.localPosition = itemOriginalLocalPosition;
        itemObject.localRotation = itemOriginalLocalRotation;
        itemObject.localScale = itemOriginalLocalScale;
        SetItemVisualAlpha(spriteRenderers, uiGraphics, spriteOriginalColors, graphicOriginalColors, 1f);
        itemObject.gameObject.SetActive(itemWasActive);
    }

    public bool CanUseEgoResonance(BattleUnit actor)
    {
        if (actor == null || actor.IsDead)
        {
            return false;
        }

        if (!egoResonanceOneUsePerUnit)
        {
            return true;
        }

        return !egoResonanceUsedUnits.Contains(actor);
    }

    public void MarkEgoResonanceUsed(BattleUnit actor)
    {
        if (actor == null || !egoResonanceOneUsePerUnit)
        {
            return;
        }

        egoResonanceUsedUnits.Add(actor);

        if (currentMenu != null && currentSelectingUnit == actor)
        {
            ApplyEgoResonanceButtonState(currentMenu, actor);
        }
    }

    private void ApplyEgoResonanceButtonState(BattleActionMenuUI menu, BattleUnit owner)
    {
        if (menu == null)
        {
            return;
        }

        Transform egoButtonRoot = FindChildDeep(menu.transform, "EgoResonance");

        if (egoButtonRoot == null)
        {
            return;
        }

        bool canUse = CanUseEgoResonance(owner);
        Button button = egoButtonRoot.GetComponent<Button>();

        if (button == null)
        {
            button = egoButtonRoot.GetComponentInChildren<Button>(true);
        }

        if (button != null)
        {
            button.interactable = canUse;
        }

        CanvasGroup canvasGroup = egoButtonRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = egoButtonRoot.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = canUse ? 1f : Mathf.Clamp01(egoResonanceDisabledAlpha);
        canvasGroup.interactable = canUse;
        canvasGroup.blocksRaycasts = canUse;
    }


    private class TurnBorderInstance
    {
        public RectTransform Rect;
        public RectTransform Template;
        public bool HasCreatedNext;
    }

    [Header("턴 보더 연출")]
    [SerializeField] private RectTransform turnBorderL;
    [SerializeField] private RectTransform turnBorderR;

    [SerializeField] private float turnBorderFadeInTime = 0.45f;
    [SerializeField] private float turnBorderFadeOutTime = 0.35f;
    [SerializeField] private float turnBorderPlanningAlpha = 0.45f;

    [SerializeField] private float turnBorderMoveSpeed = 260f;
    [SerializeField] private float turnBorderCloneTriggerY = -355f;
    [SerializeField] private float turnBorderCloneStartY = 1277f;
    [SerializeField] private float turnBorderDestroyY = -1288f;

    private readonly List<TurnBorderInstance> activeTurnBorders = new List<TurnBorderInstance>();

    private Coroutine turnBorderMoveCoroutine;
    private float currentTurnBorderAlpha;
    private bool isTurnBorderActive;

    private IEnumerator ShowTurnBordersForPlanning()
    {
        SetupTurnBorderReferences();

        if (turnBorderL == null || turnBorderR == null)
        {
            Debug.LogWarning("Border_L 또는 Border_R을 찾지 못했습니다.");
            yield break;
        }

        ClearTurnBorderRuntimeObjects();

        isTurnBorderActive = true;
        currentTurnBorderAlpha = 0f;

        SetTurnBorderGraphicAlpha(turnBorderL.gameObject, 0f);
        SetTurnBorderGraphicAlpha(turnBorderR.gameObject, 0f);

        CreateTurnBorderInstance(turnBorderL, turnBorderL.anchoredPosition);
        CreateTurnBorderInstance(turnBorderR, turnBorderR.anchoredPosition);

        if (turnBorderMoveCoroutine != null)
        {
            StopCoroutine(turnBorderMoveCoroutine);
        }

        turnBorderMoveCoroutine = StartCoroutine(TurnBorderMoveLoop());

        yield return FadeTurnBorders(0f, turnBorderPlanningAlpha, turnBorderFadeInTime);
    }

    private IEnumerator HideTurnBordersUntilNextTurn()
    {
        if (!isTurnBorderActive)
        {
            yield break;
        }

        yield return FadeTurnBorders(currentTurnBorderAlpha, 0f, turnBorderFadeOutTime);

        isTurnBorderActive = false;

        if (turnBorderMoveCoroutine != null)
        {
            StopCoroutine(turnBorderMoveCoroutine);
            turnBorderMoveCoroutine = null;
        }

        ClearTurnBorderRuntimeObjects();

        if (turnBorderL != null)
        {
            SetTurnBorderGraphicAlpha(turnBorderL.gameObject, 0f);
        }

        if (turnBorderR != null)
        {
            SetTurnBorderGraphicAlpha(turnBorderR.gameObject, 0f);
        }
    }

    private void SetupTurnBorderReferences()
    {
        if (turnBorderL == null)
        {
            GameObject foundL = FindSceneObjectByNameForTurnBorder("Border_L");

            if (foundL != null)
            {
                turnBorderL = foundL.GetComponent<RectTransform>();
            }
        }

        if (turnBorderR == null)
        {
            GameObject foundR = FindSceneObjectByNameForTurnBorder("Border_R");

            if (foundR != null)
            {
                turnBorderR = foundR.GetComponent<RectTransform>();
            }
        }
    }

    private IEnumerator TurnBorderMoveLoop()
    {
        while (isTurnBorderActive)
        {
            for (int i = activeTurnBorders.Count - 1; i >= 0; i--)
            {
                TurnBorderInstance instance = activeTurnBorders[i];

                if (instance == null || instance.Rect == null)
                {
                    activeTurnBorders.RemoveAt(i);
                    continue;
                }

                Vector2 position = instance.Rect.anchoredPosition;
                position.y -= turnBorderMoveSpeed * Time.deltaTime;
                instance.Rect.anchoredPosition = position;

                if (!instance.HasCreatedNext && position.y <= turnBorderCloneTriggerY)
                {
                    instance.HasCreatedNext = true;

                    Vector2 clonePosition = position;
                    clonePosition.y = turnBorderCloneStartY;

                    CreateTurnBorderInstance(instance.Template, clonePosition);
                }

                if (position.y <= turnBorderDestroyY)
                {
                    Destroy(instance.Rect.gameObject);
                    activeTurnBorders.RemoveAt(i);
                }
            }

            yield return null;
        }
    }

    private void CreateTurnBorderInstance(RectTransform template, Vector2 anchoredPosition)
    {
        if (template == null)
        {
            return;
        }

        GameObject clone = Instantiate(template.gameObject, template.parent);
        clone.name = template.name + "_Runtime";

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = anchoredPosition;

        clone.SetActive(true);
        SetTurnBorderGraphicAlpha(clone, currentTurnBorderAlpha);

        TurnBorderInstance instance = new TurnBorderInstance
        {
            Rect = cloneRect,
            Template = template,
            HasCreatedNext = false
        };

        activeTurnBorders.Add(instance);
    }

    private IEnumerator FadeTurnBorders(float startAlpha, float targetAlpha, float duration)
    {
        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (timer < safeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / safeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            currentTurnBorderAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            ApplyAlphaToActiveTurnBorders(currentTurnBorderAlpha);

            yield return null;
        }

        currentTurnBorderAlpha = targetAlpha;
        ApplyAlphaToActiveTurnBorders(currentTurnBorderAlpha);
    }

    private void ApplyAlphaToActiveTurnBorders(float alpha)
    {
        for (int i = 0; i < activeTurnBorders.Count; i++)
        {
            if (activeTurnBorders[i] == null || activeTurnBorders[i].Rect == null)
            {
                continue;
            }

            SetTurnBorderGraphicAlpha(activeTurnBorders[i].Rect.gameObject, alpha);
        }
    }

    private void SetTurnBorderGraphicAlpha(GameObject target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            Color color = graphics[i].color;
            color.a = alpha;
            graphics[i].color = color;
        }
    }

    private void ClearTurnBorderRuntimeObjects()
    {
        for (int i = activeTurnBorders.Count - 1; i >= 0; i--)
        {
            if (activeTurnBorders[i] != null && activeTurnBorders[i].Rect != null)
            {
                Destroy(activeTurnBorders[i].Rect.gameObject);
            }
        }

        activeTurnBorders.Clear();
    }

    private GameObject FindSceneObjectByNameForTurnBorder(string objectName)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform target = allTransforms[i];

            if (target == null)
            {
                continue;
            }

            if (target.name != objectName)
            {
                continue;
            }

            if (!target.gameObject.scene.IsValid())
            {
                continue;
            }

            return target.gameObject;
        }

        return null;
    }


}
