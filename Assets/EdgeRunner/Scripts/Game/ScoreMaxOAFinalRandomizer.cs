using System.Text;
using UnityEngine;

public class ScoreMaxOAFinalRandomizer : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private ScoreAttackManager manager;
    [SerializeField] private ScoreAttackCoin[] lowCoins;
    [SerializeField] private ScoreAttackCoin[] highCoins;
    [SerializeField] private ScoreAttackAndroid android;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform startPlatform;
    [SerializeField] private Transform lowPlatform;
    [SerializeField] private Transform highRecoveryPlatform;
    [SerializeField] private Transform finalPlatform;

    [Header("Controlled Runtime Layout")]
    [SerializeField] private Vector2 firstGapWidthRange = new Vector2(1.5f, 2.2f);
    [SerializeField] private Vector2 secondGapWidthRange = new Vector2(2f, 2.8f);
    [SerializeField] private float startPlatformLeftX = -2f;
    [SerializeField] private float startPlatformWidth = 15f;
    [SerializeField] private float lowPlatformWidth = 6f;
    [SerializeField] private float highRecoveryPlatformWidth = 8f;
    [SerializeField] private float finalPlatformWidth = 10f;
    [SerializeField] private float finalPlatformOverlap = 0.5f;
    [SerializeField] private float platformCenterY = -0.2f;
    [SerializeField] private float platformHeight = 0.4f;

    [Header("Objective Placement")]
    [SerializeField] private float lowCoinY = 0.85f;
    [SerializeField] private float highCoinY = 2.55f;
    [SerializeField] private float androidY = 1.02f;
    [SerializeField] private float goalY = 1.2f;
    [SerializeField] private float objectiveXJitter = 0.25f;
    [SerializeField] private float minimumLowCoinToHighCoinDistance = 4f;
    [SerializeField] private float minimumHighCoinToAndroidDistance = 3f;
    [SerializeField] private float minimumAndroidToGoalDistance = 3f;

    [Header("Safe Initial Low Coin Zone")]
    [SerializeField] private float playerStartX = 0f;
    [SerializeField] private Vector2 firstLowCoinXRange = new Vector2(4f, 6f);
    [SerializeField] private Vector2 secondLowCoinXRange = new Vector2(8f, 10f);
    [SerializeField] private float minFlatRunBeforeLowCoin = 4f;
    [SerializeField] private float minFlatRunAfterLowCoin = 2f;
    [SerializeField] private float minLowCoinDistanceFromGapEdge = 3f;
    [SerializeField] private float minLowCoinDistanceFromLandingZone = 3f;
    [SerializeField] private bool firstLowCoinSafe = true;

    [Header("Debug")]
    [SerializeField] private bool debugObjectAwareFinalRandomPositions = false;

    public void RandomizeEpisode()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "[OBJECT AWARE FINAL RANDOM] Missing manager, objectives, or platform references.",
                this);
            return;
        }

        int activeLowCount = Random.Range(1, Mathf.Min(2, lowCoins.Length) + 1);
        int activeHighCount = Random.Range(1, Mathf.Min(2, highCoins.Length) + 1);
        float firstGapWidth = RandomInRange(firstGapWidthRange);
        float secondGapWidth = RandomInRange(secondGapWidthRange);

        float startRightX = startPlatformLeftX + startPlatformWidth;
        float lowLeftX = startRightX + firstGapWidth;
        float lowRightX = lowLeftX + lowPlatformWidth;
        float highLeftX = lowRightX + secondGapWidth;
        float highRightX = highLeftX + highRecoveryPlatformWidth;
        float finalLeftX = highRightX - finalPlatformOverlap;
        float finalRightX = finalLeftX + finalPlatformWidth;

        PlacePlatform(startPlatform, startPlatformLeftX, startPlatformWidth);
        PlacePlatform(lowPlatform, lowLeftX, lowPlatformWidth);
        PlacePlatform(highRecoveryPlatform, highLeftX, highRecoveryPlatformWidth);
        PlacePlatform(finalPlatform, finalLeftX, finalPlatformWidth);

        // Every low coin stays on the uninterrupted initial platform. The first
        // gap only begins after the complete safe-flat zone, so no low coin can
        // become part of a gap jump or post-gap landing trajectory.
        float safeFlatZoneMinX = Mathf.Max(
            playerStartX + minFlatRunBeforeLowCoin,
            startPlatformLeftX + minLowCoinDistanceFromLandingZone);
        float safeFlatZoneMaxX = startRightX - Mathf.Max(
            minFlatRunAfterLowCoin,
            minLowCoinDistanceFromGapEdge);
        float[] lowXs =
        {
            RandomInClampedRange(firstLowCoinXRange, safeFlatZoneMinX, safeFlatZoneMaxX),
            RandomInClampedRange(secondLowCoinXRange, safeFlatZoneMinX, safeFlatZoneMaxX)
        };
        lowXs[1] = Mathf.Clamp(
            Mathf.Max(lowXs[1], lowXs[0] + 2f),
            safeFlatZoneMinX,
            safeFlatZoneMaxX);
        float lastLowX = lowXs[activeLowCount - 1];

        float[] highXs =
        {
            highLeftX + 1.8f + RandomJitter(),
            highLeftX + 5.8f + RandomJitter()
        };
        highXs[0] = Mathf.Max(
            highXs[0],
            lastLowX + minimumLowCoinToHighCoinDistance);
        highXs[1] = Mathf.Max(highXs[1], highXs[0] + 3f);
        float lastHighX = highXs[activeHighCount - 1];

        float androidX = Mathf.Max(
            finalLeftX + 4.5f + RandomJitter(),
            lastHighX + minimumHighCoinToAndroidDistance);
        float goalX = Mathf.Max(
            finalRightX - 1.5f,
            androidX + minimumAndroidToGoalDistance);

        ResetCoins(lowCoins, activeLowCount, lowXs, lowCoinY);
        ResetCoins(highCoins, activeHighCount, highXs, highCoinY);
        android.ResetForNewEpisode(true, new Vector3(androidX, androidY, 0f));
        goal.position = new Vector3(goalX, goalY, goal.position.z);
        manager.OverrideActiveObjectiveCountsForRuntimeLayout(
            activeLowCount + activeHighCount,
            1);

        if (debugObjectAwareFinalRandomPositions)
        {
            bool firstLowCoinSafeThisEpisode =
                firstLowCoinSafe && IsSafeInitialLowCoin(lowXs[0], startRightX);
            Debug.Log(
                $"[OBJECT AWARE FINAL RANDOM] lowCoins={FormatPositions(lowXs, activeLowCount)} " +
                $"lowSafety={FormatLowSafety(lowXs, activeLowCount, startRightX)} " +
                $"safeFlatZone=[{safeFlatZoneMinX:F2},{safeFlatZoneMaxX:F2}] " +
                $"firstLowCoinSafe={firstLowCoinSafeThisEpisode} " +
                $"highCoins={FormatPositions(highXs, activeHighCount)} " +
                $"androidX={androidX:F2} goalX={goalX:F2} " +
                $"sequence={BuildSequence(activeLowCount, activeHighCount)} " +
                $"gap1={firstGapWidth:F2} gap2={secondGapWidth:F2} " +
                $"Low->High={highXs[0] - lastLowX:F2} " +
                $"High->Android={androidX - lastHighX:F2} " +
                $"Android->Goal={goalX - androidX:F2}",
                this);
        }
    }

    private bool HasRequiredReferences()
    {
        return manager != null &&
            lowCoins != null && lowCoins.Length >= 2 &&
            highCoins != null && highCoins.Length >= 2 &&
            lowCoins[0] != null && lowCoins[1] != null &&
            highCoins[0] != null && highCoins[1] != null &&
            android != null && goal != null &&
            startPlatform != null && lowPlatform != null &&
            highRecoveryPlatform != null && finalPlatform != null;
    }

    private void PlacePlatform(Transform platform, float leftX, float width)
    {
        platform.position = new Vector3(
            leftX + width * 0.5f,
            platformCenterY,
            platform.position.z);
        platform.localScale = new Vector3(width, platformHeight, platform.localScale.z);
    }

    private static void ResetCoins(
        ScoreAttackCoin[] coins,
        int activeCount,
        float[] xPositions,
        float y)
    {
        for (int i = 0; i < coins.Length; i++)
        {
            bool active = i < activeCount;
            float x = xPositions[Mathf.Min(i, xPositions.Length - 1)];
            coins[i].ResetForNewEpisode(active, new Vector3(x, y, 0f));
        }
    }

    private float RandomJitter()
    {
        return Random.Range(-objectiveXJitter, objectiveXJitter);
    }

    private static float RandomInRange(Vector2 range)
    {
        return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }

    private static float RandomInClampedRange(Vector2 range, float minimum, float maximum)
    {
        float rangeMinimum = Mathf.Clamp(Mathf.Min(range.x, range.y), minimum, maximum);
        float rangeMaximum = Mathf.Clamp(Mathf.Max(range.x, range.y), minimum, maximum);
        return Random.Range(
            Mathf.Min(rangeMinimum, rangeMaximum),
            Mathf.Max(rangeMinimum, rangeMaximum));
    }

    private bool IsSafeInitialLowCoin(float x, float startRightX)
    {
        float flatRunBefore = x - playerStartX;
        float flatRunAfter = startRightX - x;
        float gapEdgeDistance = startRightX - x;
        float previousLandingZoneClearance =
            x - (startPlatformLeftX + minLowCoinDistanceFromLandingZone);
        return flatRunBefore >= minFlatRunBeforeLowCoin &&
            flatRunAfter >= minFlatRunAfterLowCoin &&
            gapEdgeDistance >= minLowCoinDistanceFromGapEdge &&
            previousLandingZoneClearance >= 0f;
    }

    private string FormatLowSafety(float[] positions, int activeCount, float startRightX)
    {
        StringBuilder builder = new StringBuilder("[");
        for (int i = 0; i < activeCount; i++)
        {
            if (i > 0)
            {
                builder.Append(';');
            }

            float x = positions[i];
            float flatRunBefore = x - playerStartX;
            float flatRunAfter = startRightX - x;
            float gapEdgeDistance = startRightX - x;
            float previousLandingZoneClearance =
                x - (startPlatformLeftX + minLowCoinDistanceFromLandingZone);
            builder.Append(
                $"LowCoin_{i + 1:00}:x={x:F2},flatBefore={flatRunBefore:F2}," +
                $"flatAfter={flatRunAfter:F2},gapEdgeDistance={gapEdgeDistance:F2}," +
                $"previousLandingZoneClearance={previousLandingZoneClearance:F2}," +
                $"safeFlatZone={IsSafeInitialLowCoin(x, startRightX)}");
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string FormatPositions(float[] positions, int activeCount)
    {
        StringBuilder builder = new StringBuilder("[");
        for (int i = 0; i < activeCount; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(positions[i].ToString("F2"));
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string BuildSequence(int activeLowCount, int activeHighCount)
    {
        return $"LowCoinx{activeLowCount}->HighCoinx{activeHighCount}->Enemy->Goal";
    }

    private void OnValidate()
    {
        firstGapWidthRange.x = Mathf.Max(0.5f, firstGapWidthRange.x);
        firstGapWidthRange.y = Mathf.Max(firstGapWidthRange.x, firstGapWidthRange.y);
        secondGapWidthRange.x = Mathf.Max(0.5f, secondGapWidthRange.x);
        secondGapWidthRange.y = Mathf.Max(secondGapWidthRange.x, secondGapWidthRange.y);
        minFlatRunBeforeLowCoin = Mathf.Max(4f, minFlatRunBeforeLowCoin);
        minFlatRunAfterLowCoin = Mathf.Max(2f, minFlatRunAfterLowCoin);
        minLowCoinDistanceFromGapEdge = Mathf.Max(3f, minLowCoinDistanceFromGapEdge);
        minLowCoinDistanceFromLandingZone = Mathf.Max(
            3f,
            minLowCoinDistanceFromLandingZone);
        startPlatformWidth = Mathf.Max(15f, startPlatformWidth);
        lowPlatformWidth = Mathf.Max(6f, lowPlatformWidth);
        highRecoveryPlatformWidth = Mathf.Max(8f, highRecoveryPlatformWidth);
        finalPlatformWidth = Mathf.Max(10f, finalPlatformWidth);
        finalPlatformOverlap = Mathf.Clamp(finalPlatformOverlap, 0f, 2f);
        platformHeight = Mathf.Max(0.2f, platformHeight);
        objectiveXJitter = Mathf.Clamp(objectiveXJitter, 0f, 0.4f);
        minimumLowCoinToHighCoinDistance = Mathf.Max(4f, minimumLowCoinToHighCoinDistance);
        minimumHighCoinToAndroidDistance = Mathf.Max(3f, minimumHighCoinToAndroidDistance);
        minimumAndroidToGoalDistance = Mathf.Max(3f, minimumAndroidToGoalDistance);
        firstLowCoinXRange = new Vector2(
            Mathf.Clamp(firstLowCoinXRange.x, 4f, 6f),
            Mathf.Clamp(firstLowCoinXRange.y, 4f, 6f));
        secondLowCoinXRange = new Vector2(
            Mathf.Clamp(secondLowCoinXRange.x, 8f, 10f),
            Mathf.Clamp(secondLowCoinXRange.y, 8f, 10f));
        firstLowCoinSafe = true;
    }
}
