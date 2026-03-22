using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;
using UnityEditor.Animations;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private List<Transform> wayToBarrel;
    private float timer = 0;
    private float coughtFishCount = 0;
    private bool isFishing = false;
    private bool hasMovedToLocation = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    void Start()
    {
        if (progressCircle != null) progressCircle.fillAmount = 0;

        if (isFollowingCamera)
        {
            Camera.main.transform.SetParent(transform);
            originalCameraPosition = Camera.main.transform.localPosition;
            originalCameraRotation = Camera.main.transform.localRotation;
        }

        MoveFromToLocation();
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
            if(isFollowingCamera) RotateCamera();

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

    [ContextMenu("Move To Barrel")]
    public void MoveToBarrel()
    {
        hasMovedToLocation = false;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        Vector3[] path = new Vector3[wayToBarrel.Count];

        for (int i = 0; i < wayToBarrel.Count; i++)
        {
            path[i] = GetGroundPosition(wayToBarrel[i].position) + Vector3.up;
        }

        // 🎥 Camera (local only)
        RotateCameraLocalTween();
        // 🔄 STEP 1: Rotate player instantly (NO tween conflict)
        transform.rotation = GetLookRotation(path[0]);
        // 🏃 STEP 2: Start movement immediately
        animator.runtimeAnimatorController = runningAnimatorController;
        transform.DOPath(path, 4f, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetOptions(false) // 🔥 IMPORTANT: disables automatic rotation
            .OnUpdate(UpdateRotationWhileMoving)
            .OnComplete(() =>
            {
                hasMovedToLocation = true;
                animator.runtimeAnimatorController = idleAnimatorController;

                transform.position = GetGroundPosition(transform.position) + Vector3.up;
            });

    }
    void RotateCameraProper()
    {
        if (!isFollowingCamera || Camera.main == null) return;

        Transform cam = Camera.main.transform;

        // move to offset first
        cam.DOLocalMove(cameraPositionOffset, 1f).SetEase(Ease.OutSine);

        // then look at player PROPERLY
        Vector3 worldLookDir = (transform.position - cam.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(worldLookDir);

        cam.DORotateQuaternion(lookRot, 1f).SetEase(Ease.OutSine);

        Camera.main.DOFieldOfView(cameraFieldOfViewAfterSet, 1f);
    }
    void FaceMovementDirection()
    {
        Vector3 velocity = (transform.position - previousPosition);
        velocity.y = 0;

        if (velocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        previousPosition = transform.position;
    }

    private Vector3 previousPosition;
    [ContextMenu("Return To Fishing Site")]
    public void ReturnToFishingSite()
    {
        hasMovedToLocation = false;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        List<Vector3> path = new List<Vector3>();

        for (int i = wayToBarrel.Count - 1; i >= 0; i--)
        {
            path.Add(GetGroundPosition(wayToBarrel[i].position) + Vector3.up);
        }

        path.Add(GetGroundPosition(targetLocation) + Vector3.up);
        Sequence camSequence = DOTween.Sequence();

        // Step 1: Reset camera first
        camSequence.Append(ResetCameraLocalTween());

        // Step 2: Then move character (NO WAIT FEEL)
        camSequence.AppendCallback(() =>
        {
            StartReturnMovement();
        });
        // 🔄 Instant correct facing
        transform.rotation = GetLookRotation(path[0]);
        animator.runtimeAnimatorController = runningAnimatorController;
        transform.DOPath(path.ToArray(), 4f, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetOptions(false)
            .OnUpdate(UpdateRotationWhileMoving)
            .OnComplete(() =>
            {
                hasMovedToLocation = true;
                animator.runtimeAnimatorController = idleAnimatorController;

                transform.position = GetGroundPosition(transform.position) + Vector3.up;
            });
    }
    void StartReturnMovement()
    {
        List<Vector3> path = new List<Vector3>();

        for (int i = wayToBarrel.Count - 1; i >= 0; i--)
        {
            path.Add(GetGroundPosition(wayToBarrel[i].position) + Vector3.up);
        }

        path.Add(GetGroundPosition(targetLocation) + Vector3.up);

        transform.rotation = GetLookRotation(path[0]);

        animator.runtimeAnimatorController = runningAnimatorController;

        transform.DOPath(path.ToArray(), 4f, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetOptions(false)
            .OnUpdate(UpdateRotationWhileMoving)
            .OnComplete(() =>
            {
                hasMovedToLocation = true;
                animator.runtimeAnimatorController = idleAnimatorController;

                transform.position = GetGroundPosition(transform.position) + Vector3.up;
            });
    }
    Vector3 lastPosition;

    void UpdateRotationWhileMoving()
    {
        Vector3 movement = transform.position - lastPosition;
        movement.y = 0;

        if (movement.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }

        lastPosition = transform.position;
    }

    void RotateCameraLocal()
    {
        if (!isFollowingCamera) return;

        Transform cam = Camera.main.transform;

        cam.DOLocalMove(cameraPositionOffset, 1f).SetEase(Ease.OutSine);
        cam.DOLocalRotate(cameraRotationOffset, 1f).SetEase(Ease.OutSine);
    }
    Tween RotateCameraLocalTween()
    {
        if (!isFollowingCamera || Camera.main == null) return null;

        Transform cam = Camera.main.transform;

        Sequence seq = DOTween.Sequence();

        seq.Join(cam.DOLocalMove(cameraPositionOffset, 1f).SetEase(Ease.OutSine));
        seq.Join(cam.DOLocalRotate(cameraRotationOffset, 1f).SetEase(Ease.OutSine));
        seq.Join(Camera.main.DOFieldOfView(cameraFieldOfViewAfterSet, 1f));

        return seq;
    }
    Tween ResetCameraLocalTween()
    {
        if (!isFollowingCamera || Camera.main == null) return null;

        Transform cam = Camera.main.transform;

        Sequence seq = DOTween.Sequence();

        seq.Join(cam.DOLocalMove(originalCameraPosition, 0.5f).SetEase(Ease.OutSine));
        seq.Join(cam.DOLocalRotateQuaternion(originalCameraRotation, 0.5f).SetEase(Ease.OutSine));
        seq.Join(Camera.main.DOFieldOfView(60f, 0.5f)); // default FOV (adjust if needed)

        return seq;
    }
    Quaternion GetLookRotation(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude < 0.001f)
            return transform.rotation;

        return Quaternion.LookRotation(dir);
    }

}