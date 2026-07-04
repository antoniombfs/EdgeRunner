using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class FinalDemoRandomSpeedRun : MonoBehaviour
{
    public const string SceneName = "ER_FinalDemo_SpeedRun_Random";

    private static int currentSeed;
    private static bool hasSeed;

    [SerializeField] private Transform[] platforms;
    [SerializeField] private Transform[] neonStrips;
    [SerializeField] private GameObject[] androids;
    [SerializeField] private FinalDemoRandomPatrol[] patrols;
    [SerializeField] private Transform goal;
    [SerializeField] private FinalDemoController controller;
    [SerializeField] private FinalDemoVisualCollectible[] powerCells;

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
        Transform[] stripTransforms,
        GameObject[] obstacleAndroids,
        FinalDemoRandomPatrol[] obstaclePatrols,
        Transform goalTransform,
        FinalDemoController levelController,
        FinalDemoVisualCollectible[] collectibles)
    {
        platforms = platformTransforms;
        neonStrips = stripTransforms;
        androids = obstacleAndroids;
        patrols = obstaclePatrols;
        goal = goalTransform;
        controller = levelController;
        powerCells = collectibles;
    }

    private void Awake()
    {
        if (!hasSeed)
        {
            RequestNewSeed();
        }
        Generate(currentSeed);
    }

    private void Generate(int seed)
    {
        if (platforms == null || platforms.Length != 7 || goal == null)
        {
            Debug.LogError("[RANDOM SPEEDRUN] Scene references are incomplete.", this);
            return;
        }

        var random = new System.Random(seed);
        float[] widths =
        {
            20f,
            Range(random, 38f, 44f),
            Range(random, 14f, 18f),
            Range(random, 14f, 18f),
            Range(random, 14f, 18f),
            Range(random, 24f, 30f),
            Range(random, 18f, 22f)
        };

        // Heights and gaps are generated together so a bigger vertical change never lands on
        // the tightest gap, and any "big" step is always followed by a forced flat recovery
        // step before the next one — this is what guarantees no Gap-into-Gap without recovery.
        // Bands stay inside/near the range validated for this exact demo model
        // (BuildER_V5_SpeedRunOAFinalDemoScene.cs uses 2.0-2.4m): flat landings get the most
        // visible gaps (occasionally near 3.0 when the arriving deck is wide), moderate steps
        // stay in the validated 2.0-2.4m band, and only a "big" height change (paired with the
        // forced recovery right after it) gets a smaller, more conservative gap.
        float[] tops = new float[7];
        float[] gaps = new float[7];
        tops[0] = 0f;
        tops[1] = 0f; // Android 1 deck: always flat, untouched.
        tops[2] = 0f; // Post-android recovery: always flat.
        gaps[1] = Range(random, 2.3f, widths[1] >= 20f ? 3.0f : 2.9f);
        gaps[2] = Range(random, 2.3f, widths[2] >= 16f ? 2.9f : 2.7f);

        bool previousWasBigChange = false;
        for (int i = 3; i < 7; i++)
        {
            float delta;
            if (i == 5)
            {
                // Android 2 deck: keep its approach/exit calm regardless of what came before.
                delta = Range(random, -0.3f, 0.3f);
            }
            else if (previousWasBigChange)
            {
                // Forced calm recovery right after a big change.
                delta = Range(random, -0.25f, 0.25f);
            }
            else
            {
                // Guarantee a visible rise/drop instead of occasionally landing near-flat by
                // pure chance — this is what keeps the level from reading as a straight line.
                float magnitude = Range(random, 0.25f, 0.95f);
                delta = random.Next(0, 2) == 0 ? -magnitude : magnitude;
            }
            tops[i] = Mathf.Clamp(tops[i - 1] + delta, -0.2f, 1.4f);
            bool bigChange = Mathf.Abs(delta) > 0.5f;
            bool flatChange = Mathf.Abs(delta) <= 0.3f;
            float wideLandingCeiling = widths[i] >= 16f ? 3.0f : 2.9f;
            gaps[i] = bigChange
                ? Range(random, 1.9f, 2.3f)
                : flatChange
                    ? Range(random, 2.3f, wideLandingCeiling)
                    : Range(random, 2.1f, 2.5f);
            previousWasBigChange = bigChange;
        }

        float previousRight = -2f;
        for (int i = 0; i < platforms.Length; i++)
        {
            float gap = i == 0 ? 0f : gaps[i];
            float left = i == 0 ? -2f : previousRight + gap;
            float center = left + widths[i] * 0.5f;
            Transform platform = platforms[i];
            platform.position = new Vector3(center, tops[i] - 0.22f, platform.position.z);
            platform.localScale = new Vector3(widths[i], 0.44f, 1f);

            if (neonStrips != null && i < neonStrips.Length && neonStrips[i] != null)
            {
                neonStrips[i].position = new Vector3(center, tops[i] + 0.055f, neonStrips[i].position.z);
                neonStrips[i].localScale = new Vector3(widths[i] - 0.25f, 0.09f, 1f);
            }
            previousRight = left + widths[i];
        }

        int androidCount = random.Next(0, 3);
        ConfigureAndroid(0, androidCount >= 1, 1, 2f,
            Range(random, 0.30f, 0.42f), Range(random, 0.9f, 1.2f), tops, seed);
        ConfigureAndroid(1, androidCount >= 2, 5, 0f,
            Range(random, 0.45f, 0.60f), Range(random, 1.2f, 1.5f), tops, seed + 17);

        float goalX = platforms[6].position.x + widths[6] * 0.22f;
        goal.position = new Vector3(goalX, tops[6] + 1.2f, goal.position.z);

        if (powerCells != null)
        {
            int[] safeIndices = { 2, 4, 6 };
            for (int i = 0; i < powerCells.Length; i++)
            {
                int platformIndex = safeIndices[i % safeIndices.Length];
                // Every other cell hovers at the apex of the jump into that deck instead of
                // sitting flat on it — a visual-only "collect trail" through the gap arcs.
                // Purely cosmetic (Ignore Raycast layer): does not affect the agent.
                if (platformIndex > 0 && i % 2 == 1)
                {
                    int previousIndex = platformIndex - 1;
                    float leftEdge = platforms[previousIndex].position.x + widths[previousIndex] * 0.5f;
                    float rightEdge = platforms[platformIndex].position.x - widths[platformIndex] * 0.5f;
                    float apexY = Mathf.Max(tops[previousIndex], tops[platformIndex]) + 1.9f;
                    powerCells[i].transform.position =
                        new Vector3((leftEdge + rightEdge) * 0.5f, apexY, 0.8f);
                }
                else
                {
                    powerCells[i].transform.position = new Vector3(
                        platforms[platformIndex].position.x,
                        tops[platformIndex] + 1.25f,
                        0.8f);
                }
            }
        }

        controller?.Configure(
            6,
            "FINAL_SpeedRunOA_FinalDemo03_207k_67obs · ObstacleAware · 67 observações",
            $"Random conservador · seed {seed} · {androidCount} Android(s)");
        Physics2D.SyncTransforms();
        Debug.Log($"[RANDOM SPEEDRUN] Generated seed={seed}, length={goalX:F1}m, androids={androidCount}.");
    }

    private void ConfigureAndroid(
        int androidIndex,
        bool enabled,
        int platformIndex,
        float xOffset,
        float speed,
        float distance,
        float[] tops,
        int seed)
    {
        if (androids == null || patrols == null || androidIndex >= androids.Length ||
            androidIndex >= patrols.Length || androids[androidIndex] == null)
        {
            return;
        }

        GameObject android = androids[androidIndex];
        android.SetActive(enabled);
        if (!enabled)
        {
            return;
        }

        Rigidbody2D body = android.GetComponent<Rigidbody2D>();
        Collider2D obstacleCollider = android.GetComponent<Collider2D>();
        Vector3 position = new Vector3(
            platforms[platformIndex].position.x + xOffset,
            tops[platformIndex] + 1f,
            android.transform.position.z);
        android.transform.position = position;
        if (body != null)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
        }
        Physics2D.SyncTransforms();
        if (obstacleCollider != null)
        {
            position.y += tops[platformIndex] + 0.01f - obstacleCollider.bounds.min.y;
            android.transform.position = position;
            if (body != null)
            {
                body.position = position;
            }
        }
        patrols[androidIndex]?.Configure(speed, distance, seed);
    }

    private static float Range(System.Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}

[RequireComponent(typeof(Rigidbody2D))]
public sealed class FinalDemoRandomPatrol : MonoBehaviour, IEdgeRunnerResettable
{
    private Rigidbody2D body;
    private Vector2 center;
    private float speed;
    private float distance;
    private int direction = 1;

    public void Configure(float patrolSpeed, float patrolDistance, int seed)
    {
        body = GetComponent<Rigidbody2D>();
        center = body != null ? body.position : (Vector2)transform.position;
        speed = Mathf.Max(0f, patrolSpeed);
        distance = Mathf.Clamp(patrolDistance, 0.1f, 1f);
        direction = (seed & 1) == 0 ? 1 : -1;
    }

    private void FixedUpdate()
    {
        if (body == null || speed <= 0f)
        {
            return;
        }

        float half = distance * 0.5f;
        Vector2 next = body.position + Vector2.right * direction * speed * Time.fixedDeltaTime;
        if (next.x >= center.x + half)
        {
            next.x = center.x + half;
            direction = -1;
        }
        else if (next.x <= center.x - half)
        {
            next.x = center.x - half;
            direction = 1;
        }
        body.MovePosition(next);
    }

    public void ResetForNewRun()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }
        if (body != null)
        {
            body.position = center;
            body.linearVelocity = Vector2.zero;
        }
    }
}
