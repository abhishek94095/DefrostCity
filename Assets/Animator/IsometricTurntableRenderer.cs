using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[ExecuteInEditMode]
public class IsometricTurntableRenderer : MonoBehaviour
{
    [Header("Batch Input")]
    public string inputFolder = "Models";
    public float spacing = 100f;

    [Header("Camera")]
    public float elevationAngle = 30f;
    public float stepDegrees = 30f;

    [Header("Render")]
    public string outputRoot = "Renders";
    public int renderWidth = 512;
    public int renderHeight = 512;
    public int antiAliasing = 4;
    public bool transparentBackground = true;

    class RenderItem
    {
        public GameObject obj;
        public Transform pivot;
    }

    private List<RenderItem> items = new List<RenderItem>();

    private int currentModelIndex = 0;
    private int currentAngleIndex = 0;
    private int totalSteps;

    private Camera cam;
    private bool isRendering = false;

    private GameObject doneParent;

    // ─────────────────────────────────────────────
    // START (CALLED FROM BUTTON)
    // ─────────────────────────────────────────────
#if UNITY_EDITOR
    public void StartRender()
    {
        cam = GetComponent<Camera>();

        if (cam == null)
        {
            Debug.LogError("No Camera found.");
            return;
        }

        SetupDoneParent();
        SpawnAllModels();

        totalSteps = Mathf.RoundToInt(360f / stepDegrees);

        currentModelIndex = 0;
        currentAngleIndex = 0;

        isRendering = true;

        EditorApplication.update += OnEditorUpdate;
    }
#endif

    // ─────────────────────────────────────────────
    void SetupDoneParent()
    {
        GameObject existing = GameObject.Find("Done");

        if (existing != null)
            doneParent = existing;
        else
            doneParent = new GameObject("Done");
    }

    // ─────────────────────────────────────────────
    void SpawnAllModels()
    {
        items.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/" + inputFolder });

        float offsetX = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            obj.transform.position = new Vector3(offsetX, 0, 0);

            Transform pivot = CreateRenderPivotSafe(obj);

            obj.SetActive(false); // 🔥 initially disable everything

            items.Add(new RenderItem
            {
                obj = obj,
                pivot = pivot
            });

            offsetX += spacing;
        }

        Debug.Log($"Spawned {items.Count} models.");
    }

    // ─────────────────────────────────────────────
    void OnEditorUpdate()
    {
        if (!isRendering)
            return;

        if (currentModelIndex >= items.Count)
        {
            Debug.Log("✅ Rendering Complete");
            EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "..", outputRoot));
            EditorApplication.update -= OnEditorUpdate;
            isRendering = false;
            return;
        }

        var item = items[currentModelIndex];

        if (item == null || item.obj == null || item.pivot == null)
        {
            currentModelIndex++;
            currentAngleIndex = 0;
            return;
        }

        // 🔥 Activate ONLY current object
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].obj != null)
                items[i].obj.SetActive(i == currentModelIndex);
        }

        float yaw = currentAngleIndex * stepDegrees;

        PositionCamera(cam, yaw, item.obj, item.pivot);

        RenderAndSave(cam, yaw, item.obj.name);

        currentAngleIndex++;

        // ─────────────────────────────
        // FINISHED THIS MODEL
        // ─────────────────────────────
        if (currentAngleIndex >= totalSteps)
        {
            currentAngleIndex = 0;

            // ✅ Move to Done folder
            item.obj.transform.SetParent(doneParent.transform);

            // ❌ Disable after render
            item.obj.SetActive(false);

            currentModelIndex++;
        }
    }

    // ─────────────────────────────────────────────
    Transform CreateRenderPivotSafe(GameObject obj)
    {
        if (obj == null) return null;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
            return obj.transform;

        Bounds bounds = renderers[0].bounds;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            bounds.Encapsulate(r.bounds);
        }

        GameObject pivot = new GameObject(obj.name + "_Pivot");
        pivot.transform.position = bounds.center;

        return pivot.transform;
    }

    // ─────────────────────────────────────────────
    void PositionCamera(Camera cam, float yaw, GameObject obj, Transform pivot)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            bounds.Encapsulate(r.bounds);
        }

        float size = bounds.extents.magnitude;
        float radius = size * 2.5f;

        float yawRad = yaw * Mathf.Deg2Rad;
        float elevRad = elevationAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            radius * Mathf.Cos(elevRad) * Mathf.Sin(yawRad),
            radius * Mathf.Sin(elevRad),
            radius * Mathf.Cos(elevRad) * Mathf.Cos(yawRad)
        );

        transform.position = pivot.position + offset;
        transform.LookAt(pivot.position);
    }

    // ─────────────────────────────────────────────
    void RenderAndSave(Camera cam, float yaw, string modelName)
    {
        string folder = ((int)yaw).ToString("D3");

        string dir = Path.Combine(Application.dataPath, "..", outputRoot, modelName, folder);
        Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, "render.png");

        RenderTexture rt = new RenderTexture(renderWidth, renderHeight, 32, RenderTextureFormat.ARGB32);
        rt.antiAliasing = antiAliasing;
        rt.Create();

        var oldFlags = cam.clearFlags;
        var oldColor = cam.backgroundColor;

        if (transparentBackground)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
        }

        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = null;

        cam.clearFlags = oldFlags;
        cam.backgroundColor = oldColor;

        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(renderWidth, renderHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, renderWidth, renderHeight), 0, 0);
        tex.Apply();

        RenderTexture.active = null;

        File.WriteAllBytes(path, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(IsometricTurntableRenderer))]
public class IsometricTurntableRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        var renderer = (IsometricTurntableRenderer)target;

        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("▶ Render All Models", GUILayout.Height(40)))
        {
            renderer.StartRender();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif