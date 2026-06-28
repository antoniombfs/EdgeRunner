using UnityEngine;

public class ScoreMaxOAMixedRandomWarmupRandomizer : MonoBehaviour
{
    [Header("Ordered Objectives")]
    [SerializeField] private ScoreAttackCoin lowCoin;
    [SerializeField] private ScoreAttackCoin highCoin;
    [SerializeField] private ScoreAttackAndroid android;
    [SerializeField] private Transform goal;

    [Header("Per-Episode X Ranges")]
    [SerializeField] private Vector2 lowCoinXRange = new Vector2(3f, 4.2f);
    [SerializeField] private Vector2 highCoinXRange = new Vector2(8.5f, 10.5f);
    [SerializeField] private Vector2 androidXRange = new Vector2(14f, 16f);
    [SerializeField] private Vector2 goalXRange = new Vector2(20f, 22f);

    [Header("Fixed Heights And Spacing")]
    [SerializeField] private float lowCoinY = 0.85f;
    [SerializeField] private float highCoinY = 2.55f;
    [SerializeField] private float androidY = 1.02f;
    [SerializeField] private float goalY = 1.2f;
    [SerializeField] private float minimumLowCoinToHighCoinDistance = 4f;
    [SerializeField] private float minimumHighCoinToAndroidDistance = 3f;

    [Header("Debug")]
    [SerializeField] private bool debugObjectAwareMixedRandomPositions = false;

    public void RandomizeEpisode()
    {
        if (lowCoin == null || highCoin == null || android == null || goal == null)
        {
            Debug.LogError(
                "[OBJECT AWARE MIXED RANDOM] Missing LowCoin, HighCoin, Android, or Goal reference.",
                this);
            return;
        }

        float lowCoinX = RandomInRange(lowCoinXRange);
        float highCoinMinimumX = Mathf.Max(
            Mathf.Min(highCoinXRange.x, highCoinXRange.y),
            lowCoinX + minimumLowCoinToHighCoinDistance);
        float highCoinMaximumX = Mathf.Max(highCoinXRange.x, highCoinXRange.y);

        if (highCoinMinimumX > highCoinMaximumX)
        {
            lowCoinX = Mathf.Min(
                lowCoinX,
                highCoinMaximumX - minimumLowCoinToHighCoinDistance);
            highCoinMinimumX = Mathf.Max(
                Mathf.Min(highCoinXRange.x, highCoinXRange.y),
                lowCoinX + minimumLowCoinToHighCoinDistance);
        }

        float highCoinX = Random.Range(highCoinMinimumX, highCoinMaximumX);
        float androidMinimumX = Mathf.Max(
            Mathf.Min(androidXRange.x, androidXRange.y),
            highCoinX + minimumHighCoinToAndroidDistance);
        float androidMaximumX = Mathf.Max(androidXRange.x, androidXRange.y);

        if (androidMinimumX > androidMaximumX)
        {
            highCoinX = Mathf.Min(
                highCoinX,
                androidMaximumX - minimumHighCoinToAndroidDistance);
            androidMinimumX = Mathf.Max(
                Mathf.Min(androidXRange.x, androidXRange.y),
                highCoinX + minimumHighCoinToAndroidDistance);
        }

        float androidX = Random.Range(androidMinimumX, androidMaximumX);
        float goalMinimumX = Mathf.Max(
            Mathf.Min(goalXRange.x, goalXRange.y),
            androidX + 0.1f);
        float goalMaximumX = Mathf.Max(goalXRange.x, goalXRange.y);
        float goalX = Random.Range(Mathf.Min(goalMinimumX, goalMaximumX), goalMaximumX);

        lowCoin.ResetForNewEpisode(true, new Vector3(lowCoinX, lowCoinY, 0f));
        highCoin.ResetForNewEpisode(true, new Vector3(highCoinX, highCoinY, 0f));
        android.ResetForNewEpisode(true, new Vector3(androidX, androidY, 0f));
        goal.position = new Vector3(goalX, goalY, goal.position.z);

        if (debugObjectAwareMixedRandomPositions)
        {
            float lowToHighDistance = highCoinX - lowCoinX;
            float highToAndroidDistance = androidX - highCoinX;
            Debug.Log(
                $"[OBJECT AWARE MIXED RANDOM] LowCoin x={lowCoinX:F2}, " +
                $"HighCoin x={highCoinX:F2}, Android x={androidX:F2}, Goal x={goalX:F2}, " +
                $"LowCoin->HighCoin={lowToHighDistance:F2}, " +
                $"HighCoin->Android={highToAndroidDistance:F2}",
                this);
        }
    }

    private static float RandomInRange(Vector2 range)
    {
        return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }

    private void OnValidate()
    {
        minimumLowCoinToHighCoinDistance = Mathf.Max(
            4f,
            minimumLowCoinToHighCoinDistance);
        minimumHighCoinToAndroidDistance = Mathf.Max(
            3f,
            minimumHighCoinToAndroidDistance);
    }
}
