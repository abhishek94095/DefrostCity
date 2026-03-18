using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public Image progressCircle; // UI Image with Fill Method: Radial 360
    public GameObject fishPrefab;
    public Transform barrelTransform;
    
    private float timer = 0;
    private bool isFishing = true;

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

        while (elapsed < duration)
        {
            fish.transform.position = Vector3.MoveTowards(fish.transform.position, barrelTransform.position, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(fish);
        barrelTransform.GetComponent<Barrel>().AddFish(1); // Update barrel count
    }
    
    public void StopFishing() => isFishing = false;
}