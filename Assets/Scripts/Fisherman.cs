using UnityEngine;
using System.Collections;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

public class Fisherman : MonoBehaviour
{
    public float catchTime = 3.0f;
    public GameObject fishPrefab;
    public Transform fishSpawnPoint, fishSpawnParent;
    [SerializeField] private bool isFollowingCamera = false;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI fishCountText;
    
    [SerializeField] private Vector3 targetLocation, startingLocation;
    [SerializeField] private DragonController dragon;
    private float timer = 0;
    private int coughtFishCount = 0;
    [SerializeField] private bool isFishing = false;
    [SerializeField] private bool canUseSpear = false;
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
    public Rigidbody rb;
    private Vector2   _moveInput;
    private bool      _isMoving;

    [Header("Movement Bounds")]
    [Tooltip("Layers that block movement: walls, rocks, buildings")]
    public LayerMask obstacleLayer;

    [Tooltip("Layers that block movement: water, forbidden zones")]
    public LayerMask waterLayer;

    [Tooltip("Sphere radius used for obstacle check — match BoxCollider half-width")]
    public float collisionRadius = 0.4f;

    void Start()
    {
        if(isFishing && !canUseSpear) animator.SetBool("IsFishing", true);
        if(canUseSpear) animator.SetTrigger("IsSecondFisherman");
    }
    void Update()
    {
        float inputMagnitude = 0;
        Vector2 input = Vector2.zero;
        if (joystick != null)
        {
            input = joystick.Direction;
            inputMagnitude = input.magnitude;
        }
        bool isMoving = inputMagnitude > 0.1f;
        // foreach (GameObject fish in spawnedFish)
        //         if (fish != null) fish.SetActive(currentZone == InteractionType.Dragon);
        // Store for FixedUpdate (movement lives there now)
        _moveInput = input;
        _isMoving  = isMoving;

        // 🎣 Fishing logic — IDENTICAL to yours
        if (isFishing || canUseSpear)
        {
            timer += Time.deltaTime;
            if (timer >= catchTime)
            {
                CompleteCatch();
            }
        }

        if (!isFollowingCamera) return;

        // 🎯 Animation transitions — IDENTICAL to yours
        if (isMoving && !wasMoving)
        {
            animator.SetBool("RunningStart", true);
            animator.SetBool("RunningEnd", false);
            wasMoving = true;
            if (currentZone != InteractionType.FishingArea)
            {
                Camera.main.DOOrthoSize(9.5f, 0.5f);
            }
        }
        else if (!isMoving && wasMoving)
        {
            animator.SetBool("RunningStart", false);
            animator.SetBool("RunningEnd", true);
            wasMoving = false;
            if (currentZone == InteractionType.FishingArea)
            {
                Camera.main.DOOrthoSize(7f, 0.5f);
            }
        }

        // ✅ StartFishing when idle in zone — IDENTICAL to yours
        if (!isMoving)
        {
            if (currentZone == InteractionType.Fisherman && !isFishing)
            {
                StartFishing();
            }
            
        }

        HandleInteraction(inputMagnitude);
    }
    void FixedUpdate()
    {
        if (!isFollowingCamera || !_isMoving) return;

        Vector3 move = new Vector3(_moveInput.x, 0f, _moveInput.y).normalized;

        // 1️⃣ Candidate next position (flat, no Y yet)
        Vector3 nextPos = rb.position + move * speed * Time.fixedDeltaTime;

        // 2️⃣ Clamp to terrain edges first (cheap, no raycast)
        nextPos = ClampToTerrain(nextPos);

        // 3️⃣ Check for obstacles and water at that position
        Vector3 checkOrigin = nextPos + Vector3.up * 0.5f;
        bool hitObstacle = Physics.CheckSphere(checkOrigin, collisionRadius, obstacleLayer);
        bool hitWater    = Physics.CheckSphere(checkOrigin, collisionRadius, waterLayer);

        if (hitObstacle || hitWater)
        {
            // Try sliding along each axis separately so player doesn't get stuck on corners
            Vector3 slideX = rb.position + new Vector3(move.x, 0f, 0f) * speed * Time.fixedDeltaTime;
            Vector3 slideZ = rb.position + new Vector3(0f, 0f, move.z) * speed * Time.fixedDeltaTime;

            slideX = ClampToTerrain(slideX);
            slideZ = ClampToTerrain(slideZ);

            bool blockedX = Physics.CheckSphere(slideX + Vector3.up * 0.5f, collisionRadius, obstacleLayer | waterLayer);
            bool blockedZ = Physics.CheckSphere(slideZ + Vector3.up * 0.5f, collisionRadius, obstacleLayer | waterLayer);

            if      (!blockedX) nextPos = slideX;
            else if (!blockedZ) nextPos = slideZ;
            else                return; // fully blocked, don't move
        }

        // 4️⃣ Snap Y to ground
        nextPos = GetGroundPosition(nextPos);

        // 5️⃣ Apply
        rb.MovePosition(nextPos);

        // 6️⃣ Smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(move);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
    }

    // ─────────────────────────────────────────────────────────────
    // Clamps position so the fisherman never walks off terrain edges
    Vector3 ClampToTerrain(Vector3 pos)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return pos;

        Vector3 origin = terrain.transform.position;
        TerrainData td = terrain.terrainData;

        pos.x = Mathf.Clamp(pos.x, origin.x + collisionRadius, origin.x + td.size.x - collisionRadius);
        pos.z = Mathf.Clamp(pos.z, origin.z + collisionRadius, origin.z + td.size.z - collisionRadius);
        return pos;
    }

    // ─────────────────────────────────────────────────────────────
    // Keep your existing GetGroundPosition unchanged
    private Vector3 GetGroundPosition(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f))
            return hit.point;
        return position;
    }
    public void CompleteCatch()
    {
        timer = 0;
        isNearDragon = false;
        if(isFollowingCamera) StartCoroutine(MoveFishToInventory());
        else 
        {
            firstFisherMan.CountOtherFishermanFish();
            GameObject fish = Instantiate(fishPrefab, fishCountText.transform.position, Quaternion.identity, fishSpawnParent);
            firstFisherMan.spawnedFish.Add(fish);
        }
    }

    public void CountOtherFishermanFish()
    {
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
    }
    public Vector3 initialFishLocalPosition;
    IEnumerator MoveFishToInventory()
    {
        GameObject fish = Instantiate(fishPrefab, Vector3.zero, Quaternion.identity, fishSpawnPoint);
        SoundController.Instance.PlaySFX(SoundType.FishCatch);
        float elapsed = 0;
        float duration = 0.5f;
        coughtFishCount++;
        fishCountText.text = coughtFishCount.ToString();
        isNearDragon = false;
        spawnedFish.Add(fish);
        yield return null; // Wait one frame for Instantiate to complete
        fish.transform.localPosition = initialFishLocalPosition;
        fish.transform.SetParent(fishSpawnParent);
        while (elapsed < duration)
        {
            fish.transform.localPosition = Vector3.Lerp(initialFishLocalPosition, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            fish.gameObject.SetActive(true);
            yield return null;
        }
        DOVirtual.DelayedCall(1f, () => fish.SetActive(false));
    }
    
    public void StopFishing()
    {
        Debug.Log("Stop Fishing");
        isFishing = false;
        animator.SetBool("IsFishing", false); // ⭐ ADD THIS
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

    public void FeedFishToDragon()
    {
        if (isFeeding) return;
        isFeeding = true;
        StartCoroutine(FeedFishSequence());
    }

    IEnumerator FeedFishSequence()
    {
        // One reusable visual — no per-frame Instantiate/Destroy
        GameObject fishVisual = Instantiate(fishPrefab, fishCountText.transform.position, Quaternion.identity, fishSpawnParent);
        fishVisual.SetActive(false);

        while (true)
        {
            // Wait until there is at least one fish ready and conditions are met
            yield return new WaitUntil(() =>
                coughtFishCount > 0 &&
                !dragon.IsBusy &&
                currentZone == InteractionType.Dragon &&
                !_isMoving
            );

            // Feed exactly ONE fish per cycle
            coughtFishCount--;
            fishCountText.text = coughtFishCount.ToString();

            // Animate the visual flying to the dragon plate
            fishVisual.transform.position = fishCountText.transform.position;
            fishVisual.SetActive(true);

            float duration = 0.1f;
            float elapsed = 0f;
            Vector3 startPos = fishCountText.transform.position;

            while (elapsed < duration)
            {
                fishVisual.transform.position = Vector3.Lerp(startPos, dragon.plate.position, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            fishVisual.SetActive(false);

            dragon.FeedAnimation();
            dragon.MeltIce();
            dragon.FeedFishOneByOne(1);

            // Wait for the dragon animation, but cut short if new fish arrive
            // (other fishermen's contributions skip the remaining delay)
            float animWait = 4f;
            float waited = 0f;
            while (waited < animWait)
            {
                // If a new fish arrived from another fisherman, feed it immediately
                if (coughtFishCount > 0 && !dragon.IsBusy &&
                    currentZone == InteractionType.Dragon && !_isMoving)
                    break;
                waited += Time.deltaTime;
                yield return null;
            }
        }
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
                FeedFishToDragon();
                break;

            case InteractionType.Fisherman:
                OnPurchaseButtonClick();
                break;

        }
    }

    void StartFishing()
    {
        if(canUseSpear)
        {
            animator.SetTrigger("IsSecondFisherman");
            return;
        }
        if (isFishing) return;
        
        isFishing = true;
        animator.SetBool("IsFishing", true);
    }

    private void OnTriggerEnter(Collider other)
    {
        InteractionZone zone = other.GetComponent<InteractionZone>();
        if (zone == null) return;

        currentZone = zone.type;
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