using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public Image progressCircle; // UI Image with Fill Method: Radial 360
    public GameObject fishPrefab;
    public Transform barrelTransform;
    
    private float timer = 0;
    private bool isFishing = false;
    private bool hasMovedToLocation = true;
    private Vector3 targetLocation, startingLocation;

    void Start()
    {
        if(progressCircle != null) progressCircle.fillAmount = 0;
    }

    void Update()
    {
        if (isFishing)
        {
            timer += Time.deltaTime;
            if(progressCircle != null) progressCircle.fillAmount = timer / catchTime;

            if (timer >= catchTime)
            {
                CompleteCatch();
            }
        }
    }

    // Manual click to start/speed up as requested
    public void OnMouseDown() 
    {
        if(!hasMovedToLocation) return; // Ignore clicks until fisherman has moved to location

        if (!isFishing) {
            timer = 0;
            isFishing = true;
        }
    }

    void CompleteCatch()
    {
        timer = 0;
        StartCoroutine(MoveFishToBarrel());
    }

    IEnumerator MoveFishToBarrel()
    {
        GameObject fish = Instantiate(fishPrefab, transform.position, Quaternion.identity);
        float elapsed = 0;
        float duration = 0.5f;
        StopFishing();
        progressCircle.fillAmount = 0;

        while (elapsed < duration)
        {
            fish.transform.position = Vector3.MoveTowards(fish.transform.position, barrelTransform.position, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(fish,0.1f);
        barrelTransform.GetComponent<Barrel>().AddFish(1); // Update barrel count
    }
    
    public void StopFishing() => isFishing = false;

    internal void MoveToLocation(Vector3 startLocation)
    {
        hasMovedToLocation = false;
        targetLocation = transform.position; // Current position is the target
        startingLocation = startLocation;
        transform.position = startingLocation; // Move to starting point first
        transform.DOMove(targetLocation, 1f).OnComplete(() => {
            hasMovedToLocation = true;
        });
    }
}