using UnityEngine;
using System.Collections;
using System;

public class TerrainPainter : MonoBehaviour
{
    public Terrain terrain;

    const int FREEZE_LAYER = 0;
    const int GRASS_LAYER = 4;

    public int brushSize = 5;
    public Transform worldPos;

    public float multiplier = 1f;

    // ─────────────────────────────────────
    // ❄️ FREEZE SYSTEM

    [Header("Freeze Settings")]
    [Tooltip("1 = full freeze in 1 sec, 0.166 = ~6 sec")]
    public float freezeSpeed = 0.166f;

    public bool stopFreezing = false;

    private float freezeProgress = 0f;
    private Action onFreezeComplete;

    public void Start()
    {
        StartPaintTest();
    }

    // ─────────────────────────────────────
    // 🔥 MELT (GRASS)

    [ContextMenu("Paint Test")]
    public void StartPaintTest()
    {
        StartPaint(brushSize);
    }

    public void StartPaint(int brushSize)
    {
        this.brushSize = brushSize;
        StartCoroutine(PaintRoutine(worldPos.position));
    }

    IEnumerator PaintRoutine(Vector3 worldPos, float duration = 1.5f)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float radius = Mathf.Lerp(1f, brushSize, t);

            PaintCircle(worldPos, radius);

            // 🔥 CRITICAL FIX: sync freeze with melt
            float meltRatio = radius / brushSize;
            freezeProgress = Mathf.Clamp01(1f - meltRatio);

            elapsed += Time.deltaTime;
            yield return null;
        }

        PaintCircle(worldPos, brushSize);

        // fully melted → no freeze
        freezeProgress = 0f;
    }

    void PaintCircle(Vector3 worldPos, float radius)
    {
        TerrainData data = terrain.terrainData;

        int mapWidth = data.alphamapWidth;
        int mapHeight = data.alphamapHeight;

        Vector3 terrainPos = worldPos - terrain.transform.position;

        int x = (int)((terrainPos.x / data.size.x) * mapWidth);
        int z = (int)((terrainPos.z / data.size.z) * mapHeight);

        int brush = Mathf.RoundToInt(radius);

        float[,,] alpha = data.GetAlphamaps(0, 0, mapWidth, mapHeight);

        for (int i = -brush; i <= brush; i++)
        {
            for (int j = -brush; j <= brush; j++)
            {
                int px = x + i;
                int pz = z + j;

                if (px < 0 || px >= mapWidth || pz < 0 || pz >= mapHeight)
                    continue;

                float dist = Mathf.Sqrt(i * i + j * j);
                if (dist > brush)
                    continue;

                float normalized = dist / brush;
                float strength = (1f - normalized) * multiplier;
                strength = Mathf.Clamp01(strength);

                for (int l = 0; l < alpha.GetLength(2); l++)
                    alpha[pz, px, l] *= (1 - strength);

                alpha[pz, px, GRASS_LAYER] += strength;
            }
        }

        data.SetAlphamaps(0, 0, alpha);
    }

    // ─────────────────────────────────────
    // ❄️ FREEZE CONTROL

    [ContextMenu("Start Freezing")]
    public void DebugStartFreeze()
    {
        StartFreezing(() => Debug.Log("Fully Frozen ✅"));
    }

    [ContextMenu("Stop Freezing")]
    public void DebugStopFreeze()
    {
        stopFreezing = true;
    }

    [ContextMenu("Resume Freezing")]
    public void DebugResumeFreeze()
    {
        stopFreezing = false;
    }

    public void StartFreezing(Action onComplete = null)
    {
        onFreezeComplete = onComplete;
        stopFreezing = false;
    }

    public void StopFreezing()
    {
        stopFreezing = true;
    }

    void Update()
    {
        if (stopFreezing)
            return;

        // 🔥 SPEED BASED FREEZE
        freezeProgress += freezeSpeed * Time.deltaTime;
        freezeProgress = Mathf.Clamp01(freezeProgress);

        float outer = brushSize;
        float inner = Mathf.Lerp(brushSize, 0f, freezeProgress);

        PaintFreezeRing(worldPos.position, outer, inner);

        if (freezeProgress >= 1f)
        {
            onFreezeComplete?.Invoke();
        }
    }

    // ─────────────────────────────────────
    // ❄️ FREEZE RING

    void PaintFreezeRing(Vector3 worldPos, float outerRadius, float innerRadius)
    {
        TerrainData data = terrain.terrainData;

        int mapWidth = data.alphamapWidth;
        int mapHeight = data.alphamapHeight;

        Vector3 terrainPos = worldPos - terrain.transform.position;

        int x = (int)((terrainPos.x / data.size.x) * mapWidth);
        int z = (int)((terrainPos.z / data.size.z) * mapHeight);

        int outer = Mathf.RoundToInt(outerRadius);
        int inner = Mathf.RoundToInt(innerRadius);

        float[,,] alpha = data.GetAlphamaps(0, 0, mapWidth, mapHeight);

        for (int i = -outer; i <= outer; i++)
        {
            for (int j = -outer; j <= outer; j++)
            {
                int px = x + i;
                int pz = z + j;

                if (px < 0 || px >= mapWidth || pz < 0 || pz >= mapHeight)
                    continue;

                float dist = Mathf.Sqrt(i * i + j * j);

                if (dist > outer || dist < inner)
                    continue;

                float normalized = (dist - inner) / (outer - inner);
                normalized = Mathf.SmoothStep(0, 1, normalized);

                float strength = (1f - normalized) * multiplier;
                strength = Mathf.Clamp01(strength);

                for (int l = 0; l < alpha.GetLength(2); l++)
                    alpha[pz, px, l] *= (1 - strength);

                alpha[pz, px, FREEZE_LAYER] += strength;
            }
        }

        data.SetAlphamaps(0, 0, alpha);
    }

    // ─────────────────────────────────────
    // 🔁 RESET

    [ContextMenu("Reset Original (Snow)")]
    public void ResetToOriginal()
    {
        TerrainData data = terrain.terrainData;

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        int layers = data.alphamapLayers;

        float[,,] alpha = new float[height, width, layers];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int l = 0; l < layers; l++)
                    alpha[z, x, l] = 0f;

                alpha[z, x, FREEZE_LAYER] = 1f;
            }
        }

        data.SetAlphamaps(0, 0, alpha);

        freezeProgress = 0f;
        stopFreezing = true;
    }
}