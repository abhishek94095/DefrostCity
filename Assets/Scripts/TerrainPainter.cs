using UnityEngine;
using System;
using System.Collections;

public class TerrainPainter : MonoBehaviour
{
    public Terrain terrain;
    public Transform worldPos;

    const int FREEZE_LAYER = 4;
    const int GRASS_LAYER  = 0;

    public int   brushSize   = 5;
    public float multiplier  = 1f;
    public float freezeSpeed = 0.166f;
    public bool  stopFreezing = false;
    public bool  isGameOver   = false;
    [Header("UI")]
    public SnowMeter snowMeter;
    [Header("Freeze Ring")]
    [Tooltip("How wide the freeze ring is in alphamap pixels")]
    public float ringWidth = 100f;
    private float freezeProgress = 0f;
    private Action onFreezeComplete;

    // ── Throttle state ─────────────────────────
    private int   _skipFrame = 0;
    const   int   FLUSH_EVERY = 2;          // write every N frames

    // ── Dirty-rect helpers ─────────────────────
    /// Read only the brush bounding box, not the whole map.
    private float[,,] ReadDirtyRegion(int cx, int cz, int radius,
                                       out int x0, out int z0,
                                       out int w,  out int h)
    {
        TerrainData data = terrain.terrainData;
        x0 = Mathf.Max(0, cx - radius);
        z0 = Mathf.Max(0, cz - radius);
        int x1 = Mathf.Min(data.alphamapWidth  - 1, cx + radius);
        int z1 = Mathf.Min(data.alphamapHeight - 1, cz + radius);
        w  = x1 - x0 + 1;
        h  = z1 - z0 + 1;
        return data.GetAlphamaps(x0, z0, w, h);
    }

    /// Convert world position → alphamap cell centre.
    private (int cx, int cz) WorldToAlphamap(Vector3 wp)
    {
        TerrainData data = terrain.terrainData;
        Vector3 local = wp - terrain.transform.position;
        int cx = (int)((local.x / data.size.x) * data.alphamapWidth);
        int cz = (int)((local.z / data.size.z) * data.alphamapHeight);
        return (cx, cz);
    }

    // ── Paint (melt / grass) ───────────────────
    public void StartPaint(int size) { brushSize = size; StartCoroutine(PaintRoutine(worldPos.position)); }

    IEnumerator PaintRoutine(Vector3 wp, float duration = 1.5f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t      = elapsed / duration;
            float radius = Mathf.Lerp(1f, brushSize, t);
            PaintCircleFast(wp, radius, GRASS_LAYER);

            // As melt progresses radius grows → freezeProgress drops → temp rises
            freezeProgress = Mathf.Clamp01(1f - radius / brushSize);
            snowMeter?.SetFreezeProgress(freezeProgress); // ← was missing

            elapsed += Time.deltaTime;
            yield return null;
        }

        PaintCircleFast(wp, brushSize, GRASS_LAYER);
        freezeProgress = 0f;
        snowMeter?.SetFreezeProgress(0f);
    }

    void PaintCircleFast(Vector3 wp, float radius, int targetLayer)
    {
        var (cx, cz) = WorldToAlphamap(wp);
        int brush = Mathf.RoundToInt(radius);

        // ── KEY CHANGE: only read the dirty bounding box ──
        float[,,] alpha = ReadDirtyRegion(cx, cz, brush,
                              out int x0, out int z0, out int w, out int h);

        int layers = alpha.GetLength(2);
        int brushSq = brush * brush;

        for (int dz = 0; dz < h; dz++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int lx = (x0 + dx) - cx;
                int lz = (z0 + dz) - cz;
                float distSq = lx * lx + lz * lz;   // no Sqrt!
                if (distSq > brushSq) continue;

                float normalized = Mathf.Sqrt(distSq) / brush;
                float strength   = Mathf.Clamp01((1f - normalized) * multiplier);

                for (int l = 0; l < layers; l++)
                    alpha[dz, dx, l] *= (1f - strength);
                alpha[dz, dx, targetLayer] += strength;
            }
        }

        // ── KEY CHANGE: write only the dirty bounding box ──
        terrain.terrainData.SetAlphamaps(x0, z0, alpha);
    }

    // ── Freeze (Update-driven) ─────────────────
    void Update()
    {
        if (stopFreezing || isGameOver) return;

        freezeProgress += freezeSpeed * Time.deltaTime;
        freezeProgress  = Mathf.Clamp01(freezeProgress);
        snowMeter?.SetFreezeProgress(freezeProgress); 
        _skipFrame++;
        if (_skipFrame < FLUSH_EVERY) return;
        _skipFrame = 0;

        float frozenInnerRadius = Mathf.Lerp(brushSize, 0f, freezeProgress);
        PaintFreezeInwardFast(worldPos.position, frozenInnerRadius);

        if (freezeProgress >= 1f)
        {
            snowMeter?.SetGameOver(); // ← trigger game over animation
            onFreezeComplete?.Invoke();
        }
    }
void PaintFreezeInwardFast(Vector3 wp, float innerRadius)
{
    var (cx, cz) = WorldToAlphamap(wp);
    int outer = Mathf.RoundToInt(brushSize);

    float[,,] alpha = ReadDirtyRegion(cx, cz, outer,
                          out int x0, out int z0, out int w, out int h);

    int layers = alpha.GetLength(2);

    for (int dz = 0; dz < h; dz++)
    {
        for (int dx = 0; dx < w; dx++)
        {
            int lx = (x0 + dx) - cx;
            int lz = (z0 + dz) - cz;
            float dist = Mathf.Sqrt(lx * lx + lz * lz);

            if (dist > brushSize) continue;

            float strength;

            if (dist >= innerRadius)
            {
                // Ring goes from innerRadius outward by ringWidth
                // Anything beyond ringWidth from innerRadius is full freeze
                float distIntoRing = dist - innerRadius;
                float normalized = Mathf.Clamp01(distIntoRing / ringWidth);
                normalized = Mathf.SmoothStep(0f, 1f, normalized);

                // normalized=0 at inner edge (0% freeze), normalized=1 at outer (100% freeze)
                strength = Mathf.Clamp01(normalized * multiplier);

                for (int l = 0; l < layers; l++)
                    alpha[dz, dx, l] *= (1f - strength);
                alpha[dz, dx, FREEZE_LAYER] += strength;
            }
            else
            {
                // Inside grass zone — keep as grass, no change needed
                for (int l = 0; l < layers; l++)
                    alpha[dz, dx, l] *= 1f;
                alpha[dz, dx, GRASS_LAYER] = Mathf.Max(alpha[dz, dx, GRASS_LAYER], 0f);
            }
        }
    }

    terrain.terrainData.SetAlphamaps(x0, z0, alpha);
}
    void PaintFreezeFullFast(Vector3 wp, float frozenRadius)
    {
        var (cx, cz) = WorldToAlphamap(wp);
        int outer = Mathf.RoundToInt(brushSize);   // always read full brush area

        float[,,] alpha = ReadDirtyRegion(cx, cz, outer,
                            out int x0, out int z0, out int w, out int h);

        int layers = alpha.GetLength(2);

        for (int dz = 0; dz < h; dz++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int lx = (x0 + dx) - cx;
                int lz = (z0 + dz) - cz;
                float dist = Mathf.Sqrt(lx * lx + lz * lz);

                if (dist > brushSize) continue;

                float strength;

                if (dist <= frozenRadius)
                {
                    // Inside frozen zone — paint freeze with soft edge
                    float normalized = dist / Mathf.Max(frozenRadius, 0.001f);
                    normalized = Mathf.SmoothStep(0f, 1f, normalized);
                    strength = Mathf.Clamp01((1f - normalized * 0.3f) * multiplier);

                    for (int l = 0; l < layers; l++)
                        alpha[dz, dx, l] *= (1f - strength);
                    alpha[dz, dx, FREEZE_LAYER] += strength;
                }
                else
                {
                    // Outside frozen zone but inside brush — restore grass
                    float edgeDist = (dist - frozenRadius) / Mathf.Max(brushSize - frozenRadius, 0.001f);
                    strength = Mathf.Clamp01((1f - edgeDist) * multiplier);

                    for (int l = 0; l < layers; l++)
                        alpha[dz, dx, l] *= (1f - strength);
                    alpha[dz, dx, GRASS_LAYER] += strength;
                }
            }
        }

        terrain.terrainData.SetAlphamaps(x0, z0, alpha);
    }

    void PaintFreezeRingFast(Vector3 wp, float outerRadius, float innerRadius)
    {
        var (cx, cz) = WorldToAlphamap(wp);
        int outer = Mathf.RoundToInt(outerRadius);

        float[,,] alpha = ReadDirtyRegion(cx, cz, outer,
                              out int x0, out int z0, out int w, out int h);

        int layers  = alpha.GetLength(2);
        float range = outerRadius - innerRadius;

        for (int dz = 0; dz < h; dz++)
        {
            for (int dx = 0; dx < w; dx++)
            {
                int lx = (x0 + dx) - cx;
                int lz = (z0 + dz) - cz;
                float dist = Mathf.Sqrt(lx * lx + lz * lz);

                if (dist > outerRadius || dist < innerRadius) continue;

                float normalized = (range > 0.001f) ? (dist - innerRadius) / range : 1f;
                normalized = Mathf.SmoothStep(0, 1, normalized);
                float strength = Mathf.Clamp01((1f - normalized) * multiplier);

                for (int l = 0; l < layers; l++)
                    alpha[dz, dx, l] *= (1f - strength);
                alpha[dz, dx, FREEZE_LAYER] += strength;
            }
        }

        terrain.terrainData.SetAlphamaps(x0, z0, alpha);
    }

    // ── Freeze control ─────────────────────────
    public void StartFreezing(Action onComplete = null)
    {
        onFreezeComplete = onComplete;
        stopFreezing = false;
    }
    public void StopFreezing()  => stopFreezing = true;

    // ── Reset ──────────────────────────────────
    [ContextMenu("Reset to Snow")]
    public void ResetToOriginal()
    {
        TerrainData data = terrain.terrainData;
        int W = data.alphamapWidth, H = data.alphamapHeight, L = data.alphamapLayers;
        float[,,] alpha = new float[H, W, L];
        for (int z = 0; z < H; z++)
            for (int x = 0; x < W; x++)
                alpha[z, x, FREEZE_LAYER] = 1f;
        data.SetAlphamaps(0, 0, alpha);
        freezeProgress = 0f;
        stopFreezing   = true;
    }
}