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
    public GameObject flameThrowEffectLevel2, flameThrowEffectLevel3, upgradeEffect;
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
        DragonLevel2.SetActive(true);
        foreach (var obj in currentDragonObjects) obj.SetActive(false);
        dragonAnimator = DragonLevel2.GetComponent<Animator>();
        dragonAnimator.SetTrigger("Firebreath_L");
        StartFlamethrower();
        upgradeEffect.SetActive(true);
        DOVirtual.DelayedCall(3f, () => upgradeEffect.SetActive(false));
        currentStage++;

        terrainPainter.StopFreezing();
        terrainPainter.StartPaint(45);
        terrainPainter.snowMeter?.OnUpgrade(1);  // meter handles resume internally

        // Resume terrain freeze after melt + upgrade animation
        DOVirtual.DelayedCall(3f, () => {
            terrainPainter.stopFreezing = false;
            terrainPainter.snowMeter?.StartFreezing();
        });

        fishCountText.text = Math.Max(0, requirements[currentStage - 1] - fishFed).ToString();

    }   

    public void UpgradeDragon()
    {
        IsBusy = true; // ⛔ BLOCK feeding
        fishFed = 0;
        CurrencyHandler.Instance.AddGoldFromFish(4);
        SoundController.Instance.PlaySFX(SoundType.Upgrade);
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
        flameThrowEffectLevel2.gameObject.SetActive(true);
        flameThrowEffectLevel3.gameObject.SetActive(true);
    }

    public void StopFlamethrower()
    {
        flameThrowEffectLevel2.gameObject.SetActive(false);
        flameThrowEffectLevel3.gameObject.SetActive(false);
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
        terrainPainter.StopFreezing();
        terrainPainter.StartPaint(terrainPainter.brushSize + (1/currentStage));
        DOVirtual.DelayedCall(0.2f, () => {
            terrainPainter.stopFreezing = false;
            terrainPainter.snowMeter?.OnFeedingStopped(); // resumes freeze
        });
    }
    public void FeedFishOneByOne(int fishAmount)
    {
        if (fishAmount <= 0) return;
    fishFed += fishAmount;
    fishBGFill.fillAmount = 1f - (float)fishFed / requirements[currentStage - 1];
    fishCountText.text = Math.Max(0, requirements[currentStage - 1] - fishFed).ToString();
    SoundController.Instance.PlaySFX(SoundType.Feed);
    
    terrainPainter.snowMeter?.OnFishFed(2f); // ← each fish raises temp 2°C
    terrainPainter.stopFreezing = true;       // ← pause terrain freeze too

    if (canAddCoins)
    {
        canAddCoins = false;
        CurrencyHandler.Instance.AddGoldFromFish(1);
    }
    if (fishFed >= requirements[currentStage - 1])
        UpgradeDragon();
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
        Debug.Log("Congratulations!");
        SoundController.Instance.PlaySFX(SoundType.Win);
        terrainPainter.StartPaint(75);
        dragonAnimator.SetTrigger("Upgrade3");
        DOVirtual.DelayedCall(dragonFlyDelayForFlameStart, () => StartFlamethrower());
        DOVirtual.DelayedCall(dragonFlyDelayForFlameEnd,   () => StopFlamethrower());
        terrainPainter.StopFreezing();
        terrainPainter.isGameOver = true;
        terrainPainter.snowMeter?.OnUpgrade(2); // handles 30°C → delay → 40°C internally
        DOVirtual.DelayedCall(3f, () => terrainPainter.StartPaint(800));
    }

    public float dragonFlyDelayForFlameStart = 2f;
    public float dragonFlyDelayForFlameEnd = 5f;

    void Start()
    {
        DOVirtual.DelayedCall(3.8f, () =>
        { 
            terrainPainter.StartPaint(terrainPainter.brushSize);
        });
        DOVirtual.DelayedCall(4.8f, () =>
        { 
            terrainPainter.StartFreezing(() => loseLevelScreen.SetActive(true));
        });
    }
}