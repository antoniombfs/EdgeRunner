using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct GeneratedGapInfo
{
    public int index;
    public float startX;
    public float endX;
    public float width;
}

public class GapGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Generation Settings")]
    [SerializeField] private int minGaps = 1;
    [SerializeField] private int maxGaps = 3;

    [SerializeField] private float minPlatformWidth = 4f;
    [SerializeField] private float maxPlatformWidth = 8f;

    [SerializeField] private float minGapWidth = 1.5f;
    [SerializeField] private float maxGapWidth = 3f;

    [SerializeField] private bool generateOnStart = true;

    [Header("Layout")]
    [SerializeField] private float groundY = 0f;
    [SerializeField] private float startX = 0f;
    [SerializeField] private float agentSpawnXOffset = 1f;
    [SerializeField] private float agentSpawnYOffset = 1f;
    [SerializeField] private float goalXOffsetFromEnd = 2f;
    [SerializeField] private float goalYOffset = 1f;

    [Header("References")]
    [SerializeField] private Transform generatedRoot;

    public Vector3 AgentSpawnPosition { get; private set; }
    public Transform CurrentGoal { get; private set; }
    public IReadOnlyList<GeneratedGapInfo> CurrentGaps => currentGaps;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private readonly List<GeneratedGapInfo> currentGaps = new List<GeneratedGapInfo>();

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateEpisode();
        }
    }

    public void GenerateEpisode()
    {
        currentGaps.Clear();

        ValidateReferences();

        if (platformPrefab == null || goalPrefab == null || generatedRoot == null)
        {
            return;
        }

        ClearLevel();

        float currentX = startX;
        int numGaps = Random.Range(minGaps, maxGaps + 1);

        float firstWidth = Random.Range(minPlatformWidth, maxPlatformWidth);
        CreatePlatform(currentX, firstWidth);

        AgentSpawnPosition = new Vector3(
            currentX + agentSpawnXOffset,
            groundY + agentSpawnYOffset,
            0f
        );

        currentX += firstWidth;

        for (int i = 0; i < numGaps; i++)
        {
            float gapStartX = currentX;
            float gapWidth = Random.Range(minGapWidth, maxGapWidth);
            float gapEndX = gapStartX + gapWidth;

            currentGaps.Add(new GeneratedGapInfo
            {
                index = i,
                startX = gapStartX,
                endX = gapEndX,
                width = gapWidth
            });

            currentX = gapEndX;

            float platformWidth = Random.Range(minPlatformWidth, maxPlatformWidth);
            CreatePlatform(currentX, platformWidth);

            currentX += platformWidth;
        }

        Vector3 goalPosition = new Vector3(
            currentX - goalXOffsetFromEnd,
            groundY + goalYOffset,
            0f
        );

        GameObject goalInstance = Instantiate(
            goalPrefab,
            goalPosition,
            Quaternion.identity,
            generatedRoot
        );

        spawnedObjects.Add(goalInstance);
        CurrentGoal = goalInstance.transform;
    }

    private void CreatePlatform(float platformStartX, float width)
    {
        Vector3 platformPosition = new Vector3(
            platformStartX + width / 2f,
            groundY,
            0f
        );

        GameObject platformInstance = Instantiate(
            platformPrefab,
            platformPosition,
            Quaternion.identity,
            generatedRoot
        );

        platformInstance.transform.localScale = new Vector3(width, 1f, 1f);
        spawnedObjects.Add(platformInstance);
    }

    private void ClearLevel()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
        CurrentGoal = null;
    }

    private void ValidateReferences()
    {
        if (platformPrefab == null)
        {
            Debug.LogError("GapGenerator: platformPrefab não está atribuído.");
        }

        if (goalPrefab == null)
        {
            Debug.LogError("GapGenerator: goalPrefab não está atribuído.");
        }

        if (generatedRoot == null)
        {
            Debug.LogError("GapGenerator: generatedRoot não está atribuído.");
        }

        if (minGaps < 0)
        {
            minGaps = 0;
        }

        if (maxGaps < minGaps)
        {
            maxGaps = minGaps;
        }

        if (maxPlatformWidth < minPlatformWidth)
        {
            maxPlatformWidth = minPlatformWidth;
        }

        if (maxGapWidth < minGapWidth)
        {
            maxGapWidth = minGapWidth;
        }
    }
}
