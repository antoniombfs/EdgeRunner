using System.Collections.Generic;
using System.Reflection;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EdgeRunnerAgentV5SpeedRunObstacleAware : EdgeRunnerAgentV5
{
    public const string ExpectedBehaviorName = "EdgeRunnerV5SpeedRunObstacleAware";
    public new const int DefaultExpectedObservationSize =
        EdgeRunnerAgentV5.DefaultExpectedObservationSize + AndroidObservationCount;
    public const int AndroidObservationCount = 12;
    public const int MovementActionBranchSize = 3;
    public const int JumpActionBranchSize = 2;
    public const int SprintActionBranchSize = 2;
    private const int JumpBranchIndex = 1;
    private const int JumpActionIndex = 1;

    private static readonly FieldInfo BaseMaskUselessJumpsField =
        typeof(EdgeRunnerAgentV5).GetField(
            "maskUselessJumps",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseAllowJumpField =
        typeof(EdgeRunnerAgentV5).GetField(
            "allowJump",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseJumpConsumedUntilLandingField =
        typeof(EdgeRunnerAgentV5).GetField(
            "jumpConsumedUntilLanding",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseWaitingForJumpReleaseField =
        typeof(EdgeRunnerAgentV5).GetField(
            "waitingForJumpRelease",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseFlatGroundJumpPenaltyField =
        typeof(EdgeRunnerAgentV5).GetField(
            "flatGroundJumpPenalty",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseUselessJumpPenaltyField =
        typeof(EdgeRunnerAgentV5).GetField(
            "uselessJumpPenalty",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseGroundLayerField =
        typeof(EdgeRunnerAgentV5).GetField(
            "groundLayer",
            BindingFlags.Instance | BindingFlags.NonPublic);

    [Header("SpeedRun ObstacleAware References")]
    [Tooltip("Assign the same Goal used by the inherited EdgeRunnerAgentV5 component data.")]
    [SerializeField] private Transform obstacleAwareGoal;

    [Header("Android Observation Normalization")]
    [SerializeField] private float maxAndroidHorizontalDistance = 20f;
    [SerializeField] private float maxAndroidVerticalDistance = 6f;
    [SerializeField] private float maxAndroidTotalDistance = 22f;
    [SerializeField] private float maxAndroidSpeed = 6f;
    [SerializeField] private float maxRelativeClosingSpeed = 20f;
    [SerializeField] private float horizontalDangerWindow = 3f;
    [SerializeField] private float verticalOverlapTolerance = 0.05f;
    [SerializeField] private float passedAndroidMargin = 0.9f;

    [Header("Contextual Android Jump Discipline")]
    [SerializeField] private bool enforceContextualJumpDiscipline = true;
    [SerializeField] private float androidJumpWindowMin = 0.6f;
    [SerializeField] private float androidJumpWindowMax = 4f;
    [SerializeField] private float androidJumpVerticalTolerance = 1.25f;

    [Header("Contextual Elevated Landing Jump")]
    [SerializeField] private bool allowElevatedLandingJump = true;
    [SerializeField] private float elevatedGapProbeNear = 0.5f;
    [SerializeField] private float elevatedGapProbeFar = 0.95f;
    [SerializeField] private float elevatedGapProbeDepth = 1.15f;
    [SerializeField] private float elevatedLandingWindowMin = 1.1f;
    [SerializeField] private float elevatedLandingWindowMax = 4.8f;
    [SerializeField] private float elevatedLandingScanStep = 0.2f;
    [SerializeField] private float elevatedLandingMinHeight = 0.25f;
    [SerializeField] private float elevatedLandingMaxHeight = 2.75f;
    [SerializeField] private float elevatedLandingProbeHeadroom = 0.5f;

    [Header("Obstacle Rewards")]
    [SerializeField] private float obstacleCollisionPenalty = -6f;
    [SerializeField] private float passedAndroidReward = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugObstacleAwareEvents = false;

    private readonly HashSet<EdgeRunnerEnemyMarker> rewardedPassedAndroids =
        new HashSet<EdgeRunnerEnemyMarker>();
    private EdgeRunnerEnemyMarker[] obstacleMarkers = System.Array.Empty<EdgeRunnerEnemyMarker>();
    private Collider2D[] playerColliders = System.Array.Empty<Collider2D>();
    private float lastGoalDirection = 1f;
    private bool obstacleCollisionEndedEpisode;
    private bool warnedMissingBaseJumpMaskField;

    public override void Initialize()
    {
        ValidateObstacleSettings();
        base.Initialize();
        EnsureBaseJumpDisciplineEnabled();
        playerColliders = GetComponentsInChildren<Collider2D>(true);
        ResolveGoalReference();
        RefreshObstacleCache();
        ValidateBehaviorName();
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        obstacleCollisionEndedEpisode = false;
        rewardedPassedAndroids.Clear();
        ResolveGoalReference();
        RefreshObstacleCache();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);

        float goalDirection = GetGoalDirection();
        EdgeRunnerEnemyMarker marker = SelectRelevantAndroid(goalDirection);

        if (marker == null)
        {
            AddEmptyAndroidObservations(sensor);
            return;
        }

        Vector2 playerPosition = transform.position;
        Vector2 androidPosition = marker.GetObservationPosition();
        Vector2 delta = androidPosition - playerPosition;
        Vector2 androidVelocity = marker.CurrentVelocity;
        Vector2 playerVelocity = GetCurrentVelocityForEvaluation();
        float forwardDistance = delta.x * goalDirection;
        float relativeVelocityX = androidVelocity.x - playerVelocity.x;
        float closingSpeed = -Mathf.Sign(delta.x) * relativeVelocityX;
        bool ahead = forwardDistance >= 0f;
        bool movingTowardAgent =
            Mathf.Abs(androidVelocity.x) > 0.01f &&
            Mathf.Abs(delta.x) > 0.01f &&
            Mathf.Sign(androidVelocity.x) == -Mathf.Sign(delta.x);
        bool inHorizontalDangerWindow = Mathf.Abs(forwardDistance) <= horizontalDangerWindow;
        bool verticalOverlap = HasDangerousVerticalOverlap(marker);
        bool passed = forwardDistance < -passedAndroidMargin;

        // 12 Android observations. Keep this order synchronized with the documented contract.
        sensor.AddObservation(1f);
        sensor.AddObservation(ahead ? 1f : 0f);
        sensor.AddObservation(NormalizeSigned(delta.x, maxAndroidHorizontalDistance));
        sensor.AddObservation(NormalizeSigned(delta.y, maxAndroidVerticalDistance));
        sensor.AddObservation(NormalizePositive(delta.magnitude, maxAndroidTotalDistance));
        sensor.AddObservation(NormalizeSigned(forwardDistance, maxAndroidHorizontalDistance));
        sensor.AddObservation(NormalizeSigned(androidVelocity.x, maxAndroidSpeed));
        sensor.AddObservation(NormalizeSigned(closingSpeed, maxRelativeClosingSpeed));
        sensor.AddObservation(movingTowardAgent ? 1f : 0f);
        sensor.AddObservation(inHorizontalDangerWindow ? 1f : 0f);
        sensor.AddObservation(verticalOverlap ? 1f : 0f);
        sensor.AddObservation(passed ? 1f : 0f);
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        EnsureBaseJumpDisciplineEnabled();
        base.WriteDiscreteActionMask(actionMask);

        bool androidJumpUseful = TryGetAndroidJumpContext(out _);
        bool elevatedLandingJumpUseful = TryGetElevatedLandingJumpContext(out _);
        if (androidJumpUseful || elevatedLandingJumpUseful)
        {
            // The ML-Agents mask stores the latest enabled state, so this safely
            // re-enables Jump after the base rejected a contextually useful jump.
            actionMask.SetActionEnabled(JumpBranchIndex, JumpActionIndex, true);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Skip this subclass's own contextual-jump pre-processing entirely while Manual is
        // active — base.OnActionReceived() itself already blocks all movement/jump application
        // in Manual (see EdgeRunnerAgentV5.OnActionReceived), so there is nothing useful left
        // for this override to compute, and it avoids touching any contextual-jump state that
        // a free-form human player would not keep consistent with what this logic expects.
        if (FinalDemoController.IsManualControlActive)
        {
            base.OnActionReceived(actions);
            return;
        }

        bool jumpSelected =
            actions.DiscreteActions.Length > JumpBranchIndex &&
            actions.DiscreteActions[JumpBranchIndex] == JumpActionIndex;
        bool androidJumpContext = jumpSelected && TryGetAndroidJumpContext(out _);
        bool elevatedLandingJumpContext =
            jumpSelected && TryGetElevatedLandingJumpContext(out _);

        if ((androidJumpContext || elevatedLandingJumpContext) &&
            TrySuspendBaseFlatJumpPenalties(out BaseJumpPenaltyState state))
        {
            try
            {
                base.OnActionReceived(actions);
            }
            finally
            {
                RestoreBaseFlatJumpPenalties(state);
            }
        }
        else
        {
            base.OnActionReceived(actions);
        }

        if (!obstacleCollisionEndedEpisode)
        {
            RewardNewlyPassedAndroids(GetGoalDirection());
        }
    }

    private bool TryGetAndroidJumpContext(out EdgeRunnerEnemyMarker marker)
    {
        marker = null;

        if (!enforceContextualJumpDiscipline || !CanSafelyInitiateContextualJump())
        {
            return false;
        }

        float goalDirection = GetGoalDirection();
        EdgeRunnerEnemyMarker candidate = SelectRelevantAndroid(goalDirection);
        if (candidate == null)
        {
            return false;
        }

        Vector2 delta = candidate.GetObservationPosition() - (Vector2)transform.position;
        float forwardDistance = delta.x * goalDirection;
        bool withinHorizontalWindow =
            forwardDistance >= androidJumpWindowMin &&
            forwardDistance <= androidJumpWindowMax;
        bool verticallyCompatible =
            HasDangerousVerticalOverlap(candidate) ||
            Mathf.Abs(delta.y) <= androidJumpVerticalTolerance;

        if (!withinHorizontalWindow || !verticallyCompatible)
        {
            return false;
        }

        marker = candidate;
        return true;
    }

    private bool TryGetElevatedLandingJumpContext(out ElevatedLandingContext context)
    {
        context = default;

        if (!allowElevatedLandingJump ||
            !enforceContextualJumpDiscipline ||
            !CanSafelyInitiateContextualJump() ||
            !TryGetPlayerBounds(out Bounds playerBounds))
        {
            return false;
        }

        int groundMask = GetGroundLayerMask();
        if (groundMask == 0)
        {
            return false;
        }

        float goalDirection = GetGoalDirection();
        float referenceX = playerBounds.center.x;
        float referenceGroundY = playerBounds.min.y;

        // Require empty space at two nearby points. This delays the exception until
        // the actual ledge and prevents a distant elevated platform from enabling
        // repeated flat-ground jumps.
        bool nearGap = !HasGroundNearCurrentHeight(
            referenceX + goalDirection * elevatedGapProbeNear,
            referenceGroundY,
            groundMask);
        bool farGap = !HasGroundNearCurrentHeight(
            referenceX + goalDirection * elevatedGapProbeFar,
            referenceGroundY,
            groundMask);
        if (!nearGap || !farGap)
        {
            return false;
        }

        float scanStart = Mathf.Max(
            elevatedLandingWindowMin,
            elevatedGapProbeFar + elevatedLandingScanStep);
        for (float distance = scanStart;
             distance <= elevatedLandingWindowMax + 0.001f;
             distance += elevatedLandingScanStep)
        {
            float sampleX = referenceX + goalDirection * distance;
            if (!TryGetFirstGroundSurfaceY(
                    sampleX,
                    referenceGroundY,
                    groundMask,
                    out float landingY))
            {
                continue;
            }

            float landingDeltaY = landingY - referenceGroundY;
            if (landingDeltaY < elevatedLandingMinHeight ||
                landingDeltaY > elevatedLandingMaxHeight)
            {
                continue;
            }

            context = new ElevatedLandingContext(distance, landingDeltaY);
            return true;
        }

        return false;
    }

    private bool HasGroundNearCurrentHeight(float sampleX, float referenceGroundY, int groundMask)
    {
        Vector2 origin = new Vector2(sampleX, referenceGroundY + 0.2f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            origin,
            Vector2.down,
            elevatedGapProbeDepth,
            groundMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D collider = hits[i].collider;
            if (collider == null || collider.isTrigger || IsPlayerCollider(collider))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool TryGetFirstGroundSurfaceY(
        float sampleX,
        float referenceGroundY,
        int groundMask,
        out float surfaceY)
    {
        surfaceY = 0f;
        float originY =
            referenceGroundY + elevatedLandingMaxHeight + elevatedLandingProbeHeadroom;
        float castDistance =
            elevatedLandingMaxHeight + elevatedLandingProbeHeadroom + elevatedGapProbeDepth;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(sampleX, originY),
            Vector2.down,
            castDistance,
            groundMask);

        bool found = false;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || IsPlayerCollider(collider))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                surfaceY = hit.point.y;
                found = true;
            }
        }

        return found;
    }

    private int GetGroundLayerMask()
    {
        if (BaseGroundLayerField?.GetValue(this) is LayerMask groundMask)
        {
            return groundMask.value;
        }

        return LayerMask.GetMask("Ground");
    }

    private bool IsPlayerCollider(Collider2D candidate)
    {
        for (int i = 0; i < playerColliders.Length; i++)
        {
            if (playerColliders[i] == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanSafelyInitiateContextualJump()
    {
        if (!IsCurrentlyGroundedForEvaluation())
        {
            return false;
        }

        bool allowJump = GetBaseBool(BaseAllowJumpField, false);
        bool jumpConsumed = GetBaseBool(BaseJumpConsumedUntilLandingField, true);
        bool waitingForRelease = GetBaseBool(BaseWaitingForJumpReleaseField, true);
        return allowJump && !jumpConsumed && !waitingForRelease;
    }

    private void EnsureBaseJumpDisciplineEnabled()
    {
        if (!enforceContextualJumpDiscipline)
        {
            return;
        }

        if (BaseMaskUselessJumpsField == null)
        {
            if (!warnedMissingBaseJumpMaskField)
            {
                warnedMissingBaseJumpMaskField = true;
                Debug.LogError(
                    "SpeedRunObstacleAware could not enable the V5 base jump discipline: " +
                    "private field 'maskUselessJumps' was not found.",
                    this);
            }

            return;
        }

        BaseMaskUselessJumpsField.SetValue(this, true);
    }

    private bool GetBaseBool(FieldInfo field, bool fallback)
    {
        return field?.GetValue(this) is bool value
            ? value
            : fallback;
    }

    private bool TrySuspendBaseFlatJumpPenalties(out BaseJumpPenaltyState state)
    {
        state = default;

        if (!(BaseFlatGroundJumpPenaltyField?.GetValue(this) is float flatPenalty) ||
            !(BaseUselessJumpPenaltyField?.GetValue(this) is float uselessPenalty))
        {
            return false;
        }

        state = new BaseJumpPenaltyState(flatPenalty, uselessPenalty);
        BaseFlatGroundJumpPenaltyField.SetValue(this, 0f);
        BaseUselessJumpPenaltyField.SetValue(this, 0f);
        return true;
    }

    private void RestoreBaseFlatJumpPenalties(BaseJumpPenaltyState state)
    {
        BaseFlatGroundJumpPenaltyField?.SetValue(this, state.flatGroundPenalty);
        BaseUselessJumpPenaltyField?.SetValue(this, state.uselessPenalty);
    }

    public void SetObstacleAwareGoal(Transform goalTransform)
    {
        obstacleAwareGoal = goalTransform;
    }

    public void RefreshObstacleCache()
    {
        obstacleMarkers = FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude);
    }

    public void NotifyObstacleCollision(SpeedRunObstacleHazard hazard)
    {
        if (obstacleCollisionEndedEpisode)
        {
            return;
        }

        obstacleCollisionEndedEpisode = true;
        AddReward(obstacleCollisionPenalty);

        if (debugObstacleAwareEvents)
        {
            string obstacleName = hazard != null ? hazard.name : "unknown";
            Debug.Log(
                $"[SPEEDRUN OBSTACLE AWARE] collision obstacle={obstacleName} " +
                $"penalty={obstacleCollisionPenalty:F3}",
                this);
        }

        EndEpisode();
    }

    private EdgeRunnerEnemyMarker SelectRelevantAndroid(float goalDirection)
    {
        EdgeRunnerEnemyMarker nearestAhead = null;
        EdgeRunnerEnemyMarker nearestBehind = null;
        float nearestAheadDistanceSqr = float.PositiveInfinity;
        float nearestBehindDistanceSqr = float.PositiveInfinity;
        Vector2 playerPosition = transform.position;

        for (int i = 0; i < obstacleMarkers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = obstacleMarkers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            Vector2 delta = marker.GetObservationPosition() - playerPosition;
            float distanceSqr = delta.sqrMagnitude;
            float forwardDistance = delta.x * goalDirection;

            if (forwardDistance >= 0f)
            {
                if (distanceSqr < nearestAheadDistanceSqr)
                {
                    nearestAhead = marker;
                    nearestAheadDistanceSqr = distanceSqr;
                }
            }
            else if (distanceSqr < nearestBehindDistanceSqr)
            {
                nearestBehind = marker;
                nearestBehindDistanceSqr = distanceSqr;
            }
        }

        return nearestAhead != null ? nearestAhead : nearestBehind;
    }

    private void RewardNewlyPassedAndroids(float goalDirection)
    {
        if (passedAndroidReward <= 0f)
        {
            return;
        }

        Vector2 playerPosition = transform.position;

        for (int i = 0; i < obstacleMarkers.Length; i++)
        {
            EdgeRunnerEnemyMarker marker = obstacleMarkers[i];

            if (marker == null || !marker.IsObservable || !marker.IsDangerous)
            {
                continue;
            }

            if (rewardedPassedAndroids.Contains(marker))
            {
                continue;
            }

            float forwardDistance =
                (marker.GetObservationPosition().x - playerPosition.x) * goalDirection;

            if (forwardDistance >= -passedAndroidMargin)
            {
                continue;
            }

            rewardedPassedAndroids.Add(marker);
            AddReward(passedAndroidReward);

            if (debugObstacleAwareEvents)
            {
                Debug.Log(
                    $"[SPEEDRUN OBSTACLE AWARE] passed obstacle={marker.name} " +
                    $"reward={passedAndroidReward:F3}",
                    this);
            }
        }
    }

    private bool HasDangerousVerticalOverlap(EdgeRunnerEnemyMarker marker)
    {
        Bounds androidBounds = marker.GetObservationBounds();

        if (!TryGetPlayerBounds(out Bounds playerBounds))
        {
            float deltaY = marker.GetObservationPosition().y - transform.position.y;
            float fallbackTolerance = Mathf.Max(0.1f, maxAndroidVerticalDistance * 0.2f);
            return Mathf.Abs(deltaY) <= fallbackTolerance;
        }

        return
            playerBounds.max.y + verticalOverlapTolerance >= androidBounds.min.y &&
            playerBounds.min.y - verticalOverlapTolerance <= androidBounds.max.y;
    }

    private bool TryGetPlayerBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider2D collider = playerColliders[i];

            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return hasBounds;
    }

    private float GetGoalDirection()
    {
        ResolveGoalReference();

        if (obstacleAwareGoal != null)
        {
            float deltaX = obstacleAwareGoal.position.x - transform.position.x;

            if (Mathf.Abs(deltaX) > 0.05f)
            {
                lastGoalDirection = Mathf.Sign(deltaX);
            }
        }

        return Mathf.Abs(lastGoalDirection) > 0.01f ? lastGoalDirection : 1f;
    }

    private void ResolveGoalReference()
    {
        if (obstacleAwareGoal != null)
        {
            return;
        }

        GameObject namedGoal = GameObject.Find("Goal");

        if (namedGoal != null)
        {
            obstacleAwareGoal = namedGoal.transform;
        }
    }

    private static void AddEmptyAndroidObservations(VectorSensor sensor)
    {
        for (int i = 0; i < AndroidObservationCount; i++)
        {
            sensor.AddObservation(0f);
        }
    }

    private static float NormalizeSigned(float value, float maximumAbsoluteValue)
    {
        return Mathf.Clamp(
            value / Mathf.Max(0.0001f, maximumAbsoluteValue),
            -1f,
            1f);
    }

    private static float NormalizePositive(float value, float maximumValue)
    {
        return Mathf.Clamp01(value / Mathf.Max(0.0001f, maximumValue));
    }

    private void ValidateBehaviorName()
    {
        BehaviorParameters behavior = GetComponent<BehaviorParameters>();

        if (behavior != null && behavior.BehaviorName != ExpectedBehaviorName)
        {
            Debug.LogWarning(
                $"EdgeRunnerAgentV5SpeedRunObstacleAware expects Behavior Name " +
                $"'{ExpectedBehaviorName}', but found '{behavior.BehaviorName}'.",
                this);
        }
    }

    private void ValidateObstacleSettings()
    {
        maxAndroidHorizontalDistance = Mathf.Max(0.1f, maxAndroidHorizontalDistance);
        maxAndroidVerticalDistance = Mathf.Max(0.1f, maxAndroidVerticalDistance);
        maxAndroidTotalDistance = Mathf.Max(0.1f, maxAndroidTotalDistance);
        maxAndroidSpeed = Mathf.Max(0.1f, maxAndroidSpeed);
        maxRelativeClosingSpeed = Mathf.Max(0.1f, maxRelativeClosingSpeed);
        horizontalDangerWindow = Mathf.Max(0f, horizontalDangerWindow);
        verticalOverlapTolerance = Mathf.Max(0f, verticalOverlapTolerance);
        passedAndroidMargin = Mathf.Max(0.05f, passedAndroidMargin);
        androidJumpWindowMin = Mathf.Max(0.05f, androidJumpWindowMin);
        androidJumpWindowMax = Mathf.Max(androidJumpWindowMin, androidJumpWindowMax);
        androidJumpVerticalTolerance = Mathf.Max(0f, androidJumpVerticalTolerance);
        elevatedGapProbeNear = Mathf.Max(0.05f, elevatedGapProbeNear);
        elevatedGapProbeFar = Mathf.Max(elevatedGapProbeNear, elevatedGapProbeFar);
        elevatedGapProbeDepth = Mathf.Max(0.1f, elevatedGapProbeDepth);
        elevatedLandingWindowMin = Mathf.Max(elevatedGapProbeFar, elevatedLandingWindowMin);
        elevatedLandingWindowMax = Mathf.Max(
            elevatedLandingWindowMin,
            elevatedLandingWindowMax);
        elevatedLandingScanStep = Mathf.Clamp(elevatedLandingScanStep, 0.05f, 1f);
        elevatedLandingMinHeight = Mathf.Max(0.05f, elevatedLandingMinHeight);
        elevatedLandingMaxHeight = Mathf.Max(
            elevatedLandingMinHeight,
            elevatedLandingMaxHeight);
        elevatedLandingProbeHeadroom = Mathf.Max(0.05f, elevatedLandingProbeHeadroom);
        obstacleCollisionPenalty = Mathf.Min(-0.01f, obstacleCollisionPenalty);
        passedAndroidReward = Mathf.Max(0f, passedAndroidReward);
    }

    private readonly struct ElevatedLandingContext
    {
        public readonly float distance;
        public readonly float deltaY;

        public ElevatedLandingContext(float distance, float deltaY)
        {
            this.distance = distance;
            this.deltaY = deltaY;
        }
    }

    private readonly struct BaseJumpPenaltyState
    {
        public readonly float flatGroundPenalty;
        public readonly float uselessPenalty;

        public BaseJumpPenaltyState(float flatGroundPenalty, float uselessPenalty)
        {
            this.flatGroundPenalty = flatGroundPenalty;
            this.uselessPenalty = uselessPenalty;
        }
    }
}
