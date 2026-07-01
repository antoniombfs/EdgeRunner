using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public enum EdgeRunnerObjectAwarePhase
{
    TraversalBase = 0,
    LowCoinRun = 1,
    HighCoinJump = 2,
    StaticAndroidAvoid = 3,
    StaticAndroidStomp = 4,
    MixedWarmup = 5,
    MixedRandomWarmup = 6,
    FinalRandom = 7,
    FinalLongChallenge = 8,
    FinalLongZone4Warmup = 9
}

public class EdgeRunnerAgentV5ScoreMaxObjectAware : EdgeRunnerAgentV5
{
    private const int JumpBranchIndex = 1;
    private const int NoJumpAction = 0;
    private const int JumpAction = 1;

    public const int GlobalObjectiveObservationCount = 8;
    public const int NextObjectiveObservationCount = 10;
    public const int LowCoinObservationCount = 6;
    public const int HighCoinObservationCount = 6;
    public const int AndroidObservationCount = 12;
    public const int JumpContextObservationCount = 14;
    public const int ObjectAwareExtraObservationCount =
        GlobalObjectiveObservationCount +
        NextObjectiveObservationCount +
        LowCoinObservationCount +
        HighCoinObservationCount +
        AndroidObservationCount +
        JumpContextObservationCount;
    public new const int DefaultExpectedObservationSize =
        EdgeRunnerAgentV5.DefaultExpectedObservationSize + ObjectAwareExtraObservationCount;

    [Header("ObjectAware References")]
    [SerializeField] private Rigidbody2D objectAwareRigidbody;
    [SerializeField] private Transform objectAwareGoal;
    [SerializeField] private ScoreMaxOAMixedRandomWarmupRandomizer mixedRandomWarmupRandomizer;
    [SerializeField] private ScoreMaxOAFinalRandomizer finalRandomizer;

    [Header("ObjectAware Normalization")]
    [SerializeField] private float maxObjectiveDistance = 35f;
    [SerializeField] private float maxObjectiveHeight = 12f;
    [SerializeField] private float maxCoinCount = 8f;
    [SerializeField] private float maxEnemyCount = 4f;

    [Header("ObjectAware Classification")]
    [SerializeField] private float lowCoinHeightThreshold = 0.45f;
    [SerializeField] private float lowCoinRunWindowX = 3f;
    [SerializeField] private float highCoinJumpWindowX = 2.5f;
    [SerializeField] private float androidContextWindowX = 3f;
    [SerializeField] private float androidVerticalTolerance = 1.5f;

    [Header("ObjectAware Curriculum")]
    [SerializeField] private EdgeRunnerObjectAwarePhase objectAwarePhase =
        EdgeRunnerObjectAwarePhase.TraversalBase;
    [SerializeField] private bool enableObjectAwareRewardShaping = false;
    [SerializeField] private bool enableMissedCoinEpisodeEnd = false;
    [SerializeField] private bool enableContextualJumpMask = false;
    [SerializeField] private bool enforceLowCoinRunGroundCollection = false;
    [SerializeField] private bool requireGroundedBetweenHighCoins = false;
    [SerializeField] private float sameJumpSecondCoinPenalty = -2f;
    [SerializeField] private bool endEpisodeOnSameJumpSecondCoin = false;
    [SerializeField] private float missedCoinPenalty = -2f;
    [SerializeField] private float missedCoinForwardMargin = 2.5f;

    [Header("ObjectAware Mixed Random Low/High Gate")]
    [SerializeField] private bool requireGroundedLowCoin = false;
    [SerializeField] private float airborneLowCoinPenalty = -2f;
    [SerializeField] private bool endEpisodeOnAirborneLowCoin = false;
    [SerializeField] private bool requireGroundedBetweenLowAndHigh = false;
    [SerializeField] private float sameJumpHighCoinPenalty = -2f;
    [SerializeField] private bool endEpisodeOnSameJumpHighCoin = false;
    [SerializeField] private float finalLongZone4WarmupLandingGateX = 11f;
    [SerializeField] private float finalLongHighCoin01LandingGateX = 16.5f;
    [SerializeField] private float finalLongAndroid01LandingGateX = 33.9f;
    [SerializeField] private float finalLongAndroid02LandingGateX = 78.3f;

    [Header("ObjectAware Low Coin Rewards")]
    [SerializeField] private float lowCoinGroundApproachReward = 0.01f;
    [SerializeField] private float lowCoinGroundedAlignmentReward = 0.005f;
    [SerializeField] private float lowCoinUnnecessaryJumpPenalty = -0.02f;

    [Header("ObjectAware High Coin Rewards")]
    [SerializeField] private float highCoinApproachReward = 0.01f;
    [SerializeField] private float highCoinJumpCueReward = 0.04f;
    [SerializeField] private float earlyJumpPenalty = -0.01f;
    [SerializeField] private float jumpSpamPenalty = -0.01f;

    [Header("ObjectAware High Coin Approach Discipline")]
    [SerializeField] private bool enableHighCoinApproachDiscipline = false;
    [SerializeField] private float highCoinEarlyJumpDistance = 4f;
    [SerializeField] private float highCoinJumpWindowDistanceMin = 1f;
    [SerializeField] private float highCoinJumpWindowDistanceMax = 3f;
    [SerializeField] private float highCoinEarlyJumpPenalty = -0.02f;
    [SerializeField] private float highCoinGroundedApproachReward = 0.005f;
    [SerializeField] private bool debugHighCoinApproachDiscipline = false;

    [Header("ObjectAware Static Android Stomp")]
    [SerializeField] private float enemyApproachReward = 0.01f;
    [SerializeField] private float enemyStompWindowReward = 0.05f;
    [SerializeField] private float enemyStompWindowHorizontalRange = 3.5f;
    [SerializeField] private float missedEnemyPenalty = -3f;
    [SerializeField] private float missedEnemyForwardMargin = 2.5f;
    [SerializeField] private bool endEpisodeOnMissedEnemy = false;

    [Header("ObjectAware Gap Context")]
    [SerializeField] private float nearGapProbeDistance = 0.8f;
    [SerializeField] private float midGapProbeDistance = 1.6f;
    [SerializeField] private float farGapProbeDistance = 2.4f;
    [SerializeField] private float landingProbeDistance = 3.4f;
    [SerializeField] private float gapProbeDepth = 3.5f;

    [Header("ObjectAware FinalRandom Anti Ledge Stuck")]
    [SerializeField] private bool enableAntiLedgeStuckFailSafe = false;
    [SerializeField] private float ledgeStuckGraceTime = 0.5f;
    [SerializeField] private float ledgeStuckMinYBelowGround = 0.25f;
    [SerializeField] private float ledgeStuckMaxVelocity = 0.25f;
    [SerializeField] private float ledgeStuckProgressEpsilon = 0.03f;
    [SerializeField] private float ledgeStuckPenalty = -4f;
    [SerializeField] private bool debugAntiLedgeStuck = false;

    [Header("ObjectAware Debug")]
    [SerializeField] private bool debugObjectAwareObservationCount = false;
    [SerializeField] private bool debugObjectAwareNextObjective = false;
    [SerializeField] private bool debugObjectAwareJumpContext = false;
    [SerializeField] private bool debugObjectAwareGizmos = false;
    [SerializeField] private bool debugObjectAwareFinalLongValidation = false;
    [SerializeField] private bool debugFinalLongFailureReason = false;
    [SerializeField] private float debugObjectAwareLogInterval = 1f;

    private static readonly FieldInfo BaseGroundLayerField =
        typeof(EdgeRunnerAgentV5).GetField("groundLayer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseCoyoteTimerField =
        typeof(EdgeRunnerAgentV5).GetField("coyoteTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseDebugEpisodeResetReasonField =
        typeof(EdgeRunnerAgentV5).GetField(
            "debugEpisodeResetReason",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo VectorSensorObservationsField =
        typeof(VectorSensor).GetField("m_Observations", BindingFlags.Instance | BindingFlags.NonPublic);

    private ScoreAttackCoin[] cachedCoins = Array.Empty<ScoreAttackCoin>();
    private ScoreAttackAndroid[] cachedAndroids = Array.Empty<ScoreAttackAndroid>();
    private float nextObjectScanTime;
    private float nextDebugLogTime;
    private bool loggedObservationCountThisEpisode;
    private bool warnedObservationMismatchThisEpisode;
    private Transform previousTrainingObjective;
    private float previousTrainingObjectiveDistance;
    private float previousTrainingObjectiveHorizontalDistance;
    private bool hasPreviousTrainingObjectiveDistance;
    private int previousObjectAwareJumpAction;
    private bool objectAwareEpisodeEnding;
    private bool firstHighCoinCollectedThisEpisode;
    private bool awaitingGroundedAfterHighCoin;
    private bool hasLandedAfterPreviousHighCoin;
    private bool sameJumpCoinAttempt;
    private bool mixedRandomLowCoinCollectedThisEpisode;
    private bool mixedRandomAwaitingGroundedAfterLowCoin;
    private bool mixedRandomHasLandedAfterLowCoin;
    private bool mixedRandomSameJumpHighCoinAttempt;
    private bool mixedRandomAirborneLowCoinAttempt;
    private int mixedRandomLowCoinCollectionFrame = -1;
    private bool finalLongAwaitingGroundedAfterHighCoin;
    private bool finalLongHasLandedAfterHighCoin;
    private int finalLongHighCoinCollectionFrame = -1;
    private bool finalLongZone4WarmupHasLandedAfterGap;
    private bool finalLongHasLandedBeforeHighCoin01;
    private bool finalLongHasLandedBeforeAndroid01;
    private bool finalLongHasLandedBeforeAndroid02;
    private bool finalLongAwaitingGroundedAfterAndroid;
    private bool finalLongHasLandedAfterAndroid;
    private int finalLongAndroidStompFrame = -1;
    private string finalLongLastStompedAndroidName = "none";
    private string lastCompletedFinalLongObjective = "none";
    private bool lastCoinCollectionWasNextObjective;
    private bool lastCoinCollectionGrounded;
    private bool lastCoinCollectionGroundedByProbe;
    private string lastCoinCollectionReason = "not_attempted";
    private string lastCoinCollectionObjective = "none";
    private bool missedEnemyPenaltyAppliedThisEpisode;
    private ScoreAttackManager objectAwareScoreAttackManager;
    private EdgeRunnerEvaluationManager objectAwareEvaluationManager;
    private Collider2D antiLedgeCollider;
    private bool hasAntiLedgeLastGroundedLevel;
    private float antiLedgeLastGroundedFeetY;
    private float antiLedgeCandidateStartTime = -1f;
    private Vector2 antiLedgeCandidateAnchor;
    private bool antiLedgeEpisodeEnding;
    private Transform previousHighCoinDisciplineTarget;
    private float previousHighCoinDisciplineDistanceX;
    private bool hasPreviousHighCoinDisciplineDistance;
    private float nextHighCoinDisciplineDebugTime;
    private readonly HashSet<Transform> rewardedHighCoinJumpCues = new HashSet<Transform>();
    private readonly HashSet<Transform> rewardedEnemyStompWindows = new HashSet<Transform>();

    private enum ObjectAwareObjectiveType
    {
        None,
        Coin,
        Android,
        Goal
    }

    private struct TargetSnapshot
    {
        public bool exists;
        public Transform target;
        public ObjectAwareObjectiveType type;
        public Vector2 delta;
        public float distance;
        public bool ahead;
    }

    private struct GapSnapshot
    {
        public bool nearMissing;
        public bool midMissing;
        public bool farMissing;
        public bool landingAvailable;
        public bool gapAhead;
        public Vector3 nearProbe;
        public Vector3 midProbe;
        public Vector3 farProbe;
        public Vector3 landingProbe;
    }

    private struct ObjectAwareContext
    {
        public int coinsRemaining;
        public int enemiesRemaining;
        public bool objectivesComplete;
        public TargetSnapshot nextObjective;
        public TargetSnapshot lowCoin;
        public TargetSnapshot highCoin;
        public TargetSnapshot android;
        public GapSnapshot gap;
        public bool grounded;
        public float coyoteTimer;
        public Vector2 velocity;
        public bool nextRequiresJump;
        public bool lowCoinRunContext;
        public bool highCoinJumpContext;
        public bool androidStompContext;
        public bool coinNeedsJump;
        public bool coinRunCollectable;
        public bool coinJumpWindow;
        public bool jumpContextValid;
        public bool shouldNotJumpNow;
        public bool highCoinRequiresLanding;
        public bool hasLandedAfterPreviousHighCoin;
        public bool sameJumpCoinAttempt;
        public bool lowCoinRequiresGrounded;
        public bool hasLandedAfterLowCoin;
        public bool highCoinLockedUntilLanding;
        public bool sameJumpHighCoinAttempt;
        public bool airborneLowCoinAttempt;
        public bool enemyAhead;
        public bool enemySideDanger;
        public bool enemyAvoidContext;
        public bool enemyStompWindow;
    }

    public override void Initialize()
    {
        ConfigureFinalLongFailureDebug();
        ResolveObjectAwareReferences();
        base.Initialize();
        ResolveObjectAwareReferences();
        RefreshObjectCache(true);
    }

    public override void OnEpisodeBegin()
    {
        ConfigureFinalLongFailureDebug();
        ResolveObjectAwareReferences();
        base.OnEpisodeBegin();
        ResolveObjectAwareReferences();
        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup)
        {
            mixedRandomWarmupRandomizer?.RandomizeEpisode();
        }
        else if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom)
        {
            finalRandomizer?.RandomizeEpisode();
        }

        RefreshObjectCache(true);
        loggedObservationCountThisEpisode = false;
        warnedObservationMismatchThisEpisode = false;
        ResetCurriculumState();

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongChallenge &&
            debugObjectAwareFinalLongValidation)
        {
            LogFinalLongChallengeValidation();
        }
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        base.WriteDiscreteActionMask(actionMask);

        if (!enableContextualJumpMask ||
            (objectAwarePhase != EdgeRunnerObjectAwarePhase.LowCoinRun &&
             !IsMixedWarmupCurriculum()))
        {
            return;
        }

        ResolveObjectAwareReferences();
        RefreshObjectCache(false);
        ObjectAwareContext context = BuildContext();
        if (ShouldBlockLowCoinJump(context))
        {
            actionMask.SetActionEnabled(JumpBranchIndex, JumpAction, false);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        ConfigureFinalLongFailureDebug();
        ResolveObjectAwareReferences();
        RefreshObjectCache(false);
        ObjectAwareContext context = BuildContext();
        int jumpAction = actions.DiscreteActions.Length > JumpBranchIndex
            ? actions.DiscreteActions[JumpBranchIndex]
            : NoJumpAction;
        bool jumpRequested = jumpAction == JumpAction;
        bool jumpPressed = jumpRequested && previousObjectAwareJumpAction == NoJumpAction;
        bool blockLowCoinJump = ShouldBlockLowCoinJump(context);

        bool endedForMissedCoin = ApplyObjectAwareCurriculumRewards(
            context,
            jumpPressed,
            jumpRequested);
        previousObjectAwareJumpAction = jumpAction;
        if (endedForMissedCoin)
        {
            return;
        }

        if (TryEndEpisodeForAntiLedgeStuck(context.grounded))
        {
            return;
        }

        base.OnActionReceived(
            blockLowCoinJump && jumpRequested
                ? WithoutJumpAction(actions)
                : actions);
    }

    public EdgeRunnerObjectAwareEvaluationSnapshot GetEvaluationSnapshot(bool goalReached)
    {
        ResolveObjectAwareReferences();
        if (objectAwareScoreAttackManager == null)
        {
            return default;
        }

        bool allCoinsCollected = objectAwareScoreAttackManager.CoinsRemaining == 0;
        bool androidStomped = objectAwareScoreAttackManager.EnemiesRemaining == 0;
        return new EdgeRunnerObjectAwareEvaluationSnapshot
        {
            valid = true,
            goalReached = goalReached,
            allCoinsCollected = allCoinsCollected,
            androidStomped = androidStomped,
            fullObjectiveSuccess = goalReached && allCoinsCollected && androidStomped,
            coinsCollected = objectAwareScoreAttackManager.CoinsCollected,
            coinsRemaining = objectAwareScoreAttackManager.CoinsRemaining,
            enemiesKilled = objectAwareScoreAttackManager.EnemiesKilled,
            enemiesRemaining = objectAwareScoreAttackManager.EnemiesRemaining,
            prematureGoalTouches = objectAwareScoreAttackManager.PrematureGoalTouches
        };
    }

    public void EndEpisodeWithObjectAwareEvaluationReason(EdgeRunnerEpisodeEndReason reason)
    {
        objectAwareEpisodeEnding = true;
        LogFinalLongFailureReason(reason.ToString());
        ResolveObjectAwareEvaluationManager();
        objectAwareEvaluationManager?.NotifyEpisodeEnded(this, reason);
        EndEpisode();
    }

    public bool TryAcceptScoreAttackCoinCollection(ScoreAttackCoin coin)
    {
        if (coin == null)
        {
            return false;
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup)
        {
            RefreshObjectCache(true);
            ScoreAttackCoin lowCoin = FindCoinByName("MixedRandomWarmup_LowCoin");
            ScoreAttackCoin highCoin = FindCoinByName("MixedRandomWarmup_HighCoin");

            if (coin == lowCoin && IsLiveCoin(lowCoin))
            {
                bool grounded = IsCurrentlyGroundedForEvaluation();
                if (requireGroundedLowCoin && !grounded)
                {
                    mixedRandomAirborneLowCoinAttempt = true;
                    AddReward(airborneLowCoinPenalty);

                    if (debugObjectAwareNextObjective)
                    {
                        Debug.LogWarning(
                            $"[OBJECT AWARE LOW COIN BLOCK] target={coin.name} " +
                            $"lowCoinRequiresGrounded={requireGroundedLowCoin} " +
                            $"grounded={grounded} airborneLowCoinAttempt=true " +
                            $"penalty={airborneLowCoinPenalty:F2}",
                            this);
                    }

                    if (endEpisodeOnAirborneLowCoin)
                    {
                        objectAwareEpisodeEnding = true;
                        EndEpisodeWithObjectAwareEvaluationReason(
                            EdgeRunnerEpisodeEndReason.ObjectAwareAirborneLowCoin);
                    }

                    return false;
                }

                mixedRandomLowCoinCollectedThisEpisode = true;
                mixedRandomAwaitingGroundedAfterLowCoin =
                    requireGroundedBetweenLowAndHigh;
                mixedRandomHasLandedAfterLowCoin =
                    !requireGroundedBetweenLowAndHigh;
                mixedRandomSameJumpHighCoinAttempt = false;
                mixedRandomAirborneLowCoinAttempt = false;
                mixedRandomLowCoinCollectionFrame = Time.frameCount;
                return true;
            }

            if (coin != highCoin || IsLiveCoin(lowCoin) ||
                !mixedRandomLowCoinCollectedThisEpisode)
            {
                return false;
            }

            UpdateMixedRandomLowHighLandingState();
            if (MixedRandomHighCoinLockedUntilLanding())
            {
                mixedRandomSameJumpHighCoinAttempt = true;
                AddReward(sameJumpHighCoinPenalty);

                if (debugObjectAwareNextObjective)
                {
                    Debug.LogWarning(
                        $"[OBJECT AWARE MIXED HIGH COIN BLOCK] target={coin.name} " +
                        $"highCoinLockedUntilLanding=true " +
                        $"hasLandedAfterLowCoin={mixedRandomHasLandedAfterLowCoin} " +
                        $"sameJumpHighCoinAttempt=true penalty={sameJumpHighCoinPenalty:F2}",
                        this);
                }

                if (endEpisodeOnSameJumpHighCoin)
                {
                    objectAwareEpisodeEnding = true;
                    EndEpisodeWithObjectAwareEvaluationReason(
                        EdgeRunnerEpisodeEndReason.ObjectAwareSameJumpHighCoin);
                }

                return false;
            }

            return true;
        }

        if (IsFinalLongOrderedCurriculum())
        {
            return TryAcceptFinalLongChallengeCoin(coin);
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom)
        {
            RefreshObjectCache(true);
            ScoreAttackCoin lowCoin = FindFirstLiveOrderedCoin(true);
            ScoreAttackCoin highCoin = FindFirstLiveOrderedCoin(false);

            if (coin == lowCoin && IsLiveCoin(lowCoin))
            {
                bool grounded = IsCurrentlyGroundedForEvaluation();
                if (requireGroundedLowCoin && !grounded)
                {
                    mixedRandomAirborneLowCoinAttempt = true;
                    AddReward(airborneLowCoinPenalty);

                    if (debugObjectAwareNextObjective)
                    {
                        Debug.LogWarning(
                            $"[OBJECT AWARE LOW COIN BLOCK] target={coin.name} " +
                            $"lowCoinRequiresGrounded={requireGroundedLowCoin} " +
                            $"grounded={grounded} airborneLowCoinAttempt=true " +
                            $"penalty={airborneLowCoinPenalty:F2}",
                            this);
                    }

                    if (endEpisodeOnAirborneLowCoin)
                    {
                        objectAwareEpisodeEnding = true;
                        EndEpisodeWithObjectAwareEvaluationReason(
                            EdgeRunnerEpisodeEndReason.ObjectAwareAirborneLowCoin);
                    }

                    return false;
                }

                bool hasAnotherLowCoin =
                    FindFirstLiveOrderedCoin(true, coin) != null;
                mixedRandomLowCoinCollectedThisEpisode = !hasAnotherLowCoin;
                mixedRandomAwaitingGroundedAfterLowCoin =
                    !hasAnotherLowCoin && requireGroundedBetweenLowAndHigh;
                mixedRandomHasLandedAfterLowCoin =
                    hasAnotherLowCoin || !requireGroundedBetweenLowAndHigh;
                mixedRandomSameJumpHighCoinAttempt = false;
                mixedRandomAirborneLowCoinAttempt = false;
                mixedRandomLowCoinCollectionFrame = Time.frameCount;
                return true;
            }

            if (IsLiveCoin(lowCoin))
            {
                if (IsOrderedCurriculumCoin(coin, false))
                {
                    return RejectOrderedHighCoinAttempt(coin, "low_coin_still_alive");
                }

                return false;
            }

            if (coin != highCoin)
            {
                return IsOrderedCurriculumCoin(coin, false)
                    ? RejectOrderedHighCoinAttempt(coin, "high_coin_out_of_order")
                    : false;
            }

            if (!mixedRandomLowCoinCollectedThisEpisode)
            {
                return RejectOrderedHighCoinAttempt(coin, "low_stage_not_completed");
            }

            UpdateMixedRandomLowHighLandingState();
            if (MixedRandomHighCoinLockedUntilLanding())
            {
                return RejectOrderedHighCoinAttempt(coin, "landing_required");
            }

            bool hasAnotherHighCoin =
                FindFirstLiveOrderedCoin(false, coin) != null;
            mixedRandomAwaitingGroundedAfterLowCoin =
                hasAnotherHighCoin && requireGroundedBetweenLowAndHigh;
            mixedRandomHasLandedAfterLowCoin =
                !hasAnotherHighCoin || !requireGroundedBetweenLowAndHigh;
            mixedRandomSameJumpHighCoinAttempt = false;
            mixedRandomLowCoinCollectionFrame = Time.frameCount;
            return true;
        }

        if (!UsesHighCoinLandingCurriculum())
        {
            return true;
        }

        RefreshObjectCache(true);
        UpdateHighCoinLandingState();
        ScoreAttackCoin firstCoin = FindHighCoinSequenceCoin(true);
        ScoreAttackCoin secondCoin = FindHighCoinSequenceCoin(false);

        if (coin == firstCoin)
        {
            firstHighCoinCollectedThisEpisode = true;
            awaitingGroundedAfterHighCoin = true;
            hasLandedAfterPreviousHighCoin = false;
            sameJumpCoinAttempt = false;
            return true;
        }

        if (coin != secondCoin)
        {
            return true;
        }

        bool attemptedBeforeFirstCoin = !firstHighCoinCollectedThisEpisode;
        bool attemptedBeforeLanding =
            firstHighCoinCollectedThisEpisode && !hasLandedAfterPreviousHighCoin;
        if (!attemptedBeforeFirstCoin && !attemptedBeforeLanding)
        {
            awaitingGroundedAfterHighCoin = false;
            return true;
        }

        sameJumpCoinAttempt = attemptedBeforeLanding;
        AddReward(sameJumpSecondCoinPenalty);

        if (debugObjectAwareNextObjective)
        {
            Debug.LogWarning(
                $"[OBJECT AWARE HIGH COIN BLOCK] target={coin.name} " +
                $"highCoinRequiresLanding={attemptedBeforeLanding} " +
                $"hasLandedAfterPreviousHighCoin={hasLandedAfterPreviousHighCoin} " +
                $"sameJumpCoinAttempt={sameJumpCoinAttempt} " +
                $"penalty={sameJumpSecondCoinPenalty:F2}",
                this);
        }

        if (endEpisodeOnSameJumpSecondCoin)
        {
            objectAwareEpisodeEnding = true;
            EndEpisodeWithObjectAwareEvaluationReason(
                EdgeRunnerEpisodeEndReason.ObjectAwareSameJumpHighCoin);
        }

        return false;
    }

    private bool TryAcceptFinalLongChallengeCoin(ScoreAttackCoin coin)
    {
        RefreshObjectCache(true);
        UpdateFinalLongChallengeLandingState();
        UpdateFinalLongHighCoin01ApproachLandingGate();
        UpdateFinalLongAndroidLandingState();

        TargetSnapshot expectedObjective = FindFinalLongChallengeObjective(
            CreateTargetSnapshot(objectAwareGoal, ObjectAwareObjectiveType.Goal));
        bool isLowCoin = IsOrderedCurriculumCoin(coin, true);
        bool isHighCoin = IsOrderedCurriculumCoin(coin, false);
        bool isNextObjective = expectedObjective.target == coin.transform;
        bool grounded = IsCurrentlyGroundedForEvaluation();
        bool groundedByProbe = false;
        bool groundedForCollection = grounded ||
            (isLowCoin && IsSupportedForFinalLongLowCoinCollection(out groundedByProbe));

        lastCoinCollectionWasNextObjective = isNextObjective;
        lastCoinCollectionGrounded = grounded;
        lastCoinCollectionGroundedByProbe = groundedByProbe;
        lastCoinCollectionObjective = expectedObjective.target != null
            ? expectedObjective.target.name
            : FinalLongOrderedLandingRequired()
                ? "landing_required"
                : "none";

        if (!isNextObjective)
        {
            lastCoinCollectionReason = FinalLongOrderedLandingRequired()
                ? "landing_required"
                : "objective_out_of_order";
            if (isHighCoin)
            {
                return RejectOrderedHighCoinAttempt(coin, lastCoinCollectionReason);
            }

            return false;
        }

        if (isLowCoin)
        {
            if (requireGroundedLowCoin && !groundedForCollection)
            {
                lastCoinCollectionReason = "airborne_low_coin";
                mixedRandomAirborneLowCoinAttempt = true;
                AddReward(airborneLowCoinPenalty);

                if (debugObjectAwareNextObjective)
                {
                    Debug.LogWarning(
                        $"[OBJECT AWARE LOW COIN BLOCK] target={coin.name} " +
                        $"lowCoinRequiresGrounded={requireGroundedLowCoin} " +
                        $"grounded={grounded} groundedByProbe={groundedByProbe} " +
                        "airborneLowCoinAttempt=true " +
                        $"penalty={airborneLowCoinPenalty:F2}",
                        this);
                }

                if (endEpisodeOnAirborneLowCoin)
                {
                    objectAwareEpisodeEnding = true;
                    EndEpisodeWithObjectAwareEvaluationReason(
                        EdgeRunnerEpisodeEndReason.ObjectAwareAirborneLowCoin);
                }

                return false;
            }

            lastCoinCollectionReason = "accepted_grounded_low_coin";
            mixedRandomAirborneLowCoinAttempt = false;
            lastCompletedFinalLongObjective = coin.name;
            return true;
        }

        if (!isHighCoin)
        {
            lastCoinCollectionReason = "invalid_coin_type";
            return false;
        }

        lastCoinCollectionReason = "accepted_high_coin";
        finalLongAwaitingGroundedAfterHighCoin = true;
        finalLongHasLandedAfterHighCoin = false;
        finalLongHighCoinCollectionFrame = Time.frameCount;
        mixedRandomSameJumpHighCoinAttempt = false;
        lastCompletedFinalLongObjective = coin.name;
        return true;
    }

    private bool IsSupportedForFinalLongLowCoinCollection(out bool groundedByProbe)
    {
        groundedByProbe = false;
        if (IsCurrentlyGroundedForEvaluation())
        {
            return true;
        }

        ResolveObjectAwareReferences();
        if (objectAwareRigidbody == null || antiLedgeCollider == null ||
            Mathf.Abs(objectAwareRigidbody.linearVelocity.y) > 0.5f)
        {
            return false;
        }

        Bounds bounds = antiLedgeCollider.bounds;
        float horizontalInset = Mathf.Min(bounds.extents.x * 0.55f, 0.3f);
        float probeY = bounds.min.y + 0.05f;
        LayerMask groundMask = GetBaseGroundLayer();
        for (int i = -1; i <= 1; i++)
        {
            float x = bounds.center.x + horizontalInset * i;
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(x, probeY),
                Vector2.down,
                0.22f,
                groundMask.value);
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                groundedByProbe = true;
                return true;
            }
        }

        return false;
    }

    public void GetLastCoinCollectionDecision(
        out bool isNextObjective,
        out bool grounded,
        out bool groundedByProbe,
        out string reason,
        out string currentObjective,
        out string phase)
    {
        isNextObjective = lastCoinCollectionWasNextObjective;
        grounded = lastCoinCollectionGrounded;
        groundedByProbe = lastCoinCollectionGroundedByProbe;
        reason = lastCoinCollectionReason;
        currentObjective = lastCoinCollectionObjective;
        phase = objectAwarePhase.ToString();
    }

    public bool TryAcceptScoreAttackAndroidStomp(ScoreAttackAndroid android)
    {
        if (android == null)
        {
            return false;
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup)
        {
            RefreshObjectCache(true);
            ScoreAttackCoin lowCoin = FindCoinByName("MixedRandomWarmup_LowCoin");
            ScoreAttackCoin highCoin = FindCoinByName("MixedRandomWarmup_HighCoin");
            return !IsLiveCoin(lowCoin) && !IsLiveCoin(highCoin);
        }

        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalRandom &&
            !IsFinalLongOrderedCurriculum())
        {
            return true;
        }

        RefreshObjectCache(true);
        if (IsFinalLongOrderedCurriculum())
        {
            UpdateFinalLongChallengeLandingState();
            UpdateFinalLongAndroidApproachLandingGate();
            UpdateFinalLongAndroidLandingState();
            TargetSnapshot expectedObjective = FindFinalLongChallengeObjective(
                CreateTargetSnapshot(objectAwareGoal, ObjectAwareObjectiveType.Goal));
            bool accepted = expectedObjective.type == ObjectAwareObjectiveType.Android &&
                expectedObjective.target == android.transform;
            if (accepted)
            {
                lastCompletedFinalLongObjective = android.name;
                finalLongAwaitingGroundedAfterAndroid = true;
                finalLongHasLandedAfterAndroid = false;
                finalLongAndroidStompFrame = Time.frameCount;
                finalLongLastStompedAndroidName = android.name;
            }

            return accepted;
        }

        return FindFirstLiveOrderedCoin(true) == null &&
            FindFirstLiveOrderedCoin(false) == null;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        ResolveObjectAwareReferences();
        RefreshObjectCache(false);

        int startCount = GetObservationCount(sensor);
        base.CollectObservations(sensor);
        int afterBaseCount = GetObservationCount(sensor);

        ObjectAwareContext context = BuildContext();
        int globalCount = AddGlobalObjectiveObservations(sensor, context);
        int nextCount = AddNextObjectiveObservations(sensor, context);
        int lowCoinCount = AddCoinObservations(sensor, context.lowCoin, true);
        int highCoinCount = AddCoinObservations(sensor, context.highCoin, false);
        int androidCount = AddAndroidObservations(sensor, context);
        int jumpCount = AddJumpContextObservations(sensor, context);
        int finalCount = GetObservationCount(sensor);

        ValidateObservationCount(
            startCount,
            afterBaseCount,
            finalCount,
            globalCount,
            nextCount,
            lowCoinCount,
            highCoinCount,
            androidCount,
            jumpCount);
        LogObjectAwareContext(context);
    }

    private int AddGlobalObjectiveObservations(VectorSensor sensor, ObjectAwareContext context)
    {
        int count = 0;
        Add(sensor, NormalizePositive(context.coinsRemaining, maxCoinCount), ref count);
        Add(sensor, NormalizePositive(context.enemiesRemaining, maxEnemyCount), ref count);
        Add(sensor, context.coinsRemaining > 0, ref count);
        Add(sensor, context.enemiesRemaining > 0, ref count);
        Add(sensor, context.objectivesComplete, ref count);
        Add(sensor, objectAwareGoal != null, ref count);
        Add(sensor, context.objectivesComplete && objectAwareGoal != null, ref count);
        Add(sensor, context.coinsRemaining == 0 && context.enemiesRemaining == 0, ref count);
        return count;
    }

    private int AddNextObjectiveObservations(VectorSensor sensor, ObjectAwareContext context)
    {
        int count = 0;
        TargetSnapshot target = context.nextObjective;
        Add(sensor, target.type == ObjectAwareObjectiveType.None, ref count);
        Add(sensor, target.type == ObjectAwareObjectiveType.Coin, ref count);
        Add(sensor, target.type == ObjectAwareObjectiveType.Android, ref count);
        Add(sensor, target.type == ObjectAwareObjectiveType.Goal, ref count);
        Add(sensor, NormalizeSigned(target.delta.x, maxObjectiveDistance), ref count);
        Add(sensor, NormalizeSigned(target.delta.y, maxObjectiveHeight), ref count);
        Add(sensor, NormalizePositive(target.distance, maxObjectiveDistance), ref count);
        Add(sensor, target.exists && target.ahead, ref count);
        Add(sensor, target.exists && !target.ahead, ref count);
        Add(sensor, context.nextRequiresJump, ref count);
        return count;
    }

    private int AddCoinObservations(VectorSensor sensor, TargetSnapshot coin, bool lowCoin)
    {
        int count = 0;
        float forwardDistance = coin.delta.x * GetForwardDirection();
        float contextWindow = lowCoin ? lowCoinRunWindowX : highCoinJumpWindowX;
        Add(sensor, coin.exists, ref count);
        Add(sensor, NormalizeSigned(coin.delta.x, maxObjectiveDistance), ref count);
        Add(sensor, NormalizeSigned(coin.delta.y, maxObjectiveHeight), ref count);
        Add(sensor, NormalizePositive(coin.distance, maxObjectiveDistance), ref count);
        Add(sensor, coin.exists && coin.ahead, ref count);
        Add(
            sensor,
            coin.exists && coin.ahead && forwardDistance <= contextWindow,
            ref count);
        return count;
    }

    private int AddAndroidObservations(VectorSensor sensor, ObjectAwareContext context)
    {
        int count = 0;
        TargetSnapshot android = context.android;
        float forwardDistance = android.delta.x * GetForwardDirection();
        bool horizontalDanger =
            android.exists && Mathf.Abs(android.delta.x) <= androidContextWindowX;
        bool verticalDanger =
            android.exists && Mathf.Abs(android.delta.y) <= androidVerticalTolerance;
        bool playerAbove = android.exists && -android.delta.y > lowCoinHeightThreshold;
        bool descending = objectAwareRigidbody != null && objectAwareRigidbody.linearVelocity.y <= 0.1f;

        Add(sensor, android.exists, ref count);
        Add(sensor, NormalizeSigned(android.delta.x, maxObjectiveDistance), ref count);
        Add(sensor, NormalizeSigned(android.delta.y, maxObjectiveHeight), ref count);
        Add(sensor, NormalizePositive(android.distance, maxObjectiveDistance), ref count);
        Add(sensor, android.exists && android.ahead, ref count);
        Add(sensor, horizontalDanger, ref count);
        Add(sensor, verticalDanger, ref count);
        Add(sensor, context.androidStompContext, ref count);
        Add(sensor, playerAbove, ref count);
        Add(sensor, descending, ref count);
        Add(sensor, android.exists && forwardDistance < 0f, ref count);
        Add(sensor, android.exists && android.target != null && android.target.gameObject.activeInHierarchy, ref count);
        return count;
    }

    private int AddJumpContextObservations(VectorSensor sensor, ObjectAwareContext context)
    {
        int count = 0;
        Add(sensor, context.grounded, ref count);
        Add(sensor, context.coyoteTimer > 0f, ref count);
        Add(sensor, NormalizeSigned(context.velocity.x, 12f), ref count);
        Add(sensor, NormalizeSigned(context.velocity.y, 20f), ref count);
        Add(sensor, context.gap.gapAhead, ref count);
        Add(sensor, context.gap.nearMissing, ref count);
        Add(sensor, context.gap.midMissing, ref count);
        Add(sensor, context.gap.landingAvailable, ref count);
        Add(sensor, context.nextRequiresJump, ref count);
        Add(sensor, context.jumpContextValid, ref count);
        Add(sensor, context.shouldNotJumpNow, ref count);
        Add(sensor, context.lowCoinRunContext, ref count);
        Add(sensor, context.highCoinJumpContext, ref count);
        Add(sensor, context.androidStompContext || context.enemyAvoidContext, ref count);
        return count;
    }

    private ObjectAwareContext BuildContext()
    {
        UpdateHighCoinLandingState();
        UpdateMixedRandomLowHighLandingState();
        UpdateFinalLongChallengeLandingState();
        UpdateFinalLongHighCoin01ApproachLandingGate();
        UpdateFinalLongAndroidApproachLandingGate();
        UpdateFinalLongAndroidLandingState();
        TargetSnapshot nearestCoin = FindNearestCoin(null);
        TargetSnapshot lowCoin = FindNearestCoin(true);
        TargetSnapshot highCoin = FindNearestCoin(false);
        TargetSnapshot android = FindNearestAndroid();
        TargetSnapshot goal = CreateTargetSnapshot(objectAwareGoal, ObjectAwareObjectiveType.Goal);
        bool highCoinRequiresLanding = HighCoinRequiresLanding();
        bool highCoinLockedUntilLanding =
            MixedRandomHighCoinLockedUntilLanding() ||
            FinalLongOrderedLandingRequired();
        bool staticAndroidAvoid =
            objectAwarePhase == EdgeRunnerObjectAwarePhase.StaticAndroidAvoid;
        bool staticAndroidStomp =
            objectAwarePhase == EdgeRunnerObjectAwarePhase.StaticAndroidStomp;
        bool mixedWarmup = IsMixedWarmupCurriculum();
        TargetSnapshot nextObjective;
        if (staticAndroidAvoid)
        {
            nextObjective = goal;
        }
        else if (mixedWarmup)
        {
            nextObjective = FindMixedWarmupObjective(android, goal);
        }
        else if (UsesHighCoinLandingCurriculum())
        {
            TargetSnapshot orderedHighCoin = FindHighCoinCurriculumObjective();
            nextObjective = orderedHighCoin.exists
                ? orderedHighCoin
                : nearestCoin.exists
                    ? default
                    : android.exists
                        ? android
                        : goal;
        }
        else
        {
            nextObjective = nearestCoin.exists
                ? nearestCoin
                : android.exists
                    ? android
                    : goal;
        }
        GapSnapshot gap = EvaluateGapContext();
        bool grounded = IsCurrentlyGroundedForEvaluation();
        float coyoteTimer = GetBaseCoyoteTimer();
        Vector2 velocity = objectAwareRigidbody != null
            ? objectAwareRigidbody.linearVelocity
            : Vector2.zero;
        float direction = GetForwardDirection();
        float lowCoinForwardDistance = lowCoin.delta.x * direction;
        float highCoinForwardDistance = highCoin.delta.x * direction;
        float androidForwardDistance = android.delta.x * direction;
        bool lowCoinRunContext =
            lowCoin.exists && lowCoin.ahead && lowCoinForwardDistance <= lowCoinRunWindowX;
        bool highCoinJumpContext =
            highCoin.exists &&
            !highCoinLockedUntilLanding &&
            highCoin.ahead &&
            highCoinForwardDistance <= highCoinJumpWindowX;
        bool stompCurriculumActive =
            staticAndroidStomp ||
            (mixedWarmup && nextObjective.type == ObjectAwareObjectiveType.Android);
        float stompWindowHorizontalRange = stompCurriculumActive
            ? enemyStompWindowHorizontalRange
            : androidContextWindowX;
        bool androidStompContext =
            android.exists &&
            android.ahead &&
            (!mixedWarmup || nextObjective.type == ObjectAwareObjectiveType.Android) &&
            androidForwardDistance <= stompWindowHorizontalRange &&
            Mathf.Abs(android.delta.y) <= androidVerticalTolerance;
        bool enemyAhead = android.exists && android.ahead;
        bool enemySideDanger =
            enemyAhead &&
            androidForwardDistance <= androidContextWindowX &&
            Mathf.Abs(android.delta.y) <= androidVerticalTolerance;
        bool enemyAvoidContext = staticAndroidAvoid && enemySideDanger;
        bool enemyStompWindow = stompCurriculumActive && androidStompContext;
        bool coinNeedsJump =
            nextObjective.type == ObjectAwareObjectiveType.Coin &&
            !IsLowCoin(nextObjective.delta);
        bool coinRunCollectable =
            nextObjective.type == ObjectAwareObjectiveType.Coin &&
            !coinNeedsJump;
        bool nextRequiresJump =
            gap.gapAhead ||
            coinNeedsJump ||
            enemyAvoidContext ||
            (nextObjective.type == ObjectAwareObjectiveType.Android && androidStompContext);
        bool coinJumpWindow =
            coinNeedsJump &&
            nextObjective.ahead &&
            nextObjective.delta.x * direction <= highCoinJumpWindowX;
        bool canInitiateJump = grounded || coyoteTimer > 0f;
        bool jumpContextValid =
            canInitiateJump &&
            (gap.gapAhead || highCoinJumpContext || androidStompContext || enemyAvoidContext);

        return new ObjectAwareContext
        {
            coinsRemaining = CountLiveCoins(),
            enemiesRemaining = CountLiveAndroids(),
            objectivesComplete =
                !nearestCoin.exists && (staticAndroidAvoid || !android.exists),
            nextObjective = nextObjective,
            lowCoin = lowCoin,
            highCoin = highCoin,
            android = android,
            gap = gap,
            grounded = grounded,
            coyoteTimer = coyoteTimer,
            velocity = velocity,
            nextRequiresJump = nextRequiresJump,
            lowCoinRunContext = lowCoinRunContext,
            highCoinJumpContext = highCoinJumpContext,
            androidStompContext = androidStompContext,
            coinNeedsJump = coinNeedsJump,
            coinRunCollectable = coinRunCollectable,
            coinJumpWindow = coinJumpWindow,
            jumpContextValid = jumpContextValid,
            shouldNotJumpNow = canInitiateJump && !jumpContextValid,
            highCoinRequiresLanding = highCoinRequiresLanding,
            hasLandedAfterPreviousHighCoin = hasLandedAfterPreviousHighCoin,
            sameJumpCoinAttempt = sameJumpCoinAttempt,
            lowCoinRequiresGrounded =
                UsesGroundedLowHighGateCurriculum() &&
                requireGroundedLowCoin,
            hasLandedAfterLowCoin = IsFinalLongOrderedCurriculum()
                    ? finalLongHasLandedAfterHighCoin
                    : mixedRandomHasLandedAfterLowCoin,
            highCoinLockedUntilLanding = highCoinLockedUntilLanding,
            sameJumpHighCoinAttempt = mixedRandomSameJumpHighCoinAttempt,
            airborneLowCoinAttempt = mixedRandomAirborneLowCoinAttempt,
            enemyAhead = enemyAhead,
            enemySideDanger = enemySideDanger,
            enemyAvoidContext = enemyAvoidContext,
            enemyStompWindow = enemyStompWindow
        };
    }

    private bool ApplyObjectAwareCurriculumRewards(
        ObjectAwareContext context,
        bool jumpPressed,
        bool jumpRequested)
    {
        if (!enableObjectAwareRewardShaping ||
            objectAwarePhase == EdgeRunnerObjectAwarePhase.TraversalBase ||
            objectAwareEpisodeEnding)
        {
            return false;
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.StaticAndroidStomp)
        {
            return ApplyStaticAndroidStompCurriculum(context, jumpPressed);
        }

        if (IsMixedWarmupCurriculum() &&
            context.nextObjective.type == ObjectAwareObjectiveType.Android)
        {
            return ApplyStaticAndroidStompCurriculum(context, jumpPressed);
        }

        TargetSnapshot objective = context.nextObjective;
        if (objective.type != ObjectAwareObjectiveType.Coin || !objective.exists)
        {
            ClearPreviousTrainingObjective();
            ResetHighCoinApproachDisciplineTracking();
            return false;
        }

        UpdateObjectiveProgressReward(context);

        if (context.coinRunCollectable)
        {
            ResetHighCoinApproachDisciplineTracking();
            ApplyLowCoinRunReward(context, jumpRequested);
        }
        else if (context.coinNeedsJump)
        {
            ApplyHighCoinApproachDiscipline(context, jumpPressed, jumpRequested);
            ApplyHighCoinJumpReward(context, jumpPressed);
        }

        if (enableMissedCoinEpisodeEnd)
        {
            return EndEpisodeForMissedCoinIfNeeded();
        }

        return false;
    }

    private bool ApplyStaticAndroidStompCurriculum(
        ObjectAwareContext context,
        bool jumpPressed)
    {
        TargetSnapshot objective = context.nextObjective;
        if (objective.type == ObjectAwareObjectiveType.Android && objective.exists)
        {
            UpdateEnemyApproachReward(objective);

            if (context.enemyStompWindow &&
                rewardedEnemyStompWindows.Add(objective.target))
            {
                AddReward(enemyStompWindowReward);
            }

            if (jumpPressed &&
                !context.enemyStompWindow &&
                !context.gap.gapAhead)
            {
                AddReward(jumpSpamPenalty);
            }
        }
        else
        {
            ClearPreviousTrainingObjective();
        }

        return EndEpisodeForMissedEnemyIfNeeded();
    }

    private void UpdateEnemyApproachReward(TargetSnapshot objective)
    {
        float currentDistance = objective.distance;
        if (!hasPreviousTrainingObjectiveDistance || previousTrainingObjective != objective.target)
        {
            previousTrainingObjective = objective.target;
            previousTrainingObjectiveDistance = currentDistance;
            previousTrainingObjectiveHorizontalDistance = Mathf.Abs(objective.delta.x);
            hasPreviousTrainingObjectiveDistance = true;
            return;
        }

        float delta = previousTrainingObjectiveDistance - currentDistance;
        if (delta > 0f)
        {
            AddReward(Mathf.Clamp(delta, 0f, 1f) * enemyApproachReward);
        }

        previousTrainingObjectiveDistance = currentDistance;
        previousTrainingObjectiveHorizontalDistance = Mathf.Abs(objective.delta.x);
    }

    private void UpdateObjectiveProgressReward(ObjectAwareContext context)
    {
        TargetSnapshot objective = context.nextObjective;
        float currentDistance = objective.distance;
        float currentHorizontalDistance = Mathf.Abs(objective.delta.x);

        if (!hasPreviousTrainingObjectiveDistance || previousTrainingObjective != objective.target)
        {
            previousTrainingObjective = objective.target;
            previousTrainingObjectiveDistance = currentDistance;
            previousTrainingObjectiveHorizontalDistance = currentHorizontalDistance;
            hasPreviousTrainingObjectiveDistance = true;
            return;
        }

        bool lowCoinObjective = context.coinRunCollectable;
        float rewardScale = lowCoinObjective
            ? lowCoinGroundApproachReward
            : highCoinApproachReward;
        float previousDistance = lowCoinObjective
            ? previousTrainingObjectiveHorizontalDistance
            : previousTrainingObjectiveDistance;
        float distance = lowCoinObjective
            ? currentHorizontalDistance
            : currentDistance;
        float delta = previousDistance - distance;

        if (delta > 0f)
        {
            AddReward(Mathf.Clamp(delta, 0f, 1f) * rewardScale);
        }
        else if (delta < 0f)
        {
            AddReward(Mathf.Clamp(delta, -1f, 0f) * rewardScale * 0.5f);
        }

        previousTrainingObjectiveDistance = currentDistance;
        previousTrainingObjectiveHorizontalDistance = currentHorizontalDistance;
    }

    private void ApplyLowCoinRunReward(ObjectAwareContext context, bool jumpRequested)
    {
        bool validLowCoinContext =
            context.coinRunCollectable &&
            context.nextObjective.ahead &&
            Mathf.Abs(context.nextObjective.delta.x) <= lowCoinRunWindowX;

        if (validLowCoinContext && context.grounded)
        {
            AddReward(lowCoinGroundedAlignmentReward);
        }

        if (jumpRequested && ShouldBlockLowCoinJump(context))
        {
            AddReward(lowCoinUnnecessaryJumpPenalty);
        }
    }

    private void ApplyHighCoinJumpReward(ObjectAwareContext context, bool jumpPressed)
    {
        if (!jumpPressed)
        {
            return;
        }

        bool validHighCoinJump =
            context.coinNeedsJump &&
            context.coinJumpWindow &&
            (context.grounded || context.coyoteTimer > 0f);

        if (validHighCoinJump)
        {
            if (rewardedHighCoinJumpCues.Add(context.nextObjective.target))
            {
                AddReward(highCoinJumpCueReward);
            }

            return;
        }

        bool disciplineActive = IsHighCoinApproachDisciplineActive();
        if (context.coinNeedsJump && !disciplineActive)
        {
            AddReward(earlyJumpPenalty);
        }

        if (!disciplineActive &&
            !context.gap.gapAhead &&
            !context.highCoinJumpContext &&
            !context.androidStompContext)
        {
            AddReward(jumpSpamPenalty);
        }
    }

    private void ApplyHighCoinApproachDiscipline(
        ObjectAwareContext context,
        bool jumpPressed,
        bool jumpRequested)
    {
        if (!IsHighCoinApproachDisciplineActive() ||
            context.nextObjective.type != ObjectAwareObjectiveType.Coin ||
            !context.nextObjective.exists ||
            !context.coinNeedsJump)
        {
            ResetHighCoinApproachDisciplineTracking();
            return;
        }

        Transform target = context.nextObjective.target;
        float distanceX = Mathf.Abs(context.nextObjective.delta.x);
        bool inJumpWindow =
            distanceX >= highCoinJumpWindowDistanceMin &&
            distanceX <= highCoinJumpWindowDistanceMax;
        bool farFromJumpWindow = distanceX > highCoinEarlyJumpDistance;
        bool ignoredBecauseGapOrAndroid =
            context.gap.gapAhead ||
            context.androidStompContext ||
            context.nextObjective.type == ObjectAwareObjectiveType.Android;
        bool canExecuteJump = context.grounded || context.coyoteTimer > 0f;
        bool earlyJumpPenaltyApplied =
            jumpPressed &&
            canExecuteJump &&
            farFromJumpWindow &&
            !inJumpWindow &&
            !ignoredBecauseGapOrAndroid;

        float groundedApproachRewardApplied = 0f;
        if (hasPreviousHighCoinDisciplineDistance &&
            previousHighCoinDisciplineTarget == target)
        {
            float progress = previousHighCoinDisciplineDistanceX - distanceX;
            if (context.grounded &&
                farFromJumpWindow &&
                progress > 0f &&
                !ignoredBecauseGapOrAndroid)
            {
                groundedApproachRewardApplied =
                    Mathf.Clamp(progress, 0f, 1f) *
                    highCoinGroundedApproachReward;
                AddReward(groundedApproachRewardApplied);
            }
        }

        if (earlyJumpPenaltyApplied)
        {
            AddReward(highCoinEarlyJumpPenalty);
        }

        previousHighCoinDisciplineTarget = target;
        previousHighCoinDisciplineDistanceX = distanceX;
        hasPreviousHighCoinDisciplineDistance = true;

        if (debugHighCoinApproachDiscipline &&
            (jumpPressed ||
             groundedApproachRewardApplied > 0f ||
             Time.time >= nextHighCoinDisciplineDebugTime))
        {
            nextHighCoinDisciplineDebugTime =
                Time.time + Mathf.Max(0.05f, debugObjectAwareLogInterval);
            Debug.Log(
                $"[HIGH COIN APPROACH] currentHighCoin={target.name} " +
                $"distanceX={distanceX:F2} grounded={context.grounded} " +
                $"verticalVelocity={context.velocity.y:F2} " +
                $"jumpAction={(jumpRequested ? 1 : 0)} " +
                $"inJumpWindow={inJumpWindow} " +
                $"earlyJumpPenaltyApplied={earlyJumpPenaltyApplied} " +
                $"groundedApproachRewardApplied=" +
                $"{groundedApproachRewardApplied > 0f} " +
                $"ignoredBecauseGapOrAndroid={ignoredBecauseGapOrAndroid}",
                this);
        }
    }

    private bool IsHighCoinApproachDisciplineActive()
    {
        return enableHighCoinApproachDiscipline &&
            objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongChallenge;
    }

    private void ResetHighCoinApproachDisciplineTracking()
    {
        previousHighCoinDisciplineTarget = null;
        previousHighCoinDisciplineDistanceX = 0f;
        hasPreviousHighCoinDisciplineDistance = false;
    }

    private bool EndEpisodeForMissedCoinIfNeeded()
    {
        float direction = GetForwardDirection();
        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (coin == null || !coin.IsAvailable || !coin.gameObject.activeInHierarchy)
            {
                continue;
            }

            float passedDistance = (transform.position.x - coin.transform.position.x) * direction;
            if (passedDistance <= missedCoinForwardMargin)
            {
                continue;
            }

            objectAwareEpisodeEnding = true;
            AddReward(missedCoinPenalty);
            EndEpisodeWithObjectAwareEvaluationReason(
                EdgeRunnerEpisodeEndReason.ObjectAwareMissedCoin);
            return true;
        }

        return false;
    }

    private bool EndEpisodeForMissedEnemyIfNeeded()
    {
        if (missedEnemyPenaltyAppliedThisEpisode)
        {
            return false;
        }

        float direction = GetForwardDirection();
        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android == null || !android.IsAlive || !android.gameObject.activeInHierarchy)
            {
                continue;
            }

            float passedDistance =
                (transform.position.x - android.transform.position.x) * direction;
            if (passedDistance <= missedEnemyForwardMargin)
            {
                continue;
            }

            missedEnemyPenaltyAppliedThisEpisode = true;
            AddReward(missedEnemyPenalty);
            if (endEpisodeOnMissedEnemy)
            {
                objectAwareEpisodeEnding = true;
                EndEpisodeWithObjectAwareEvaluationReason(
                    EdgeRunnerEpisodeEndReason.ObjectAwareMissedEnemy);
                return true;
            }

            return false;
        }

        return false;
    }

    private void ResetCurriculumState()
    {
        previousTrainingObjective = null;
        previousTrainingObjectiveDistance = 0f;
        previousTrainingObjectiveHorizontalDistance = 0f;
        hasPreviousTrainingObjectiveDistance = false;
        previousObjectAwareJumpAction = 0;
        objectAwareEpisodeEnding = false;
        firstHighCoinCollectedThisEpisode = false;
        awaitingGroundedAfterHighCoin = false;
        hasLandedAfterPreviousHighCoin = false;
        sameJumpCoinAttempt = false;
        mixedRandomLowCoinCollectedThisEpisode = false;
        mixedRandomAwaitingGroundedAfterLowCoin = false;
        mixedRandomHasLandedAfterLowCoin = false;
        mixedRandomSameJumpHighCoinAttempt = false;
        mixedRandomAirborneLowCoinAttempt = false;
        mixedRandomLowCoinCollectionFrame = -1;
        finalLongAwaitingGroundedAfterHighCoin = false;
        finalLongHasLandedAfterHighCoin = true;
        finalLongHighCoinCollectionFrame = -1;
        finalLongZone4WarmupHasLandedAfterGap = false;
        finalLongHasLandedBeforeHighCoin01 = false;
        finalLongHasLandedBeforeAndroid01 = false;
        finalLongHasLandedBeforeAndroid02 = false;
        finalLongAwaitingGroundedAfterAndroid = false;
        finalLongHasLandedAfterAndroid = false;
        finalLongAndroidStompFrame = -1;
        finalLongLastStompedAndroidName = "none";
        lastCompletedFinalLongObjective = "none";
        lastCoinCollectionWasNextObjective = false;
        lastCoinCollectionGrounded = false;
        lastCoinCollectionGroundedByProbe = false;
        lastCoinCollectionReason = "not_attempted";
        lastCoinCollectionObjective = "none";
        missedEnemyPenaltyAppliedThisEpisode = false;
        ResetHighCoinApproachDisciplineTracking();
        nextHighCoinDisciplineDebugTime = 0f;
        ResetAntiLedgeStuckState();
        rewardedHighCoinJumpCues.Clear();
        rewardedEnemyStompWindows.Clear();
    }

    private bool TryEndEpisodeForAntiLedgeStuck(bool grounded)
    {
        if (!enableAntiLedgeStuckFailSafe ||
            (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalRandom &&
             !IsFinalLongOrderedCurriculum()) ||
            objectAwareEpisodeEnding || antiLedgeEpisodeEnding ||
            objectAwareRigidbody == null || antiLedgeCollider == null)
        {
            ResetAntiLedgeCandidate();
            return false;
        }

        float feetY = antiLedgeCollider.bounds.min.y;
        if (grounded)
        {
            hasAntiLedgeLastGroundedLevel = true;
            antiLedgeLastGroundedFeetY = feetY;
            ResetAntiLedgeCandidate();
            return false;
        }

        if (!hasAntiLedgeLastGroundedLevel)
        {
            ResetAntiLedgeCandidate();
            return false;
        }

        Vector2 velocity = objectAwareRigidbody.linearVelocity;
        bool belowLastGround =
            antiLedgeLastGroundedFeetY - feetY >= ledgeStuckMinYBelowGround;
        bool nearlyStopped =
            Mathf.Abs(velocity.x) <= ledgeStuckMaxVelocity &&
            Mathf.Abs(velocity.y) <= ledgeStuckMaxVelocity;
        bool touchingPlatformSide =
            antiLedgeCollider.IsTouchingLayers(GetBaseGroundLayer().value);

        if (!belowLastGround || !nearlyStopped || !touchingPlatformSide)
        {
            ResetAntiLedgeCandidate();
            return false;
        }

        Vector2 currentPosition = objectAwareRigidbody.position;
        bool candidateMoved = antiLedgeCandidateStartTime >= 0f &&
            Vector2.Distance(currentPosition, antiLedgeCandidateAnchor) >
                ledgeStuckProgressEpsilon;
        if (antiLedgeCandidateStartTime < 0f || candidateMoved)
        {
            antiLedgeCandidateStartTime = Time.time;
            antiLedgeCandidateAnchor = currentPosition;
            return false;
        }

        if (Time.time - antiLedgeCandidateStartTime < ledgeStuckGraceTime)
        {
            return false;
        }

        antiLedgeEpisodeEnding = true;
        objectAwareEpisodeEnding = true;
        AddReward(ledgeStuckPenalty);
        if (debugAntiLedgeStuck)
        {
            Debug.LogWarning(
                "[ANTI LEDGE STUCK] ending episode because agent is stuck on " +
                "platform edge after missed gap",
                this);
        }

        EndEpisodeWithObjectAwareEvaluationReason(
            EdgeRunnerEpisodeEndReason.ObjectAwareAntiLedgeStuck);
        return true;
    }

    private void ResetAntiLedgeCandidate()
    {
        antiLedgeCandidateStartTime = -1f;
        antiLedgeCandidateAnchor = objectAwareRigidbody != null
            ? objectAwareRigidbody.position
            : (Vector2)transform.position;
    }

    private void ResetAntiLedgeStuckState()
    {
        hasAntiLedgeLastGroundedLevel = false;
        antiLedgeLastGroundedFeetY = 0f;
        antiLedgeEpisodeEnding = false;
        ResetAntiLedgeCandidate();
    }

    private void ClearPreviousTrainingObjective()
    {
        previousTrainingObjective = null;
        hasPreviousTrainingObjectiveDistance = false;
    }

    private TargetSnapshot FindNearestCoin(bool? lowCoin)
    {
        TargetSnapshot best = default;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (coin == null || !coin.IsAvailable || !coin.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector2 delta = coin.transform.position - transform.position;
            bool isLow = IsLowCoin(delta);
            if (lowCoin.HasValue && isLow != lowCoin.Value)
            {
                continue;
            }

            float distance = delta.magnitude;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = CreateTargetSnapshot(coin.transform, ObjectAwareObjectiveType.Coin);
        }

        return best;
    }

    private TargetSnapshot FindHighCoinCurriculumObjective()
    {
        ScoreAttackCoin firstCoin = FindHighCoinSequenceCoin(true);
        if (IsLiveCoin(firstCoin))
        {
            return CreateTargetSnapshot(firstCoin.transform, ObjectAwareObjectiveType.Coin);
        }

        ScoreAttackCoin secondCoin = FindHighCoinSequenceCoin(false);
        if (IsLiveCoin(secondCoin) &&
            firstHighCoinCollectedThisEpisode &&
            hasLandedAfterPreviousHighCoin)
        {
            return CreateTargetSnapshot(secondCoin.transform, ObjectAwareObjectiveType.Coin);
        }

        return default;
    }

    private ScoreAttackCoin FindHighCoinSequenceCoin(bool first)
    {
        string expectedName = first
            ? "HighCoinJump_Coin_01"
            : "HighCoinJump_Coin_02";

        return FindCoinByName(expectedName);
    }

    private TargetSnapshot FindMixedWarmupObjective(
        TargetSnapshot android,
        TargetSnapshot goal)
    {
        if (IsFinalLongOrderedCurriculum())
        {
            return FindFinalLongChallengeObjective(goal);
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom)
        {
            ScoreAttackCoin orderedLowCoin = FindFirstLiveOrderedCoin(true);
            if (IsLiveCoin(orderedLowCoin))
            {
                return CreateTargetSnapshot(
                    orderedLowCoin.transform,
                    ObjectAwareObjectiveType.Coin);
            }

            ScoreAttackCoin orderedHighCoin = FindFirstLiveOrderedCoin(false);
            if (IsLiveCoin(orderedHighCoin))
            {
                if (MixedRandomHighCoinLockedUntilLanding())
                {
                    return default;
                }

                return CreateTargetSnapshot(
                    orderedHighCoin.transform,
                    ObjectAwareObjectiveType.Coin);
            }

            return android.exists ? android : goal;
        }

        string objectivePrefix =
            objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup
                ? "MixedRandomWarmup"
                : "MixedWarmup";
        ScoreAttackCoin lowCoin = FindCoinByName($"{objectivePrefix}_LowCoin");
        if (IsLiveCoin(lowCoin))
        {
            return CreateTargetSnapshot(lowCoin.transform, ObjectAwareObjectiveType.Coin);
        }

        ScoreAttackCoin highCoin = FindCoinByName($"{objectivePrefix}_HighCoin");
        if (IsLiveCoin(highCoin))
        {
            if (objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup &&
                MixedRandomHighCoinLockedUntilLanding())
            {
                return default;
            }

            return CreateTargetSnapshot(highCoin.transform, ObjectAwareObjectiveType.Coin);
        }

        return android.exists ? android : goal;
    }

    private TargetSnapshot FindFinalLongChallengeObjective(TargetSnapshot goal)
    {
        if (FinalLongOrderedLandingRequired())
        {
            return default;
        }

        Transform bestTarget = null;
        ObjectAwareObjectiveType bestType = ObjectAwareObjectiveType.None;
        float bestForwardPosition = float.PositiveInfinity;
        float direction = GetForwardDirection();

        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (!IsLiveCoin(coin) ||
                !coin.name.StartsWith("FinalLongChallenge_", StringComparison.Ordinal))
            {
                continue;
            }

            float forwardPosition = coin.transform.position.x * direction;
            if (forwardPosition < bestForwardPosition)
            {
                bestForwardPosition = forwardPosition;
                bestTarget = coin.transform;
                bestType = ObjectAwareObjectiveType.Coin;
            }
        }

        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android == null || !android.IsAlive ||
                !android.gameObject.activeInHierarchy ||
                !android.name.StartsWith("FinalLongChallenge_Android_", StringComparison.Ordinal))
            {
                continue;
            }

            float forwardPosition = android.transform.position.x * direction;
            if (forwardPosition < bestForwardPosition)
            {
                bestForwardPosition = forwardPosition;
                bestTarget = android.transform;
                bestType = ObjectAwareObjectiveType.Android;
            }
        }

        return bestTarget != null
            ? CreateTargetSnapshot(bestTarget, bestType)
            : goal;
    }

    private ScoreAttackCoin FindFirstLiveOrderedCoin(
        bool lowCoin,
        ScoreAttackCoin excludedCoin = null)
    {
        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom ||
            IsFinalLongOrderedCurriculum())
        {
            bool finalLong = IsFinalLongOrderedCurriculum();
            string prefix = lowCoin
                ? finalLong
                    ? "FinalLongChallenge_LowCoin_"
                    : "FinalRandom_LowCoin_"
                : finalLong
                    ? "FinalLongChallenge_HighCoin_"
                    : "FinalRandom_HighCoin_";
            int slotCount = finalLong ? (lowCoin ? 4 : 3) : 2;
            for (int index = 1; index <= slotCount; index++)
            {
                ScoreAttackCoin candidate = FindCoinByName($"{prefix}{index:00}");
                if (candidate != excludedCoin && IsLiveCoin(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        string objectivePrefix =
            objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup
                ? "MixedRandomWarmup"
                : "MixedWarmup";
        ScoreAttackCoin coin = FindCoinByName(
            $"{objectivePrefix}_{(lowCoin ? "LowCoin" : "HighCoin")}");
        return coin != excludedCoin && IsLiveCoin(coin) ? coin : null;
    }

    private bool IsOrderedCurriculumCoin(ScoreAttackCoin coin, bool lowCoin)
    {
        if (coin == null)
        {
            return false;
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom ||
            IsFinalLongOrderedCurriculum())
        {
            bool finalLong = IsFinalLongOrderedCurriculum();
            string prefix = lowCoin
                ? finalLong
                    ? "FinalLongChallenge_LowCoin_"
                    : "FinalRandom_LowCoin_"
                : finalLong
                    ? "FinalLongChallenge_HighCoin_"
                    : "FinalRandom_HighCoin_";
            return coin.name.StartsWith(prefix, StringComparison.Ordinal);
        }

        string expectedName =
            objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup
                ? $"MixedRandomWarmup_{(lowCoin ? "LowCoin" : "HighCoin")}"
                : $"MixedWarmup_{(lowCoin ? "LowCoin" : "HighCoin")}";
        return coin.name == expectedName;
    }

    private bool RejectOrderedHighCoinAttempt(ScoreAttackCoin coin, string reason)
    {
        mixedRandomSameJumpHighCoinAttempt = true;
        AddReward(sameJumpHighCoinPenalty);

        if (debugObjectAwareNextObjective)
        {
            Debug.LogWarning(
                $"[OBJECT AWARE ORDERED HIGH COIN BLOCK] target={coin.name} " +
                $"reason={reason} highCoinLockedUntilLanding=" +
                $"{MixedRandomHighCoinLockedUntilLanding() || FinalLongChallengeLandingRequired()} " +
                $"hasLandedAfterLowCoin={mixedRandomHasLandedAfterLowCoin} " +
                $"sameJumpHighCoinAttempt=true penalty={sameJumpHighCoinPenalty:F2}",
                this);
        }

        if (endEpisodeOnSameJumpHighCoin)
        {
            objectAwareEpisodeEnding = true;
            EndEpisodeWithObjectAwareEvaluationReason(
                EdgeRunnerEpisodeEndReason.ObjectAwareSameJumpHighCoin);
        }

        return false;
    }

    private ScoreAttackCoin FindCoinByName(string expectedName)
    {

        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (coin != null && coin.name == expectedName)
            {
                return coin;
            }
        }

        return null;
    }

    private ScoreAttackAndroid FindAndroidByName(string expectedName)
    {
        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android != null && android.name == expectedName)
            {
                return android;
            }
        }

        return null;
    }

    private static bool IsLiveCoin(ScoreAttackCoin coin)
    {
        return coin != null && coin.IsAvailable && coin.gameObject.activeInHierarchy;
    }

    private static bool IsLiveAndroid(ScoreAttackAndroid android)
    {
        return android != null && android.IsAlive && android.gameObject.activeInHierarchy;
    }

    private bool UsesHighCoinLandingCurriculum()
    {
        return requireGroundedBetweenHighCoins &&
            objectAwarePhase == EdgeRunnerObjectAwarePhase.HighCoinJump;
    }

    private bool HighCoinRequiresLanding()
    {
        return UsesHighCoinLandingCurriculum() &&
            firstHighCoinCollectedThisEpisode &&
            awaitingGroundedAfterHighCoin &&
            !hasLandedAfterPreviousHighCoin;
    }

    private void UpdateHighCoinLandingState()
    {
        if (!HighCoinRequiresLanding() || objectAwareEpisodeEnding)
        {
            return;
        }

        if (IsCurrentlyGroundedForEvaluation())
        {
            hasLandedAfterPreviousHighCoin = true;
            awaitingGroundedAfterHighCoin = false;
            ClearPreviousTrainingObjective();
        }
    }

    private bool MixedRandomHighCoinLockedUntilLanding()
    {
        return UsesGroundedLowHighGateCurriculum() &&
            requireGroundedBetweenLowAndHigh &&
            mixedRandomLowCoinCollectedThisEpisode &&
            mixedRandomAwaitingGroundedAfterLowCoin &&
            !mixedRandomHasLandedAfterLowCoin;
    }

    private void UpdateMixedRandomLowHighLandingState()
    {
        if (!MixedRandomHighCoinLockedUntilLanding() || objectAwareEpisodeEnding)
        {
            return;
        }

        bool observedAfterCollection =
            mixedRandomLowCoinCollectionFrame >= 0 &&
            Time.frameCount > mixedRandomLowCoinCollectionFrame;
        if (observedAfterCollection && IsCurrentlyGroundedForEvaluation())
        {
            mixedRandomHasLandedAfterLowCoin = true;
            mixedRandomAwaitingGroundedAfterLowCoin = false;
            ClearPreviousTrainingObjective();
        }
    }

    private bool FinalLongChallengeLandingRequired()
    {
        return IsFinalLongOrderedCurriculum() &&
            finalLongAwaitingGroundedAfterHighCoin &&
            !finalLongHasLandedAfterHighCoin;
    }

    private void UpdateFinalLongChallengeLandingState()
    {
        if (!FinalLongChallengeLandingRequired() || objectAwareEpisodeEnding)
        {
            return;
        }

        bool observedAfterCollection =
            finalLongHighCoinCollectionFrame >= 0 &&
            Time.frameCount > finalLongHighCoinCollectionFrame;
        if (observedAfterCollection && IsCurrentlyGroundedForEvaluation())
        {
            finalLongHasLandedAfterHighCoin = true;
            finalLongAwaitingGroundedAfterHighCoin = false;
            if (lastCompletedFinalLongObjective.StartsWith(
                "FinalLongChallenge_HighCoin_",
                StringComparison.Ordinal))
            {
                lastCompletedFinalLongObjective =
                    $"landing_required_after_{lastCompletedFinalLongObjective}";
            }
            ClearPreviousTrainingObjective();
        }
    }

    private bool FinalLongLandingRequiredBeforeAndroid()
    {
        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongZone4Warmup)
        {
            if (finalLongZone4WarmupHasLandedAfterGap)
            {
                return false;
            }

            ScoreAttackCoin warmupLowCoin = FindCoinByName(
                "FinalLongChallenge_LowCoin_04");
            return !IsLiveCoin(warmupLowCoin);
        }

        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalLongChallenge)
        {
            return false;
        }

        ScoreAttackAndroid android01 = FindAndroidByName(
            "FinalLongChallenge_Android_01");
        if (IsLiveAndroid(android01) && !finalLongHasLandedBeforeAndroid01)
        {
            ScoreAttackCoin lowCoin03 = FindCoinByName(
                "FinalLongChallenge_LowCoin_03");
            return !IsLiveCoin(lowCoin03);
        }

        ScoreAttackAndroid android02 = FindAndroidByName(
            "FinalLongChallenge_Android_02");
        if (IsLiveAndroid(android02) && !finalLongHasLandedBeforeAndroid02)
        {
            ScoreAttackCoin lowCoin04 = FindCoinByName(
                "FinalLongChallenge_LowCoin_04");
            return !IsLiveCoin(lowCoin04);
        }

        return false;
    }

    private bool FinalLongLandingRequiredBeforeHighCoin01()
    {
        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalLongChallenge ||
            finalLongHasLandedBeforeHighCoin01)
        {
            return false;
        }

        ScoreAttackCoin lowCoin02 = FindCoinByName("FinalLongChallenge_LowCoin_02");
        ScoreAttackCoin highCoin01 = FindCoinByName("FinalLongChallenge_HighCoin_01");
        return !IsLiveCoin(lowCoin02) && IsLiveCoin(highCoin01);
    }

    private void UpdateFinalLongHighCoin01ApproachLandingGate()
    {
        if (!FinalLongLandingRequiredBeforeHighCoin01() ||
            objectAwareEpisodeEnding ||
            transform.position.x < finalLongHighCoin01LandingGateX ||
            !IsCurrentlyGroundedForEvaluation())
        {
            return;
        }

        finalLongHasLandedBeforeHighCoin01 = true;
        ClearPreviousTrainingObjective();
    }

    private void UpdateFinalLongAndroidApproachLandingGate()
    {
        if (!FinalLongLandingRequiredBeforeAndroid() || objectAwareEpisodeEnding)
        {
            return;
        }

        float gateX;
        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongZone4Warmup)
        {
            gateX = finalLongZone4WarmupLandingGateX;
        }
        else if (IsLiveAndroid(FindAndroidByName("FinalLongChallenge_Android_01")))
        {
            gateX = finalLongAndroid01LandingGateX;
        }
        else
        {
            gateX = finalLongAndroid02LandingGateX;
        }

        if (transform.position.x < gateX || !IsCurrentlyGroundedForEvaluation())
        {
            return;
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongZone4Warmup)
        {
            finalLongZone4WarmupHasLandedAfterGap = true;
        }
        else if (IsLiveAndroid(FindAndroidByName("FinalLongChallenge_Android_01")))
        {
            finalLongHasLandedBeforeAndroid01 = true;
        }
        else
        {
            finalLongHasLandedBeforeAndroid02 = true;
        }

        ClearPreviousTrainingObjective();
    }

    private bool FinalLongLandingRequiredAfterAndroid()
    {
        return IsFinalLongOrderedCurriculum() &&
            finalLongAwaitingGroundedAfterAndroid &&
            !finalLongHasLandedAfterAndroid;
    }

    private void UpdateFinalLongAndroidLandingState()
    {
        if (!FinalLongLandingRequiredAfterAndroid() || objectAwareEpisodeEnding)
        {
            return;
        }

        bool observedAfterStomp =
            finalLongAndroidStompFrame >= 0 &&
            Time.frameCount > finalLongAndroidStompFrame;
        if (observedAfterStomp && IsCurrentlyGroundedForEvaluation())
        {
            finalLongHasLandedAfterAndroid = true;
            finalLongAwaitingGroundedAfterAndroid = false;
            lastCompletedFinalLongObjective =
                $"landing_required_after_{finalLongLastStompedAndroidName}";
            ClearPreviousTrainingObjective();
        }
    }

    private bool FinalLongOrderedLandingRequired()
    {
        return FinalLongChallengeLandingRequired() ||
            FinalLongLandingRequiredBeforeHighCoin01() ||
            FinalLongLandingRequiredBeforeAndroid() ||
            FinalLongLandingRequiredAfterAndroid();
    }

    private bool IsLowCoin(Vector2 delta)
    {
        return (objectAwarePhase == EdgeRunnerObjectAwarePhase.LowCoinRun &&
                enforceLowCoinRunGroundCollection) ||
            delta.y <= lowCoinHeightThreshold;
    }

    private bool ShouldBlockLowCoinJump(ObjectAwareContext context)
    {
        if (!enableContextualJumpMask ||
            (objectAwarePhase != EdgeRunnerObjectAwarePhase.LowCoinRun &&
             !IsMixedWarmupCurriculum()))
        {
            return false;
        }

        // This dedicated curriculum contains no gaps or Androids, so jump is
        // invalid for the entire episode, including the very first decision
        // before the object cache or grounded state has settled.
        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.LowCoinRun &&
            enforceLowCoinRunGroundCollection)
        {
            return true;
        }

        if (context.highCoinLockedUntilLanding && !context.gap.gapAhead)
        {
            return true;
        }

        if (
            context.nextObjective.type != ObjectAwareObjectiveType.Coin ||
            !context.coinRunCollectable ||
            context.coinNeedsJump ||
            context.androidStompContext)
        {
            return false;
        }

        return !context.gap.gapAhead;
    }

    private bool IsMixedWarmupCurriculum()
    {
        return objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedWarmup ||
            objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup ||
            objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom ||
            IsFinalLongOrderedCurriculum();
    }

    private bool UsesGroundedLowHighGateCurriculum()
    {
        return objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup ||
            objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom ||
            IsFinalLongOrderedCurriculum();
    }

    private bool IsFinalLongOrderedCurriculum()
    {
        return objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongChallenge ||
            objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalLongZone4Warmup;
    }

    private static ActionBuffers WithoutJumpAction(ActionBuffers actions)
    {
        int[] discreteActions = new int[actions.DiscreteActions.Length];
        for (int i = 0; i < discreteActions.Length; i++)
        {
            discreteActions[i] = actions.DiscreteActions[i];
        }

        if (discreteActions.Length > JumpBranchIndex)
        {
            discreteActions[JumpBranchIndex] = NoJumpAction;
        }

        return new ActionBuffers(
            actions.ContinuousActions,
            new ActionSegment<int>(discreteActions));
    }

    private TargetSnapshot FindNearestAndroid()
    {
        TargetSnapshot best = default;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android == null || !android.IsAlive || !android.gameObject.activeInHierarchy)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, android.transform.position);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = CreateTargetSnapshot(android.transform, ObjectAwareObjectiveType.Android);
        }

        return best;
    }

    private TargetSnapshot CreateTargetSnapshot(Transform target, ObjectAwareObjectiveType type)
    {
        if (target == null)
        {
            return default;
        }

        Vector2 delta = target.position - transform.position;
        return new TargetSnapshot
        {
            exists = true,
            target = target,
            type = type,
            delta = delta,
            distance = delta.magnitude,
            ahead = delta.x * GetForwardDirection() >= 0f
        };
    }

    private GapSnapshot EvaluateGapContext()
    {
        float direction = GetForwardDirection();
        Vector3 nearProbe = GetProbePosition(direction, nearGapProbeDistance);
        Vector3 midProbe = GetProbePosition(direction, midGapProbeDistance);
        Vector3 farProbe = GetProbePosition(direction, farGapProbeDistance);
        Vector3 landingProbe = GetProbePosition(direction, landingProbeDistance);
        bool nearMissing = !HasGroundBelow(nearProbe);
        bool midMissing = !HasGroundBelow(midProbe);
        bool farMissing = !HasGroundBelow(farProbe);
        bool landingAvailable = HasGroundBelow(landingProbe);

        return new GapSnapshot
        {
            nearMissing = nearMissing,
            midMissing = midMissing,
            farMissing = farMissing,
            landingAvailable = landingAvailable,
            gapAhead = nearMissing || midMissing,
            nearProbe = nearProbe,
            midProbe = midProbe,
            farProbe = farProbe,
            landingProbe = landingProbe
        };
    }

    private Vector3 GetProbePosition(float direction, float distance)
    {
        Vector3 origin = objectAwareRigidbody != null
            ? objectAwareRigidbody.position
            : transform.position;
        return origin + new Vector3(direction * Mathf.Max(0.1f, distance), 0.1f, 0f);
    }

    private bool HasGroundBelow(Vector3 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            Vector2.down,
            Mathf.Max(0.2f, gapProbeDepth),
            GetBaseGroundLayer());
        return hit.collider != null && !hit.collider.isTrigger;
    }

    private void ResolveObjectAwareEvaluationManager()
    {
        if (objectAwareEvaluationManager == null)
        {
            objectAwareEvaluationManager = FindFirstObjectByType<EdgeRunnerEvaluationManager>();
        }
    }

    private void ResolveObjectAwareReferences()
    {
        if (objectAwareScoreAttackManager == null)
        {
            objectAwareScoreAttackManager = FindAnyObjectByType<ScoreAttackManager>();
        }

        if (objectAwareRigidbody == null)
        {
            objectAwareRigidbody = GetComponent<Rigidbody2D>();
        }

        if (antiLedgeCollider == null)
        {
            antiLedgeCollider = GetComponent<Collider2D>();
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.MixedRandomWarmup &&
            mixedRandomWarmupRandomizer == null)
        {
            mixedRandomWarmupRandomizer =
                FindAnyObjectByType<ScoreMaxOAMixedRandomWarmupRandomizer>();
        }

        if (objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom &&
            finalRandomizer == null)
        {
            finalRandomizer = FindAnyObjectByType<ScoreMaxOAFinalRandomizer>();
        }
    }

    private void RefreshObjectCache(bool force)
    {
        if (!force && Application.isPlaying && Time.time < nextObjectScanTime)
        {
            return;
        }

        cachedCoins = FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);
        cachedAndroids = FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include);
        nextObjectScanTime = Time.time + 0.25f;
    }

    private int CountLiveCoins()
    {
        int count = 0;
        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (coin != null && coin.IsAvailable && coin.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private int CountLiveAndroids()
    {
        int count = 0;
        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android != null && android.IsAlive && android.gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private float GetForwardDirection()
    {
        if (objectAwareGoal == null)
        {
            return 1f;
        }

        float dx = objectAwareGoal.position.x - transform.position.x;
        return Mathf.Abs(dx) > 0.05f ? Mathf.Sign(dx) : 1f;
    }

    private LayerMask GetBaseGroundLayer()
    {
        if (BaseGroundLayerField?.GetValue(this) is LayerMask layerMask && layerMask.value != 0)
        {
            return layerMask;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        return groundLayer >= 0 ? 1 << groundLayer : Physics2D.DefaultRaycastLayers;
    }

    private float GetBaseCoyoteTimer()
    {
        return BaseCoyoteTimerField?.GetValue(this) is float value ? value : 0f;
    }

    private void ValidateObservationCount(
        int startCount,
        int afterBaseCount,
        int finalCount,
        int globalCount,
        int nextCount,
        int lowCoinCount,
        int highCoinCount,
        int androidCount,
        int jumpCount)
    {
        int extraCount =
            globalCount + nextCount + lowCoinCount + highCoinCount + androidCount + jumpCount;
        int baseCount = startCount >= 0 && afterBaseCount >= 0
            ? afterBaseCount - startCount
            : EdgeRunnerAgentV5.DefaultExpectedObservationSize;
        int totalCount = startCount >= 0 && finalCount >= 0
            ? finalCount - startCount
            : baseCount + extraCount;
        bool mismatch =
            baseCount != EdgeRunnerAgentV5.DefaultExpectedObservationSize ||
            extraCount != ObjectAwareExtraObservationCount ||
            totalCount != DefaultExpectedObservationSize;

        if (debugObjectAwareObservationCount && !loggedObservationCountThisEpisode)
        {
            loggedObservationCountThisEpisode = true;
            Debug.Log(
                $"[OBJECT AWARE OBS] count={totalCount} expected={DefaultExpectedObservationSize} " +
                $"blocks=base:{baseCount},global:{globalCount},next:{nextCount}," +
                $"lowCoin:{lowCoinCount},highCoin:{highCoinCount},android:{androidCount}," +
                $"jumpContext:{jumpCount}",
                this);
        }

        if (debugObjectAwareObservationCount && mismatch && !warnedObservationMismatchThisEpisode)
        {
            warnedObservationMismatchThisEpisode = true;
            Debug.LogWarning(
                $"[OBJECT AWARE OBS] mismatch count={totalCount} " +
                $"expected={DefaultExpectedObservationSize} base={baseCount} extra={extraCount}",
                this);
        }
    }

    private void LogObjectAwareContext(ObjectAwareContext context)
    {
        if (Time.time < nextDebugLogTime ||
            (!debugObjectAwareNextObjective && !debugObjectAwareJumpContext))
        {
            return;
        }

        nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugObjectAwareLogInterval);

        if (debugObjectAwareNextObjective)
        {
            TargetSnapshot next = context.nextObjective;
            string currentTarget = next.target != null
                ? next.target.name
                : context.highCoinRequiresLanding || context.highCoinLockedUntilLanding
                    ? "landing_required"
                    : "none";
            string coinClass = next.type == ObjectAwareObjectiveType.Coin
                ? context.coinNeedsJump ? "high" : "low"
                : "none";
            string sequenceStage = objectAwarePhase == EdgeRunnerObjectAwarePhase.FinalRandom
                ? GetFinalRandomSequenceStage(context)
                : IsFinalLongOrderedCurriculum()
                    ? FinalLongChallengeLandingRequired()
                        ? "landing_required"
                        : currentTarget
                    : currentTarget;
            Debug.Log(
                $"[OBJECT AWARE NEXT] type={FormatObjectiveType(next.type)} " +
                $"currentTarget={currentTarget} " +
                $"sequenceStage={sequenceStage} " +
                $"exists={next.exists} dx={next.delta.x:F2} dy={next.delta.y:F2} " +
                $"dist={next.distance:F2} ahead={next.ahead} " +
                $"coinClass={coinClass} coinNeedsJump={context.coinNeedsJump} " +
                $"coinRunCollectable={context.coinRunCollectable} " +
                $"coinJumpWindow={context.coinJumpWindow} " +
                $"highCoinRequiresLanding={context.highCoinRequiresLanding} " +
                $"hasLandedAfterPreviousHighCoin={context.hasLandedAfterPreviousHighCoin} " +
                $"sameJumpCoinAttempt={context.sameJumpCoinAttempt} " +
                $"lowCoinRequiresGrounded={context.lowCoinRequiresGrounded} " +
                $"hasLandedAfterLowCoin={context.hasLandedAfterLowCoin} " +
                $"highCoinLockedUntilLanding={context.highCoinLockedUntilLanding} " +
                $"sameJumpHighCoinAttempt={context.sameJumpHighCoinAttempt} " +
                $"airborneLowCoinAttempt={context.airborneLowCoinAttempt} " +
                $"jumpMasked={ShouldBlockLowCoinJump(context)} " +
                $"coinsRemaining={context.coinsRemaining} " +
                $"enemiesRemaining={context.enemiesRemaining} " +
                $"enemyAhead={context.enemyAhead} " +
                $"enemyStompWindow={context.enemyStompWindow} " +
                $"enemySideDanger={context.enemySideDanger} " +
                $"enemyAvoidContext={context.enemyAvoidContext}",
                this);
        }

        if (debugObjectAwareJumpContext)
        {
            Debug.Log(
                $"[OBJECT AWARE JUMP] grounded={context.grounded} " +
                $"coyote={context.coyoteTimer:F3} gapAhead={context.gap.gapAhead} " +
                $"highCoin={context.highCoinJumpContext} android={context.androidStompContext} " +
                $"enemyStompWindow={context.enemyStompWindow} " +
                $"enemyAvoidContext={context.enemyAvoidContext} " +
                $"jumpMasked={ShouldBlockLowCoinJump(context)} " +
                $"valid={context.jumpContextValid} shouldNotJump={context.shouldNotJumpNow}",
                this);
        }
    }

    private void LogFinalLongChallengeValidation()
    {
        LayerMask groundMask = GetBaseGroundLayer();
        const float flatProbeOffset = 2f;
        List<string> coinStates = new List<string>();
        List<string> androidStates = new List<string>();

        for (int i = 0; i < cachedCoins.Length; i++)
        {
            ScoreAttackCoin coin = cachedCoins[i];
            if (coin == null ||
                !coin.name.StartsWith("FinalLongChallenge_", StringComparison.Ordinal))
            {
                continue;
            }

            Vector2 delta = coin.transform.position - transform.position;
            bool low = IsLowCoin(delta);
            bool safeFlat = !low || HasConsistentGroundUnder(
                coin.transform.position,
                flatProbeOffset,
                groundMask);
            coinStates.Add(
                $"{coin.name}:class={(low ? "low" : "high")}," +
                $"x={coin.transform.position.x:F1},dy={delta.y:F2},safeFlat={safeFlat}");
        }

        for (int i = 0; i < cachedAndroids.Length; i++)
        {
            ScoreAttackAndroid android = cachedAndroids[i];
            if (android != null &&
                android.name.StartsWith("FinalLongChallenge_Android_", StringComparison.Ordinal))
            {
                androidStates.Add(
                    $"{android.name}:x={android.transform.position.x:F1},alive={android.IsAlive}");
            }
        }

        bool goalLocked = objectAwareScoreAttackManager != null &&
            !objectAwareScoreAttackManager.ObjectivesComplete;
        Debug.Log(
            $"[OBJECT AWARE FINAL LONG VALIDATION] coins=[{string.Join("; ", coinStates)}] " +
            $"requiredAndroids=[{string.Join("; ", androidStates)}] " +
            $"coinsRemaining={CountLiveCoins()} enemiesRemaining={CountLiveAndroids()} " +
            $"goalLock={goalLocked} antiLedgeStuck={enableAntiLedgeStuckFailSafe}",
            this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Application.logMessageReceived += HandleFinalLongBaseEpisodeLog;
    }

    protected override void OnDisable()
    {
        Application.logMessageReceived -= HandleFinalLongBaseEpisodeLog;
        base.OnDisable();
    }

    private void ConfigureFinalLongFailureDebug()
    {
        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalLongChallenge ||
            !debugFinalLongFailureReason ||
            BaseDebugEpisodeResetReasonField == null)
        {
            return;
        }

        BaseDebugEpisodeResetReasonField.SetValue(this, true);
    }

    private void HandleFinalLongBaseEpisodeLog(
        string condition,
        string stackTrace,
        LogType type)
    {
        const string prefix = "[SCOREMAX RESET] reason=";
        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalLongChallenge ||
            !debugFinalLongFailureReason ||
            string.IsNullOrEmpty(condition) ||
            !condition.StartsWith(prefix, StringComparison.Ordinal))
        {
            return;
        }

        int reasonEnd = condition.IndexOf(' ', prefix.Length);
        string reason = reasonEnd > prefix.Length
            ? condition.Substring(prefix.Length, reasonEnd - prefix.Length)
            : condition.Substring(prefix.Length);
        LogFinalLongFailureReason(reason);
    }

    private void LogFinalLongFailureReason(string reason)
    {
        if (objectAwarePhase != EdgeRunnerObjectAwarePhase.FinalLongChallenge ||
            !debugFinalLongFailureReason)
        {
            return;
        }

        ResolveObjectAwareReferences();
        RefreshObjectCache(false);
        UpdateFinalLongChallengeLandingState();

        TargetSnapshot current = FindFinalLongChallengeObjective(
            CreateTargetSnapshot(objectAwareGoal, ObjectAwareObjectiveType.Goal));
        string currentObjective = FinalLongChallengeLandingRequired()
            ? "landing_required"
            : current.target != null
                ? current.target.name
                : "none";
        float distanceToNextObjective = current.exists ? current.distance : -1f;
        Vector2 velocity = objectAwareRigidbody != null
            ? objectAwareRigidbody.linearVelocity
            : Vector2.zero;
        bool grounded = IsCurrentlyGroundedForEvaluation();
        int coinsCollected = objectAwareScoreAttackManager != null
            ? objectAwareScoreAttackManager.CoinsCollected
            : -1;
        int coinsTotal = objectAwareScoreAttackManager != null
            ? objectAwareScoreAttackManager.CoinsCollected +
                objectAwareScoreAttackManager.CoinsRemaining
            : -1;
        int enemiesKilled = objectAwareScoreAttackManager != null
            ? objectAwareScoreAttackManager.EnemiesKilled
            : -1;
        int enemiesTotal = objectAwareScoreAttackManager != null
            ? objectAwareScoreAttackManager.EnemiesKilled +
                objectAwareScoreAttackManager.EnemiesRemaining
            : -1;

        Debug.LogWarning(
            $"[FINAL LONG FAILURE] step={StepCount} " +
            $"failReason={FormatFinalLongFailureReason(reason)} " +
            $"currentObjective={currentObjective} " +
            $"lastCompletedObjective={lastCompletedFinalLongObjective} " +
            $"playerPosition={transform.position} velocity={velocity} " +
            $"grounded={grounded} zoneIndex={GetFinalLongZoneIndex(currentObjective)} " +
            $"coins={coinsCollected}/{coinsTotal} " +
            $"enemies={enemiesKilled}/{enemiesTotal} " +
            $"distanceToNextObjective={distanceToNextObjective:F2}",
            this);
    }

    private string FormatFinalLongFailureReason(string reason)
    {
        if (reason == EdgeRunnerEpisodeEndReason.ObjectAwareAirborneLowCoin.ToString() ||
            reason == EdgeRunnerEpisodeEndReason.ObjectAwareSameJumpHighCoin.ToString())
        {
            return $"coin_rejected:{lastCoinCollectionReason}";
        }

        if (reason == EdgeRunnerEpisodeEndReason.ObjectAwareMissedCoin.ToString() &&
            lastCoinCollectionReason != "not_attempted" &&
            !lastCoinCollectionReason.StartsWith("accepted_", StringComparison.Ordinal))
        {
            return $"coin_rejected_then_missed:{lastCoinCollectionReason}";
        }

        return reason;
    }

    private int GetFinalLongZoneIndex(string currentObjective)
    {
        if (!string.IsNullOrEmpty(currentObjective))
        {
            if (currentObjective.EndsWith("LowCoin_01", StringComparison.Ordinal) ||
                currentObjective.EndsWith("LowCoin_02", StringComparison.Ordinal) ||
                currentObjective.EndsWith("HighCoin_01", StringComparison.Ordinal))
            {
                return 1;
            }

            if (currentObjective.EndsWith("LowCoin_03", StringComparison.Ordinal) ||
                currentObjective.EndsWith("Android_01", StringComparison.Ordinal))
            {
                return 2;
            }

            if (currentObjective.EndsWith("HighCoin_02", StringComparison.Ordinal) ||
                currentObjective.EndsWith("LowCoin_04", StringComparison.Ordinal))
            {
                return 3;
            }

            if (currentObjective.EndsWith("Android_02", StringComparison.Ordinal) ||
                currentObjective.EndsWith("HighCoin_03", StringComparison.Ordinal) ||
                currentObjective.Contains("Goal"))
            {
                return 4;
            }
        }

        if (FinalLongChallengeLandingRequired())
        {
            if (lastCompletedFinalLongObjective.Contains("HighCoin_01"))
            {
                return 1;
            }

            if (lastCompletedFinalLongObjective.Contains("HighCoin_02"))
            {
                return 3;
            }

            return 4;
        }

        float x = transform.position.x;
        return x < 35f ? 1 : x < 79f ? 2 : x < 112.3f ? 3 : 4;
    }

    private static bool HasConsistentGroundUnder(
        Vector3 position,
        float horizontalOffset,
        LayerMask groundMask)
    {
        RaycastHit2D left = Physics2D.Raycast(
            new Vector2(position.x - horizontalOffset, position.y + 1f),
            Vector2.down,
            8f,
            groundMask.value);
        RaycastHit2D center = Physics2D.Raycast(
            new Vector2(position.x, position.y + 1f),
            Vector2.down,
            8f,
            groundMask.value);
        RaycastHit2D right = Physics2D.Raycast(
            new Vector2(position.x + horizontalOffset, position.y + 1f),
            Vector2.down,
            8f,
            groundMask.value);

        return left.collider != null && center.collider != null && right.collider != null &&
            Mathf.Abs(left.point.y - center.point.y) <= 0.1f &&
            Mathf.Abs(right.point.y - center.point.y) <= 0.1f;
    }

    private string GetFinalRandomSequenceStage(ObjectAwareContext context)
    {
        if (context.highCoinLockedUntilLanding)
        {
            return "landing_required";
        }

        if (FindFirstLiveOrderedCoin(true) != null)
        {
            return "LowCoin";
        }

        if (FindFirstLiveOrderedCoin(false) != null)
        {
            return "HighCoin";
        }

        return context.android.exists ? "Enemy" : "Goal";
    }

    private static string FormatObjectiveType(ObjectAwareObjectiveType type)
    {
        return type switch
        {
            ObjectAwareObjectiveType.Coin => "coin",
            ObjectAwareObjectiveType.Android => "enemy",
            ObjectAwareObjectiveType.Goal => "goal",
            _ => "none"
        };
    }

    private static int GetObservationCount(VectorSensor sensor)
    {
        if (sensor == null || VectorSensorObservationsField == null)
        {
            return -1;
        }

        return VectorSensorObservationsField.GetValue(sensor) is ICollection observations
            ? observations.Count
            : -1;
    }

    private static void Add(VectorSensor sensor, float value, ref int count)
    {
        sensor.AddObservation(value);
        count++;
    }

    private static void Add(VectorSensor sensor, bool value, ref int count)
    {
        sensor.AddObservation(value ? 1f : 0f);
        count++;
    }

    private static float NormalizeSigned(float value, float maxAbsValue)
    {
        return Mathf.Clamp(value / Mathf.Max(0.0001f, maxAbsValue), -1f, 1f);
    }

    private static float NormalizePositive(float value, float maxValue)
    {
        return Mathf.Clamp01(value / Mathf.Max(0.0001f, maxValue));
    }

    private void OnDrawGizmos()
    {
        if (!debugObjectAwareGizmos)
        {
            return;
        }

        ResolveObjectAwareReferences();
        RefreshObjectCache(true);
        ObjectAwareContext context = BuildContext();

        if (context.gap.gapAhead)
        {
            Gizmos.color = Color.red;
            DrawProbe(context.gap.nearProbe, context.gap.nearMissing);
            DrawProbe(context.gap.midProbe, context.gap.midMissing);
            DrawProbe(context.gap.farProbe, context.gap.farMissing);
        }

        if (context.lowCoin.exists)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, context.lowCoin.target.position);
            Gizmos.DrawWireSphere(context.lowCoin.target.position, 0.35f);
        }

        if (context.highCoin.exists)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, context.highCoin.target.position);
            Gizmos.DrawWireSphere(context.highCoin.target.position, 0.4f);

            float direction = GetForwardDirection();
            Vector3 windowCenter = context.highCoin.target.position -
                Vector3.right * direction * highCoinJumpWindowX * 0.5f;
            Gizmos.DrawWireCube(
                windowCenter,
                new Vector3(highCoinJumpWindowX, 1.0f, 0f));
        }

        if (context.highCoinRequiresLanding || context.highCoinLockedUntilLanding)
        {
            ScoreAttackCoin blockedCoin = context.highCoinLockedUntilLanding
                ? FindFirstLiveOrderedCoin(false)
                : FindHighCoinSequenceCoin(false);
            if (IsLiveCoin(blockedCoin))
            {
                Vector3 blockedPosition = blockedCoin.transform.position;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(blockedPosition, 0.4f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(
                    blockedPosition + new Vector3(-0.35f, -0.35f, 0f),
                    blockedPosition + new Vector3(0.35f, 0.35f, 0f));
                Gizmos.DrawLine(
                    blockedPosition + new Vector3(-0.35f, 0.35f, 0f),
                    blockedPosition + new Vector3(0.35f, -0.35f, 0f));
            }
        }

        if (context.android.exists)
        {
            Gizmos.color = new Color(1f, 0.45f, 0.05f, 1f);
            Gizmos.DrawLine(transform.position, context.android.target.position);
            Gizmos.DrawWireCube(context.android.target.position, new Vector3(1.8f, 1.6f, 0f));
        }

        if (context.nextObjective.exists)
        {
            Gizmos.color = context.nextObjective.type == ObjectAwareObjectiveType.Goal
                ? Color.cyan
                : new Color(0.65f, 0.2f, 1f, 1f);
            Gizmos.DrawLine(transform.position, context.nextObjective.target.position);
        }

        if (context.gap.landingAvailable)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(context.gap.landingProbe, 0.2f);
        }
    }

    private void DrawProbe(Vector3 probe, bool missing)
    {
        if (!missing)
        {
            return;
        }

        Gizmos.DrawLine(probe, probe + Vector3.down * gapProbeDepth);
        Gizmos.DrawWireSphere(probe, 0.15f);
    }

    private void OnValidate()
    {
        maxObjectiveDistance = Mathf.Max(1f, maxObjectiveDistance);
        maxObjectiveHeight = Mathf.Max(1f, maxObjectiveHeight);
        maxCoinCount = Mathf.Max(1f, maxCoinCount);
        maxEnemyCount = Mathf.Max(1f, maxEnemyCount);
        lowCoinHeightThreshold = Mathf.Max(0f, lowCoinHeightThreshold);
        lowCoinRunWindowX = Mathf.Max(0f, lowCoinRunWindowX);
        highCoinJumpWindowX = Mathf.Max(0f, highCoinJumpWindowX);
        androidContextWindowX = Mathf.Max(0f, androidContextWindowX);
        androidVerticalTolerance = Mathf.Max(0f, androidVerticalTolerance);
        highCoinEarlyJumpDistance = Mathf.Max(0f, highCoinEarlyJumpDistance);
        highCoinJumpWindowDistanceMin = Mathf.Max(0f, highCoinJumpWindowDistanceMin);
        highCoinJumpWindowDistanceMax = Mathf.Max(
            highCoinJumpWindowDistanceMin,
            highCoinJumpWindowDistanceMax);
        highCoinEarlyJumpDistance = Mathf.Max(
            highCoinJumpWindowDistanceMax,
            highCoinEarlyJumpDistance);
        highCoinGroundedApproachReward = Mathf.Max(0f, highCoinGroundedApproachReward);
        missedCoinForwardMargin = Mathf.Max(0f, missedCoinForwardMargin);
        enemyStompWindowHorizontalRange = Mathf.Max(0f, enemyStompWindowHorizontalRange);
        missedEnemyForwardMargin = Mathf.Max(0f, missedEnemyForwardMargin);
        nearGapProbeDistance = Mathf.Max(0.1f, nearGapProbeDistance);
        midGapProbeDistance = Mathf.Max(nearGapProbeDistance, midGapProbeDistance);
        farGapProbeDistance = Mathf.Max(midGapProbeDistance, farGapProbeDistance);
        landingProbeDistance = Mathf.Max(farGapProbeDistance, landingProbeDistance);
        gapProbeDepth = Mathf.Max(0.2f, gapProbeDepth);
        finalLongZone4WarmupLandingGateX = Mathf.Max(0f, finalLongZone4WarmupLandingGateX);
        finalLongHighCoin01LandingGateX = Mathf.Max(0f, finalLongHighCoin01LandingGateX);
        finalLongAndroid01LandingGateX = Mathf.Max(0f, finalLongAndroid01LandingGateX);
        finalLongAndroid02LandingGateX = Mathf.Max(0f, finalLongAndroid02LandingGateX);
        debugObjectAwareLogInterval = Mathf.Max(0.05f, debugObjectAwareLogInterval);
    }
}
