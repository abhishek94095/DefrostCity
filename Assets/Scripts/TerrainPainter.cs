using UnityEngine;
using System.Collections;
using System;

public class TerrainPainter : MonoBehaviour
{
    public Terrain terrain;
    public int targetLayerIndex = 0, brushSize = 5; // grass layer index
    public Transform worldPos;

    [ContextMenu("Paint Test")]
    public void StartPaintTest()
    {
        StartCoroutine(PaintRoutine(worldPos.position, 0));
    }

    public void StartPaint(int brushSize)
    {
        this.brushSize = brushSize;
        StartCoroutine(PaintRoutine(worldPos.position));
    }

    IEnumerator PaintRoutine(Vector3 worldPos, float duration = 1.5f, Action onCompleteAction = null)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Animate radius from small → full
            float currentRadius = Mathf.Lerp(1f, brushSize, t);

            PaintCircle(worldPos, currentRadius);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final pass to ensure full coverage
        PaintCircle(worldPos, brushSize);
        onCompleteAction?.Invoke();
    }

    void PaintCircle(Vector3 worldPos, float radius)
    {
        TerrainData data = terrain.terrainData;

        int mapWidth = data.alphamapWidth;
        int mapHeight = data.alphamapHeight;

        Vector3 terrainPos = worldPos - terrain.transform.position;

        int x = (int)((terrainPos.x / data.size.x) * mapWidth);
        int z = (int)((terrainPos.z / data.size.z) * mapHeight);

        int brushSize = Mathf.RoundToInt(radius);

        float[,,] alpha = data.GetAlphamaps(0, 0, mapWidth, mapHeight);

        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int px = x + i;
                int pz = z + j;

                if (px < 0 || px >= mapWidth || pz < 0 || pz >= mapHeight)
                    continue;

                float dist = Mathf.Sqrt(i * i + j * j);

                if (dist > brushSize)
                    continue;

                // ⭐ Smooth circular falloff
                float strength = (1f - (dist / brushSize)) * multiplier;
                strength = Mathf.Clamp01(strength);
                for (int l = 0; l < alpha.GetLength(2); l++)
                    alpha[pz, px, l] *= (1 - strength);

                alpha[pz, px, targetLayerIndex] += strength;
            }
        }

        data.SetAlphamaps(0, 0, alpha);
    }
    public float multiplier;
    [ContextMenu("Reset Original")]
    public void ResetToOriginal()
    {
        int previousLayer = targetLayerIndex;
        int oldBrushSize = brushSize;
        brushSize = 800;
        targetLayerIndex = 0;
        StartCoroutine(PaintRoutine(worldPos.position, 0f, () =>
        {
            targetLayerIndex = previousLayer;
            brushSize = oldBrushSize;
        }));
    }
}