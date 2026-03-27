using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragonController : MonoBehaviour
{
    public int currentStage = 1; // 1: Young, 2: Teen, 3: Adult
    public Animator dragonAnimator;
    public Transform[] spawnPoints;
    public GameObject endScreen, DragonLevel2;
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

    public void UpgradeDragon()
    {
        // 1. Play Growing/Eating Animation
        fishFed = 0;
        CurrencyHandler.Instance.AddGoldFromFish(4);
        // dragonAnimator.Play("Grow_2");
        SoundController.Instance.PlaySFX(SoundType.Upgrade);
        flameThrowEffect.SetActive(true); 
        if (villagerAnimation != null) villagerAnimation.SetActive(true);
        MoveSnowDown();
        dragonAnimator.SetTrigger("Upgrade2");
        //EnableGrassObjectAndDisableSnowObject();
        // 3. Increment stage and spawn more fishermen
        if (currentStage < 2)
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
            fishCountText.text = Math.Max(0,requirements[currentStage - 1] - fishFed).ToString(); // Update UI with remaining fish needed
        }
        else
        {
            FinalWin(); // End screen on last stage
        }
    }

    public void StartFlamethrower()
    {
        flameThrowEffect.gameObject.SetActive(true);
    }

    public void StopFlamethrower()
    {
        flameThrowEffect.gameObject.SetActive(false);
    }

    [ContextMenu("Level up")]
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

    public bool canAddCoins = true;
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
        Fisherman[] allFishermen = FindObjectsOfType<Fisherman>();
        foreach (var f in allFishermen) f.StopFishing();
        
        Debug.Log("Congratulations! You've saved the city!");
        SoundController.Instance.PlaySFX(SoundType.Win);
        // endVillageViewObject.gameObject.SetActive(true);
        // DOVirtual.DelayedCall(4.1f, () => endVillageViewObject.gameObject.SetActive(false));
        // DOVirtual.DelayedCall(4.1f, () => endScreen.gameObject.SetActive(true));
    }
}