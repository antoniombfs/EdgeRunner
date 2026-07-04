using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class FinalDemoRandomMaxScore : MonoBehaviour
{
    public const string SceneName = "ER_FinalDemo_MaxScore_Random";

    private static int currentSeed;
    private static bool hasSeed;

    private static readonly FieldInfo HighCoin01GateField = typeof(EdgeRunnerAgentV5ScoreMaxObjectAware)
        .GetField("finalLongHighCoin01LandingGateX", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Android01GateField = typeof(EdgeRunnerAgentV5ScoreMaxObjectAware)
        .GetField("finalLongAndroid01LandingGateX", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo Android02GateField = typeof(EdgeRunnerAgentV5ScoreMaxObjectAware)
        .GetField("finalLongAndroid02LandingGateX", BindingFlags.Instance | BindingFlags.NonPublic);

    [SerializeField] private Transform[] safePlatforms;
    [SerializeField] private ScoreAttackCoin[] coins;
    [SerializeField] private ScoreAttackAndroid[] androids;
    [SerializeField] private Transform goal;
    [SerializeField] private FinalDemoController controller;

    public static int CurrentSeed => hasSeed ? currentSeed : 0;

    public static void RequestNewSeed()
    {
        currentSeed = UnityEngine.Random.Range(100000, 999999);
        hasSeed = true;
    }

    public static void LoadNewRandomScene()
    {
        RequestNewSeed();
        LoadScene();
    }

    public static void ReloadSameSeed()
    {
        if (!hasSeed)
        {
            RequestNewSeed();
        }
        LoadScene();
    }

    private static void LoadScene()
    {
#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(SceneName))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                $"Assets/EdgeRunner/Scenes/DemoFinal/{SceneName}.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(SceneName);
    }

    public void Configure(
        Transform[] platformTransforms,
        ScoreAttackCoin[] scoreCoins,
        ScoreAttackAndroid[] scoreAndroids,
        Transform goalTransform,
        FinalDemoController levelController)
    {
        safePlatforms = platformTransforms;
        coins = scoreCoins;
        androids = scoreAndroids;
        goal = goalTransform;
        controller = levelController;
    }

    private void Awake()
    {
        if (!hasSeed)
        {
            RequestNewSeed();
        }
        Generate(currentSeed);
    }

    // A high coin must be unambiguously one of two safe patterns — never something in between,
    // which is exactly what was causing "grab coin then fall in the next gap":
    //
    //   Pattern A (same-platform): the coin sits well back from the right edge. The agent hops
    //   up, grabs it, comes back down on the SAME deck, and still has a large runway left to set
    //   up the next gap on its own terms.
    //
    //   Pattern B (gap-arc): the coin sits at the apex of the jump ACROSS the gap, between the
    //   current deck and the next one. There is no "same platform" landing to speak of — the
    //   coin is collected mid-arc while already committed to the jump, and the expected landing
    //   is the next deck, which always has generous room of its own.
    //
    // Classification is decided once from the reference layout (not per seed / not random), so
    // it never flip-flops between runs: HighCoin_03 sits only ~3.8m from its deck's edge in the
    // reference scene — too close to safely be Pattern A — so it is always treated as Pattern B.
    private const float CoinLeftMargin = 1.3f;
    private const float LowCoinRightMargin = 1.6f;
    private const float GapArcClassificationThreshold = 6.0f;
    private const float SameDeckHighCoinRunway = 6.0f;
    private const float MinArcLandingWidth = 6.0f;
    private const int MaxGenerateAttempts = 8;

    private void Generate(int seed)
    {
        if (safePlatforms == null || safePlatforms.Length != 6 ||
            coins == null || coins.Length != 7 ||
            androids == null || androids.Length != 2 || goal == null)
        {
            Debug.LogError("[RANDOM MAXSCORE] Scene references are incomplete.", this);
            return;
        }

        int count = safePlatforms.Length;

        // Capture the untouched reference geometry ONCE (transforms are still original here).
        float[] originalCenter = new float[count];
        float[] originalWidth = new float[count];
        float[] originalLeftEdge = new float[count];
        float[] originalY = new float[count];
        for (int i = 0; i < count; i++)
        {
            originalCenter[i] = safePlatforms[i] != null ? safePlatforms[i].position.x : 0f;
            originalWidth[i] = safePlatforms[i] != null ? safePlatforms[i].localScale.x : 0f;
            originalLeftEdge[i] = originalCenter[i] - originalWidth[i] * 0.5f;
            originalY[i] = safePlatforms[i] != null ? safePlatforms[i].position.y : 0f;
        }

        int NearestPlatform(float x)
        {
            int best = 0;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                float half = originalWidth[i] * 0.5f;
                float distance = (x < originalCenter[i] - half) ? (originalCenter[i] - half - x)
                    : (x > originalCenter[i] + half) ? (x - (originalCenter[i] + half))
                    : 0f;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        Array.Sort(coins, CompareByName);
        int[] coinPlatform = new int[coins.Length];
        float[] coinOffset = new float[coins.Length];
        float[] coinOffsetY = new float[coins.Length];
        bool[] coinIsHigh = new bool[coins.Length];
        bool[] coinIsGapArc = new bool[coins.Length];
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] == null)
            {
                continue;
            }
            int p = coinPlatform[i] = NearestPlatform(coins[i].transform.position.x);
            coinOffset[i] = coins[i].transform.position.x - originalCenter[p];
            coinOffsetY[i] = coins[i].transform.position.y - originalY[p];
            coinIsHigh[i] = coins[i].name.IndexOf("HighCoin", StringComparison.OrdinalIgnoreCase) >= 0;

            float originalRightRunway = (originalCenter[p] + originalWidth[p] * 0.5f)
                - coins[i].transform.position.x;
            coinIsGapArc[i] = coinIsHigh[i]
                && originalRightRunway < GapArcClassificationThreshold
                && p + 1 < count;
        }

        Array.Sort(androids, CompareByName);
        int[] androidPlatform = new int[androids.Length];
        float[] androidOffset = new float[androids.Length];
        float[] androidOffsetY = new float[androids.Length];
        for (int i = 0; i < androids.Length; i++)
        {
            if (androids[i] == null)
            {
                continue;
            }
            androidPlatform[i] = NearestPlatform(androids[i].transform.position.x);
            androidOffset[i] = androids[i].transform.position.x - originalCenter[androidPlatform[i]];
            androidOffsetY[i] = androids[i].transform.position.y - originalY[androidPlatform[i]];
        }

        int goalPlatformIndex = count - 1;
        float goalOffset = goal.position.x - originalCenter[goalPlatformIndex];
        float goalOffsetY = goal.position.y - originalY[goalPlatformIndex];

        // Candidate buffers reused per attempt.
        float[] newWidth = new float[count];
        float[] newCenter = new float[count];
        float[] newLeftEdge = new float[count];
        float[] newRightEdge = new float[count];
        float[] newGap = new float[count];
        float[] newY = new float[count];
        float[] coinX = new float[coins.Length];
        float[] coinY = new float[coins.Length];

        // Compute → validate the post-collect runway rule → only apply a candidate that passes.
        // A derived seed is used per retry so a rejected layout produces a *different* one, while
        // R (same original seed) always replays the identical accepted layout.
        int usedSeed = seed;
        int usedProfile = 0;
        bool accepted = false;
        for (int attempt = 0; attempt < MaxGenerateAttempts; attempt++)
        {
            int attemptSeed = attempt == 0 ? seed : unchecked(seed * 1103515245 + 12345 + attempt * 7919);
            var random = new System.Random(attemptSeed);
            int profile = random.Next(0, 3);

            for (int i = 0; i < count; i++)
            {
                float extraWidth = Range(random, 0.35f, i == 2 || i == 3 ? 1.6f : 1.05f);
                newWidth[i] = originalWidth[i] + extraWidth;
            }
            newCenter[0] = originalCenter[0];
            newLeftEdge[0] = newCenter[0] - newWidth[0] * 0.5f;
            newRightEdge[0] = newLeftEdge[0] + newWidth[0];
            newGap[0] = 0f;
            newY[0] = originalY[0];
            for (int i = 1; i < count; i++)
            {
                newGap[i] = Range(random, 2.4f, 3.0f);
                newLeftEdge[i] = newRightEdge[i - 1] + newGap[i];
                newCenter[i] = newLeftEdge[i] + newWidth[i] * 0.5f;
                newRightEdge[i] = newLeftEdge[i] + newWidth[i];
                // Gentle terrain: each deck stays flat; only its height vs the previous deck
                // varies. Rises are capped tighter than drops so a rise never compounds with a
                // jump-coin + gap, and the walk is clamped to a small absolute band.
                newY[i] = Mathf.Clamp(newY[i - 1] + Range(random, -0.42f, 0.28f), -0.5f, 0.5f);
            }

            bool valid = true;
            for (int i = 0; i < coins.Length && valid; i++)
            {
                if (coins[i] == null)
                {
                    continue;
                }
                int p = coinPlatform[i];

                if (coinIsGapArc[i])
                {
                    // Pattern B: sits at the apex of the jump across the gap, between this deck
                    // and the next — never treated as "still on this platform". The next deck
                    // must have generous landing room for this to be safe.
                    if (newWidth[p + 1] < MinArcLandingWidth)
                    {
                        valid = false;
                        break;
                    }
                    coinX[i] = (newRightEdge[p] + newLeftEdge[p + 1]) * 0.5f;
                    coinY[i] = Mathf.Max(newY[p], newY[p + 1]) + coinOffsetY[i];
                    continue;
                }

                // Pattern A (high coins) / low coins: clamp well clear of both edges.
                float rightRunway = coinIsHigh[i] ? SameDeckHighCoinRunway : LowCoinRightMargin;
                float leftBound = newLeftEdge[p] + CoinLeftMargin;
                float rightBound = newRightEdge[p] - rightRunway;
                float jitterRange = coinIsHigh[i] ? 0.08f : 0.12f;
                float desired = newCenter[p] + coinOffset[i] + Range(random, -jitterRange, jitterRange);
                if (rightBound < leftBound)
                {
                    // Deck cannot hold this pickup with a safe runway → reject this layout.
                    valid = false;
                    break;
                }
                coinX[i] = Mathf.Clamp(desired, leftBound, rightBound);
                coinY[i] = newY[p] + coinOffsetY[i];
            }

            if (valid)
            {
                usedSeed = attemptSeed;
                usedProfile = profile;
                accepted = true;
                // Consume the android jitter draws so the accepted stream stays deterministic.
                for (int i = 0; i < androids.Length; i++)
                {
                    if (androids[i] == null)
                    {
                        continue;
                    }
                    float aX = newCenter[androidPlatform[i]] + androidOffset[i] + Range(random, -0.10f, 0.10f);
                    androids[i].transform.position = new Vector3(
                        aX, newY[androidPlatform[i]] + androidOffsetY[i], androids[i].transform.position.z);
                    DemoAndroidPatrol patrol = androids[i].GetComponent<DemoAndroidPatrol>();
                    if (patrol != null)
                    {
                        patrol.enabled = false;
                    }
                }
                break;
            }
        }

        if (!accepted)
        {
            // Extremely unlikely on these decks (they are wide enough that the clamp always
            // fits). If it ever happens, keep the last candidate: the clamp already guarantees
            // the runway, so the layout is still completable.
            for (int i = 0; i < androids.Length; i++)
            {
                if (androids[i] == null)
                {
                    continue;
                }
                androids[i].transform.position = new Vector3(
                    newCenter[androidPlatform[i]] + androidOffset[i],
                    newY[androidPlatform[i]] + androidOffsetY[i],
                    androids[i].transform.position.z);
                DemoAndroidPatrol patrol = androids[i].GetComponent<DemoAndroidPatrol>();
                if (patrol != null)
                {
                    patrol.enabled = false;
                }
            }
        }

        // Apply the accepted candidate to the actual transforms.
        for (int i = 0; i < count; i++)
        {
            Transform platform = safePlatforms[i];
            if (platform == null)
            {
                continue;
            }
            platform.position = new Vector3(newCenter[i], newY[i], platform.position.z);
            Vector3 scale = platform.localScale;
            scale.x = newWidth[i];
            platform.localScale = scale;
        }
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i] == null)
            {
                continue;
            }
            coins[i].transform.position = new Vector3(coinX[i], coinY[i], coins[i].transform.position.z);
        }
        goal.position = new Vector3(
            newCenter[goalPlatformIndex] + goalOffset,
            newY[goalPlatformIndex] + goalOffsetY,
            goal.position.z);

        ShiftLandingGates(originalLeftEdge, newLeftEdge);

        controller?.Configure(
            7,
            "FINAL_ScoreMaxOA_FinalLongChallenge_BEST_200k · ObjectAware · 111 observações",
            $"Random conservador · seed {seed} · perfil {usedProfile + 1} · 7 powercells · 2 Androids");
        Physics2D.SyncTransforms();
        Debug.Log(
            $"[RANDOM MAXSCORE] Generated seed={seed} (layout seed={usedSeed}), profile={usedProfile + 1}, " +
            $"length={goal.position.x:F1}m, coins={coins.Length}, androids={androids.Length}.");
    }

    // Platform indices 1/2/3 are exactly where the agent's hardcoded landing-gate X thresholds
    // (finalLong*LandingGateX) apply — each gate sits ~0.5m past that platform's original left
    // edge. Shifting them by the same delta as the platform keeps the agent's "have I landed
    // yet" checks aligned with the new (wider-gapped) layout instead of firing early/late.
    private void ShiftLandingGates(float[] originalLeftEdge, float[] newLeftEdge)
    {
        EdgeRunnerAgentV5ScoreMaxObjectAware agent =
            FindAnyObjectByType<EdgeRunnerAgentV5ScoreMaxObjectAware>();
        if (agent == null)
        {
            return;
        }

        ShiftGate(agent, HighCoin01GateField, originalLeftEdge, newLeftEdge, 1);
        ShiftGate(agent, Android01GateField, originalLeftEdge, newLeftEdge, 2);
        ShiftGate(agent, Android02GateField, originalLeftEdge, newLeftEdge, 3);
    }

    private static void ShiftGate(
        EdgeRunnerAgentV5ScoreMaxObjectAware agent,
        FieldInfo field,
        float[] originalLeftEdge,
        float[] newLeftEdge,
        int platformIndex)
    {
        if (field == null || platformIndex >= originalLeftEdge.Length)
        {
            return;
        }
        float originalValue = (float)field.GetValue(agent);
        float buffer = originalValue - originalLeftEdge[platformIndex];
        field.SetValue(agent, newLeftEdge[platformIndex] + buffer);
    }

    private static int CompareByName(Component left, Component right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.CompareOrdinal(leftName, rightName);
    }

    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
