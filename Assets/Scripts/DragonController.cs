using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragonController : MonoBehaviour
{
    public int currentStage = 1; // 1: Young, 2: Teen, 3: Adult
    public Animator dragonAnimator;
    public Transform[] spawnPoints;
    public TerrainPainter terrainPainter;
    public GameObject endScreen, DragonLevel2, loseLevelScreen;
    public GameObject villagerAnimation; // Visual for "freeing villagers"
    public Transform plate;
    public TextMeshProUGUI fishCountText;
    public Image fishBGFill;
    public GameObject flameThrowEffect, upgradeEffect;
    public GameObject[] currentDragonObjects;
    public Vector3 lowerPoint;
    public GameObject boatObject, endVillageViewObject;
    public Transform level2GrassPatchParent, level3GrassPatchParent, level2SnowPatchParent, level3SnowPathParent; 
    public int[] requirements = { 2, 5, 10 }; // Fish needed for Stage 1, 2, 3
    private int fishFed = 0;
    public bool IsBusy { get; private set; }

    public void OnDisable()
    {
        terrainPainter.ResetToOriginal();
    }

    private void UpgradeToLevel2()
    {
        SoundController.Instance.PlaySFX(SoundType.FireBreath);
        DragonLevel2.SetActive(true); // Show new dragon visuals
        foreach (var obj in currentDragonObjects) obj.SetActive(false); // Hide old dragon visuals
        dragonAnimator = DragonLevel2.GetComponent<Animator>(); // Switch to new animator
        dragonAnimator.SetTrigger("Firebreath_L"); // Play upgrade animation
        //SpawnFishermen(spawnPoints.Length); // Spawns more as dragon grows
        upgradeEffect.SetActive(true);
        DOVirtual.DelayedCall(3f, () => upgradeEffect.SetActive(false));
        currentStage++;
        terrainPainter.StartPaint(45);
        terrainPainter.StopFreezing(); // Stop any ongoing freezing
        DOVirtual.DelayedCall(8f, () => terrainPainter.DebugResumeFreeze());
        fishCountText.text = Math.Max(0,requirements[currentStage - 1] - fishFed).ToString(); // Update UI with remaining fish needed
    }

    public void UpgradeDragon()
    {
        IsBusy = true; // ⛔ BLOCK feeding
        fishFed = 0;
        CurrencyHandler.Instance.AddGoldFromFish(4);
        SoundController.Instance.PlaySFX(SoundType.Upgrade);
        flameThrowEffect.SetActive(true); 
        if (villagerAnimation != null) villagerAnimation.SetActive(true);
        MoveSnowDown();
        dragonAnimator.SetTrigger("Upgrade2");
        if (currentStage < 2)
        {
            UpgradeToLevel2();
        }
        else
        {
            FinalWin();
        }

        StartCoroutine(ResumeAfterDelay(6f));
    }

    IEnumerator ResumeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        IsBusy = false;
    }

    public void StartFlamethrower()
    {
        flameThrowEffect.gameObject.SetActive(true);
    }

    public void StopFlamethrower()
    {
        flameThrowEffect.gameObject.SetActive(false);
    }

    public void MoveSnowDown()
    {
        if(currentStage == 2)
        {
            level2GrassPatchParent.DOMove(lowerPoint + level2GrassPatchParent.position, 1f);
            DOVirtual.DelayedCall(1f, () => level2SnowPatchParent.gameObject.SetActive(false));
            boatObject.gameObject.SetActive(true);
        }
        if(currentStage == 1)
        {
            level3GrassPatchParent.DOMove(lowerPoint + level3GrassPatchParent.position, 1f);
            DOVirtual.DelayedCall(1f, () => level3SnowPathParent.gameObject.SetActive(false));
        }
    }
    public void OnUpgradeAnimationComplete()
    {
        IsBusy = false;
    }
    public bool canAddCoins = true;

    public void MeltIce()
    {
        terrainPainter.stopFreezing = true; // Stop any ongoing freezing
        terrainPainter.StartPaint(terrainPainter.brushSize + 10);
        DOVirtual.DelayedCall(3f, () => terrainPainter.stopFreezing = false);
    }
    public void FeedFishOneByOne(int fishAmount)
    {
        if (fishAmount <= 0) return;
        fishFed += fishAmount;
        fishBGFill.fillAmount = 1f - (float)fishFed / (float)requirements[currentStage - 1] ;
        fishCountText.text = Math.Max(0,requirements[currentStage - 1] - fishFed).ToString(); // Update UI with remaining fish needed
        // 🔥 Play animation per fish
        SoundController.Instance.PlaySFX(SoundType.Feed);
        if(canAddCoins) 
        {
            canAddCoins = false;
            CurrencyHandler.Instance.AddGoldFromFish(1);
        }
        if (fishFed >= requirements[currentStage - 1])
        {
            UpgradeDragon();
        }

    }

    public void FeedAnimation() => dragonAnimator.SetTrigger("Feed");

    public void FeedFish(int fishAmount)
    {
        if (fishAmount <= 0) return;

        fishFed += fishAmount;
        SoundController.Instance.PlaySFX(SoundType.Feed);
    }

    void FinalWin()
    {
        Debug.Log("Congratulations! You've saved the city!");
        SoundController.Instance.PlaySFX(SoundType.Win);
        terrainPainter.StartPaint(75);
        dragonAnimator.SetTrigger("Upgrade3");
        DOVirtual.DelayedCall(3f, () => terrainPainter.StartPaint(800));
    }

    void Start()
    {
        terrainPainter.StartFreezing(() => loseLevelScreen.SetActive(true));
    }
}