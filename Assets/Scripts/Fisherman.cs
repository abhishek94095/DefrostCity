using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;
using UnityEditor.Animations;
using TMPro;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public Image progressCircle; // UI Image with Fill Method: Radial 360
    public GameObject fishPrefab;
    public Transform inventoryTransform;
    [SerializeField] private bool isFollowingCamera = false;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI fishCountText;
    [SerializeField] private AnimatorController runningAnimatorController, idleAnimatorController;
    
    [SerializeField] private Vector3 targetLocation, startingLocation;
    [SerializeField] private Vector3 cameraPositionOffset, cameraRotationOffset;
    [SerializeField] private float cameraFieldOfViewAfterSet = 30f;
    private float timer = 0;
    private float coughtFishCount = 0;
    private bool isFishing = false;
    private bool hasMovedToLocation = false;

    void Start()
    {
        if(progressCircle != null) progressCircle.fillAmount = 0;
        MoveFromToLocation();
        if(isFollowingCamera) Camera.main.transform.SetParent(transform);
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
            FTUEManager.Instance.StopStartFTUE();
        }
    }

    void CompleteCatch()
    {
        timer = 0;
        StartCoroutine(MoveFishToInventory());
    }

    IEnumerator MoveFishToInventory()
    {
        GameObject fish = Instantiate(fishPrefab, transform.position, Quaternion.identity);
        SoundController.Instance.PlaySFX(SoundType.FishCatch);
        float elapsed = 0;
        float duration = 0.5f;
        StopFishing();
        progressCircle.fillAmount = 0;
        coughtFishCount++;
        fishCountText.text = (coughtFishCount).ToString();

        while (elapsed < duration)
        {
            fish.transform.position = Vector3.MoveTowards(fish.transform.position, inventoryTransform.position, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(fish, 0.1f);
        //barrelTransform.GetComponent<Barrel>().AddFish(1); // Update barrel count
    }
    
    public void StopFishing() => isFishing = false;
    
    public void MoveFromToLocation()
    {
        animator.runtimeAnimatorController = runningAnimatorController;
        hasMovedToLocation = false;

        SoundController.Instance.PlaySFX(SoundType.Walking);

        // Snap starting position to ground
        Vector3 groundedStart = GetGroundPosition(startingLocation);
        transform.position = groundedStart;

        // Snap target position to ground
        Vector3 groundedTarget = GetGroundPosition(targetLocation);

        // Move to grounded target
        transform.DOMove(groundedTarget, 3f).OnComplete(() =>
        {
            hasMovedToLocation = true;
            animator.runtimeAnimatorController = idleAnimatorController;
            RotateCamera();

            // Final snap (safety)
            transform.position = GetGroundPosition(groundedTarget) + Vector3.up; // Slightly above ground to avoid clipping
        });
    }

    private void RotateCamera()
    {
        if (!isFollowingCamera) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 targetPosition = cameraPositionOffset;
        Vector3 targetRotation = cameraRotationOffset;

        // change local position and local rotation
        mainCamera.transform.DOLocalMove(targetPosition, 1f);
        mainCamera.transform.DOLocalRotate(targetRotation, 1f);
        mainCamera.DOFieldOfView(cameraFieldOfViewAfterSet, 1f);
    }

    private Vector3 GetGroundPosition(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 1f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            return hit.point;
        }

        return position; // fallback if nothing hit
    }

    internal void MoveToLocation(Vector3 startLocation)
    {
        animator.runtimeAnimatorController = runningAnimatorController;
        hasMovedToLocation = false;
        targetLocation = transform.position; // Current position is the target
        startingLocation = startLocation;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        transform.position = startingLocation; // Move to starting point first
        transform.DOMove(targetLocation, 1f).OnComplete(() => {
            hasMovedToLocation = true;
            animator.runtimeAnimatorController = idleAnimatorController;
        });
    }
}