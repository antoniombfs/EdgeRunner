using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct MixedSegmentInfo
{
    public int index;
    public string type;
    public float startX;
    public float endX;
    public float y;
    public float width;
}

[System.Serializable]
public struct MixedGapInfo
{
    public int index;
    public float startX;
    public float endX;
    public float width;
    public float startY;
    public float endY;
}

public class MixedLevelGenerator : MonoBehaviour
{
    private enum MixedSegmentKind
    {
        Flat,
        Gap,
        StepUp,
        StepDown,
        SafeDrop,
        PlatformChain
    }

    private const float HardSafeDropHeightThreshold = 0.75f;
    private const float MinimumGoalHeightOffset = 0.25f;

    [Header("References")]
    [SerializeField] private Transform levelRoot;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private PhysicsMaterial2D platformPhysicsMaterial;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = false;
    [SerializeField] private int minSegments = 6;
    [SerializeField] private int maxSegments = 9;
    [SerializeField] private float minPlatformWidth = 4.5f;
    [SerializeField] private float minLandingPlatformWidth = 4.5f;
    [SerializeField] private float maxPlatformWidth = 7f;
    [SerializeField] private float minGapWidth = 1.5f;
    [SerializeField] private float maxGapWidth = 2.4f;
    [SerializeField] private float minHeightDelta = 0.4f;
    [SerializeField] private float maxHeightDelta = 1.0f;
    [SerializeField] private float minDropHeight = 0.5f;
    [SerializeField] private float maxDropHeight = 1.2f;
    [SerializeField] private float minPlatformChainWidth = 2.5f;
    [SerializeField] private float maxPlatformChainWidth = 4.0f;
    [SerializeField] private int minPlatformChainPieces = 2;
    [SerializeField] private int maxPlatformChainPieces = 3;
    [SerializeField] private float minPlatformChainGap = 1.2f;
    [SerializeField] private float maxPlatformChainGap = 1.8f;
    [SerializeField] private int maxPlatformChainsPerEpisode = 2;
    [SerializeField] private bool preventConsecutivePlatformChains = true;
    [SerializeField] private bool avoidHardSegmentAfterPlatformChain = true;
    [SerializeField] private float verticalSafetyLimit = 1.2f;
    [SerializeField] private Vector2 agentSpawnPosition = new Vector2(1f, 1.5f);

    [Header("V5 Generation Rules")]
    [SerializeField] private bool useV5GenerationRules = false;
    [SerializeField] private bool preferVerticalVariety = true;
    [SerializeField] private float verticalSegmentChanceMultiplier = 1.25f;
    [SerializeField] private int maxConsecutiveHardSegments = 2;
    [SerializeField] private bool forceRecoveryPlatformAfterHardSegment = true;
    [SerializeField] private float minRecoveryPlatformWidth = 4.5f;
    [SerializeField] private float safeEdgeMargin = 0.75f;
    [SerializeField] private float minDistanceBetweenGaps = 0f;
    [SerializeField] private float minRunupBeforeGap = 0f;
    [SerializeField] private float minLandingAfterGap = 0f;
    [SerializeField] private float finalGoalPlatformWidth = 0f;
    [SerializeField] private float finalGoalSafeRunup = 0f;
    [SerializeField] private float goalEdgeMargin = 0.75f;
    [SerializeField] private bool useWideGoalTrigger = false;
    [SerializeField] private Vector2 wideGoalTriggerSize = new Vector2(2.5f, 6f);
    [SerializeField] private bool debugGenerationValues = false;
    [SerializeField] private bool avoidRepeatedStepUps = true;
    [SerializeField] private bool avoidRepeatedGaps = true;
    [SerializeField] private bool avoidHardGapIntoStepUp = true;
    [SerializeField] private bool allowHigherGoalPlacement = true;
    [SerializeField] private float maxGoalHeightOffset = 1.5f;

    [Header("Enabled Segment Types")]
    [SerializeField] private bool enableFlatSegment = true;
    [SerializeField] private bool enableGapSegment = true;
    [SerializeField] private bool enableStepUpSegment = true;
    [SerializeField] private bool enableStepDownSegment = true;
    [SerializeField] private bool enableSafeDropSegment = true;
    [SerializeField] private bool enablePlatformChainSegment = true;

    [Header("Layout")]
    [SerializeField] private float startX = 0f;
    [SerializeField] private float startY = 0f;
    [SerializeField] private float platformHeightScale = 1f;
    [SerializeField] private float goalXOffsetFromEnd = 2f;
    [SerializeField] private float goalYOffset = 1f;

    [Header("Diagnostics")]
    [SerializeField] private bool debugDrawSegments = false;
    [SerializeField] private bool validateGeneratedSegments = true;
    [SerializeField] private bool mergeContinuousSameHeightPlatforms = true;
    [SerializeField] private float transitionTolerance = 0.02f;
    [SerializeField] private float colliderAlignmentTolerance = 0.02f;

    public Vector2 AgentSpawnPosition { get; private set; }
    public Transform CurrentGoal { get; private set; }
    public IReadOnlyList<MixedSegmentInfo> CurrentSegments => currentSegments;
    public IReadOnlyList<MixedGapInfo> CurrentGaps => currentGaps;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private readonly List<MixedSegmentInfo> currentSegments = new List<MixedSegmentInfo>();
    private readonly List<MixedGapInfo> currentGaps = new List<MixedGapInfo>();
    private readonly List<SpawnedPlatformInfo> spawnedPlatforms = new List<SpawnedPlatformInfo>();
    private readonly List<SegmentPlatformBinding> segmentPlatformBindings = new List<SegmentPlatformBinding>();

    private int nextSegmentIndex;
    private int nextGapIndex;
    private float currentX;
    private float currentY;
    private float lastPlatformStartX;
    private float lastPlatformEndX;
    private int platformChainsGenerated;
    private MixedSegmentKind lastGeneratedSegmentKind;
    private bool hasLastGeneratedSegmentKind;
    private int consecutiveHardSegments;
    private bool pendingRecoveryPlatform;
    private float lastSafeDropHeight;
    private bool hasLastGap;
    private float lastGapEndX;
    private bool loggedHardSegmentsWarning;
    private bool loggedRecoveryWidthWarning;
    private bool loggedHardGapIntoStepUpWarning;
    private bool loggedGoalHeightWarning;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateEpisode();
        }
    }

    public void GenerateEpisode()
    {
        ResetV5RuntimeState();
        ValidateSettings();

        if (!HasRequiredReferences())
        {
            return;
        }

        ClearLevel();

        currentSegments.Clear();
        currentGaps.Clear();
        CurrentGoal = null;
        AgentSpawnPosition = agentSpawnPosition;
        nextSegmentIndex = 0;
        nextGapIndex = 0;
        currentX = startX;
        currentY = startY;
        platformChainsGenerated = 0;
        hasLastGeneratedSegmentKind = false;
        consecutiveHardSegments = 0;
        pendingRecoveryPlatform = false;
        lastSafeDropHeight = 0f;
        hasLastGap = false;
        lastGapEndX = 0f;

        int segmentCount = Random.Range(minSegments, maxSegments + 1);
        int requiredVerticalSegment = segmentCount > 2 ? Random.Range(1, segmentCount) : -1;

        CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
        RememberGeneratedSegmentKind(MixedSegmentKind.Flat);

        for (int i = 1; i < segmentCount; i++)
        {
            if (ShouldCreateRecoveryPlatform())
            {
                CreateRecoveryPlatformSegment();
                continue;
            }

            if (i == requiredVerticalSegment)
            {
                CreateRandomVerticalSegment();
                continue;
            }

            CreateRandomSegment();
        }

        if (ShouldCreateRecoveryPlatform())
        {
            CreateRecoveryPlatformSegment();
        }

        CreateFinalGoalPlatformSegment();
        CreateGoalSegment();
        ValidateGeneratedLayout();
    }

    private void CreateRandomSegment()
    {
        CreateSegment(PickRandomSegmentKind(false));
    }

    private void CreateRandomVerticalSegment()
    {
        CreateSegment(PickRandomSegmentKind(true));
    }

    private void CreateSegment(MixedSegmentKind segmentKind)
    {
        MixedSegmentKind generatedSegmentKind;

        switch (segmentKind)
        {
            case MixedSegmentKind.Gap:
                generatedSegmentKind = CreateGapSegment();
                break;
            case MixedSegmentKind.StepUp:
                generatedSegmentKind = CreateStepUpSegment();
                break;
            case MixedSegmentKind.StepDown:
                generatedSegmentKind = CreateStepDownSegment();
                break;
            case MixedSegmentKind.SafeDrop:
                generatedSegmentKind = CreateSafeDropSegment();
                break;
            case MixedSegmentKind.PlatformChain:
                generatedSegmentKind = CreatePlatformChainSegment();
                break;
            default:
                CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
                generatedSegmentKind = MixedSegmentKind.Flat;
                break;
        }

        RememberGeneratedSegmentKind(generatedSegmentKind);
    }

    private bool ShouldCreateRecoveryPlatform()
    {
        return useV5GenerationRules &&
               forceRecoveryPlatformAfterHardSegment &&
               pendingRecoveryPlatform;
    }

    private void CreateRecoveryPlatformSegment()
    {
        CreatePlatformSegment("FlatSegment", RandomRecoveryPlatformWidth(), currentY);
        RememberGeneratedSegmentKind(MixedSegmentKind.Flat);
    }

    private bool ShouldUseV5VerticalVarietyBias()
    {
        if (!useV5GenerationRules || !preferVerticalVariety)
        {
            return false;
        }

        float extraVerticalChance = Mathf.Clamp01(verticalSegmentChanceMultiplier - 1f);
        return Random.value < extraVerticalChance;
    }

    private MixedSegmentKind PickRandomSegmentKind(bool preferVerticalSegment)
    {
        List<MixedSegmentKind> enabledKinds = new List<MixedSegmentKind>();

        if (preferVerticalSegment || ShouldUseV5VerticalVarietyBias())
        {
            AddEnabledVerticalSegmentKinds(enabledKinds);
        }

        enabledKinds = FilterSegmentKinds(enabledKinds, true);

        if (enabledKinds.Count == 0 && preferVerticalSegment)
        {
            AddEnabledSegmentKinds(enabledKinds);
            enabledKinds = FilterSegmentKinds(enabledKinds, true);
        }

        if (enabledKinds.Count == 0 && preferVerticalSegment)
        {
            AddEnabledVerticalSegmentKinds(enabledKinds);
            enabledKinds = FilterSegmentKinds(enabledKinds, false);
        }

        if (enabledKinds.Count == 0)
        {
            AddEnabledSegmentKinds(enabledKinds);
            enabledKinds = FilterSegmentKinds(enabledKinds, useV5GenerationRules);
        }

        if (enabledKinds.Count == 0 && useV5GenerationRules)
        {
            AddEnabledSegmentKinds(enabledKinds);
            enabledKinds = FilterSegmentKinds(enabledKinds, false);
        }

        if (enabledKinds.Count == 0)
        {
            return MixedSegmentKind.Flat;
        }

        return enabledKinds[Random.Range(0, enabledKinds.Count)];
    }

    private List<MixedSegmentKind> FilterSegmentKinds(List<MixedSegmentKind> source, bool applySoftRules)
    {
        List<MixedSegmentKind> filteredKinds = new List<MixedSegmentKind>();

        for (int i = 0; i < source.Count; i++)
        {
            MixedSegmentKind segmentKind = source[i];

            if (!CanUseSegmentKind(segmentKind))
            {
                continue;
            }

            if (applySoftRules && ShouldAvoidSegmentKind(segmentKind))
            {
                continue;
            }

            filteredKinds.Add(segmentKind);
        }

        return filteredKinds;
    }

    private bool CanUseSegmentKind(MixedSegmentKind segmentKind)
    {
        if (useV5GenerationRules &&
            consecutiveHardSegments >= maxConsecutiveHardSegments &&
            IsCandidateHardSegmentKind(segmentKind))
        {
            LogV5GenerationWarningOnce(
                ref loggedHardSegmentsWarning,
                "max consecutive hard segments reached; forcing a recovery/easier segment."
            );
            return false;
        }

        if (segmentKind != MixedSegmentKind.PlatformChain)
        {
            return true;
        }

        if (!enablePlatformChainSegment || platformChainsGenerated >= maxPlatformChainsPerEpisode)
        {
            return false;
        }

        return !preventConsecutivePlatformChains ||
               !hasLastGeneratedSegmentKind ||
               lastGeneratedSegmentKind != MixedSegmentKind.PlatformChain;
    }

    private bool ShouldAvoidSegmentKind(MixedSegmentKind segmentKind)
    {
        if (!hasLastGeneratedSegmentKind)
        {
            return false;
        }

        if (useV5GenerationRules)
        {
            if (avoidRepeatedStepUps &&
                lastGeneratedSegmentKind == MixedSegmentKind.StepUp &&
                segmentKind == MixedSegmentKind.StepUp)
            {
                return true;
            }

            if (avoidRepeatedGaps &&
                lastGeneratedSegmentKind == MixedSegmentKind.Gap &&
                segmentKind == MixedSegmentKind.Gap)
            {
                return true;
            }

            if (avoidHardGapIntoStepUp &&
                lastGeneratedSegmentKind == MixedSegmentKind.Gap &&
                segmentKind == MixedSegmentKind.StepUp)
            {
                LogV5GenerationWarningOnce(
                    ref loggedHardGapIntoStepUpWarning,
                    "avoided GapSegment -> StepUpSegment sequence under V5 generation rules."
                );
                return true;
            }
        }

        if (!avoidHardSegmentAfterPlatformChain ||
            lastGeneratedSegmentKind != MixedSegmentKind.PlatformChain)
        {
            return false;
        }

        return segmentKind == MixedSegmentKind.StepUp ||
               segmentKind == MixedSegmentKind.PlatformChain;
    }

    private void AddEnabledSegmentKinds(List<MixedSegmentKind> enabledKinds)
    {
        if (enableFlatSegment)
        {
            enabledKinds.Add(MixedSegmentKind.Flat);
        }

        if (enableGapSegment)
        {
            enabledKinds.Add(MixedSegmentKind.Gap);
        }

        AddEnabledVerticalSegmentKinds(enabledKinds);

        if (enablePlatformChainSegment)
        {
            enabledKinds.Add(MixedSegmentKind.PlatformChain);
        }
    }

    private void AddEnabledVerticalSegmentKinds(List<MixedSegmentKind> enabledKinds)
    {
        if (enableStepUpSegment)
        {
            enabledKinds.Add(MixedSegmentKind.StepUp);
        }

        if (enableStepDownSegment)
        {
            enabledKinds.Add(MixedSegmentKind.StepDown);
        }

        if (enableSafeDropSegment)
        {
            enabledKinds.Add(MixedSegmentKind.SafeDrop);
        }
    }

    private MixedSegmentKind CreateGapSegment()
    {
        float gapWidth = RandomGapWidth();
        if (!TryCreateGap(gapWidth, currentY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        CreatePlatformSegment("GapSegment", RandomLandingPlatformWidth(), currentY);
        return MixedSegmentKind.Gap;
    }

    private MixedSegmentKind CreateStepUpSegment()
    {
        float nextY = ClampY(currentY + Random.Range(minHeightDelta, maxHeightDelta));

        if (Mathf.Approximately(nextY, currentY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        float gapWidth = RandomModerateGapWidth(2.0f);
        if (!TryCreateGap(gapWidth, nextY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        CreatePlatformSegment("StepUpSegment", RandomLandingPlatformWidth(), nextY);
        return MixedSegmentKind.StepUp;
    }

    private MixedSegmentKind CreateStepDownSegment()
    {
        float nextY = ClampY(currentY - Random.Range(minHeightDelta, maxHeightDelta));

        if (Mathf.Approximately(nextY, currentY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        float gapWidth = RandomModerateGapWidth(2.0f);
        if (!TryCreateGap(gapWidth, nextY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        CreatePlatformSegment("StepDownSegment", RandomLandingPlatformWidth(), nextY);
        return MixedSegmentKind.StepDown;
    }

    private MixedSegmentKind CreateSafeDropSegment()
    {
        float previousY = currentY;
        float nextY = ClampY(currentY - Random.Range(minDropHeight, maxDropHeight));
        lastSafeDropHeight = Mathf.Abs(previousY - nextY);

        if (Mathf.Approximately(nextY, currentY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        float gapWidth = RandomModerateGapWidth(2.2f);
        if (!TryCreateGap(gapWidth, nextY))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        CreatePlatformSegment("SafeDropSegment", RandomLandingPlatformWidth(), nextY);
        return MixedSegmentKind.SafeDrop;
    }

    private MixedSegmentKind CreatePlatformChainSegment()
    {
        if (!CanUseSegmentKind(MixedSegmentKind.PlatformChain))
        {
            CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
            return MixedSegmentKind.Flat;
        }

        int platformCount = Random.Range(minPlatformChainPieces, maxPlatformChainPieces + 1);
        platformChainsGenerated++;

        for (int i = 0; i < platformCount; i++)
        {
            if (i > 0)
            {
                if (!TryCreateGap(RandomPlatformChainGap(), currentY))
                {
                    CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
                    continue;
                }
            }
            else if (currentSegments.Count > 0)
            {
                if (!TryCreateGap(RandomModerateGapWidth(2.0f), currentY))
                {
                    CreatePlatformSegment("FlatSegment", RandomPlatformWidth(), currentY);
                    continue;
                }
            }

            float width = RandomPlatformChainWidth();
            CreatePlatformSegment("PlatformChainSegment", width, currentY);
        }

        return MixedSegmentKind.PlatformChain;
    }

    private void CreateFinalGoalPlatformSegment()
    {
        if (!useV5GenerationRules || finalGoalPlatformWidth <= 0f)
        {
            return;
        }

        float minimumFinalWidth = Mathf.Max(
            finalGoalPlatformWidth,
            finalGoalSafeRunup + goalEdgeMargin + 0.5f
        );

        CreatePlatformSegment("FinalGoalPlatform", minimumFinalWidth, currentY);
        RememberGeneratedSegmentKind(MixedSegmentKind.Flat);
    }

    private void CreateGoalSegment()
    {
        float goalX = CalculateGoalX();
        float heightOffset = GetGoalHeightOffset();

        Vector3 goalPosition = new Vector3(goalX, currentY + heightOffset, 0f);
        GameObject goalInstance = Instantiate(goalPrefab, goalPosition, Quaternion.identity, levelRoot);
        ConfigureGoalTrigger(goalInstance, heightOffset);
        spawnedObjects.Add(goalInstance);
        CurrentGoal = goalInstance.transform;

        if (debugGenerationValues)
        {
            Debug.Log(
                $"[MIXED GEN] finalGoalPlatformWidth={finalGoalPlatformWidth:F2} " +
                $"goalX={goalX:F2} goalY={goalPosition.y:F2} " +
                $"wideGoalTrigger={useWideGoalTrigger} triggerSize={wideGoalTriggerSize}"
            );
        }

        currentSegments.Add(new MixedSegmentInfo
        {
            index = nextSegmentIndex++,
            type = "GoalSegment",
            startX = goalX,
            endX = goalX,
            y = goalPosition.y,
            width = 0f
        });
    }

    private void CreatePlatformSegment(string segmentType, float width, float y)
    {
        float platformStartX = currentX;
        float platformEndX = platformStartX + width;

        MixedSegmentInfo segment = new MixedSegmentInfo
        {
            index = nextSegmentIndex++,
            type = segmentType,
            startX = platformStartX,
            endX = platformEndX,
            y = y,
            width = width
        };

        currentSegments.Add(segment);

        int mergedPlatformIndex;
        if (TryMergeWithPreviousPhysicalPlatform(segment, out mergedPlatformIndex))
        {
            segmentPlatformBindings.Add(new SegmentPlatformBinding
            {
                segmentIndex = segment.index,
                platformIndex = mergedPlatformIndex
            });
        }
        else
        {
            Vector3 platformPosition = new Vector3(platformStartX + width * 0.5f, y, 0f);
            GameObject platformInstance = Instantiate(platformPrefab, platformPosition, Quaternion.identity, levelRoot);
            platformInstance.transform.localScale = new Vector3(width, platformHeightScale, 1f);
            SetLayerRecursively(platformInstance, LayerMask.NameToLayer("Ground"));
            ApplyPlatformPhysicsMaterial(platformInstance);
            spawnedObjects.Add(platformInstance);

            int platformIndex = spawnedPlatforms.Count;
            spawnedPlatforms.Add(new SpawnedPlatformInfo
            {
                instance = platformInstance,
                firstSegmentIndex = segment.index,
                lastSegmentIndex = segment.index,
                startX = segment.startX,
                endX = segment.endX,
                y = segment.y
            });
            segmentPlatformBindings.Add(new SegmentPlatformBinding
            {
                segmentIndex = segment.index,
                platformIndex = platformIndex
            });
        }

        currentX = platformEndX;
        currentY = y;
        lastPlatformStartX = platformStartX;
        lastPlatformEndX = platformEndX;
    }

    private bool TryCreateGap(float gapWidth, float nextY)
    {
        if (gapWidth < minGapWidth)
        {
            LogGenerationValue(
                $"skipped micro-gap width={gapWidth:F2}, minGapWidth={minGapWidth:F2}"
            );
            return false;
        }

        if (useV5GenerationRules)
        {
            float runupBeforeGap = currentX - lastPlatformStartX;
            if (runupBeforeGap < minRunupBeforeGap)
            {
                LogGenerationValue(
                    $"skipped gap because runup={runupBeforeGap:F2}, minRunupBeforeGap={minRunupBeforeGap:F2}"
                );
                return false;
            }

            if (hasLastGap)
            {
                float distanceSinceLastGap = currentX - lastGapEndX;
                if (distanceSinceLastGap < minDistanceBetweenGaps)
                {
                    LogGenerationValue(
                        $"skipped gap because distanceSinceLastGap={distanceSinceLastGap:F2}, " +
                        $"minDistanceBetweenGaps={minDistanceBetweenGaps:F2}"
                    );
                    return false;
                }
            }
        }

        CreateGap(gapWidth, nextY);
        return true;
    }

    private void CreateGap(float gapWidth, float nextY)
    {
        float gapStartX = currentX;
        float gapEndX = gapStartX + gapWidth;

        currentGaps.Add(new MixedGapInfo
        {
            index = nextGapIndex++,
            startX = gapStartX,
            endX = gapEndX,
            width = gapWidth,
            startY = currentY,
            endY = nextY
        });

        currentX = gapEndX;
        hasLastGap = true;
        lastGapEndX = gapEndX;

        LogGenerationValue(
            $"gapWidth={gapWidth:F2} startX={gapStartX:F2} endX={gapEndX:F2}"
        );
    }

    private float CalculateGoalX()
    {
        float edgeMargin = Mathf.Max(safeEdgeMargin, goalEdgeMargin);
        float minGoalX = lastPlatformStartX + edgeMargin;

        if (useV5GenerationRules && finalGoalSafeRunup > 0f)
        {
            minGoalX = Mathf.Max(minGoalX, lastPlatformStartX + finalGoalSafeRunup);
        }

        float maxGoalX = lastPlatformEndX - edgeMargin;

        if (maxGoalX < minGoalX)
        {
            return (lastPlatformStartX + lastPlatformEndX) * 0.5f;
        }

        return Mathf.Clamp(lastPlatformEndX - goalXOffsetFromEnd, minGoalX, maxGoalX);
    }

    private void ConfigureGoalTrigger(GameObject goalInstance, float heightOffset)
    {
        if (!useWideGoalTrigger || goalInstance == null)
        {
            return;
        }

        BoxCollider2D boxCollider = goalInstance.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = goalInstance.AddComponent<BoxCollider2D>();
        }

        float triggerWidth = Mathf.Max(0.1f, wideGoalTriggerSize.x);
        float triggerHeight = Mathf.Max(0.1f, wideGoalTriggerSize.y);

        boxCollider.enabled = true;
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector2(triggerWidth, triggerHeight);
        boxCollider.offset = new Vector2(0f, triggerHeight * 0.5f - heightOffset);
    }

    private void ApplyPlatformPhysicsMaterial(GameObject platformInstance)
    {
        if (platformPhysicsMaterial == null || platformInstance == null)
        {
            return;
        }

        Collider2D[] colliders = platformInstance.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].sharedMaterial = platformPhysicsMaterial;
            }
        }
    }

    private float RandomPlatformWidth()
    {
        return Random.Range(minPlatformWidth, maxPlatformWidth);
    }

    private float RandomLandingPlatformWidth()
    {
        float minimumLandingWidth = useV5GenerationRules
            ? Mathf.Max(minLandingPlatformWidth, minLandingAfterGap)
            : minLandingPlatformWidth;
        return Mathf.Max(minimumLandingWidth, RandomPlatformWidth());
    }

    private float RandomRecoveryPlatformWidth()
    {
        return Mathf.Max(minRecoveryPlatformWidth, RandomLandingPlatformWidth());
    }

    private float RandomPlatformChainWidth()
    {
        float minimumWidth = useV5GenerationRules
            ? Mathf.Max(minPlatformChainWidth, minLandingPlatformWidth)
            : minPlatformChainWidth;
        float maximumWidth = Mathf.Max(minimumWidth, maxPlatformChainWidth);
        return Random.Range(minimumWidth, maximumWidth);
    }

    private float RandomGapWidth()
    {
        return Random.Range(minGapWidth, maxGapWidth);
    }

    private float RandomModerateGapWidth(float maxRecommendedWidth)
    {
        float upperLimit = Mathf.Max(minGapWidth, Mathf.Min(maxGapWidth, maxRecommendedWidth));
        return Random.Range(minGapWidth, upperLimit);
    }

    private float RandomPlatformChainGap()
    {
        float minimumGap = useV5GenerationRules
            ? Mathf.Max(minPlatformChainGap, minGapWidth)
            : minPlatformChainGap;
        float maximumGap = Mathf.Max(minimumGap, maxPlatformChainGap);
        return Random.Range(minimumGap, maximumGap);
    }

    private float ClampY(float y)
    {
        return Mathf.Clamp(y, startY - verticalSafetyLimit, startY + verticalSafetyLimit);
    }

    private void RememberGeneratedSegmentKind(MixedSegmentKind segmentKind)
    {
        if (useV5GenerationRules)
        {
            UpdateV5GenerationState(segmentKind);
        }

        lastGeneratedSegmentKind = segmentKind;
        hasLastGeneratedSegmentKind = true;
    }

    private void UpdateV5GenerationState(MixedSegmentKind segmentKind)
    {
        if (hasLastGeneratedSegmentKind &&
            avoidHardGapIntoStepUp &&
            lastGeneratedSegmentKind == MixedSegmentKind.Gap &&
            segmentKind == MixedSegmentKind.StepUp)
        {
            LogV5GenerationWarningOnce(
                ref loggedHardGapIntoStepUpWarning,
                "generated GapSegment -> StepUpSegment because no easier candidate was available."
            );
        }

        bool hardSegment = IsGeneratedHardSegmentKind(segmentKind);

        if (hardSegment)
        {
            consecutiveHardSegments++;

            if (consecutiveHardSegments > maxConsecutiveHardSegments)
            {
                LogV5GenerationWarningOnce(
                    ref loggedHardSegmentsWarning,
                    "generated more hard segments in a row than requested; recovery platform will be forced."
                );
            }

            pendingRecoveryPlatform = forceRecoveryPlatformAfterHardSegment;
            return;
        }

        consecutiveHardSegments = 0;
        pendingRecoveryPlatform = false;
    }

    private bool IsCandidateHardSegmentKind(MixedSegmentKind segmentKind)
    {
        switch (segmentKind)
        {
            case MixedSegmentKind.Gap:
            case MixedSegmentKind.StepUp:
            case MixedSegmentKind.PlatformChain:
                return true;
            case MixedSegmentKind.SafeDrop:
                return maxDropHeight >= HardSafeDropHeightThreshold;
            default:
                return false;
        }
    }

    private bool IsGeneratedHardSegmentKind(MixedSegmentKind segmentKind)
    {
        if (segmentKind == MixedSegmentKind.SafeDrop)
        {
            return lastSafeDropHeight >= HardSafeDropHeightThreshold;
        }

        return IsCandidateHardSegmentKind(segmentKind);
    }

    private float GetGoalHeightOffset()
    {
        if (!useV5GenerationRules || !allowHigherGoalPlacement)
        {
            return goalYOffset;
        }

        float maxOffset = Mathf.Max(MinimumGoalHeightOffset, maxGoalHeightOffset);
        float variedOffset = goalYOffset + Random.Range(-maxOffset, maxOffset);
        return Mathf.Clamp(variedOffset, MinimumGoalHeightOffset, maxOffset);
    }

    private void ResetV5RuntimeState()
    {
        consecutiveHardSegments = 0;
        pendingRecoveryPlatform = false;
        lastSafeDropHeight = 0f;
        loggedHardSegmentsWarning = false;
        loggedRecoveryWidthWarning = false;
        loggedHardGapIntoStepUpWarning = false;
        loggedGoalHeightWarning = false;
    }

    private void ClearLevel()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(spawnedObjects[i]);
            }
            else
            {
                DestroyImmediate(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
        spawnedPlatforms.Clear();
        segmentPlatformBindings.Clear();
    }

    private bool TryMergeWithPreviousPhysicalPlatform(MixedSegmentInfo segment, out int platformIndex)
    {
        platformIndex = -1;

        if (!mergeContinuousSameHeightPlatforms || spawnedPlatforms.Count == 0)
        {
            return false;
        }

        float epsilon = Mathf.Max(0.0001f, transitionTolerance);
        int previousPlatformIndex = spawnedPlatforms.Count - 1;
        SpawnedPlatformInfo previousPlatform = spawnedPlatforms[previousPlatformIndex];

        if (previousPlatform.instance == null ||
            Mathf.Abs(previousPlatform.y - segment.y) > epsilon ||
            Mathf.Abs(previousPlatform.endX - segment.startX) > epsilon)
        {
            return false;
        }

        previousPlatform.endX = segment.endX;
        previousPlatform.lastSegmentIndex = segment.index;
        spawnedPlatforms[previousPlatformIndex] = previousPlatform;
        ApplyPhysicalPlatformBounds(previousPlatform);

        if (debugDrawSegments)
        {
            Debug.Log(
                "MixedLevelGenerator diagnostics: merged continuous same-height platform collider " +
                $"for segment #{segment.index} ({segment.type}) at X={segment.startX:F3}.",
                this
            );
        }

        platformIndex = previousPlatformIndex;
        return true;
    }

    private void ApplyPhysicalPlatformBounds(SpawnedPlatformInfo platformInfo)
    {
        if (platformInfo.instance == null)
        {
            return;
        }

        float width = Mathf.Max(0.0001f, platformInfo.endX - platformInfo.startX);
        platformInfo.instance.transform.position = new Vector3(
            platformInfo.startX + width * 0.5f,
            platformInfo.y,
            platformInfo.instance.transform.position.z
        );
        platformInfo.instance.transform.localScale = new Vector3(width, platformHeightScale, 1f);
    }

    private void ValidateGeneratedLayout()
    {
        if (!validateGeneratedSegments)
        {
            return;
        }

        float transitionEpsilon = Mathf.Max(0.0001f, transitionTolerance);
        float colliderEpsilon = Mathf.Max(0.0001f, colliderAlignmentTolerance);

        ValidateSegmentWidths(transitionEpsilon);
        ValidateSegmentTransitions(transitionEpsilon);
        ValidateGapRecords(transitionEpsilon);
        ValidatePlatformColliderAlignment(colliderEpsilon);
    }

    private void ValidateSegmentWidths(float epsilon)
    {
        for (int i = 0; i < currentSegments.Count; i++)
        {
            MixedSegmentInfo segment = currentSegments[i];

            if (IsMarkerSegment(segment))
            {
                continue;
            }

            if (segment.width <= 0f || segment.endX <= segment.startX + epsilon)
            {
                LogLayoutWarning(
                    $"segment #{segment.index} ({segment.type}) has non-positive/tiny width. " +
                    $"startX={segment.startX:F3}, endX={segment.endX:F3}, width={segment.width:F3}"
                );
            }
        }
    }

    private void ValidateSegmentTransitions(float epsilon)
    {
        for (int i = 1; i < currentSegments.Count; i++)
        {
            MixedSegmentInfo previous = currentSegments[i - 1];
            MixedSegmentInfo current = currentSegments[i];

            if (IsMarkerSegment(previous) || IsMarkerSegment(current))
            {
                continue;
            }

            float horizontalDistance = current.startX - previous.endX;
            float heightDelta = current.y - previous.y;
            string transitionLabel = $"{previous.index}:{previous.type} -> {current.index}:{current.type}";

            if (horizontalDistance < -epsilon)
            {
                LogLayoutWarning(
                    $"overlap detected at {transitionLabel}. " +
                    $"previousEndX={previous.endX:F3}, currentStartX={current.startX:F3}, overlap={-horizontalDistance:F3}"
                );
                continue;
            }

            if (Mathf.Abs(horizontalDistance) <= epsilon)
            {
                if (Mathf.Abs(heightDelta) <= epsilon)
                {
                    if (!AreSegmentsOnSamePhysicalPlatform(previous.index, current.index))
                    {
                        LogLayoutWarning(
                            "possible collider seam between " +
                            $"{previous.type} and {current.type} at X={current.startX:F3}. " +
                            $"Separate colliders touch at same Y={current.y:F3}."
                        );
                    }
                }
                else
                {
                    LogLayoutWarning(
                        $"vertical step without horizontal gap at {transitionLabel}. " +
                        $"x={current.startX:F3}, heightDelta={heightDelta:F3}"
                    );
                }

                continue;
            }

            if (horizontalDistance < minGapWidth - epsilon)
            {
                LogLayoutWarning(
                    $"gap smaller than minGapWidth at {transitionLabel}. " +
                    $"gap={horizontalDistance:F3}, minGapWidth={minGapWidth:F3}"
                );
            }

            if (horizontalDistance > maxGapWidth + epsilon)
            {
                LogLayoutWarning(
                    $"gap larger than maxGapWidth at {transitionLabel}. " +
                    $"gap={horizontalDistance:F3}, maxGapWidth={maxGapWidth:F3}"
                );
            }

            MixedGapInfo matchingGap;
            if (!TryFindGap(previous.endX, current.startX, epsilon, out matchingGap))
            {
                LogLayoutWarning(
                    $"platform transition has no matching gap record at {transitionLabel}. " +
                    $"previousEndX={previous.endX:F3}, currentStartX={current.startX:F3}"
                );
                continue;
            }

            if (Mathf.Abs(matchingGap.startY - previous.y) > epsilon ||
                Mathf.Abs(matchingGap.endY - current.y) > epsilon)
            {
                LogLayoutWarning(
                    $"gap height metadata mismatch at {transitionLabel}. " +
                    $"gapStartY={matchingGap.startY:F3}, previousY={previous.y:F3}, " +
                    $"gapEndY={matchingGap.endY:F3}, currentY={current.y:F3}"
                );
            }
        }
    }

    private void ValidateGapRecords(float epsilon)
    {
        for (int i = 0; i < currentGaps.Count; i++)
        {
            MixedGapInfo gap = currentGaps[i];
            float measuredWidth = gap.endX - gap.startX;

            if (gap.width <= 0f || measuredWidth <= epsilon)
            {
                LogLayoutWarning(
                    $"gap #{gap.index} has non-positive/tiny width. " +
                    $"startX={gap.startX:F3}, endX={gap.endX:F3}, width={gap.width:F3}"
                );
            }

            if (Mathf.Abs(measuredWidth - gap.width) > epsilon)
            {
                LogLayoutWarning(
                    $"gap #{gap.index} width metadata mismatch. " +
                    $"measured={measuredWidth:F3}, stored={gap.width:F3}"
                );
            }

            if (gap.width > maxGapWidth + epsilon)
            {
                LogLayoutWarning(
                    $"gap #{gap.index} is larger than maxGapWidth. " +
                    $"width={gap.width:F3}, maxGapWidth={maxGapWidth:F3}"
                );
            }

            if (gap.width < minGapWidth - epsilon)
            {
                LogLayoutWarning(
                    $"gap #{gap.index} is smaller than minGapWidth. " +
                    $"width={gap.width:F3}, minGapWidth={minGapWidth:F3}"
                );
            }
        }
    }

    private void ValidatePlatformColliderAlignment(float epsilon)
    {
        for (int i = 0; i < spawnedPlatforms.Count; i++)
        {
            SpawnedPlatformInfo platformInfo = spawnedPlatforms[i];

            if (platformInfo.instance == null)
            {
                continue;
            }

            BoxCollider2D boxCollider = platformInfo.instance.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                LogLayoutWarning(
                    $"physical platform for segments #{platformInfo.firstSegmentIndex}-{platformInfo.lastSegmentIndex} has no BoxCollider2D."
                );
                continue;
            }

            if (!boxCollider.enabled)
            {
                LogLayoutWarning(
                    $"physical platform for segments #{platformInfo.firstSegmentIndex}-{platformInfo.lastSegmentIndex} BoxCollider2D is disabled."
                );
            }

            Bounds colliderBounds = boxCollider.bounds;
            if (Mathf.Abs(colliderBounds.min.x - platformInfo.startX) > epsilon ||
                Mathf.Abs(colliderBounds.max.x - platformInfo.endX) > epsilon ||
                Mathf.Abs(colliderBounds.center.y - platformInfo.y) > epsilon)
            {
                LogLayoutWarning(
                    $"physical platform for segments #{platformInfo.firstSegmentIndex}-{platformInfo.lastSegmentIndex} collider bounds do not match generated span. " +
                    $"span=[{platformInfo.startX:F3}, {platformInfo.endX:F3}] y={platformInfo.y:F3}; " +
                    $"collider=[{colliderBounds.min.x:F3}, {colliderBounds.max.x:F3}] y={colliderBounds.center.y:F3}"
                );
            }

            SpriteRenderer spriteRenderer = platformInfo.instance.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                continue;
            }

            Bounds spriteBounds = spriteRenderer.bounds;
            if (Mathf.Abs(spriteBounds.min.x - colliderBounds.min.x) > epsilon ||
                Mathf.Abs(spriteBounds.max.x - colliderBounds.max.x) > epsilon ||
                Mathf.Abs(spriteBounds.min.y - colliderBounds.min.y) > epsilon ||
                Mathf.Abs(spriteBounds.max.y - colliderBounds.max.y) > epsilon)
            {
                LogLayoutWarning(
                    $"physical platform for segments #{platformInfo.firstSegmentIndex}-{platformInfo.lastSegmentIndex} visual bounds and collider bounds are misaligned. " +
                    $"sprite=[{spriteBounds.min.x:F3}, {spriteBounds.max.x:F3}] y=[{spriteBounds.min.y:F3}, {spriteBounds.max.y:F3}]; " +
                    $"collider=[{colliderBounds.min.x:F3}, {colliderBounds.max.x:F3}] y=[{colliderBounds.min.y:F3}, {colliderBounds.max.y:F3}]"
                );
            }
        }
    }

    private bool AreSegmentsOnSamePhysicalPlatform(int firstSegmentIndex, int secondSegmentIndex)
    {
        int firstPlatformIndex = FindPhysicalPlatformIndex(firstSegmentIndex);
        int secondPlatformIndex = FindPhysicalPlatformIndex(secondSegmentIndex);

        return firstPlatformIndex >= 0 &&
               secondPlatformIndex >= 0 &&
               firstPlatformIndex == secondPlatformIndex;
    }

    private int FindPhysicalPlatformIndex(int segmentIndex)
    {
        for (int i = 0; i < segmentPlatformBindings.Count; i++)
        {
            if (segmentPlatformBindings[i].segmentIndex == segmentIndex)
            {
                return segmentPlatformBindings[i].platformIndex;
            }
        }

        return -1;
    }

    private bool TryFindGap(float startX, float endX, float epsilon, out MixedGapInfo matchingGap)
    {
        for (int i = 0; i < currentGaps.Count; i++)
        {
            MixedGapInfo gap = currentGaps[i];

            if (Mathf.Abs(gap.startX - startX) <= epsilon &&
                Mathf.Abs(gap.endX - endX) <= epsilon)
            {
                matchingGap = gap;
                return true;
            }
        }

        matchingGap = default;
        return false;
    }

    private bool IsMarkerSegment(MixedSegmentInfo segment)
    {
        return string.Equals(segment.type, "GoalSegment", System.StringComparison.Ordinal);
    }

    private void LogLayoutWarning(string message)
    {
        Debug.LogWarning("MixedLevelGenerator diagnostics: " + message, this);
    }

    private void LogGenerationValue(string message)
    {
        if (!debugGenerationValues)
        {
            return;
        }

        Debug.Log("[MIXED GEN] " + message, this);
    }

    private void LogV5GenerationWarningOnce(ref bool warningFlag, string message)
    {
        if (warningFlag)
        {
            return;
        }

        warningFlag = true;
        LogLayoutWarning("V5 generation rules: " + message);
    }

    private bool HasRequiredReferences()
    {
        if (levelRoot == null)
        {
            Debug.LogError("MixedLevelGenerator: levelRoot não está atribuído.");
            return false;
        }

        if (platformPrefab == null)
        {
            Debug.LogError("MixedLevelGenerator: platformPrefab não está atribuído.");
            return false;
        }

        if (goalPrefab == null)
        {
            Debug.LogError("MixedLevelGenerator: goalPrefab não está atribuído.");
            return false;
        }

        return true;
    }

    private void ValidateSettings()
    {
        minSegments = Mathf.Max(2, minSegments);
        maxSegments = Mathf.Max(minSegments, maxSegments);
        minPlatformWidth = Mathf.Max(2.5f, minPlatformWidth);
        minLandingPlatformWidth = Mathf.Max(0.1f, minLandingPlatformWidth);
        maxPlatformWidth = Mathf.Max(minPlatformWidth, maxPlatformWidth);
        minGapWidth = Mathf.Max(0f, minGapWidth);
        maxGapWidth = Mathf.Max(minGapWidth, maxGapWidth);
        minHeightDelta = Mathf.Max(0f, minHeightDelta);
        maxHeightDelta = Mathf.Max(minHeightDelta, maxHeightDelta);
        minDropHeight = Mathf.Max(0f, minDropHeight);
        maxDropHeight = Mathf.Max(minDropHeight, maxDropHeight);
        minPlatformChainWidth = Mathf.Max(0.1f, minPlatformChainWidth);
        maxPlatformChainWidth = Mathf.Max(minPlatformChainWidth, maxPlatformChainWidth);
        minPlatformChainPieces = Mathf.Max(1, minPlatformChainPieces);
        maxPlatformChainPieces = Mathf.Max(minPlatformChainPieces, maxPlatformChainPieces);
        minPlatformChainGap = Mathf.Max(0f, minPlatformChainGap);
        maxPlatformChainGap = Mathf.Max(minPlatformChainGap, maxPlatformChainGap);
        maxPlatformChainsPerEpisode = Mathf.Max(0, maxPlatformChainsPerEpisode);
        verticalSafetyLimit = Mathf.Max(0.1f, verticalSafetyLimit);
        platformHeightScale = Mathf.Max(0.1f, platformHeightScale);
        verticalSegmentChanceMultiplier = Mathf.Max(0f, verticalSegmentChanceMultiplier);
        maxConsecutiveHardSegments = Mathf.Max(1, maxConsecutiveHardSegments);
        minRecoveryPlatformWidth = Mathf.Max(0.1f, minRecoveryPlatformWidth);
        safeEdgeMargin = Mathf.Max(0f, safeEdgeMargin);
        minDistanceBetweenGaps = Mathf.Max(0f, minDistanceBetweenGaps);
        minRunupBeforeGap = Mathf.Max(0f, minRunupBeforeGap);
        minLandingAfterGap = Mathf.Max(0f, minLandingAfterGap);
        finalGoalPlatformWidth = Mathf.Max(0f, finalGoalPlatformWidth);
        finalGoalSafeRunup = Mathf.Max(0f, finalGoalSafeRunup);
        goalEdgeMargin = Mathf.Max(0f, goalEdgeMargin);
        wideGoalTriggerSize = new Vector2(
            Mathf.Max(0.1f, wideGoalTriggerSize.x),
            Mathf.Max(0.1f, wideGoalTriggerSize.y)
        );
        maxGoalHeightOffset = Mathf.Max(MinimumGoalHeightOffset, maxGoalHeightOffset);

        if (!useV5GenerationRules)
        {
            return;
        }

        if (forceRecoveryPlatformAfterHardSegment && minRecoveryPlatformWidth < minPlatformWidth)
        {
            LogV5GenerationWarningOnce(
                ref loggedRecoveryWidthWarning,
                $"recovery platform minimum width ({minRecoveryPlatformWidth:F2}) is smaller than minPlatformWidth ({minPlatformWidth:F2})."
            );
        }

        if (allowHigherGoalPlacement && maxGoalHeightOffset > verticalSafetyLimit + goalYOffset)
        {
            LogV5GenerationWarningOnce(
                ref loggedGoalHeightWarning,
                $"maxGoalHeightOffset ({maxGoalHeightOffset:F2}) exceeds the current vertical safety envelope."
            );
        }
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugDrawSegments || currentSegments == null)
        {
            return;
        }

        for (int i = 0; i < currentSegments.Count; i++)
        {
            DrawSegmentGizmo(currentSegments[i]);
        }
    }

    private void DrawSegmentGizmo(MixedSegmentInfo segment)
    {
        Color color = GetSegmentDebugColor(segment.type);
        float halfMarkerHeight = Mathf.Max(0.25f, platformHeightScale * 0.75f);
        float minX = Mathf.Min(segment.startX, segment.endX);
        float maxX = Mathf.Max(segment.startX, segment.endX);
        Vector3 startBottom = new Vector3(segment.startX, segment.y - halfMarkerHeight, 0f);
        Vector3 startTop = new Vector3(segment.startX, segment.y + halfMarkerHeight, 0f);
        Vector3 endBottom = new Vector3(segment.endX, segment.y - halfMarkerHeight, 0f);
        Vector3 endTop = new Vector3(segment.endX, segment.y + halfMarkerHeight, 0f);
        Vector3 lineStart = new Vector3(minX, segment.y, 0f);
        Vector3 lineEnd = new Vector3(maxX, segment.y, 0f);

        Gizmos.color = color;
        Gizmos.DrawLine(startBottom, startTop);
        Gizmos.DrawLine(endBottom, endTop);
        Gizmos.DrawLine(lineStart, lineEnd);
        Gizmos.DrawSphere(lineStart, 0.08f);
        Gizmos.DrawSphere(lineEnd, 0.08f);

#if UNITY_EDITOR
        Handles.color = color;
        Vector3 labelPosition = new Vector3(minX, segment.y + halfMarkerHeight + 0.2f, 0f);
        Handles.Label(
            labelPosition,
            $"#{segment.index} {segment.type}\nstart={segment.startX:F2} end={segment.endX:F2} y={segment.y:F2}"
        );
#endif
    }

    private Color GetSegmentDebugColor(string segmentType)
    {
        switch (segmentType)
        {
            case "FlatSegment":
                return Color.green;
            case "GapSegment":
                return Color.yellow;
            case "StepUpSegment":
                return Color.cyan;
            case "StepDownSegment":
                return Color.magenta;
            case "SafeDropSegment":
                return new Color(1f, 0.5f, 0f, 1f);
            case "PlatformChainSegment":
                return Color.white;
            case "GoalSegment":
                return Color.red;
            default:
                return Color.gray;
        }
    }

    private struct SpawnedPlatformInfo
    {
        public GameObject instance;
        public int firstSegmentIndex;
        public int lastSegmentIndex;
        public float startX;
        public float endX;
        public float y;
    }

    private struct SegmentPlatformBinding
    {
        public int segmentIndex;
        public int platformIndex;
    }
}
