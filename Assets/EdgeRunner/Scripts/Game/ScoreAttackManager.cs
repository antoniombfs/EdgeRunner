using System.Collections.Generic;
using UnityEngine;

public enum ScoreMaxObjectiveType
{
    None = 0,
    Coin = 1,
    Enemy = 2,
    Goal = 3
}

public class ScoreAttackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EdgeRunnerAgentV5 agent;
    [SerializeField] private Transform goal;

    [Header("Rewards")]
    [SerializeField] private float coinReward = 1.0f;
    [SerializeField] private float enemyKillReward = 3.0f;
    [SerializeField] private float finalCompletionReward = 10.0f;
    [SerializeField] private float prematureGoalPenalty = -2.0f;
    [SerializeField] private float enemySideHitPenalty = -6.0f;
    [SerializeField] private float repeatedPrematureGoalCooldown = 0.75f;
    [SerializeField] private bool endEpisodeOnPrematureGoal = false;

    [Header("Episode")]
    [SerializeField] private bool resetOnStart = true;
    [SerializeField] private bool randomizeObjectPositionsOnReset = false;
    [SerializeField] private int minActiveCoins = 1;
    [SerializeField] private int maxActiveCoins = 3;
    [SerializeField] private int minActiveEnemies = 1;
    [SerializeField] private int maxActiveEnemies = 2;
    [SerializeField] private Vector2 coinRandomXRange = new Vector2(2.5f, 8.5f);
    [SerializeField] private Vector2 enemyRandomXRange = new Vector2(5.0f, 9.5f);
    [SerializeField] private float coinY = 1.55f;
    [SerializeField] private float enemyY = 1.02f;

    [Header("Random Coin Placement")]
    [SerializeField] private float coinPlatformTopY = 0f;
    [SerializeField] private float coinVerticalOffset = 1.2f;
    [SerializeField] private float minCoinSpacing = 2.0f;
    [SerializeField] private float coinEdgeMargin = 1.0f;
    [SerializeField] private float minCoinDistanceFromAndroid = 2.0f;
    [SerializeField] private float minCoinDistanceFromGoal = 2.5f;
    [SerializeField] private int maxCoinPlacementAttempts = 30;

    [Header("Ordered Curriculum Coin Placement")]
    [SerializeField] private bool useOrderedCoinXSlots = false;
    [SerializeField] private Vector2 firstCoinXRange = new Vector2(3.0f, 4.5f);
    [SerializeField] private Vector2 secondCoinXRange = new Vector2(5.8f, 7.2f);
    [SerializeField] private bool debugScoreMaxRandomWarmupPositions = false;

    [Header("ScoreMax Objective Selection")]
    [SerializeField] private bool preferForwardCoinObjectives = false;
    [SerializeField] private float forwardCoinObjectiveTolerance = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly List<ScoreAttackCoin> coins = new List<ScoreAttackCoin>();
    private readonly List<ScoreAttackAndroid> enemies = new List<ScoreAttackAndroid>();
    private readonly HashSet<ScoreAttackCoin> collectedCoins = new HashSet<ScoreAttackCoin>();
    private readonly HashSet<ScoreAttackAndroid> killedEnemies = new HashSet<ScoreAttackAndroid>();

    private int activeCoinCount;
    private int activeEnemyCount;
    private float nextPrematureGoalPenaltyTime;
    private bool finalRewardGranted;

    public int CoinsCollected { get; private set; }
    public int EnemiesKilled { get; private set; }
    public int PrematureGoalTouches { get; private set; }
    public int CoinsRemaining => Mathf.Max(0, activeCoinCount - CoinsCollected);
    public int EnemiesRemaining => Mathf.Max(0, activeEnemyCount - EnemiesKilled);
    public bool ObjectivesComplete => CoinsRemaining == 0 && EnemiesRemaining == 0;
    public float CoinReward => coinReward;
    public float EnemyKillReward => enemyKillReward;
    public Transform Goal => goal;
    public IReadOnlyList<ScoreAttackCoin> Coins => coins;
    public IReadOnlyList<ScoreAttackAndroid> Enemies => enemies;

    private void Start()
    {
        RegisterSceneObjects();

        if (resetOnStart)
        {
            ResetEpisode();
        }
    }

    public void RegisterCoin(ScoreAttackCoin coin)
    {
        if (coin != null && !coins.Contains(coin))
        {
            coins.Add(coin);
        }
    }

    public void RegisterEnemy(ScoreAttackAndroid enemy)
    {
        if (enemy != null && !enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public void ResetEpisode()
    {
        RegisterSceneObjects();

        CoinsCollected = 0;
        EnemiesKilled = 0;
        PrematureGoalTouches = 0;
        finalRewardGranted = false;
        nextPrematureGoalPenaltyTime = 0f;
        collectedCoins.Clear();
        killedEnemies.Clear();

        activeCoinCount = randomizeObjectPositionsOnReset
            ? Mathf.Clamp(Random.Range(minActiveCoins, maxActiveCoins + 1), 0, coins.Count)
            : coins.Count;
        activeEnemyCount = randomizeObjectPositionsOnReset
            ? Mathf.Clamp(Random.Range(minActiveEnemies, maxActiveEnemies + 1), 0, enemies.Count)
            : enemies.Count;

        List<Vector3> enemyPositions = randomizeObjectPositionsOnReset
            ? BuildEnemyPositions()
            : new List<Vector3>();
        List<Vector3> coinPositions = randomizeObjectPositionsOnReset
            ? BuildCoinPositions(enemyPositions)
            : new List<Vector3>();

        for (int i = 0; i < coins.Count; i++)
        {
            bool active = i < activeCoinCount;
            Vector3 position = active && randomizeObjectPositionsOnReset && i < coinPositions.Count
                ? coinPositions[i]
                : coins[i].InitialPosition;
            coins[i].ResetForNewEpisode(active, position);
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            bool active = i < activeEnemyCount;
            Vector3 position = active && randomizeObjectPositionsOnReset && i < enemyPositions.Count
                ? enemyPositions[i]
                : enemies[i].InitialPosition;
            enemies[i].ResetForNewEpisode(active, position);
        }

        LogRandomWarmupPositions();

        if (debugLogs)
        {
            Debug.Log($"[SCORE ATTACK] ResetEpisode coins={activeCoinCount} enemies={activeEnemyCount}", this);
        }
    }

    private List<Vector3> BuildEnemyPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < activeEnemyCount; i++)
        {
            Vector3 position = randomizeObjectPositionsOnReset
                ? new Vector3(Random.Range(enemyRandomXRange.x, enemyRandomXRange.y), enemyY, 0f)
                : enemies[i].InitialPosition;
            positions.Add(position);
        }

        return positions;
    }

    private List<Vector3> BuildCoinPositions(List<Vector3> enemyPositions)
    {
        if (useOrderedCoinXSlots)
        {
            return BuildOrderedCoinPositions(enemyPositions);
        }

        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < activeCoinCount; i++)
        {
            if (TryFindValidCoinPosition(positions, enemyPositions, out Vector3 position))
            {
                positions.Add(position);
            }
            else if (TryFindFallbackCoinPosition(positions, enemyPositions, out position))
            {
                positions.Add(position);
            }
            else
            {
                activeCoinCount = positions.Count;
                break;
            }
        }

        return positions;
    }

    private List<Vector3> BuildOrderedCoinPositions(List<Vector3> enemyPositions)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < activeCoinCount; i++)
        {
            Vector2 range = i == 0
                ? firstCoinXRange
                : i == 1
                    ? secondCoinXRange
                    : coinRandomXRange;

            if (TryFindValidCoinPositionInRange(range, positions, enemyPositions, out Vector3 position))
            {
                positions.Add(position);
                continue;
            }

            activeCoinCount = positions.Count;
            break;
        }

        return positions;
    }

    private bool TryFindValidCoinPositionInRange(
        Vector2 range,
        List<Vector3> placedCoins,
        List<Vector3> enemyPositions,
        out Vector3 position)
    {
        float minX = Mathf.Min(range.x, range.y);
        float maxX = Mathf.Max(range.x, range.y);
        int attempts = Mathf.Max(1, maxCoinPlacementAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            position = new Vector3(Random.Range(minX, maxX), GetCoinPlacementY(), 0f);
            if (IsValidCoinPosition(position, placedCoins, enemyPositions))
            {
                return true;
            }
        }

        float step = Mathf.Max(0.1f, (maxX - minX) / 10f);
        for (float x = minX; x <= maxX + 0.001f; x += step)
        {
            position = new Vector3(x, GetCoinPlacementY(), 0f);
            if (IsValidCoinPosition(position, placedCoins, enemyPositions))
            {
                return true;
            }
        }

        position = default;
        return false;
    }

    private bool TryFindValidCoinPosition(
        List<Vector3> placedCoins,
        List<Vector3> enemyPositions,
        out Vector3 position)
    {
        int attempts = Mathf.Max(1, maxCoinPlacementAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            position = CreateRandomCoinPosition();

            if (IsValidCoinPosition(position, placedCoins, enemyPositions))
            {
                return true;
            }
        }

        position = default;
        return false;
    }

    private bool TryFindFallbackCoinPosition(
        List<Vector3> placedCoins,
        List<Vector3> enemyPositions,
        out Vector3 position)
    {
        float minX = GetCoinPlacementMinX();
        float maxX = GetCoinPlacementMaxX();

        if (maxX < minX)
        {
            position = default;
            return false;
        }

        float step = Mathf.Max(0.25f, minCoinSpacing * 0.5f);

        for (float x = minX; x <= maxX + 0.001f; x += step)
        {
            position = new Vector3(x, GetCoinPlacementY(), 0f);

            if (IsValidCoinPosition(position, placedCoins, enemyPositions))
            {
                return true;
            }
        }

        position = default;
        return false;
    }

    private Vector3 CreateRandomCoinPosition()
    {
        float minX = GetCoinPlacementMinX();
        float maxX = GetCoinPlacementMaxX();

        if (maxX < minX)
        {
            float centerX = (coinRandomXRange.x + coinRandomXRange.y) * 0.5f;
            return new Vector3(centerX, GetCoinPlacementY(), 0f);
        }

        return new Vector3(
            Random.Range(minX, maxX),
            GetCoinPlacementY(),
            0f
        );
    }

    private bool IsValidCoinPosition(
        Vector3 position,
        List<Vector3> placedCoins,
        List<Vector3> enemyPositions)
    {
        for (int i = 0; i < placedCoins.Count; i++)
        {
            if (Mathf.Abs(position.x - placedCoins[i].x) < minCoinSpacing)
            {
                return false;
            }
        }

        for (int i = 0; i < enemyPositions.Count; i++)
        {
            if (Mathf.Abs(position.x - enemyPositions[i].x) < minCoinDistanceFromAndroid)
            {
                return false;
            }
        }

        if (goal != null && Mathf.Abs(position.x - goal.position.x) < minCoinDistanceFromGoal)
        {
            return false;
        }

        return true;
    }

    private float GetCoinPlacementMinX()
    {
        return Mathf.Min(coinRandomXRange.x, coinRandomXRange.y) + coinEdgeMargin;
    }

    private float GetCoinPlacementMaxX()
    {
        return Mathf.Max(coinRandomXRange.x, coinRandomXRange.y) - coinEdgeMargin;
    }

    private float GetCoinPlacementY()
    {
        if (coinVerticalOffset <= 0f)
        {
            return coinY;
        }

        return coinPlatformTopY + coinVerticalOffset;
    }

    public void CollectCoin(ScoreAttackCoin coin, EdgeRunnerAgentV5 sourceAgent)
    {
        if (coin == null || !coins.Contains(coin) || collectedCoins.Contains(coin))
        {
            return;
        }

        collectedCoins.Add(coin);
        CoinsCollected = Mathf.Clamp(CoinsCollected + 1, 0, Mathf.Max(1, activeCoinCount));
        AddAgentReward(sourceAgent, coinReward);

        if (debugLogs)
        {
            Debug.Log($"[SCORE ATTACK] Coin collected {CoinsCollected}/{activeCoinCount}", coin);
        }
    }

    public void KillEnemy(ScoreAttackAndroid enemy, EdgeRunnerAgentV5 sourceAgent)
    {
        if (enemy == null || !enemies.Contains(enemy) || killedEnemies.Contains(enemy))
        {
            return;
        }

        killedEnemies.Add(enemy);
        EnemiesKilled = Mathf.Clamp(EnemiesKilled + 1, 0, Mathf.Max(1, activeEnemyCount));
        AddAgentReward(sourceAgent, enemyKillReward);

        if (debugLogs)
        {
            Debug.Log($"[SCORE ATTACK] Enemy killed {EnemiesKilled}/{activeEnemyCount}", enemy);
        }
    }

    public void HandleEnemySideHit(ScoreAttackAndroid enemy, EdgeRunnerAgentV5 sourceAgent)
    {
        if (enemy == null || !enemy.IsAlive || killedEnemies.Contains(enemy))
        {
            return;
        }

        EdgeRunnerAgentV5 targetAgent = sourceAgent != null ? sourceAgent : agent;

        if (targetAgent == null)
        {
            return;
        }

        if (debugLogs)
        {
            Debug.Log($"[SCORE ATTACK] Enemy side hit penalty={enemySideHitPenalty}", enemy);
        }

        targetAgent.ScoreAttackEnemyHit(enemySideHitPenalty);
    }

    public bool TryHandleGoalReached(EdgeRunnerAgentV5 sourceAgent)
    {
        EdgeRunnerAgentV5 targetAgent = sourceAgent != null ? sourceAgent : agent;

        if (!ObjectivesComplete)
        {
            if (Time.time >= nextPrematureGoalPenaltyTime)
            {
                PrematureGoalTouches++;
                nextPrematureGoalPenaltyTime = Time.time + Mathf.Max(0.05f, repeatedPrematureGoalCooldown);
                AddAgentReward(targetAgent, prematureGoalPenalty);
            }

            if (debugLogs)
            {
                Debug.Log(
                    $"[SCORE ATTACK] Goal blocked coinsRemaining={CoinsRemaining} enemiesRemaining={EnemiesRemaining}",
                    this);
            }

            if (endEpisodeOnPrematureGoal && targetAgent != null)
            {
                targetAgent.EndEpisode();
            }

            return false;
        }

        if (!finalRewardGranted)
        {
            finalRewardGranted = true;
            AddAgentReward(targetAgent, finalCompletionReward);
        }

        if (debugLogs)
        {
            Debug.Log("[SCORE ATTACK] Objectives complete; goal allowed.", this);
        }

        return true;
    }

    public bool TryGetNearestActiveCoin(Vector3 fromPosition, out ScoreAttackCoin nearestCoin, out float distance)
    {
        RegisterSceneObjects();

        nearestCoin = null;
        distance = float.PositiveInfinity;

        for (int i = 0; i < coins.Count; i++)
        {
            ScoreAttackCoin coin = coins[i];

            if (coin == null || !coin.IsAvailable || collectedCoins.Contains(coin))
            {
                continue;
            }

            float candidateDistance = Vector2.Distance(fromPosition, coin.transform.position);

            if (candidateDistance < distance)
            {
                nearestCoin = coin;
                distance = candidateDistance;
            }
        }

        return nearestCoin != null;
    }

    public bool TryGetNearestActiveEnemy(Vector3 fromPosition, out ScoreAttackAndroid nearestEnemy, out float distance)
    {
        RegisterSceneObjects();

        nearestEnemy = null;
        distance = float.PositiveInfinity;

        for (int i = 0; i < enemies.Count; i++)
        {
            ScoreAttackAndroid enemy = enemies[i];

            if (enemy == null || !enemy.IsAlive || killedEnemies.Contains(enemy) || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            float candidateDistance = Vector2.Distance(fromPosition, enemy.transform.position);

            if (candidateDistance < distance)
            {
                nearestEnemy = enemy;
                distance = candidateDistance;
            }
        }

        return nearestEnemy != null;
    }

    public bool TryGetRecommendedNextObjective(
        Vector3 fromPosition,
        out Transform target,
        out ScoreMaxObjectiveType objectiveType,
        out float distance)
    {
        bool hasCoin = preferForwardCoinObjectives
            ? TryGetRelevantActiveCoin(fromPosition, out ScoreAttackCoin nearestCoin, out distance)
            : TryGetNearestActiveCoin(fromPosition, out nearestCoin, out distance);

        if (hasCoin)
        {
            target = nearestCoin.transform;
            objectiveType = ScoreMaxObjectiveType.Coin;
            return true;
        }

        if (TryGetNearestActiveEnemy(fromPosition, out ScoreAttackAndroid nearestEnemy, out distance))
        {
            target = nearestEnemy.transform;
            objectiveType = ScoreMaxObjectiveType.Enemy;
            return true;
        }

        if (goal != null)
        {
            target = goal;
            objectiveType = ScoreMaxObjectiveType.Goal;
            distance = Vector2.Distance(fromPosition, goal.position);
            return true;
        }

        target = null;
        objectiveType = ScoreMaxObjectiveType.None;
        distance = 0f;
        return false;
    }

    private bool TryGetRelevantActiveCoin(
        Vector3 fromPosition,
        out ScoreAttackCoin relevantCoin,
        out float distance)
    {
        RegisterSceneObjects();

        relevantCoin = null;
        distance = float.PositiveInfinity;
        float bestSelectionScore = float.PositiveInfinity;
        float forwardDirection = goal != null && goal.position.x < fromPosition.x ? -1f : 1f;
        float behindPreferenceCost = Mathf.Max(0f, forwardCoinObjectiveTolerance);

        for (int i = 0; i < coins.Count; i++)
        {
            ScoreAttackCoin candidate = coins[i];
            if (candidate == null || !candidate.IsAvailable || collectedCoins.Contains(candidate))
            {
                continue;
            }

            float candidateDistance = Vector2.Distance(fromPosition, candidate.transform.position);
            float forwardDelta =
                (candidate.transform.position.x - fromPosition.x) * forwardDirection;
            bool isBehind = forwardDelta < -behindPreferenceCost;
            float selectionScore = candidateDistance + (isBehind ? behindPreferenceCost : 0f);
            if (selectionScore < bestSelectionScore)
            {
                bestSelectionScore = selectionScore;
                relevantCoin = candidate;
                distance = candidateDistance;
            }
        }

        return relevantCoin != null;
    }

    private void RegisterSceneObjects()
    {
        ScoreAttackCoin[] sceneCoins = FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Include);

        for (int i = 0; i < sceneCoins.Length; i++)
        {
            RegisterCoin(sceneCoins[i]);
            sceneCoins[i].SetManager(this);
        }

        if (useOrderedCoinXSlots)
        {
            coins.Sort(CompareCoinsByName);
        }

        ScoreAttackAndroid[] sceneEnemies = FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Include);

        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            RegisterEnemy(sceneEnemies[i]);
            sceneEnemies[i].SetManager(this);
        }
    }

    private void LogRandomWarmupPositions()
    {
        if (!debugScoreMaxRandomWarmupPositions)
        {
            return;
        }

        string coin1X = coins.Count > 0 && coins[0] != null
            ? coins[0].transform.position.x.ToString("F2")
            : "missing";
        string coin2X = coins.Count > 1 && coins[1] != null
            ? coins[1].transform.position.x.ToString("F2")
            : "missing";
        string androidX = enemies.Count > 0 && enemies[0] != null
            ? enemies[0].transform.position.x.ToString("F2")
            : "missing";
        string goalX = goal != null ? goal.position.x.ToString("F2") : "missing";

        Debug.Log(
            $"[SCOREMAX RANDOM WARMUP] coin1X={coin1X} coin2X={coin2X} " +
            $"androidX={androidX} goalX={goalX}",
            this);
    }

    private static int CompareCoinsByName(ScoreAttackCoin left, ScoreAttackCoin right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private void AddAgentReward(EdgeRunnerAgentV5 sourceAgent, float reward)
    {
        EdgeRunnerAgentV5 targetAgent = sourceAgent != null ? sourceAgent : agent;

        if (targetAgent != null)
        {
            targetAgent.AddReward(reward);
        }
    }

    private void OnValidate()
    {
        minActiveCoins = Mathf.Max(0, minActiveCoins);
        maxActiveCoins = Mathf.Max(minActiveCoins, maxActiveCoins);
        minActiveEnemies = Mathf.Max(0, minActiveEnemies);
        maxActiveEnemies = Mathf.Max(minActiveEnemies, maxActiveEnemies);
        repeatedPrematureGoalCooldown = Mathf.Max(0.05f, repeatedPrematureGoalCooldown);
        coinVerticalOffset = Mathf.Max(0.1f, coinVerticalOffset);
        minCoinSpacing = Mathf.Max(0f, minCoinSpacing);
        coinEdgeMargin = Mathf.Max(0f, coinEdgeMargin);
        minCoinDistanceFromAndroid = Mathf.Max(0f, minCoinDistanceFromAndroid);
        minCoinDistanceFromGoal = Mathf.Max(0f, minCoinDistanceFromGoal);
        maxCoinPlacementAttempts = Mathf.Max(1, maxCoinPlacementAttempts);
        forwardCoinObjectiveTolerance = Mathf.Max(0f, forwardCoinObjectiveTolerance);
    }
}
