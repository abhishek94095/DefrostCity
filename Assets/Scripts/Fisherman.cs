using UnityEngine;
using System.Collections;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public GameObject fishPrefab;
    public Transform inventoryTransform, fishSpawnPoint;
    [SerializeField] private bool isFollowingCamera = false;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI fishCountText;
    
    [SerializeField] private Vector3 targetLocation, startingLocation;
    [SerializeField] private DragonController dragon;
    private float timer = 0;
    private float coughtFishCount = 0;
    [SerializeField] private bool isFishing = false;
    private bool hasMovedToLocation = false;
    private bool isNearDragon = false;
    private bool wasMoving = false;
    private List<GameObject> spawnedFish = new List<GameObject>();
    private Fisherman firstFisherMan;
    private bool isFeeding = false;
    public float speed = 15f;
    public Joystick joystick;
    private float idleTimer = 0f;
    private bool hasTriggeredInteraction = false;
    private InteractionType currentZone = InteractionType.None;
    public PurchaseFishman purchaseFishman;

    void Start()
    {
        if(isFishing) animator.SetBool("IsFishing", true); // ⭐ ADD THIS
    }
    void Update()
    {
        float inputMagnitude = 0;
        Vector2 input = Vector2.zero;
        if(joystick != null)
        {
            input = joystick.Direction;
            inputMagnitude = input.magnitude;
        }
        bool isMoving = inputMagnitude > 0.1f;
        // 🎣 Fishing logic
        if (isFishing)
        {
            timer += Time.deltaTime;

            if (timer >= catchTime)
            {
                CompleteCatch();
            }
        }

        if (!isFollowingCamera) return;

        // 🎯 Handle animation transitions ONLY on change
        if (isMoving && !wasMoving)
        {
            // Started moving
            animator.SetBool("RunningStart", true);
            animator.SetBool("RunningEnd", false);
            wasMoving = true;
            if(currentZone != InteractionType.FishingArea)
            {
                Camera.main.DOOrthoSize(9,0.5f);
            }
        }
        else if (!isMoving && wasMoving)
        {
            // Stopped moving
            animator.SetBool("RunningStart", false);
            animator.SetBool("RunningEnd", true);
            wasMoving = false;
            if(currentZone == InteractionType.FishingArea)
            {
                Camera.main.DOOrthoSize(6, 0.5f);
            }
        }

        // ✅ Movement
        if (isMoving)
        {
            Vector3 move = new Vector3(input.x, 0f, input.y).normalized;
            Vector3 targetPos = transform.position + move * speed * Time.deltaTime;
            targetPos = GetGroundPosition(targetPos);
            transform.position = targetPos;
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        HandleInteraction(inputMagnitude);
    }
    public void CompleteCatch()
    {
        timer = 0;
        isNearDragon = false;
        if(isFollowingCamera) StartCoroutine(MoveFishToInventory());
        else 
        {
            firstFisherMan.CountOtherFishermanFish();
            GameObject fish = Instantiate(fishPrefab, firstFisherMan.inventoryTransform.position, Quaternion.identity, firstFisherMan.transform);
            firstFisherMan.spawnedFish.Add(fish);
        }
    }

    public void CountOtherFishermanFish()
    {
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
    }

    IEnumerator MoveFishToInventory()
    {
        GameObject fish = Instantiate(fishPrefab, fishSpawnPoint.position, Quaternion.identity, transform);
        SoundController.Instance.PlaySFX(SoundType.FishCatch);
        float elapsed = 0;
        float duration = 0.5f;
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
        isNearDragon = false;
        spawnedFish.Add(fish);
        while (elapsed < duration)
        {
            fish.transform.position = Vector3.MoveTowards(fish.transform.position, inventoryTransform.position, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    public void StopFishing()
    {
        Debug.Log("Stop Fishing");
        isFishing = false;
        animator.SetBool("IsFishing", false); // ⭐ ADD THIS
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

    public void MoveFishToDragon(Fisherman fisherman)
    {
        if (fisherman.coughtFishCount <= 0) return;
        StartCoroutine(FeedFishSequence(fisherman));
        dragon.FeedAnimation();
        dragon.MeltIce();
    }
    IEnumerator FeedFishSequence(Fisherman fisherman)
    {
        isFeeding = true;
        while (true)
        {
            // ⛔ Stop if no fish
            if (coughtFishCount <= 0)
            {
                yield return null;
                continue;
            }

            // ⛔ Pause if dragon is busy (upgrade)
            if (dragon.IsBusy)
            {
                yield return null;
                continue;
            }

            if(currentZone != InteractionType.Dragon)
            {
                yield return null;
                continue;
            }

            coughtFishCount--;
            fishCountText.text = coughtFishCount.ToString();

            if (spawnedFish.Count == 0)
            {
                GameObject newFish = Instantiate(
                    fishPrefab,
                    inventoryTransform.position,
                    Quaternion.identity,
                    firstFisherMan.transform
                );
                spawnedFish.Add(newFish);
            }

            GameObject fish = spawnedFish[0];
            Transform target = dragon.plate.transform;

            float duration = 0.1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (fish == null) yield break;
                fish.transform.SetParent(dragon.plate);
                fish.transform.position = Vector3.Lerp(
                    fish.transform.position,
                    target.position,
                    elapsed / duration
                );

                elapsed += Time.deltaTime;
                yield return null;
            }

            spawnedFish.RemoveAt(0);
            Destroy(fish, 2.1f);

            dragon.FeedFishOneByOne(1);

            yield return new WaitForSeconds(0.05f);
        }
    }

    public void FeedFishToDragon(Fisherman fisherman)
    {
        if(isFeeding) return;
        isFeeding = true;
        MoveFishToDragon(fisherman);
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

        transform.localPosition = GetGroundPosition(transform.localPosition);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);
            transform.localPosition = GetGroundPosition(currentPos);
            yield return null;
        }

        transform.localPosition = GetGroundPosition(endPos);
        transform.localRotation = Quaternion.identity;
        hasMovedToLocation = true;
        animator.SetBool("RunningStart", false);
        animator.SetBool("RunningEnd", true);
        targetLocation = transform.position;
    }
    internal void SetFirstFisherman(Fisherman firstFisherman, int index)
    {
        this.firstFisherMan = firstFisherman;
    }

    void HandleInteraction(float inputMagnitude)
    {
        // Consider "stopped" when input < 20%
        bool isStopped = inputMagnitude < 0.2f;

        // ⏱ Track idle time
        if (isStopped)
        {
            idleTimer += Time.deltaTime;
        }
        else
        {
            // Reset when player moves
            idleTimer = 0f;
            hasTriggeredInteraction = false;
            return;
        }

        // ⛔ Wait before triggering (prevents instant activation)
        if (idleTimer < 0.5f || hasTriggeredInteraction)
            return;

        hasTriggeredInteraction = true;

        switch (currentZone)
        {
            case InteractionType.Dragon:
                FeedFishToDragon(this);
                break;

            case InteractionType.Fisherman:
                OnPurchaseButtonClick();
                break;

        }
    }

    void StartFishing()
    {
        if (isFishing) return;

        isFishing = true;
        animator.SetBool("IsFishing", true);
    }

    private void OnTriggerEnter(Collider other)
    {
        InteractionZone zone = other.GetComponent<InteractionZone>();
        if (zone != null)
        {
            currentZone = zone.type;
        }
        if (zone.type == InteractionType.Fisherman)
        {
            StartFishing();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        InteractionZone zone = other.GetComponent<InteractionZone>();
        if (zone != null && zone.type == currentZone)
        {
            currentZone = InteractionType.None;
            if (isFishing)
                StopFishing();
        }
    }

    void OnPurchaseButtonClick()
    {
        purchaseFishman.OnPurchaseButtonClick();
    }
}