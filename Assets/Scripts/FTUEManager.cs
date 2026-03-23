using UnityEngine;
using System;
using System.Collections;

public class FTUEManager : MonoBehaviour
{
    public static FTUEManager Instance;

    public Action OnFTUEStopped;

    [Header("FTUE Objects")]
    [SerializeField] private GameObject startFTUE;   // Drag hand
    [SerializeField] private GameObject feedFTUE;    // Feed dragon hint
    private Coroutine startFtueCoroutine;
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
        // change this to coroutine and store the coroutine
        startFtueCoroutine = StartCoroutine(DelayedStartFTUE());
    }

    private IEnumerator DelayedStartFTUE()
    {
        yield return new WaitForSeconds(5.5f);
        ShowStartFTUE();
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
        StopCoroutine(startFtueCoroutine); // Stop the coroutine if it's still running
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