using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>PNG-based, renderer-only presentation skin for Final Demo scenes.</summary>
public sealed class FinalDemoVisualPolish : MonoBehaviour
{
    private const string Prefix = "ER_FinalDemo_";
    private const string Menu = "ER_FinalDemo_Menu";
    private const string Bootstrap = "FinalDemo_LevelVisualPolish";
    private const string RuntimeRoot = "FinalDemo_RuntimeVisuals";
    private const string Art = "DemoFinal/Art/";
    private static Sprite platform, player, android, pickupCyan, pickupGold, goalOpen, goalLocked, background;
    private readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
    private Transform visualRoot;
    private Scene scene;
    private int rescanFrames;
    private bool maxScoreMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapCurrent() { SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

    private static void SceneLoaded(Scene loaded, LoadSceneMode mode)
    {
        if (!IsDemo(loaded)) return;
        foreach (GameObject root in loaded.GetRootGameObjects()) if (root.name == Bootstrap) return;
        GameObject host = new GameObject(Bootstrap);
        SceneManager.MoveGameObjectToScene(host, loaded);
        host.AddComponent<FinalDemoVisualPolish>();
    }

    private static bool IsDemo(Scene candidate)
    {
        return candidate.IsValid() && candidate.name.StartsWith(Prefix, StringComparison.Ordinal) && candidate.name != Menu;
    }

    private IEnumerator Start()
    {
        scene = gameObject.scene;
        if (!IsDemo(scene)) yield break;
        maxScoreMode = scene.name.IndexOf("MaxScore", StringComparison.OrdinalIgnoreCase) >= 0;
        visualRoot = RecreateRoot(scene);
        HideOld(scene);
        yield return null;
        bool assets = LoadAssets();
        int platforms = SkinPlatforms(scene);
        bool playerSkinned = SkinPlayers(scene);
        int androids = SkinAndroids(scene);
        int goals = SkinGoals(scene);
        int pickups = SkinPickups(scene);
        SkinBackground(scene);
        HideOld(scene);
        yield return null;
        HideOld(scene);
        int external = CountExternal(scene);
#if UNITY_EDITOR
        Debug.Log($"[FinalDemoArtPass] assets generated/loaded success={assets}");
        Debug.Log($"[FinalDemoArtPass] platforms skinned={platforms}");
        Debug.Log($"[FinalDemoArtPass] player skinned={playerSkinned.ToString().ToLowerInvariant()}");
        Debug.Log($"[FinalDemoArtPass] androids skinned={androids}");
        Debug.Log($"[FinalDemoArtPass] goals skinned={goals}");
        Debug.Log($"[FinalDemoArtPass] pickups skinned={pickups}");
        Debug.Log($"[FinalDemoArtPass] old renderers disabled={hidden.Count} activeExternalRenderers={external}");
#endif
    }

    private void LateUpdate()
    {
        foreach (Renderer renderer in hidden)
            if (renderer != null && renderer.enabled && !UnderRoot(renderer.transform) && !Exception(renderer)) renderer.enabled = false;
        if (rescanFrames++ < 18 && scene.IsValid()) HideOld(scene);
    }

    private static Transform RecreateRoot(Scene target)
    {
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (item == null || item.gameObject.scene != target || item.name != RuntimeRoot) continue;
            item.gameObject.SetActive(false);
            item.name = RuntimeRoot + "_PendingDestroy";
            Destroy(item.gameObject);
        }
        GameObject root = new GameObject(RuntimeRoot);
        SceneManager.MoveGameObjectToScene(root, target);
        return root.transform;
    }

    private void HideOld(Scene target)
    {
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (renderer == null || renderer.gameObject.scene != target || UnderRoot(renderer.transform) || Exception(renderer)) continue;
            hidden.Add(renderer);
            renderer.enabled = false;
        }
    }

    private bool UnderRoot(Transform candidate)
    {
        for (Transform current = candidate; current != null; current = current.parent) if (current == visualRoot) return true;
        return false;
    }

    private static bool Exception(Renderer renderer)
    {
        // The player's sprint TrailRenderer is a real gameplay-feedback renderer; keep it visible
        // instead of hiding it with the other original renderers.
        if (renderer is TrailRenderer) return true;
        if (renderer.GetComponentInParent<Canvas>(true) != null || renderer.GetComponentInParent<TextMesh>(true) != null ||
            renderer.GetComponent<Camera>() != null || renderer.GetComponent<Light>() != null || renderer.GetComponent<AudioSource>() != null) return true;
        for (Transform current = renderer.transform; current != null; current = current.parent)
        {
            string n = current.name;
            if (n.Equals("UI", StringComparison.OrdinalIgnoreCase) || n.Equals("HUD", StringComparison.OrdinalIgnoreCase) ||
                n.Equals("Canvas", StringComparison.OrdinalIgnoreCase) || n.StartsWith("UI_", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("HUD_", StringComparison.OrdinalIgnoreCase)) return true;
            foreach (Component component in current.GetComponents<Component>())
            {
                string type = component != null ? component.GetType().FullName : null;
                if (!string.IsNullOrEmpty(type) && type.StartsWith("TMPro.", StringComparison.Ordinal)) return true;
            }
        }
        return false;
    }

    private static bool LoadAssets()
    {
        platform = Load("platform_slab", 16, new Vector2(.5f, .5f), new Vector4(4, 3, 4, 3), true);
        player = Load("player_runner", 32, new Vector2(.5f, .08f), Vector4.zero, true);
        android = Load("android_industrial", 32, new Vector2(.5f, .06f), Vector4.zero, true);
        pickupCyan = Load("pickup_powercell_cyan", 24, Vector2.one * .5f, Vector4.zero, true);
        pickupGold = Load("pickup_powercell_gold", 24, Vector2.one * .5f, Vector4.zero, true);
        goalOpen = Load("goal_portal_unlocked", 32, new Vector2(.5f, .08f), Vector4.zero, true);
        goalLocked = Load("goal_portal_locked", 32, new Vector2(.5f, .08f), Vector4.zero, true);
        background = Load("background_training_sim", 100, Vector2.one * .5f, Vector4.zero, false);
        return platform && player && android && pickupCyan && pickupGold && goalOpen && goalLocked && background;
    }

    private static Sprite Load(string asset, float ppu, Vector2 pivot, Vector4 border, bool point)
    {
        Texture2D texture = Resources.Load<Texture2D>(Art + asset);
        if (!texture) { Debug.LogError("[FinalDemoArtPass] missing PNG resource=" + Art + asset); return null; }
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu, 0,
            SpriteMeshType.FullRect, border);
        sprite.name = "FinalDemoArt_" + asset;
        return sprite;
    }

    private GameObject Visual(string name, Sprite sprite, int order)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(visualRoot, false);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return item;
    }

    private int SkinPlatforms(Scene target)
    {
        int totalColliders = 0;
        int acceptedWalkable = 0;
        int visualsCreated = 0;
        int hiddenSolidCollidersWithoutVisual = 0;
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Include);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (!collider || collider.gameObject.scene != target) continue;
            totalColliders++;

            string exclusionReason;
            if (!IsWalkableCollider(collider, target, out exclusionReason))
            {
                if (IsSolidGeometryCandidate(collider, target))
                {
                    hiddenSolidCollidersWithoutVisual++;
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"[FinalDemoVisualPolish][WALKABLE_AUDIT] excludedSolid " +
                        $"path={Path(collider.transform)} layer={LayerName(collider.gameObject.layer)} " +
                        $"tag={collider.tag} bounds={collider.bounds} isTrigger={collider.isTrigger} " +
                        $"components={ComponentList(collider.transform)} reason={exclusionReason}",
                        collider);
#endif
                }
                continue;
            }

            acceptedWalkable++;
            GameObject item = Visual("PlatformSkin_" + acceptedWalkable.ToString("00"), platform, 140);
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Sliced;
            // Subtle mode tint: SpeedRun stays cool cyan, MaxScore leans warm magenta/violet.
            renderer.color = maxScoreMode ? new Color(1f, 0.85f, 0.96f) : new Color(0.88f, 0.97f, 1f);
            Color accent = maxScoreMode ? new Color(1f, 0.42f, 0.85f) : new Color(0.22f, 0.95f, 1f);
            item.AddComponent<FinalDemoPlatformSkin>().Configure(new[] { collider }, renderer, accent);
            visualsCreated++;
        }
#if UNITY_EDITOR
        Debug.Log(
            $"[FinalDemoVisualPolish][WALKABLE_AUDIT] totalColliders={totalColliders} " +
            $"acceptedWalkable={acceptedWalkable} visualsCreated={visualsCreated} " +
            $"hiddenSolidCollidersWithoutVisual={hiddenSolidCollidersWithoutVisual}");
#endif
        return visualsCreated;
    }

    private bool SkinPlayers(Scene target)
    {
        bool found = false;
        foreach (EdgeRunnerAgentV5 agent in FindObjectsByType<EdgeRunnerAgentV5>(FindObjectsInactive.Exclude))
        {
            if (!agent || agent.gameObject.scene != target) continue;
            GameObject item = Visual("PlayerSkin", player, 260);
            Fit(item.transform, player, 1.72f);
            item.AddComponent<FinalDemoActorSkin>().Configure(agent.transform, PrimaryCollider(agent.gameObject),
                agent.GetComponent<Rigidbody2D>(), agent.GetComponentsInChildren<Renderer>(true), true);
            found = true;
        }
        return found;
    }

    private int SkinAndroids(Scene target)
    {
        HashSet<GameObject> roots = new HashSet<GameObject>();
        Add(FindObjectsByType<ScoreAttackAndroid>(FindObjectsInactive.Exclude), target, roots);
        Add(FindObjectsByType<StompableAndroidEnemy>(FindObjectsInactive.Exclude), target, roots);
        Add(FindObjectsByType<EdgeRunnerEnemyMarker>(FindObjectsInactive.Exclude), target, roots);
        Add(FindObjectsByType<DemoAndroidPatrol>(FindObjectsInactive.Exclude), target, roots);
        int count = 0;
        foreach (GameObject owner in roots)
        {
            if (!owner || owner.GetComponentInParent<EdgeRunnerAgentV5>() || AncestorIn(owner.transform, roots)) continue;
            GameObject item = Visual("AndroidSkin_" + count++.ToString("00"), android, 250);
            Fit(item.transform, android, 1.85f);
            item.AddComponent<FinalDemoAndroidSkin>().Configure(owner.transform, PrimaryCollider(owner, true),
                owner.GetComponent<Rigidbody2D>(), owner.GetComponent<ScoreAttackAndroid>(),
                owner.GetComponent<StompableAndroidEnemy>(), owner.GetComponentsInChildren<Renderer>(true), maxScoreMode);
        }
        return count;
    }

    private int SkinPickups(Scene target)
    {
        HashSet<GameObject> roots = new HashSet<GameObject>();
        Add(FindObjectsByType<ScoreAttackCoin>(FindObjectsInactive.Exclude), target, roots);
        Add(FindObjectsByType<DemoEnergyCell>(FindObjectsInactive.Exclude), target, roots);
        Add(FindObjectsByType<FinalDemoVisualCollectible>(FindObjectsInactive.Exclude), target, roots);
        int count = 0;
        foreach (GameObject owner in roots)
        {
            if (!owner || AncestorIn(owner.transform, roots)) continue;
            bool gold = Has(owner.name, "High", "Gold");
            Sprite sprite = gold ? pickupGold : pickupCyan;
            GameObject item = Visual("PickupSkin_" + count++.ToString("00"), sprite, 230);
            Fit(item.transform, sprite, gold ? .92f : .8f);
            item.AddComponent<FinalDemoPickupSkin>().Configure(owner.transform, owner.GetComponent<ScoreAttackCoin>(),
                owner.GetComponent<FinalDemoVisualCollectible>(), owner.GetComponentsInChildren<Renderer>(true), gold ? 2.1f : 1.7f, gold);
        }
        return count;
    }

    private int SkinGoals(Scene target)
    {
        HashSet<GameObject> done = new HashSet<GameObject>();
        ScoreAttackManager manager = FindAnyObjectByType<ScoreAttackManager>();
        foreach (FinalDemoGoalObserver observer in FindObjectsByType<FinalDemoGoalObserver>(FindObjectsInactive.Exclude))
        {
            if (!observer || observer.gameObject.scene != target) continue;
            ScoreAttackGoalLock goalLock = observer.GetComponent<ScoreAttackGoalLock>();
            GameObject owner = observer.gameObject;
            if (!goalLock) { goalLock = ClosestLock(observer.transform.position, target); if (goalLock) owner = goalLock.gameObject; }
            if (!done.Add(owner)) continue;
            GameObject item = Visual("GoalSkin_" + done.Count.ToString("00"), goalOpen, 270);
            Fit(item.transform, goalOpen, 2.6f);
            item.AddComponent<FinalDemoGoalSkin>().Configure(owner.transform, item.GetComponent<SpriteRenderer>(), goalLock,
                manager, goalOpen, goalLocked, owner.GetComponentsInChildren<Renderer>(true));
        }
        return done.Count;
    }

    private void SkinBackground(Scene target)
    {
        Camera camera = Camera.main;
        if (!camera || camera.gameObject.scene != target || !background) return;
        GameObject item = Visual("BackgroundSkin", background, -1000);
        SpriteRenderer backgroundRenderer = item.GetComponent<SpriteRenderer>();
        backgroundRenderer.color = maxScoreMode ? new Color(0.95f, 0.82f, 0.96f) : new Color(0.82f, 0.92f, 1f);
        item.AddComponent<FinalDemoBackgroundSkin>().Configure(camera, backgroundRenderer);

        GameObject backdrop = new GameObject("AmbientBackdrop");
        backdrop.transform.SetParent(visualRoot, false);
        backdrop.AddComponent<FinalDemoAmbientBackdrop>().Configure(camera, maxScoreMode);
    }

    private static void Fit(Transform target, Sprite sprite, float height)
    {
        if (!sprite) return;
        float scale = height / sprite.bounds.size.y;
        target.localScale = new Vector3(scale, scale, 1);
    }

    private static Collider2D PrimaryCollider(GameObject owner, bool includeTriggers = false)
    {
        Collider2D best = null;
        float bestArea = -1;
        foreach (Collider2D collider in owner.GetComponentsInChildren<Collider2D>(true))
        {
            if (!collider || (!includeTriggers && collider.isTrigger)) continue;
            float area = collider.bounds.size.x * collider.bounds.size.y;
            if (area > bestArea) { best = collider; bestArea = area; }
        }
        return best;
    }

    private static void Add<T>(T[] components, Scene target, HashSet<GameObject> roots) where T : Component
    {
        foreach (T component in components) if (component && component.gameObject.scene == target) roots.Add(component.gameObject);
    }

    private static bool AncestorIn(Transform candidate, HashSet<GameObject> roots)
    {
        for (Transform parent = candidate.parent; parent != null; parent = parent.parent)
            if (roots.Contains(parent.gameObject)) return true;
        return false;
    }

    private static bool IsWalkableCollider(Collider2D collider, Scene target, out string exclusionReason)
    {
        exclusionReason = string.Empty;
        if (!collider || collider.gameObject.scene != target)
        {
            exclusionReason = "different-scene-or-null";
            return false;
        }
        if (!collider.enabled || !collider.gameObject.activeInHierarchy)
        {
            exclusionReason = "disabled-or-inactive";
            return false;
        }
        if (collider.isTrigger)
        {
            exclusionReason = "trigger";
            return false;
        }

        Vector2 size = collider.bounds.size;
        if (size.x < .8f)
        {
            exclusionReason = "width-below-0.8";
            return false;
        }
        if (size.y < .05f || size.y > 2f)
        {
            exclusionReason = "height-outside-0.05-to-2.0";
            return false;
        }

        string semanticReason;
        if (HasExcludedWalkableSemantics(collider, out semanticReason))
        {
            exclusionReason = semanticReason;
            return false;
        }

        // Layer/tag/name can strengthen acceptance, but are deliberately not required.
        // Any remaining solid collider with plausible walkable bounds receives a visual.
        return true;
    }

    private static bool IsSolidGeometryCandidate(Collider2D collider, Scene target)
    {
        if (!collider || collider.gameObject.scene != target || !collider.enabled ||
            !collider.gameObject.activeInHierarchy || collider.isTrigger) return false;
        Vector2 size = collider.bounds.size;
        return size.x >= .8f && size.y >= .05f && size.y <= 2f;
    }

    private static bool HasExcludedWalkableSemantics(Collider2D collider, out string reason)
    {
        if (collider.GetComponentInParent<Canvas>(true) != null)
        {
            reason = "ui-canvas";
            return true;
        }
        if (collider.GetComponentInParent<EdgeRunnerAgentV5>(true) != null)
        {
            reason = "player-or-agent-component";
            return true;
        }
        if (collider.GetComponentInParent<ScoreAttackAndroid>(true) != null ||
            collider.GetComponentInParent<StompableAndroidEnemy>(true) != null ||
            collider.GetComponentInParent<EdgeRunnerEnemyMarker>(true) != null ||
            collider.GetComponentInParent<DemoAndroidPatrol>(true) != null)
        {
            reason = "android-or-enemy-component";
            return true;
        }
        if (collider.GetComponentInParent<ScoreAttackCoin>(true) != null ||
            collider.GetComponentInParent<DemoEnergyCell>(true) != null ||
            collider.GetComponentInParent<FinalDemoVisualCollectible>(true) != null)
        {
            reason = "pickup-or-coin-component";
            return true;
        }
        if (collider.GetComponentInParent<ScoreAttackGoalLock>(true) != null ||
            collider.GetComponentInParent<FinalDemoGoalObserver>(true) != null)
        {
            reason = "goal-or-observer-component";
            return true;
        }

        string layer = LayerName(collider.gameObject.layer);
        bool explicitGround = Has(layer, "Ground", "Platform") ||
                              collider.CompareTag("Ground") || collider.CompareTag("Platform");
        if (Has(layer, "Player", "Agent", "Android", "Enemy", "Hazard", "Death", "Goal", "Trigger",
            "Sensor", "Pickup", "Coin", "UI"))
        {
            reason = "excluded-layer:" + layer;
            return true;
        }

        for (Transform current = collider.transform; current != null; current = current.parent)
        {
            if (!explicitGround && Has(current.name, "Player", "Agent", "Android", "Enemy", "Hazard", "DeathZone", "Death Zone",
                "GoalLock", "Goal Lock", "Goal", "Trigger", "Sensor", "Pickup", "PowerCell", "Power Cell",
                "Coin", "Observer", "UI"))
            {
                reason = "excluded-hierarchy-name:" + current.name;
                return true;
            }

            Component[] components = current.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!component || component is Collider2D || component is Rigidbody2D || component is Transform ||
                    component is Renderer) continue;
                string typeName = component.GetType().Name;
                if (Has(typeName, "Agent", "Android", "Enemy", "Hazard", "DeathZone", "Goal", "GoalLock",
                    "Trigger", "Sensor", "Pickup", "PowerCell", "Coin", "Observer", "Canvas"))
                {
                    reason = "excluded-component:" + typeName;
                    return true;
                }
            }
        }

        reason = string.Empty;
        return false;
    }

    private static bool Has(string source, params string[] terms)
    {
        if (string.IsNullOrEmpty(source)) return false;
        foreach (string term in terms) if (source.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    private static string LayerName(int layer)
    {
        string name = LayerMask.LayerToName(layer);
        return string.IsNullOrEmpty(name) ? layer.ToString() : name;
    }

    private static string ComponentList(Transform item)
    {
        List<string> names = new List<string>();
        for (Transform current = item; current != null; current = current.parent)
        {
            Component[] components = current.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!component) continue;
                string name = component.GetType().Name;
                if (!names.Contains(name)) names.Add(name);
            }
        }
        return string.Join(",", names);
    }

    private static ScoreAttackGoalLock ClosestLock(Vector3 position, Scene target)
    {
        ScoreAttackGoalLock best = null;
        float bestDistance = float.PositiveInfinity;
        foreach (ScoreAttackGoalLock item in FindObjectsByType<ScoreAttackGoalLock>(FindObjectsInactive.Exclude))
        {
            if (!item || item.gameObject.scene != target) continue;
            float distance = (item.transform.position - position).sqrMagnitude;
            if (distance < bestDistance) { best = item; bestDistance = distance; }
        }
        return best;
    }

    private int CountExternal(Scene target)
    {
        int count = 0;
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
        {
            if (!renderer || renderer.gameObject.scene != target || !renderer.enabled || UnderRoot(renderer.transform) || Exception(renderer)) continue;
            count++;
#if UNITY_EDITOR
            Debug.LogWarning($"[FinalDemoArtPass] external renderer active path={Path(renderer.transform)} type={renderer.GetType().Name}", renderer);
#endif
        }
        return count;
    }

    private static string Path(Transform item)
    {
        string path = item ? item.name : "<null>";
        for (Transform parent = item ? item.parent : null; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return path;
    }
}

internal static class FinalDemoSkinUtil
{
    private static readonly int GroundMask = LayerMask.GetMask("Ground");

    public static void Hide(Renderer[] renderers)
    {
        if (renderers == null) return;
        foreach (Renderer renderer in renderers) if (renderer) renderer.enabled = false;
    }

    public static Vector3 Position(Transform anchor, Collider2D collider)
    {
        if (collider && collider.enabled) return new Vector3(collider.bounds.center.x, collider.bounds.min.y, 0);
        return anchor ? new Vector3(anchor.position.x, anchor.position.y, 0) : Vector3.zero;
    }

    /// <summary>
    /// Gameplay hitboxes for Androids/Goal are triggers sized and offset for detection
    /// logic, not for visual grounding, so they can sit noticeably above the platform
    /// they stand on. This probes the actual walkable surface directly beneath the
    /// anchor so the sprite's feet/base render flush with the platform.
    /// </summary>
    public static float GroundY(Vector2 anchorPosition, float fallbackY)
    {
        RaycastHit2D hit = Physics2D.Raycast(anchorPosition + Vector2.up * 0.5f, Vector2.down, 8f, GroundMask);
        return hit.collider ? hit.point.y : fallbackY;
    }

    private static Sprite sharedGlow;

    /// <summary>Soft radial glow shared by sensor lights, sprint boost and powercell halos.</summary>
    public static Sprite Glow()
    {
        if (sharedGlow) return sharedGlow;
        const int s = 24;
        Texture2D texture = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2 center = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
        float radius = s * 0.5f;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
            float alpha = Mathf.Clamp01(1f - distance);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
        }
        texture.Apply();
        sharedGlow = Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
        sharedGlow.name = "FinalDemoSharedGlow";
        return sharedGlow;
    }

    private static Sprite sharedSolid;

    /// <summary>Flat white unit sprite for rails, panel accents and background silhouettes.</summary>
    public static Sprite Solid()
    {
        if (sharedSolid) return sharedSolid;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        sharedSolid = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        sharedSolid.name = "FinalDemoSharedSolid";
        return sharedSolid;
    }
}

internal sealed class FinalDemoPlatformSkin : MonoBehaviour
{
    private Collider2D[] colliders;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer topEdge;
    private SpriteRenderer underGlow;
    private Color accent;
    public void Configure(Collider2D[] value, SpriteRenderer target, Color accentColor)
    {
        colliders = value; spriteRenderer = target; accent = accentColor;
        int order = spriteRenderer ? spriteRenderer.sortingOrder : 140;
        topEdge = MakeChild("PlatformNeonTop", order + 3);
        topEdge.sprite = FinalDemoSkinUtil.Solid();
        topEdge.color = accent;
        underGlow = MakeChild("PlatformUnderGlow", order - 2);
        underGlow.sprite = FinalDemoSkinUtil.Glow();
        underGlow.color = new Color(accent.r, accent.g, accent.b, 0.14f);
        Sync();
    }
    private SpriteRenderer MakeChild(string name, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = order;
        return sr;
    }
    private void LateUpdate() { Sync(); }
    private void Sync()
    {
        if (!spriteRenderer || colliders == null) return;
        bool found = false;
        Bounds bounds = default;
        foreach (Collider2D collider in colliders)
        {
            if (!collider || !collider.enabled) continue;
            if (!found) { bounds = collider.bounds; found = true; }
            else { bounds.Encapsulate(collider.bounds.min); bounds.Encapsulate(collider.bounds.max); }
        }
        spriteRenderer.enabled = found;
        if (topEdge) topEdge.enabled = found;
        if (underGlow) underGlow.enabled = found;
        if (!found) return;
        float height = Mathf.Max(.42f, bounds.size.y);
        transform.position = new Vector3(bounds.center.x, bounds.max.y - height * .5f, 0);
        spriteRenderer.size = new Vector2(Mathf.Max(.1f, bounds.size.x), height);
        if (topEdge)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f + bounds.center.x * 0.2f);
            topEdge.color = new Color(accent.r, accent.g, accent.b, pulse);
            topEdge.transform.position = new Vector3(bounds.center.x, bounds.max.y - 0.015f, -0.02f);
            topEdge.transform.localScale = new Vector3(bounds.size.x * 0.99f, 0.1f, 1f);
        }
        if (underGlow)
        {
            underGlow.transform.position = new Vector3(bounds.center.x, bounds.min.y + 0.1f, 0.12f);
            underGlow.transform.localScale = new Vector3(bounds.size.x * 0.7f, 1.1f, 1f);
        }
    }
}

internal sealed class FinalDemoActorSkin : MonoBehaviour
{
    private static readonly Color SprintCyan = new Color(0.4f, 0.95f, 1f, 1f);
    private Transform anchor;
    private Collider2D bodyCollider;
    private Rigidbody2D body;
    private Renderer[] originals;
    private bool flip;
    private Vector3 baseScale;
    private float phase;
    private SpriteRenderer skinSprite;
    private DemoSprintVisual sprint;
    private SpriteRenderer backGlow;
    private SpriteRenderer[] ghosts;
    private float[] ghostAlpha;
    private int ghostCursor;
    private float nextGhostTime;
    private SpriteRenderer footGlow;
    private float prevVelY;
    private float landSquashAt = -1f;
    public void Configure(Transform owner, Collider2D shape, Rigidbody2D rigidbody, Renderer[] hidden, bool shouldFlip)
    {
        anchor = owner; bodyCollider = shape; body = rigidbody; originals = hidden; flip = shouldFlip; baseScale = transform.localScale;
        skinSprite = GetComponent<SpriteRenderer>();
        // Reuse the existing sprint detector (velocity thresholds + hysteresis) instead of
        // re-deriving sprint state, so the effect only shows during a real sprint.
        sprint = owner ? owner.GetComponent<DemoSprintVisual>() : null;
        int sortBase = skinSprite ? skinSprite.sortingOrder : 260;
        Transform container = transform.parent != null ? transform.parent : transform;

        GameObject glowGo = new GameObject("PlayerSprintGlow");
        glowGo.transform.SetParent(container, false);
        backGlow = glowGo.AddComponent<SpriteRenderer>();
        backGlow.sprite = FinalDemoSkinUtil.Glow();
        backGlow.color = new Color(SprintCyan.r, SprintCyan.g, SprintCyan.b, 0f);
        backGlow.sortingOrder = sortBase - 1;
        backGlow.enabled = false;

        GameObject footGo = new GameObject("PlayerFootGlow");
        footGo.transform.SetParent(container, false);
        footGlow = footGo.AddComponent<SpriteRenderer>();
        footGlow.sprite = FinalDemoSkinUtil.Glow();
        footGlow.color = new Color(SprintCyan.r, SprintCyan.g, SprintCyan.b, 0f);
        footGlow.sortingOrder = sortBase - 1;
        footGlow.enabled = false;

        ghosts = new SpriteRenderer[6];
        ghostAlpha = new float[6];
        for (int i = 0; i < ghosts.Length; i++)
        {
            GameObject g = new GameObject("PlayerAfterImage_" + i);
            g.transform.SetParent(container, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = skinSprite ? skinSprite.sprite : null;
            sr.sortingOrder = sortBase - 2;
            sr.enabled = false;
            ghosts[i] = sr;
        }
    }
    private void LateUpdate()
    {
        FinalDemoSkinUtil.Hide(originals);
        if (!anchor) { gameObject.SetActive(false); return; }
        float speed = body ? Mathf.Abs(body.linearVelocity.x) : 0;
        phase += Time.deltaTime * Mathf.Lerp(3.5f, 10, Mathf.InverseLerp(0, 9, speed));
        Vector3 position = FinalDemoSkinUtil.Position(anchor, bodyCollider);
        position.y += Mathf.Abs(Mathf.Sin(phase)) * Mathf.Lerp(.002f, .018f, Mathf.InverseLerp(.3f, 8, speed));
        transform.position = position;
        float direction = Mathf.Sign(baseScale.x);
        if (flip && body && Mathf.Abs(body.linearVelocity.x) > .12f) direction = Mathf.Sign(body.linearVelocity.x);
        transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * direction, baseScale.y, 1);
        transform.rotation = Quaternion.Euler(0, 0, body ? Mathf.Clamp(-body.linearVelocity.x * .2f, -2.5f, 2.5f) : 0);
        UpdateJumpLand(direction);
        UpdateSprintEffect(sprint != null && sprint.IsSprintVisualActive, direction);
    }

    private void UpdateJumpLand(float direction)
    {
        float vy = body ? body.linearVelocity.y : 0f;
        // Landing: a sharp downward speed resolving to ~0 triggers a brief squash.
        if (prevVelY < -4f && vy > -1f) landSquashAt = Time.time;
        prevVelY = vy;

        if (footGlow)
        {
            // Foot burst that flares as the player pushes off / rises (jump), fades on the ground.
            float rise = Mathf.Clamp01(vy / 8f);
            float alpha = 0.5f * rise;
            footGlow.enabled = alpha > 0.02f;
            footGlow.color = new Color(SprintCyan.r, SprintCyan.g, SprintCyan.b, alpha);
            footGlow.transform.position = transform.position + new Vector3(0f, 0.06f, 0.06f);
            footGlow.transform.localScale = Vector3.one * (Mathf.Abs(baseScale.x) * (1.1f + 0.7f * rise));
        }

        if (landSquashAt > 0f)
        {
            float lt = (Time.time - landSquashAt) / 0.18f;
            if (lt < 1f)
            {
                float s = Mathf.Sin(lt * Mathf.PI);
                Vector3 sc = transform.localScale;
                transform.localScale = new Vector3(sc.x * (1f + 0.12f * s), sc.y * (1f - 0.12f * s), 1f);
            }
            else
            {
                landSquashAt = -1f;
            }
        }
    }

    private void UpdateSprintEffect(bool sprinting, float direction)
    {
        if (skinSprite)
        {
            Color target = sprinting ? SprintCyan : Color.white;
            skinSprite.color = Color.Lerp(skinSprite.color, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }
        if (backGlow)
        {
            float glowAlpha = sprinting ? 0.34f + 0.14f * Mathf.Sin(Time.time * 18f) : 0f;
            backGlow.enabled = glowAlpha > 0.02f;
            backGlow.color = new Color(SprintCyan.r, SprintCyan.g, SprintCyan.b, glowAlpha);
            backGlow.transform.position = transform.position + new Vector3(-direction * 0.18f, 0.15f, 0.05f);
            backGlow.transform.localScale = Vector3.one * (Mathf.Abs(baseScale.x) * 2.6f);
        }
        if (ghosts == null) return;
        if (sprinting && Time.time >= nextGhostTime)
        {
            nextGhostTime = Time.time + 0.05f;
            int idx = ghostCursor;
            ghostCursor = (ghostCursor + 1) % ghosts.Length;
            SpriteRenderer g = ghosts[idx];
            if (g)
            {
                if (skinSprite) g.sprite = skinSprite.sprite;
                g.transform.SetPositionAndRotation(transform.position + new Vector3(0f, 0f, 0.02f), transform.rotation);
                g.transform.localScale = transform.localScale;
                ghostAlpha[idx] = 0.5f;
            }
        }
        for (int i = 0; i < ghosts.Length; i++)
        {
            if (!ghosts[i]) continue;
            if (ghostAlpha[i] > 0f)
            {
                ghostAlpha[i] = Mathf.Max(0f, ghostAlpha[i] - Time.deltaTime * 1.8f);
                ghosts[i].enabled = ghostAlpha[i] > 0.02f;
                ghosts[i].color = new Color(SprintCyan.r, SprintCyan.g, SprintCyan.b, ghostAlpha[i]);
            }
            else if (ghosts[i].enabled)
            {
                ghosts[i].enabled = false;
            }
        }
    }
}

internal sealed class FinalDemoAndroidSkin : MonoBehaviour
{
    private Transform anchor;
    private Collider2D bodyCollider;
    private Rigidbody2D body;
    private ScoreAttackAndroid score;
    private StompableAndroidEnemy stompable;
    private Renderer[] originals;
    private SpriteRenderer sprite;
    private SpriteRenderer sensor;
    private SpriteRenderer scanBeam;
    private Vector3 baseScale;
    private float deathAt = -1;
    private float idlePhase;
    private float lastTrackedX;
    private bool hasTrackedX;
    private float patrolHold;
    private float lastDirection = 1f;
    private bool sentinel;
    private Color beamColor = new Color(1f, 0.22f, 0.12f);
    private float headWorldY = 1.15f;
    private static Sprite glowSprite;
    private static Sprite beamSprite;
    private static Sprite railSprite;
    public void Configure(Transform owner, Collider2D shape, Rigidbody2D rigidbody, ScoreAttackAndroid scoreAndroid,
        StompableAndroidEnemy stompableAndroid, Renderer[] hidden, bool maxScore)
    {
        anchor = owner; bodyCollider = shape; body = rigidbody; score = scoreAndroid; stompable = stompableAndroid;
        originals = hidden; sprite = GetComponent<SpriteRenderer>(); baseScale = transform.localScale;
        int sortBase = sprite ? sprite.sortingOrder : 250;
        // MaxScore Androids are stationary sentinels (magenta guard scan); SpeedRun Androids
        // physically patrol (red-orange directional beam + ground rail).
        sentinel = maxScore;
        beamColor = maxScore ? new Color(1f, 0.32f, 0.9f) : new Color(1f, 0.22f, 0.12f);
        // Dedicated red sensor light near the head so even stationary MaxScore Androids read as "alive".
        idlePhase = Mathf.Repeat(owner ? Mathf.Abs(owner.position.x) * 0.7f : 0f, 6.283f);
        lastDirection = Mathf.Sign(baseScale.x) != 0 ? Mathf.Sign(baseScale.x) : 1f;
        float inv = Mathf.Abs(baseScale.y) > 0.0001f ? 1f / Mathf.Abs(baseScale.y) : 1f;
        // Head height derived from the actual sprite so the eye/beam track any size change.
        headWorldY = (sprite ? sprite.bounds.size.y : 2f) * Mathf.Abs(baseScale.y) * 0.72f;
        GameObject glow = new GameObject("AndroidSensorGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, headWorldY * inv, -0.1f);
        glow.transform.localScale = Vector3.one * (0.62f * inv);
        sensor = glow.AddComponent<SpriteRenderer>();
        sensor.sprite = GlowSprite();
        sensor.color = new Color(1f, 0.16f, 0.12f, 0.9f);
        sensor.sortingOrder = sortBase + 2;

        // Container is the shared visual root, so the beam/rail are NOT flipped/scaled by the
        // Android body and can be driven directly in world space.
        Transform container = transform.parent != null ? transform.parent : transform;

        // Directional scan beam that clearly points where the sentinel is walking.
        GameObject beam = new GameObject("AndroidScanBeam");
        beam.transform.SetParent(container, false);
        scanBeam = beam.AddComponent<SpriteRenderer>();
        scanBeam.sprite = BeamSprite();
        scanBeam.color = new Color(1f, 0.22f, 0.12f, 0f);
        scanBeam.sortingOrder = sortBase + 1;
        scanBeam.enabled = false;

        // Subtle ground rail marking the patrol lane (SpeedRun physical patrols only).
        if (TryGetPatrolRange(owner, out float railWidth))
        {
            float groundY = FinalDemoSkinUtil.GroundY(owner.position, owner.position.y);
            GameObject railGo = new GameObject("AndroidPatrolRail");
            railGo.transform.SetParent(container, false);
            railGo.transform.position = new Vector3(owner.position.x, groundY + 0.06f, 0.2f);
            railGo.transform.localScale = new Vector3(railWidth, 0.09f, 1f);
            SpriteRenderer railRenderer = railGo.AddComponent<SpriteRenderer>();
            railRenderer.sprite = RailSprite();
            railRenderer.color = new Color(1f, 0.38f, 0.2f, 0.24f);
            railRenderer.sortingOrder = sortBase - 6;
        }
    }

    private static bool TryGetPatrolRange(Transform owner, out float railWidth)
    {
        railWidth = 0f;
        if (owner == null) return false;
        DemoAndroidPatrol patrol = owner.GetComponent<DemoAndroidPatrol>();
        if (patrol == null || !patrol.enabled) return false;
        System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.FieldInfo speedField = typeof(DemoAndroidPatrol).GetField("speed", flags);
        System.Reflection.FieldInfo distField = typeof(DemoAndroidPatrol).GetField("patrolDistance", flags);
        float speed = speedField != null ? (float)speedField.GetValue(patrol) : 0f;
        float distance = distField != null ? (float)distField.GetValue(patrol) : 0f;
        if (speed <= 0.01f || distance <= 0.05f) return false;
        railWidth = distance + 0.9f;
        return true;
    }

    private void LateUpdate()
    {
        FinalDemoSkinUtil.Hide(originals);
        if (!anchor || !sprite) return;
        Vector3 baseline = FinalDemoSkinUtil.Position(anchor, bodyCollider);
        baseline.y = FinalDemoSkinUtil.GroundY(anchor.position, baseline.y);
        bool alive = (!score || score.IsAlive) && (!stompable || stompable.IsAlive) && anchor.gameObject.activeInHierarchy;
        if (alive)
        {
            deathAt = -1; sprite.enabled = true; sprite.color = Color.white;
            idlePhase += Time.deltaTime;
            // Track REAL horizontal movement of the physical Android. The patrol drives a
            // kinematic body with MovePosition, so linearVelocity stays 0 and cannot be used
            // to tell "walking" from "idle" — the collider's X delta is the truth. A short hold
            // keeps the "walking" state stable across the brief pause at each turnaround.
            float travelX = hasTrackedX ? baseline.x - lastTrackedX : 0f;
            hasTrackedX = true; lastTrackedX = baseline.x;
            if (Mathf.Abs(travelX) > 0.0009f)
            {
                patrolHold = 0.45f;
                lastDirection = Mathf.Sign(travelX);
            }
            else
            {
                patrolHold = Mathf.Max(0f, patrolHold - Time.deltaTime);
            }
            bool patrolling = patrolHold > 0f;
            float direction = lastDirection != 0f ? lastDirection : Mathf.Sign(baseScale.x);
            if (patrolling)
            {
                // Unmistakable "walking" read: face travel, lean in, brisk step bob + squash.
                float step = Mathf.Sin(idlePhase * 10f);
                baseline.y += Mathf.Abs(step) * 0.06f;
                transform.position = baseline;
                float squash = 1f + step * 0.05f;
                transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * direction, baseScale.y * squash, 1);
                transform.rotation = Quaternion.Euler(0, 0, -direction * 7f);
            }
            else
            {
                // Stationary Androids (MaxScore) breathe with a slow sensor sway + hover.
                baseline.y += Mathf.Abs(Mathf.Sin(idlePhase * 1.5f)) * 0.06f;
                transform.position = baseline;
                transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * direction, baseScale.y, 1);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(idlePhase * 1.9f) * 4.5f);
            }
            UpdateScanBeam(patrolling, direction);
            if (sensor)
            {
                sensor.enabled = true;
                float baseA = patrolling ? 0.75f : 0.5f;
                float amp = patrolling ? 0.25f : 0.45f;
                float pulse = baseA + amp * Mathf.Sin(idlePhase * (patrolling ? 5f : 3.2f));
                sensor.color = new Color(1f, 0.16f, 0.12f, Mathf.Clamp01(pulse));
                float inv = Mathf.Abs(baseScale.y) > 0.0001f ? 1f / Mathf.Abs(baseScale.y) : 1f;
                float sizeBoost = patrolling ? 1.15f : 1f;
                sensor.transform.localScale = Vector3.one * (0.62f * inv * sizeBoost * (0.85f + 0.3f * pulse));
            }
            return;
        }
        if (sensor) sensor.enabled = false;
        if (scanBeam) scanBeam.enabled = false;
        hasTrackedX = false;
        patrolHold = 0f;
        transform.position = baseline;
        if (deathAt < 0) deathAt = Time.unscaledTime;
        float t = Mathf.Clamp01((Time.unscaledTime - deathAt) / .45f);
        transform.localScale = new Vector3(baseScale.x * (1 + .12f * t), baseScale.y * Mathf.Lerp(.45f, .08f, t), 1);
        sprite.color = new Color(1, 1, 1, 1 - t); sprite.enabled = t < 1;
    }

    private void UpdateScanBeam(bool patrolling, float direction)
    {
        if (!scanBeam) return;
        if (patrolling)
        {
            // SpeedRun: aggressive horizontal beam pointing exactly where it walks.
            scanBeam.enabled = true;
            float length = 1.3f + 0.4f * Mathf.Abs(Mathf.Sin(idlePhase * 5f));
            float bright = 0.5f + 0.35f * (0.5f + 0.5f * Mathf.Sin(idlePhase * 8f));
            scanBeam.color = new Color(beamColor.r, beamColor.g, beamColor.b, Mathf.Clamp01(bright));
            scanBeam.transform.position = new Vector3(
                transform.position.x + direction * 0.32f,
                transform.position.y + headWorldY,
                transform.position.z - 0.05f);
            scanBeam.transform.rotation = Quaternion.identity;
            scanBeam.transform.localScale = new Vector3(direction * length, 0.6f, 1f);
            return;
        }
        if (sentinel)
        {
            // MaxScore: stationary sentinel that slowly sweeps a magenta guard scan downward,
            // reading as "watching for a stomp" rather than patrolling.
            scanBeam.enabled = true;
            float facing = Mathf.Sign(baseScale.x) != 0 ? Mathf.Sign(baseScale.x) : 1f;
            float sweep = -18f + Mathf.Sin(idlePhase * 1.2f) * 30f;
            float length = 1.4f + 0.25f * Mathf.Sin(idlePhase * 2.6f);
            float bright = 0.32f + 0.22f * (0.5f + 0.5f * Mathf.Sin(idlePhase * 3f));
            scanBeam.color = new Color(beamColor.r, beamColor.g, beamColor.b, Mathf.Clamp01(bright));
            scanBeam.transform.position = new Vector3(
                transform.position.x + facing * 0.26f,
                transform.position.y + headWorldY,
                transform.position.z - 0.05f);
            scanBeam.transform.rotation = Quaternion.Euler(0f, 0f, sweep * facing);
            scanBeam.transform.localScale = new Vector3(facing * length, 0.55f, 1f);
            return;
        }
        scanBeam.enabled = false;
    }

    private static Sprite GlowSprite()
    {
        if (glowSprite) return glowSprite;
        const int s = 24;
        Texture2D texture = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2 center = new Vector2((s - 1) * 0.5f, (s - 1) * 0.5f);
        float radius = s * 0.5f;
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
            float alpha = Mathf.Clamp01(1f - distance);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
        }
        texture.Apply();
        glowSprite = Sprite.Create(texture, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 24f);
        glowSprite.name = "FinalDemoSensorGlow";
        return glowSprite;
    }

    private static Sprite BeamSprite()
    {
        if (beamSprite) return beamSprite;
        const int w = 32, h = 8;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float alongFade = 1f - (x / (float)(w - 1));                       // bright at base, fade to tip
            float acrossFade = 1f - Mathf.Abs((y - (h - 1) * 0.5f) / ((h - 1) * 0.5f));
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alongFade * acrossFade * acrossFade));
        }
        texture.Apply();
        // Pivot at the left edge so the beam extends outward from the Android's head.
        beamSprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(0f, 0.5f), 32f);
        beamSprite.name = "FinalDemoScanBeam";
        return beamSprite;
    }

    private static Sprite RailSprite()
    {
        if (railSprite) return railSprite;
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        railSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        railSprite.name = "FinalDemoPatrolRail";
        return railSprite;
    }
}

internal sealed class FinalDemoPickupSkin : MonoBehaviour
{
    private Transform anchor;
    private ScoreAttackCoin coin;
    private FinalDemoVisualCollectible collectible;
    private Renderer[] originals;
    private float speed;
    private Vector3 baseScale;
    private SpriteRenderer glow;
    private Color glowColor;
    public void Configure(Transform owner, ScoreAttackCoin scoreCoin, FinalDemoVisualCollectible item,
        Renderer[] hidden, float pulseSpeed, bool gold)
    {
        anchor = owner; coin = scoreCoin; collectible = item; originals = hidden; speed = pulseSpeed; baseScale = transform.localScale;
        SpriteRenderer self = GetComponent<SpriteRenderer>();
        // Soft matching halo so gold (high/jump) and cyan (low/ground) cells read cleanly.
        glowColor = gold ? new Color(1f, 0.82f, 0.2f, 0.44f) : new Color(0.3f, 0.9f, 1f, 0.44f);
        GameObject glowGo = new GameObject("PickupGlow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
        glowGo.transform.localScale = Vector3.one * 2.9f;
        glow = glowGo.AddComponent<SpriteRenderer>();
        glow.sprite = FinalDemoSkinUtil.Glow();
        glow.color = glowColor;
        glow.sortingOrder = (self ? self.sortingOrder : 230) - 1;
    }
    private void LateUpdate()
    {
        FinalDemoSkinUtil.Hide(originals);
        if (!anchor) { gameObject.SetActive(false); return; }
        bool visible = anchor.gameObject.activeInHierarchy && (!coin || coin.IsAvailable) && (!collectible || !collectible.IsCollected);
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer) renderer.enabled = visible;
        if (glow)
        {
            glow.enabled = visible;
            if (visible)
            {
                float a = glowColor.a * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * speed * 3f));
                glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);
            }
        }
        if (!visible) return;
        transform.position = new Vector3(anchor.position.x, anchor.position.y, 0);
        transform.localScale = baseScale * (1 + Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2) * .035f);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * speed) * 2);
    }
}

internal sealed class FinalDemoGoalSkin : MonoBehaviour
{
    private static readonly Color OpenGlow = new Color(0.3f, 1f, 0.85f, 1f);
    private static readonly Color LockedGlow = new Color(1f, 0.28f, 0.7f, 1f);
    private Transform anchor;
    private SpriteRenderer spriteRenderer;
    private ScoreAttackGoalLock goalLock;
    private ScoreAttackManager manager;
    private Sprite open, locked;
    private Renderer[] originals;
    private SpriteRenderer glow;
    public void Configure(Transform owner, SpriteRenderer target, ScoreAttackGoalLock lockComponent,
        ScoreAttackManager scoreManager, Sprite openSprite, Sprite lockedSprite, Renderer[] hidden)
    {
        anchor = owner; spriteRenderer = target; goalLock = lockComponent; manager = scoreManager;
        open = openSprite; locked = lockedSprite; originals = hidden;
        GameObject glowGo = new GameObject("GoalGlow");
        glowGo.transform.SetParent(transform, false);
        glowGo.transform.localPosition = new Vector3(0f, 1.1f, 0.1f);
        glow = glowGo.AddComponent<SpriteRenderer>();
        glow.sprite = FinalDemoSkinUtil.Glow();
        glow.sortingOrder = (spriteRenderer ? spriteRenderer.sortingOrder : 270) - 1;
    }
    private void LateUpdate()
    {
        FinalDemoSkinUtil.Hide(originals);
        if (!anchor || !spriteRenderer) return;
        float groundY = FinalDemoSkinUtil.GroundY(anchor.position, anchor.position.y);
        transform.position = new Vector3(anchor.position.x, groundY, 0);
        transform.rotation = anchor.rotation;
        bool lockedNow = goalLock && manager && !manager.ObjectivesComplete;
        spriteRenderer.sprite = lockedNow ? locked : open;
        spriteRenderer.enabled = anchor.gameObject.activeInHierarchy;
        if (glow)
        {
            glow.enabled = spriteRenderer.enabled;
            Color c = lockedNow ? LockedGlow : OpenGlow;
            float pulse = 0.35f + 0.25f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f));
            glow.color = new Color(c.r, c.g, c.b, pulse);
            glow.transform.localScale = Vector3.one * (3.4f + 0.4f * Mathf.Sin(Time.unscaledTime * 2.4f));
        }
    }
}

internal sealed class FinalDemoBackgroundSkin : MonoBehaviour
{
    private Camera targetCamera;
    private SpriteRenderer spriteRenderer;
    public void Configure(Camera target, SpriteRenderer sprite) { targetCamera = target; spriteRenderer = sprite; Sync(); }
    private void LateUpdate() { Sync(); }
    private void Sync()
    {
        if (!targetCamera || !spriteRenderer || !spriteRenderer.sprite) return;
        float height = targetCamera.orthographic ? targetCamera.orthographicSize * 2.08f : 12;
        float width = height * Mathf.Max(1, targetCamera.aspect);
        Vector2 native = spriteRenderer.sprite.bounds.size;
        float scale = Mathf.Max(width / native.x, height / native.y);
        transform.localScale = new Vector3(scale, scale, 1);
        transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, 8);
    }
}

/// <summary>
/// Purely decorative far-background: a clean parallax starfield with a soft glowing moon and a
/// few drifting accent lights. No skyline blocks, no colliders, no gameplay effect.
/// </summary>
internal sealed class FinalDemoAmbientBackdrop : MonoBehaviour
{
    private Camera cam;
    private Transform[] layers;
    private float[] layerDepth;
    private readonly List<SpriteRenderer> twinklers = new List<SpriteRenderer>();
    private readonly List<float> twinklePhase = new List<float>();
    private readonly List<float> twinkleBase = new List<float>();
    private readonly List<Transform> drifters = new List<Transform>();
    private readonly List<float> drifterPhase = new List<float>();
    private readonly List<float> drifterBaseY = new List<float>();

    public void Configure(Camera camera, bool maxScore)
    {
        cam = camera;
        Color starColor = maxScore ? new Color(0.98f, 0.9f, 0.98f, 1f) : new Color(0.85f, 0.95f, 1f, 1f);
        Color accentLight = maxScore ? new Color(1f, 0.55f, 0.9f, 0.7f) : new Color(0.45f, 0.95f, 1f, 0.7f);
        Color moonColor = maxScore ? new Color(1f, 0.9f, 0.95f, 1f) : new Color(0.88f, 0.96f, 1f, 1f);

        // Three depth planes: distant stars barely move, near accents drift more.
        layerDepth = new[] { 0.95f, 0.88f, 0.74f };
        layers = new Transform[layerDepth.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            GameObject r = new GameObject("BackdropLayer_" + i);
            r.transform.SetParent(transform, false);
            layers[i] = r.transform;
        }

        // Soft glowing moon + halo on the far plane.
        SpriteRenderer moonHalo = MakeSprite(layers[0], "BackdropMoonHalo", FinalDemoSkinUtil.Glow(),
            new Color(moonColor.r, moonColor.g, moonColor.b, 0.16f), -931);
        moonHalo.transform.localPosition = new Vector3(15f, 9.4f, 6.2f);
        moonHalo.transform.localScale = Vector3.one * 7f;
        SpriteRenderer moon = MakeSprite(layers[0], "BackdropMoon", FinalDemoSkinUtil.Glow(),
            new Color(moonColor.r, moonColor.g, moonColor.b, 0.92f), -930);
        moon.transform.localPosition = new Vector3(15f, 9.4f, 6f);
        moon.transform.localScale = Vector3.one * 3.1f;

        AddStars(layers[0], -925, 44, -30f, 145f, 3.5f, 13f, 0.05f, 0.16f, starColor, 11);
        AddStars(layers[1], -920, 28, -25f, 140f, 3f, 12f, 0.09f, 0.24f, starColor, 29);
        AddDrifters(layers[2], -910, 6, -18f, 132f, 5.5f, 10.5f, accentLight);

        twinklers.TrimExcess();
    }

    private void AddStars(Transform layer, int order, int count, float x0, float x1, float y0, float y1,
        float minA, float maxA, Color color, int seed)
    {
        for (int i = 0; i < count; i++)
        {
            int n = seed * 1000 + i;
            float x = Mathf.Lerp(x0, x1, Hash(n));
            float y = Mathf.Lerp(y0, y1, Hash(n + 7));
            float size = 0.05f + Hash(n + 3) * 0.13f;
            float alpha = Mathf.Lerp(minA, maxA, Hash(n + 5));
            SpriteRenderer star = MakeSprite(layer, "Star_" + order + "_" + i, FinalDemoSkinUtil.Glow(),
                new Color(color.r, color.g, color.b, alpha), order);
            star.transform.localPosition = new Vector3(x, y, 6f);
            star.transform.localScale = Vector3.one * size;
            twinklers.Add(star);
            twinklePhase.Add(Hash(n + 9) * 6.283f);
            twinkleBase.Add(alpha);
        }
    }

    private void AddDrifters(Transform layer, int order, int count, float x0, float x1, float y0, float y1, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            int n = 700 + i;
            float x = Mathf.Lerp(x0, x1, (i + 0.5f) / count);
            float y = Mathf.Lerp(y0, y1, Hash(n));
            SpriteRenderer light = MakeSprite(layer, "BackdropLight_" + i, FinalDemoSkinUtil.Glow(), color, order);
            light.transform.localPosition = new Vector3(x, y, 5f);
            light.transform.localScale = Vector3.one * (0.32f + Hash(n + 2) * 0.16f);
            drifters.Add(light.transform);
            drifterPhase.Add(i * 0.9f);
            drifterBaseY.Add(y);
        }
    }

    private static float Hash(int n)
    {
        float s = Mathf.Sin(n * 12.9898f) * 43758.5453f;
        return s - Mathf.Floor(s);
    }

    private SpriteRenderer MakeSprite(Transform parent, string name, Sprite sprite, Color color, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private void LateUpdate()
    {
        if (!cam) return;
        float camX = cam.transform.position.x;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i]) layers[i].position = new Vector3(camX * layerDepth[i], 0f, 0f);
        }
        float t = Time.unscaledTime;
        for (int i = 0; i < twinklers.Count; i++)
        {
            SpriteRenderer star = twinklers[i];
            if (!star) continue;
            float a = twinkleBase[i] * (0.55f + 0.45f * Mathf.Sin(t * 1.4f + twinklePhase[i]));
            Color c = star.color;
            star.color = new Color(c.r, c.g, c.b, Mathf.Max(0f, a));
        }
        for (int i = 0; i < drifters.Count; i++)
        {
            if (!drifters[i]) continue;
            drifterPhase[i] += Time.deltaTime * 0.5f;
            Vector3 p = drifters[i].localPosition;
            p.y = drifterBaseY[i] + Mathf.Sin(drifterPhase[i]) * 0.6f;
            drifters[i].localPosition = p;
        }
    }
}
