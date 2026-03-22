using UnityEngine;
using System;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public Action OnFTUEStopped;

    [Header("FTUE Objects")]
    [SerializeField] private GameObject startFTUE;   // Drag hand
    [SerializeField] private GameObject feedFTUE;    // Feed dragon hint

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Invoke(nameof(ShowStartFTUE), 5.5f);
    }

    // 🟢 START FTUE
    public void ShowStartFTUE()
    {

        if (startFTUE != null)
            startFTUE.SetActive(true);
    }

    // 🔴 STOP START FTUE (on player input)
    public void StopStartFTUE()
    {
        if (startFTUE != null && startFTUE.activeSelf)
        {
            startFTUE.SetActive(false);
            OnFTUEStopped?.Invoke();
        }
    }

    // 🟡 SHOW FEED FTUE (when barrel full)
    public void ShowFeedFTUE()
    {
        if (feedFTUE != null)
            feedFTUE.SetActive(true);
    }

    // 🔴 STOP FEED FTUE (when button clicked)
    public void StopFeedFTUE()
    {
        if (feedFTUE != null && feedFTUE.activeSelf)
        {
            feedFTUE.SetActive(false);
            OnFTUEStopped?.Invoke();
        }
    }
}