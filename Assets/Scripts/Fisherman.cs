using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using DG.Tweening;
using UnityEditor.Animations;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public Image progressCircle; // UI Image with Fill Method: Radial 360
    public GameObject fishPrefab;
    public Transform inventoryTransform, fishSpawnPoint;
    [SerializeField] private bool isFollowingCamera = false;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI fishCountText, movementButtonText;
    
    [SerializeField] private Vector3 targetLocation, startingLocation;
    [SerializeField] private Vector3 cameraPositionOffset, cameraRotationOffset;
    [SerializeField] private float cameraFieldOfViewAfterSet = 30f;
    [SerializeField] private List<Transform> wayToBarrel;
    [SerializeField] private Button movementButton, feedButton;
    [SerializeField] private DragonController dragon;
    [SerializeField] private Transform CharacterInfoBG;
    private const string nearDragonKey = "Go Fishing";
    private const string nearPondKey = "Go To Dragon";
    private float timer = 0;
    private float coughtFishCount = 0;
    private bool isFishing = false;
    private bool hasMovedToLocation = false;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isNearDragon = false;

    void Start()
    {
        if (progressCircle != null) progressCircle.fillAmount = 0;

        if (isFollowingCamera)
        {
            // Camera.main.transform.SetParent(transform);
            // Camera.main.transform.localPosition -= Vector3.forward * 12f; // Default offset, adjust as needed
            originalCameraPosition = Camera.main.transform.localPosition;
            originalCameraRotation = Camera.main.transform.localRotation;
            //Barrel.Instance.dragon.FeedButton.onClick.AddListener(() => FeedFishToDragon(this));
            MoveFromToLocation();
        }
        else
        {
            movementButton.transform.parent.gameObject.SetActive(false); // Hide movement button if not following camera
        }

    }

    void Update()
    {
        if (isFishing)
        {
            timer += Time.deltaTime;
            if(progressCircle != null) progressCircle.fillAmount = timer / catchTime;
            //FTUEManager.Instance.StopFeedFTUE();

            if (timer >= catchTime)
            {
                CompleteCatch();
            }
        }
    }

    // Manual click to start/speed up as requested
    public void OnMouseDown() 
    {
        if(!hasMovedToLocation || isNearDragon) return; // Ignore clicks until fisherman has moved to location

        if (!isFishing) {
            timer = 0;
            isFishing = true;
            animator.SetBool("RunningEnd", false);
            animator.SetBool("IsFishing", true);
            DOVirtual.DelayedCall(0.1f, () => animator.SetBool("IsFishing", false));
            //FTUEManager.Instance.StopStartFTUE();
        }
    }

    public void CompleteCatch()
    {
        timer = 0;
        if(isFollowingCamera) StartCoroutine(MoveFishToInventory());
        else 
        {
            firstFisherMan.CountOtherFishermanFish();
            StopFishing();
            isNearDragon = false;
        }
    }

    public void CountOtherFishermanFish()
    {
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
        SetButtonStatus();
    }

    IEnumerator MoveFishToInventory()
    {
        GameObject fish = Instantiate(fishPrefab, fishSpawnPoint.position, Quaternion.identity, transform);
        SoundController.Instance.PlaySFX(SoundType.FishCatch);
        float elapsed = 0;
        float duration = 0.5f;
        StopFishing();
        progressCircle.fillAmount = 0;
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
        isNearDragon = false;
        DOVirtual.DelayedCall(0.5f, SetButtonStatus);
        spawnedFish.Add(fish);
        while (elapsed < duration)
        {
            fish.transform.position = Vector3.MoveTowards(fish.transform.position, inventoryTransform.position, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy(fish, 0.1f);
        //barrelTransform.GetComponent<Barrel>().AddFish(1); // Update barrel count
    }
    
    public void StopFishing() => isFishing = false;
    
    public void MoveFromToLocation()
    {
        // //animator.runtimeAnimatorController = runningAnimatorController;
        // hasMovedToLocation = false;

        // SoundController.Instance.PlaySFX(SoundType.Walking);

        // // Snap starting position to ground
        // Vector3 groundedStart = GetGroundPosition(startingLocation);
        // transform.position = groundedStart;

        // // Snap target position to ground
        // Vector3 groundedTarget = GetGroundPosition(targetLocation);

        // // Move to grounded target
        // transform.DOMove(groundedTarget, 3f).OnComplete(() =>
        // {
        //     hasMovedToLocation = true;
        //     if(isFollowingCamera) RotateCamera();

        //     // Final snap (safety)
        //     transform.position = GetGroundPosition(groundedTarget) + Vector3.up; // Slightly above ground to avoid clipping
        // });
        StartCoroutine(MoveRoutine(1f));
    }

    private IEnumerator MoveRoutine(float duration)
    {
        hasMovedToLocation = false;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        animator.SetBool("RunningStart", true);
        animator.SetBool("RunningEnd", false);
        Vector3 startPos = startingLocation;
        Vector3 endPos = targetLocation;
        float elapsed = 0f;

        // Initial snap to ground
        transform.position = GetGroundPosition(startPos);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // 1. Calculate the horizontal/linear interpolation
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

            // 2. Sample the ground height at this specific point every frame
            // This prevents the "hovering" effect over dips or hills
            transform.position = GetGroundPosition(currentPos);

            yield return null;
        }

        // Ensure we land exactly at the target grounded position
        transform.position = GetGroundPosition(endPos);
        
        // Final logic previously in .OnComplete
        hasMovedToLocation = true;
        if (isFollowingCamera) RotateCamera();
        animator.SetBool("RunningStart", false);
        animator.SetBool("RunningEnd", true);
        Camera.main.DOOrthoSize(7.5f, 0.5f); 
        // Optional: Keep your safety offset if needed
        // transform.position += Vector3.up * 0.1f; 
    }

    private void RotateCamera()
    {
        return;
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
        Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
        {
            return hit.point;
        }

        return position; // fallback if nothing hit
    }

    internal void MoveToLocation(Vector3 startLocation)
    {
        // animator.runtimeAnimatorController = runningAnimatorController;
        hasMovedToLocation = false;
        targetLocation = transform.position; // Current position is the target
        startingLocation = startLocation;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        transform.position = startingLocation; // Move to starting point first
        transform.DOMove(targetLocation, 1f).OnComplete(() => {
            hasMovedToLocation = true;
            // animator.runtimeAnimatorController = idleAnimatorController;
        });
    }
    private IEnumerator MoveToBarrelRoutine(float duration)
    {
        yield return new WaitForSeconds(purchaseIndex * 0.2f);
        hasMovedToLocation = false;
        movementButton.gameObject.SetActive(false);
        SoundController.Instance.PlaySFX(SoundType.Walking);
        Camera.main.DOOrthoSize(9.5f, 0.5f); 
        
        List<Vector3> path = new List<Vector3>();
        path.Add(transform.position);
        goToBarrel?.Invoke();
        for (int i = 0; i < wayToBarrel.Count; i++)
        {
            path.Add(wayToBarrel[i].position);
        }
        // Calculate total path distance to keep speed constant
        float totalDistance = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(path[i], path[i + 1]);
        }
        float moveSpeed = totalDistance / duration;

        //FTUEManager.Instance.StopFeedFTUE();

        // 🎥 Camera (local only)
        RotateCameraLocalTween();
        // 🔄 STEP 1: Rotate player instantly (NO tween conflict)
        if (path.Count > 1) transform.rotation = GetLookRotation(GetGroundPosition(path[1]));
        CharacterInfoBG.eulerAngles += new Vector3(0,200,0);
        // 🏃 STEP 2: Start movement immediately
        //animator.runtimeAnimatorController = runningAnimatorController;
        animator.SetBool("RunningStart", true);
        DOVirtual.DelayedCall(0.5f, () => animator.SetBool("RunningStart", false));
        animator.SetBool("RunningEnd", false);
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 startPoint = path[i];
            Vector3 endPoint = path[i + 1];
            float segmentDist = Vector3.Distance(startPoint, endPoint);
            if (segmentDist <= 0) continue;

            float segmentDuration = segmentDist / moveSpeed;
            float elapsed = 0f;

            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / segmentDuration;
                
                Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, t);
                transform.position = GetGroundPosition(currentPos);
                UpdateRotationWhileMoving();

                yield return null;
            }
        }

        transform.position = GetGroundPosition(path[path.Count - 1]);

        hasMovedToLocation = true;
        // animator.runtimeAnimatorController = idleAnimatorController;
        isNearDragon = true;
        SetButtonStatus();
        feedButton.gameObject.SetActive(true);
        transform.position = GetGroundPosition(transform.position);
        // transform.eulerAngles = new Vector3(0, 180, 0); // Ensure facing dragon
        animator.SetBool("RunningStart", false);
        animator.SetBool("RunningEnd", true);
    }
    [ContextMenu("Move To Barrel")]
    public void MoveToBarrel()
    {
        StartCoroutine(MoveToBarrelRoutine(0.5f));
        // hasMovedToLocation = false;
        // movementButton.gameObject.SetActive(false);
        // SoundController.Instance.PlaySFX(SoundType.Walking);
        // Vector3[] path = new Vector3[wayToBarrel.Count];

        // for (int i = 0; i < wayToBarrel.Count; i++)
        // {
        //     path[i] = GetGroundPosition(wayToBarrel[i].position);
        // }
        // FTUEManager.Instance.StopFeedFTUE();

        // // 🎥 Camera (local only)
        // RotateCameraLocalTween();
        // // 🔄 STEP 1: Rotate player instantly (NO tween conflict)
        // transform.rotation = GetLookRotation(path[0]);
        // // 🏃 STEP 2: Start movement immediately
        // //animator.runtimeAnimatorController = runningAnimatorController;
        // transform.DOPath(path, 4f, PathType.CatmullRom)
        //     .SetEase(Ease.Linear)
        //     .SetOptions(false) // 🔥 IMPORTANT: disables automatic rotation
        //     .OnUpdate(UpdateRotationWhileMoving)
        //     .OnComplete(() =>
        //     {
        //         hasMovedToLocation = true;
        //         // animator.runtimeAnimatorController = idleAnimatorController;
        //         isNearDragon = true;
        //         SetButtonStatus();
        //         feedButton.gameObject.SetActive(true);
        //         transform.position = GetGroundPosition(transform.position) + Vector3.up;
        //     });

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
    private IEnumerator ReturnToFishingSiteRoutine(float duration)
    {
        hasMovedToLocation = false;
        goToFishingSpot?.Invoke();
        SoundController.Instance.PlaySFX(SoundType.Walking);
        animator.SetBool("RunningStart", true);
        DOVirtual.DelayedCall(0.5f, () => animator.SetBool("RunningStart", false));
        animator.SetBool("RunningEnd", false);
        List<Vector3> path = new List<Vector3>();
        path.Add(transform.position); 
        movementButton.gameObject.SetActive(false);
        //feedButton.gameObject.SetActive(false);
        CharacterInfoBG.eulerAngles += new Vector3(0,160,0);

        for (int i = wayToBarrel.Count - 1; i >= 0; i--)
        {
            path.Add(wayToBarrel[i].position);
        }
        path.Add(targetLocation);

        // Calculate total path distance to keep speed constant
        float totalDistance = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(path[i], path[i + 1]);
        }
        float moveSpeed = totalDistance / duration;

        // 🎥 Reset camera (same as before)
        ResetCameraLocalTween();
        // ✅ ONLY HERE we lock rotation (your desired angle)
        transform.eulerAngles = new Vector3(0, 180, 0); 
        // animator.runtimeAnimatorController = runningAnimatorController;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 startPoint = path[i];
            Vector3 endPoint = path[i + 1];
            float segmentDist = Vector3.Distance(startPoint, endPoint);
            if (segmentDist <= 0) continue;

            float segmentDuration = segmentDist / moveSpeed;
            float elapsed = 0f;

            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / segmentDuration;

                Vector3 currentPos = Vector3.Lerp(startPoint, endPoint, t);
                transform.position = GetGroundPosition(currentPos);

                yield return null;
            }
        }

        animator.SetBool("RunningStart", false);
        animator.SetBool("RunningEnd", true);
        transform.position = GetGroundPosition(path[path.Count - 1]);

        hasMovedToLocation = true;
        // animator.runtimeAnimatorController = idleAnimatorController;
        transform.position = GetGroundPosition(transform.position);
        isNearDragon = false;
        Camera.main.DOOrthoSize(7.5f, 0.5f); 
        SetButtonStatus();
        RotateCamera();
    }
    private Vector3 previousPosition;
    [ContextMenu("Return To Fishing Site")]
    public void ReturnToFishingSite()
    {
        StartCoroutine(ReturnToFishingSiteRoutine(0.5f));
        // hasMovedToLocation = false;
        // SoundController.Instance.PlaySFX(SoundType.Walking);
        // List<Vector3> path = new List<Vector3>();
        // movementButton.gameObject.SetActive(false);
        // feedButton.gameObject.SetActive(false);

        // for (int i = wayToBarrel.Count - 1; i >= 0; i--)
        // {
        //     path.Add(GetGroundPosition(wayToBarrel[i].position));
        // }

        // path.Add(GetGroundPosition(targetLocation) + Vector3.up);
        // // 🎥 Reset camera (same as before)
        // ResetCameraLocalTween();
        // // ✅ ONLY HERE we lock rotation (your desired angle)
        // transform.eulerAngles = new Vector3(0, 180, 0); // change if needed
        // // animator.runtimeAnimatorController = runningAnimatorController;
        // transform.DOPath(path.ToArray(), 4f, PathType.CatmullRom)
        //     .SetEase(Ease.Linear)
        //     .SetOptions(false) // important: no auto rotation
        //     .OnComplete(() =>
        //     {
        //         hasMovedToLocation = true;
        //         // animator.runtimeAnimatorController = idleAnimatorController;
        //         transform.position = GetGroundPosition(transform.position) + Vector3.up;
        //         isNearDragon = false;
        //         SetButtonStatus();
        //         RotateCamera();
        //     });
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
        // animator.runtimeAnimatorController = runningAnimatorController;
        transform.DOPath(path.ToArray(), 4f, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetOptions(false)
            .OnUpdate(UpdateRotationWhileMoving)
            .OnComplete(() =>
            {
                hasMovedToLocation = true;
                // animator.runtimeAnimatorController = idleAnimatorController;

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
        return null;
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
        return null;
        if (!isFollowingCamera || Camera.main == null) return null;

        Transform cam = Camera.main.transform;

        Sequence seq = DOTween.Sequence();

        seq.Join(cam.DOLocalMove(originalCameraPosition, 0.5f).SetEase(Ease.OutSine));
        seq.Join(cam.DOLocalRotateQuaternion(originalCameraRotation, 0.5f).SetEase(Ease.OutSine));
        seq.Join(Camera.main.DOFieldOfView(30f, 0.5f)); // default FOV (adjust if needed)

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

    public void SetButtonStatus()
    {
        if(isNearDragon)
        {
            movementButtonText.text = nearDragonKey;
            movementButton.onClick.RemoveAllListeners();
            movementButton.onClick.AddListener(ReturnToFishingSite);
        }
        else
        {
            movementButtonText.text = nearPondKey;
            movementButton.onClick.RemoveAllListeners();
            movementButton.onClick.AddListener(MoveToBarrel);
        }
        movementButton.gameObject.SetActive(coughtFishCount > 3 && isFollowingCamera);
    }

    public void MoveFishToDragon(int fishCountToRemove = 0)
    {
        if (fishCountToRemove <= 0) return;
        StartCoroutine(FeedFishSequence(fishCountToRemove));
        dragon.FeedAnimation();
    }
    IEnumerator FeedFishSequence(int fishCount)
    {
        for (int i = 0; i < fishCount; i++)
        {
            if (coughtFishCount <= 0) yield break;
            // 🔻 Reduce fish
            coughtFishCount--;
            fishCountText.text = coughtFishCount.ToString();
            // 🐟 Spawn fish
            GameObject fish = spawnedFish[0];
            // GameObject fish = Instantiate(fishPrefab, transform.position, Quaternion.identity);
            // 🎯 Move to dragon
            Transform target = dragon.head.transform;
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (fish == null) yield break;
                fish.transform.position = Vector3.Lerp(
                    fish.transform.position,
                    target.position,
                    elapsed / duration
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            spawnedFish.RemoveAt(0);
            Destroy(fish);
            // 💰 Give gold for ONE fish
            // CurrencyHandler.Instance.AddGoldFromFish(1);
            // 🐉 Feed dragon ONE fish
            dragon.FeedFishOneByOne(1, i == 0);
            yield return new WaitForSeconds(0.1f); // spacing between feeds
        }
    }

    private List<GameObject> spawnedFish = new List<GameObject>();
    public void FeedFishToDragon(Fisherman fisherman)
    {
        //int fishToFeed = (int) MathF.Min(fisherman.coughtFishCount, (float) Barrel.Instance.requirements[Barrel.Instance.dragon.currentStage - 1]);
        //FTUEManager.Instance.StopFeedFTUE();
        MoveFishToDragon((int)coughtFishCount);
    }

    internal void MoveToPosition(Transform parent)
    {
        StartCoroutine(MoveToPositionRoutineAfterPurchase(parent));
    }

    public IEnumerator MoveToPositionRoutineAfterPurchase(Transform parent, float duration = 0.25f)
    {
        hasMovedToLocation = false;
        SoundController.Instance.PlaySFX(SoundType.Walking);
        animator.SetBool("RunningStart", true);
        animator.SetBool("RunningEnd", false);
        transform.SetParent(parent);
        yield return null; // Wait one frame for parent change to take effect
        transform.localRotation = Quaternion.identity; // Reset rotation to match parent
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = Vector3.zero;
        float elapsed = 0f;

        // Initial snap to ground
        transform.localPosition = GetGroundPosition(transform.localPosition);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;

            // 1. Calculate the horizontal/linear interpolation
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

            // 2. Sample the ground height at this specific point every frame
            // This prevents the "hovering" effect over dips or hills
            transform.localPosition = GetGroundPosition(currentPos);

            yield return null;
        }

        // Ensure we land exactly at the target grounded position
        transform.localPosition = GetGroundPosition(endPos);
        transform.localRotation = Quaternion.identity; // Reset rotation to match parent
        // Final logic previously in .OnComplete
        hasMovedToLocation = true;
        animator.SetBool("RunningStart", false);
        animator.SetBool("RunningEnd", true);
        targetLocation = transform.position;
        // Optional: Keep your safety offset if needed
        // transform.position += Vector3.up * 0.1f; 
    }
    private Fisherman firstFisherMan;
    private int purchaseIndex;
    public Action goToBarrel, goToFishingSpot;
    internal void SetFirstFisherman(Fisherman firstFisherman, int index)
    {
        this.firstFisherMan = firstFisherman;
        purchaseIndex = index;
    }
}