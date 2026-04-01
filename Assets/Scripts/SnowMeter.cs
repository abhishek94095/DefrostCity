using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SnowMeter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform handle;
    [SerializeField] private TextMeshProUGUI tempText;

    [Header("Temperature Range")]
    public float minTemp = -10f;   // top = full freeze (cold)
    public float maxTemp =  40f;   // bottom = fully melted (hot)

    [Header("Bar Size")]
    [Tooltip("Height of the fill bar in pixels — match your Background height")]
    public float barHeight = 260f;

    [Header("Smoothing")]
    [Tooltip("How fast the bar visually catches up — lower = smoother lag")]
    public float smoothSpeed = 4f;

    private float _currentProgress = 0f;   // actual game value
    private float _displayProgress = 0f;   // smoothed visual value
    private bool  _gameOver = false;
    private Tweener _gameOverTween;

    void Update()
    {
        if (_gameOver) return;

        // Smooth the display toward the actual value
        _displayProgress = Mathf.Lerp(_displayProgress, _currentProgress, Time.deltaTime * smoothSpeed);
        RefreshUI(_displayProgress);
    }

    // Called every frame from TerrainPainter
    public void SetFreezeProgress(float progress)
    {
        _currentProgress = Mathf.Clamp01(progress);
    }

    // Called on game over — animate to full freeze then lock
    public void SetGameOver()
    {
        _gameOver = true;
        _gameOverTween?.Kill();
        _gameOverTween = DOTween.To(
            () => _displayProgress,
            x  => { _displayProgress = x; RefreshUI(x); },
            1f, 0.8f
        ).SetEase(Ease.OutQuad);
    }

    void RefreshUI(float progress)
    {
        fillImage.fillAmount = progress;

        float handleY = Mathf.Lerp(0f, barHeight, progress);
        handle.anchoredPosition = new Vector2(0f, handleY);

        float temp = Mathf.Lerp(maxTemp, minTemp, progress);
        tempText.text = Mathf.RoundToInt(temp) + "°C";
    }
}