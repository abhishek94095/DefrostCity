using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SnowMeter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image            fillImage;
    [SerializeField] private RectTransform    handle;
    [SerializeField] private TextMeshProUGUI  tempText;

    [Header("Bar Size")]
    public float barHeight = 260f;

    [Header("Temperature Range")]
    public float absoluteMin = -10f;
    public float absoluteMax =  40f;

    [Header("Freeze Speed")]
    [Tooltip("Degrees C dropped per second when freezing")]
    public float freezeRate = 5f;

    private readonly float[] _stageWarmMax   = { 0f, 15f, 30f };
    private readonly float[] _stageDivisors  = { 1f,  4f,  8f };

    private int   _currentStage = 0;
    private float _currentTemp  = 0f;
    private float _displayTemp  = 0f;

    private enum State { Idle, Freezing, FeedingFish, Upgrading, GameOver }
    private State   _state = State.Idle;
    private Tweener _tween;

    // Read by TerrainPainter.Update to drive terrain painting
    public float FreezeProgress =>
        Mathf.InverseLerp(_stageWarmMax[_currentStage], absoluteMin, _currentTemp);

    void Start()
    {
        _currentTemp = 0f;
        _displayTemp = 0f;
        RefreshUI(_displayTemp);
    }

    void Update()
    {
        // Drop temp each frame when freezing
        if (_state == State.Freezing)
        {
            _currentTemp -= (freezeRate / _stageDivisors[_currentStage]) * Time.deltaTime;
            _currentTemp  = Mathf.Max(_currentTemp, absoluteMin);
        }

        // Smooth display toward actual value
        _displayTemp = Mathf.Lerp(_displayTemp, _currentTemp, Time.deltaTime * 8f);
        RefreshUI(_displayTemp);
    }

    // Called by TerrainPainter.StartFreezing
    public void StartFreezing()
    {
        if (_state == State.GameOver || _state == State.Upgrading) return;
        _state = State.Freezing;
    }

    // Called by TerrainPainter.StopFreezing
    public void StopFreezing()
    {
        if (_state == State.Freezing)
            _state = State.Idle;
    }

    // Called by DragonController.FeedFishOneByOne — each fish raises temp
    public void OnFishFed(float tempRisePerFish = 2f)
    {
        if (_state == State.GameOver || _state == State.Upgrading) return;
        _state = State.FeedingFish;
        float cap = _stageWarmMax[_currentStage];
        _currentTemp = Mathf.Min(_currentTemp + tempRisePerFish, cap);
    }

    // Called when feeding stops (no fish left / player walked away)
    public void OnFeedingStopped()
    {
        if (_state == State.FeedingFish)
            _state = State.Freezing;
    }

    // Called on upgrade 1 or 2
    public void OnUpgrade(int newStage)
    {
        if (_state == State.GameOver) return;
        if (newStage < 0 || newStage >= _stageWarmMax.Length) return;

        _currentStage = newStage;
        _state = State.Upgrading;

        float target = _stageWarmMax[newStage];
        _tween?.Kill();
        _tween = DOTween.To(
            () => _currentTemp,
            x  => _currentTemp = x,
            target,
            1f
        ).SetEase(Ease.OutQuad)
         .OnComplete(() => OnUpgradeComplete(newStage));
    }

    private void OnUpgradeComplete(int completedStage)
    {
        freezeRate *= 2f;

        if (completedStage == 2)
        {
            // Final upgrade: wait 1s then animate to absoluteMax (40°C) = win
            DOVirtual.DelayedCall(1f, () =>
            {
                _state = State.GameOver;
                _tween?.Kill();
                _tween = DOTween.To(
                    () => _currentTemp,
                    x  => _currentTemp = x,
                    absoluteMax,
                    1.5f
                ).SetEase(Ease.OutQuad);
            });
        }
        else
        {
            // TerrainPainter.ResumeFreezingAfter calls StartFreezing after the 8s delay
            _state = State.Idle;
        }
    }

    // Called on game over (lose — freeze wins)
    public void SetGameOver()
    {
        _tween?.Kill();
        _state = State.GameOver;
        _tween = DOTween.To(
            () => _currentTemp,
            x  => _currentTemp = x,
            absoluteMin,
            0.8f
        ).SetEase(Ease.OutQuad);
    }

    private void RefreshUI(float temp)
    {
        float progress = Mathf.InverseLerp(absoluteMax, absoluteMin, temp);
        fillImage.fillAmount = progress;
        handle.anchoredPosition = new Vector2(0f, Mathf.Lerp(0f, barHeight, progress));
        tempText.text = Mathf.RoundToInt(temp) + "\u00b0C";
    }
}