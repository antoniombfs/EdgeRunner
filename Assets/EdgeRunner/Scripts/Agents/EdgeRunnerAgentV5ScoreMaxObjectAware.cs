using System;
using System.Collections;
using System.Reflection;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EdgeRunnerAgentV5ScoreMaxObjectAware : EdgeRunnerAgentV5
{
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

    [Header("ObjectAware Gap Context")]
    [SerializeField] private float nearGapProbeDistance = 0.8f;
    [SerializeField] private float midGapProbeDistance = 1.6f;
    [SerializeField] private float farGapProbeDistance = 2.4f;
    [SerializeField] private float landingProbeDistance = 3.4f;
    [SerializeField] private float gapProbeDepth = 3.5f;

    [Header("ObjectAware Debug")]
    [SerializeField] private bool debugObjectAwareObservationCount = false;
    [SerializeField] private bool debugObjectAwareNextObjective = false;
    [SerializeField] private bool debugObjectAwareJumpContext = false;
    [SerializeField] private bool debugObjectAwareGizmos = false;
    [SerializeField] private float debugObjectAwareLogInterval = 1f;

    private static readonly FieldInfo BaseGroundLayerField =
        typeof(EdgeRunnerAgentV5).GetField("groundLayer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BaseCoyoteTimerField =
        typeof(EdgeRunnerAgentV5).GetField("coyoteTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo VectorSensorObservationsField =
        typeof(VectorSensor).GetField("m_Observations", BindingFlags.Instance | BindingFlags.NonPublic);

    private ScoreAttackCoin[] cachedCoins = Array.Empty<ScoreAttackCoin>();
    private ScoreAttackAndroid[] cachedAndroids = Array.Empty<ScoreAttackAndroid>();
    private float nextObjectScanTime;
    private float nextDebugLogTime;
    private bool loggedObservationCountThisEpisode;
    private bool warnedObservationMismatchThisEpisode;

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
        public bool jumpContextValid;
        public bool shouldNotJumpNow;
    }

    public override void Initialize()
    {
        ResolveObjectAwareReferences();
        base.Initialize();
        ResolveObjectAwareReferences();
        RefreshObjectCache(true);
    }

    public override void OnEpisodeBegin()
    {
        ResolveObjectAwareReferences();
        base.OnEpisodeBegin();
        ResolveObjectAwareReferences();
        RefreshObjectCache(true);
        loggedObservationCountThisEpisode = false;
        warnedObservationMismatchThisEpisode = false;
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
        Add(sensor, context.androidStompContext, ref count);
        return count;
    }

    private ObjectAwareContext BuildContext()
    {
        TargetSnapshot nearestCoin = FindNearestCoin(null);
        TargetSnapshot lowCoin = FindNearestCoin(true);
        TargetSnapshot highCoin = FindNearestCoin(false);
        TargetSnapshot android = FindNearestAndroid();
        TargetSnapshot goal = CreateTargetSnapshot(objectAwareGoal, ObjectAwareObjectiveType.Goal);
        TargetSnapshot nextObjective = nearestCoin.exists
            ? nearestCoin
            : android.exists
                ? android
                : goal;
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
            highCoin.exists && highCoin.ahead && highCoinForwardDistance <= highCoinJumpWindowX;
        bool androidStompContext =
            android.exists &&
            android.ahead &&
            androidForwardDistance <= androidContextWindowX &&
            Mathf.Abs(android.delta.y) <= androidVerticalTolerance;
        bool nextRequiresJump =
            gap.gapAhead ||
            (nextObjective.type == ObjectAwareObjectiveType.Coin &&
             nextObjective.delta.y > lowCoinHeightThreshold) ||
            (nextObjective.type == ObjectAwareObjectiveType.Android && androidStompContext);
        bool canInitiateJump = grounded || coyoteTimer > 0f;
        bool jumpContextValid =
            canInitiateJump && (gap.gapAhead || highCoinJumpContext || androidStompContext);

        return new ObjectAwareContext
        {
            coinsRemaining = CountLiveCoins(),
            enemiesRemaining = CountLiveAndroids(),
            objectivesComplete = !nearestCoin.exists && !android.exists,
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
            jumpContextValid = jumpContextValid,
            shouldNotJumpNow = canInitiateJump && !jumpContextValid
        };
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
            bool isLow = delta.y <= lowCoinHeightThreshold;
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

    private void ResolveObjectAwareReferences()
    {
        if (objectAwareRigidbody == null)
        {
            objectAwareRigidbody = GetComponent<Rigidbody2D>();
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
            Debug.Log(
                $"[OBJECT AWARE NEXT] type={FormatObjectiveType(next.type)} " +
                $"exists={next.exists} dx={next.delta.x:F2} dy={next.delta.y:F2} " +
                $"dist={next.distance:F2} ahead={next.ahead} " +
                $"coins={context.coinsRemaining} enemies={context.enemiesRemaining}",
                this);
        }

        if (debugObjectAwareJumpContext)
        {
            Debug.Log(
                $"[OBJECT AWARE JUMP] grounded={context.grounded} " +
                $"coyote={context.coyoteTimer:F3} gapAhead={context.gap.gapAhead} " +
                $"highCoin={context.highCoinJumpContext} android={context.androidStompContext} " +
                $"valid={context.jumpContextValid} shouldNotJump={context.shouldNotJumpNow}",
                this);
        }
    }

    private static string FormatObjectiveType(ObjectAwareObjectiveType type)
    {
        return type switch
        {
            ObjectAwareObjectiveType.Coin => "coin",
            ObjectAwareObjectiveType.Android => "android",
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
        nearGapProbeDistance = Mathf.Max(0.1f, nearGapProbeDistance);
        midGapProbeDistance = Mathf.Max(nearGapProbeDistance, midGapProbeDistance);
        farGapProbeDistance = Mathf.Max(midGapProbeDistance, farGapProbeDistance);
        landingProbeDistance = Mathf.Max(farGapProbeDistance, landingProbeDistance);
        gapProbeDepth = Mathf.Max(0.2f, gapProbeDepth);
        debugObjectAwareLogInterval = Mathf.Max(0.05f, debugObjectAwareLogInterval);
    }
}
