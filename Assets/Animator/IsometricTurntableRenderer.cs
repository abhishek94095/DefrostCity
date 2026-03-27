using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Isometric Turntable Renderer
/// -------------------------------------------------------
/// Attach this script to your isometric Camera GameObject.
/// Lights should already be parented to the camera so they
/// travel with it automatically.
///
/// Usage:
///   1. Select your Camera in the Hierarchy.
///   2. Add this component (Component > Rendering > Isometric Turntable Renderer).
///   3. Set the Target to the model/pivot you want to orbit around.
///   4. Adjust Resolution, Output Root, and any other settings.
///   5. In the Inspector click "Render All Angles" or use the
///      menu  Tools > Isometric Turntable > Render All Angles.
/// </summary>
[ExecuteInEditMode]
public class IsometricTurntableRenderer : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The model (or an empty pivot at its centre) the camera orbits around.")]
    public Transform target;

    [Header("Camera Orbit Settings")]
    [Tooltip("Orbit radius – distance from target centre to camera.")]
    public float orbitRadius = 10f;

    [Tooltip("Fixed elevation angle above the horizon (isometric feel: ~30–45°).")]
    public float elevationAngle = 30f;

    [Tooltip("Degrees per step. 30 gives 12 frames (0,30,60,...,330).")]
    public float stepDegrees = 30f;

    [Header("Render Output")]
    [Tooltip("Root folder for all renders (relative to project root).")]
    public string outputRoot = "Renders";

    [Tooltip("Sub-folder name for this model batch (e.g. 'Knight', 'Barrel').")]
    public string batchName = "Model_01";

    [Tooltip("Render width in pixels.")]
    public int renderWidth = 512;

    [Tooltip("Render height in pixels.")]
    public int renderHeight = 512;

    [Tooltip("Anti-aliasing sample count (1, 2, 4, or 8).")]
    public int antiAliasing = 4;

    [Tooltip("PNG background is transparent when true, opaque black when false.")]
    public bool transparentBackground = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Editor Button
    // ─────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Render All Angles")]
    public void RenderAllAngles()
    {
        if (target == null)
        {
            Debug.LogError("[TurntableRenderer] No Target assigned. Please assign the model transform.");
            return;
        }

        Camera cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("[TurntableRenderer] No Camera component found on this GameObject.");
            return;
        }

        // Store original camera transform so we can restore it afterwards
        Vector3 originalPosition  = transform.position;
        Quaternion originalRotation = transform.rotation;

        int totalSteps = Mathf.RoundToInt(360f / stepDegrees);
        int rendered   = 0;

        for (int i = 0; i < totalSteps; i++)
        {
            float yaw = i * stepDegrees;          // 0, 30, 60 … 330
            PositionCamera(cam, yaw);
            RenderAndSave(cam, yaw);
            rendered++;

            // Keep the editor from freezing on large batches
            EditorUtility.DisplayProgressBar(
                "Turntable Render",
                $"Rendering angle {yaw:000}°  ({rendered}/{totalSteps})",
                (float)rendered / totalSteps);
        }

        EditorUtility.ClearProgressBar();

        // Restore camera to where it was
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputRoot, batchName));
        Debug.Log($"[TurntableRenderer] Done! {rendered} frames saved to:\n{fullPath}");
        EditorUtility.RevealInFinder(fullPath);
    }
#endif

    // ─────────────────────────────────────────────────────────────────────────
    //  Core helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves and rotates the camera to orbit the target at the given yaw angle,
    /// keeping all other dimensions (elevation, radius, FOV, etc.) constant.
    /// </summary>
    private void PositionCamera(Camera cam, float yaw)
    {
        // Convert spherical coords → world position
        float yawRad  = yaw           * Mathf.Deg2Rad;
        float elevRad = elevationAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            orbitRadius * Mathf.Cos(elevRad) * Mathf.Sin(yawRad),
            orbitRadius * Mathf.Sin(elevRad),
            orbitRadius * Mathf.Cos(elevRad) * Mathf.Cos(yawRad)
        );

        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }

    /// <summary>
    /// Renders a single frame from the camera and writes a PNG to disk.
    /// Folder name is zero-padded to match "000" format (e.g. "030", "120").
    /// </summary>
    private void RenderAndSave(Camera cam, float yaw)
    {
        // Build output path:  <outputRoot>/<batchName>/<angle>/frame.png
        string folderName = ((int)yaw).ToString("D3");   // "000", "030", "330"
        string dirPath    = Path.Combine(Application.dataPath, "..", outputRoot, batchName, folderName);
        Directory.CreateDirectory(dirPath);

        string filePath = Path.Combine(dirPath, "render.png");

        // 32-bit depth + ARGB32 are both required for a real alpha channel.
        // The old 24-bit depth buffer had no alpha at all — transparent renders
        // came out black because there was nowhere for the alpha data to live.
        RenderTexture rt = new RenderTexture(renderWidth, renderHeight, 32,
            RenderTextureFormat.ARGB32);
        rt.antiAliasing = antiAliasing;
        rt.Create();

        CameraClearFlags originalClearFlags = cam.clearFlags;
        Color             originalBgColor   = cam.backgroundColor;

        // Force Solid Color + fully transparent black regardless of what the
        // camera has set in its Inspector. Skybox and Depth clear modes ignore
        // backgroundColor.a entirely and will always produce a black background.
        if (transparentBackground)
        {
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }

        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture   = null;

        // Restore camera state immediately after render
        cam.clearFlags      = originalClearFlags;
        cam.backgroundColor = originalBgColor;

        // Read pixels back into a Texture2D.
        // Must be RGBA32 — RGB24 silently discards the alpha channel, making
        // the PNG encoder write a fully opaque image.
        RenderTexture.active = rt;
        TextureFormat fmt = transparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24;
        Texture2D tex     = new Texture2D(renderWidth, renderHeight, fmt, false);
        tex.ReadPixels(new Rect(0, 0, renderWidth, renderHeight), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        // Write PNG — EncodeToPNG preserves the alpha channel when the texture is RGBA32
        File.WriteAllBytes(filePath, tex.EncodeToPNG());

        // Cleanup — Release() frees GPU memory before DestroyImmediate,
        // important when rendering many frames back-to-back
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);

        Debug.Log($"[TurntableRenderer] Saved angle {yaw:000}° → {filePath}");
    }
}


// ─────────────────────────────────────────────────────────────────────────────
//  Custom Inspector + Menu Item
// ─────────────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
[CustomEditor(typeof(IsometricTurntableRenderer))]
public class IsometricTurntableRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12);

        IsometricTurntableRenderer renderer = (IsometricTurntableRenderer)target;

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶  Render All Angles", GUILayout.Height(36)))
        {
            renderer.RenderAllAngles();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Output:  <Project>/" + renderer.outputRoot + "/" + renderer.batchName + "/\n" +
            "Folders: 000  030  060  090  120  150  180  210  240  270  300  330\n" +
            "Each folder contains render.png",
            MessageType.Info);
    }
}

public static class TurntableMenu
{
    [MenuItem("Tools/Isometric Turntable/Render All Angles")]
    public static void RenderFromMenu()
    {
        IsometricTurntableRenderer renderer = Object.FindObjectOfType<IsometricTurntableRenderer>();
        if (renderer == null)
        {
            Debug.LogError("[TurntableRenderer] No IsometricTurntableRenderer found in scene.");
            return;
        }
        renderer.RenderAllAngles();
    }
}
#endif
