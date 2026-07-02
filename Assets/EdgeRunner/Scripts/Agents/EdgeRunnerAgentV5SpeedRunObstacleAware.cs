using System.Collections.Generic;
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

    [Header("Obstacle Rewards")]
    [SerializeField] private float obstacleCollisionPenalty = -6f;
    [SerializeField] private float passedAndroidReward = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugObstacleAwareEvents = false;

    private readonly HashSet<int> rewardedPassedAndroids = new HashSet<int>();
    private EdgeRunnerEnemyMarker[] obstacleMarkers = System.Array.Empty<EdgeRunnerEnemyMarker>();
    private Collider2D[] playerColliders = System.Array.Empty<Collider2D>();
    private float lastGoalDirection = 1f;
    private bool obstacleCollisionEndedEpisode;

    public override void Initialize()
    {
        ValidateObstacleSettings();
        base.Initialize();
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

    public override void OnActionReceived(ActionBuffers actions)
    {
        base.OnActionReceived(actions);

        if (!obstacleCollisionEndedEpisode)
        {
            RewardNewlyPassedAndroids(GetGoalDirection());
        }
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

            int markerId = marker.GetInstanceID();

            if (rewardedPassedAndroids.Contains(markerId))
            {
                continue;
            }

            float forwardDistance =
                (marker.GetObservationPosition().x - playerPosition.x) * goalDirection;

            if (forwardDistance >= -passedAndroidMargin)
            {
                continue;
            }

            rewardedPassedAndroids.Add(markerId);
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
        obstacleCollisionPenalty = Mathf.Min(-0.01f, obstacleCollisionPenalty);
        passedAndroidReward = Mathf.Max(0f, passedAndroidReward);
    }
}
